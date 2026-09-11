# 03: Listas de oferta + snapshot auditable

**What to build:** Añadir a la oferta las listas must-have/nice-to-have editables con pesos v1 por defecto (ajuste avanzado opcional), y congelar un snapshot reproducible al pulsar "Rankear".

**Blocked by:** 01 (Scorer core + IRanker seam).

**Status:** resolved

- [x] Schema: JD texto libre + must-have + nice-to-have (una por línea) + pesos + versión del scorer
- [x] "Rankear" congela snapshot (JD + listas + pesos + versión + set de CVs); re-rank solo manual
- [x] Snapshot reproduce el mismo ranking a posteriori (test de reproducibilidad)

## Answer

`RankingService.Rank` congela (`Offer` con `EffectiveWeights` → defaults v1 si se omite, `OfferLists.ParseLines` una-por-línea, copias defensivas) + `RankingSnapshot` (id, fecha UTC, oferta, CVs, resultados, `scorer-v1`) en `ISnapshotStore` (`InMemorySnapshotStore`). Sin re-rank automático. 15/15 tests en verde. Notas: `InMemorySnapshotStore` no thread-safe (revisitar si se comparte entre requests); `scorerVersion` viaja con el snapshot pero lo inyecta el llamador, no el `IRanker`.
