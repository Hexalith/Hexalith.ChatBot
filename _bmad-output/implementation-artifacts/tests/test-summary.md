# Test Automation Summary — Story 7.19 (Quarantine AI actor)

**Date:** 2026-06-02
**Workflow:** bmad-qa-generate-e2e-tests · **QA:** Jerome (Chatbot)
**Story:** `_bmad-output/implementation-artifacts/7-19-quarantine-ai-actor.md` (status: review)
**Framework:** xUnit v3 + Shouldly + NSubstitute (.NET 10, repo-pinned). No new framework introduced.

## Approach

Story 7.19 ships a backend governance feature with **no UI surface** (AC6 is satisfied by the message
catalog + authorization reason code + audit metadata, consistent with 7.12–7.18). There is no web
front-end to drive, so "E2E" here means the **API / command-pipeline** end-to-end paths: the gateway
authorization stage, the accepted-command dispatcher, the `ServiceClientGrantValidator` admission seam,
the aggregate two-person flow, the audit envelope via `CommandGateway`, and public contract/client
parity — no Playwright/Cypress layer applies.

The dev handoff (Amelia) already shipped a faithful mirror of the 7.18 disable cell with the
disable→quarantine substitution. This QA pass mapped every AC9 scenario to an existing test and found
**one genuine gap**, auto-applied.

## Gap Found and Applied

| Gap (AC9 clause) | Status |
| --- | --- |
| "quarantine does not mutate existing committed/audit records" (AC5 / NFR17 / FR75c) — the 7.16 service-client cell has `HandleServiceClientQuarantineShouldNotMutatePriorCommittedRecords`, but the 7.19 AI-actor cell had **no** equivalent immutability test | ✅ Added `HandleAiActorQuarantineShouldNotMutatePriorCommittedRecords` |

**New test:** `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
→ `HandleAiActorQuarantineShouldNotMutatePriorCommittedRecords`. Mirrors the 7.16 anchor with the
AI-actor subject class: a prior committed AI-actor **disable** for `ai-actor:legacy-actor` and an
unrelated pending AI-actor quarantine for `ai-actor:third-actor` both remain intact and reconstructable
after the target AI actor is quarantined through the two-person flow (the committed disable is not
rewritten to quarantined; the unrelated pending change still awaits its own distinct second approver).

## AC9 Coverage Map (existing + added)

| AC9 scenario | Test(s) |
| --- | --- |
| Single-actor quarantine never takes effect (proposal alone does not block) | `HandleAiActorQuarantineProposalShouldCreatePendingWithoutQuarantining` |
| Distinct second human policy-admin applies | `HandleAiActorQuarantineApprovalShouldRequirePendingAndDistinctSecondActor` |
| `RequesterRef == ApproverRef` AND `RequesterActorId == UserId` rejected at gateway / dispatcher / aggregate | `QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover` (gateway), `DispatchShouldRejectAiActorQuarantineApprovalWhenApproverEqualsRequester` (dispatcher), `HandleAiActorQuarantineApprovalShouldRequirePendingAndDistinctSecondActor` (aggregate — both ref and actor-id paths) |
| Service clients + AI actors denied (propose + approve) even with admin-looking claims | `QuarantineProposalShouldRequireHumanPolicyAdmin`, `QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover` |
| Non-policy human scope denied; policy-admin **and** tenant-admin (union) allowed | `QuarantineProposalShouldRequireHumanPolicyAdmin`, `QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover` |
| Quarantined AI actor fails closed at `ServiceClientGrantValidator` with `ai_actor_quarantined` before grant-scope checks; distinct from `ai_actor_disabled` / `service_client_quarantined` / `service_client_grant_revoked` | `QuarantinedAiActorShouldFailClosedBeforeGrantScopeChecksWithDistinctReason`, `QuarantinedAiActorAiProposalShouldFailClosedAtAuthorizationStageBeforeApprovalGate` |
| Sibling Active AI actor unaffected (isolation) | `ActiveSiblingAiActorShouldBeUnaffectedByQuarantinedPeer` |
| Service actor not matched by AI-actor quarantine set (subject-class separation) | `ServiceActorShouldNotBeMatchedByAiActorQuarantinedSet` |
| **Quarantine does not mutate committed/audit records (NFR17)** | **`HandleAiActorQuarantineShouldNotMutatePriorCommittedRecords` (added)** |
| Audit envelope: actor/scope/subject/reason/old/new/policy-snapshot/timestamp + `StateTransition "Active->Quarantined"` + `admin-scope:policy` + no leakage | `AiActorQuarantineAuditEnvelopeShouldCarryActiveToQuarantinedTransitionAndRemainMetadataOnly` |
| Audit-unavailable → no durable quarantine + no admission-blocking side effect (fail closed) | `AiActorQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` |
| Subject/version/reason mismatch + unknown pending rejected | `HandleAiActorQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch` |
| Duplicate / already-quarantined → NoOp | `HandleAiActorQuarantineProposalShouldNoOpForAlreadyQuarantinedOrDuplicate` |
| Wire/serialization parity for the two commands + `Quarantined` enum; metadata-only redaction | `AiActorQuarantineContractsShouldSerializeFiniteStateTokensAndMetadataOnlyFields` + command-type roster |
| Message catalog finite-reason guidance (request-access + reused disabled-action, ≤80-char headline) | `MessageCatalogContractTests` (extended) |
| Dispatcher routing of the quarantine approval to the change aggregate (PascalCase payload) | `DispatchShouldRouteAiActorQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover` |
| OpenAPI/client/checksum parity | `Hexalith.ChatBot.Client.Tests` (generated-client checksum parity) |

## Test Results (compiled in-process xUnit v3 runners, `-parallel none`)

| Suite | Total | Failed |
| --- | --- | --- |
| Hexalith.ChatBot.Server.Tests | 783 (+1) | 0 |
| Hexalith.ChatBot.Contracts.Tests | 259 | 0 |
| Hexalith.ChatBot.Client.Tests (OpenAPI/checksum parity) | 17 | 0 |
| Hexalith.ChatBot.Conformance.Tests | 75 | 0 |
| Hexalith.ChatBot.Architecture.Tests | 37 | 0 |

Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → 0 Warning(s), 0 Error(s).
Submodule guard: `git submodule status` → no gitlink drift; no submodule pointer bumped.

## Coverage

- AC1–AC9 command-pipeline / API paths: fully covered after the added immutability test.
- No UI E2E added — feature has no front-end surface (S5 admin surface deferred, consistent with 7.12–7.18).
- Read-side projection + query-admission gateway wiring remain deferred (sanctioned by 7.16/7.18 reviews); the
  `ServiceClientGrantValidator` quarantine seam is unit-tested in isolation via the injected provider fake.

## Next Steps

- Run suites in CI (already green locally).
- When the durable read-side projection / query-admission gateway are built (7.20+), extend the seam tests to
  exercise the live provider rather than the fake.
