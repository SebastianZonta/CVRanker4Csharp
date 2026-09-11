namespace CVRanker;

public sealed record PilotResult(
    bool HiredInTop10,
    double Mrr,
    double PAt10,
    double NdcgAt10,
    double TriageSaving);

/// <summary>Per-pilot metrics + build-wide success rule: hired top-10 in ≥2/3 (or mean MRR ≥0.2) AND mean triage saving ≥50%.</summary>
public static class PilotEvaluator
{
    public static PilotResult Evaluate(
        IReadOnlyList<string> rankedRefs,
        IReadOnlyDictionary<string, int> gradesByRef,
        string hiredRef,
        int reviewSetSize)
    {
        int total = rankedRefs.Count;
        return new PilotResult(
            rankedRefs.Take(10).Contains(hiredRef),
            RetrievalMetrics.Mrr(rankedRefs, gradesByRef),
            RetrievalMetrics.PAt10(rankedRefs, gradesByRef),
            RetrievalMetrics.NdcgAt10(rankedRefs, gradesByRef),
            total == 0 ? 0 : 1.0 - (double)reviewSetSize / total);
    }

    public static bool IsSuccess(IReadOnlyList<PilotResult> pilots)
    {
        if (pilots.Count == 0)
            return false;
        double hiredTop10Rate = (double)pilots.Count(p => p.HiredInTop10) / pilots.Count;
        double meanMrr = pilots.Average(p => p.Mrr);
        double meanSaving = pilots.Average(p => p.TriageSaving);
        return (hiredTop10Rate >= 2.0 / 3.0 || meanMrr >= 0.2) && meanSaving >= 0.5;
    }
}
