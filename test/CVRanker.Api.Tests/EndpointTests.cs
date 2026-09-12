using CVRanker.Contracts.Responses.Rankings;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace CVRanker.Api.Tests;

public sealed class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _pdfDir;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _pdfDir = Path.Combine(Path.GetTempPath(), "cvranker-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pdfDir);
        File.Copy(Fixture("cv-native.pdf"), Path.Combine(_pdfDir, "A.pdf"));
        File.Copy(Fixture("cv-native-2.pdf"), Path.Combine(_pdfDir, "B.pdf"));
        _factory = factory.WithWebHostBuilder(b => b.UseSetting("PdfDirectory", _pdfDir));
    }

    private static string Fixture(string name)
    {
        var dir = AppContext.BaseDirectory;
        foreach (var candidate in new[]
        {
            Path.Combine(dir, "Fixtures", name),
            Path.Combine(dir, "..", "..", "..", "..", "CVRanker.Tests", "Fixtures", name),
        })
            if (File.Exists(candidate))
                return candidate;
        throw new FileNotFoundException($"Fixture not found: {name}");
    }

    private static object RankPayload() => new
    {
        jobDescription = "Senior backend engineer with C# .NET REST SQL Azure",
        mustHave = new[] { "c#", ".net", "rest", "sql" },
        niceToHave = new[] { "azure", "docker", "english" },
        weights = (object?)null,
        cvs = new[]
        {
            new { @ref = "A", experienceYears = 8.0, hasDegree = true, isSenior = true, hasLanguage = true },
            new { @ref = "B", experienceYears = 6.0, hasDegree = true, isSenior = true, hasLanguage = true },
        },
    };

    private async Task<string> RankAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/rankings", RankPayload());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("snapshotId").GetString()!;
    }

    [Fact]
    public async Task PostRankings_MissingPdf_ReturnsBadRequestInsteadOf500()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            jobDescription = "x",
            mustHave = Array.Empty<string>(),
            niceToHave = Array.Empty<string>(),
            weights = (object?)null,
            cvs = new[] { new { @ref = "ZZZ", experienceYears = 1.0, hasDegree = false, isSenior = false, hasLanguage = false } },
        };

        var response = await client.PostAsJsonAsync("/rankings", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostRankings_FreezesSnapshot_GetReturnsFullViewWithDisclaimer()
    {
        var client = _factory.CreateClient();
        var id = await RankAsync();

        var view = await client.GetFromJsonAsync<RankingView>($"/rankings/{id}");

        Assert.NotNull(view);
        Assert.Equal(2, view.Items.Count);
        Assert.Equal("A", view.Items[0].CvRef);
        Assert.False(string.IsNullOrWhiteSpace(view.Disclaimer));
        Assert.False(string.IsNullOrWhiteSpace(view.BlindPhaseBanner));
        Assert.All(view.Items, i => Assert.InRange(i.Score, 0, 100));
    }

    [Fact]
    public async Task GetRanking_Filters_MustCompleteAndKeyword()
    {
        var client = _factory.CreateClient();
        var id = await RankAsync();

        var mustOnly = await client.GetFromJsonAsync<RankingView>($"/rankings/{id}?mustComplete=true");
        Assert.DoesNotContain(mustOnly!.Items, i => i.CvRef == "B");
        Assert.All(mustOnly.Items, i => Assert.False(i.HasMustMissing));
        Assert.Equal(mustOnly.Items.Select((_, idx) => idx + 1), mustOnly.Items.Select(i => i.Rank));

        var keyword = await client.GetFromJsonAsync<RankingView>($"/rankings/{id}?q=azure");
        Assert.Contains(keyword!.Items, i => i.CvRef == "A");
        Assert.DoesNotContain(keyword.Items, i => i.CvRef == "B");
    }

    [Fact]
    public async Task GetPdf_ServesOriginalBehindBlindPhase_UnknownIs404()
    {
        var client = _factory.CreateClient();
        var id = await RankAsync();

        var pdf = await client.GetAsync($"/rankings/{id}/cvs/A/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.True((await pdf.Content.ReadAsByteArrayAsync()).Length > 0);
        Assert.True(pdf.Headers.Contains("X-Blind-Phase"));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/rankings/{id}/cvs/ZZZ/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/rankings/nope")).StatusCode);
    }
}
