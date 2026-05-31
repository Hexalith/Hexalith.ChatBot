# Test Automation Summary — Story 1.9

**Story:** 1.9 — First Governed Command End-to-End with Surface-Origin Attribution
**Workflow:** bmad-qa-generate-e2e-tests (QA automation — test generation only)
**Date:** 2026-05-31
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0); ASP.NET Core `WebApplicationFactory<Program>` for API/Tier-2; `Aspire.Hosting.Testing` for Tier-3.
**Run method:** VSTest (`dotnet test`) is blocked in this sandbox (socket `Permission denied`), so the compiled xUnit v3 in-process binaries were invoked directly — consistent with the story's documented approach.

> The prior Story 1.8 run of this workflow is preserved at `test-summary-story-1.8.md` (this file is the skill's single fixed output path).

## Gap Analysis (AC → coverage)

| AC | Behaviour | Pre-existing | Gap filled |
|----|-----------|--------------|------------|
| AC1 | Full spine order, EventStore dispatch, projection | stage-order, dispatcher, projection handler covered | UI seam had **zero** behavioural tests → **Gap D** |
| AC2 | Surface origin immutable + safe default | gateway threads origin → audit envelope covered | Boundary **capture** (body/header/default), wire **mapping**, client **mapping** untested → **Gaps A/B/C** |
| AC3 | Tenant / fail-closed / audit / idempotency real | fully covered | — |
| AC4 | Allowlist guardrail (fail-closed) | Tier-1 + Tier-2 covered | — |
| AC5 | Tier-3 Aspire E2E reads state-store end-state | self-skipping E2E present | Runtime (Docker/DAPR/Keycloak) absent in sandbox — left as documented self-skip |

## Generated Tests

### API Tests (Tier-2, HTTP boundary — `ServerBootstrapApiTests`)
Auto-applied **Gap A** — adapter-boundary surface-origin capture at `POST /api/v1/commands`, asserted by inspecting the recorded audit envelope (not just HTTP 202):
- [x] `CommandEndpointShouldCaptureBodyDeclaredSurfaceOriginImmutablyIntoEveryAuditEnvelope` — body `origin:"ui"` → both pre/post-commit envelopes carry `ui` (single distinct value = structurally immutable)
- [x] `CommandEndpointShouldFallBackToSurfaceOriginHeaderWhenBodyOriginIsAbsent` — `X-Hexalith-Surface-Origin` header fallback
- [x] `CommandEndpointBodyDeclaredSurfaceOriginShouldTakePrecedenceOverHeader` — body wins over header
- [x] `CommandEndpointShouldDefaultSurfaceOriginToApiWhenNeitherBodyNorHeaderDeclareIt` — safe `api` default, still audited
- [x] `CommandEndpointShouldCollapseUnknownSurfaceOriginToTheSafeApiDefault` — unknown → `api` (never trusted as arbitrary)

### Contract Tests (Tier-1 — `ChatBotSurfaceOriginsTests`, new file)
Auto-applied **Gap B** — the surface-origin wire mapping (FR85 / S7):
- [x] Default wire value is `api`; `Api` is the zero value (unset → safe surface)
- [x] `ToWireValue` / `FromWireValueOrDefault` for all 7 members + both-direction round-trip (theory)
- [x] Case- and whitespace-insensitive resolution (`"UI"`, `" ui "`, …)
- [x] Absent / blank / unknown → safe `Api` default (theory)
- [x] `[EnumMember]` tokens match the wire mapping

### Client Tests (Tier-1 — `ClientGenerationTests`)
Auto-applied **Gap C** — facade maps the declared origin onto the wire request:
- [x] `SubmitAsyncShouldMapEveryDeclaredSurfaceOriginOntoTheWireRequest` — every `ChatBotSurfaceOrigin` → matching generated `SurfaceOrigin`
- [x] `SubmitAsyncWithoutDeclaredOriginShouldDefaultToApiOnTheWire`

### E2E / UI Tests (Tier-1 service seam — `GovernedOperationServiceTests`, new project `Hexalith.ChatBot.UI.Tests`)
Auto-applied **Gap D** — the UI's single seam onto the spine (AC1 + UX floor), using a fake `IChatBotClient`:
- [x] Declares the `ui` surface origin at the boundary (submits `RecordGovernedNote`)
- [x] Reads the outcome back through operation status, keyed by task id
- [x] **Never-false-Done** — surfaces `accepted-projection-pending`, never a premature `completed`
- [x] Falls back to command id for the status read when no task id is returned
- [x] Audit history is metadata-only (audit-status + correlation codes; no payload/tenant/secret/path/exception leakage)

> Tier-3 browser E2E was intentionally not added: the project has no Playwright/Cypress/bUnit harness, and the named AC1 UI deliverable is exercised at its testable seam (`GovernedOperationService`) plus the existing self-skipping Aspire E2E. Adding a browser stack would over-engineer beyond the skill's "keep it simple" guidance.

## Results

Full solution: **`dotnet build Hexalith.ChatBot.slnx` → 0 warnings / 0 errors** (warnings-as-errors).

| Project | Total | Failed | Skipped |
|---------|------:|-------:|--------:|
| Contracts.Tests | 66 | 0 | 0 |
| Client.Tests | 13 | 0 | 0 |
| Server.Tests | 103 | 0 | 0 |
| UI.Tests *(new)* | 5 | 0 | 0 |
| Architecture.Tests | 20 | 0 | 0 |
| Conformance.Tests | 3 | 0 | 0 |
| IntegrationTests | 3 | 0 | 1 *(Tier-3 Aspire E2E — no Docker/DAPR)* |
| Aspire.Tests | 2 | 0 | 0 |
| AppHost.Tests | 3 | 0 | 0 |
| ServiceDefaults.Tests | 3 | 0 | 0 |
| Testing.Tests | 1 | 0 | 0 |
| **Total** | **222** | **0** | **1** |

Net new from this workflow: **+51 executed cases** (20 new test methods; baseline was 171 with 1 skip).

## Coverage

- **AC2 surface-origin attribution** — now covered end-to-end: contract mapping (Tier-1) → client facade (Tier-1) → adapter boundary capture + audit-envelope immutability (Tier-2). Previously only the gateway→envelope hop was tested.
- **AC1 UI seam** — `GovernedOperationService` now has behavioural coverage (origin declaration, status read-back, never-false-Done, metadata-only audit history).
- Leakage sentinels and metadata-only assertions preserved on every new surface; existing Story 1.3–1.8 regression tests remain green.

## Validation (checklist.md)

- [x] API tests generated (Tier-2 boundary origin capture).
- [x] E2E / UI seam tests generated (UI exists → `GovernedOperationService`); browser E2E N/A (no harness).
- [x] Tests use standard framework APIs (xUnit v3 + Shouldly + `WebApplicationFactory`).
- [x] Tests cover happy path (declared/admitted origin) and critical error cases (absent/unknown → safe default).
- [x] Tests use proper, semantic assertions (HTTP status, audit-envelope fields, typed enums); no brittle string scraping.
- [x] Clear, descriptive test names; intent comments tie each to its AC.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent (each builds its own factory / fakes; no shared mutable state or order dependency).
- [x] Test summary created with coverage metrics; tests saved to appropriate directories.
- [x] All generated tests run successfully (222 pass, 0 fail; 1 Tier-3 self-skip for absent runtime).

## Next Steps

- Run the suite in CI (xUnit v3 binaries, or `dotnet test` where the runner socket is permitted).
- Execute the Tier-3 `TrivialGovernedCommandAspireE2eTests` under a Docker + DAPR CLI + Keycloak runtime to exercise the documented `chatbot-statestore` end-state, published-event, and idempotent-replay assertions (it self-skips today).
- Optional future depth: add a bUnit component test for `GovernedOperations.razor` rendering (status legible without colour, keyboard-reachable) once a Blazor component-test harness is introduced — tracked under Stories 1.14–1.19.
