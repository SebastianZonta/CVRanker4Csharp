using CVRanker.Domain;
namespace CVRanker;

public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly Dictionary<string, RankingSnapshot> _snapshots = new(StringComparer.Ordinal);

    public void Save(RankingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshots[snapshot.Id] = snapshot;
    }

    public RankingSnapshot Get(string id) =>
        _snapshots.TryGetValue(id, out var snapshot)
            ? snapshot
            : throw new KeyNotFoundException($"Snapshot not found: {id}");
}
