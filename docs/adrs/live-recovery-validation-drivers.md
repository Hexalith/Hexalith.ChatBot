# ADR: Live recovery-validation drivers and sandbox fault authority

## Status

Proposed (2026-08-01, Story 12.15). Winston owns the architecture decision and sandbox-suitability conclusion; Murat owns authenticity review of the resulting Tier-3 evidence.

This ADR moves to **Accepted** when Story 12.15 reaches `done`. It was briefly marked Accepted on 2026-08-01 and returned to Proposed by the round-4 review: the story is still `in-progress` with open evidence-integrity decisions, and the scenario-to-mechanism matrix below still describes seams that are not built as described (see the re-opened rows). Withdrawal from Accepted follows the same rule in reverse — a review that re-opens a matrix row or a residual returns this ADR to Proposed.

## Context

The continuity, projection-rebuild, and scoped-outage coordinators created by Stories 9.11, 9.12, and 9.13 preserve the required verdict semantics, test-tenant guard, and audit-before-alert behavior, but their production registrations intentionally use inert `Deferred*` implementations. Story 12.15 must provide live implementations without giving a product process authority to stop itself or arbitrary Aspire/DCP resources, without weakening the existing evaluators, and without presenting scripted measurements as live evidence.

The local AppHost currently composes EventStore, Tenants, ChatBot, Keycloak, DAPR Redis components, EventStore Admin, and the UI. It has no live Graph/M365 resource, no hosted Workers resource, process-local `InMemoryWormAuditStore`, and no production AKS or multi-replica control plane. The primary projection subscription is also fixed to `tenant-alpha`. These are implementation constraints and evidence residuals, not facts that a test may hide.

## Decision

The sandbox is **Conditionally suitable** for Story 12.15. It is suitable only when the explicitly enabled, serialized Tier-3 lane composes the dedicated validation tenant, versioned non-empty dataset, provider simulator, Worker/subscription path, and live drivers described here. It is not suitable for an automatic product-hosted drill and must not be described as production-equivalent.

The Tier-3 orchestration boundary owns every fault and restoration action. It builds and starts the AppHost with `DistributedApplicationTestingBuilder`, resolves exact resources from the application model, and uses Aspire 13.4.6 `ResourceCommandService.ExecuteCommandAsync` with `KnownResourceCommands.StopCommand`, `KnownResourceCommands.StartCommand`, or `KnownResourceCommands.RestartCommand` only for an allowlisted resource. The production Server never receives AppHost or DCP resource-lifecycle authority.

The Tier-3 assembly owns separate live implementations of `IContinuityDrillScenarioRunner`, `IProjectionRebuildDriver`, and `IScopedOutageInjectionDriver`. Product dependency injection retains the three `Deferred*` implementations. Live drivers are constructed explicitly by the opted-in harness and passed through the existing coordinators; no product registration silently changes mode.

Provider and component faults that cannot be represented by an Aspire resource command use a bounded sandbox control plane. It is mapped only when live recovery validation is explicitly enabled, requires a dedicated capability secret, accepts only closed scenario tokens and a tenant satisfying `ReplayTenantPolicy.IsTestTenant` with the `replay-test:` prefix, and stores metadata-only state. It cannot name an arbitrary resource, dependency, tenant, route, or command. The control plane can change only the test tenant's closed provider/component fault switches; it cannot invoke AppHost/DCP lifecycle APIs.

Activation is doubly opt-in:

1. `LiveRecoveryValidation.Enabled` must be true in the Tier-3 AppHost configuration.
2. The configured tenant must pass `ReplayTenantPolicy.IsTestTenant`.

The harness rejects a missing controller capability, missing/blank secret, production environment, non-test tenant, unknown scenario, empty or mismatched dataset, zero scenario coverage, zero rebuilt resources, stale/future/incomplete evidence, timeout, failed fault observation, or failed cleanup before it can be accepted as evidence. Such a run is `Unmeasurable` and stop-ship; there is no fallback to a deferred or scripted driver.

## Trust boundary and control flow

