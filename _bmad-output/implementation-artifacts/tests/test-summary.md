# Test Automation Summary — Story 7.23 (Rate-limit command capability)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **QA:** Jerome (Chatbot)
**Story:** `_bmad-output/implementation-artifacts/7-23-rate-limit-command-capability.md` (status: review)
**Framework detected:** xUnit v3 + Shouldly + NSubstitute (.NET 10, repo-pinned, compiled in-process runners). No new framework introduced.

## Approach

Story 7.23 ships a backend governance feature — single-actor `SubmitCommandCapabilityRateLimit` + actor-agnostic
final-gate enforcement at `ParticipantAuthorizationStage` — with **no UI surface** (AC6/AC7 satisfied by the message
catalog + authorization reason code + audit metadata, consistent with 7.12–7.22). There is no web front-end to drive,
so "E2E" here means the **API / command-pipeline** paths: gateway authorization stage, aggregate handler, audit
envelope via `CommandGateway`, the read-side enforcement seam (in isolation via injected fakes), and public
contract/client parity — no Playwright/Cypress layer applies.

The dev handoff already shipped a comprehensive mirror of the 7.20 AI-actor rate-limit cell + the 7.21/7.22
command-capability subject. This QA pass mapped all 30 AC10 + Tasks coverage points (A–DD) to existing assertions
(all present and green) and probed for genuine gaps the enumeration glosses over. **Two gaps found, both auto-applied.**

## Gaps Found and Applied

| # | Gap | Value | Status |
| --- | --- | --- | --- |
| 1 | **Trailing-window aging (AC5)** — every existing enforcement test seeded admitted-command timestamps within *minutes* of the fixed clock, so the rolling-hour `WindowDuration` boundary was never actually exercised. A wrong window duration (a day, or zero) would have passed every existing test. | HIGH | ✅ Added |
| 2 | **Capacity-impact observation shape (AC6)** — `CommandCapabilityRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled)` had zero coverage. | MEDIUM | ✅ Added |

Both gaps were also absent from the 7.20 AI-actor template they mirror.

**Added tests** (in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/CommandCapabilityRateLimitAuthorizationTests.cs`):

- `RateLimitShouldCountOnlyAdmittedCommandsInsideTheTrailingWindow` — seeds 6 timestamps (2 inside the hour, 3 aged
  out incl. the exact 1-hour boundary which is *outside*, 1 future-dated) against `budget = 3`; asserts **admitted**
  (in-window count 2 < 3, where a naive total of 6 would wrongly deny). A contrast half adds a 3rd in-window command
  (count 3 ≥ 3) → asserts **throttled** with `command_capability_rate_limited`, locking the boundary so the admit
  cannot be a "history ignored entirely" false pass. Proves the `NotificationThrottleEvaluator.CountInTrailingWindow`
  + server-measured-UTC-age wiring is real (AC5: server-measured age, future timestamps ignored).
- `CapacityImpactObservationShouldCarryFiniteIntegerBudgetCountAndThrottledFlag` — asserts the throttled/admitted
  observation shapes carry budget/observed-count/throttled and that the record exposes **only finite int/bool tokens**
  (`[int, int, bool]`) — AC6's "integer/rational arithmetic, never floats". Pins the deferred Epic-8 dashboard seam.

## AC10 Coverage Map (existing baseline + added)

| Area | Coverage |
| --- | --- |
| Single human policy-admin + tenant-admin (FR75a union) allowed; mailbox/compliance/operations + service + AI denied | `RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover` |
| Out-of-bounds/undeclared budget rejected at gateway | `RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway` |
| Out-of-bounds rejected at aggregate; invalid metadata rejected | `HandleCommandCapabilityRateLimitShouldRejectOutOfBoundsBudget`, `...ShouldRejectInvalidMetadata` |
| Self-lockout guard (cannot rate-limit FR74 governance incl. itself) | `SelfLockoutGuardShouldRejectRateLimitingAnFr74GovernanceCommand` |
| At-budget denied for human/service/AI as final gate; reason distinct from disabled/quarantined/not-allowlisted/under-scoped/ai+service rate-limited; no-credential seam | `RateLimitedCapabilityAtBudgetShouldFailClosedForEveryActorAsFinalGate` |
| Under-budget admitted | `UnderBudgetSubmissionShouldBeAdmitted` |
| Sibling type + cross-tenant isolation (NFR30) | `RateLimitShouldIsolateSiblingCommandTypesAndOtherTenants` |
| FR74 governance commands exempt from enforcement | `RateLimitShouldExemptFr74GovernanceCommands` |
| Out-of-bounds configured budget → safe default (never raises cap) | `OutOfBoundsConfiguredBudgetShouldFallBackToSafeDefaultNeverRaisingTheCap` |
| Disabled/Quarantined keeps control reason (top-of-stage switch + bottom gate coexist) | `DisabledOrQuarantinedCapabilityShouldKeepItsControlReasonOverTheRateLimitGate` |
| **Trailing-window aging (AC5)** | **`RateLimitShouldCountOnlyAdmittedCommandsInsideTheTrailingWindow` (added)** |
| **Capacity-impact observation shape (AC6)** | **`CapacityImpactObservationShouldCarryFiniteIntegerBudgetCountAndThrottledFlag` (added)** |
| Aggregate: direct configure (IsSuccess), idempotent no-op, per-type independence | `HandleCommandCapabilityRateLimit...`, `CommandCapabilityRateLimitsShouldKeepEachCommandTypeBudgetIndependent` |
| Gateway/audit: fail-closed on audit-unavailable; envelope refs + `admin-scope:policy`; no `StateTransition`; redaction | `CommandCapabilityRateLimitPreCommitAuditUnavailable...`, `CommandCapabilityRateLimitAuditEnvelopeShouldCarryBudgetWindowAndRemainMetadataOnly` |
| Contract: command/bounds/window serialization, safe tokens, `Maximum == ServiceClientRateLimitBounds.Maximum`; catalog transient entry | `CommandCapabilityRateLimitContractShouldSerialize...`, `MessageCatalogContractTests` |
| OpenAPI/client/checksum parity | `Hexalith.ChatBot.Client.Tests` |

## Test Results (compiled in-process xUnit v3 runners, `-parallel none`)

| Suite | Total | Failed |
| --- | --- | --- |
| Hexalith.ChatBot.Server.Tests | 853 (+2) | 0 |
| Hexalith.ChatBot.Contracts.Tests | 263 | 0 |
| Hexalith.ChatBot.Client.Tests (OpenAPI/checksum parity) | 17 | 0 |
| Hexalith.ChatBot.Conformance.Tests | 75 | 0 |
| Hexalith.ChatBot.Architecture.Tests | 37 | 0 |

Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → 0 Warning(s), 0 Error(s).
Submodule guard: `git submodule status` → no gitlink drift; no submodule pointer bumped.

## Coverage

- AC1–AC10 command-pipeline / API paths: fully covered; trailing-window aging (AC5) and observation shape (AC6) now
  exercised where the baseline only implied them.
- No UI E2E added — feature has no front-end surface (S5 admin surface deferred, consistent with 7.12–7.22).
- Durable read-side projection + increment-on-admit history remain deferred (sanctioned series deferral); the
  `ParticipantAuthorizationStage` rate-limit seam is unit-tested in isolation via injected fakes.

## Next Steps

- Run suites in CI (already green locally).
- When the durable read-side projection + increment-on-admit history land, extend the seam tests to exercise the live
  provider end-to-end rather than the fakes.

**Result:** All generated tests pass. ✅
