# 05: Piloto ciego + calibración

**What to build:** Validar el rankeador con 3 ofertas cerradas en revisión ciega y calibrar pesos v1 con los resultados.

**Blocked by:** 04 (Endpoint + vista RRHH).

**Status:** resolved

- [x] Protocolo ciego: contratado oculto al equipo técnico; juicio RRHH sin scores (top-20 + contratado + 10 aleatorios; binario mínimo, 0–3 preferido)
- [x] Métricas P@10 + MRR primarias (+nDCG@10 si graduado); éxito: contratado top-10 ≥2/3 (o MRR medio ≥0.2) + ahorro triaje ≥50%
- [x] Grid grueso paso 0.05 sobre (must, BM25, nice) con objetivo nDCG@10+P@10 promediado; afinar b/k1 solo en empates; recalibrar cada ~10 ofertas

## Answer

En `src/CVRanker/`: `RetrievalMetrics` (P@10, MRR, nDCG@10 con ganancia 2^g−1, IDCG sobre todos los juicios) · `BlindReviewSet.Build` (top-20 + contratado + 10 aleatorios, shuffle con seed, sin scores) · `WeightCalibrator.Calibrate` (grid 0.05 sobre must/BM25/nice hasta 0.6, objetivo media de nDCG@10+P@10, desempate b/k1 en rejilla pequeña) · `PilotEvaluator` (regla de éxito del ticket + ahorro = 1 − revisados/totales). 29/29 tests en verde. Proceso (no código): contratado oculto al equipo técnico + recalibración cada ~10 ofertas al correr los pilotos reales.
