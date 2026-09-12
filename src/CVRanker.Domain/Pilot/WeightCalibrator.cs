namespace CVRanker.Domain;

public sealed record PilotCase(
    Offer Offer,
    IReadOnlyList<CandidateCv> Cvs,
    IReadOnlyDictionary<string, int> Grades);

/// <summary>Coarse grid (step 0.05) over (must, BM25, nice), rest fixed at v1 defaults; objective = mean(nDCG@10 + P@10). b/k1 tuned only on ties.</summary>
public static class WeightCalibrator
{
    public sealed record CalibrationResult(ScoringWeights Weights, double K1, double B);

    private static readonly double[] K1Grid = [0.8, 1.2, 1.6];
    private static readonly double[] BGrid = [0.5, 0.75, 0.9];

    public static ScoringWeights GridSearch(
        IReadOnlyList<PilotCase> cases,
        double step = 0.05,
        double max = 0.6) =>
        Calibrate(cases, step, max).Weights;

    public static CalibrationResult Calibrate(
        IReadOnlyList<PilotCase> cases,
        double step = 0.05,
        double max = 0.6)
    {
        ArgumentNullException.ThrowIfNull(cases);
        if (cases.Count == 0)
            return new CalibrationResult(ScoringWeights.Default, 1.2, 0.75);

        var baseline = ScoringWeights.Default;
        double bestScore = MeanObjective(cases, new Bm25Ranker(), baseline);
        var tied = new List<ScoringWeights> { baseline };

        for (double must = 0; must <= max + 1e-9; must += step)
            for (double bm25 = 0; bm25 <= max + 1e-9; bm25 += step)
                for (double nice = 0; nice <= max + 1e-9; nice += step)
                {
                    var candidate = baseline with { Must = must, Bm25 = bm25, Nice = nice };
                    double score = MeanObjective(cases, new Bm25Ranker(), candidate);
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        tied.Clear();
                        tied.Add(candidate);
                    }
                    else if (Math.Abs(score - bestScore) <= 1e-9 && !tied.Contains(candidate))
                    {
                        tied.Add(candidate);
                    }
                }

        // Tie-break only: small b/k1 sweep over the tied weight vectors.
        var bestWeights = tied[0];
        double bestK1 = 1.2, bestB = 0.75;
        foreach (var weights in tied)
            foreach (var k1 in K1Grid)
                foreach (var b in BGrid)
                {
                    double score = MeanObjective(cases, new Bm25Ranker(k1, b), weights);
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        bestWeights = weights;
                        bestK1 = k1;
                        bestB = b;
                    }
                }

        return new CalibrationResult(bestWeights, bestK1, bestB);
    }

    public static double MeanObjective(IReadOnlyList<PilotCase> cases, IRanker ranker, ScoringWeights weights)
    {
        double total = 0;
        foreach (var c in cases)
        {
            var offer = c.Offer with { Weights = weights };
            var ranked = ranker.Rank(offer, c.Cvs).Select(r => r.CvId).ToList();
            total += RetrievalMetrics.NdcgAt10(ranked, c.Grades) + RetrievalMetrics.PAt10(ranked, c.Grades);
        }
        return total / cases.Count;
    }
}
