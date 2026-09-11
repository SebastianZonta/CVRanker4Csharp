# 01: Scorer core + IRanker seam

**What to build:** Rankear un set fijo de CVs (texto ya extraído) contra una oferta y devolver orden 1..N con score 0–100, top-3 razones, flag de must-have faltante y desglose por sección, tras un único seam `IRanker.Rank`.

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] `IRanker.Rank(oferta, cvs)` devuelve 1..N + score + top-3 razones + flag must + desglose, con fixtures de 6 CVs ingleses
- [x] Fórmula v1 (del prototipo `prototype/scorer-v1.html` en `.scratch/cv-ranker/`): `Score = 100 × Σ wᵢ·fᵢ`, features en [0,1] intra-oferta; pesos must 0.35 · BM25 (k1=1.2,b=0.75) 0.25 · nice 0.15 · experiencia 0.12 · educación 0.05 · título 0.05 · idiomas 0.03; veto blando (must=0 → cola con flag, nunca auto-rechazo)
- [x] Tests de comportamiento externo (orden/scores/razones/flags ante entradas fijas), no internos de BM25

## Answer

Implementado en `src/CVRanker/` (`Bm25Ranker : IRanker`, records `Offer`/`CandidateCv`/`ScoringWeights`/`FeatureBreakdown`/`RankedCandidate`, `ScorerVersion = "scorer-v1"`). Scores verificados idénticos al prototipo: A 96.1, D 80.0, E 75.0, C 55.0, F 49.8, B 38.9. 5 tests xUnit en verde (`dotnet test`). Nota: experiencia = solo años (sin recency, como el prototipo; el spec menciona años+recency) — posible follow-up cuando `CandidateCv` tenga fechas.
