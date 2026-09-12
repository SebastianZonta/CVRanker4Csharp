namespace CVRanker.Contracts.Requests.Candidates;

public sealed record CvEntry(
    string Ref,
    double ExperienceYears,
    bool HasDegree,
    bool IsSenior,
    bool HasLanguage);