1. The serialized release/Tier-3 workflow opts in and launches the AppHost with sandbox-only configuration.
2. The harness validates options, the controller capability, the dedicated `replay-test:` tenant, and the versioned dataset before any injection.
3. The harness resolves an allowlisted resource or invokes a closed sandbox provider/component token.
4. It checkpoints a non-zero known workload and a separate control tenant.
5. It injects the fault and proves the application observed the intended dependency failure before starting the recovery measurement.
6. It runs the existing coordinator/evaluator/audit path and inspects state-store end-state after recovery.
7. Fault restoration runs in `finally`; restoration is bounded by its own timeout, followed by declared resource health and cleanup/end-state assertions.
8. Every measured report is written to the evidence sink before `RunAll*` reduces it to aggregate counts.
9. The release evidence gate validates freshness, provenance, coverage, cleanup, verdict dimensions, and alert reconciliation.

The controller secret is minted per run by the Tier-3 harness from a CSPRNG (`RandomNumberGenerator`) and passed to the AppHost as sandbox-only configuration; no workflow secret is involved and none is required. It is never committed to the realm, manifest, logs, traces, filenames, or report. The sandbox endpoint compares it without reflecting either the configured or presented value. Controller calls and fault transitions produce metadata-only audit entries containing only run/scenario ULIDs, safe tenant/dataset locators, scenario/dependency token, action, UTC timestamp, and stable reason code.

## Closed scenario-to-mechanism matrix

