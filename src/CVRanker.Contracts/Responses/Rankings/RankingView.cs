namespace CVRanker.Contracts.Responses.Rankings;

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
}
