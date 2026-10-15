---
title: 'Persist the First Isolated Policy Snapshot'
type: 'feature'
created: '2026-10-15'
status: 'in-review'
baseline_commit: '38ca946f1dca9978f0eed5c5948d69421d6dac17'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Legacy policy writes bypass the canonical guard. Current snapshots and shared storage lack accepted immutability, protection and native isolation evidence.

**Approach:** Enforce A6 denial, establish immutable history and exact decision references, and require accepted protection and witnessed isolation before live persistence.

**Scope decision (2026-10-15):** Build guards and explicitly synthetic snapshot behavior now. Live persistence, native isolation and protection acceptance remain incomplete; Story 1.6 stays open. Synthetic metadata cannot represent accepted A6 evidence.

## Boundaries & Constraints

**Always:** Use trusted tenant/actor context, EventStore domain writes and SDK projections/cursors. Retain snapshot ID, tenant, schema/policy versions, effective UTC, actor, provenance, redaction, retention, hold, export/delete, backup, encryption and supersession/rollback references. Protection values require accepted evidence. Preserve historical decisions and catalog outcomes.

**Never:** Substitute prefixes, filters, admission markers, tests or flags for A6 approval. Add another store, migrate history, modify siblings, deploy or claim readiness. Atomicity remains stories 1.7/1.8.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior |
| --- | --- | --- |
| Persistence | Missing A6, unavailable store or absent/stale/failing isolation proof | Safe denial before policy mutation |
| ID/version reuse | Equivalent/conflicting contents | Replay/conflict; retain original |
| Resolution | Missing, stale, malformed, foreign, unauthorized or ambiguous | Deny; no default policy |
| Replacement/rollback | New effective version | Preserve historical decision ID/version |
| Native isolation | No accepted native proof | Keep live persistence disabled; native adversarial acceptance remains incomplete |
| Export/evidence | Synthetic diagnostics/artifacts | No protected values/secrets; label synthetic; native backup/protection evidence remains incomplete |

</frozen-after-approval>

## Code Map

- `release-metadata.json` — A6/A13 open; historical inventory is insufficient.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`, `GovernedOperationState.cs` — legacy handlers trust versions; activation overwrites IDs. Preserve historical replay.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/ITenantAiPolicySnapshotProvider.cs` — existing resolver; production unavailable.
- `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicySchema.cs` — reuse knob validation and `ChatBotRefusalReasonCodes.PolicySnapshotUnavailable` → `dependency_degraded`.

## Tasks & Acceptance

**Execution:**

Abbreviated filenames share the preceding directory. `Gateway/` paths resolve from the task's Server or Server.Tests root.

