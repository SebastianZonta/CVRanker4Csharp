# Layered architecture without MediatR

Status: accepted

API depends on Application (CQRS handlers, no MediatR) and Contracts (HTTP DTOs per entity); Application depends on Domain and Contracts; Infrastructure depends on Application and holds the adapters (PdfPig extraction, PDF/file stores, persistence when it arrives); Domain holds the ranking aggregate with the freeze invariant and references nothing. Each project exposes its own `AddX()` DI extension, called only from the composition root (`Program.cs`).

## Considered Options

- **MediatR + events**: rejected — rank-once + read-filtered flows don't pay for the pipeline; plain handlers suffice until a third flow with real events appears.
- **Single-project vertical slices**: rejected — the team explicitly wants Clean Architecture layering with per-layer DI ownership.

## Consequences

- Application carries bidirectional Contracts↔Domain mappers; accepted duplication cost for keeping HTTP shapes out of Domain.
- `RankingSnapshot` freeze invariant moves from `RankingService` defensive copies into the aggregate itself (`RankingSnapshot.Create`).
- Three handlers mirror three endpoints: `RankOfferHandler`, `GetRankingHandler`, `GetCandidatePdfHandler`.
- Contracts layout is `Requests/<entity>/` + `Responses/<entity>/`; no DTOs without an endpoint.
- Migration is stepwise (Domain → Contracts → Application → Infrastructure → API rewiring), suite green between steps, one commit per step.
