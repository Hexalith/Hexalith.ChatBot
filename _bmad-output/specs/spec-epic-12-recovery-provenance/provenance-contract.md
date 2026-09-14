# Recovery Provenance Contract

This companion applies AD-1 through AD-9 without renumbering or weakening them. If this rendering conflicts with the adopted architecture spine, the spine wins and the contradiction must be logged rather than silently resolved.

## Existing Recovery Contract Preserved

The completion-provenance work does not replace Story 12.15's live recovery scope:

| Exercise | Required coverage | Preserved result contract |
| --- | --- | --- |
| Continuity | `eventstore-outage` and `m365-subscription-failure` through the real composed ChatBot boundary | `met`, `missed`, and `unmeasurable` remain distinct; report observed RPO/RTO, reconstructability, loss, isolation, unauthorized mutation, restoration, and residual external-M365 fidelity. |
| Projection rebuild | Every configured non-empty versioned baseline dataset, using immutable source metadata and WORM history without mailbox re-ingestion | Equivalence/divergence, duration-exceeded, and unmeasurable remain separate; report dataset/schema identity, structural digests, duration, population, isolation, and cleanup. |
| Scoped outage | `graph`, `identity`, `ai-provider`, `command-execution`, `audit-store`, and `attachment-processing` | Contained/breached, late scope recording, and unmeasurable remain separate; report affected/unaffected scope, safety assertions, recoverability, duplicates, latency, restoration, and cleanup. |

Fault authority remains outside the ChatBot process in the explicitly enabled Tier-3/AppHost harness. The run is admitted only for a `ReplayTenantPolicy.IsTestTenant` tenant, closed scenario tokens, non-empty data, and complete controller capability. Missing coverage, wrong environment or tenant, failure to inject/observe/restore/clean, timeout, or missing evidence is unmeasurable and stop-ship; there is no scripted or deferred fallback.

A10 covers only RPO <= 15 minutes and RTO <= 4 hours under NFR56. Projection rebuild <= 4 hours belongs to NFR57, and dependency/scope recording <= 5 minutes belongs to NFR41. Runtime code never mutates these targets. Confirmation, tightening, loosening, remediation, or a return to provisional requires its own governed decision and qualifying operational evidence.

## Interfaces and Ownership

| Interface or boundary | Input | Output / authority | Fail-closed rule |
| --- | --- | --- | --- |
| `IContinuityDrillScenarioRunner` | Closed continuity scenario, test tenant, configuration, cancellation | `ContinuityDrillReport` through the existing coordinator/evaluator/audit path | Product default stays inert; only the opted-in Tier-3 harness supplies the live runner. |
| `IProjectionRebuildDriver` | Versioned non-empty dataset, immutable source metadata, WORM history, isolated partition | `ProjectionRebuildReport` with equivalence, duration, population, and provenance | Any mailbox/current-upstream/sibling-tenant read, zero population, cleanup failure, or unverifiable comparison is unmeasurable/stop-ship. |
| `IScopedOutageInjectionDriver` | One allow-listed dependency token, expected scope, test tenant, cancellation | `ScopedOutageDegradationReport` through the existing coordinator/evaluator/audit path | Unknown tokens, unobserved faults, missing independent control, restoration/cleanup failure, or missing scope evidence is unmeasurable/stop-ship. |
| `CompletionProductionPlanner` | Exact base/head, policy, active contracts, story lifecycle, scope/File List/mappings, result paths | Side-effect-free authorization for zero or one canonical recovery producer | Any malformed, mismatched, colliding, retained, or multiple-consumer declaration prevents Dapr admission. |
| Scenario cleanup producers | Policy-declared scenario/mutation identity and observed postconditions | `hexalith.chatbot.recovery-cleanup-observation/v1` | Producers cannot write the aggregate receipt; negative, missing, duplicate, unexpected, or timed-out observations cannot become complete. |
| Repository cleanup finalizer | Complete closed observation set plus Aspire/Dapr/ephemeral teardown observations | Sole `hexalith.chatbot.recovery-cleanup-receipt/v1` writer | Only exact identity/cardinality with every required positive postcondition yields `complete`. |
| `RecoveryTrxSanitizer` + `RecoveryCleanupTrxBinding` | Raw producer TRX outside retention plus the validated exact receipt bytes | Canonical metadata-only TRX carrying the exact cleanup schema/status/digest attributes | Non-passing raw results or any non-complete/malformed/mismatched receipt is rejected. |
| `ProvenanceAttestor` | Preflighted canonical current-run lane results and exact scope digest | Sole TE-2 provenance-sidecar writer | No sidecar is written until every current-run lane preflights; partial multi-lane attestation is forbidden. |
| `StoryEvidenceValidator` | Canonical TRX, sidecar, retained receipt, policy, exact candidate | Independent completion-transition verdict | Revalidates receipt identity/schema/status and exact-byte digest; any mismatch keeps the check red. |
| Artifact publishers | Closed diagnostic staging and post-validation authoritative/rejection staging | Two disjoint 30-day artifacts in exclusive deadline windows | Both are always attempted; diagnostics cannot satisfy TE-2/A10, and only a passing completion-transition report has completion authority. |
| `LiveRecoveryValidationEvidenceGate` | Fresh scheduled/release reports and manifests | Independent operational recovery/A10 verdict | It cannot mint TE-2 sidecars or satisfy `recovery-primary`. |
| TE-2 ledger | Branch pattern, exact check/source, rule identifier, owners, verification run | Sole durable activation record | Missing or unverified facts keep activation pending and Story 12.15 / TE-2 in review. |

