# ADR: Live recovery-validation drivers and sandbox fault authority

## Status

Proposed (2026-08-01, Story 12.15). Winston owns the architecture decision and sandbox-suitability conclusion; Murat owns authenticity review of the resulting Tier-3 evidence. Winston approved the non-circular `recovery-primary` completion-provenance slice on 2026-08-24, and ratified the domain-service projection-identity decision below on 2026-08-27 against a genuine hosted bundle; neither approval authenticates a hosted run for A10 by itself or ratifies A10, which remains provisional (see "A10 governance" below).

This ADR moves to **Accepted** when Story 12.15 reaches `done`. It was briefly marked Accepted on 2026-08-01 and returned to Proposed by the round-4 review: the story is still `in-progress` with open evidence-integrity and residual decisions. The matrix below reflects the post-remediation seams; residuals that remain open are named explicitly. Withdrawal from Accepted follows the same rule in reverse — a review that re-opens a matrix row or a residual returns this ADR to Proposed.

## Context

The continuity, projection-rebuild, and scoped-outage coordinators created by Stories 9.11, 9.12, and 9.13 preserve the required verdict semantics, test-tenant guard, and audit-before-alert behavior, but their production registrations intentionally use inert `Deferred*` implementations. Story 12.15 must provide live implementations without giving a product process authority to stop itself or arbitrary Aspire/DCP resources, without weakening the existing evaluators, and without presenting scripted measurements as live evidence.

The local AppHost currently composes EventStore, Tenants, ChatBot, Keycloak, DAPR Redis components, EventStore Admin, and the UI. It has no live Graph/M365 resource, no hosted Workers resource, process-local `InMemoryWormAuditStore`, and no production AKS or multi-replica control plane. The primary projection subscription is also fixed to `tenant-alpha`. These are implementation constraints and evidence residuals, not facts that a test may hide.

## Decision

The sandbox is **Conditionally suitable** for Story 12.15. It is suitable only when the explicitly enabled Tier-3 lane (scheduled CI or required release, with per-commit non-cancelling concurrency on release) composes the dedicated validation tenant, versioned non-empty dataset, provider simulator, Worker/subscription path, and live drivers described here. It is not suitable for an automatic product-hosted drill and must not be described as production-equivalent.

The Tier-3 orchestration boundary owns every fault and restoration action. It builds and starts the AppHost with `DistributedApplicationTestingBuilder`, resolves exact resources from the application model, and uses Aspire 13.4.6 `ResourceCommandService.ExecuteCommandAsync` with `KnownResourceCommands.StopCommand`, `KnownResourceCommands.StartCommand`, or `KnownResourceCommands.RestartCommand` only for an allowlisted resource. The production Server never receives AppHost or DCP resource-lifecycle authority.

The Tier-3 assembly owns separate live implementations of `IContinuityDrillScenarioRunner`, `IProjectionRebuildDriver`, and `IScopedOutageInjectionDriver`. Product dependency injection retains the three `Deferred*` implementations. Live drivers are constructed explicitly by the opted-in harness and passed through the existing coordinators; no product registration silently changes mode.

Provider and component faults that cannot be represented by an Aspire resource command use a bounded sandbox control plane. It is mapped only when live recovery validation is explicitly enabled, requires a dedicated capability secret, accepts only closed scenario tokens and a tenant satisfying `ReplayTenantPolicy.IsTestTenant` with the `replay-test:` prefix, and stores metadata-only state. It cannot name an arbitrary resource, dependency, tenant, route, or command. The control plane can change only the test tenant's closed provider/component fault switches; it cannot invoke AppHost/DCP lifecycle APIs.

Activation is doubly opt-in:

1. `LiveRecoveryValidation.Enabled` must be true in the Tier-3 AppHost configuration.
2. The configured tenant must pass `ReplayTenantPolicy.IsTestTenant`.

The harness rejects a missing controller capability, missing/blank secret, production environment, non-test tenant, unknown scenario, empty or mismatched dataset, zero scenario coverage, zero rebuilt resources, stale/future/incomplete evidence, timeout, failed fault observation, or failed cleanup before it can be accepted as evidence. Such a run is `Unmeasurable` and stop-ship; there is no fallback to a deferred or scripted driver.

## Trust boundary and control flow

