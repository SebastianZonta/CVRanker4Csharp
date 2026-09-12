namespace CVRanker.Domain;

/// <summary>Blind review set: top-N + hired + randoms, shuffled with a seed so HR judges without scores or order.</summary>
public static class BlindReviewSet
{
    public static IReadOnlyList<string> Build(
        IReadOnlyList<string> rankedRefs,
        string hiredRef,
        int topN = 20,
        int randomCount = 10,
        int seed = 42)
    {
        ArgumentNullException.ThrowIfNull(rankedRefs);
        ArgumentException.ThrowIfNullOrWhiteSpace(hiredRef);

        var top = rankedRefs.Take(topN).ToList();
        var rest = rankedRefs.Skip(topN).Where(r => r != hiredRef).ToList();
        var random = new Random(seed);
        var sampled = rest.OrderBy(_ => random.Next()).Take(randomCount).ToList();

        var set = top.Concat(sampled).ToList();
        if (!set.Contains(hiredRef))
            set.Add(hiredRef);
        return set.OrderBy(_ => random.Next()).ToList();
    }
}
