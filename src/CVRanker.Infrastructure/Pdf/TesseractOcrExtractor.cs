using System.Diagnostics;
using CVRanker.Application;

namespace CVRanker.Infrastructure;

/// <summary>Local Tesseract OCR via CLI (eng pinned, bundled tessdata preferred). Stateless: one process per image.</summary>
public sealed class TesseractOcrExtractor : IOcrExtractor
{
    public const string EngineName = "tesseract";
    public const string Language = "eng";

    private readonly string _cliPath;
    private readonly string _language;
    private readonly string? _tessDataDir;
    private readonly string _version;

    public TesseractOcrExtractor(string tessDataPath, string cliPath = "tesseract")
    {
        _cliPath = cliPath;
        _language = Language;
        _tessDataDir = ResolveDirectory(tessDataPath, Language);
        _version = ReadVersion(cliPath);
    }

    public OcrResult Extract(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        var imagePath = Path.Combine(Path.GetTempPath(), $"cvranker-ocr-{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(imagePath, imageBytes);
            var text = Run(imagePath, null);
            var confidences = ParseWordConfidences(Run(imagePath, "tsv"));
            return new OcrResult(text.Trim(), confidences.Count > 0 ? confidences.Average() / 100f : 0f, EngineName, _version);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    private string Run(string imagePath, string? config)
    {
        var args = config is null
            ? $"\"{imagePath}\" stdout -l {_language} --psm 6"
            : $"\"{imagePath}\" stdout -l {_language} --psm 6 {config}";
        if (_tessDataDir is not null)
            args += $" --tessdata-dir \"{_tessDataDir}\"";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(_cliPath, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        try
        {
            process.Start();
        }
        catch (Exception e) when (e is not ArgumentNullException)
        {
            throw new InvalidOperationException(
                $"Tesseract CLI not found at '{_cliPath}'. Install tesseract-ocr (apt) or set Ocr:CliPath.", e);
        }
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(60_000) || process.ExitCode != 0)
            throw new InvalidOperationException($"Tesseract failed (exit {process.ExitCode}): {error.Trim()}");
        return output;
    }

    // TSV levels: 1 page, 2 block, 3 para, 4 line, 5 word. Mean of word confidences (0-based col 10, -1 = none).
    private static List<float> ParseWordConfidences(string tsv)
    {
        var confidences = new List<float>();
        foreach (var line in tsv.Split('\n').Skip(1))
        {
            var cols = line.Split('\t');
            if (cols.Length < 12 || cols[0] != "5")
                continue;
            if (float.TryParse(cols[10], out var conf) && conf >= 0)
                confidences.Add(conf);
        }
        return confidences;
    }

    private static string ReadVersion(string cliPath)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(cliPath, "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            },
        };
        try
        {
            process.Start();
            var first = process.StandardOutput.ReadLine() ?? string.Empty;
            process.WaitForExit(10_000);
            return first.Replace("tesseract", string.Empty).Trim();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Tesseract CLI not found at '{cliPath}'. Install tesseract-ocr (apt) or set Ocr:CliPath.", e);
        }
    }

    private static string? ResolveDirectory(string tessDataPath, string language)
    {
        var candidates = new[]
        {
            tessDataPath,
            Path.Combine(AppContext.BaseDirectory, "tessdata"),
            Path.Combine(AppContext.BaseDirectory, tessDataPath),
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), tessDataPath),
        };
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate) && Path.GetFileName(candidate) == $"{language}.traineddata")
                return Path.GetDirectoryName(candidate)!;
            var file = Path.Combine(candidate, $"{language}.traineddata");
            if (File.Exists(file))
                return candidate;
        }
        return null; // fall back to system tessdata
    }
}
