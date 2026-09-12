namespace CVRanker.Domain;

/// <summary>Per-section features in [0,1], intra-offer normalized.</summary>
public sealed record FeatureBreakdown(
    double Must,
    double Bm25,
    double Nice,
    double Experience,
    double Education,
    double Title,
    double Languages)
{
    public double Must { get; init; } = CheckRange(Must, nameof(Must));
    public double Bm25 { get; init; } = CheckRange(Bm25, nameof(Bm25));
    public double Nice { get; init; } = CheckRange(Nice, nameof(Nice));
    public double Experience { get; init; } = CheckRange(Experience, nameof(Experience));
    public double Education { get; init; } = CheckRange(Education, nameof(Education));
    public double Title { get; init; } = CheckRange(Title, nameof(Title));
    public double Languages { get; init; } = CheckRange(Languages, nameof(Languages));

    private static double CheckRange(double value, string name) =>
        double.IsNaN(value) || value < 0 || value > 1
            ? throw new ArgumentOutOfRangeException(name, value, "Feature must be in [0,1].")
            : value;
}
