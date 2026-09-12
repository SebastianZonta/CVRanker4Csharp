using CVRanker.Domain;
namespace CVRanker.Application;

public interface ISnapshotStore
{
    void Save(RankingSnapshot snapshot);
    RankingSnapshot Get(string id);
    IReadOnlyList<RankingSnapshot> List();
}
