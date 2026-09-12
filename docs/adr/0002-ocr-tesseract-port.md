# OCR for scanned CVs via IOcrExtractor port (local Tesseract)

Scanned CVs (no text layer) scored zero and tailed with `NeedsOcr`. We decided: new Application port `IOcrExtractor`, `PdfPigTextExtractor` delegates textless pages by OCRing their embedded images locally with Tesseract (`eng` pinned); engine, version, and per-page confidence extend `PdfExtractionResult` and freeze per candidate in the ranking snapshot. OCR failure or imageless pages keep `NeedsOcr` and tail, never break ranking.

## Considered Options

- **Cloud OCR (Document Intelligence / Textract)**: rejected for now — PII leaves the box and per-page cost; the port allows a second adapter later behind config.
- **Full PDF-page renderer (Ghostscript / Pdfium)**: rejected — native weight; extracting embedded images covers scanner output without a renderer.

## Consequences

- Local `tesseract` 5.x CLI plus `tessdata/eng` ships with the app (`--tessdata-dir` pinned; system tessdata as fallback). Requires `tesseract-ocr` on dev/CI/prod machines or images (Azure pipeline: `sudo apt-get install -y tesseract-ocr`).
- The `Tesseract` 5.2.0 NuGet was rejected: it ships Windows-only natives and fails on Linux (`libleptonica-1.82.0.so` missing).
- Blank pages with no images still tail with `NeedsOcr` (see `cv-scanned.pdf`); image-only scans are covered (see `cv-scanned-text.pdf`).
