# 02: Puerto PDF con PdfPig

**What to build:** Convertir PDFs ingleses (nativos) en texto por CV listo para el scorer, con fallback definido para escaneados.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Extracción a nivel palabra con PdfPig (nunca texto crudo de página); iText descartado por AGPL
- [ ] Fixtures: nativos extraen limpio; página sin texto se deriva a cola OCR (Tesseract, fuera del slice salvo detección)
- [ ] Tests con PDFs fixture: texto esperado por CV; pilotaje carga por path (blob Azure queda para producción)