- [x] `src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyPersistenceGate.cs`, `Gateway/ChatBotCommandAdmissionPipeline.cs`, `Gateway/ChatBotDomainServiceAdmissionStage.cs` — deny policy submit/approve before reservation/dispatch and signed-marker bypass; reuse audit/redaction and authentication semantics.
- [x] `src/Hexalith.ChatBot.Server/Governance/Policy/ImmutableTenantPolicySnapshot.cs`, `TenantPolicySnapshotHistory.cs`, `TenantPolicySnapshotResolver.cs` — pure internal model/fold/resolver; defensive copies, conflict detection and prospective replacement. No runtime registration/store.
- [x] `src/Hexalith.ChatBot.Server/Governance/AiMediation/TenantAiPolicySnapshot.cs`, `AiActionPolicyDecision.cs`, `DefaultAiActionPolicyEvaluator.cs` — carry exact tenant/ID/version; keep production unavailable.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Governance/Policy/ImmutableTenantPolicySnapshotTests.cs`, `Gateway/PolicyPersistenceAdmissionTests.cs` — test matrix, nested mutation, historical references and two-tenant resolution. Exercise legacy/canonical transports and signed markers; assert no reservation/dispatch/policy events. Label fixtures synthetic.
- [x] `docs/adrs/isolated-policy-snapshots.md` — document denial, missing acceptance and future native proof obligations.

**Acceptance Criteria:**

- Given missing A6, when any policy transport submits, then safe denial precedes policy persistence.
- Given synthetic history, when replaced/replayed/rolled back, then original values and decision references remain immutable.
- Given unresolved/unauthorized evidence, when evaluated, then no permission or restricted metadata escapes.
- Given synthetic evidence only, when reported, then native acceptance remains incomplete and status cannot become done.

## Implementation Notes

- Implemented the approved synthetic scope only. The build artifact is `in-review` and sprint Story 1.6 remains open in `review`; live persistence, accepted A6 protection, witnessed native isolation and native persisted-end-state acceptance remain incomplete.
- Policy submit/approve and direct association-threshold update denial now precedes risk evaluation, admission reservations and dispatch in the shared pipeline. SDK admission applies the same guard before accepting even a valid signed marker, while preserving authentication and tenant-binding denial semantics. The gateway maps `policy-snapshot-unavailable` to the existing `dependency_degraded` safe outcome.
- Added an unregistered internal synthetic snapshot/history/resolver: complete closed-schema validation; defensive copies of every nested collection; structural equivalent replay versus ID/version conflict; prospective replacement and rollback; exact authorized tenant/ID/version resolution with freshness and ambiguity denial. Protection references remain explicitly `synthetic-unaccepted`.
- AI policy evidence and decisions carry exact tenant/ID/version. Missing, malformed, unavailable, invalid, stale or foreign evidence returns a redacted unavailable decision without resolved policy metadata. The production provider remains unavailable; existing synthetic acceptance/recovery providers and test fixtures supply their exact tenant/version for the internal constructor.
- Historical aggregate/replay behavior and public wire contracts remain unchanged. Updated obsolete accepted-policy gateway expectations, added synthetic HTTP legacy/canonical and signed-marker SDK denial tests, and documented remaining native proof obligations in `docs/adrs/isolated-policy-snapshots.md`.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-1: threshold policy write bypass | high | `SetAssociationConfidenceThresholds` is documented as a security-sensitive policy update, remains in the allowlist/authority catalog, and dispatches threshold events. The new guard omits it. Patch the command-name predicate and test the existing path. |
| BH-2: unavailable reason lost by approval admission | false | The registered deterministic classifier receives `ChatBotAuthorityPrincipal`, whose binding/owner claims exclude requester authority; it routes execution as approval-required before policy resolution. The production policy provider is unavailable. Thus the claimed newly reachable missing-policy execution path does not exist; the new dependency mapping does handle the actual policy-write denial. |
| BH-3: actual persisted decisions lose resolved tuple | false | No live resolved policy decision can be produced: the production provider returns null and the trusted principal supplies no low-risk requester authority. The approved scope explicitly excludes live persistence and preserves public wire contracts; the synthetic decision retains its exact tuple and historical tests verify it. Existing dispatcher wire records cannot substantiate the claimed new live persistence loss. |
| BH-4: expected version absent from request | false | The current request selects an immutable snapshot identity, not an independently supplied expected version; the internal resolved decision records the provider's exact version. The synthetic fold rejects ID/version reuse with conflicting contents. No runtime provider returns competing versions, so a mismatched requested version is not a reachable input to this change. |
| BH-5: AI provider retains mutable collections | medium | The primary-constructor AI evidence record already retained lists/maps before this change; a mutable provider can change its authorization values. The new synthetic snapshot model defensively copies all collections. Defer the pre-existing AI-provider contract issue; production remains unavailable. |
| BH-6: provider-owned cancellation escapes | medium | The catch excludes every `OperationCanceledException`, even when the caller token is active. A provider-owned timeout then escapes instead of producing unavailable evidence. Patch caller-token discrimination; group with EH-1. |
| BH-7: dependency denial increments authorization counter | medium | The new policy guard uses `DenyAsync`, whose unconditional counter feeds `AuthorizationFailureSpikeEvaluator` (baseline ten failures). Authorized protection dependency failures can trigger security alerts. Patch the counter for this exact dependency reason while retaining safe audit. |
| BH-8: approval SDK fixture serializes submit payload | low | The SDK theory labels approval envelopes but always serializes submit values. Although denial occurs before payload handling, this fails to demonstrate the valid approval envelope boundary. Patch the fixture to serialize the matching command. |
| BH-9: signed workload fallback lacks coverage | medium | The new policy branch can call `ResolveBoundGrantIssuer` after `ResolveCommand` returns null; the added test uses a human principal and directly invokes the stage. Add signed EventStore-workload `/process` coverage with bound-origin/actor extensions. |
| BH-10: new policy branch lacks binding regression cases | medium | The added unauthenticated gateway case does not cover the new SDK guard's subject/tenant comparisons or missing/conflicting tenant context. Add focused binding cases asserting existing safe reasons and no reservation/dispatch. |
| EH-1: independent provider timeout | medium | Same demonstrated catch-filter defect as BH-6; patch active-caller-token timeouts to unavailable and preserve genuine caller cancellation. |
| VG-1: early-route reference redaction unverified | medium | Pre-verified gap: existing evaluator and HTTP execution tests exercise early approval routing but only assert reasons. Add unavailable snapshot and null resolved-reference assertions, plus dispatched/pending-record identity assertions. Patch. |

## Design Notes

Footprint: internal Server model/admission, tests and ADR only. Preserve public wire contracts. No migration, deployment or external mutation; the synthetic model has no runtime registration or persistence path.

## Verification

- Build Debug with project references: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj -p:UseHexalithProjectReferences=true -m:1 /nr:false`.
- Run affected Server classes, then Server/Architecture individually via `.github/scripts/run-xunit-v4.sh`, zero skips, retained TRX; other lanes if affected.
- Attempt the required Aspire baseline before code edits. Record blockers. Completion requires accepted A6, native persisted-end-state tests and `docs/story-evidence-integrity.md` qualification.

