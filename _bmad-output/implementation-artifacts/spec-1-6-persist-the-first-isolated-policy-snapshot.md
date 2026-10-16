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
| R4-BH-1: two snapshot-write commands omit A6 denial | high | `ChatBotAuditPathMap` identifies mailbox configuration and notification routing as tenant-policy mutations; their gateway tests currently dispatch, and notification routing activates a snapshot. Add both names to the shared gate, cover transports and authorization precedence, and correct the ADR. Patch. |
| R4-BH-2: provider collection enumeration/lookup can throw | maybe-false | The pre-existing AI evidence contract accepts provider-owned collections; baseline enumeration/lookup already escaped, and all current providers use ordinary arrays/maps with production unavailable. A reachable live provider with failing collection access would establish impact; defer as unverified medium before live activation. |
| R4-BH-3: actor token validation excludes stable subject characters | low | The synthetic constructor rejects `@` and `|` accepted by the request resolver. Only explicit synthetic fixtures construct this unregistered type; no authenticated request enters it. Reject additional actor-contract branching for this unused future integration; the approved intent excludes live persistence. |
| R4-BH-4: metadata conflict replay not covered | medium | Existing conflict tests alter values, tenant, ID and version but cannot detect omission of actor, effective time or protection references from content equality. Add identity-reuse cases that retain the original history. Patch. |
| R4-BH-5: schema separation cases absent | medium | Constructor tests cover incomplete M0 and unknown knobs; they do not pin rejection of M1-only knobs in M0 or incomplete M1. Add those closed-schema contract cases. Patch. |
| R4-BH-6: nested canonical replay not covered | medium | Equivalent replay reverses only outer values, leaving map/scope insertion-order equivalence unchecked. Add independent nested collections with differing order. Patch. |
| R4-BH-7: history monotonic boundaries not isolated | medium | Existing tests use reused version numbers or backdated times; no unused lower version or equal effective timestamp pins strict prospective append behavior. Add rejection and unchanged-history assertions. Patch. |
| R4-BH-8: resolver UTC and exact freshness boundary absent | medium | The tests deny future and over-five-minute evidence but do not assert both UTC guards or acceptance exactly at five minutes. Add those observable resolution boundaries. Patch. |
| R4-BH-9: negative signed-workload fallback coverage absent | medium | Policy tests cover valid workload markers and human binding failures; resolver-only authority tests do not exercise tampered/malformed workload evidence through the new policy branch. Add authentication denial, safe audit and zero-effect cases. Patch. |
| R4-BH-10: production DI boundary not exercised | medium | The unavailable-provider test constructs its type directly, so it cannot detect a live provider registration or synthetic model registration. Add a service/host test with acceptance disabled. Patch. |

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

### Review Findings (round 2)

Code review 2026-10-16 (diff `38ca946..HEAD`, 4 layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor). No `decision-needed`.

- [x] [Review][Patch] Pin the evaluator catch's caller-cancellation re-check: add a case where the provider cancels the caller token then throws a non-cancellation exception (`new FixedProvider(null, fails: true, onRead: caller.Cancel)`), expecting `OperationCanceledException`; deleting the `ThrowIfCancellationRequested()` inside the catch currently fails no test [tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs]
- [x] [Review][Patch] Pin history ID reuse with a new version: add `Snapshot(id: "synthetic-first", version: 2, supersedes: "synthetic-first")` against `[v1]` expecting `TryAppend == false`, `replay == false`; dropping the `existing.SnapshotId == snapshot.SnapshotId` clause currently fails no test [tests/Hexalith.ChatBot.Server.Tests/Governance/Policy/ImmutableTenantPolicySnapshotTests.cs]
- [x] [Review][Patch] ADR understates the denied surface: it lists submit, approve and association-threshold commands, but `TenantPolicyPersistenceGate.Denies` also denies `SubmitEscalationPolicyChange` [docs/adrs/isolated-policy-snapshots.md:8]
- [x] [Review][Defer] Gate not enforced in `GovernedOperationAggregate` / `AcceptedCommandDispatcher` (flagged again by Blind Hunter and Acceptance Auditor) — deferred: already recorded in `deferred-work.md` (round 1); unverified medium, no new evidence

Applied 2026-10-16: the two new tests plus the ADR edit; the two touched test classes pass 30/30 (Debug, `NuGetAudit=false`). Story remains `review`.

#### Rejected

