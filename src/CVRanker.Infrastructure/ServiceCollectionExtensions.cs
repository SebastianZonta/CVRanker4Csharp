using CVRanker.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CVRanker.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var pdfDirectory = config["PdfDirectory"] ?? "pdfs";
        return services
            .AddSingleton<ISnapshotStore, InMemorySnapshotStore>()
            .AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>()
            .AddSingleton<IPdfStore>(_ => new DirectoryPdfStore(pdfDirectory));
    }
}
