using CVRanker.Contracts.Responses.Rankings;
using CVRanker.Domain;

namespace CVRanker.Application.Mappers;

public static class SnapshotSummaryMappingExtensions
{
    public static SnapshotSummary ToSnapshotSummary(this RankingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new SnapshotSummary(
            snapshot.Id,
            snapshot.CreatedAt,
            snapshot.ScorerVersion,
            snapshot.Cvs.Count);
    }
}
