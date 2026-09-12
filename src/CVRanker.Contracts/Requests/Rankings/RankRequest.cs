namespace CVRanker.Contracts.Requests.Rankings;

public sealed record RankRequest(
    string JobDescription,
    IReadOnlyList<string> MustHave,
    IReadOnlyList<string> NiceToHave,
    WeightsRequest? Weights,
    IReadOnlyList<Candidates.CvEntry> Cvs);
