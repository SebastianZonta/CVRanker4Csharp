namespace CVRanker.Domain;

/// <summary>OCR audit trail frozen per candidate: engine, version, mean confidence by scanned page.</summary>
public sealed record OcrProvenance(
    string Engine,
    string Version,
    IReadOnlyDictionary<int, float> ConfidenceByPage);
