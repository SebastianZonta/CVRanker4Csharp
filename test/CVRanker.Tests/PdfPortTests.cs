using CVRanker.Application;
using CVRanker.Infrastructure;
using CVRanker.Domain;
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
        var result = extractor.Extract(Fixture("DNS.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Equal(2, result.PageCount);
        Assert.Empty(result.PagesWithoutText);
        Assert.Equal(2, result.PageTexts.Count);

        // Word-level extraction: words joined with single spaces, pages in order
        Assert.Equal(
            "ELENA VIDAL Senior .NET Developer | elena.vidal@example.com | +1 555 015 6678 | Barcelona ES SUMMARY Senior backend engineer with 8 years building C# .NET services on Azure. REST APIs and SQL Server with Docker and English fluent. EXPERIENCE Senior .NET Developer - Kiosko Cloud, Barcelona ES (2021 - Present) - Lead C# .NET services on Azure for billing tenants. - Design REST APIs with versioning and rate limits. - Tune SQL Server queries with execution plan reviews. - Mentor four engineers with design reviews and pairing. .NET Developer - Kiosko Cloud (2018 - 2021) - Built background workers in C# for PDF exports. - Moved file storage from disk to Azure Blob.",
            result.PageTexts[0]);
        Assert.Equal(
            "ELENA VIDAL (continued) Junior .NET Developer - Fabrica Soft, Valencia ES (2016 - 2018) - Fixed WinForms bugs and wrote SQL reports. SKILLS C# .NET REST SQL Server Azure Docker Git English EDUCATION BSc Computer Science, UPC (2012 - 2016). CERTIFICATIONS Azure Developer Associate 2022.",
            result.PageTexts[1]);
        Assert.Equal(
            "ELENA VIDAL Senior .NET Developer | elena.vidal@example.com | +1 555 015 6678 | Barcelona ES SUMMARY Senior backend engineer with 8 years building C# .NET services on Azure. REST APIs and SQL Server with Docker and English fluent. EXPERIENCE Senior .NET Developer - Kiosko Cloud, Barcelona ES (2021 - Present) - Lead C# .NET services on Azure for billing tenants. - Design REST APIs with versioning and rate limits. - Tune SQL Server queries with execution plan reviews. - Mentor four engineers with design reviews and pairing. .NET Developer - Kiosko Cloud (2018 - 2021) - Built background workers in C# for PDF exports. - Moved file storage from disk to Azure Blob.\nELENA VIDAL (continued) Junior .NET Developer - Fabrica Soft, Valencia ES (2016 - 2018) - Fixed WinForms bugs and wrote SQL reports. SKILLS C# .NET REST SQL Server Azure Docker Git English EDUCATION BSc Computer Science, UPC (2012 - 2016). CERTIFICATIONS Azure Developer Associate 2022.",
            result.Text);
    }

    [Fact]
    public void Extract_SecondNativePdf_ReturnsExpectedText()
    {
        IPdfTextExtractor extractor = new PdfPigTextExtractor();
        var result = extractor.Extract(Fixture("DBJ.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(
            "LUIS HERRERA Junior Database Administrator | luis.herrera@example.com | +1 555 012 7765 | Miami FL SUMMARY Junior Database Administrator with 2 years keeping SQL Server healthy. Backups and T-SQL queries plus monitoring with alerts. Spanish native. EXPERIENCE Junior DBA - Harbor Logistics, Miami FL (2023 - Present) - Ran nightly backups and recovery drills for 20 databases. - Wrote T-SQL reports for inventory and billing teams. - Watched disk space and job failures during business hours. SKILLS SQL Server T-SQL Backups Monitoring Spanish EDUCATION Technical Diploma in Databases, Miami Dade College (2021 - 2023).",
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
        var extracted = extractor.Extract(Fixture("DNS.pdf"));
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