1. The required release/Tier-3 workflow (per-commit non-cancelling concurrency on release; scheduled/manual on CI) opts in and launches the AppHost with sandbox-only configuration.
2. The harness validates options, the controller capability, the dedicated `replay-test:` tenant, and the versioned dataset before any injection.
3. The harness resolves an allowlisted resource or invokes a closed sandbox provider/component token.
4. It checkpoints a non-zero known workload and a separate control tenant.
5. It injects the fault and proves the application observed the intended dependency failure before starting the recovery measurement.
6. It runs the existing coordinator/evaluator/audit path and inspects state-store end-state after recovery.
7. Fault restoration runs in `finally`; restoration is bounded by its own timeout, followed by declared resource health and cleanup/end-state assertions.
8. Every measured report is written to the evidence sink before `RunAll*` reduces it to aggregate counts.
9. A separate `controlled-loss-path` runner deliberately rejects one sandbox notification, witnesses retained EventStore commits before and after it, and proves the rejected candidate absent without adding a third continuity scenario.
10. The release evidence gate independently derives the controlled-loss RPO from those persisted commit timestamps, then validates freshness, provenance, coverage, cleanup, verdict dimensions, and alert reconciliation for all retained jobs.

The controller secret is minted per run by the Tier-3 harness from a CSPRNG (`RandomNumberGenerator`) and passed to the AppHost as sandbox-only configuration; no workflow secret is involved and none is required. It is never committed to the realm, manifest, logs, traces, filenames, or report. The sandbox endpoint compares it without reflecting either the configured or presented value. Controller calls and fault transitions produce metadata-only audit entries containing only run/scenario ULIDs, safe tenant/dataset locators, scenario/dependency token, action, UTC timestamp, and stable reason code.

## Closed scenario-to-mechanism matrix

| Scenario | Faulted boundary | Observed-fault proof | Recovery proof | Production-equivalence residual |
| --- | --- | --- | --- | --- |
| `eventstore-outage` | Aspire `eventstore` project resource, stopped and started by Tier-3 `ResourceCommandService`. | A previously healthy governed command through ChatBot/DAPR reaches the EventStore dependency failure and no unauthorized mutation is committed. | EventStore and ChatBot return healthy; the committed-before-outage set is reconstructed from state/audit evidence; a post-restore idempotent command succeeds; RPO/RTO and data-loss are derived from observed timestamps and sets. | Local single-process Aspire/DAPR and Redis are not AKS, durable production storage, or a multi-replica control plane. |
| `m365-subscription-failure` | Test-only topology composes a closed Graph/subscription simulator behind `GraphMailboxIntakeWorker`; the shipped AppHost has no sandbox reference. | The subscription is expired through the authenticated controller; the Worker returns its stable recoverable result and affected/control DAPR read-model sentinels remain unchanged. | Renewal drives the Worker through the generated ChatBot client; the EventStore owner-sidecar actor-state API proves the committed mailbox-intake aggregate, a lane-stable second notification proves coarse-idempotent no-duplicate behavior, both tenant sentinels remain unchanged, and all directly seeded test keys are erased. | The simulator proves the ChatBot/Worker contract and recovery path, not external Microsoft Graph behavior, throttling, permissions, webhook delivery, or tenant-scale fidelity. |
| `controlled-loss-path` | The subscription simulator admits one closed `loss` notification while the sandbox-only generated-client decorator deliberately returns a recoverable rejection. This is an evidence job, not a `ContinuityDrillScenarios` member. | EventStore proves a retained pre-fault envelope; the decorator records only the candidate ULID and observation time; the rejected candidate never becomes a durable EventStore commit. | A distinct post-recovery envelope is durable, the candidate remains absent throughout the isolation sweep, restoration and cleanup complete, and RPO is derived exclusively from the two persisted EventStore envelope timestamps. Only `0 < rpo <= RecoveryTargets.MaxRpo` qualifies. | The mechanism is implemented and locally verified, but no hosted controlled-loss artifact has yet been retained and cited. Ordinary continuity's RPO 0s remains safety evidence only. |
| `projection-rebuild` | Fresh DAPR validation partition selected by a file-loaded versioned dataset; the driver reads immutable `ProjectConversationSourceEmailView` metadata plus `IWormAuditStore.EnumerateChain` only. | A separately seeded persisted baseline is read back before rebuild; zero source or audit coverage and any of the six missing dataset categories are rejected. | Production projection stores write and read a distinct partition; source-email rebuild reconstructs captured events and replays them through the real `AssociationProjectionHandler`; duration/resources are recorded; ETag cleanup erases the rebuilt partition and the drivers stamp `cleanup-complete` from that outcome. **Source-email equivalence is no longer an identity tautology.** Governed/WORM projections remain identity-written (`RV-REBUILD-WORM`); full NFR57 coverage for audit-derived projections is still residual. | Only source-email and WORM records are projection inputs. The other four parsed categories establish configured-corpus provenance but are never materialized, seeded or compared. The manifest reports `configuredDatasetVolume: 6` and the actual compared-resource count in `datasetVolume`. WORM remains process-local and scale is non-production. |
| `graph` | The same hosted Worker-to-provider simulator boundary, including expired-subscription behavior. | The affected mailbox returns a recoverable Graph/subscription failure while the control tenant/mailbox operation stays available. | Restored intake reconciles once with no duplicate side effect and records mailbox scope. | No external Graph service or production webhook infrastructure. |
| `identity` | Allowlisted Aspire Keycloak `security` resource stop/start, with the token-acquisition/authentication boundary as the probe. | New authentication fails closed and cannot mutate the test tenant; a separately established control operation is checked according to the active token/JWKS-cache policy. | Keycloak health and token acquisition recover; a newly authenticated governed operation succeeds without broad fallback access. | Local Keycloak, development certificates, and single replica do not prove the production identity control plane or cache distribution. |
| `ai-provider` | Sandbox `IAiAssistanceProvider` exercised through the real `AcceptedCommandDispatcher` / admission path (`RecoveryDependencyExercise`), not by calling the leaf provider alone. | The dispatcher observes the injected provider failure; the independent ChatBot control operation remains available. | Restoration permits the same correlation to complete once. Safety outcomes are derived by comparing the fault switch's ground truth to what the orchestrator committed. | Proves neither product DI composition nor a production AI provider/model/quota/region outage (`RV-PROVIDER-SCALE`). |
| `command-execution` | Test-hosted `AcceptedCommandDispatcher` using a faultable `IEventStoreGatewayClient` — degrades ChatBot dispatch code. | The dispatcher reaches its EventStore dispatch plan and observes the injected client failure; unrelated ChatBot control remains available. | Restoration executes the same correlation twice with one recorded EventStore effect. Safety outcomes are derived from orchestrator commit decisions vs the fault switch. | Bypasses the HTTP gateway policy/audit pipeline; not a DAPR/AKS network partition or multi-service command-plane outage. |
| `audit-store` | Sandbox `IAuditWriter` exercised through `ChatBotCommandAdmissionPipeline`'s fail-closed `RecordPreCommitAsync` gate (`RecoveryDependencyExercise`). | Admission observes the unavailable/pre-commit outcome and records no effect; unrelated ChatBot control remains available. | Restoration accepts the same envelope once. Safety outcomes are derived from admission decisions vs the fault switch. | Does not fault product DI, durable WORM storage, KMS, or the full governed HTTP pipeline (`RV-DURABLE-WORM` / `RV-PROVIDER-SCALE`). |
| `attachment-processing` | Sandbox `IMailboxAttachmentContentSource` exercised through `AttachmentCaptureCoordinator` (`RecoveryDependencyExercise`). | The coordinator observes the retryable unavailable outcome with no content/effect; unrelated ChatBot control remains available. | Restoration makes the same metadata-only item available once. Safety outcomes are derived from coordinator decisions vs the fault switch. | Does not execute production malware scanning, Folders storage, network, or large-file behavior (`RV-PROVIDER-SCALE`). |

