using CVRanker.Domain;
namespace CVRanker.Tests;

public sealed class PilotTests
{
    [Fact]
    public void Metrics_PAt10_Mrr_Ndcg_OnFixedRanking()
    {
        // Ranking: R1 grade 3, R2 grade 0, R3 grade 1
        var ranked = new List<string> { "R1", "R2", "R3" };
        var grades = new Dictionary<string, int> { ["R1"] = 3, ["R2"] = 0, ["R3"] = 1 };

        Assert.Equal(0.2, RetrievalMetrics.PAt10(ranked, grades), precision: 9); // 2 relevant / 10
        Assert.Equal(1.0, RetrievalMetrics.Mrr(ranked, grades), precision: 9); // first relevant at rank 1

        double ideal = 7 / 1 + 1 / Math.Log2(3); // gains 7,0,1 ordered 7,1,0
        double actual = 7 / 1 + 1 / Math.Log2(4); // gains 7,0,1 in rank order
        Assert.Equal(actual / ideal, RetrievalMetrics.NdcgAt10(ranked, grades), precision: 9);
    }

    [Fact]
    public void Metrics_NoRelevant_ReturnsZero()
    {
        var ranked = new List<string> { "R1", "R2" };
        var grades = new Dictionary<string, int> { ["R1"] = 0, ["R2"] = 0 };

        Assert.Equal(0, RetrievalMetrics.PAt10(ranked, grades));
        Assert.Equal(0, RetrievalMetrics.Mrr(ranked, grades));
        Assert.Equal(0, RetrievalMetrics.NdcgAt10(ranked, grades));
    }

    [Fact]
    public void BlindReviewSet_ContainsTopHiredAndRandoms_ShuffledWithSeed()
    {
        var ranked = Enumerable.Range(1, 40).Select(i => $"C{i:00}").ToList();

        var first = BlindReviewSet.Build(ranked, hiredRef: "C35", topN: 20, randomCount: 10, seed: 7);
        var second = BlindReviewSet.Build(ranked, hiredRef: "C35", topN: 20, randomCount: 10, seed: 7);

        Assert.Equal(31, first.Count); // top-20 + hired (outside top) + 10 randoms
        Assert.Contains("C35", first);
        Assert.All(ranked.Take(20), r => Assert.Contains(r, first));
        Assert.Equal(first, second); // reproducible
        Assert.NotEqual(ranked.Take(31).ToList(), first); // order hidden (shuffled)
    }

    [Fact]
    public void Calibrator_GridSearch_NeverWorseThanDefaults()
    {
        var offer = new Offer("C# .NET backend REST SQL", ["c#", ".net"], ["azure"], ScoringWeights.Default);
        var cases = new List<PilotCase>
        {
            new(offer,
                [
                    new("H", "C# .NET developer REST SQL backend services", 5, HasDegree: true, IsSenior: false, HasLanguage: true),
                    new("D", "C# .NET backend REST SQL C# .NET backend REST SQL C# .NET", 5, HasDegree: true, IsSenior: true, HasLanguage: true),
                ],
                new Dictionary<string, int> { ["H"] = 3, ["D"] = 0 }),
        };

        var best = WeightCalibrator.GridSearch(cases);
        var ranker = new Bm25Ranker();

        Assert.InRange(best.Must, 0, 0.6);
        Assert.InRange(best.Bm25, 0, 0.6);
        Assert.InRange(best.Nice, 0, 0.6);
        Assert.True(WeightCalibrator.MeanObjective(cases, ranker, best) >=
                    WeightCalibrator.MeanObjective(cases, ranker, ScoringWeights.Default));

        var calibrated = WeightCalibrator.Calibrate(cases);
        Assert.Contains(calibrated.K1, new[] { 0.8, 1.2, 1.6 });
        Assert.Contains(calibrated.B, new[] { 0.5, 0.75, 0.9 });
        Assert.True(WeightCalibrator.MeanObjective(cases, new Bm25Ranker(calibrated.K1, calibrated.B), calibrated.Weights) >=
                    WeightCalibrator.MeanObjective(cases, ranker, ScoringWeights.Default));
    }

    [Fact]
    public void Metrics_Ndcg_IdealCountsRelevantBeyondTop10()
    {
        // R2 grade 1 (gain 1) inside top-10; R12 grade 3 (gain 7) outside.
        // DCG@10 sees only R2, but IDCG@10 must rank both relevant first.
        var ranked = Enumerable.Range(1, 12).Select(i => $"R{i}").ToList();
        var grades = new Dictionary<string, int> { ["R2"] = 1, ["R12"] = 3 };

        double expected = (1 / Math.Log2(3)) / (7 + 1 / Math.Log2(3));
        Assert.Equal(expected, RetrievalMetrics.NdcgAt10(ranked, grades), precision: 9);
    }

    [Fact]
    public void PilotEvaluator_SuccessRule_MatchesTicket()
    {
        // 2/3 hired in top-10 + saving ≥50% -> success
        var pilots = new List<PilotResult>
        {
            new(HiredInTop10: true, Mrr: 0.5, PAt10: 0.2, NdcgAt10: 0.8, TriageSaving: 0.6),
            new(HiredInTop10: true, Mrr: 1.0, PAt10: 0.3, NdcgAt10: 0.9, TriageSaving: 0.7),
            new(HiredInTop10: false, Mrr: 0.05, PAt10: 0.1, NdcgAt10: 0.3, TriageSaving: 0.6),
        };
        Assert.True(PilotEvaluator.IsSuccess(pilots));

        // Saving below 50% -> fail even with good ranks
        var noSaving = pilots.Select(p => p with { TriageSaving = 0.2 }).ToList();
        Assert.False(PilotEvaluator.IsSuccess(noSaving));

        // Mean MRR >= 0.2 rescues 1/3 hired-in-top-10
        var mrrRescue = new List<PilotResult>
        {
            new(HiredInTop10: true, Mrr: 0.5, PAt10: 0.2, NdcgAt10: 0.8, TriageSaving: 0.6),
            new(HiredInTop10: false, Mrr: 0.25, PAt10: 0.1, NdcgAt10: 0.3, TriageSaving: 0.6),
            new(HiredInTop10: false, Mrr: 0.2, PAt10: 0.1, NdcgAt10: 0.3, TriageSaving: 0.6),
        };
        Assert.True(PilotEvaluator.IsSuccess(mrrRescue));
    }

    [Fact]
    public void PilotEvaluator_Evaluate_ComputesSavingAndHiredFlag()
    {
        var ranked = new List<string> { "H", "X", "Y", "Z" };
        var grades = new Dictionary<string, int> { ["H"] = 2 };

        var result = PilotEvaluator.Evaluate(ranked, grades, hiredRef: "H", reviewSetSize: 2);

        Assert.True(result.HiredInTop10);
        Assert.Equal(1.0, result.Mrr, precision: 9);
        Assert.Equal(0.5, result.TriageSaving, precision: 9); // 1 - 2/4
    }
}
