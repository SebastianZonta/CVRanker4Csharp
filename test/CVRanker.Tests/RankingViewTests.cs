using CVRanker.Application.Mappers;
using CVRanker;
using CVRanker.Application;
using CVRanker.Contracts.Responses.Rankings;
using CVRanker.Domain;
using System.Text.Json;

namespace CVRanker.Tests;

public sealed class RankingViewTests
{
    private static RankingSnapshot SampleSnapshot()
    {
        var service = new RankingService(new Bm25Ranker(), new InMemorySnapshotStore());
        var offer = new Offer(
            "Senior backend engineer with C# .NET REST SQL Azure",
            ["c#", ".net", "rest", "sql"],
            ["azure", "docker", "english"],
            ScoringWeights.Default);
        var cvs = new List<CandidateCv>
        {
            new("A", "Senior C# .NET engineer, 8 years, REST APIs, SQL Server, Azure, Docker, English fluent.", 8, HasDegree: true, IsSenior: true, HasLanguage: true),
            new("B", "Java backend engineer, 6 years, Spring, REST, PostgreSQL, AWS, English.", 6, HasDegree: true, IsSenior: true, HasLanguage: true),
            new("C", "Junior C# .NET developer, 1 year, REST, SQL basics, Spanish only.", 1, HasDegree: true, IsSenior: false, HasLanguage: false),
        };
        return service.Rank(offer, cvs);
    }

    [Fact]
    public void FromSnapshot_ProjectsFullViewWithDisclaimerAndBanner()
    {
        var view = RankingViewMapper.FromSnapshot(SampleSnapshot());

        Assert.Equal(3, view.Items.Count);
        Assert.Equal(CVRanker.Contracts.Responses.Rankings.RankingView.SupportToolDisclaimer, view.Disclaimer);
        Assert.False(string.IsNullOrWhiteSpace(view.Disclaimer));
        Assert.False(string.IsNullOrWhiteSpace(view.BlindPhaseBanner));

        var first = view.Items[0];
        Assert.Equal(1, first.Rank);
        Assert.InRange(first.Score, 0, 100);
        Assert.NotEmpty(first.TopReasons);
        Assert.True(first.TopReasons.Count <= 3);
        Assert.NotNull(first.Breakdown);
    }

    [Fact]
    public void FromSnapshot_MustCompleteOnly_HidesFlaggedCandidates()
    {
        var view = RankingViewMapper.FromSnapshot(SampleSnapshot(), mustCompleteOnly: true);

        Assert.All(view.Items, i => Assert.False(i.HasMustMissing));
        Assert.DoesNotContain(view.Items, i => i.CvRef == "B");
    }

    [Fact]
    public void FromSnapshot_Keyword_FiltersByCvText()
    {
        var view = RankingViewMapper.FromSnapshot(SampleSnapshot(), keyword: "azure");

        Assert.Contains(view.Items, i => i.CvRef == "A");
        Assert.DoesNotContain(view.Items, i => i.CvRef == "B");
    }

    [Fact]
    public void ViewModel_SerializesWithoutPersonalFields()
    {
        // Anonymization is structural: no name/photo/age/gender/address fields exist anywhere in the view.
        var json = JsonSerializer.Serialize(RankingViewMapper.FromSnapshot(SampleSnapshot())).ToLowerInvariant();

        foreach (var field in new[] { "name", "photo", "age", "gender", "address", "direcci", "edad", "sexo", "foto", "nombre" })
            Assert.DoesNotContain($"\"{field}\"", json);

        // And those fields are never features either.
        var props = typeof(FeatureBreakdown).GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();
        foreach (var field in new[] { "name", "photo", "age", "gender", "address" })
            Assert.DoesNotContain(field, props);
    }
}
