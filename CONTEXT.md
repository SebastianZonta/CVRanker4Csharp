# CV Ranking

Ranking of job candidates (1..N) for HR first-pass triage: CV PDFs in English are scored against an offer's requirements, with reasons and must-have flags, frozen per ranking for audit.

## Language

**Offer**:
The hiring requirements for one opening: free-text job description plus must-have/nice-to-have lists and scoring weights.
_Avoid_: job description (only the free-text part), opening, vacancy

**Candidate**:
A person considered for an offer, represented by extracted CV text plus rule features. Never carries identity fields into ranking.
_Avoid_: applicant, CV (the CV is the document, not the person)

**Ranking snapshot**:
The frozen, auditable record of one ranking: inputs, scorer version, CV set, and results. Never changes after creation.
_Avoid_: result set, ranking (the live ordered view, not the frozen record)

**Must-have**:
A hard requirement a candidate must cover; missing ones sink the candidate with a flag, never auto-reject.
_Avoid_: required skill, hard requirement

**Nice-to-have**:
A desirable skill that lifts a candidate without ever sinking one.
_Avoid_: bonus, plus

**Score**:
The system's 0–100 measure of candidate-to-offer fit for one ranking.
_Avoid_: grade (the human's judgment, not the system's)

**Grade**:
A human HR judgment (0–3) given during blind review.
_Avoid_: score, rating

**Blind review**:
HR judging candidates without seeing scores, order, or identity.
_Avoid_: blind pilot (the evaluation round, not the act of judging)

**Scanned CV**:
A CV PDF page without a text layer, requiring OCR extraction before scoring.
_Avoid_: scanned pdf, image CV

**OCR extraction**:
Deriving CV text from scanned pages, with engine, version, and per-page confidence frozen for audit.
_Avoid_: OCR queue, OCR fallback

**Triage saving**:
The fraction of the CV set HR does not need to read because of the ranking.
_Avoid_: time saved, efficiency