`graph` includes the subscription-expiry exercise; no seventh dependency token is introduced. Expected scopes continue to use the existing `ScopedOutageScopes` vocabulary.

**NFR58 / NFR41 honesty (updated).** (1) *Observed scope (NFR58).* The sandbox monitor maps the independently sourced fault signal through `ScopeForSignal` — keyed by the signal the failing component returned, not by a second copy of `ExpectedScope` — so a mismatched signal can produce `scope_escape`. Containment is observed from that signal table, not asserted by copying the expectation. (2) *Scope-recording latency (NFR41).* Product monitoring instrumentation is still absent; when honest observation stamps cannot be produced, the path must yield `unmeasurable`, not a fabricated sub-millisecond latency, and no NFR41 figure may be published as product-monitoring evidence.

## Timeouts and restoration

Per-scenario execution timeout, restoration timeout, cadence, evidence maximum age, and workflow timeout are validated together. The workflow timeout must exceed `RecoveryTargets.MaxRto` plus startup and cleanup margin if the lane is used to confirm the four-hour target. A shorter harness timeout can only yield `Unmeasurable`; it cannot demonstrate a target miss or pass.

**The lane's measurable recovery ceiling is 180 seconds, against a 4-hour RTO target.** The Tier-3 lane sets `RestorationTimeout = 3 minutes` — a deliberate trade, since a 4-hour restoration budget is impractical on every scheduled run — and each manifest publishes it as `MeasurableRecoveryCeilingSeconds`. Any genuine recovery between 3 minutes and 4 hours therefore converts to `unmeasurable`, never `missed`. The gate emits `{job}:{key}:target_exceeds_measurable_ceiling` as a non-blocking claim limitation on every passing run whose canonical target exceeds that ceiling. **A pass from this lane proves recovery within 180 seconds; it is not evidence for RTO ≤ 4 hours and must never be cited as such.** The same applies to NFR57, whose 4-hour rebuild target is measured inside the same bounded lane.

