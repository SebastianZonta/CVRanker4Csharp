using CVRanker.Contracts.Requests.Candidates;
using CVRanker.Contracts.Requests.Rankings;
using CVRanker.Domain;

namespace CVRanker.Application.Mappers;

public static class OfferMapper
{
    public static Offer ToOffer(RankRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ScoringWeights? weights = request.Weights is null ? null : new ScoringWeights(
            request.Weights.Must, request.Weights.Bm25, request.Weights.Nice,
            request.Weights.Experience, request.Weights.Education,
            request.Weights.Title, request.Weights.Languages);
        return new Offer(request.JobDescription, request.MustHave.ToList(), request.NiceToHave.ToList(), weights);
    }

    public static CandidateCv ToCandidate(CvEntry entry, string text)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new CandidateCv(entry.Ref, text, entry.ExperienceYears,
            entry.HasDegree, entry.IsSenior, entry.HasLanguage);
    }
}
