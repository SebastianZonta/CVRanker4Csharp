namespace CVRanker.Domain;

/// <summary>Single seam: ranking internals stay swappable behind this interface.</summary>
public interface IRanker
{
    IReadOnlyList<RankedCandidate> Rank(Offer offer, IEnumerable<CandidateCv> cvs);
}
