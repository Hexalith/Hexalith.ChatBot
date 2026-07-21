---
baseline_commit: eaee04f7943f4cf24a3d791a6d1abae374133023
---

# Story 12.14: Wire the M2 audit and recovery runtime scheduler

Status: in-progress

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operations owner,
I want the WORM chain verifier, audit-completeness measurer, and replay/derived-store isolation probes to run automatically on a durable schedule,
so that tamper-evidence, audit completeness, and isolation guarantees are continuously enforced in production rather than only provable on manual invocation.

## Acceptance Criteria

*(Verbatim from `epics.md` §Story 12.14, lines 3161-3180. Numbered here for task traceability.)*

1. **Given** the durable control-plane runtime already delivered by canonical Story 9.1 (Runtime Governance Control Plane), **when** the M2 runtime scheduler starts, **then** it invokes, on documented cadences: Story 12.1's `WormAuditChainVerifier` via `AuditChainVerificationCoordinator` (nightly), Story 12.2's `AuditCompletenessMeasurer` (rolling 7-day), Story 12.4's replay-isolation probe (`ReplayIsolationProbeCoordinator`) and Story 12.5's `DerivedStoreIsolationProbeCoordinator` (both nightly M2 release gates), and Story 12.6's correction-propagation SLO-deadline sweep — each tenant-scoped, idempotent, and observable (NFR13, NFR19).

2. **Given** scheduler failure or partial execution, **when** a cadence is missed, **then** the miss itself is observable (metric/log) and does not silently suppress the fail-closed alert paths already built in each coordinator (NFR7, NFR15a).

3. **Given** this story lands, **then** no Epic 12 story's Completion Notes may describe its runtime trigger as "deferred" any longer — the constructed coordinators become genuinely live, and the M2 release gates (12.4, 12.5) block release on a real (not merely provable) breach signal.

### ⚠️ Scope reconciliation you MUST read before starting (AC1 is not five equal "activate" tasks)

Analysis of the actual codebase shows the five AC1 items are in three different states. **Do not assume "pure activation" applies uniformly — it does not.**

| AC1 coordinator | Pre-built sweep entry point | State today | Your task |
| --- | --- | --- | --- |
| 12.2 `AuditCompletenessMeasurer` / `AuditCompletenessAlertCoordinator.MeasureAllTenantsAndAlertAsync` | ✅ exists | **ALREADY WIRED & LIVE** in `PeriodicEnforcementCoordinator.RunOnceAsync` (the `audit-completeness` evaluator, `PeriodicEnforcementRuntime.cs:364-373`) | Keep as-is. Do **not** duplicate it. Verify it still runs; add a nightly cadence gate only if you also gate the others (see below) — otherwise leave it on the existing per-tick cadence. |
| 12.1 `AuditChainVerificationCoordinator.VerifyAllTenantsAsync` | ✅ exists | Registered singleton (`CommandGatewayServiceCollectionExtensions.cs:180`), **never invoked by any scheduler** (test-only call sites) | **Activate** — add as a nightly-gated evaluator. |
| 12.4 `ReplayIsolationProbeCoordinator.SweepAllProductionTenantsAsync` | ✅ exists | Registered singleton (`…Extensions.cs:191`), **never invoked by any scheduler** | **Activate** — nightly-gated evaluator + M2 release-gate signal. |
| 12.5 `DerivedStoreIsolationProbeCoordinator.SweepAllTenantPairsAsync` | ✅ exists | Registered singleton (`…Extensions.cs:196`), **never invoked by any scheduler** | **Activate** — nightly-gated evaluator + M2 release-gate signal. |
| 12.6 correction-propagation SLO-deadline **sweep** | ❌ **DOES NOT EXIST** | No `Sweep`/`ScanDeadlines` method exists on any correction-propagation coordinator, and there is **no seam to enumerate in-flight/incomplete propagations**. The SLO is enforced **inline, synchronously**: `DaprCorrectionPropagationCoordinator.StartAsync` sets the deadline (`CorrectionPropagationSlo.DeadlineFor`) and `InMemoryVectorReindexer` checks `IsBreached` within the same call (`InMemoryVectorReindexer.cs:44,96,107`). | **Do NOT invent a new coordinator.** See the correction-propagation subsection in Dev Notes. Default: record it as an explicit residual coupled to the deferred async reindex runtime (Story 12.16), because a *periodic* sweep only has in-flight work to scan once reindex is asynchronous. This preserves the epic's "pure activation — no new coordinator logic" framing (`epics.md:3163`) and AC3's no-fabrication requirement. Confirm this interpretation with the story author before building any new sweep. |

