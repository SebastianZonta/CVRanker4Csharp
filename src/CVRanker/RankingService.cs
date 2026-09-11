namespace CVRanker;

/// <summary>"Rankear" button: ranks once, freezes the snapshot, stores it. No auto re-rank.</summary>
public sealed class RankingService(IRanker ranker, ISnapshotStore store, string scorerVersion = Bm25Ranker.ScorerVersion)
{
    public RankingSnapshot Rank(Offer offer, IEnumerable<CandidateCv> cvs)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(cvs);

        // Defensive copies: later edits to the caller's lists must not leak into the frozen snapshot.
        var frozenOffer = offer with
        {
            MustHave = offer.MustHave.ToList(),
            NiceToHave = offer.NiceToHave.ToList(),
        };
        var frozenCvs = cvs.ToList();
        var results = ranker.Rank(frozenOffer, frozenCvs).ToList();

        var snapshot = new RankingSnapshot(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            frozenOffer,
            frozenCvs,
            results,
            scorerVersion);
        store.Save(snapshot);
        return snapshot;
    }
}
