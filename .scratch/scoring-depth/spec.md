# Spec: deepen the Scoring module

Status: ready-for-agent

## Problem Statement

When an Offer carries must-have or nice-to-have entries with punctuation or casing variants (e.g. "C#", "C#.", "Node.js"), Candidates that clearly cover them can still be flagged as missing them. Understanding why requires reading four small modules at once, and a fix in any one of them can silently break the others. HR sees wrong missing-must flags and wrong reasons on the frozen Ranking snapshot.

## Solution

Collapse the Scoring cluster into one deep module behind the existing ranker seam: a single normalization path shared by Offer lists and Candidate text, with the weight math, reasons, and soft veto kept inside. Callers and tests use one interface; the helpers become internal detail.

## User Stories

1. As an HR reviewer, I want must-have entries like "C#." to match Candidate text containing "c#", so that missing-must flags reflect reality.
2. As an HR reviewer, I want nice-to-have entries with mixed case to lift Candidates that mention them, so that fit order is fair.
3. As an HR reviewer, I want the top reasons on each ranked Candidate to name the must-have/nice-to-have entries actually found, so that I can trust the triage order.
4. As an HR reviewer, I want Candidates missing every must-have to sink to the tail without being auto-rejected, so that the soft veto keeps working.
5. As an HR reviewer, I want an empty Offer description or empty Offer lists to still produce a stable ranking instead of an error, so that drafts can be trialled.
6. As an HR reviewer, I want Candidates with empty CV text to rank at the bottom deterministically, so that data gaps are visible.
7. As a hiring manager, I want the Score to stay on the 0–100 scale for every Candidate, so that snapshots remain comparable.
8. As a hiring manager, I want the scorer version recorded on each Ranking snapshot to stay accurate, so that audits can tell which scorer produced a ranking.
9. As a pilot evaluator, I want weight calibration results to be unchanged for inputs unaffected by the normalization fix, so that the blind-review pilot stays valid.
10. As a pilot evaluator, I want Grades outside 0–3 to keep their documented non-relevant meaning, so that retrieval metrics are not polluted.
11. As a maintainer, I want one interface to learn for Scoring behaviour, so that future changes (tokenizer tweaks, weight changes) land in one place.
12. As a maintainer, I want no dead public helpers in the Scoring module, so that readers are not misled about what is used.
13. As a maintainer, I want feature values documented and enforced in range, so that negative experience years cannot leak into Scores.

## Implementation Decisions

- The modules to modify are the Scoring module (ranker, tokenizer, weights, feature breakdown) and its use of the Offer module's effective weights. No other module changes shape.
- The interface that matters is the existing ranker seam: rank one Offer against a set of Candidates, returning ordered ranked Candidates with Score, reasons, and missing must-have entries. Helpers (tokenize, list normalization, weighted sum) become internal seams, not public surface.
- One normalization path serves both Offer lists (must-have, nice-to-have) and Candidate text, so a term matches identically on both sides. Prototype fidelity is preserved: the trailing-period behaviour of the prototype scorer stays as-is unless the normalization decision explicitly changes it.
- Dead surface is removed: the unused token-set helper goes away.
- Feature values are validated at construction (documented ranges hold even for edge inputs such as negative experience years).
- The scorer version string and the Ranking snapshot freeze invariant do not change.
- Rank-once flow, read-model filtering, HTTP contracts, and store/extraction ports do not change.
- Calibration keeps producing the same winners on unaffected inputs; any acceptance of the ranker interface by calibration is a signature relaxation only, not a behaviour change.

## Testing Decisions

- A good test pins external behaviour for fixed inputs: order, Scores, reasons, and missing-must flags. Tests never assert BM25 internals, tokenizer regexes, or intermediate sums.
- The modules tested are the Scoring module through the ranker seam, plus the pilot calibration objective as a regression guard.
- Prior art: the existing ranker behaviour tests (fixed-input order/scores/reasons/flags), snapshot freeze tests, and pilot metric tests. New tests follow the same pattern: fixed Offer plus fixed Candidate texts, asserting only the public ranking outcome.
- New coverage: punctuation/case variants in must-have and nice-to-have entries ("c#", "C#.", "Node.js"), empty description, empty lists, empty CV text, zero-sum weights, and negative experience years.

## Out of Scope

- A second ranker adapter (semantic/LLM) and any ranker factory; the ranker seam stays single-adapter per the hypothetical-seam rule.
- Candidate ingestion changes: PDF fetching, extraction overloads, OCR-queue routing, and error-policy unification.
- Read-model changes: filtering, keyword search, and re-ranking stay where they are.
- Handler/endpoint restructuring and any event pipeline; ADR-0001 (layered architecture, no MediatR) stands.
- Grade-range enforcement in pilot metrics beyond documenting current behaviour.

## Further Notes

- Source: candidate 1 ("Collapse the Scoring cluster") of the architecture review report in the OS temp directory (`architecture-review-20260911-220328.html`), chosen as Top recommendation.
- Behaviour reference for tokenizer fidelity is the v1 prototype scorer kept under `.scratch/cv-ranker/prototype/`.
- The ingestion module (OCR flag, corrupt-PDF policy) is the recommended follow-up and is deliberately excluded here to keep this spec to one seam.

## Decision log (implementation)

- Normalization: Offer entries go through the same tokenizer as Candidate text, plus trailing-period trim (`C#.` → `c#`); doc-side tokens keep raw tokenizer output for prototype BM25 fidelity. Residual asymmetry (a CV sentence ending in `C#.` still misses) is accepted to keep exact prototype scores/order green; full symmetry is a follow-up with prototype re-validation.
- Multi-word entries match when all their terms are present (`sql server` needs both); display form in reasons/missing lists is the normalized entry.
- Void entries (no tokens after normalization) are dropped from must-have/nice-to-have, like blanks.