**Net new work in this story = activate 3 audit coordinators (WORM verify, replay-isolation, derived-store-isolation) on a nightly cadence inside the existing periodic runtime, add per-job missed-cadence observability, retire the "deferred scheduler" language in Stories 9.1/9.4/9.5, and honestly document the completeness (already-live) and correction-propagation (inline/residual) states.** It is NOT "build a second BackgroundService" and NOT "build a correction-propagation sweep."

## Tasks / Subtasks

- [x] **Task 1 — Extend the existing periodic runtime to run the three M2 audit/isolation sweeps on a nightly cadence (AC1)**
  - [x] Inject `AuditChainVerificationCoordinator`, `ReplayIsolationProbeCoordinator`, and `DerivedStoreIsolationProbeCoordinator` into `PeriodicEnforcementCoordinator` (constructor, `PeriodicEnforcementRuntime.cs:247-263`). They are already registered singletons — no new DI registration for the coordinators themselves.
  - [x] Invoke each sweep **once per pass, after the per-tenant loop** — the same placement as today's `audit-completeness` / `audit-projection-lag` evaluators (`PeriodicEnforcementRuntime.cs:364-381`). These coordinators **self-enumerate tenants** (they do not consume the runtime's per-tenant list), so they are per-run, not per-tenant iterations:
    - `worm-audit-chain` → `VerifyAllTenantsAsync(correlationId, ct)` → `AuditChainVerificationOutcome(TenantsChecked, Breaches, Alerted)`
    - `replay-isolation-probe` → `SweepAllProductionTenantsAsync(correlationId, ct)` → `ReplayIsolationProbeOutcome(TenantsSwept, Breaches, Alerted)`
    - `derived-store-isolation-probe` → `SweepAllTenantPairsAsync(correlationId, ct)` → `DerivedStoreIsolationProbeOutcome(PartitionsProbed, Breaches, Alerted)`
  - [x] Wrap each in the existing `RunEvaluatorAsync("<name>", …)` fail-isolation wrapper (`PeriodicEnforcementRuntime.cs:509-525`) so one throwing sweep records a per-name failure and increments the pass's `EvaluatorsFailed` count without aborting the other sweeps or the enforcement pass.
  - [x] **Nightly cadence gate.** The runtime ticks at `Options.Cadence` (default 1 min). Running a nightly sweep every tick would re-emit the same breach alerts each minute. Add a once-per-UTC-day guard for each of the three sweeps, modeled exactly on the once-per-ISO-week runbook guard (`_lastRunbookWeekByTenant` + `WeeklyPartitionKey`, `PeriodicEnforcementRuntime.cs:265,468-489,552-557`). Because these sweeps are process-global (not per-tenant), key the guard by `"{jobName}:{yyyyMMdd}"` (UTC day), not by tenant. A process restart harmlessly lets that day's sweep run once more (same as the runbook guard's documented restart behavior).
  - [x] Make the nightly cadence configurable and independently toggleable on `PeriodicEnforcementOptions` (`PeriodicEnforcementRuntime.cs:17-28`) — e.g. `bool RunM2AuditRecoverySweeps` (default keeps current behavior) and a `TimeSpan M2SweepCadence` / day-anchor if a finer contract than "once per UTC day" is required. Bind from the existing `ChatBot:PeriodicEnforcement` config section (`Program.cs:33`). **Do not** hardcode `TimeSpan.FromHours(24)` inline in the coordinator (define-once discipline — see the `CorrectionPropagationSlo` "one source" lesson).
