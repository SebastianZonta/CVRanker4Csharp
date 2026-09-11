namespace CVRanker;

/// <summary>Editable offer lists: one item per line, blanks ignored.</summary>
public static class OfferLists
{
    public static IReadOnlyList<string> ParseLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }
}