- False: "policy-gate unauthorized callers get `dependency_degraded` on the SDK path" — the SDK stage only re-runs the workload's authentication and tenant binding; authorization already ran in the gateway pipeline, which returns first. Round 1 refuted the same claim.
- False: "retry of an accepted policy command returns 503" — the gate denies unconditionally, so no policy command is accepted to be retried (round 1).
- False: duplicate `KnobId` values retained — `TenantPolicySchema.Validate` rejects them (`seen.Add`).
- False: `EvidenceKind` not compared / not enforced — it is a constant property, so every instance carries `synthetic-unaccepted`.
- False: mixed-tenant history lets one tenant resolve for another — `Resolve` filters every snapshot by tenant, and `TryAppend` requires a matching tenant after the first entry.
- Intended: stale/invalid/unavailable now `Blocked`, and early routes report snapshot id `"unavailable"` — matches the I/O row "Resolution: deny; no default policy" and the ADR redaction rule.
- Intended: blank requested snapshot id is now `Blocked`, which makes the acceptance provider's default-id branch unreachable — spec says "no default policy"; tests pass 2748/2748.
- Low, not worth fixing: `DenyAsync` reason-string coupling; resolver bool/5-minute bound; hard-coded four-name gate list (a catalog-derived test needs an agreed definition of "policy-mutating"); casing variants of `CommandType`; non-logged provider failure; `StringListValue` order sensitivity; no-op rollback source; using-directive order; partial-class file naming; deleted escalation audit-shape tests (restore when the gate lifts); 503 retry semantics (spec-mandated `dependency_degraded`); SDK stage unauthorized-vs-degraded wording.
- Rejected (spec edit only): frontmatter `in-review` vs sprint `review`, `review_loop_iteration`, execution-evidence totals.

### Review Findings (round 3)

Code review 2026-10-16 (diff `38ca946..HEAD`, 4 layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor). No `decision-needed`.

- [x] [Review][Patch] Pin the unresolved-policy outcome at the gate/pipeline: add a pipeline test with the real evaluator and a null/stale/foreign provider asserting the caller-visible rejection and zero dispatch/reservation (the Routed→Blocked change is only pinned at `EvaluateAsync`) [tests/Hexalith.ChatBot.Server.Tests/Gateway/, AiActionApprovalGate]
- [x] [Review][Patch] Pin authorization precedence: add a denying `IAuthorizationStage` case with a policy command expecting the authorization reason, not `policy-snapshot-unavailable`; moving the gate above authorization currently fails no test [tests/Hexalith.ChatBot.Server.Tests/Gateway/PolicyPersistenceAdmissionTests.cs]
- [x] [Review][Patch] Cover `SubmitEscalationPolicyChange` in the gateway/SDK/marker theories — only "submit"/"approval"/"thresholds" rows exist, so the fourth gated command's branches are untested [tests/Hexalith.ChatBot.Server.Tests/Gateway/PolicyPersistenceAdmissionTests.cs:37-148]
- [x] [Review][Patch] ADR claims the AI evaluator "checks exact tenant/ID/version evidence", but the evaluator matches tenant and ID and only requires `PolicyVersion >= 1` (the request carries no version); correct the wording [docs/adrs/isolated-policy-snapshots.md:32-33]
- [x] [Review][Defer] Gate not enforced in `GovernedOperationAggregate` / `AcceptedCommandDispatcher` (raised a third time) — deferred: already in `deferred-work.md`; unverified medium, settled by tracing dispatch paths that skip admission
- [x] [Review][Defer] Audit-enabled build red on `Microsoft.AspNetCore.SignalR.Common` NU1903 (GHSA-334f-vvp4-7g6p); evidence only passes with `NuGetAudit=false` and no tracked item exists — deferred: pre-existing dependency advisory, not caused by this change

#### Rejected

- False: "superseded older version resolves as current" — exact-version resolution of historical references is the intent ("Historical decision references remain exact"); "stale" refers to the 5-minute evidence-freshness bound.
- False: "signed policy follow-up gets AuthenticationDenied" — the SDK stage already falls back to `ResolveBoundGrantIssuer` for valid markers, and the signed-marker theory covers it.
- False: "unvalidated `LowRiskAllowedByActionClass` keys" — keyed by enum; requested classes pass `TryParseActionClass`.
- False: "replay of accepted policy commands broken" — rejected in rounds 1–2 (no policy command can be accepted).
- Intended: Routed→Blocked for stale/invalid/unavailable policy, `"unavailable"` snapshot id on early routes, `SafeNextAction none` — I/O row "deny; no default policy".
- Intended: `SdkConfirmation` early `return` for the policy row — policy writes are denied by design.
- Intended: audit fact recorded while the counter is skipped — spec BH-7 scopes the counter skip to dependency denials.
- Low, not worth fixing: 503 retry semantics (spec-mandated); non-contiguous/first-version history and unbound first tenant (synthetic, unwired); null-argument guards in `HasSameContents`/`Resolve`; lazy provider property reads outside the catch; provider-tautology tests; silent catch/no logging; `StringListValue` order; hard-coded four-name gate (no agreed definition of "policy-mutating"); ADR gate-lifting checklist; style/naming nits.
- Rejected (spec edit only): `review_loop_iteration`, test-total inconsistencies, untracked TRX evidence references.

