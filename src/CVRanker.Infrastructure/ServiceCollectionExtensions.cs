using CVRanker.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CVRanker.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var pdfDirectory = config["PdfDirectory"] ?? "pdfs";
        var tessDataPath = config["Ocr:TessDataPath"] ?? "tessdata";
        var cliPath = config["Ocr:CliPath"] ?? "tesseract";
        return services
            .AddSingleton<ISnapshotStore, InMemorySnapshotStore>()
            .AddSingleton<IOcrExtractor>(_ => new TesseractOcrExtractor(tessDataPath, cliPath))
            .AddSingleton<IPdfTextExtractor>(sp => new PdfPigTextExtractor(sp.GetRequiredService<IOcrExtractor>()))
            .AddSingleton<IPdfStore>(_ => new DirectoryPdfStore(pdfDirectory));
    }
}
