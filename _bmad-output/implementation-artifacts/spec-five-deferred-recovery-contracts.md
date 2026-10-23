---
title: 'Resolve five deferred recovery-harness contract items'
type: 'bugfix'
created: '2026-10-23'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'e97e7a8831768aee1ca1cb7cfc9ec5c64b7dfe68'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close recovery-harness deferred items DW-5, DW-6, DW-10, DW-11 and DW-12. Verify cleanup failure cannot contaminate a subsequent scenario and that mid-loop seeding failure safely cleans an unconfirmed reference. Add executable route, scenario-header and composite-restore contract regressions. Resolve the sandbox project from the repository content root rather than fixed AppHost depth, and share validator token-request fields between ordinary acquisition and the Identity outage probe. Preserve existing authorization, cancellation, isolation and release gates. Run focused checks and independent review, record closure evidence, commit and push only this task's changes to main as requested.

</frozen-after-approval>

## Implementation Notes

Investigation: DW-5's generation handoff is already implemented by DetachAndReset in all three cleanup states, with tests proving active successors survive incomplete cleanup. Extend AspireRecoveryCleanupStateTests to execute subsequent cleanup across EventStore, subscription, Graph and Identity, and fail seeding after one confirmed note for DW-6. Reuse the existing in-memory read-model and durable-state handlers, concrete operation constructors and environment delegates. Do not change cleanup runtime behavior.

DW-10: RecoverySandboxRoute.ScopedOutage enforces correlation shape; RecoverySandboxRestoreResponse rejects flat snapshots; RecoveryNotificationIdentity.Compose enforces lane/phase pairs. Exercise these boundaries and the subscription endpoint's real header parsing via a shared internal helper used by Program.cs. DW-11: RecoverySandboxTopologyComposer.AddRecoverySandbox uses ../../tests; replace with ancestor discovery anchored at Hexalith.ChatBot.slnx and require the sandbox project. Test relocated AppHosts and absent roots/projects in temporary directories. DW-12: RecoveryAccessTokenProvider.AcquireAsync and AspireScopedOutageOperations.TryAcquireRecoveryTokenOnceAsync duplicate validator form fields; share an internal factory and pin its values to the checked-in local realm.

There are no unresolved intent gaps, dependency changes, new public APIs or irreversible implementation operations. Footprint is confined to the existing recovery test harness/sandbox, regression tests, this spec and the deferred ledger. The explicitly requested commit/push occurs after validation and review.

Baseline: aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj exited 2 after successful Debug compilation (zero warnings/errors); AppHost exited 134 because ChatBot:LiveRecoveryValidation:MailboxClientSecret is missing. No running AppHost was found before startup. Log: /tmp/chatbot-five-harness-aspire.log. Focused infrastructure-free checks cannot replace the recovery/aspire-dapr primary lanes required for terminal completion by docs/story-evidence-integrity.md and story-evidence-policy.json. Keep the spec in-review if live evidence remains unavailable; ledger closures describe only the specific harness defects and executed checks.


Implemented and reviewed the five ledger items. The shared header helper is called by the real sandbox endpoint; HTTP coverage starts the actual sandbox executable on an OS-assigned loopback port, uses bounded startup/request/cleanup deadlines and verifies rejected requests never increment processing/submission counters. Sandbox discovery stops at the nearest repository marker even if its project is missing. Validator forms are shared by both callers and verified against the local realm's enabled public direct-grant client and non-temporary credential. Realm-reader verification lives in RecoveryValidationTopologyContractTests alongside its fixture writer to avoid cross-class read/write races.

Validation commands and results:

- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 --nologo` failed with existing CS1704 duplicate Hexalith.Commons.UniqueIds/Serialization assemblies in unchanged dependency projects; log `/tmp/chatbot-five-contracts-build.log`.
- `dotnet build tests/Hexalith.ChatBot.RecoverySandbox/Hexalith.ChatBot.RecoverySandbox.csproj --configuration Debug -p:UseHexalithProjectReferences=true -p:BuildProjectReferences=false --no-restore -m:1 --nologo` passed with zero warnings/errors against available dependency outputs; log `/tmp/chatbot-five-contracts-sandbox-build.log`.
- The integration build command above with `-p:BuildProjectReferences=false` passed with zero warnings/errors after the final source changes; log `/tmp/chatbot-five-contracts-final-build.log`.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests TestResults/five-recovery-contracts-final.ctrf.json -result-trx TestResults/five-recovery-contracts-final.trx` with repeated `-class Hexalith.ChatBot.IntegrationTests.Recovery.<class>` for AspireRecoveryCleanupStateTests, RecoverySandboxBoundaryTests, RecoverySandboxProjectPathTests, RecoverySandboxHttpBoundaryTests, RecoverySandboxContractTests and LiveContinuityAspireE2eContractTests passed 128 tests with zero failures/skips.
- The same boundary with output `TestResults/five-recovery-contracts-topology.{ctrf.json,trx}` and repeated `-method Hexalith.ChatBot.IntegrationTests.Recovery.RecoveryValidationTopologyContractTests.<method>` for SharedRequestMatchesTheRealmValidatorAndUsesIndependentForms, RecoveryWorkerSimulatorIsAbsentByDefaultAndComposedOnlyWithExplicitCapabilityConfiguration, AddRecoverySandboxRejectsInvalidCapabilityTenantOrEnvironment and RecoverySandboxComposedEnvironmentKeepsLogicalAndPhysicalTenantsDistinct passed 9 tests with zero failures/skips.
- An earlier wider class run, `TestResults/five-recovery-contracts.{ctrf.json,trx}`, executed 154 tests: 152 passed, two failed, zero skipped. The two failures are existing RealmAddsClosedRecoveryMailboxServiceIdentityWithoutACommittedSecret (an unscoped RandomNumberGenerator source assertion) and RenderedRealmServiceClientGrantExpiryIsAcceptedByTheClaimsGrantResolver (claims-only fixture against an owner-authority resolver). Their test bodies and failing dependencies are identical at the full baseline revision. Both are recorded in deferred-work.md; the wider run is not represented as passing.

Repository completion limitation: the broad dependency build, two pre-existing topology assertions and live startup remain separate limitations. The bound recovery/aspire-dapr primary lanes were not produced. This spec stays in-review under the repository evidence policy; only the five specific harness deferrals are closed by focused evidence. No product story, sprint status, release gate, dependency version or submodule pointer changed.

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| New realm reader can race the tracked fixture writer | medium / patched | Moved the reader into the writer's existing test class, where xUnit serializes methods. |
| Exception-only successor coverage omits false-returning cleanup | medium / patched | Extended incomplete subscription/Graph/Identity tests through successful successor cleanup; EventStore executes fresh seeding and cleanup after false. |
| Seeding test lacks projected-but-unconfirmed residue | medium / patched | Added a theory case whose real durable-state HTTP handler throws after the second projection persists, then asserts both projections are erased. |
| Header-helper tests cannot catch an endpoint bypass | medium / patched | Real sandbox HTTP test asserts 404 and zero processing/submission/recoverable counters for malformed headers. |
| Validator fixture test omits public-client/enabled/temporary settings | low / patched | Asserts enabled public direct-grant client and enabled user with non-temporary password. |
| Resolver test lacks nearest-root refusal | low / patched | Nested inner root without a project must refuse despite a valid sandbox in its outer checkout. |

Independent follow-up inspection confirmed all six corrections and found no concrete HTTP subprocess correctness or lifecycle defect. Review was read-only; the executed results above provide test evidence.
