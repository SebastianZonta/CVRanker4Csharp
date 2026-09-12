using CVRanker.Domain;
namespace CVRanker;

public interface ISnapshotStore
{
    void Save(RankingSnapshot snapshot);
    RankingSnapshot Get(string id);
}