**The 180-second ceiling itself includes harness self-verification time, not only product recovery time.** The continuity EventStore-outage drill stamps `RecoveredAtUtc` only after its concurrent cross-tenant-isolation and fault-probe-absence checks complete — each of which sustains its observation for a full `AbsenceConfirmationWindow` (1 minute) by design, to prove absence rather than snapshot a single eventually-consistent read. On a healthy run, roughly a third of the 180-second ceiling is this verification window, not the product's own recovery time. This is deliberate — the sustained-isolation proof must complete before recovery is declared — and it further narrows the 180-second figure; it is disclosed here rather than re-timed.

Every destructive operation resolves its exact allowlisted target before execution. Restoration runs in `finally`, even after cancellation, assertion failure, or coordinator exception. A failed start/restart command, unsuccessful `ExecuteCommandResult`, missed health transition, or post-restore end-state failure is retained in the manifest and is stop-ship.

**Cleanup outcome is threaded.** All three live drivers stamp `CleanupComplete` from the real cleanup step's independently observed outcome (initially `false`, patched after cleanup). The evidence sink maps that into the `cleanup-complete` assertion; the gate's `{job}:cleanup_incomplete` branch is therefore reachable for a passing verdict whose cleanup failed, not only via the exception/`unmeasurable` path.

## Dataset and tenant isolation

The validation tenant is dedicated and never aliases `tenant-alpha` or `tenant-beta`; those tenants remain independent controls. The guarded logical locator remains `replay-test:recovery-validation` and must pass `ReplayTenantPolicy`. EventStore's tenant grammar does not admit `:`, so the dedicated Keycloak identity binds only inside this test topology to the closed physical partition `recovery-validation`; the Tier-3 harness fixes the matching projection topic and records both names as configuration provenance. This transport alias cannot be configured by a caller and does not weaken the logical replay-tenant guard. Note precisely what enforces that: `ReplayTenantPolicy.StorageTenantFor` is a bare prefix strip, so it would happily derive `tenant-alpha` from a label `replay-test:tenant-alpha`. The alias cannot reach a control tenant because the topology hard-codes the single label `replay-test:recovery-validation`, not because the policy excludes the control names. Any future caller-influenced label must add that exclusion. `DistributedApplicationTestingBuilder` adds the recovery sandbox only in the test assembly; the shipped AppHost neither references nor composes it. The existing DAPR state store holds the isolated validation keys. The file-loaded baseline **parses** six categories for provenance (immutable source metadata, WORM records, governed commands, approvals, policy snapshots, and attachment metadata); version, schema, and exact count must match before injection. **Only source-email and WORM records are materialized, seeded, or compared** (`RV-PROVIDER-SCALE`). Evidence preserves the distinction: `configuredDatasetVolume` records all six configured categories; `datasetVolume` is zero for scenarios that do not consult the corpus and equals the rebuild's compared-resource count.

The rebuild driver receives narrow source/evidence interfaces rather than a general service provider. Architecture tests prevent Graph/mailbox re-ingestion, Party/Folder current-state reads, sibling-tenant access, or writes to the baseline partition.

## Evidence retention and release gate

The canonical reports remain the verdict authority. A metadata-only manifest adds run/scenario ULIDs, UTC bounds, repository/topology/configuration versions, safe tenant and dataset locators, configured corpus volume, per-scenario exercised dataset volume, driver mode, fault/restore/cleanup actions, expected/observed scope, measurements and targets, verdict dimensions, assertions, coverage, deviations, durable residual identifiers, and raw-artifact locators.

When both the canonical report write and its unmeasurable fallback write fail, the coordinator makes exactly one
best-effort write through an independent retention-failure sink before auditing and alerting. Its sentinel is diagnostic
proof of evidence-sink loss, never recovery evidence. The closed `chatbot.recovery-retention-failure.v1` schema carries
only kind `evidence-retention-failure`, the canonical run ULID, a closed job/scenario pair, a UTC failure instant, and
reason `evidence_retention_failed`; it contains no tenant, path, payload, claim, exception, stack trace, credential, or
secret. Marker failure cannot mask or alter the returned `unmeasurable` report.

The live sink writes deterministic `{job}-{scenario}.retention-failure.json` files through sibling temporary files and
atomic replacement into a workflow-owned runner-temp root that is path-disjoint from the canonical evidence directory.
Same-scenario retries overwrite in place without exposing a truncated prior marker, serialized size and path containment
are bounded, and every live producer stages that independent root before other failure-prone finalization commands under
the uploaded `TestResults/retention-failures` artifact directory. The attempt summary is written once into that same
independent root rather than the canonical evidence directory, so total canonical-directory loss remains replayable.
Every live producer invocation receives that root through the required
`HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR` environment variable, which must name an absolute directory outside
the canonical evidence directory; the live E2E fails fast rather than silently writing markers nowhere, so a local
Tier-3 run must set it too. A marker write is bounded at one second and is abandoned rather than allowed to delay audit
and alert. The replay loader reads markers only from the designated uploaded directory, rejects oversized or unmapped JSON, and
maps bounded file races/read failures to invalid candidates. The external gate accepts a marker only when its schema
vocabulary is exact, its run matches the attempt, its job/scenario is closed, and its UTC timestamp is neither future,
stale, nor outside the attempt window. Any valid same-run job marker yields `{job}:evidence_retention_failed` even beside
partial or contradictorily complete evidence, and it participates in the existing alert-delivery accounting. Absent or
rejected markers preserve ordinary `{job}:missing_evidence`, while malformed markers also yield the stable
`retention_failure_marker_invalid` stop-ship reason. Neither outcome relaxes stop-ship behavior.

