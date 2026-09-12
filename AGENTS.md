## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Using the default triage labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/` at repo root). See `docs/agents/domain.md`.

## Build & test

- Full suite: `dotnet test CVRanker.slnx` (lib unit/behavior + API integration via `WebApplicationFactory`).
- Single project: `dotnet test test/CVRanker.Tests/CVRanker.Tests.csproj`.
- Run API: `dotnet run --project src/CVRanker.Api --urls http://localhost:5000 --PdfDirectory=/tmp/pdfs`.
- Commits must use Conventional Commits with author `Sebastian Vladimir <seba_12345@yahoo.com>` via `git -c user.name=... -c user.email=... commit`. Never commit `bin/`/`obj/` (gitignored).

## Architecture (Clean, no MediatR)

`Api → Application → Domain`, `Api → Contracts`, `Application → Contracts`, `Infrastructure → Application`. Domain and Contracts reference nothing. Each project owns its `AddX()` DI extension, called only from `Program.cs`. See `docs/adr/0001-layered-architecture.md`.

- 3 handlers mirror 3 endpoints: `RankOffer` / `GetRanking` / `GetCandidatePdf` in `src/CVRanker.Application/Handlers/`.
- Mappers are extension methods in `src/CVRanker.Application/Mappers/` (`request.ToOffer()`, `snapshot.ToRankingView(...)`).
- Freeze invariant lives in the aggregate: `RankingSnapshot.Create` (Domain), not the handler.

## Gotchas

- PDFs resolve as `<PdfDirectory>/<ref>.pdf` (default dir `pdfs`, relative to CWD). Missing file → `400` on POST with the looked-for path, `404` on PDF GET.
- Test fixtures live in `test/CVRanker.Tests/Fixtures/` (copied to output; API tests copy them to a temp dir as `{ref}.pdf`).
- Tokenizer keeps trailing periods (`c#.` ≠ `c#`) — prototype fidelity, not a bug. Behavior reference: `.scratch/cv-ranker/prototype/scorer-v1.html`. Offer must-have/nice-to-have entries additionally trim trailing periods when matching, so HR-typed `C#.` matches CV term `c#` (see `.scratch/scoring-depth/spec.md`); doc-side tokens keep prototype behavior.
- `UglyToad.PdfPig` is only available as prerelease on this feed: `dotnet add package UglyToad.PdfPig --prerelease`. That version has no `PdfDocumentBuilder`, so generate fixture PDFs with PyMuPDF.
- Tests assert external behavior (order/scores/reasons/flags for fixed inputs), never BM25 internals.
