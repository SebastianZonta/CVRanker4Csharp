using CVRanker.Domain;
namespace CVRanker.Application;

/// <summary>"Rankear" button: ranks once, freezes the snapshot, stores it. No auto re-rank.</summary>
public sealed class RankingService(IRanker ranker, ISnapshotStore store, string scorerVersion = Bm25Ranker.ScorerVersion)
{
    public RankingSnapshot Rank(Offer offer, IEnumerable<CandidateCv> cvs)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(cvs);

        var frozenCvs = cvs.ToList();
        var results = ranker.Rank(offer, frozenCvs).ToList();

        var snapshot = RankingSnapshot.Create(offer, frozenCvs, results, scorerVersion);
        store.Save(snapshot);
        return snapshot;
    }
}
