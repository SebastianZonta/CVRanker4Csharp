using CVRanker.Domain;
namespace CVRanker.Application;

public sealed record PdfExtractionResult(
    string Text,
    IReadOnlyList<string> PageTexts,
    int PageCount,
    IReadOnlyList<int> PagesWithoutText,
    string? OcrEngine = null,
    string? OcrVersion = null,
    IReadOnlyDictionary<int, float>? OcrConfidenceByPage = null)
{
    public bool NeedsOcr => PagesWithoutText.Count > 0;
}