Release validation remains per commit and non-cancelling: every pushed SHA retains its own recovery producer and
independent evidence-gate verdict even when runs finish out of order. Publication eligibility is a separate final
decision. Immediately before semantic-release on `main`, the workflow fetches remote `main`; only an exact match
publishes, a strict ancestor succeeds explicitly as `superseded` without publishing, and missing or divergent
history fails closed. The configured `next`, `alpha`, and `beta` prerelease branches bypass this `main` comparison
and retain their existing semantic-release behavior. This guard does not weaken, replace, serialize, or cancel any
validated SHA's recovery verdict.

The required lanes initialize a metadata-only workflow-attempt envelope before checkout, finalize it under `if: always()`, and use `if: always()` artifact upload with `if-no-files-found: error` and an explicit 30-day retention policy. They upload the raw `.trx` plus generated reports/manifests under `TestResults` when produced and always retain the workflow envelope on an ordinary runner/step failure. State-store assertions are captured in the domain report verdicts; the workflow envelope is diagnostic and cannot substitute for the domain attempt summary. The lane does not currently claim separate logs, traces, metrics, or state snapshots. Local ignored `TestResults` files are diagnostic only and are not repository-retained evidence.

Evidence older than `MaximumEvidenceAge` (8 days, matching the 7-day cadence) is rejected as `stale_result`. Note the asymmetry with the 30-day artifact retention: an artifact aged 8–30 days is still downloadable but is no longer citable for ratification.

The gate fails closed on disabled required validation, never-run/incomplete/stale/future evidence, a newer incomplete attempt, exception or timeout failure, `Unmeasurable`, divergence, containment breach, unalerted serious breach (unmeasurable / structural / target-deviation only — `cleanup_incomplete` and failed safety assertions are gate-only stop-ships without an `unalerted_breach` obligation), zero population, zero scenario coverage, missing dataset/scenario provenance, missing artifact locators, or `cleanup-complete: false` (`{job}:cleanup_incomplete`). Measurable RPO/RTO misses, rebuild-duration misses, and late scope recording remain distinct target deviations and are governed by their A10/NFR decision; the gate never relabels them as met/equivalent/contained.

Reports, logs, traces, screenshots, filenames, and CI artifacts exclude message bodies, attachment content, tokens, secrets, credentials, raw claims, and other tenant payloads. Metadata is passed through existing sanitization conventions and stable reason tokens.

## Story-completion provenance lifecycle

`recovery-primary` has one TE-2 completion path: **transition-declared `current-run` production** inside the exact-head `story-evidence-integrity` job. At most one active contract may declare the logical lane, the exact bound `LiveContinuityAspireE2eTests` class selector, `source: current-run`, `trx: recovery-primary/live-recovery-validation.trx`, `provenance: recovery-primary/live-recovery-validation.provenance.json`, and its exact `file:` locator. The repository-owned side-effect-free `plan` command validates pinned policy, status/lifecycle, scope digest, File List, checked mapping declarations, collision-free paths, and this exact single-consumer binding before any destructive setup. Only its validated plan can enable DAPR initialization and the live recovery sweep; ordinary runs with no completion transition do not execute this additional producer.

The completion path and the exact-head topology producer it consumes install a checksum-pinned Dapr CLI 1.18.0 archive, initialize runtime 1.18.0, and exchange evidence with current artifact-action majors. Recovery's raw TRX is staged outside all upload paths. After the live class and DAPR cleanup succeed, `RecoveryTrxSanitizer` projects only the bound test identity, times, counters, IDs, and passing outcome into the canonical completion TRX; raw output/error diagnostics are never published. The attestor then binds exact base/head, implementation digest, sanitized-TRX checksum, selector, source, timestamp, and locator into the sidecar. Validation consumes that sidecar in the same exact-head job. The 360-minute job fixes closeout at minute 330, refuses DAPR initialization at or after minute 40, caps initialization/live execution at 10/280 minutes, sets the in-process deadline to 265 minutes, and dynamically interrupts production early enough to preserve 15 minutes of unwind before fixed closeout. Producer, timeout, skip/no-test, cleanup, projection, attestation, or validation failure leaves the completion check red.

