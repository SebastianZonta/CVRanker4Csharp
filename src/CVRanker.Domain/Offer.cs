namespace CVRanker.Domain;

/// <summary>Editable offer input: free-text JD + must/nice lists + weights (v1 defaults when omitted).</summary>
public sealed record Offer(
    string JobDescription,
    IReadOnlyList<string> MustHave,
    IReadOnlyList<string> NiceToHave,
    ScoringWeights? Weights = null)
{
    public ScoringWeights EffectiveWeights => Weights ?? ScoringWeights.Default;
}
