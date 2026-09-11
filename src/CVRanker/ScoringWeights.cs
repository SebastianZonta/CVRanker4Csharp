namespace CVRanker;

/// <summary>v1 default weights from the prototype scorer-v1.html.</summary>
public sealed record ScoringWeights(
    double Must,
    double Bm25,
    double Nice,
    double Experience,
    double Education,
    double Title,
    double Languages)
{
    public static ScoringWeights Default { get; } = new(
        Must: 0.35,
        Bm25: 0.25,
        Nice: 0.15,
        Experience: 0.12,
        Education: 0.05,
        Title: 0.05,
        Languages: 0.03);

    public double Sum => Must + Bm25 + Nice + Experience + Education + Title + Languages;

    public double WeightedSum(FeatureBreakdown f) =>
        Must * f.Must +
        Bm25 * f.Bm25 +
        Nice * f.Nice +
        Experience * f.Experience +
        Education * f.Education +
        Title * f.Title +
        Languages * f.Languages;
}
