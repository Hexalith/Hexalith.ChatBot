# Test Automation Summary — Story 7.27 (Command allowlist v1 + full lifecycle completion)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only)

**Stack:** .NET 10 / xUnit v3 / Shouldly / NSubstitute. This is a governed command-spine backend — there is **no browser UI**, so "E2E" here = the API / acceptance / aggregate / gateway-stage integration suites. Tests run with the compiled in-process xUnit v3 runners (`dotnet test` is sandbox-blocked: `SocketException (13)`).

## Scope

Story 7.27 shipped already-substantial test coverage. This QA pass ran a gap analysis of the **existing** tests against the AC10 acceptance-coverage checklist and **auto-applied** every gap found. Tests only — no feature code touched.

## Discovered Gaps (auto-applied)

### Gap 1 — Metadata completeness asserted only 3 of the 4 required v1 fields (AC2/AC10)
`EveryV1MemberMustResolveNonNullMetadataWithAllFourRequiredFields` named "all four required fields" but asserted only **effect surface**, **authority class**, and **idempotency contract** — it never asserted the 4th required field, **default risk** (`CommandDefaultRisk : AiActionRiskClass`). A member could have shipped with an undefined/wrong risk class and the suite would have stayed green.
- **Fix:** added field-by-field assertions (labelled 1–4) including that `CommandDefaultRisk` is a defined `AiActionRiskClass` and matches the documented per-command classification (`AppendConversationMessage → ApprovalRequired`, `ExecuteLowRiskAssistance → LowRisk`).
- **File:** `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/ApprovedAiActionCommandAllowlistTests.cs`

### Gap 2 — Aggregate enforcement seam never version-gated the new v1 vocabulary (AC5/AC10)
The `GovernedOperationAggregate.Handle(ExecuteApprovedAIAction)` seam test only rejected `Project.SendEmail` — a command in **neither** the M0 nor v1 set. Nothing proved a **v1-only** member (`ExecuteLowRiskAssistance` — allowlisted at v1 but not at M0) still **fails closed at the M0 version** at the live seam. That is exactly the "v1 breadth must not leak into the M0 floor, version-keyed on `command.CommandAllowlistVersion`" invariant AC5/AC10 require.
- **Fix:** added `HandleApprovedAiActionExecutionShouldVersionGateV1OnlyCommandAtTheAllowlistSeam` — a v1-only command presented at `M0AllowlistVersion` is rejected with `ChatBotRefusalReasonCodes.CommandNotAllowlisted`.
- **File:** `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`

## Coverage vs AC10 acceptance-coverage list

### Allowlist v1
- [x] v1 resolves expected set at `V1AllowlistVersion` — `V1AllowlistShouldAddBreadthWithoutRelaxingTheVersionGate`
- [x] every member has non-null metadata with **all four** required fields — strengthened this pass (Gap 1)
- [x] M0 set unchanged (`{Project.AppendConversationMessage}` at `.m0`) — `M0SetMustNotBeMutatedByV1Work`
- [x] command not in v1 rejected at v1 — `TenantDisallowedForAiCommandsAreExcludedFromV1`
- [x] v1 command at wrong version rejected — `CommandRequestedAtTheWrongVersionMustBeRejected`
- [x] `disallowed-for-AI` excluded from v1 — `TenantDisallowedForAiCommandsAreExcludedFromV1`
- [x] enforcement seams fail closed for un-allowlisted command — aggregate seam (`...ShouldRejectNonAllowlistedCommand` + new v1 version-gate test, Gap 2); dispatcher/DI exercised via existing gateway/bootstrap suites

### Lifecycle
- [x] completed vocabulary/matrix — `StateVocabularyShouldBeStableAndOrdered`, `ValidatorShouldAcceptExplicitEdgesOnly`
- [x] duplicate-suppression + out-of-scope-mailbox map via guard to valid `Received->Skipped` — `GuardShouldMapEverySkipTriggerToAValidReceivedToSkippedTransition`
- [x] `Skipped` terminal (no outgoing edge; reprocess → new instance) — `SkippedShouldBeTerminalWithNoOutgoingEdge`
- [x] every guard switch arm resolves to Valid — `EveryGuardSwitchArmShouldResolveToAValidTransition`
- [x] representative invalid transition rejected + recorded — `GuardRejectedTransitionShouldBeRecordedWithTheInvalidReasonCode`

### Cross-actor isolation
- [x] disabled/quarantined/rate-limited service-client, CLI-class, MCP-class denied with correct reason + recorded actorType — `CrossActorTypeIsolationParityTests`
- [x] UI/CLI/MCP parity for ≥1 isolation scenario — CLI/MCP parity here + `Epic5AdapterIntentParityTests` (UI/CLI/MCP)

## Test Results

Built with `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded, 0 Warnings, 0 Errors**.
Ran compiled in-process xUnit v3 runner (`-parallel none`):

| Project | Total | Failed |
|---|---|---|
| `Hexalith.ChatBot.Server.Tests` | **938** | **0** |

Was 937 pre-pass; **+1** = new aggregate-seam version-gate test. Gap 1 strengthened an existing test in place (no count change).

`git submodule status`: no gitlink drift. No public wire contract touched — generated client / OpenAPI / checksum unchanged (tests-only change).

## Validation (checklist.md)

- [x] API/acceptance tests generated/strengthened (no browser UI → API-level E2E)
- [x] Tests use standard framework APIs (xUnit v3 + Shouldly)
- [x] Happy path + critical error/version-gate cases covered
- [x] All generated tests run successfully (938/0)
- [x] Clear descriptions, no hardcoded sleeps, order-independent
- [x] Summary saved with coverage metrics

## Next Steps

- Run the full suite in CI on the next pipeline pass.
- If a future story adds a third v1 member, the metadata-completeness test now enforces a defined `CommandDefaultRisk` for it automatically.
