# 02: Puerto PDF con PdfPig

**What to build:** Convertir PDFs ingleses (nativos) en texto por CV listo para el scorer, con fallback definido para escaneados.

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] Extracción a nivel palabra con PdfPig (nunca texto crudo de página); iText descartado por AGPL
- [x] Fixtures: nativos extraen limpio; página sin texto se deriva a cola OCR (Tesseract, fuera del slice salvo detección)
- [x] Tests con PDFs fixture: texto esperado por CV; pilotaje carga por path (blob Azure queda para producción)

## Answer

Puerto `IPdfTextExtractor.Extract(path)` en `src/CVRanker/` (`PdfPigTextExtractor` vía `page.GetWords()`, `PdfExtractionResult` con flag `NeedsOcr` solo-detección). Fixtures generados con PyMuPDF en `test/CVRanker.Tests/Fixtures/` (`cv-native.pdf` 2 págs, `cv-native-2.pdf`, `cv-scanned.pdf` en blanco). 10/10 tests en verde (`dotnet test`). Dep: `UglyToad.PdfPig 1.7.0-custom-5` (Apache-2.0). Nota: `Extract(string path)` se revisitará (stream/bytes) cuando llegue el slice del blob Azure.