| Scenario | Faulted boundary | Observed-fault proof | Recovery proof | Production-equivalence residual |
| --- | --- | --- | --- | --- |
| `eventstore-outage` | Aspire `eventstore` project resource, stopped and started by Tier-3 `ResourceCommandService`. | A previously healthy governed command through ChatBot/DAPR reaches the EventStore dependency failure and no unauthorized mutation is committed. | EventStore and ChatBot return healthy; the committed-before-outage set is reconstructed from state/audit evidence; a post-restore idempotent command succeeds; RPO/RTO and data-loss are derived from observed timestamps and sets. | Local single-process Aspire/DAPR and Redis are not AKS, durable production storage, or a multi-replica control plane. |
| `m365-subscription-failure` | Test-only topology composes a closed Graph/subscription simulator behind `GraphMailboxIntakeWorker`; the shipped AppHost has no sandbox reference. | The subscription is expired through the authenticated controller; the Worker returns its stable recoverable result and affected/control DAPR read-model sentinels remain unchanged. | Renewal drives the Worker through the generated ChatBot client; the EventStore owner-sidecar actor-state API proves the committed mailbox-intake aggregate, a lane-stable second notification proves coarse-idempotent no-duplicate behavior, both tenant sentinels remain unchanged, and all directly seeded test keys are erased. | The simulator proves the ChatBot/Worker contract and recovery path, not external Microsoft Graph behavior, throttling, permissions, webhook delivery, or tenant-scale fidelity. |
| `projection-rebuild` | Fresh DAPR validation partition selected by a file-loaded versioned dataset; the driver reads immutable `ProjectConversationSourceEmailView` metadata plus `IWormAuditStore.EnumerateChain` only. | A separately seeded persisted baseline is read back before rebuild; zero source or audit coverage and any of the six missing dataset categories are rejected. | Production projection stores write and read a distinct partition, duration/resources are recorded, and ETag cleanup erases the rebuilt partition. **The store round-trip is proven; the equivalence verdict is not** — the driver writes an identity copy of an already-projected view, no projection translator or handler is exercised, and the lane supplies one source-record instance to both sides, so `divergent` is currently unreachable. NFR57 equivalence is re-opened pending that rework. | Only source-email and WORM records are projection inputs; the other four parsed dataset categories establish provenance and are counted into `datasetVolume` but are never materialized, seeded or compared. WORM remains process-local and scale is non-production. |
| `graph` | The same hosted Worker-to-provider simulator boundary, including expired-subscription behavior. | The affected mailbox returns a recoverable Graph/subscription failure while the control tenant/mailbox operation stays available. | Restored intake reconciles once with no duplicate side effect and records mailbox scope. | No external Graph service or production webhook infrastructure. |
| `identity` | Allowlisted Aspire Keycloak `security` resource stop/start, with the token-acquisition/authentication boundary as the probe. | New authentication fails closed and cannot mutate the test tenant; a separately established control operation is checked according to the active token/JWKS-cache policy. | Keycloak health and token acquisition recover; a newly authenticated governed operation succeeds without broad fallback access. | Local Keycloak, development certificates, and single replica do not prove the production identity control plane or cache distribution. |
| `ai-provider` | **Re-opened.** A sandbox type shaped like the Server's `IAiAssistanceProvider`, registered as a concrete singleton and called directly; it is not resolved through the interface and `AcceptedCommandDispatcher` — the real consumer — is bypassed. | The sandbox type returns the retryable failure shape the exercise then asserts, and the independent ChatBot control operation remains available. | Restoration permits the same correlation to complete once. **The four NFR59 safety flags are structurally constant** — the faulted branch never records an effect and the unfaulted branch always records exactly one, over a correlation-id set both calls share, so unauthorized-mutation, silent-loss and duplicate-effect cannot be `true`, and nothing in the sandbox can write a second tenant. | Proves neither the product DI composition nor a production AI provider/model/quota/region outage; pending rework, it does not yet prove the adapter contract under fault either. |
| `command-execution` | Test-hosted `AcceptedCommandDispatcher` using a faultable `IEventStoreGatewayClient`. This is the one seam of the four that genuinely degrades ChatBot code. | The dispatcher reaches its EventStore dispatch plan and observes the injected client failure; unrelated ChatBot control remains available. | Restoration executes the same correlation twice with one recorded EventStore effect. **Safety flags re-opened:** the effect-set derivation is the same structurally-constant construct as the other sandbox-exercised dependencies. | Bypasses the HTTP gateway policy/audit pipeline; not a DAPR/AKS network partition or multi-service command-plane outage. |
| `audit-store` | **Re-opened.** A sandbox type shaped like the Server's `IAuditWriter`, called directly rather than through `ChatBotCommandAdmissionPipeline`'s fail-closed `RecordPreCommitAsync` gate. | The sandbox writer returns its unavailable/pre-commit outcome and records no effect; unrelated ChatBot control remains available. | Restoration accepts the same envelope once. **The row's own requirement — "governed mutation fails closed when required audit evidence is unavailable" — is not exercised:** no governed mutation is attempted during this scenario, and the only governed command in flight is the independent control, which is expected to succeed and does, because ChatBot's `ChainedAuditWriter` is never faulted. The same structurally-constant safety flags apply as for `ai-provider`. | Does not fault product DI, durable WORM storage, KMS, or the governed HTTP pipeline; pending rework it does not prove the fail-closed audit gate at all. |
| `attachment-processing` | **Re-opened.** A sandbox type shaped like `IMailboxAttachmentContentSource`, registered concretely and called directly; no ChatBot attachment path participates. | The sandbox source returns its retryable unavailable outcome with no content/effect; unrelated ChatBot control remains available. | Restoration makes the same metadata-only item available once. The same structurally-constant safety flags apply as for `ai-provider`, so "no duplicate or cross-tenant effect" is asserted rather than observed. | Does not execute the production attachment workflow, malware scanning, Folders storage, network, or large-file behavior. |

`graph` includes the subscription-expiry exercise; no seventh dependency token is introduced. Expected scopes continue to use the existing `ScopedOutageScopes` vocabulary.

**Re-opened by the round-4 review — two claims this section previously made do not hold for any dependency.** (1) *Observed scope (NFR58).* The sandbox monitor's `ScopeFor` is a byte-identical copy of the driver's `ExpectedScope` switch and feeds `ObservedScope` on all six paths, so `ExpectedScope == ObservedScope` unconditionally and the evaluator's `scope_escape` deviation is unreachable. Containment is asserted, not observed. (2) *Scope-recording latency (NFR41).* Both time bounds are minted inside the sandbox process one in-process channel hop apart; for `identity` and `graph` the single honest timestamp — taken immediately before the real token-acquisition failure — is overwritten by an on-demand stamp. The earlier carve-out claiming identity and Graph "do not claim product monitoring latency" was wrong: the driver computes and reports latency for all six, judged against the 5-minute NFR41 budget. Until real scope instrumentation exists, absence of a product monitoring observation is `unmeasurable`, not a sub-millisecond latency, and no NFR41 figure may be published.

## Timeouts and restoration

