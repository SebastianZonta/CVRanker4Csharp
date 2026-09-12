using CVRanker.Contracts.Responses.Rankings;
using CVRanker.Domain;

namespace CVRanker;

/// <summary>Temporary home for the snapshot→view projection; moves to Application mappers in step 3.</summary>
public static class RankingViews
{
    public static RankingView FromSnapshot(
        RankingSnapshot snapshot,
        bool mustCompleteOnly = false,
        string? keyword = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var byId = snapshot.Cvs.ToDictionary(c => c.Id, StringComparer.Ordinal);

        var items = snapshot.Results
            .Where(r => !mustCompleteOnly || !r.HasMustMissing)
            .Where(r => string.IsNullOrWhiteSpace(keyword) ||
                (byId.TryGetValue(r.CvId, out var cv) &&
                 cv.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select((r, idx) => new RankingViewItem(
                idx + 1, r.CvId, r.Score, r.TopReasons,
                r.HasMustMissing, r.MissingMust, Map(r.Breakdown)))
            .ToList();

        return new RankingView(
            snapshot.Id, RankingView.SupportToolDisclaimer, RankingView.BlindPhaseBannerText, items);
    }

    private static ScoreBreakdown Map(FeatureBreakdown f) => new(
        f.Must, f.Bm25, f.Nice, f.Experience, f.Education, f.Title, f.Languages);
}
