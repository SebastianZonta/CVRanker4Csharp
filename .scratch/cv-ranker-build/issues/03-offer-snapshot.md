# 03: Listas de oferta + snapshot auditable

**What to build:** Añadir a la oferta las listas must-have/nice-to-have editables con pesos v1 por defecto (ajuste avanzado opcional), y congelar un snapshot reproducible al pulsar "Rankear".

**Blocked by:** 01 (Scorer core + IRanker seam).

**Status:** ready-for-agent

- [ ] Schema: JD texto libre + must-have + nice-to-have (una por línea) + pesos + versión del scorer
- [ ] "Rankear" congela snapshot (JD + listas + pesos + versión + set de CVs); re-rank solo manual
- [ ] Snapshot reproduce el mismo ranking a posteriori (test de reproducibilidad)