- [x] **Task 2 — Missed-cadence observability for the M2 sweeps (AC2)**
  - [x] Track per-sweep last-run/last-success timestamps in `IPeriodicEnforcementStatusStore` (extend `PeriodicEnforcementRunStatus` + the in-memory store, `PeriodicEnforcementRuntime.cs:131-245`), or add a parallel per-job status record. The existing per-evaluator failure map (`EvaluatorFailureCounts`) already captures throws; add positive last-ran evidence so a *missed* (never-ran / stale) nightly sweep is observable, not just a failed one.
  - [x] Extend `CheckHealthAsync` (`PeriodicEnforcementRuntime.cs:415-430`) — or add an analogous check — to emit an operator alert when an enabled nightly sweep has not completed within its cadence + `MissedCadenceAlertAfter` budget. Reuse `EmitSchedulerAlertAsync` / `IOperatorAlertSink` with `OperatorAlertKind.DependencyDegraded` and a distinct reason code per sweep (e.g. `m2_worm_verify_missed_cadence`).
  - [x] **Critical (AC2):** the miss signal is additive. It must **not** suppress or short-circuit the fail-closed breach/alert path already inside each coordinator (each writes a pre-commit audit envelope then emits exactly one `IOperatorAlertSink` breach alert). A missed cadence and a detected breach are independent signals — verify both survive together.
- [x] **Task 3 — Retire the "deferred scheduler" language in the now-activated stories (AC3)**
  - [x] Update the Completion Notes / "Deferral confirmed" sections of the canonical story files so they no longer read as "deferred," and instead state the trigger is now wired by Story 12.14:
    - `9-1-tamper-evident-worm-audit-chain.md` (lines ~173-175, 252, 254-256 — "Deferral confirmed", "No always-on `BackgroundService`/Dapr-timer scheduler is wired")
    - `9-4-replay-and-simulation-isolation.md` (lines ~178-183 — "Explicit deferrals … periodic scheduler/trigger")
    - `9-5-derived-store-cross-tenant-isolation.md` (Completion Notes deferral of the periodic scheduler)
  - [x] Update the class-comment "deferred trigger" notes in the coordinator sources if they still say a scheduler is not wired: `AuditChainVerificationCoordinator.cs` (class comment ~lines 20-24), `ReplayIsolationProbeCoordinator.cs`, `DerivedStoreIsolationProbeCoordinator.cs`. Keep the class comments truthful about what IS and ISN'T wired after this story.
  - [x] **Do NOT** rewrite `9-2` (completeness was already live via the 8.7b runtime — say so) and **do NOT** claim the `9-6` correction-propagation *periodic SLO sweep* is now wired (it is not — see Dev Notes). Record 9.6's periodic-sweep residual as carried to Story 12.16, not closed.
  - [x] Confirm the M2 release-gate intent (AC3): the replay-isolation and derived-store-isolation sweeps' `…Outcome.Breaches == 0` is the real, running stop-ship signal for M2 release — not merely provable by a manual test call. Document where that gate is asserted (release-readiness check / existing gate test).
- [x] **Task 4 — Tests (mirror existing conventions; no new test infra)**
  - [x] Extend `PeriodicEnforcementCoordinatorTests` (`tests/Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/PeriodicEnforcementCoordinatorTests.cs`) — xUnit `[Fact]`, Shouldly, `MutableClock`, in-memory stores, `TestContext.Current.CancellationToken`:
    - A pass with a seeded broken WORM chain / replay marker leak / cross-tenant derived-store read produces the coordinator's breach alert **and** counts the sweep in the run outcome.
    - The nightly cadence gate: two ticks within the same UTC day run each sweep **once**; a tick on the next UTC day runs it again (assert against a `MutableClock` you advance).
    - A throwing sweep is fail-isolated (recorded in the failure map, other sweeps + the enforcement pass still complete) — mirror the existing `RunEvaluatorAsync` failure test.
    - Missed-cadence: advance the clock past the budget with no sweep having run ⇒ `CheckHealthAsync` emits the per-sweep missed-cadence alert; a detected breach in the same pass still emits its own breach alert (AC2 independence).
  - [x] Extend `PeriodicEnforcementDependencyInjectionTests` for the new option flag + that the three coordinators resolve into the runtime.
  - [x] Run the narrow suite: `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj` (this project also holds `AuditChainVerificationCoordinatorTests`, `ReplayIsolationProbeCoordinatorTests`, `DerivedStoreIsolationProbeCoordinatorTests` — keep them green). Then the Architecture fitness suite: `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/…`.
