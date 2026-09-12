namespace CVRanker.Domain;

/// <summary>Per-section features in [0,1], intra-offer normalized.</summary>
public sealed record FeatureBreakdown(
    double Must,
    double Bm25,
    double Nice,
    double Experience,
    double Education,
    double Title,
    double Languages);