### Execution evidence (2026-10-15; synthetic, not A6/native acceptance)

- Required Aspire baseline attempted before edits: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2. Projects restore failed with NU1903-as-error for `Microsoft.AspNetCore.SignalR.Common` 10.0.12 / GHSA-334f-vvp4-7g6p; an OpenSSL development-certificate warning also appeared. No running topology or native isolation proof was established.
- The exact required Server Debug build failed at restore with the same NU1903 advisory. Focused fallback `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -m:1 /nr:false` passed with zero warnings/errors. No repository gate or dependency version was changed; the original audit-enabled gate remains blocked.
- Architecture and RecoverySandbox projects also built individually in Debug with project references and the same command-line audit workaround, zero warnings/errors.
- Ran the catalog-pinned native runner through `.github/scripts/run-xunit-v4.sh` with `XUNIT_REQUIRE_ZERO_SKIPS=1` and retained TRX: immutable snapshot class 6/6, AI policy class 15/15, affected gateway class 429/429, full Server 2719/2719, full Architecture 123/123. All passed with zero skips. Native CTRF, TRX and checksum evidence is retained under `TestResults/story-1-6/` (`immutable-snapshots`, `ai-policy`, `gateway`, `server`, `architecture`).
- These are synthetic in-process invariants and transport checks. They do not satisfy accepted native protection/backup evidence, adversarial native persisted-end-state isolation, or a completion transition under `docs/story-evidence-integrity.md`. Story 1.6 stays open.

### Final review verification (2026-10-15)

- Applied BH-1, BH-6/EH-1, BH-7, BH-8, BH-9, BH-10 and VG-1: all three policy write commands deny before effects; provider-owned cancellation becomes redacted unavailable while caller cancellation propagates; dependency denials remain audited without authorization spike counts; valid approval payload, signed workload `/process`, binding failures, and early-route reference redaction are covered. BH-5 was recorded in `deferred-work.md`; the other reviewed claims were refuted as recorded above.
- Re-ran the exact audit-enabled Server build: exit 1, NU1903-as-error for `Microsoft.AspNetCore.SignalR.Common` 10.0.12 / GHSA-334f-vvp4-7g6p. The gate remains unchanged. Debug Server and Architecture builds with project references and invocation-only `-p:NuGetAudit=false` each passed with zero warnings/errors.
- Final full suites passed with zero skips via `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh`: Server 2743/2743 (`TestResults/story-1-6/server-final.trx`), Architecture 123/123 (`TestResults/story-1-6/architecture-final.trx`), each with native CTRF and SHA-256. Review-fix selectors passed 55/55 before the full rerun.
- Matrix audit matched every row to passing final Server tests: persistence/native isolation to `SyntheticPolicyWritesMustDenyBothLegacyAndCanonicalAdmissionBeforeReservation`, `SyntheticSdkPolicyWritesMustDenyEvenAnAuthenticSignedMarker`, `SyntheticHttpPolicyTransportsMustReturnSafeDependencyDenial` and `ProductionPolicyProviderMustRemainUnavailable`; ID/version reuse to `SyntheticHistoryMustReplayEquivalentContentsAndRetainOriginalOnIdentityOrVersionConflict`; resolution to `SyntheticResolutionMustBeExactAndTenantBoundWithoutAnyDefault` and the malformed/unresolved evaluator cases; replacement/rollback to `SyntheticReplacementAndRollbackMustPreserveHistoricalDecisionReferences`; safe synthetic diagnostics/evidence to `SyntheticSnapshotMustDefensivelyCopyEveryMutableCollection`, `SyntheticUnresolvedReferencesMustNotPermitExecutionOrRevealProviderMetadata` and `SyntheticUnavailableOrMalformedProviderMustNotLeakItsFailureOrGrantPermission`. These are synthetic denial/invariant checks, not native persistence, backup, or A6 protection acceptance.

