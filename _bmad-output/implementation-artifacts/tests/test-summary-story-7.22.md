# Test Automation Summary — Story 7.22 (Quarantine command capability)

**Date:** 2026-06-03
**Workflow:** bmad-qa-generate-e2e-tests · **QA:** Jerome (Chatbot)
**Story:** `_bmad-output/implementation-artifacts/7-22-quarantine-command-capability.md` (status: review)
**Feature:** FR74 command-capability **quarantine** — two-person submit→approve control + actor-agnostic fail-closed admission seam.
**Framework:** xUnit v3 + Shouldly + NSubstitute (.NET 10 / `net10.0`). No new framework introduced — the project's existing test stack was used. Compiled in-process xUnit v3 runners (sandbox-safe, `-parallel none`), per the story's testing notes.

## Mode

Story 7.22 was already implemented (status `review`) with tests authored mirroring the Story 7.21 disable cell. This QA pass **audited the AC-9 test obligations against actual coverage**, verified parity with the reviewed 7.21 reference cell, auto-applied any gaps, and re-ran the suites to confirm green.

## Coverage vs AC-9 obligations

This is a server/contract feature — there is no UI E2E surface to drive (AC6 is satisfied by the message catalog + authorization reason code + audit metadata; the S5 admin status surface is deferred, consistent with 7.12–7.21). Coverage is therefore API/behavioural.

### Authorization + enforcement seam — `tests/.../Gateway/Stages/CommandCapabilityQuarantineAuthorizationTests.cs` (new, 8 facts)
- [x] Human **policy-admin** allowed for propose + approve; **tenant-admin** allowed (FR75a union)
- [x] Non-policy human scopes (mailbox/compliance/operations) denied; **service** + **AI** actors denied even with admin-looking claims
- [x] Distinct-approver enforced; `RequesterRef == ApproverRef` denied at the gateway (first of three checks)
- [x] Invalid metadata-only payloads rejected (bad `SourceVersion`/`SchemaVersion`/`ReasonCode`/`OldState`/`NewState`)
- [x] **Self-lockout guard** — proposing/approving a quarantine whose subject names an FR74 governance command (incl. the quarantine commands themselves) is rejected
- [x] **All-actor fail-closed** — quarantined capability → `command_capability_quarantined` for human **and** service **and** AI, *before* grant validation (proven via a sentinel-denying grant validator); reason **distinct** from `command_capability_disabled` / `command_not_allowlisted` / `service_client_grant_under_scoped` / `ai_actor_quarantined`
- [x] Isolation — sibling Active type unaffected; same type under a different tenant unaffected
- [x] FR74 governance commands exempt at the seam; provider observes only the safe tenant id + command type name (no credential/PII)
- [x] Regression — Disabled + Quarantined reasons stay distinct off **one** provider read (the 7.22 single-read switch)

### Aggregate (two-person, idempotency, immutability) — `tests/.../Operations/GovernedOperationAggregateTests.cs`
- [x] Proposal alone produces a **pending** approval, never quarantines (`IsNoOp` / empty quarantined set, not activate)
- [x] Approval by same requester ref **and** same envelope actor both rejected; distinct second human applies `CommandCapabilityQuarantined`
- [x] Subject/version/reason mismatch rejected; unknown pending → `command_capability_quarantine_unavailable`
- [x] Duplicate / already-quarantined re-submit → NoOp (idempotency)
- [x] Committing a quarantine does not mutate prior committed/pending records (AC5 / NFR17 / FR75c)

### Gateway / audit — `tests/.../Gateway/CommandGatewayTests.cs`
- [x] Audit-unavailable on the approval **fails closed** — no durable quarantine, dispatcher never invoked (no admission-block side effect)
- [x] Audit envelope carries actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp + `StateTransition "Active->Quarantined"` + `admin-scope:policy`
- [x] Redaction — no `@` / `secret` / `oauth` / `bearer` / `project-` refs (metadata-only)

### Dispatcher (third two-person layer) — `tests/.../Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- [x] `ApproveCommandCapabilityQuarantine` with `RequesterRef == ApproverRef` rejected at the dispatcher
- [x] Distinct approver passes the dispatcher guard and routes to the quarantine-change aggregate

### Contracts — `tests/.../Contracts.Tests/AdminContractTests.cs` + `MessageCatalogContractTests.cs`
- [x] Wire-token round-trip for both commands; `Quarantined` serializes to `"quarantined"`
- [x] Both quarantine command types included in the no-secret-bearing-properties guard
- [x] Catalog exposes `command_capability_quarantined` with `request-access` + `disabled-action`, headline ≤ 80; finite-reason set still valid

### Client / generated-client parity — `tests/Hexalith.ChatBot.Client.Tests`
- [x] OpenAPI ↔ generated-client parity + checksum freshness (`hexalith-chatbot-generated-client.sha256`)

## Gaps discovered / auto-applied

**None.** The audit confirmed 1:1 parity with the reviewed Story 7.21 disable cell across all 14 AC-9 obligation groups (aggregate ×5, gateway/audit ×3, dispatcher ×2, contract ×4), and the quarantine all-actor enforcement test is in fact **stronger** than the disable equivalent — it adds the reason-distinctness assertions against `command_capability_disabled` / `command_not_allowlisted` / `service_client_grant_under_scoped` / `ai_actor_quarantined`. No fabricated or redundant tests were added; there were no genuine coverage gaps to fill.

## Test run results (compiled in-process xUnit v3 runners, `-parallel none`)

| Suite | Total | Failed |
|-------|------:|-------:|
| `Hexalith.ChatBot.Contracts.Tests` | 262 | 0 |
| `Hexalith.ChatBot.Client.Tests` (OpenAPI/generated-client parity + checksum) | 17 | 0 |
| `Hexalith.ChatBot.Server.Tests` (incl. 17 `*CommandCapabilityQuarantine*`) | 835 | 0 |

Full solution build: **0 Warning(s) / 0 Error(s)** (nullable + warnings-as-errors clean).

## Coverage

- AC-9 obligation groups: **14 / 14** covered
- All-actor admission-seam fail-closed: human / service / AI all covered
- Two-person rule defense-in-depth: gateway validator + dispatcher + aggregate all covered
- Quarantine test files: `CommandCapabilityQuarantineAuthorizationTests.cs` (new) + extensions to `GovernedOperationAggregateTests.cs`, `CommandGatewayTests.cs`, `AcceptedCommandDispatcherTests.cs`, `AdminContractTests.cs`, `MessageCatalogContractTests.cs`

## Next steps

- Run suites in CI (no CI change required by this story).
- The durable read-side projection of `CommandCapabilityQuarantined` into `ICommandCapabilityControlStateProvider` is **deferred** (sanctioned by the 7.12/7.15/7.18/7.21 reviews); when implemented, add an integration test that the projected provider observes a committed quarantine end-to-end.
- A future release/re-activate flow (out of scope here) will need its own reversal-path tests.
