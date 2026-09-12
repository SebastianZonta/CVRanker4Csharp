namespace CVRanker.Domain;

/// <summary>CV with already-extracted text plus small rule features (never name/photo/age/gender/address).</summary>
public sealed record CandidateCv(
    string Id,
    string Text,
    double ExperienceYears,
    bool HasDegree,
    bool IsSenior,
    bool HasLanguage);
