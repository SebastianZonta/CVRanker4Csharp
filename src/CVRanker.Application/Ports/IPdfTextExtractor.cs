using CVRanker.Domain;
namespace CVRanker.Application;

/// <summary>PDF text-extraction port (PdfPig now, OCR fallback later). Pilot loads by path; production reads Azure blob.</summary>
public interface IPdfTextExtractor
{
    PdfExtractionResult Extract(string path);
    PdfExtractionResult Extract(byte[] pdfBytes);
}
