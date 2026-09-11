namespace CVRanker;

public sealed record PdfExtractionResult(
    string Text,
    IReadOnlyList<string> PageTexts,
    int PageCount,
    IReadOnlyList<int> PagesWithoutText)
{
    public bool NeedsOcr => PagesWithoutText.Count > 0;
}
