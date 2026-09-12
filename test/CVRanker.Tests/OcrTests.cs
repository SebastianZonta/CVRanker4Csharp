using CVRanker.Application;
using CVRanker.Infrastructure;

namespace CVRanker.Tests;

public sealed class OcrTests
{
    private sealed class FakeOcr(string text, float confidence = 0.9f) : IOcrExtractor
    {
        public int Calls { get; private set; }

        public OcrResult Extract(byte[] imageBytes)
        {
            Calls++;
            return new OcrResult(text, confidence, "fake-ocr", "0");
        }
    }

    private static string Fixture(string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        return Path.Combine(dir, name);
    }

    [Fact]
    public void Extract_ImageOnlyPdf_DelegatesToOcrAndRecordsEngine()
    {
        var extractor = new PdfPigTextExtractor(new FakeOcr("Senior backend engineer REST SQL"));

        var result = extractor.Extract(Fixture("cv-scanned-text.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Contains("REST", result.Text);
        Assert.Equal("fake-ocr", result.OcrEngine);
        Assert.Equal("0", result.OcrVersion);
        Assert.NotNull(result.OcrConfidenceByPage);
        var page = Assert.Single(result.OcrConfidenceByPage);
        Assert.Equal(0.9f, page.Value);
    }

    [Fact]
    public void Extract_BlankPdfWithoutImages_KeepsNeedsOcrWithoutCallingOcr()
    {
        var fake = new FakeOcr("should never be used");
        var extractor = new PdfPigTextExtractor(fake);

        var result = extractor.Extract(Fixture("cv-scanned.pdf"));

        Assert.True(result.NeedsOcr);
        Assert.Equal(string.Empty, result.Text);
        Assert.Equal(0, fake.Calls);
        Assert.Null(result.OcrEngine);
    }

    [Fact]
    public void Extract_NativePdf_NeverCallsOcr()
    {
        var fake = new FakeOcr("should never be used");
        var extractor = new PdfPigTextExtractor(fake);

        var result = extractor.Extract(Fixture("DNS.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Equal(0, fake.Calls);
        Assert.Null(result.OcrEngine);
    }

    [Fact]
    public void Extract_ImageOnlyPdf_WithTesseract_ReadsRenderedText()
    {
        var extractor = new PdfPigTextExtractor(new TesseractOcrExtractor(ResolveTessData()));

        var result = extractor.Extract(Fixture("cv-scanned-text.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Contains("REST", result.Text);
        Assert.Contains("Azure", result.Text);
        Assert.Equal(TesseractOcrExtractor.EngineName, result.OcrEngine);
        Assert.NotNull(result.OcrConfidenceByPage);
    }

    private sealed class ThrowingOcr : IOcrExtractor
    {
        public OcrResult Extract(byte[] imageBytes) =>
            throw new InvalidOperationException("ocr down");
    }

    [Fact]
    public void Extract_MixedNativeAndScannedPages_KeepsPageOrder()
    {
        var extractor = new PdfPigTextExtractor(new FakeOcr("scanned delta epsilon", 0.8f));

        var result = extractor.Extract(Fixture("cv-mixed.pdf"));

        Assert.False(result.NeedsOcr);
        Assert.Equal(2, result.PageCount);
        Assert.Contains("alpha", result.PageTexts[0]);
        Assert.Equal("scanned delta epsilon", result.PageTexts[1]);
        Assert.StartsWith(result.PageTexts[0], result.Text);
        Assert.EndsWith(result.PageTexts[1], result.Text);
        Assert.Equal([2], result.OcrConfidenceByPage!.Keys);
    }

    [Fact]
    public void Extract_OcrFailure_KeepsNeedsOcrWithoutThrowing()
    {
        var extractor = new PdfPigTextExtractor(new ThrowingOcr());

        var result = extractor.Extract(Fixture("cv-scanned-text.pdf"));

        Assert.True(result.NeedsOcr);
        Assert.Equal(string.Empty, result.Text);
        Assert.Null(result.OcrEngine);
    }

    private static string ResolveTessData()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (File.Exists(Path.Combine(dir, "eng.traineddata")))
            return dir;
        throw new FileNotFoundException("tessdata/eng.traineddata not found for OCR test.");
    }
}
