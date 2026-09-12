using CVRanker.Application;
using CVRanker;
using CVRanker.Domain;
namespace CVRanker.Tests;

public sealed class SnapshotTests
{
    private static Offer SampleOffer() => new(
        "Senior backend engineer. Build C# .NET services on Azure, SQL Server, REST APIs.",
        ["c#", ".net", "rest", "sql"],
        ["azure", "docker", "english"],
        ScoringWeights.Default);

    private static IReadOnlyList<CandidateCv> SampleCvs() =>
    [
        new("A", "Senior C# .NET engineer, 8 years, REST APIs, SQL Server, Azure, Docker, English fluent.", 8, HasDegree: true, IsSenior: true, HasLanguage: true),
        new("B", "Java backend engineer, 6 years, Spring, REST, PostgreSQL, AWS, English.", 6, HasDegree: true, IsSenior: true, HasLanguage: true),
    ];

    [Fact]
    public void ParseLines_OnePerLine_TrimsAndDropsEmpties()
    {
        Assert.Equal(["C#", ".NET", "REST"],
            OfferLists.ParseLines("C#\n.NET\n\n  REST  \n"));
        Assert.Empty(OfferLists.ParseLines("  \n\n"));
    }

    [Fact]
    public void Rank_FreezesSnapshotWithInputsVersionAndResults()
    {
        var service = new RankingService(new Bm25Ranker(), new InMemorySnapshotStore());

        var snapshot = service.Rank(SampleOffer(), SampleCvs());

        Assert.Equal(SampleOffer().JobDescription, snapshot.Offer.JobDescription);
        Assert.Equal(SampleOffer().MustHave, snapshot.Offer.MustHave);
        Assert.Equal(SampleOffer().NiceToHave, snapshot.Offer.NiceToHave);
        Assert.Equal(ScoringWeights.Default, snapshot.Offer.Weights);
        Assert.Equal(Bm25Ranker.ScorerVersion, snapshot.ScorerVersion);
        Assert.Equal(["A", "B"], snapshot.Cvs.Select(c => c.Id).ToArray());
        Assert.Equal(["A", "B"], snapshot.Results.Select(r => r.CvId).ToArray());
        Assert.Equal([1, 2], snapshot.Results.Select(r => r.Rank).ToArray());
        Assert.False(string.IsNullOrWhiteSpace(snapshot.Id));
    }

    [Fact]
    public void Snapshot_IsFrozen_LaterMutationsDoNotLeakIn()
    {
        var service = new RankingService(new Bm25Ranker(), new InMemorySnapshotStore());
        var must = new List<string> { "c#", ".net", "rest", "sql" };
        var cvs = new List<CandidateCv>(SampleCvs());
        var offer = SampleOffer() with { MustHave = must };

        var snapshot = service.Rank(offer, cvs);
        must.Clear();
        cvs.Clear();

        Assert.Equal(4, snapshot.Offer.MustHave.Count);
        Assert.Equal(2, snapshot.Cvs.Count);
        Assert.Equal(2, snapshot.Results.Count);
    }

    [Fact]
    public void Snapshot_ReproducesSameRankingAPosteriori()
    {
        var store = new InMemorySnapshotStore();
        var service = new RankingService(new Bm25Ranker(), store);

        var snapshot = service.Rank(SampleOffer(), SampleCvs());
        var reloaded = store.Get(snapshot.Id);
        var rerun = new Bm25Ranker().Rank(reloaded.Offer, reloaded.Cvs);

        Assert.Equal(reloaded.Results.Select(r => r.CvId), rerun.Select(r => r.CvId));
        Assert.Equal(reloaded.Results.Select(r => r.Score), rerun.Select(r => r.Score));
    }

    [Fact]
    public void Store_GetUnknownId_ThrowsKeyNotFound()
    {
        Assert.Throws<KeyNotFoundException>(() => new InMemorySnapshotStore().Get("missing"));
    }
}