- Final workflow status: retained spec `in-review` and sprint `review`, overriding the generic terminal `done` transition because the frozen approved intent explicitly prohibits completion on synthetic evidence. Native protection/isolation and the normal audit-enabled gate remain incomplete.

### Review Findings

Code review 2026-10-15 (diff `38ca946..HEAD`, 4 layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor).

- [x] [Review][Decision] Resolved 2026-10-15: gate `SubmitEscalationPolicyChange` (added to `TenantPolicyPersistenceGate.Denies`; its two gateway tests became denial tests). Should it be denied by the A6 policy gate? — `TenantPolicyPersistenceGate.Denies` is a three-name list. `SubmitEscalationPolicyChange` is mapped to the `tenant-policy-mutation` audit path but is not gated, while AC1 says "any policy transport submits → safe denial". Options: add it to the gate (and tests), or record that escalation policy is a separate snapshot family out of Story 1.6 scope. Medium. [src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyPersistenceGate.cs:10] (edge-case-hunter+acceptance-auditor+blind-hunter)
- [x] [Review][Patch] Assert resolved-but-denied evaluator decisions carry exact tenant, version and snapshot id (`low_risk_policy_false`, `policy_surface_mismatch`); today the `Routed(snapshot, …)` overload could drop them with no test failing [tests/…/AiMediation/AiActionPolicyEvaluatorTests.cs]
- [x] [Review][Patch] Add evaluator cases for an unsafe `AssistanceKinds` entry and an unsafe `EffectSurface` expecting `Blocked`/`policy-snapshot-unavailable` [tests/…/AiMediation/AiActionPolicyEvaluatorTests.cs]
- [x] [Review][Patch] Pin resolver guards: future-effective snapshot resolves to null; principal without an authenticated identity resolves to null [tests/…/Policy/ImmutableTenantPolicySnapshotTests.cs]
- [x] [Review][Patch] Pin history rejections: first snapshot carrying supersedes/rollback, supersedes naming a non-previous id, rollback to an unknown source, rollback with a different schema [tests/…/Policy/ImmutableTenantPolicySnapshotTests.cs]
- [x] [Review][Patch] Evaluator catch block returns `Unavailable()` for a non-cancellation exception even when the caller token is already cancelled; call `cancellationToken.ThrowIfCancellationRequested()` first [src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs:43-50]
- [x] [Review][Defer] `GovernedOperationAggregate` / `AcceptedCommandDispatcher` still handle the three policy commands and are not gated; only the gateway pipeline and SDK admission stage deny — deferred: all members maybe-false (unverified medium). Settle by tracing whether any dispatch path reaches the aggregate without either admission stage (the EventStore→/process callback runs the SDK stage, which is gated).

Applied 2026-10-15: Server tests 2748/2748 (was 2743; +5 new), zero skips, built with invocation-only `NuGetAudit=false -p:UseHexalithProjectReferences=true`. Story remains `review`.

#### Rejected

- False: "unauthorized callers get `dependency_degraded` instead of an authorization denial" — the gate sits after the authorization result in `ChatBotCommandAdmissionPipeline`, so authorization failures return first.
- False: "retry of an accepted policy command returns 503 instead of the original outcome" — no policy command can be accepted while the gate denies unconditionally.
- False: "ID-or-version reuse check depends on append order" — ids and versions are unique within a history, so a snapshot equal to entry N cannot match any other entry.
- False: "authorization-failure counter skip is unspecified" — spec BH-7 says dependency denials do not increment the counter.
- Intended: evaluator collapses stale/invalid/unavailable to `Blocked` with `policy-snapshot-unavailable` — matches the I/O row "Resolution: deny; no default policy" and the ADR redaction rule.
- Low, not worth fixing: resolver takes `isAuthorized` as a bool and a hard-coded 5-minute bound (internal, unwired); empty history not tenant-bound (resolver filters by tenant); list-order comparison in `HasSameValues`; early-route snapshot id now `"unavailable"`; using-directive ordering; deleted audit-shape tests; tenant-id charset in `IsSafePolicyToken`; hard-coded fold magic numbers in tests.
