using CVRanker.Application;
using CVRanker.Domain;
using UglyToad.PdfPig;

namespace CVRanker.Infrastructure;

/// <summary>PdfPig word-level extraction (never raw page text). Pages with zero words but embedded images route to OCR; imageless pages stay flagged.</summary>
public sealed class PdfPigTextExtractor(IOcrExtractor? ocr = null) : IPdfTextExtractor
{
    public PdfExtractionResult Extract(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"PDF fixture not found: {path}", path);
        return Extract(File.ReadAllBytes(path));
    }

    public PdfExtractionResult Extract(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var pages = new List<string>(document.NumberOfPages);
        var empty = new List<int>();
        var acc = new OcrAccumulator();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().Select(w => w.Text).Where(t => t.Length > 0).ToList();
            if (words.Count > 0)
            {
                pages.Add(string.Join(" ", words));
                continue;
            }

            var ocrText = ocr is null ? null : OcrPage(page, ocr, acc);
            if (ocrText is null)
                empty.Add(page.Number);
            pages.Add(ocrText ?? string.Empty);
        }

        var nonEmpty = pages.Where(p => p.Length > 0).ToList();
        return new PdfExtractionResult(
            string.Join("\n", nonEmpty),
            pages,
            document.NumberOfPages,
            empty,
            acc.Engine,
            acc.Version,
            acc.Confidences.Count > 0 ? acc.Confidences : null);
    }

    private sealed class OcrAccumulator
    {
        public string? Engine { get; set; }
        public string? Version { get; set; }
        public Dictionary<int, float> Confidences { get; } = new();
    }

    // Q8: OCR is best-effort — any failure keeps the page flagged instead of breaking ranking.
    private static string? OcrPage(
        UglyToad.PdfPig.Content.Page page,
        IOcrExtractor ocr,
        OcrAccumulator acc)
    {
        try
        {
            var texts = new List<string>();
            var confidences = new List<float>();
            foreach (var image in page.GetImages())
            {
                if (!image.TryGetPng(out var png) || png is null || png.Length == 0)
                    continue;
                var result = ocr.Extract(png);
                if (string.IsNullOrWhiteSpace(result.Text))
                    continue;
                texts.Add(result.Text.Trim());
                confidences.Add(result.Confidence);
                acc.Engine ??= result.Engine;
                acc.Version ??= result.Version;
            }
            if (texts.Count == 0)
                return null;
            acc.Confidences[page.Number] = confidences.Average();
            return string.Join(" ", texts);
        }
        catch
        {
            return null;
        }
    }
}