## Decision Contract

### AD-1 — Current-run completion source

- Policy source is exactly `current-run`.
- Canonical result is `recovery-primary/live-recovery-validation.trx`.
- Canonical sidecar is `recovery-primary/live-recovery-validation.provenance.json`.
- Locator is exactly `file:recovery-primary/live-recovery-validation.trx`.
- One exact transition has at most one active recovery consumer. Retained input, alternate paths, or a second consumer fails before production.

### AD-2 — Exact-head production order

After exact base/head resolution, `CompletionProductionPlanner` validates the pinned policy, strict contract grammar, lifecycle/status, scope digest, File List, checked mappings, collision-free result paths, exact recovery binding, and consumer cardinality before Dapr admission. The authorized order is:

1. Run the bound `LiveContinuityAspireE2eTests` producer.
2. Close recovery observations, restore faults, dispose Aspire, and tear down Dapr/ephemeral state.
3. Finalize the aggregate cleanup receipt.
4. Project raw output into the canonical metadata-only TRX.
5. Preflight every current-run lane before any sidecar write.
6. Let `ProvenanceAttestor` alone write sidecars.
7. Independently validate immutable inputs.
8. Publish the post-validation authoritative or rejection artifact.

Producer, skip/no-test, timeout, restoration, cleanup, projection, preflight, attestation, validation, or publication failure leaves the exact transition check red.

### AD-3 — Operational evidence boundary

Scheduled and release recovery bundles feed `LiveRecoveryValidationEvidenceGate` and A10 governance only. They do not mint TE-2 sidecars and cannot satisfy `recovery-primary`. The current-run completion result and diagnostics cannot ratify A10. Adding retained completion requires a policy-version change and a new architecture decision.

### AD-4 — Evidence channels and authority

The transition job retains only `contents: read` and `actions: read` permissions.

| Channel | Closed contents | Authority and failure behavior |
| --- | --- | --- |
| `recovery-primary-diagnostics` | One `hexalith.chatbot.recovery-primary-diagnostics/v1` inventory, allow-listed scenario reports, a byte-identical receipt copy at `cleanup/recovery-cleanup-receipt.json`, and `failure/recovery-attempt-summary.json` | Metadata-only, retained 30 days, always attempted on success/failure, and has neither TE-2 nor A10 authority. Locators may name only these entries under `artifact:recovery-primary-diagnostics`; TRX, sidecars, and `test-output` locators are invalid. |
| `story-evidence-integrity-reports` | Policy-allowed validated reports, the aggregate receipt, canonical sanitized TRX, and provenance sidecars | Sole completion-authority channel, retained 30 days and uploaded after validation is attempted. Only a passing `completion-transition` report for the exact protected-main candidate has authority; no-transition, diagnostic, rejection, skipped, or neutral outcomes do not. |

The raw producer TRX remains outside every upload path. Amelia owns workflow/schema implementation, Murat owns evidence-authority policy, and Winston reviews the boundary.

### AD-5 — Deadlines

All absolute minutes are measured from the recorded job start; producer-relative durations are measured from producer start.

| Deadline | Owner and required outcome |
| --- | --- |
| Before absolute minute 40 | CI refuses late Dapr admission. |
| 10 minutes after admitted initialization starts | Initialization completes or fails. |
| Producer-relative minute 250 | Recovery harness in-process workflow deadline. |
| Producer-relative minute 265, capped at absolute minute 315 | CI sends external `SIGINT`. |
| 15 minutes after `SIGINT` | CI forcibly terminates if needed, without exceeding absolute minute 330. |
| Producer-relative minute 285 | Producer step completes or fails. |
| Absolute minute 330 | Producer hard stop. |
| Absolute minute 335 | Topology and Dapr/ephemeral-store teardown completes or fails. |
| Absolute minute 350 | Receipt finalization, projection, attestation, and validation completes or fails. |
| Absolute minutes 350-355 | Exclusive always-attempted diagnostics upload window. |
| Absolute minutes 355-360 | Exclusive always-attempted authoritative/rejection upload window; failure of the diagnostic upload cannot skip it. |
| Absolute minute 360 | Job ceiling. |

