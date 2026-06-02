# Test Automation Summary — Story 7.18 (Disable AI actor)

**Workflow:** bmad-qa-generate-e2e-tests · **Date:** 2026-06-02 · **QA:** Jerome (Chatbot)
**Story:** `_bmad-output/implementation-artifacts/7-18-disable-ai-actor.md` (status: review)

## Framework

.NET 10 (`net10.0`), **xUnit v3** (in-process compiled runners, `-parallel none`), Shouldly, NSubstitute. No new
framework introduced — used the project's existing test conventions and the Story 7.15 disable cell as the
structural template. There is no UI/browser surface in scope (S5 admin status surface deferred, consistent with
7.12–7.17), so all coverage is API/domain/contract-level — no Playwright/Cypress E2E layer applies.

## Coverage Audit (existing dev-authored tests vs. the 9 Acceptance Criteria)

The dev handoff shipped a faithful mirror of the 7.15 disable cell. This QA pass audited that coverage against
all nine ACs and **auto-applied the two genuine gaps** found, rather than re-generating duplicate tests.

### Gap 1 — AC4: disabled AI actor's *AI proposal command* path (was untested)

The enforcement-seam test `DisabledAiActorShouldFailClosedBeforeGrantScopeChecksWithDistinctReason` exercised the
validator with a generic mailbox-intake command. Nothing proved the story's headline behavior — that a disabled
AI actor's **actual AI proposal command** (`ExecuteLowRiskAIAssistance`) fails closed at the authorization stage,
upstream of `AiActionApprovalGate` / policy evaluation.

- **Added:** `DisabledAiActorAiProposalShouldFailClosedAtAuthorizationStageBeforeApprovalGate`
  in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`.
- Asserts the proposal command is denied with `ai_actor_disabled` **before** the grant scope/allowlist check
  (the AI actor's grant only allows `notes.write`, so it would otherwise be denied under-scoped), distinct from
  `service_client_disabled`, with a redacted denial (no `ServiceClientGrantEvidence` / OAuth fingerprint leaked).

### Gap 2 — AC6: catalog entry next-action/reason tokens (membership-only assertion)

`MessageCatalogContractTests.CatalogShouldExposeStableVersionAndRequiredEntries` asserted only that
`AiActorDisabled` *exists* in the catalog. Unlike its sibling entries (service-client quarantine / rate-limit),
it never asserted the AC6-mandated terminal/await-admin tokens.

- **Added:** assertions on the resolved `AiActorDisabled` entry in
  `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`:
  `NextAction == RequestAccess`, `DisabledActionReason == DisabledAction`, headline ≤ 80 chars.

## AC → Test Traceability (post-augmentation)

| AC | Proof | Status |
|----|-------|--------|
| 1 | Two-person submit→approve; single-actor disable only pends | `GovernedOperationAggregateTests` (proposal/approval) | ✅ existing |
| 2 | Policy-admin gate; tenant-admin via union; non-policy/service/AI denied; distinct approver | `AiActorDisableAuthorizationTests` | ✅ existing |
| 3 | Audit envelope refs + `Active->Disabled`; fail-closed pre-commit | `CommandGatewayTests` (envelope + audit-unavailable) | ✅ existing |
| 4 | Disabled AI actor fails closed at validator with `ai_actor_disabled`; **AI proposal command path** | `ServiceClientGrantAuthorizationTests` (generic + **new proposal-command test**) | ✅ **gap filled** |
| 5 | Disable affects future admission only; idempotent; no mutation of prior records | `GovernedOperationAggregateTests` (no-op/idempotency, state apply) | ✅ existing |
| 6 | Finite catalog guidance; **request-access + disabled-action tokens** | `MessageCatalogContractTests` (**new token assertions**) | ✅ **gap filled** |
| 7 | Metadata-only safe tokens; no credential/OAuth/prompt/PII leakage | `AdminContractTests`, `CommandGatewayTests` (serialized redaction) | ✅ existing |
| 8 | OpenAPI-first + regenerated client + checksum parity | `Hexalith.ChatBot.Client.Tests` + `hexalith-chatbot-generated-client.sha256` | ✅ existing |
| 9 | Gateway/dispatcher/aggregate distinct-approver; isolation; fail-closed audit; parity | across all suites above | ✅ existing |

## Validation Run (compiled in-process runners, `-parallel none`)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `Hexalith.ChatBot.Server.Tests` → **Total: 767, Failed: 0** (was 766; +1 new AC4 validator test).
- `Hexalith.ChatBot.Contracts.Tests` → **Total: 258, Failed: 0** (AC6 token assertions added to existing test).

## Files Changed (tests only — no production code touched)

- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`
  (new `ExecuteLowRiskAIAssistance` disabled-proposal test + `AiAssistanceProposal()` helper + `Contracts.Queries` using)
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
  (AC6 next-action/reason/headline assertions for the `AiActorDisabled` entry)

## Checklist

- [x] API/contract tests generated (gateway, validator, aggregate, audit, catalog)
- [x] E2E: no UI surface added for this story (deferred per AC6/Dev Notes); covered by contract + conformance suites
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly)
- [x] Happy path + critical error cases covered
- [x] All generated tests run successfully (767 + 258 green)
- [x] Semantic, intention-revealing assertions; clear descriptions
- [x] No hardcoded waits/sleeps; tests independent (no order dependency)
- [x] Summary saved with coverage metrics

## Next Steps

- Run the full regression (`Client`, `Conformance`, `Architecture`) in CI; the author already recorded
  Client 17 / Conformance 75 / Architecture 37 green — unaffected by these test-only additions.
- When the durable read-side projection of `AiActorDisabled` (deferred per 7.12/7.15) is built, add an
  integration test that the projection feeds `IAiActorControlStateProvider` end-to-end.
