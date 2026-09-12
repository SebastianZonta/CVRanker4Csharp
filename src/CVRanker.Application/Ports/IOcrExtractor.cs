namespace CVRanker.Application;

/// <summary>OCR port for scanned pages (local Tesseract now, cloud adapter later). Image bytes in, text out.</summary>
public interface IOcrExtractor
{
    OcrResult Extract(byte[] imageBytes);
}
