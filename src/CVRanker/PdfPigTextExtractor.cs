using CVRanker.Domain;
using UglyToad.PdfPig;

namespace CVRanker;

/// <summary>PdfPig word-level extraction (never raw page text). Pages with zero words route to the OCR queue (Tesseract, out of slice).</summary>
public sealed class PdfPigTextExtractor : IPdfTextExtractor
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

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().Select(w => w.Text).Where(t => t.Length > 0).ToList();
            if (words.Count == 0)
                empty.Add(page.Number);
            pages.Add(string.Join(" ", words));
        }

        var nonEmpty = pages.Where(p => p.Length > 0).ToList();
        return new PdfExtractionResult(
            string.Join("\n", nonEmpty),
            pages,
            document.NumberOfPages,
            empty);
    }
}
