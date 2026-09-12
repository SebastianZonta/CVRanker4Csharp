namespace CVRanker.Contracts.Responses.Rankings;

public sealed record ScoreBreakdown(
    double Must,
    double Bm25,
    double Nice,
    double Experience,
    double Education,
    double Title,
    double Languages);