- [x] **Task 5 — Primary-path / live evidence (per Epic 13 open action items — required, not optional, for runtime claims)**
  - [x] The periodic runtime is only hosted when `ChatBot:UsePeriodicEnforcementRuntime=true`. It is set in the Aspire topology (`AppHost/Program.cs:39`, `ChatBot__UsePeriodicEnforcementRuntime=true`). Provide direct evidence the scheduler actually invokes the M2 sweeps in the live/hosted path (a Tier-3 Aspire run log line, or an integration test that starts the hosted `PeriodicEnforcementBackgroundService` and observes a sweep run) — a unit test that calls `RunOnceAsync` directly is necessary but **not sufficient** for the "genuinely live" claim in AC3. See the sprint-status Epic 13 action item: "Make primary-path evidence mandatory for … hosting … runtime claims; keep fallbacks diagnostic only."
  - [x] State explicitly in Completion Notes what was proven in the hosted path vs. by direct-invocation unit test.

## Dev Notes

### The single most important framing

There is exactly **one** periodic runtime in this system, and you are extending it — not adding a second one. architecture.md:233 says it plainly: "**one periodic enforcement runtime** drives the canonical 8.2–8.7 notification/escalation evaluators, 11.4 alert coordinator, 11.5 runbook sampler, **audit-completeness publication**, audit-projection-lag publication, and control-state freshness heartbeats." That runtime is canonical Story 9.1 (legacy 8.7a/8.7b). Story 12.14 adds the WORM-verify + replay-isolation + derived-store-isolation sweeps into it. **Do not introduce a new `BackgroundService`.**

### The runtime you are extending (read these before touching anything)

Everything lives in **one file**: `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs` (625 lines — options → coordinator → status store → background service → DI, all here).

- **Trigger:** `PeriodicEnforcementBackgroundService : BackgroundService` (lines 574-595) drives a `System.Threading.PeriodicTimer(Options.Cadence)`. Each tick calls `coordinator.RunOnceAsync(correlationId, ct)` then `coordinator.CheckHealthAsync(correlationId, ct)`. It returns immediately (inert) unless `Options.UsePeriodicEnforcementRuntime` is true (line 581). **This is the canonical pattern — a `BackgroundService`+`PeriodicTimer` loop calling coordinator sweep methods. There are NO Dapr reminders/timers/cron anywhere in `src/`** — do not add one.
- **Unit of work:** `PeriodicEnforcementCoordinator.RunOnceAsync` (lines 270-413): whole-run overlap guard (`Interlocked.Exchange(ref _running, 1)`, lines 277-289, reset in `finally` 409-412) → `statusStore.RecordStarted` → enumerate tenants → per-tenant evaluator loop (escalation / notification-throttle / reviewer-backlog / approval-rubber-stamp / operational-alerts / weekly runbook sampler, lines 307-349) → **then** two per-run evaluators after the loop: `audit-completeness` (lines 364-373) and `audit-projection-lag` (lines 375-381) → record success/failure. **Your three M2 sweeps go where `audit-completeness` is — per-run, after the tenant loop.**
- **Fail-isolation seam:** `RunEvaluatorAsync(string name, Func<Task>)` (lines 509-525): runs the action, returns 0 on success, and on a non-cancellation throw calls `statusStore.RecordEvaluatorFailure(name)` and returns 1. This is the ONLY plug-in mechanism — **there is no `IJob`/`IEnforcement` interface**; coordinators are hand-wired by name into `RunOnceAsync`. Wire each M2 sweep the same way.
- **Sub-cadence guard (copy this pattern for "nightly"):** the NFR44 runbook sweep runs at most once per ISO week per tenant even though the pass ticks every minute — via `_lastRunbookWeekByTenant` (`ConcurrentDictionary<string,string>`, line 265) keyed by `WeeklyPartitionKey(tenant, now)` = `"{tenant}:{year}:W{week}"` (lines 468-489, 552-557). The doc comment at 477-483 explains the design. For nightly, key by `"{jobName}:{yyyyMMdd}"` (UTC day) since the sweeps are process-global.
- **Observability:** `IPeriodicEnforcementStatusStore` / `InMemoryPeriodicEnforcementStatusStore` (lines 142-245) records started/succeeded/failed/overlap/evaluator-failure/runbook-sweep and exposes `PeriodicEnforcementRunStatus`. `CheckHealthAsync` (415-430) emits `periodic_enforcement_missed_cadence` / `periodic_enforcement_stalled` via `EmitSchedulerAlertAsync` → `OperatorAlert(OperatorAlertKind.DependencyDegraded, reason, "system", "PeriodicEnforcementRuntime", correlationId, …)` (527-539). The coordinator status is already surfaced on a health endpoint (`ChatBotCompatibilityEndpointExtensions.cs:48` returns `coordinator.Status`) — consider surfacing M2 sweep status there too (optional).
- **Correlation id:** per-tick, time-based `"periodic-enforcement:{yyyyMMddHHmmss}"` (line 589), threaded to every coordinator and alert.

