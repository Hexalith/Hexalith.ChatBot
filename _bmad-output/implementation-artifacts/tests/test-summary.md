# Test Automation Summary — Story 7.16 (Quarantine service client)

**Workflow:** bmad-qa-generate-e2e-tests · **Date:** 2026-06-02 · **QA:** Jerome (Chatbot)
**Story:** `_bmad-output/implementation-artifacts/7-16-quarantine-service-client.md` (status: review)

## Framework

.NET 10 (`net10.0`), **xUnit v3** (in-process compiled runners, `-parallel none`), Shouldly, NSubstitute. No new
framework introduced — used the project's existing test conventions and the Story 7.15 disable cell as the
structural template. There is no UI/browser surface in scope (S5 admin status surface deferred, consistent with
7.12/7.13/7.15), so all coverage is API/domain/contract-level — no Playwright/Cypress E2E layer applies.

## Coverage Audit (existing dev-authored tests vs. the 9 Acceptance Criteria)

The dev handoff shipped a faithful mirror of the 7.15 disable cell. Each AC point in AC9 was mapped to a test:

| AC | Behaviour | Covered by |
|----|-----------|-----------|
| 1, 9 | Single-actor quarantine never takes effect (proposal → pending only) | `HandleServiceClientQuarantineProposalShouldCreatePendingWithoutQuarantining` |
| 1, 2, 9 | Distinct second human tenant-admin applies; same-ref & same-actor rejected (aggregate) | `HandleServiceClientQuarantineApprovalShouldRequirePendingAndDistinctSecondActor` |
| 2, 9 | Human tenant-admin only; mailbox/policy/compliance/operations scopes + service/AI denied; self-approval denied (gateway) | `ServiceClientQuarantineAuthorizationTests` (3 facts) |
| 2, 9 | Distinct-approver enforced at the dispatcher | `DispatchShouldRoute…/RejectServiceClientQuarantineApproval…` |
| 3, 9 | Subject/version/reason/unknown-pending mismatch rejected | `HandleServiceClientQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch` |
| 1, 9 | Duplicate / already-quarantined → NoOp | `HandleServiceClientQuarantineProposalShouldNoOpForAlreadyQuarantinedOrDuplicate` |
| 4, 9 | Fail-closed at `ServiceClientGrantValidator` with `service_client_quarantined` **before** grant-scope checks; distinct from `service_client_disabled` / `service_client_grant_revoked` / `…_under_scoped`; no credential/OAuth read | `QuarantinedServiceClientShouldFailClosedBeforeGrantScopeChecks` |
| 4, 9 | Isolation — sibling Active client unaffected by a quarantined peer | `ActiveSiblingServiceClientShouldBeUnaffectedByQuarantinedPeer` |
| 3, 7, 9 | Audit envelope carries actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp + `StateTransition "Active->Quarantined"`; no `@`/`secret`/`oauth`/`bearer`/`project-` leakage | `ServiceClientQuarantineAuditEnvelopeShouldCarryActiveToQuarantinedTransitionAndRemainMetadataOnly` |
| 3, 9 | Audit-unavailable → no durable quarantine, never dispatched (fail closed) | `ServiceClientQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` |
| 6 | Catalog guidance: `RequestAccess` next-action, `DisabledAction` reason, headline ≤ 80 chars | `MessageCatalogContractTests` |
| 7, 8 | Finite wire tokens (`active`/`quarantined`), round-trip, metadata-only redaction | `ServiceClientQuarantineContractsShouldSerializeFiniteStateTokensAndMetadataOnlyFields` |
| 8, 9 | OpenAPI / generated-client / checksum parity | `Hexalith.ChatBot.Client.Tests` (17) |

## Gap Found & Auto-Applied

**AC5 / NFR17 — "quarantine does not mutate existing committed/audit records" (enumerated in AC9, untested).**
The dev suite asserted the quarantined-set *grows*, but no test asserted that committing a quarantine for one
subject leaves **prior already-committed records** intact (the 7.15 disable template had the same omission).

Added one focused aggregate test:

- **`HandleServiceClientQuarantineShouldNotMutatePriorCommittedRecords`**
  (`tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`)
  — seeds a prior **committed disable** for a different service client and an **unrelated pending quarantine**
  for a third subject, then quarantines the target through the two-person flow, and asserts: the target is
  quarantined; the committed disable is untouched (not rewritten to quarantined); and the unrelated pending
  quarantine still awaits its own distinct second approver. Proves quarantine affects future admission only and
  never rewrites/removes prior records (NFR17 visible/recoverable, FR75c).

No other gaps found — the remaining AC surface was already covered. Read-side projection + query-gateway wiring
remain deferred (sanctioned by the 7.15 review); not a test gap.

## Results

| Suite | Total | Failed | Notes |
|-------|-------|--------|-------|
| `Hexalith.ChatBot.Server.Tests` | **735** | 0 | +1 (the auto-applied gap test) — was 734 |
| `Hexalith.ChatBot.Contracts.Tests` | 256 | 0 | unchanged |
| `Hexalith.ChatBot.Client.Tests` | 17 | 0 | OpenAPI/generated-client checksum parity |

Build: `dotnet build Hexalith.ChatBot.slnx` → 0 warnings, 0 errors. Submodule guard: no gitlink/pointer drift.
Only file changed by this QA pass: `GovernedOperationAggregateTests.cs` (+1 test).

## Checklist

- [x] API/domain tests generated (no UI surface → no browser E2E layer)
- [x] Tests use standard xUnit v3 + Shouldly APIs and the existing project patterns
- [x] Happy path + critical error/edge cases covered (fail-closed, two-person, redaction, immutability)
- [x] All generated tests run successfully (735 / 256 / 17 green)
- [x] Clear descriptions, no hardcoded waits/sleeps, order-independent (static deterministic state)
- [x] Summary saved with coverage metrics

## Next Steps

- Story remains in `review`; this QA pass adds the AC5/NFR17 immutability coverage. Hand to code review.
- When the durable read-side projection / service-client query-admission gateway are built (post-7.15 deferral),
  add admission-seam tests for the **query** path mirroring the command path.
