using CVRanker.Application.Mappers;
using CVRanker.Contracts.Responses.Rankings;

namespace CVRanker.Application.Handlers;

/// <summary>Filtered read over a frozen snapshot.</summary>
public sealed class GetRankingHandler(ISnapshotStore store)
{
    public RankingView Handle(string snapshotId, bool mustCompleteOnly = false, string? keyword = null) =>
        store.Get(snapshotId).ToRankingView(mustCompleteOnly, keyword);
}
