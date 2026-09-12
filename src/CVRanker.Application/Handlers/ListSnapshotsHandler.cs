using CVRanker.Application.Mappers;
using CVRanker.Contracts.Responses.Rankings;

namespace CVRanker.Application.Handlers;

/// <summary>List frozen snapshots in creation order (summary only, no CV text).</summary>
public sealed class ListSnapshotsHandler(ISnapshotStore store)
{
    public IReadOnlyList<SnapshotSummary> Handle() =>
        store.List().Select(s => s.ToSnapshotSummary()).ToList();
}
