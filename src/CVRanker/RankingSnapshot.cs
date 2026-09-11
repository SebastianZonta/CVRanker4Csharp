namespace CVRanker;

/// <summary>Frozen auditable snapshot: inputs + scorer version + results. Re-rank is manual only (new Rank call).</summary>
public sealed record RankingSnapshot(
    string Id,
    DateTimeOffset CreatedAt,
    Offer Offer,
    IReadOnlyList<CandidateCv> Cvs,
    IReadOnlyList<RankedCandidate> Results,
    string ScorerVersion);