### The coordinators you are activating (exact signatures — all in `src/Hexalith.ChatBot.Server/Audit/`)

All three share a uniform contract: `ValueTask<…Outcome> <Sweep>(string runCorrelationId, CancellationToken)`, they `ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId)`, they **self-enumerate tenants**, and on breach they write a pre-commit audit envelope then emit exactly one `IOperatorAlertSink` alert; a throwing enumeration ⇒ an `Unknown`/unmeasurable breach signal (never fabricated "clean").

| Coordinator (registration) | Sweep method | Outcome record | Story |
| --- | --- | --- | --- |
| `AuditChainVerificationCoordinator` (`…Extensions.cs:180`) | `VerifyAllTenantsAsync` (`AuditChainVerificationCoordinator.cs:105`) | `AuditChainVerificationOutcome(TenantsChecked, Breaches, Alerted)` | 12.1 / 9.1 |
| `ReplayIsolationProbeCoordinator` (`…Extensions.cs:191`) | `SweepAllProductionTenantsAsync` (`ReplayIsolationProbeCoordinator.cs:117`) | `ReplayIsolationProbeOutcome(TenantsSwept, Breaches, Alerted)` | 12.4 / 9.4 |
| `DerivedStoreIsolationProbeCoordinator` (`…Extensions.cs:196`) | `SweepAllTenantPairsAsync` (`DerivedStoreIsolationProbeCoordinator.cs:167`) | `DerivedStoreIsolationProbeOutcome(PartitionsProbed, Breaches, Alerted)` | 12.5 / 9.5 |

Already-wired (do not duplicate): `AuditCompletenessAlertCoordinator.MeasureAllTenantsAndAlertAsync` (`AuditCompletenessAlertCoordinator.cs:97`) → `AuditCompletenessSweepOutcome(TenantsMeasured, Breaches, Unmeasurable)`, called at `PeriodicEnforcementRuntime.cs:364-373`. The DI comment at `…Extensions.cs:184-185` already documents this: "Story 8.7b's periodic enforcement runtime calls MeasureAllTenantsAndAlertAsync and publishes the measured sweep into IAuditCompletenessSource when the production flag is enabled."

### Correction-propagation SLO-deadline sweep (AC1 item 5) — the honest treatment

**There is no pre-built sweep to activate, and building one is new coordinator logic that the epic explicitly excludes.** Facts, verified in source:
- `ICorrectionPropagationCoordinator` exposes only `IsReady` and `StartAsync(request, ct)` — no sweep/scan method.
- `CorrectionPropagationSlo` (`CorrectionPropagationSlo.cs`) is a static deadline calculator: `DeadlineFor(scope, startedAtUtc)` and `IsBreached(deadline, now)`. Its only callers set/check the deadline **inline**: `DaprCorrectionPropagationCoordinator.StartAsync` (`.cs:38`, sets `EstimatedCompletionAtUtc`) and `InMemoryVectorReindexer` (`.cs:44,96,107`, checks `SloBreached` synchronously during the reindex).
- There is **no store seam that enumerates in-flight / incomplete correction propagations** for a periodic scanner to iterate.
- Story 9.6 itself defers **both** "the async/long-running reindex runtime (the in-memory reindex is synchronous)" **and** "the periodic SLO-deadline sweep trigger" (`9-6-correction-driven-vector-reindexing.md:31,182`). These are coupled: a *periodic* sweep only has in-flight work to catch **once the reindex is asynchronous**. With today's synchronous in-memory reindex, every correction completes inside its own call and the inline `IsBreached` + `correction-delayed`/P2-delay path already enforces the SLO end-to-end — a periodic sweep would scan an empty set.