This ordering is non-circular: the tracked contract contains no future GitHub run ID, artifact ID, artifact name, or upload digest. `scope.implementationDigest` remains the only masked contract field. GitHub assigns artifact identity only after upload, so the 30-day, deletable/expiring archive is downstream retention rather than an attestation input. Completion-run operational reports/manifests remain unpublished diagnostics and cannot be cited for A10.

Scheduled CI and release recovery artifacts retain their existing operational purpose: the independent recovery evidence gate and A10 review. They intentionally do not mint TE-2 retained sidecars, and policy does not accept `retained` as a `recovery-primary` source. A future retained completion path would require a separate ADR, policy-version change, producer-side attestation before immutable upload, rerun-attempt-safe locator identity, and mutation/adversarial tests; it is not an alternate implementation of this decision.

**Winston architecture sign-off (2026-08-24): approved for implementation and review.** The lifecycle has one statically validated producer plan, one canonical consumer/path pair, one metadata-only TRX publication owner, one attestation authority, a fixed cleanup reserve, deterministic pre-upload identity, and no contract-digest cycle. This is an architecture approval of the provenance boundary only. The overall ADR remains Proposed, A10 remains provisional, and Murat's independent authenticity review remains required before Story 12.15 can reach `done`.

## Sandbox suitability and residuals

The accepted implementation can prove the exact Aspire 13.4.6/DAPR sandbox, provider simulators, dataset, scale, and single-replica configuration named in its evidence. The following remain durable production-equivalence residuals until separately closed with authentic evidence:

- `RV-EXT-M365`: no live Graph/M365 resource; external Graph permissions, webhook, throttling, and service behavior remain unproven.
- `RV-DURABLE-WORM`: process-local `InMemoryWormAuditStore`; durable WORM/KMS/storage failure and recovery remain unproven.
- `RV-PROD-CONTROL`: no production AKS or multi-replica control plane; scheduler lease, replica coordination, and platform resource control remain unproven.
- `RV-PROVIDER-SCALE`: local provider/application-boundary simulators and the versioned baseline do not prove production traffic volume, latency, replicas, or regional failure modes. Four of the baseline's six categories are parsed for provenance only and are never materialized, seeded or compared.
- `RV-MEASURABLE-CEILING`: the lane's 180-second restoration budget is two orders of magnitude below the 4-hour RTO/NFR57 targets, so no run from it can demonstrate a miss of either; recovery between 3 minutes and 4 hours converts to `unmeasurable`.
- `RV-EVIDENCE-KINDS`: retained artifacts are `.trx` plus reports/manifests only; logs, traces, metrics, and state-store end-state dumps are not produced or required.
- `RV-REBUILD-WORM`: governed/WORM projections remain identity-written on rebuild; do not claim full immutable-source+WORM rebuild equivalence for audit-derived projections.

The absence of a hosted Workers resource is a baseline gap this story must close in the opted-in recovery topology. The residual is the difference between that controlled Worker/provider composition and an external production M365 deployment, not permission to omit the mandatory subscription drill.

## A10 governance

Story 12.15 leaves A10/NFR56 provisional at `RecoveryTargets.MaxRpo` (15 minutes) and `RecoveryTargets.MaxRto` (4 hours), with no runtime constant change.

