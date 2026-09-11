Status: ready-for-agent

# Spec: Rankeador de CVs 1..N sin LLM (C# in-app)

## Problem Statement

RRHH hace triaje manual de 20–200 CVs en PDF (inglés) por oferta usando la job description guardada en el sistema (bloque de párrafos en texto libre). Es lento y no deja rastro auditable de por qué un candidato quedó arriba o abajo.

## Solution

Un rankeador integrado en la app (.NET 10, nuevo `CVRanker.csproj`, sin APIs de pago) que, al pulsar "Rankear", ordena los CVs de 1..N con score 0–100, top-3 razones y flag de must-have faltante, con pesos ajustables, snapshot auditable por ranking y resguardos anti-sesgo (anonimización + disclaimer).

## User Stories

1. As a RRHH recruiter, I want to add must-have / nice-to-have lists to an offer, so that ranking reflects hard requirements separately from free-text similarity.
2. As a RRHH recruiter, I want to press "Rankear" on an offer, so that I get a frozen 1..N ranking I can trust and revisit.
3. As a RRHH recruiter, I want to see a 0–100 score per candidate, so that I can gauge distance between candidates, not just order.
4. As a RRHH recruiter, I want to see top-3 reasons per candidate, so that I understand why each CV ranked where it did.
5. As a RRHH recruiter, I want a must-have-missing flag per candidate, so that I spot disqualifying gaps without auto-rejection.
6. As a RRHH recruiter, I want to filter by must-complete and keyword, so that I can narrow the ranked list fast.
7. As a RRHH recruiter, I want to expand a per-section score breakdown, so that I can audit skills vs experience vs education contributions.
8. As a RRHH recruiter, I want weights to default to v1 with optional per-offer adjustment, so that I rarely configure but can adapt edge offers.
9. As a RRHH recruiter, I want rankings anonymized (no name/photo/age/gender/address), so that first-pass triage is blind.
10. As a RRHH recruiter, I want one-click access to the original PDF behind a "fase ciega" banner, so that I can verify details when needed.
11. As a hiring manager, I want every ranking to store a snapshot (JD + lists + weights + scorer version + scores), so that past decisions are reproducible.
12. As a compliance owner, I want a "support tool, not automated decision" disclaimer on every ranking, so that usage stays human-in-the-loop.
13. As a developer, I want ranking behind one `IRanker.Rank` seam, so that BM25/Lucene internals stay swappable.
14. As a developer, I want PDF text extraction behind a port (PdfPig now, OCR fallback later), so that scanned PDFs don't block v1.
15. As a developer, I want CVs loadable by path in the pilot (blob Azure in production), so that integration unblocks before blob wiring.
16. As a pilot evaluator, I want blind review of top-20 + hired + randoms without scores, so that metrics (P@10, MRR, nDCG) are unbiased.

## Implementation Decisions

- New module `CVRanker` (own `.csproj` in the solution, .NET 10, MIT/Apache deps only): single entry seam `IRanker.Rank(jobDescription, cvs) -> ranked list (order + 0–100 score + top-3 reasons + must-missing flags + section breakdown)`.
- Text similarity: BM25 (k1=1.2, b=0.75) implemented in-house (~50 lines) or `Lucene.NET 4.8 beta` `BM25Similarity` per-offer `RAMDirectory`; TF-IDF plain rejected (no length norm).
- PDF port: `PdfPig` (Apache 2.0, word-level extraction, never raw page text); `PdfSharp` rejected for extraction; `iText` rejected (AGPL copyleft); `Tesseract` OCR only as fallback for scanned pages. Pilot loads by path; production reads Azure blob.
- Offer input: existing free-text JD block + two new editable lists (must-have / nice-to-have, one per line). Weights default v1, advanced per-offer override.
- Scoring (from prototype `prototype/scorer-v1.html`, logic lifted to C#): `Score = 100 × Σ wᵢ·fᵢ`, features normalized intra-offer to [0,1]:
  must-have coverage 0.35 · BM25 0.25 · nice-to-have 0.15 · experience (years + recency) 0.12 · education 0.05 · title/seniority 0.05 · languages 0.03. Soft veto: must=0 → tailed flag, never auto-reject.
- Flow: "Rankear" button freezes snapshot (JD + lists + weights + scorer version + CV set); re-rank manual only.
- Output contract: 1..N + score + top-3 reasons + must flag + collapsible section breakdown; filters (must-complete, keyword).
- Explainability & bias: anonymize name/photo/age/gender/address in ranking and blind review; those fields are never features; snapshot + disclaimer per ranking; residual bias risk documented.
- Deferred to v1.1+: local ONNX re-rank (`all-MiniLM-L6-v2`); `ML.NET` only with labeled history; full OCR pipeline.

## Testing Decisions

- Good tests assert external behavior (ranking order, scores, reasons, flags for fixed inputs), never BM25 internals.
- Modules under test: scorer pure functions (weights/coverage/BM25/veto), PDF port (word extraction on fixtures), snapshot store (reproducibility), endpoint (frozen re-rank).
- Pilot (no history, blind): 3 closed offers, hired hidden from tech; freeze JD+CVs+version; rank blind; HR judges blind top-20 + hired + 10 randoms (binary min, 0–3 preferred); metrics P@10 + MRR primary, nDCG@10 if graded (`trec_eval`; bpref if incomplete). Success: hired in top-10 ≥2/3 (or mean MRR ≥0.2) + triage saving ≥50%. Cost ≈90 judgments ≈5–8h HR. n=3 measures operational viability, not significance.
- Calibration without history: coarse grid step 0.05 over (must, BM25, nice) optimizing nDCG@10+P@10 averaged over 3 pilots; tune b/k1 only on ties; recalibrate every ~10 offers.

## Out of Scope

- Any paid LLM API; Python production microservice (last resort only); GPU training/fine-tuning.
- ONNX embeddings re-rank, ML.NET classifier, full OCR pipeline (v1.1+).
- Auto-rejection of candidates; the tool supports, never decides.

## Further Notes

- Source decisions: map + 7 closed tickets in `.scratch/cv-ranker/` (stack, weights, blind-review evaluation, inventory .NET 10, offer/UX, explainability, prototype verdict); behavior reference `prototype/scorer-v1.html`.
- Recalibration and synonym lists (curated per offer in v1) ride with the pilot protocol, not with this build.