### Round 3 implementation verification (2026-10-16; synthetic)

- Applied all four round-3 patches. Added 21 passing cases: six missing/stale/foreign-provider cases exercise the real evaluator through `AiActionApprovalGate` and assert caller-visible `refusal_blocked_action` with no reservation, dispatch, status, or metadata disclosure; eight cases pin authorization precedence for every gated command across legacy/canonical admission; seven escalation rows cover gateway, HTTP, SDK, authentic human markers, and signed workload `/process`. The two new evidence fixtures are test-only and explicitly synthetic. Corrected the ADR to distinguish exact tenant/ID matching and a positive resolved version from an expected-version request, which does not exist.
- Required pre-edit baseline `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2 on the existing `Microsoft.AspNetCore.SignalR.Common` 10.0.12 NU1903-as-error / GHSA-334f-vvp4-7g6p; an OpenSSL certificate warning also appeared. No running topology or native proof was established.
- Exact required build `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj -p:UseHexalithProjectReferences=true -m:1 /nr:false` exited 1 on that same NU1903 advisory. The Server and Architecture test projects each built in Debug with the same arguments plus invocation-only `-p:NuGetAudit=false`, zero warnings/errors. No dependency or repository gate changed.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh` passed the affected gateway class 471/471, full Server 2770/2770, and full Architecture 123/123, all with zero skips. Native CTRF, TRX, and SHA-256 evidence is retained under `TestResults/story-1-6/` as `gateway-round-3`, `server-round-3`, and `architecture-round-3`.
- Current persisted spec and sprint status remain `in-progress`. These results prove synthetic admission and invariant behavior only; live persistence, accepted A6 protection, witnessed native isolation, native persisted-end-state acceptance, and completion qualification remain incomplete. Existing deferred findings remain deferred.

### Independent review and final verification (2026-10-16; synthetic)

- Ran all three workflow review layers. Edge Case Hunter and Verification Gap returned no findings. Blind Hunter supplied ten findings; the triage log records each independently: eight patched groups, one deferred pre-existing provider-contract concern, and one rejected low-impact synthetic actor-format concern. No intent or specification loopback was required.
- Closed the two demonstrated gate omissions: `SubmitNotificationRoutingChange` and `SubmitMailboxConfigurationChange`. All six commands classified by the existing audit map as `tenant-policy-mutation` now deny before admission effects, including legacy/canonical HTTP, SDK admission, and authentic markers. Existing notification/mailbox audit expectations now assert safe denial; historical handlers and replay remain unchanged.
- Added immutable metadata-conflict, closed-schema separation, nested canonical replay, strict prospective append, exact UTC/freshness, negative signed-workload fallback, and acceptance-disabled real-host registration coverage. Focused review-fix tests passed: gateway 501/501 and snapshots 25/25, zero skips.
- Re-ran `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj -p:UseHexalithProjectReferences=true -m:1 /nr:false`: exit 1, existing NU1903-as-error for `Microsoft.AspNetCore.SignalR.Common` 10.0.12 / GHSA-334f-vvp4-7g6p. Server and Architecture Debug builds with invocation-only `-p:NuGetAudit=false` passed with zero warnings/errors. The repository gate and dependency versions remain unchanged.
- Final native runner commands passed with `XUNIT_REQUIRE_ZERO_SKIPS=1`: `bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story-1-6/server-reviewed.ctrf.json -result-trx TestResults/story-1-6/server-reviewed.trx` (2817/2817) and the corresponding Architecture runner with `architecture-reviewed` evidence paths (123/123). Independently parsed both TRX files and verified CTRF SHA-256 sidecars; no failures or skips. `git -c core.whitespace=cr-at-eol diff --check` passed using the repository's required CRLF convention.
- Re-audited every frozen matrix row against passing final Server results. Persistence/native-disable behavior has 12 gateway, 18 SDK/marker and 12 HTTP denial cases plus unavailable-provider/registration assertions. Replay/conflict, two-tenant exact resolution, prospective replacement/rollback, and safe synthetic metadata each have passing direct invariants; all new review-fix methods are present and passed in the final TRX.
- The reviewed synthetic increment is ready for review; Story 1.6 remains open. Live persistence, accepted A6 protection, witnessed native isolation, native persisted-end-state evidence and completion qualification remain incomplete. Synthetic evidence cannot authorize a `done` transition.

- Final status: spec `in-review`, sprint `review`. The frozen approved scope prohibits the generic workflow `done` transition until native acceptance and completion qualification exist.
