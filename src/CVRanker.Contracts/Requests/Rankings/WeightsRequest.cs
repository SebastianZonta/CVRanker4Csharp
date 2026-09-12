namespace CVRanker.Contracts.Requests.Rankings;

public sealed record WeightsRequest(
    double Must,
    double Bm25,
    double Nice,
    double Experience,
    double Education,
    double Title,
    double Languages);
