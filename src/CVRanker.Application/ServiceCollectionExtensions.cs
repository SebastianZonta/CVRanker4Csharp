using CVRanker.Application.Handlers;
using CVRanker.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CVRanker.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddSingleton<IRanker, Bm25Ranker>()
            .AddSingleton<RankOfferHandler>()
            .AddSingleton<GetRankingHandler>()
            .AddSingleton<GetCandidatePdfHandler>();
}
