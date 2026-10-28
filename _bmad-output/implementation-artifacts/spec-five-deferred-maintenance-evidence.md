---
title: 'Resolve five deferred maintenance evidence items'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: '696e6dd736bd50e764ccd3a392bfbc726e82ab29'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five deferred maintenance items: DW-38's undocumented static recovery-validator fixture credential; DW-78's conflated historical shell-change attribution; DW-87's missing verification of terminal correction-propagation failure; DW-128's source-only Admin port-guard assertion; and DW-141's obsolete claim that Integration tests lack a clean developer build. Document the approved fixture residual, correct attribution using Git evidence, replace the source scan with real isolated composition, verify existing terminal/exhaustion behavior and the supported source-mapped build, update the ledger, independently review, then commit and push to main as requested. Existing correct behavior should be verified rather than duplicated.

</frozen-after-approval>

## Implementation Notes

Investigation found no unresolved intent choices, new public API, dependency changes or irreversible application actions. This explicitly requested five-item batch uses the small-change oneshot route. Work is confined to ChatBot documentation, tests and local verification records.

DW-38: extend README.md's existing local-development seed-credential policy to identify the committed recovery-validator fixture and its shared RecoveryValidatorIdentity token request. This is the ledger's permitted documented-residual closure, not per-run password rotation or production identity qualification. Verify SharedRequestMatchesTheRealmValidatorAndUsesIndependentForms and tenant separation in RecoveryValidationTopologyContractTests.

DW-78: correct Historical Evidence Correction paragraphs in implementation-artifacts/10-1-frontcomposer-shell-integration.md and implementation-artifacts/tests/test-summary.md. Git commit 7e63edc3e841559e3d12ec21aad9248c7283aa01 removed direct UI EventStore registration, suppressed the account menu, and switched the real-render test to the existing banner landmark. Commit 9462de3395453c7c8a7f0bac20bbf9978435ec48 added the correction prose and Fluent/CSS changes. Preserve the distinction between fixture/browser evidence and live rendering; do not claim new live UI verification.

DW-87: CorrectionPropagationWorkflowRunner already bounds case-resolution attempts at five and emits Failed; CorrectionPropagationWorkflowBridgeTests verifies the registered workflow's operation-status projection, cause, escalation and retry refusal. Run that class, CorrectionPropagationCoordinatorTests and OperationStatusWorkflowStatusSinkTests. Preserve Delayed for store/soft failures and pending owner work; closure covers the separately implemented exhaustion/terminal path.

DW-128: replace AddEventStoreAdminBodyInvokesThePortGuard in tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs. Compose an isolated DistributedApplication builder with synthetic project/component resources and a HexalithChatBotResources record, call AddEventStoreAdmin without AddHexalithChatBot, and verify collision diagnostics. Include distinct/unconfigured positive controls and actual sidecar assertions; no containers or processes need start.

DW-141: run the documented pinned wrapper: python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -m:1. Commit 07b8ecebe3484031a66d9aea8519bd00e7de9d29 introduced this supported mapping. Preserve package/source collision guards; package-mode defects remain separate dependencies, not resolved by this developer-build verification.

Verification will retain exact commands, native xUnit CTRF/TRX counts and source hashes in validation/five-deferred-maintenance-evidence.json. Formal story/sprint or release qualification is outside scope. Keep this maintenance spec in-review if the story-evidence gate does not admit done without an approved evidence contract.

The task-owned source-mapped no-build Aspire baseline started successfully, was inspected through aspire describe, and stopped by exact AppHost path before edits. The unrelated Tenants AppHost was left running.

The first positive controls exposed an ordering mistake in the new test setup: building the application before composition freezes the service collection. Composition now occurs before Build in both tests. Final executable controls pass. Review corrections clear ambient configuration, cover both Admin collision pairs, assert each sidecar's actual nullable port and app identity, and follow Async test naming.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Ambient Dapr settings can alter isolated collision/unconfigured cases | medium / patched | Both isolated builders clear default configuration sources before adding their owned test settings. |
| Positive controls count sidecars without checking effective ports | low / patched | Both Admin app IDs and explicit 41102/41103 or null internal ports are asserted on the composed options. |
| Admin.Server/Admin.UI collision lacks direct coverage | low / patched | The negative theory executes both spine/server and server/UI collisions and checks exact port and both IDs before sidecar mutation. |
| New async tests omit Async suffix | low / patched | Both new method names now follow the repository naming rule. |

Independent correction recheck found no remaining concrete findings or ledger overclaims. No findings were deferred.

## Verification

The documented full source-mapped Integration Debug build and Server.Tests Debug build both pass with zero warnings/errors. Final Integration rebuild after review also passes. Original topology class: 35 passed, zero failed/skipped. Original registered correction workflow, coordinator and operation-status sink classes: 63 passed, zero failed/skipped. Repository runner guards validate non-vacuous CTRF evidence. Conventional commit validation and CRLF-aware whitespace validation pass.

DW-38 uses the explicitly permitted documentation closure; its static fixture password remains intentional. DW-87 verifies the existing resolver-exhaustion terminal path while retaining Delayed for store/soft failures. DW-141 verifies the supported developer build; separately recorded package-mode/native publication gaps remain open. No new UI rendering, production credential, live recovery, release or sprint qualification is claimed. This spec remains in-review under the repository's separate story-completion evidence contract; the five maintenance ledger items are resolved. Exact commands, report hashes and source hashes are retained in validation/five-deferred-maintenance-evidence.json.