**A measured figure is now published from a genuine hosted run.** Required release run
[33066358280](https://github.com/Hexalith/Hexalith.ChatBot/actions/runs/33066358280), commit `17aa94d`, evidence
run `01M11EYSDMP1ZF38B7KZA1A6FA` (2026-08-27) is the first bundle produced from the exact commit on `main`; it
retains nine manifests/reports with zero deviations and passed the independent out-of-process gate 1/1 **as that
gate stood at commit `17aa94d`**. DW-52 adds `controlled-loss-path` to `LiveRecoveryValidationJobs.All`, so the
shipped gate now requires a fourth job: replaying this three-job bundle through the current gate returns
`controlled-loss-path:missing_evidence` and stop-ships. Its measured figures remain readable as historical
evidence, but it can no longer be replayed clean, and only a hosted run produced at or after this commit can
carry the authenticity condition forward (pinned by
`LiveRecoveryValidationEvidenceGateTests.AnEvidenceBundleRetainedBeforeTheControlledLossChannelNoLongerPasses`).
It
supersedes the 2026-08-26 bundle at commit `397507b`, which predated a fix required by the intervening
`011261e` ("AI execution coordination") change. Both mandatory drills measured `met` (RPO 0s / RTO 149.6s and
60.5s against 900s/14400s); the projection rebuild measured `equivalent`; all six scoped dependencies measured
`contained`. See `.decision-log.md` (2026-08-27 entry) for the full bundle and the Winston/Murat review.

Ratification requires a complete passing scheduled-CI or release artifact plus its hosted run/artifact locator
recorded in the PRD decision log. The cited 2026-08-27 bundle satisfies that authenticity condition for its nine
jobs, but it predates the fourth, separate `controlled-loss-path` evidence channel. DW-52 now provides the local
mechanism for a citable non-vacuous RPO measurement: persisted EventStore bounds around one deliberately rejected
candidate, independently recomputed by the gate, with only `0 < rpo <= RecoveryTargets.MaxRpo` accepted. No hosted
artifact from that channel is cited, so the 15-minute RPO remains provisional; ordinary continuity's 0s values
cannot substitute. The 180-second measurable ceiling independently means a passing hosted run ratifies recovery
within the ceiling, not RTO ≤ 4 hours. Therefore **A10 remains provisional**. Ratifying the 4-hour target requires
either a lane whose restoration budget reaches it or a separately evidenced pre-production drill.

**Un-ratification.** A ratified A10 returns to provisional when the cited artifact passes the 8-day `MaximumEvidenceAge` freshness boundary without a successor, when a later required run is not passing, or when a hosted run returns `Unmeasurable` or a structural breach. The artifact remains downloadable through day 30 for investigation, but it cannot support ratification after day 8. Architecture/DevOps owns that transition and records it in the decision log.

Even a passing sandbox run cannot close `RV-EXT-M365`, `RV-DURABLE-WORM`, `RV-PROD-CONTROL`, `RV-PROVIDER-SCALE`, `RV-MEASURABLE-CEILING`, `RV-EVIDENCE-KINDS`, or `RV-REBUILD-WORM`. Tightening or loosening still requires the PRD decision, this ADR, and `RecoveryTargets` to change together with retained evidence, rationale, and approval.

## Decision: pin the ChatBot domain-service projection identity (2026-08-25)

*(The heading date is when the decision was taken in code, per the mechanism and evidence chain below. It is not
the ratification date — see the Status line immediately below: Winston ratified this decision on 2026-08-27,
against genuine hosted evidence that did not yet exist on 2026-08-25.)*

**Status: Ratified (Winston, 2026-08-27).** The mechanism, evidence chain, and regression guard below hold on
**two** independently produced hosted runs, not one: `397507b` (release run `32964163030`, 2026-08-26) was the
first — its `eventstore-outage` manifest already reports `cleanup-complete: true` and verdict `met` — and the
2026-08-27 hosted bundle (release run `33066358280`, evidence `01M11EYSDMP1ZF38B7KZA1A6FA`) is the second,
reproducing the same result: every continuity manifest reports `cleanup-complete: true` and the projection-rebuild
manifest reports `equivalent`, with zero checkpoint-refusal regressions in either run (21,034 dispatch failures /
42,066 checkpoint refusals eliminated, `ChatBotProjectionIdentityTests` and `ChatBotDomainServiceIdentityContractTests`
green). This ratifies the identity-resolution precedence chain and its fail-fast startup behaviour as an accepted
architecture decision; it does not by itself ratify A10 (see "A10 governance" above, which remains provisional for
unrelated reasons — the measurable-recovery-ceiling and RPO-constant-on-no-loss-path residuals), and it is
unrelated to the separate `recovery-primary` completion-provenance decision above (still static/local-only
evidence — see `.decision-log.md`, 2026-08-27 entry, "Architecture sign-off").

**Decision taken in code and now ratified:** `src/Hexalith.ChatBot.Server/Program.cs` resolves
`DomainProjectionIdentityOptions` through a **four-tier precedence chain** — `ChatBot:ProjectionIdentity`, then
`EventStore:DomainService`, then the `DAPR_APP_ID` environment value, and only then the pinned constants
`chatbot` / `v1` (`ChatBotDomainServiceIdentity`). *(Corrected 2026-08-26: this paragraph previously described a
two-tier pin "overridable through `ChatBot:ProjectionIdentity`" that "fails startup if either is blank". Both
halves understated the change. Two further channels override the constants, and the startup gate is not a
blank check: `IsUsableIdentityComponent` also rejects any value over 128 characters or outside `[A-Za-z0-9._-]`,
enforced by `.ValidateOnStart()`. That is a new hard startup-abort path for **every** deployment, and it was not
described in the record Winston is being asked to ratify.)*

**Fail-fast is deliberate.** A malformed candidate is **not** skipped in favour of the next tier: it is returned
by the resolver and rejected by the gate, so the host does not boot. Falling through would substitute a silent
wrong identity for a noisy refusal — and a shape-valid but wrong identity is refused verbatim by EventStore,
which post-cutover stalls the projection checkpoint indefinitely with nothing logged as an error. The resolved
identity is also written to stdout once at startup, because nothing previously recorded which identity the host
actually chose.

**Why this is a behaviour change, not configuration tidying.** The identity is what EventStore's named-projection
capability negotiation matches on. Pinning it switches this service from the legacy projection-delivery path onto
the **v2 named-projection delivery path** with its fenced completion transition. That is a real change to how
projections are delivered and checkpointed for a shipped service, which is why it is written up as a decision.

**Evidence chain, each link observed rather than inferred:**

1. The domain-service SDK derives an unconfigured `AppId` from the `DAPR_APP_ID` environment variable, falling
   back to `IHostEnvironment.ApplicationName` (`EventStoreDomainServiceExtensions.cs:447-458`). Resolved in this
   service that fallback is `Hexalith.ChatBot.Server` — never the DAPR app id EventStore registers and invokes it
   under. *(Measured: the identity resolved to `Hexalith.ChatBot.Server` in a host without `DAPR_APP_ID`. An
   earlier reading of this defect as "AppId is empty" was wrong and is corrected here.)*
2. EventStore's operational-index refresher posts `AppId` plus `ServiceVersion` to
   `/admin/operational-index-metadata`; the SDK answers `Results.BadRequest(UnsupportedCapability)` unless both
   match its own identity and exactly one domain is requested (`EventStoreDomainServiceExtensions.cs:296-320`).
3. Observed: `HttpRequestException: Response status code does not indicate success: 400 (Bad Request)` thrown from
   `AdminOperationalIndexHostedService.LoadBindingAsync`, reaching
   `NamedProjectionDispatchCoordinator.TryDispatchAsync` — 21,034 occurrences in one run, and for the neighbouring
   `sample` and `tenants` services too, consistent with a systematic identity derivation rather than one service's
   configuration.
4. With no successful dispatch there is no fenced completion. Once the store's v2 writer protocol is active — and
   it must be, because `ProjectionDeliveryWriterProtocolHealthCheck` requires it for readiness — a scoped
   checkpoint may advance *only* through that completion, so `SaveDeliveredSequenceAsync` refuses every legacy
   advance by design.
5. Observed: 42,066 checkpoint refusals in one run across `recovery-validation`, `tenant-alpha` and `tenant-beta`;
   zero `projection-checkpoints:*` scoped rows existed.
6. The checkpoint never advancing means the poller re-delivers the same aggregates indefinitely, so a
   governed-operation read model erased during recovery cleanup is re-created inside the absence window and the
   continuity drill reports `cleanup-complete: false` — the single reason the release evidence gate stop-ships.

**Guard against silent regression:** `ChatBotProjectionIdentityTests` asserts the resolved identity is usable, is
*not* the assembly name, honours the full precedence chain, refuses an unusable configured value at startup
(`AnUnusableConfiguredIdentityFailsStartup`), and does not fall through on a malformed candidate. The
**cross-assembly** invariant — that `ChatBotDomainServiceIdentity.AppId` equals the topology's
`ChatBotAspireModule.AppId` — is asserted separately in `ChatBotDomainServiceIdentityContractTests`, the only
assembly that sees both. *(Corrected 2026-08-26: this line previously credited `ChatBotProjectionIdentityTests`
with asserting the identity "equals the DAPR app id". It asserts equality with the ChatBot constant the value came
from, which is circular; and that assertion is now conditioned on `DAPR_APP_ID` being unset, because the variable
sits above the constant in the precedence chain and the unconditional form failed for environmental reasons.)*

**Explicitly not taken:** the alternative of treating "no identity configured" as capability-absent inside the SDK
(mirroring its `404` handling) was rejected — the SDK contract is not being reopened by this story.

## Consequences

- Ordinary deployments remain non-destructive and retain inert deferred live-driver defaults.
- The destructive scheduler is the Tier-3/release workflow (CI: scheduled/manual; release: per-commit non-cancelling concurrency — N concurrent 5.5-hour jobs accepted rather than a cancellable shared queue), never `PeriodicEnforcementBackgroundService` and never a second product hosted service.
- Out-of-order `main` validation completion preserves every per-SHA verdict, but only the freshly fetched exact
  remote head may invoke semantic-release; included older SHAs finish successfully as superseded.
- A story-completion transition may additionally run the same live class inside `story-evidence-integrity`, but only when its active contract declares current-run `recovery-primary`; this is evidence production, not a product scheduler.
- AppHost lifecycle control stays outside ChatBot; the bounded sandbox controller can affect only closed provider/component switches for the dedicated test tenant.
- A sandbox pass narrows uncertainty but does not erase the named external-service, durability, scale, or production-control residuals.
- Successful, missed, and unmeasurable reports become inspectable evidence before aggregate reduction without changing evaluator semantics.

## References

- `docs/adrs/continuity-drill-and-rpo-rto-validation.md`
- `docs/adrs/projection-rebuild-validation.md`
- `docs/adrs/scoped-outage-degradation-validation.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-20-epic12-recovery-deferrals.md`
- `_bmad-output/implementation-artifacts/12-14-wire-the-m2-audit-and-recovery-runtime-scheduler.md`
- Aspire 13.4.6 `ResourceCommandService` and `KnownResourceCommands` API documentation.
