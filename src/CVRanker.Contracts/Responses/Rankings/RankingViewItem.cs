namespace CVRanker.Contracts.Responses.Rankings;

public sealed record RankingViewItem(
    int Rank,
    string CvRef,
    double Score,
    IReadOnlyList<string> TopReasons,
    bool HasMustMissing,
    IReadOnlyList<string> MissingMust,
    ScoreBreakdown Breakdown);
