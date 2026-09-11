# 04: Endpoint + vista RRHH

**What to build:** Ver el ranking end-to-end en la app: botón Rankear, lista 1..N con score, razones, flags, filtros y resguardos anti-sesgo.

**Blocked by:** 01 (Scorer core + IRanker seam), 02 (Puerto PDF con PdfPig), 03 (Listas de oferta + snapshot auditable).

**Status:** resolved

- [x] Endpoint de ranking sobre el snapshot congelado; vista con 1..N + score 0–100 + top-3 razones + flag must + desglose colapsable
- [x] Filtros por must completo y keyword; anonimización (sin nombre/foto/edad/género/dirección) con PDF a un clic bajo banner "fase ciega"; esos campos nunca son features
- [x] Disclaimer "apoyo, no decisión automática" en cada ranking

## Answer

Nuevo `src/CVRanker.Api/` (minimal API): `POST /rankings` (botón Rankear: extrae PDFs por ref vía `IPdfStore`+`IPdfTextExtractor`, congela snapshot) · `GET /rankings/{id}?mustComplete=&q=` (vista 1..N densa tras filtros, con disclaimer + banner) · `GET /rankings/{id}/cvs/{ref}/pdf` (PDF original + header `X-Blind-Phase`). Vista `RankingView` en lib (anonimización estructural, verificada por serialización JSON). 22/22 tests en verde (19 lib + 3 integración `WebApplicationFactory`). Alcance: sin frontend — "colapsable"/"botón"/"un clic" quedan del lado cliente; el banner se expone como texto + header. Residual: el filtro keyword busca sobre el texto libre del CV.
