# Test Automation Summary — Story 7.12 (Disable mailbox source)

**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-02
**Engineer role:** QA automation (test generation only — no code review / story validation)
**Framework detected:** .NET 10 / xUnit v3 + Shouldly + NSubstitute (compiled in-process runners; no JS/Playwright stack — server-side governance feature, no UI surface this story per AC6/deferred S5)

## Scope

Story 7.12 is the first FR74 enforcement cell (mailbox source × disable) under the
FR75d two-person rule. It is a server-side governance + intake-enforcement feature
with public command contracts but no UI surface (AC6 satisfied by the finite message
catalog + worker reason code; S5 admin surface deferred). "API/E2E" coverage is the
submit→approve aggregate seam, the gateway authorization + fail-closed audit seam, and
the mailbox-intake-worker block seam, plus contract/client parity. The implementation
already shipped with comprehensive author-written tests; this run mapped existing
coverage against the **AC9 enumerated proofs** and **auto-applied the discovered gaps**.

## AC9 Proof → Test Coverage Matrix

| AC9 required proof | Covered by | Status |
| --- | --- | --- |
| Single-actor disable never takes effect (proposal alone) | `GovernedOperationAggregateTests.HandleMailboxSourceDisableProposalShouldCreatePendingWithoutDisabling` | Existing |
| Distinct second human approver applies the disable | `GovernedOperationAggregateTests.HandleMailboxSourceDisableApprovalShouldRequirePendingAndDistinctSecondActor` | Existing |
| `RequesterRef == ApproverRef` rejected (gateway **and** aggregate) | `MailboxSourceDisableAuthorizationTests.DisableApprovalShouldRequireHumanMailboxScopeAndDistinctApprover` (gateway) + aggregate `selfApprovalByRef` | Existing |
| `RequesterActorId == approver UserId` rejected (aggregate; gateway has no pending state) | aggregate `selfApprovalByActor` | Existing |
| Service clients + AI actors denied for propose **and** approve, even with tenant-admin claims | `MailboxSourceDisableAuthorizationTests` (both commands) | Existing |
| Non-mailbox / non-tenant-admin scope denied | `MailboxSourceDisableAuthorizationTests.DisableProposalShouldRequireHumanMailboxScope` | Existing |
| Disabled source blocks worker **before** fetch/submit with `mailbox_source_disabled` | `GraphMailboxIntakeWorkerTests.DisabledMailboxSourceShouldBlockIntakeBeforeFetch...` | Existing |
| Sibling Active source unaffected (isolation) | same worker test (2nd source submits) | Existing |
| Block is recoverable **await-admin** (not poison / not blind retry) | same worker test | **Gap filled** |
| Audit envelope carries actor/scope/subject/reason/old/new-state/policy-snapshot | `CommandGatewayTests.MailboxSourceDisableAuditEnvelopeShouldCarryActiveToDisabledTransition...` | Existing |
| Audit envelope carries **timestamp** | same gateway test | **Gap filled** |
| `StateTransition "Active->Disabled"` | same gateway test | Existing |
| No mailbox-content / PII / `@` / `secret` / project leakage | same gateway test (serialized assertions) | Existing |
| Audit-unavailable → no durable disable + no intake-block (fail closed) | `CommandGatewayTests.MailboxSourceDisableApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` | Existing |
| OpenAPI / generated-client / checksum parity | `Hexalith.ChatBot.Client.Tests` (17) | Existing (regression) |
| Contract wire/serialization + finite control-state tokens | `AdminContractTests.MailboxSourceDisableContractsShouldSerializeFiniteStateTokens...` | Existing |
| Safe-recovery catalog entry (headline ≤80, one-sentence, safe next-action, MetadataOnly, disabled-action reason) | `MessageCatalogContractTests.CatalogEntriesShouldBeSafeAndSerializationTolerant` validates the `MailboxSourceDisabled` entry | Existing |

## Gaps Auto-Applied

### Server.Tests — Gateway/CommandGatewayTests.cs
- `MailboxSourceDisableAuditEnvelopeShouldCarryActiveToDisabledTransitionAndRemainMetadataOnly`
  — added `envelope.Timestamp.ShouldBe(FixedClock.FixedUtcNow)`. AC9 explicitly enumerates
  **timestamp** among the required audit-envelope fields; every other field
  (actor/scope/subject/reason/old/new-state/policy-snapshot + `Active->Disabled`) was
  asserted, but the timestamp was not.

### Workers.Tests — Mailbox/GraphMailboxIntakeWorkerTests.cs
- `DisabledMailboxSourceShouldBlockIntakeBeforeFetchWhileSiblingActiveSourceIsUnaffected`
  — added `OperationClass == "message-intake"`, `NextRetryAt == null`, and
  `SafeNextAction == "escalate"`. AC4 requires the disabled-source block be a recoverable
  **await-admin** outcome (mailbox-admin re-enablement) — *not* a poison drop and *not* a
  blind-retry loop. The test proved `Kind`/`ReasonCode`/`OwnerRole` but not the
  retry/await-admin classification.

Both fills strengthen existing tests (no new test count), keeping the suites linear and
independent. No gaps remain against the AC9 enumeration.

## Results

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s).
- `Hexalith.ChatBot.Workers.Tests -parallel none` → Total: 23, Failed: 0.
- `Hexalith.ChatBot.Server.Tests -parallel none` → Total: 688, Failed: 0.

## Coverage Metrics

- AC9 enumerated proofs with automated coverage: 17/17 (15 pre-existing, 2 hardened this pass).
- Acceptance Criteria 1–9: fully covered across aggregate / gateway-authorization /
  gateway-audit / worker / contract / message-catalog / client-parity suites.
- Production code changed: none — test-only additions.

## Next Steps

- Run alongside Contracts/Client/Conformance/Architecture regression in CI (unchanged this pass).
- The deferred durable read-side projection that feeds the worker's
  `IMailboxConfigurationProvider` from the `MailboxSourceDisabled` event (noted in the
  story completion notes) is out of scope here; when it lands, add an integration test
  driving a disabled-event projection through a live resolver into the worker block.
