# 04: Endpoint + vista RRHH

**What to build:** Ver el ranking end-to-end en la app: botón Rankear, lista 1..N con score, razones, flags, filtros y resguardos anti-sesgo.

**Blocked by:** 01 (Scorer core + IRanker seam), 02 (Puerto PDF con PdfPig), 03 (Listas de oferta + snapshot auditable).

**Status:** ready-for-agent

- [ ] Endpoint de ranking sobre el snapshot congelado; vista con 1..N + score 0–100 + top-3 razones + flag must + desglose colapsable
- [ ] Filtros por must completo y keyword; anonimización (sin nombre/foto/edad/género/dirección) con PDF a un clic bajo banner "fase ciega"; esos campos nunca son features
- [ ] Disclaimer "apoyo, no decisión automática" en cada ranking
