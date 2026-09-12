using CVRanker.Domain;

namespace CVRanker.Application.Mappers;

public static class OcrMappingExtensions
{
    public static OcrProvenance? ToOcrProvenance(this PdfExtractionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.OcrEngine is null || result.OcrVersion is null)
            return null;
        return new OcrProvenance(
            result.OcrEngine,
            result.OcrVersion,
            result.OcrConfidenceByPage is null
                ? new Dictionary<int, float>()
                : new Dictionary<int, float>(result.OcrConfidenceByPage));
    }
}