CI owns outer deadlines; the recovery harness owns the 250-minute in-process deadline. Contract tests must preserve `250 < 265 < 285` and `315 < 330 < 335 < 350 < 355 < 360`. No phase borrows the next reserve.

### AD-6 — Required check and activation identity

Only pull requests whose base is protected `main` emit the globally unique, always-run check name `story-transition-evidence-integrity (pull_request)`. No-transition is an executed planner verdict, never a skipped job. Other pull-request bases and every push, schedule, or manual event use distinct diagnostic identities with no completion authority. Protected `main` disallows direct pushes.

The check is not active merely because repository code exists. Until TE-2 records the protected-branch pattern, exact check name, GitHub Actions source/app, ruleset/protection identifier, named owners, and verification run URL/ID, documentation says the check *must be required*. Story 12.15 remains `review` and TE-2 remains `in-review`.

### AD-7 — Cleanup observation, receipt, and TRX binding

`recovery-cleanup-policy.json` v1 owns the expected observation set. Every `hexalith.chatbot.recovery-cleanup-observation/v1` record carries repository, exact base/head, implementation digest, producer run ID, workflow run ID/attempt, job identity, policy-declared scenario and mutation class, stable postcondition IDs, and outcome `complete`, `incomplete`, or `timed-out`.

The mutation set includes injected resource/subscription fault state, run-owned derived/read-model/sentinel/fresh-partition keys, and ephemeral topology state. Immutable EventStore history is never directly deleted; it stays run-scoped until the ephemeral store is torn down.

After fault restoration, compensating erasure, bounded quiescence, postcondition reads, Aspire disposal, and Dapr/ephemeral-store teardown, one repository-owned finalizer alone writes `hexalith.chatbot.recovery-cleanup-receipt/v1` at `recovery-primary/recovery-cleanup-receipt.json`. It requires exactly one identity-matching observation for every policy-declared scenario/mutation class plus Aspire disposal, Dapr uninstall, and ephemeral-store teardown. Missing, duplicate, unexpected, non-`complete`, or non-true required postconditions make the receipt non-complete.

`RecoveryCleanupTrxBinding` is the sole serializer/parser used by both `RecoveryTrxSanitizer` and `StoryEvidenceValidator`. On the TeamTest `TestRun` root it writes exactly these unqualified attributes:

- `hexalith.cleanupReceipt.schemaVersion`
- `hexalith.cleanupReceipt.status`
- `hexalith.cleanupReceipt.sha256`

The SHA-256 is exactly 64 lowercase hexadecimal characters over the validated UTF-8 receipt bytes. Missing, duplicate, malformed, or unexpected `hexalith.cleanupReceipt.*` attributes fail. Sanitization accepts only `complete`; validation reloads the retained receipt, revalidates identity/schema/status, recomputes the exact-byte digest, and matches all three attributes before accepting `recovery-primary`.

### AD-8 — Executable version authorities

| Component | Authority |
| --- | --- |
| .NET SDK | Root `global.json`: `10.0.400` feature band with `latestPatch`; CI derives or asserts agreement. |
| Aspire | AppHost SDK and aligned central package catalog, currently 13.5.3. |
| Dapr | CLI 1.18.2, runtime 1.18.4, Linux x64 archive SHA-256 `ccfff008fd16f50096a9192ad56697ac7052e3add6fa0a07789d87b4c4df8c40`. |
| `actions/upload-artifact` | v7.0.1 at full SHA `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a`. |
| `actions/download-artifact` | v8.0.1 at full SHA `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c`. |

An executable-reference update requires explicit dependency review and focused recovery/provenance verification.

### AD-9 — Destructive-run isolation

The producer runs only on a fresh GitHub-hosted runner with job-local ephemeral topology/state, `Testing` admission guards, generated per-run secrets, and no production endpoints or credentials. Fixed logical test tenants are permitted only inside that isolated topology. Every run-owned resource/record identity and cleanup observation binds workflow run ID/attempt.

The required job has no GitHub concurrency group because GitHub may cancel an older pending member even when `cancel-in-progress` is false. Independent isolated jobs may run concurrently. Shared or self-hosted infrastructure requires a new architecture decision and a non-canceling external lease.

## Activation Checklist

All items remain pending until independently verified:

