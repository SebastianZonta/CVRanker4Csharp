namespace CVRanker.Domain;

/// <summary>Standard retrieval metrics over a frozen ranking. Binary relevance = grade &gt; 0 (grades valid 0-3; other values count as non-relevant).</summary>
public static class RetrievalMetrics
{
    public static double PAt10(IReadOnlyList<string> rankedRefs, IReadOnlyDictionary<string, int> gradesByRef)
    {
        var top10 = rankedRefs.Take(10).ToList();
        if (top10.Count == 0)
            return 0;
        return (double)top10.Count(r => IsRelevant(r, gradesByRef)) / 10;
    }

    public static double Mrr(IReadOnlyList<string> rankedRefs, IReadOnlyDictionary<string, int> gradesByRef)
    {
        for (int i = 0; i < rankedRefs.Count; i++)
            if (IsRelevant(rankedRefs[i], gradesByRef))
                return 1.0 / (i + 1);
        return 0;
    }

    public static double NdcgAt10(IReadOnlyList<string> rankedRefs, IReadOnlyDictionary<string, int> gradesByRef)
    {
        var gains = rankedRefs.Take(10).Select(r => Gain(r, gradesByRef)).ToList();
        double dcg = gains.Select((g, i) => g / Math.Log2(i + 2)).Sum();
        double idcg = gradesByRef.Values
            .Where(g => g > 0)
            .Select(g => Math.Pow(2, g) - 1)
            .OrderByDescending(g => g)
            .Take(10)
            .Select((g, i) => g / Math.Log2(i + 2))
            .Sum();
        return idcg <= 0 ? 0 : dcg / idcg;
    }

    private static bool IsRelevant(string cvRef, IReadOnlyDictionary<string, int> grades) =>
        grades.TryGetValue(cvRef, out var g) && g > 0;

    private static double Gain(string cvRef, IReadOnlyDictionary<string, int> grades) =>
        grades.TryGetValue(cvRef, out var g) && g > 0 ? Math.Pow(2, g) - 1 : 0;
}
