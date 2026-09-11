namespace CVRanker;

public sealed record RankedCandidate(
    string CvId,
    int Rank,
    double Score,
    IReadOnlyList<string> TopReasons,
    IReadOnlyList<string> MissingMust,
    FeatureBreakdown Breakdown)
{
    public bool HasMustMissing => MissingMust.Count > 0;
}