- Emit the globally unique authoritative check only for pull requests targeting protected `main`; make all other events/bases non-authoritative and disallow direct protected-main pushes.
- Require that exact check from the GitHub Actions app in the protected-main rule and capture the rule/source plus a verification run in TE-2.
- Split the diagnostic and authoritative artifacts and enforce their closed schemas and 30-day retention.
- Implement the complete cleanup observation/receipt/finalizer/codec/validator chain, including timeout and failure paths.
- Enforce the producer-relative and absolute `330/335/350/355/360` closeout/publication gates.
- Enforce fresh-runner isolation, job-local ephemeral state, `Testing` safeguards, generated secrets, production exclusion, run-attempt identity, no concurrency group, and the shared-infrastructure prohibition.
- Align completion execution with `global.json`, Aspire 13.5.3, Dapr CLI/runtime 1.18.2/1.18.4, the verified Dapr archive digest, and the reviewed artifact-action SHAs.
- Reconcile the companion architecture/ADR/runbook, pass focused recovery/provenance checks, and pass the Reviewer Gate.
- Keep Story 12.15 at `review`, TE-2 at `in-review`, and the architecture at `activation: pending` until the full TE-2 activation record exists.

## Deferred Decisions

| Decision deferred | Revisit condition |
| --- | --- |
| Producer-side retained TE-2 completion | Completion must reuse a prior run; requires a new ADR, policy version, immutable pre-upload attestation, rerun-safe artifact identity, and mutation tests. |
| Merge-queue `merge_group` enforcement | Merge queue is enabled; define the exact transition range and collision-free required identity before activation. |
| `DW-124` age-override binding | Every age-overridden lane can be allow-listed or binding-verified regardless of `requiredClasses`, proven by refusing `recovery-primary` without its trigger. |
| Hosted duration calibration | A transition-declared hosted run supplies distributions; tuning must preserve the absolute ordering and fail-closed refusal. |
| Long-lived artifact identity | Retention beyond deletion/expiry or post-upload artifact ID/digest binding becomes a requirement. |
| Recovery authenticity, production equivalence, and A10 ratification | Murat/A10 governance receives separate fresh qualifying operational evidence; completion provenance alone cannot close these concerns. |

## Reconciled Contradictions

| Existing statement | Authoritative resolution |
| --- | --- |
| Story 12.15 requires a shared or per-commit GitHub concurrency group. | Superseded by AD-9: the required job has no GitHub concurrency group; isolation is job-local, and shared infrastructure needs a new decision plus external lease. |
| Completion or diagnostic publication may retain raw `.trx` output. | Superseded by AD-4: raw producer TRX is never uploaded; diagnostics and canonical sanitized completion output use disjoint closed channels. |
| Dapr 1.18.0, Aspire 13.4.6, older current-run timing, or mutable artifact action tags govern completion. | Superseded by AD-5 and AD-8: use the exact authorities, hashes, SHAs, and deadline ordering above. |
| A passing scheduled/release bundle can satisfy `recovery-primary`, or current-run completion can ratify A10. | Superseded by AD-1 and AD-3: these are separate trust purposes and cannot substitute for one another. The 2026-08-27 release bundle is historical, stale for current A10 qualification, predates the controlled-loss requirement, and never exercised the current-run completion path. |
| Story 12.15 can move to `done` from repository implementation or a green historical run alone. | Superseded by AD-6 and the activation contract: Story 12.15 and TE-2 remain in review until external protected-main enforcement and the exact verification record exist. |

No unresolved contradiction remains between the adopted decisions and the rendered contract. Historical implementation/review notes remain audit evidence, but any conflicting prescription is non-current.

## Story Impact

- Stories 12.11-12.13 retain their scenario, report, safety, and verdict obligations; their scheduled/release outputs are operational evidence, not story-completion authority.
- Story 12.14 retains non-destructive runtime scheduling. It does not grant the ChatBot process destructive resource-lifecycle authority.
- Story 12.15 owns the current-run recovery completion producer, cleanup proof, evidence split, activation work, and the boundary with separate A10 evidence.
- Story 12.16 remains separate; live Memories binding is neither implied nor activated by this provenance contract.

## Validation Contract

- Coherence: every capability has intent and success, every constraint changes an implementation choice, non-goals are explicit, and the success signal is demonstrable.
- Preservation: every load-bearing AD-1-AD-9 rule and every still-valid Story 12.15 recovery requirement maps to this companion or `SPEC.md`.
- Mechanical: paths, check names, schemas, attributes, hashes, version authorities, ordering inequalities, scenario/dependency counts, and activation states must be checked exactly.
- Brownfield truth: validation must distinguish accepted design from active repository and GitHub enforcement; absent activation evidence is a failure, not an assumption.
