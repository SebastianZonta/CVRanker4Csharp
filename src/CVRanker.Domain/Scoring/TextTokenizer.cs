using System.Text.RegularExpressions;

namespace CVRanker.Domain;

internal static partial class TextTokenizer
{
    [GeneratedRegex(@"[^a-z0-9#+. ]")]
    private static partial Regex NonTokenChars();

    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        var lowered = text.ToLowerInvariant();
        var cleaned = NonTokenChars().Replace(lowered, " ");
        return cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
