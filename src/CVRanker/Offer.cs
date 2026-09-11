namespace CVRanker;

/// <summary>Editable offer input: free-text JD + must/nice lists + weights.</summary>
public sealed record Offer(
    string JobDescription,
    IReadOnlyList<string> MustHave,
    IReadOnlyList<string> NiceToHave,
    ScoringWeights Weights);
