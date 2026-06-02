# Test Automation Summary — Story 7.20 (Rate-limit AI actor)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-02
**Author:** Jerome (QA automation run)
**Framework:** xUnit + Shouldly (existing project framework; no new framework introduced)

## Scope

Story 7.20 was implemented and already shipped with a broad test set across authorization,
aggregate, enforcement-seam, gateway/audit, and contract layers. This QA run **mapped the
existing tests against AC10's enumerated coverage requirements** and **auto-applied the
discovered gaps**. The feature is a server/API admission-control mutation (no UI surface — AC6/AC7
are satisfied by the message catalog + authorization reason code + audit metrics), so all work is
API/seam-level automated tests. No E2E/UI tests apply.

## Coverage Map (AC10) — pre-existing tests

| AC10 requirement | Covered by (pre-existing) |
| --- | --- |
| Single human policy-admin applies (no approver); tenant-admin also (FR75a union); non-policy human denied; service/AI denied with admin claims | `AiActorRateLimitAuthorizationTests.RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover` |
| Out-of-bounds/undeclared budget rejected at gateway | `…RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway` |
| Out-of-bounds rejected at aggregate; invalid metadata rejected; idempotent re-submit NoOp; committed records untouched | `GovernedOperationAggregateTests.HandleAiActorRateLimit*` + `AiActorRateLimitsShouldKeepEachAiActorBudgetIndependent` |
| AI actor at budget denied `ai_actor_rate_limited` as final gate, distinct from every security reason | `ServiceClientGrantAuthorizationTests.RateLimitedAiActorProposalShouldDenyAsFinalGateDistinctFromEverySecurityReason` |
| Under-budget admitted; sibling AI actor unaffected (isolation) | `…UnderBudgetAiActorProposalShouldBeAdmittedNormally`, `…SiblingAiActorBudgetShouldNotThrottleAnotherAiActor` |
| Subject-class separation (service not matched by AI set; service still gets service-client reason) | `…ServiceActorShouldNotBeMatchedByAiActorRateLimitSet`, `…ServiceActorAtServiceBudgetShouldStillGetServiceClientRateLimitedNotAiActorReason` |
| Rate-limit never masks a security denial | `…AiActorSecurityDenialShouldKeepItsPreciseReasonAndNeverBeMaskedByRateLimit` |
| Out-of-bounds budget falls back to safe default at seam (never raises cap) | `…OutOfBoundsAiActorBudgetShouldFallBackToSafeDefaultAndNeverRaiseTheCap` |
| Audit envelope: actor/scope/subject/reason/old/new/window/policy-snapshot/timestamp + `admin-scope:policy`, no `StateTransition`, no leakage | `CommandGatewayTests.AiActorRateLimitAuditEnvelopeShouldCarryBudgetWindowAndRemainMetadataOnly` |
| Audit-unavailable → fail closed (no durable rate-limit, no enforcement) | `…AiActorRateLimitPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` |
| Transient catalog guidance (`retry-later` + `dependency-degraded`, ≤80 char headline) | `MessageCatalogContractTests` |
| Serialization / bounds / safe-token sweep / OpenAPI-client-checksum parity | `AdminContractTests`, client parity tests |

## Discovered Gaps — auto-applied this run

The AC5 **trailing rolling-window math** (`NotificationThrottleEvaluator.CountInTrailingWindow`,
server-measured UTC age) was proven only on the **service-client** branch. Every pre-existing
AI-actor enforcement test seeded *in-window* timestamps only, so a regression making proposals
count cumulatively (all-time) would have passed the entire AI-actor suite undetected. Added three
AI-actor parity tests mirroring the service-client `Stale…`/`TightBudgetBoundary…`/`ZeroBudget…`
tests:

### Enforcement-seam tests (new)
`tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`

- [x] `StaleAdmittedAiProposalsOutsideTrailingWindowShouldNotCountAgainstBudget` — budget 3, 5 admitted proposals but only 2 inside the trailing hour ⇒ admitted (guards against a cumulative-count regression on the AI-actor branch).
- [x] `TightAiActorBudgetBoundaryShouldThrottleUsingOnlyInWindowProposals` — budget 2, two in-window + one stale ⇒ throttled `ai_actor_rate_limited` (proves stale proposals are excluded from the *throttle* count, not just the admit path).
- [x] `ZeroAiActorBudgetShouldThrottleEveryProposalEvenWithNoRecentHistory` — `AiActorRateLimitBounds.Minimum` (0) is in-bounds (not coerced to SafeDefaults) so count 0 ≥ budget 0 ⇒ throttled even with empty history (pins the lower boundary of the closed range at the AI-actor seam).

## Coverage

- AC1–AC10: all covered (pre-existing + the three new window-correctness tests).
- AI-actor rate-limit enforcement branch: trailing-window admit, stale-exclusion (admit & throttle), boundary throttle, zero-budget throttle, isolation, subject-class separation, security-precedence — all covered.

## Validation

- Build: `Hexalith.ChatBot.Server.Tests` → **0 Warning(s), 0 Error(s)**.
- Targeted run (`ServiceClientGrantAuthorizationTests` + `AiActorRateLimitAuthorizationTests`): **46 passed, 0 failed**.
- Full `Hexalith.ChatBot.Server.Tests`: **802 passed, 0 failed** (was 799; +3 new).
- Submodule pointers: **no drift**.

## Next Steps

- Run in CI alongside the existing Contracts/Client/Conformance/Architecture suites.
- When the deferred durable read-side (the `AiActorRateLimitConfigured` projection + admitted-proposal history) is implemented in a later story, add integration tests that exercise the real provider/history (this run unit-tests the seam in isolation via injected fakes, per the sanctioned 7.12–7.19 read-side deferral).
