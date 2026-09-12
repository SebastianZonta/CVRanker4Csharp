namespace CVRanker.Contracts.Responses.Rankings;

public sealed record SnapshotSummary(
    string SnapshotId,
    DateTimeOffset CreatedAt,
    string ScorerVersion,
    int CandidateCount);
