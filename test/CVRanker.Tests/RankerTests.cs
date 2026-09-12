using CVRanker.Domain;
namespace CVRanker.Tests;

public sealed class RankerTests
{
    private static Offer SampleOffer() => new(
        JobDescription: "Senior backend engineer. Build C# .NET services on Azure, SQL Server, REST APIs. Must: C#, .NET, REST, SQL. Nice: Azure, Docker, English.",
        MustHave: ["c#", ".net", "rest", "sql"],
        NiceToHave: ["azure", "docker", "english"],
        Weights: ScoringWeights.Default);

    private static IReadOnlyList<CandidateCv> SampleCvs() =>
    [
        new("A", "Senior C# .NET engineer, 8 years, REST APIs, SQL Server, Azure, Docker, English fluent, BSc CS.", 8, HasDegree: true, IsSenior: true, HasLanguage: true),
        new("B", "Java backend engineer, 6 years, Spring, REST, PostgreSQL, AWS, English. No C#.", 6, HasDegree: true, IsSenior: true, HasLanguage: true),
        new("C", "Junior C# .NET developer, 1 year, REST, SQL basics, Spanish only.", 1, HasDegree: true, IsSenior: false, HasLanguage: false),
        new("D", "Senior C# .NET engineer, 10 years, REST, SQL Server, on-prem, no cloud, English.", 10, HasDegree: false, IsSenior: true, HasLanguage: true),
        new("E", "C# .NET engineer, 4 years, REST, SQL, Azure, English, MSc CS.", 4, HasDegree: true, IsSenior: false, HasLanguage: true),
        new("F", "Python data engineer, 5 years, ETL, SQL, Docker, English. Some C# coursework.", 5, HasDegree: true, IsSenior: false, HasLanguage: true),
    ];

    [Fact]
    public void Rank_HappyPath_ReturnsOrderedOneToNWithScoresReasonsFlagsAndBreakdown()
    {
        var ranker = new Bm25Ranker();
        var ranked = ranker.Rank(SampleOffer(), SampleCvs());

        Assert.Equal(6, ranked.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6], ranked.Select(r => r.Rank).ToArray());

        // Scores bounded 0..100 and strictly ordered by rank
        Assert.All(ranked, r => Assert.InRange(r.Score, 0, 100));
        for (int i = 1; i < ranked.Count; i++)
            Assert.True(ranked[i - 1].Score >= ranked[i].Score);

        // Full 1..N order matches the prototype scorer-v1.html fixtures
        Assert.Equal(["A", "D", "E", "C", "F", "B"], ranked.Select(r => r.CvId).ToArray());

        // Exact scores ported from the prototype (rounded to 1 decimal)
        Assert.Equal(
            [96.1, 80.0, 75.0, 55.0, 49.8, 38.9],
            ranked.Select(r => Math.Round(r.Score, 1)).ToArray());

        // Top-ranked candidate exposes must-hit reasons and a full breakdown
        var top = ranked[0];
        Assert.Equal(["must:c#", "must:.net", "must:rest"], top.TopReasons);
        Assert.Equal(1.0, top.Breakdown.Must);
        Assert.InRange(top.Breakdown.Bm25, 0, 1);

        // Every candidate exposes top-3 reasons max and a section breakdown
        Assert.All(ranked, r =>
        {
            Assert.NotNull(r.TopReasons);
            Assert.True(r.TopReasons.Count is > 0 and <= 3);
            Assert.NotNull(r.Breakdown);
        });

        // B misses must-haves -> flagged, never auto-rejected
        var b = ranked.Single(r => r.CvId == "B");
        Assert.True(b.HasMustMissing);
        Assert.NotEmpty(b.MissingMust);
        Assert.Contains(ranked, r => r.CvId == "B");

        // A covers everything -> no flag
        var a = ranked.Single(r => r.CvId == "A");
        Assert.False(a.HasMustMissing);
        Assert.Empty(a.MissingMust);
    }

    [Fact]
    public void Rank_MustZero_GoesToTailWithFlagButIsKept()
    {
        var ranker = new Bm25Ranker();
        var offer = SampleOffer();
        var cvs = new List<CandidateCv>(SampleCvs())
        {
            // Strong on paper but covers zero must-haves
            new("Z", "Senior Go engineer, 12 years, Kubernetes, payments, MSc, English fluent.", 12, HasDegree: true, IsSenior: true, HasLanguage: true),
        };

        var ranked = ranker.Rank(offer, cvs);

        Assert.Equal(7, ranked.Count);
        var z = ranked.Single(r => r.CvId == "Z");
        Assert.True(z.HasMustMissing);
        Assert.Equal(ranked.Count, z.Rank); // soft veto: tailed, never removed
    }

    [Fact]
    public void Rank_EmptyMustList_AppliesNoVeto()
    {
        var ranker = new Bm25Ranker();
        var offer = SampleOffer() with { MustHave = [] };
        var ranked = ranker.Rank(offer, SampleCvs());

        Assert.Equal(6, ranked.Count);
        Assert.All(ranked, r => Assert.False(r.HasMustMissing));
    }

    [Fact]
    public void Rank_SnapshotReproducibility_SameInputsGiveSameOrderAndScores()
    {
        var ranker = new Bm25Ranker();
        var offer = SampleOffer();
        var cvs = SampleCvs();

        var first = ranker.Rank(offer, cvs);
        var second = ranker.Rank(offer, cvs);

        Assert.Equal(first.Select(r => r.CvId), second.Select(r => r.CvId));
        Assert.Equal(first.Select(r => r.Score), second.Select(r => r.Score));
    }

    [Fact]
    public void Rank_WeightsOverride_ChangesScores()
    {
        var ranker = new Bm25Ranker();
        var cvs = SampleCvs();
        var @default = ranker.Rank(SampleOffer(), cvs);
        var bm25Heavy = SampleOffer() with
        {
            Weights = new ScoringWeights(Must: 0.1, Bm25: 0.6, Nice: 0.05, Experience: 0.1, Education: 0.05, Title: 0.05, Languages: 0.05)
        };
        var heavy = ranker.Rank(bm25Heavy, cvs);

        Assert.Equal(6, heavy.Count);
        Assert.All(heavy, r => Assert.InRange(r.Score, 0, 100));
        // At least one score must move when weights change drastically
        Assert.NotEqual(
            @default.Select(r => Math.Round(r.Score, 4)),
            heavy.Select(r => Math.Round(r.Score, 4)));
    }
}
