namespace CVRanker.Domain;

/// <summary>Frozen auditable snapshot: inputs + scorer version + results. Re-rank is manual only (new Rank call).</summary>
public sealed record RankingSnapshot(
    string Id,
    DateTimeOffset CreatedAt,
    Offer Offer,
    IReadOnlyList<CandidateCv> Cvs,
    IReadOnlyList<RankedCandidate> Results,
    string ScorerVersion)
{
    /// <summary>Freezing factory: defensive copies so later edits to the caller's lists never leak in.</summary>
    public static RankingSnapshot Create(
        Offer offer,
        IEnumerable<CandidateCv> cvs,
        IReadOnlyList<RankedCandidate> results,
        string scorerVersion)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(cvs);
        ArgumentNullException.ThrowIfNull(results);
        var frozenOffer = offer with
        {
            MustHave = offer.MustHave.ToList(),
            NiceToHave = offer.NiceToHave.ToList(),
        };
        return new RankingSnapshot(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            frozenOffer,
            cvs.ToList(),
            results.ToList(),
            scorerVersion);
    }
}