Per-scenario execution timeout, restoration timeout, cadence, evidence maximum age, and workflow timeout are validated together. The workflow timeout must exceed `RecoveryTargets.MaxRto` plus startup and cleanup margin if the lane is used to confirm the four-hour target. A shorter harness timeout can only yield `Unmeasurable`; it cannot demonstrate a target miss or pass.

**The lane's measurable recovery ceiling is 180 seconds, against a 4-hour RTO target.** The Tier-3 lane sets `RestorationTimeout = 3 minutes` — a deliberate trade, since a 4-hour restoration budget is impractical on every scheduled run — and each manifest publishes it as `MeasurableRecoveryCeilingSeconds`. Any genuine recovery between 3 minutes and 4 hours therefore converts to `unmeasurable`, never `missed`. The gate emits `{job}:{key}:target_exceeds_measurable_ceiling` as a non-blocking claim limitation on every passing run whose canonical target exceeds that ceiling. **A pass from this lane proves recovery within 180 seconds; it is not evidence for RTO ≤ 4 hours and must never be cited as such.** The same applies to NFR57, whose 4-hour rebuild target is measured inside the same bounded lane.

Every destructive operation resolves its exact allowlisted target before execution. Restoration runs in `finally`, even after cancellation, assertion failure, or coordinator exception. A failed start/restart command, unsuccessful `ExecuteCommandResult`, missed health transition, or post-restore end-state failure is retained in the manifest and is stop-ship.

**Cleanup outcome is not yet threaded (open).** The gate has a `{job}:cleanup_incomplete` branch and consumes a `cleanup-complete` assertion, but all three drivers currently emit that assertion as a literal `true` evaluated *before* cleanup runs, so the branch can only fire once `:unmeasurable` already has. Until the drivers thread their real cleanup result, a cleanup failure is stop-ship only via the exception path, not via the assertion.

## Dataset and tenant isolation

The validation tenant is dedicated and never aliases `tenant-alpha` or `tenant-beta`; those tenants remain independent controls. The guarded logical locator remains `replay-test:recovery-validation` and must pass `ReplayTenantPolicy`. EventStore's tenant grammar does not admit `:`, so the dedicated Keycloak identity binds only inside this test topology to the closed physical partition `recovery-validation`; the Tier-3 harness fixes the matching projection topic and records both names as configuration provenance. This transport alias cannot be configured by a caller and does not weaken the logical replay-tenant guard. Note precisely what enforces that: `ReplayTenantPolicy.StorageTenantFor` is a bare prefix strip, so it would happily derive `tenant-alpha` from a label `replay-test:tenant-alpha`. The alias cannot reach a control tenant because the topology hard-codes the single label `replay-test:recovery-validation`, not because the policy excludes the control names. Any future caller-influenced label must add that exclusion. `DistributedApplicationTestingBuilder` adds the recovery sandbox only in the test assembly; the shipped AppHost neither references nor composes it. The existing DAPR state store holds the isolated validation keys. The file-loaded baseline materializes non-zero immutable source metadata, WORM records, governed commands, approvals, policy snapshots, and attachment metadata; version, schema, and exact count must match before injection.

The rebuild driver receives narrow source/evidence interfaces rather than a general service provider. Architecture tests prevent Graph/mailbox re-ingestion, Party/Folder current-state reads, sibling-tenant access, or writes to the baseline partition.

## Evidence retention and release gate

The canonical reports remain the verdict authority. A metadata-only manifest adds run/scenario ULIDs, UTC bounds, repository/topology/configuration versions, safe tenant and dataset locators, dataset volume, driver mode, fault/restore/cleanup actions, expected/observed scope, measurements and targets, verdict dimensions, assertions, coverage, deviations, durable residual identifiers, and raw-artifact locators.

The required lanes use `if: always()` artifact upload and an explicit 30-day retention policy. They upload the raw `.trx` plus generated reports/manifests under `TestResults` on pass or failure. State-store assertions are captured in those report verdicts; the lane does not currently claim separate logs, traces, metrics, or state snapshots. Local ignored `TestResults` files are diagnostic only and are not repository-retained evidence.

Evidence older than `MaximumEvidenceAge` (8 days, matching the 7-day cadence) is rejected as `stale_result`. Note the asymmetry with the 30-day artifact retention: an artifact aged 8–30 days is still downloadable but is no longer citable for ratification.

