# CVRanker

A paywall-free CV ranker for HR triage. Given a job description and 20–200 English CV PDFs per opening, it returns a **1..N ranking** with a **0–100 score**, **top-3 reasons**, **must-have-missing flags**, and a per-section score breakdown — with adjustable weights, an auditable frozen snapshot per ranking, and blind-review safeguards (anonymized view, "support tool, not automated decision" disclaimer).

No paid LLM APIs. Scoring is a weighted hybrid lifted from `prototype/scorer-v1.html`:

`Score = 100 × Σ wᵢ·fᵢ` — must-have coverage **0.35** · BM25 (k1=1.2, b=0.75) **0.25** · nice-to-have **0.15** · experience **0.12** · education **0.05** · title **0.05** · languages **0.03**, with soft veto (zero must coverage sinks to the tail with a flag, never auto-rejects).

## Layout

| Project | What | Depends on |
|---|---|---|
| `src/CVRanker.Domain` | Ranking aggregate, scorer (`IRanker` seam), metrics + calibration. References nothing | — |
| `src/CVRanker.Contracts` | HTTP DTOs: `Requests/<entity>/`, `Responses/<entity>/`. References nothing | — |
| `src/CVRanker.Application` | Ports, 4 handlers (`RankOffer`/`GetRanking`/`GetCandidatePdf`/`ListSnapshots`), Contracts↔Domain mappers as extension methods (`request.ToOffer()`, `snapshot.ToRankingView(...)`), `AddApplication()` | Domain, Contracts |
| `src/CVRanker.Infrastructure` | Adapters (PdfPig extraction, Tesseract OCR for scanned pages, PDF/file stores), `AddInfrastructure(config)` | Application |
| `src/CVRanker.Api` | Minimal API: thin endpoints over handlers (composition root) | Application, Infrastructure, Contracts |
| `test/CVRanker.Tests` | 41 unit/behavior tests + PDF fixtures in `Fixtures/` | — |
| `test/CVRanker.Api.Tests` | 4 endpoint integration tests (in-memory server) | — |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` → `10.x`)
- `tesseract-ocr` 5.x with English data (only needed for scanned PDFs; native-text PDFs work without it).
  The app pins `tessdata/eng` shipped in `src/CVRanker.Infrastructure/tessdata` via `--tessdata-dir`
  (`Ocr:TessDataPath` overrides, `Ocr:CliPath` overrides the binary location).
  - **Linux**: `sudo apt-get install -y tesseract-ocr tesseract-ocr-eng`
  - **Windows 11**: installer from [UB-Mannheim/tesseract](https://github.com/UB-Mannheim/tesseract/wiki)
    (latest 5.x `tesseract-ocr-w64-setup-*.exe`) or `choco install tesseract`. The installer no longer
    touches PATH, so either add its folder to PATH manually or point the app at it:
    `"Ocr:CliPath": "C:\\Program Files\\Tesseract-OCR\\tesseract.exe"`. Then `tesseract --version`
    in a new terminal.

## Verify everything works

```bash
dotnet test CVRanker.slnx
```

Done when both test projects report `Passed!` with `Failed: 0`.

## Run the API

### 1. Put the PDFs in place (this is the step that bites)

The API resolves every candidate to a file on disk:

```
<PdfDirectory>/<ref>.pdf
```

- `<ref>` is the `ref` you send per candidate in the `POST /rankings` body (`A` → `A.pdf`).
- `PdfDirectory` is a configuration key, default `pdfs` (relative to the working directory you launch from).
- A missing file used to return a bare 500; it now returns `400` naming the candidate and the path it looked for.

Set it up with the bundled fixtures (any English text PDFs work):

```bash
mkdir -p /tmp/pdfs
cp test/CVRanker.Tests/Fixtures/DNS.pdf /tmp/pdfs/A.pdf
cp test/CVRanker.Tests/Fixtures/DBJ.pdf /tmp/pdfs/B.pdf
ls /tmp/pdfs   # A.pdf  B.pdf — names must match the refs in your request
```

Start the API pointing at that directory:

```bash
dotnet run --project src/CVRanker.Api --urls http://localhost:5000 --PdfDirectory=/tmp/pdfs
```

Done when the log shows `Now listening on: http://localhost:5000` with no exceptions. (`--PdfDirectory=...` can also be passed as the `PdfDirectory` environment variable.)

### 2. Rank ("Rankear" button)

```bash
curl -s -X POST localhost:5000/rankings -H 'Content-Type: application/json' -d '{
  "jobDescription": "Senior backend engineer with C# .NET REST SQL Azure",
  "mustHave": ["c#", ".net", "rest", "sql"],
  "niceToHave": ["azure", "docker", "english"],
  "weights": null,
  "cvs": [
    {"ref": "A", "experienceYears": 8, "hasDegree": true, "isSenior": true, "hasLanguage": true},
    {"ref": "B", "experienceYears": 6, "hasDegree": true, "isSenior": true, "hasLanguage": true}
  ]}'
```

- `"weights": null` applies the v1 defaults above; pass all seven fields to override per offer.
- Response: `{"snapshotId": "<id>"}`. The ranking is frozen at this point — re-rank is manual only (POST again).

### 3. Read the ranking

```bash
ID=<snapshotId>
curl -s localhost:5000/rankings/$ID | python3 -m json.tool
```

Every view carries the support-tool disclaimer and the blind-phase banner. HR filters:

```bash
curl -s "localhost:5000/rankings/$ID?mustComplete=true"   # only candidates covering all must-haves
curl -s "localhost:5000/rankings/$ID?q=azure"              # keyword filter over CV text
```

### 4. Open the original PDF (behind the blind phase)

```bash
curl -sI localhost:5000/rankings/$ID/cvs/A/pdf   # check the X-Blind-Phase header
curl -s -o A.pdf localhost:5000/rankings/$ID/cvs/A/pdf
```

The ranking view itself never contains names, photos, ages, genders, or addresses (verified structurally by test); those fields are never scoring features either.

## Blind pilot + calibration (offline, in-library)

- `BlindReviewSet.Build(ranked, hiredRef, topN: 20, randomCount: 10, seed)` — top-20 + hired + 10 randoms, shuffled, no scores attached.
- `RetrievalMetrics` — P@10, MRR, nDCG@10 (gain `2^grade − 1`, grades 0–3).
- `WeightCalibrator.Calibrate(cases)` — coarse grid (step 0.05 over must/BM25/nice, rest at v1 defaults) maximizing mean(nDCG@10 + P@10); b/k1 swept only to break ties.
- `PilotEvaluator.IsSuccess(pilots)` — hired in top-10 in ≥2/3 pilots (or mean MRR ≥ 0.2) **and** mean triage saving ≥ 50%.

Process notes (human side, not code): keep the hired candidate hidden from the technical team during review, and recalibrate roughly every 10 openings.
