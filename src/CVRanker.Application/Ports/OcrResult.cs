namespace CVRanker.Application;

/// <summary>One OCRed image: text plus mean confidence and engine identity for audit.</summary>
public sealed record OcrResult(
    string Text,
    float Confidence,
    string Engine,
    string Version);