**Recommended default (aligns with `epics.md:3163` "pure activation — no new coordinator logic" and AC3's no-fabrication rule):** do not build a new correction-propagation sweep in 12.14. In Completion Notes, state that correction-propagation SLO enforcement is already live inline (synchronous), and that the *periodic* SLO-deadline sweep is coupled to the deferred async reindex runtime and is carried as an explicit residual to **Story 12.16** (which binds the live Memories store and re-opens the async-reindex question). Do **not** silently mark it closed, and do **not** claim AC1 item 5 is "wired." Flag this reconciliation for the story author (see the open question this story was created with) — if they want a real in-flight sweep built now, that is added scope (a new enumeration seam + scan coordinator), not activation.

### Anti-patterns to avoid (these are the exact traps prior Epic 12 reviews caught)

- **Do not add a second `BackgroundService`/Dapr timer.** One runtime only (architecture.md:233).
- **Do not run nightly sweeps every tick.** Gate them (once-per-UTC-day), or you re-emit the same breach alert every minute — the runbook guard exists precisely because of this.
- **Do not invent a correction-propagation sweep** to satisfy AC1 literally. That contradicts "pure activation," fabricates a control, and scans state that isn't tracked.
- **Do not let a missed cadence suppress a real breach alert** (AC2). The two signals are independent; test them together.
- **Do not claim "genuinely live" from a `RunOnceAsync` unit test alone.** Provide hosted/primary-path evidence (Task 5) — the Epic 13 action items make this mandatory for hosting/runtime claims, and prior stories were dinged for fabricated/insufficient runtime evidence.
- **Define cadence once** (options/config), never a second inlined `TimeSpan.FromHours(24)` — the `CorrectionPropagationSlo` define-once lesson.
- **The coordinators self-enumerate tenants.** Do not try to pass them the runtime's per-tenant list or loop them inside the per-tenant loop — invoke once per pass, like `audit-completeness`.

### Project Structure Notes

- **Primary edit:** `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs` — constructor injection (247-263), the three sweep invocations after the tenant loop (near 364-381), the nightly guard (new, modeled on 265/468-489), options (17-28), status/health (131-245, 415-430).
- **DI:** the three coordinators are **already** registered singletons in `AddChatBotCommandGateway` (`src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs:180,191,196`); `AddChatBotPeriodicEnforcement()` is called at line 134. No new coordinator registration needed — the runtime just consumes them. Add any new option binding in `Program.cs` (existing block 33/43-48).
- **Hosting gate:** `src/Hexalith.ChatBot.Server/Program.cs:43-48` (conditional `AddChatBotPeriodicEnforcementHostedService()`); enabled in `src/Hexalith.ChatBot.AppHost/Program.cs:39` for the Tier-3 topology.
- **Tests:** `tests/Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/` (`PeriodicEnforcementCoordinatorTests.cs`, `PeriodicEnforcementDependencyInjectionTests.cs`) and `tests/Hexalith.ChatBot.Server.Tests/Audit/` (the three coordinator test files). Same project, same conventions.
- **Story-doc edits (AC3):** `_bmad-output/implementation-artifacts/9-1-…md`, `9-4-…md`, `9-5-…md` Completion Notes; coordinator class comments in the three `Audit/*.cs` files.
- **Solution:** builds via `Hexalith.ChatBot.slnx` (super-repo inits root submodules non-recursively; do not touch inner `*.slnx`). No new project or package reference is needed — this is internal to `Hexalith.ChatBot.Server`.
- **No user-facing / UI / localization / accessibility impact** (sprint-change-proposal §3.3). Internal/operational only.

### Testing standards summary

- xUnit v3 (`[Fact]`, `TestContext.Current.CancellationToken`) + Shouldly (`ShouldBe`), deterministic time via `MutableClock`/`FixedClock`, in-memory stores (`InMemoryWormAuditStore`, `InMemoryGovernedControlStateProjectionStore`, `InMemoryOperatorAlertSink`, etc.). No bUnit, no new mocking framework.
- Keep the existing Server, Architecture, Conformance, Workers suites green. Report exact `dotnet test` counts and any failure verbatim in Completion Notes (Epic 13 action item: mechanical evidence integrity — File List, scoped diff, test counts, primary-path execution must reconcile before `done`).

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.14` (lines 3161-3180) — AC verbatim; "pure activation — no new coordinator logic" (3163)]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-20-epic12-recovery-deferrals.md` §4 — 12.14 = "pure activation of already-built coordinators from 12.1, 12.2, 12.4, 12.5, 12.6, reusing the control-plane runtime already delivered by canonical Story 9.1 / legacy 8.7a/8.7b"]
- [Source: `_bmad-output/planning-artifacts/architecture.md:233` — "one periodic enforcement runtime drives … audit-completeness publication …"; :77-92 recovery/idempotency NFRs; :319,343,362,473-476 Memories/M2 framing]
- [Source: `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs` — the runtime being extended (BackgroundService 574-595; RunOnceAsync 270-413; RunEvaluatorAsync 509-525; runbook nightly-guard analogue 468-489,552-557; options 17-28; status/health 131-245,415-430)]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs:134,180-224` — coordinator DI registrations + the audit-completeness "runtime calls … when the production flag is enabled" comment (184-185)]
- [Source: `src/Hexalith.ChatBot.Server/Program.cs:33,43-48` + `src/Hexalith.ChatBot.AppHost/Program.cs:39` — hosting gate + Tier-3 enablement]
- [Source: `src/Hexalith.ChatBot.Server/Audit/AuditChainVerificationCoordinator.cs:105`, `ReplayIsolationProbeCoordinator.cs:117`, `DerivedStoreIsolationProbeCoordinator.cs:167`, `AuditCompletenessAlertCoordinator.cs:97` — sweep signatures/outcomes]
- [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationSlo.cs`; `DaprCorrectionPropagationCoordinator.cs:38`; `src/Hexalith.ChatBot.Server/Projections/DerivedStores/InMemoryVectorReindexer.cs:44,96,107` — correction-propagation SLO is inline/synchronous, no sweep seam]
- [Source: `_bmad-output/implementation-artifacts/9-1-tamper-evident-worm-audit-chain.md:173-175,254-256`; `9-4-replay-and-simulation-isolation.md:178-183`; `9-5-derived-store-cross-tenant-isolation.md`; `9-6-correction-driven-vector-reindexing.md:31,182` — the "deferred scheduler" language AC3 must retire, and 9.6's coupled async-reindex + periodic-sweep deferral]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml:189-217` — Epic 13 action items making primary-path/hosting evidence + mechanical evidence-integrity mandatory]
- [Related follow-on: Story 12.15 (live fault-injection drivers + A10) and Story 12.16 (live Hexalith.Memories binding + async reindex — the correct home for the correction-propagation periodic SLO sweep residual)]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

- 2026-07-21 — Task 1 RED: Server test compile failed on the intentionally absent M2 options, outcomes, and coordinator injections.
- 2026-07-21 — Task 1 GREEN: Server suite passed 1,691/1,691. Broad independent-project sweep reached UI E2E with one pre-existing forced-colors failure (`AssociationReviewShouldPreserveForcedColorsReducedMotionAndBlockedRedactionStates`, expected `borderStyle=solid`, observed `none`); focused rerun reproduced it and the scoped diff contains no UI/E2E changes.
- 2026-07-21 — Task 2 RED: Server test compile failed because the intentionally new `M2SweepStatuses` contract was not yet present.
- 2026-07-21 — Task 2 GREEN: Server suite passed 1,692/1,692 with per-sweep run/success evidence and three independent missed-cadence reason codes.
- 2026-07-21 — Tasks 3–5 GREEN: Server suite passed 1,696/1,696 and Architecture passed 63/63. The hosted-service integration test observed all three enabled sweeps without directly calling `RunOnceAsync`.
- 2026-07-21 — Tier-3 Aspire evidence: `chatbot` reached Running/Healthy with both periodic-runtime flags effective; `/health/chatbot/periodic-enforcement` reported one successful hosted run for `worm-audit-chain`, `replay-isolation-probe`, and `derived-store-isolation-probe`, each with zero breaches. The topology was stopped cleanly afterward.
- 2026-07-21 — Full Debug solution restore/build succeeded with 0 warnings and 0 errors. Eleven test projects were green. Two unrelated broad gates remain: UI E2E 140/141 repeats the pre-existing forced-colors `borderStyle` failure; Integration 59 passed/3 skipped/1 failed in suite mode because the sibling-failure aggregation observed 1 then 2 exceptions instead of 3, while the focused test passed 1/1. Neither project is in the scoped diff.

### Completion Notes List

- Task 1: Extended the single periodic enforcement runtime with independently gated WORM, replay-isolation, and derived-store-isolation sweeps; exposed their typed pass outcomes; kept the option disabled by default; and enabled it explicitly in the Aspire topology through `ChatBot:PeriodicEnforcement` binding.
- Task 2: Added metadata-only per-sweep status (`last ran`, `last succeeded`, breach count, correlation) to the existing health payload and distinct cadence-budget alerts for WORM, replay isolation, and derived-store isolation.
- Task 3: Retired scheduler-deferral claims from the Story 9.1/9.4/9.5 completion records and coordinator comments. Audit completeness remains the already-live evaluator. Correction propagation remains live inline/synchronous; its periodic SLO-deadline scan is explicitly residual to Story 12.16 with the asynchronous reindex runtime, and Story 9.6 was not rewritten.
- Task 4: Added deterministic stop-ship, cadence, failure-isolation, additive breach-plus-miss, option/DI, and hosted-service coverage. `RunOnceAsyncShouldRunM2SweepsOncePerUtcDayAndExposeTheirOutcomes` asserts the clean `Breaches == 0` release condition; `RunOnceAsyncShouldSurfaceEveryM2BreachAsAStopShipOutcomeAndAlert` asserts non-zero outcomes are observable stop-ship signals.
- Task 5 hosted path: the hosted-service test and a live Aspire run proved the real `PeriodicEnforcementBackgroundService` invoked all three M2 sweeps and exposed their successful status. Direct-invocation unit tests separately prove seeded breach counts/alerts, once-per-day gating, per-evaluator fail isolation, and missed-cadence independence.
- Review handoff is intentionally blocked: Story and sprint status remain `in-progress` until the unrelated UI E2E forced-colors baseline and integration sibling-aggregation flake are resolved or explicitly waived; no out-of-scope UI/integration changes were made.

### File List

- _bmad-output/implementation-artifacts/12-14-wire-the-m2-audit-and-recovery-runtime-scheduler.md
- _bmad-output/implementation-artifacts/9-1-tamper-evident-worm-audit-chain.md
- _bmad-output/implementation-artifacts/9-4-replay-and-simulation-isolation.md
- _bmad-output/implementation-artifacts/9-5-derived-store-cross-tenant-isolation.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.AppHost/Program.cs
- src/Hexalith.ChatBot.Server/Audit/AuditChainVerificationCoordinator.cs
- src/Hexalith.ChatBot.Server/Audit/DerivedStoreIsolationProbeCoordinator.cs
- src/Hexalith.ChatBot.Server/Audit/ReplayIsolationProbeCoordinator.cs
- src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/PeriodicEnforcementCoordinatorTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/PeriodicEnforcementDependencyInjectionTests.cs

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-07-21 | 0.1 | Implemented Story 12.14: activated the three nightly M2 audit/isolation sweeps in the existing periodic runtime, added per-sweep status and missed-cadence alerts, reconciled prior deferrals, and proved direct plus hosted/Aspire execution. Scoped gates pass; review transition remains blocked by two unrelated broad-suite failures recorded above. | Codex |
