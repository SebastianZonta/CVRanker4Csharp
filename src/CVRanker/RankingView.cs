using CVRanker.Domain;
namespace CVRanker;

/// <summary>HR ranking view: anonymized by construction (opaque refs, no PII fields exist to leak).</summary>
public sealed record RankingViewItem(
    int Rank,
    string CvRef,
    double Score,
    IReadOnlyList<string> TopReasons,
    bool HasMustMissing,
    IReadOnlyList<string> MissingMust,
    FeatureBreakdown Breakdown);

public sealed record RankingView(
    string SnapshotId,
    string Disclaimer,
    string BlindPhaseBanner,
    IReadOnlyList<RankingViewItem> Items)
{
    public const string SupportToolDisclaimer =
        "Herramienta de apoyo a la preselección — no es una decisión automatizada.";

    public const string BlindPhaseBannerText =
        "Fase ciega: la identidad del candidato está oculta. El PDF original solo se abre tras esta advertencia.";

    /// <summary>Projects a frozen snapshot; optional HR filters (must-complete, keyword over CV text).</summary>
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
                r.HasMustMissing, r.MissingMust, r.Breakdown))
            .ToList();

        return new RankingView(
            snapshot.Id, SupportToolDisclaimer, BlindPhaseBannerText, items);
    }
}