The gate fails closed on disabled required validation, never-run/incomplete/stale/future evidence, a newer incomplete attempt, exception or timeout failure, `Unmeasurable`, divergence, containment breach, unalerted serious breach, zero population, zero scenario coverage, missing dataset/scenario provenance, or missing artifact locators. Cleanup failure is covered by the exception path only — see "Timeouts and restoration". Measurable RPO/RTO misses, rebuild-duration misses, and late scope recording remain distinct target deviations and are governed by their A10/NFR decision; the gate never relabels them as met/equivalent/contained.

Reports, logs, traces, screenshots, filenames, and CI artifacts exclude message bodies, attachment content, tokens, secrets, credentials, raw claims, and other tenant payloads. Metadata is passed through existing sanitization conventions and stable reason tokens.

## Sandbox suitability and residuals

The accepted implementation can prove the exact Aspire 13.4.6/DAPR sandbox, provider simulators, dataset, scale, and single-replica configuration named in its evidence. The following remain durable production-equivalence residuals until separately closed with authentic evidence:

- `RV-EXT-M365`: no live Graph/M365 resource; external Graph permissions, webhook, throttling, and service behavior remain unproven.
- `RV-DURABLE-WORM`: process-local `InMemoryWormAuditStore`; durable WORM/KMS/storage failure and recovery remain unproven.
- `RV-PROD-CONTROL`: no production AKS or multi-replica control plane; scheduler lease, replica coordination, and platform resource control remain unproven.
- `RV-PROVIDER-SCALE`: local provider/application-boundary simulators and the versioned baseline do not prove production traffic volume, latency, replicas, or regional failure modes. Four of the baseline's six categories are parsed for provenance only and are never materialized, seeded or compared.
- `RV-MEASURABLE-CEILING`: the lane's 180-second restoration budget is two orders of magnitude below the 4-hour RTO/NFR57 targets, so no run from it can demonstrate a miss of either; recovery between 3 minutes and 4 hours converts to `unmeasurable`.

The absence of a hosted Workers resource is a baseline gap this story must close in the opted-in recovery topology. The residual is the difference between that controlled Worker/provider composition and an external production M365 deployment, not permission to omit the mandatory subscription drill.

## A10 governance

Story 12.15 leaves A10/NFR56 provisional at `RecoveryTargets.MaxRpo` (15 minutes) and `RecoveryTargets.MaxRto` (4 hours), with no runtime constant change.

**No measured figure is published from this story.** A local pre-remediation diagnostic run did pass the evidence gate *as that gate existed at v1.1*, but its manifests predate the current manifest contract — they carry no `MeasurableRecoveryCeilingSeconds`, which the shipped gate requires — so the bundle cannot be replayed and is not citable evidence. Its run identifier and measurements have been withdrawn from the PRD, decision log, addendum and predecessor ADRs rather than restated with caveats.

Ratification requires a complete passing scheduled-CI or release artifact plus its hosted run/artifact locator recorded in the PRD decision log, **and** is bounded by the 180-second measurable ceiling above: a passing hosted run ratifies recovery within the ceiling, not RTO ≤ 4 hours. Ratifying the 4-hour target requires either a lane whose restoration budget reaches it or a separately evidenced pre-production drill.

**Un-ratification.** A ratified A10 returns to provisional when the cited artifact passes the 30-day retention boundary without a successor, when a later required run is not passing, or when a hosted run returns `Unmeasurable` or a structural breach. Architecture/DevOps owns that transition and records it in the decision log.

Even a passing sandbox run cannot close `RV-EXT-M365`, `RV-DURABLE-WORM`, `RV-PROD-CONTROL`, `RV-PROVIDER-SCALE`, or `RV-MEASURABLE-CEILING`. Tightening or loosening still requires the PRD decision, this ADR, and `RecoveryTargets` to change together with retained evidence, rationale, and approval.

## Consequences

- Ordinary deployments remain non-destructive and retain inert deferred live-driver defaults.
- The destructive scheduler is the serialized Tier-3/release workflow, never `PeriodicEnforcementBackgroundService` and never a second product hosted service.
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
