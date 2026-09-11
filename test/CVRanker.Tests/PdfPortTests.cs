namespace CVRanker.Tests;

public sealed class PdfPortTests
{
    private static string Fixture(string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        return Path.Combine(dir, name);
    }

    [Fact]
    public void Extract_NativePdf_ReturnsExpectedTextWordLevel()
    {
        IPdfTextExtractor extractor = new PdfPigTextExtractor();
        var result = extractor.Extract(Fixture("cv-native.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Equal(2, result.PageCount);
        Assert.Empty(result.PagesWithoutText);
        Assert.Equal(2, result.PageTexts.Count);

        // Word-level extraction: words joined with single spaces, pages in order
        Assert.Equal(
            "Senior C# .NET engineer with 8 years building REST APIs and SQL Server",
            result.PageTexts[0]);
        Assert.Equal(
            "Azure Docker English fluent BSc Computer Science",
            result.PageTexts[1]);
        Assert.Equal(
            "Senior C# .NET engineer with 8 years building REST APIs and SQL Server\nAzure Docker English fluent BSc Computer Science",
            result.Text);
    }

    [Fact]
    public void Extract_SecondNativePdf_ReturnsExpectedText()
    {
        IPdfTextExtractor extractor = new PdfPigTextExtractor();
        var result = extractor.Extract(Fixture("cv-native-2.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(
            "Java backend engineer with 6 years Spring REST PostgreSQL AWS English",
            result.Text);
    }

    [Fact]
    public void Extract_ScannedPdf_FlagsOcrQueueInsteadOfEmptyText()
    {
        IPdfTextExtractor extractor = new PdfPigTextExtractor();
        var result = extractor.Extract(Fixture("cv-scanned.pdf"));

        Assert.True(result.NeedsOcr);
        Assert.Equal([1], result.PagesWithoutText);
        Assert.Equal(string.Empty, result.Text);
    }

    [Fact]
    public void Extract_MissingFile_ThrowsFileNotFound()
    {
        IPdfTextExtractor extractor = new PdfPigTextExtractor();
        Assert.Throws<FileNotFoundException>(() => extractor.Extract(Fixture("does-not-exist.pdf")));
    }

    [Fact]
    public void Extract_NativePdfText_FeedsRankerCoverage()
    {
        // Pilot wiring: path-loaded text plugs straight into the scorer
        IPdfTextExtractor extractor = new PdfPigTextExtractor();
        var extracted = extractor.Extract(Fixture("cv-native.pdf"));
        var offer = new Offer(
            "Senior backend engineer with C# .NET REST SQL Azure",
            ["c#", ".net", "rest", "sql"],
            ["azure", "docker", "english"],
            ScoringWeights.Default);
        var cvs = new List<CandidateCv>
        {
            new("PDF", extracted.Text, 8, HasDegree: true, IsSenior: true, HasLanguage: true),
            new("OTHER", "Junior clerk part time weekend shifts cash register.", 1, HasDegree: false, IsSenior: false, HasLanguage: false),
        };

        var ranked = new Bm25Ranker().Rank(offer, cvs);

        Assert.Equal("PDF", ranked[0].CvId);
        Assert.False(ranked[0].HasMustMissing);
    }
}
