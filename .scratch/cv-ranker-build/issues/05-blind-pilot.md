# 05: Piloto ciego + calibración

**What to build:** Validar el rankeador con 3 ofertas cerradas en revisión ciega y calibrar pesos v1 con los resultados.

**Blocked by:** 04 (Endpoint + vista RRHH).

**Status:** ready-for-agent

- [ ] Protocolo ciego: contratado oculto al equipo técnico; juicio RRHH sin scores (top-20 + contratado + 10 aleatorios; binario mínimo, 0–3 preferido)
- [ ] Métricas P@10 + MRR primarias (+nDCG@10 si graduado); éxito: contratado top-10 ≥2/3 (o MRR medio ≥0.2) + ahorro triaje ≥50%
- [ ] Grid grueso paso 0.05 sobre (must, BM25, nice) con objetivo nDCG@10+P@10 promediado; afinar b/k1 solo en empates; recalibrar cada ~10 ofertas
