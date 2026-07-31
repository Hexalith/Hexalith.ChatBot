---
baseline_commit: 4ac8675e0367fc314bf063a23952c9979fc68ec0
---

# Story 9.5: Derived-store cross-tenant isolation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a security owner,
I want vector/embedding/cache derived stores isolated per tenant at the store layer,
so that an application bug cannot produce a cross-tenant read.

## Acceptance Criteria

1. **Derived stores are partitioned per tenant at the store level (not application filtering); a cross-tenant query through the store's native API fails at the storage layer (FR55a, NFR9a).**
   **Given** vector indexes, embedding stores, prompt-context caches, and candidate-ranking caches (Hexalith.Memories),
   **When** built,
   **Then** they are partitioned **per tenant at the store level** — a tenant-scoped partition key/index name owns the data, **not** an application-side `WHERE tenantId = …` filter — such that a cross-tenant query issued through the store's **native API** (the store-access seam) **fails at the storage layer**: the intruder tenant's read can never observe the owner tenant's records.
   - **The partition convention is single-source and tenant-prefixed-by-construction.** Every derived-store key/index follows the same `{tenant}:{derived-class}:{resourceId}` shape already proven for the ChatBot projection read models (`GovernedOperationView.KeyFor` ⇒ `{tenant}:governed-operation:{noteId}`) and the Hexalith.Memories vector/syntactic indexes (`{tenantId}:memories:vec`, key prefix `{tenantId}:vec:`). One authoritative helper builds and validates the partition for all four derived-store classes (vector index, embedding store, prompt-context cache, candidate-ranking cache); there is never a second, drifting key scheme. The tenant id must be an `AuditMetadata.IsSafeStableIdentifier`-safe bounded token before it can scope a partition (fail-closed: an empty/unsafe tenant id resolves **no** partition, never a shared/global one).
   - **The store-access seam is where isolation lives — below the application layer.** A `tenantId`-first store-access interface (mirroring `IGovernedOperationProjectionStore`/`IOutboundTraceStore`) owns the partition lookup. The in-memory default keeps **one partition per tenant** (an ordinal-keyed `Dictionary<string, …>` per `DerivedStorePartition`), so a read under tenant B physically cannot reach tenant A's partition — there is no code path that filters a shared collection. A read for an unknown/foreign tenant yields a safe **not-found / empty**, never confirming another tenant's resource exists across the boundary.
   - **`net new` for this story is the seam + the partition contract, not the live Memories backing.** Hexalith.Memories' Redis Vector / FalkorDB binding is **not** integrated into the ChatBot today (architecture.md:324–325 — "Vector/embedding/prompt-context remains planned via Hexalith.Memories … M2"); candidate/learned ranking is M1 (architecture.md:306, 866). This story ships the **tenant-partition-by-construction contract, the store-access seam, an in-memory default, and the isolation probe** — the live Redis-Vector/FalkorDB wiring is the deferred M2 binding (see Deferrals). The seam is built so the M2 live binding is **additive** (drop in a Redis/FalkorDB `IDerivedStore` whose partition is `DerivedStorePartition` / `IndexSchemaDefinitions`), not a rewrite — exactly the inert-control-floor discipline of Stories 9.1–9.4.

2. **A nightly synthetic cross-tenant probe attempts cross-tenant reads through the store-access layer and asserts failure below the application layer; probe failure is a stop-ship defect (FR55a, NFR9a, NFR59).**
   **Given** the nightly synthetic cross-tenant probe,
   **When** it runs,
   **Then** it **actively attempts cross-tenant reads through the store-access layer** — for each ordered tenant pair `(owner, intruder)` it seeds a known sentinel in the owner's partition, then reads the owner's keys **through the intruder tenant's store-access scope** — and **asserts the cross-tenant read fails below the application layer** (the intruder observes **nothing** of the owner's data). Any cross-tenant read that **succeeds** (the intruder observes the owner's sentinel) is a **breach**.
   - **`probe failure is a stop-ship defect.**` The probe returns a structured `DerivedStoreIsolationProbeOutcome(PartitionsProbed, Breaches, Alerted)` a CI/release gate asserts against: zero breaches ⇒ the M2 release may proceed; any breach is stop-ship — **identical contract** to the Story 9.4 `ReplayIsolationProbeOutcome` M2 gate.
   - **Fail-closed (Epic 8/9 no-fabrication doctrine):** a probe that cannot complete (the store-access seam throws during seed or read-back) is itself a breach signal (`Unknown`), never a silent pass. A throw is **not** "isolation held."
   - **Injectable coordinator, no always-on `BackgroundService`.** The probe is a **pure verifier** + a **fail-closed audit-then-deliver** coordinator built to the **exact** discipline of `ReplayIsolationProbeCoordinator` / `AuditChainVerificationCoordinator`: on breach, write a metadata-only pre-commit `AuditEnvelopeFactory.DerivedStoreIsolationBreach` envelope **then** emit exactly one `OperatorAlertKind.DerivedStoreIsolationBreach` alert via `IOperatorAlertSink`. The periodic runtime trigger (Dapr timer / `PeriodicTimer`) is deferred — a scheduler need only call the sweep on its cadence. *(Superseded 2026-07-31: the trigger was wired by Story 12.14 — `PeriodicEnforcementBackgroundService` calls `SweepAllTenantPairsAsync` once per cadence partition. AC text left as written for the historical record.)*

### Cross-cutting requirements that hold for every AC

- **Tenant isolation by construction is the whole security model (NFR9a, NFR69, architecture cross-cutting #1).** Isolation is **physical partitioning at the store layer**, never an application-side filter (`Hexalith.Memories` project-context: "Tenant isolation is physical, not just filtered — use tenant-scoped RediSearch indexes, Redis Vector indexes, FalkorDB databases/graphs"). Never weaken the partition to "make a query convenient"; reads work *through* the partition.
- **Define-once / reuse — do NOT reinvent.** `DerivedStorePartition` is the single new partition predicate/helper, consumed everywhere a derived-store key is built **and** by the probe sweep — never inline a `{tenant}:` prefix twice (this is the `ReplayTenantPolicy` define-once lesson from 9.4). Consume by reference: the existing `{tenant}:domain:id` projection key convention (`GovernedOperationView.KeyFor`), the `IOutboundTraceStore` tenant-partitioned in-memory store shape, the `ReplayIsolationProbeCoordinator`/`AuditChainVerificationCoordinator` probe pattern, `IWormAuditStore.EnumerateTenants`, `IAuditWriter`/`IOperatorAlertSink`/`OperatorAlert`, `AuditMetadata` safe-token helpers, and `CommandGatewayServiceCollectionExtensions` DI shape. Cite the Hexalith.Memories `IndexSchemaDefinitions` index/key-prefix convention as the documented target for the M2 live binding so the dev does **not** invent a different prefix scheme later.
- **Metadata-only / no-leak floor (NFR2, NFR42).** Every probe result, alert payload, sentinel token, and first-offender locator is an `AuditMetadata`-safe bounded token (ASCII alnum + `.-_:@|`). The probe records **no** vector content, embedding values, prompt text, or candidate payloads — only the safe partition/tenant/sentinel locator tokens. Extend the Epic 7/8/9 no-leak serialization suites to every new type.
- **Audit two-phase / WORM is untouched (D4, NFR49a).** This story emits audit through the **existing** `AuditEnvelopeFactory` + `IAuditWriter` path; it adds **no** new commit-time gate, never mutates the chain, and does not touch the canonical hash. The probe is a **read/seed-and-read** over the derived-store seam, out-of-band.
- **Boundary (NetArchTest-enforced).** All new derived-store and probe internals (`DerivedStorePartition`, `IDerivedStore`/store seam, `DerivedStoreIsolationVerifier`/coordinator/outcome/result, the new `OperatorAlertKind`/factory) are `internal` to `Hexalith.ChatBot.Server`; no `.UI`/`.Cli`/`.Mcp` reference. Add a fitness test mirroring `ReplayIsolationBoundaryFitnessTests`.
- **Inert-control-floor honesty.** The **live Hexalith.Memories Redis-Vector/FalkorDB backing** and the **periodic scheduler** are deferred — the seams (partition contract, store-access layer, in-memory default, synthetic probe + alert + outcome) are the shippable, fully-built-and-tested deliverables. **State the deferrals explicitly in Completion Notes**; never let "the live Memories binding isn't wired" read as "isolation isn't enforced" — the enforcement (physical partition + probe) **is** built and tested.

## Tasks / Subtasks

- [x] **Task 1 — Define the single authoritative derived-store partition contract (`DerivedStorePartition`) (AC: #1, cross-cutting define-once)**
  - [x] Add `internal static class DerivedStorePartition` in `src/Hexalith.ChatBot.Server/Projections/` (or a new `DerivedStores/` folder if cleaner — keep it discoverable from both the store seam and the probe). Provide the four derived-store classes as a stable enum/const set — `vector-index`, `embedding-store`, `prompt-context-cache`, `candidate-ranking-cache` — and a `static string KeyFor(DerivedStoreClass cls, string tenantId, string resourceId)` returning `{tenantId}:{derived-class}:{resourceId}`, plus a `static string PartitionPrefix(DerivedStoreClass cls, string tenantId)` ⇒ `{tenantId}:{derived-class}:`. Mirror the `GovernedOperationView.KeyFor` convention exactly (tenant id always first). Validate tenant id with `AuditMetadata.IsSafeStableIdentifier`; an empty/unsafe tenant id resolves **no** partition (throw `ArgumentException`, fail-closed) — never a shared/global key.
  - [x] **Document the convention against the M2 live target.** In the class XML doc, cite the Hexalith.Memories `IndexSchemaDefinitions` index/key-prefix convention (`{tenantId}:memories:vec`, prefix `{tenantId}:vec:`) as the partition the live Redis-Vector/FalkorDB binding will adopt — so the M2 wiring uses this contract, not a new scheme.
  - [x] Unit-test exhaustively: tenant-prefixed + distinct across tenants for the same logical resource id (mirror `CrossTenantStorePartitioningTests.KeyForShouldBeTenantPrefixedAndDistinctAcrossTenants`); empty/whitespace/unsafe tenant id ⇒ throws; each derived-store class produces a distinct partition segment.

- [x] **Task 2 — Tenant-partitioned derived-store seam + in-memory default (AC: #1)**
  - [x] Add `internal interface IDerivedStore` in `src/Hexalith.ChatBot.Server/Projections/DerivedStores/` with a **`tenantId`-first** surface mirroring `IOutboundTraceStore`/`IGovernedOperationProjectionStore`: e.g. `ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken)`, `ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken)`, `IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId)`, and `IReadOnlyList<string> EnumerateTenants()` (so the probe can sweep). Define `internal sealed record DerivedStoreEntry(...)` as **metadata-only safe tokens** (a safe `ResourceId` + a bounded `ContentDigest`/sentinel token — **never** raw vector floats, embedding values, or prompt text); apply `AuditMetadata.SafeOptionalToken`/`SafeCommandName` on construction.
  - [x] Add `internal sealed class InMemoryDerivedStore : IDerivedStore` keeping **one partition per `DerivedStorePartition` key** (an ordinal-keyed `Dictionary<string, DerivedStoreEntry>` built via `DerivedStorePartition.KeyFor`, or a `Dictionary<string, Dictionary<string,…>>` keyed first by tenant). Isolation is structural: a `GetAsync` under tenant B builds B's partition key and so **cannot** reach tenant A's entry — there is no shared collection scanned with a filter. A foreign/unknown tenant read yields `null`/empty. Mirror `InMemoryOutboundTraceStore`'s `Lock`-guarded discipline.
  - [x] **Do not integrate the live Hexalith.Memories Redis-Vector/FalkorDB backing in this story** — it is the deferred M2 binding. Leave a documented seam (class comment + ADR) for the live `IDerivedStore` impl whose partition is `IndexSchemaDefinitions`. State this explicitly; never imply the live store is wired.

- [x] **Task 3 — Synthetic cross-tenant isolation probe (verifier + coordinator + outcome + alert + factory) (AC: #2)**
  - [x] Add a **pure** `internal static class DerivedStoreIsolationVerifier` with `DerivedStoreIsolationVerificationResult Verify(string ownerTenant, string intruderTenant, IReadOnlyList<string> sentinelResourceIds, Func<string,string,IReadOnlyList<string>> readOwnerKeysThroughIntruderScope)` — or, simpler, a verifier over the **outcome** of an attempted cross-tenant read: given the owner's seeded sentinel ids and the ids actually **observable through the intruder's scope**, a non-empty intersection ⇒ `Breach`. Return a metadata-only `DerivedStoreIsolationVerificationResult(OwnerTenantRef, IntruderTenantRef, status `Clean`/`Breach`/`Unknown`, reason code, safe first-offender locator)`. Mirror `ReplayIsolationVerifier`/`ReplayIsolationVerificationResult` shape and reason-code constants (`derived_store_isolation_clean` / `…_breach` / `…_probe_incomplete`).
  - [x] Add `internal sealed class DerivedStoreIsolationProbeCoordinator(IDerivedStore derivedStore, IAuditWriter auditWriter, IOperatorAlertSink operatorAlertSink, ISystemClock clock)` modeled **directly** on `ReplayIsolationProbeCoordinator`: for each ordered tenant pair drawn from `derivedStore.EnumerateTenants()`, **seed a synthetic sentinel** in the owner's partition (a reserved `iso-probe:` sentinel resource id, removed/ignored after — keep it metadata-only and clearly a probe artifact), **attempt the read through the intruder's scope**, run the verifier, and on any breach do **fail-closed audit-then-deliver**: write `AuditEnvelopeFactory.DerivedStoreIsolationBreach(...)` pre-commit **then** emit exactly one `OperatorAlertKind.DerivedStoreIsolationBreach` alert. A seed/read that throws ⇒ `Unknown` (breach signal), never silent pass.
  - [x] Expose `ValueTask<DerivedStoreIsolationProbeOutcome> SweepAllTenantPairsAsync(string runCorrelationId, CancellationToken)` returning `internal sealed record DerivedStoreIsolationProbeOutcome(int PartitionsProbed, int Breaches, int Alerted)` (mirror `ReplayIsolationProbeOutcome`) — the method a periodic scheduler **and the M2 release gate** call; zero breaches ⇒ release may proceed. **No `BackgroundService`** (deferred trigger — document like the 9.4 coordinator's class comment).
  - [x] Add `OperatorAlertKind.DerivedStoreIsolationBreach` to `src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs` with a Story-9.5 explanatory comment (stop-ship/M2-gating; fail-closed; one alert per breached pair). Add `AuditEnvelopeFactory.DerivedStoreIsolationBreach(DerivedStoreIsolationVerificationResult, correlationId, timestamp)` following the `ReplayIsolationBreach`/`AuditChainBroken` factory shape (pre-commit, `Decision: "alert"`, `Worker` surface origin, metadata-only refs, own `ReplayRunId` null).

- [x] **Task 4 — DI wiring (AC: #1, #2)**
  - [x] Register `IDerivedStore` → `InMemoryDerivedStore` and the `DerivedStoreIsolationProbeCoordinator` in `CommandGatewayServiceCollectionExtensions` with the existing `TryAdd` discipline, next to the 9.1/9.2/9.4 coordinators. Add a DI guard test (mirror `ReplayIsolationDependencyInjectionTests`) asserting the seam + coordinator resolve.

- [x] **Task 5 — Tests (AC: #1, #2)**
  - [x] **Partition tests** (`tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStorePartitionTests.cs`): tenant-prefixed, distinct across tenants for the same resource id, distinct per derived-store class, throws on empty/unsafe tenant id.
  - [x] **Store-isolation tests** (`tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/InMemoryDerivedStoreTests.cs`): the same logical resource id seeded under two tenants reads back only its own entry; a third, unseeded tenant gets a safe not-found; a read under tenant B never observes tenant A's entry for any derived-store class. Mirror `CrossTenantStorePartitioningTests`.
  - [x] **Conformance cross-tenant negative tests** (`tests/Hexalith.ChatBot.Conformance.Tests/DerivedStoreCrossTenantIsolationTests.cs`): reuse the Story 1.12 leakage corpus/sentinel pattern (`CrossTenantLeakageCorpus.BoundTenant`/`ForeignTenant`) — seed a foreign-tenant sentinel into each derived-store class, attempt every read through the bound tenant's scope, assert **no** foreign sentinel is observable and scan serialized outputs for foreign sentinel tokens (extend `CrossTenantLeakageScanner` coverage to the derived-store entries/probe result).
  - [x] **Probe tests** (`tests/Hexalith.ChatBot.Server.Tests/Audit/DerivedStoreIsolationProbeCoordinatorTests.cs`): a correctly-partitioned store ⇒ `Clean`, zero breaches, no alert; a **deliberately leaky** test-double `IDerivedStore` (one that ignores the tenant scope on read) ⇒ `Breach` + exactly one `DerivedStoreIsolationBreach` alert (audit-then-deliver: alert only after the breach envelope is written); a seam that throws on seed/read ⇒ `Unknown` (breach signal), no silent pass; the `DerivedStoreIsolationProbeOutcome` counts are accurate (release-gate contract); the probe leaves no production sentinel behind (or its sentinel is unambiguously a probe artifact).
  - [x] **No-leak serialization tests:** extend the Epic 7/8/9 no-leak assertions to `DerivedStoreEntry`, `DerivedStoreIsolationVerificationResult`, `DerivedStoreIsolationProbeOutcome`, and the `DerivedStoreIsolationBreach` alert payload (no vector/embedding/prompt/candidate content).
  - [x] **Architecture/boundary tests** (`tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DerivedStoreIsolationBoundaryFitnessTests.cs`): confirm `DerivedStorePartition`, `IDerivedStore`/`InMemoryDerivedStore`/`DerivedStoreEntry`, the verifier/coordinator/outcome/result remain `internal` to `.Server` (no `.UI`/`.Cli`/`.Mcp` reference). Run `tests/Hexalith.ChatBot.Architecture.Tests`.

- [x] **Task 6 — ADR + docs (AC: #1, #2)**
  - [x] Author `docs/adrs/derived-store-cross-tenant-isolation.md`: the single-source `DerivedStorePartition` convention (`{tenant}:{derived-class}:{resourceId}`) and why physical partitioning beats application filtering (NFR9a, Memories "physical, not filtered"); the `tenantId`-first store-access seam + in-memory default; the synthetic cross-tenant probe (pure verifier + fail-closed audit-then-deliver coordinator, active seed-then-read-through-intruder-scope, `DerivedStoreIsolationBreach` alert) and **the M2 release-gate wiring** (`SweepAllTenantPairsAsync` returns zero breaches ⇒ release may proceed; non-zero ⇒ stop-ship). Reference Story 9.1 `worm-audit-backing.md`, Story 9.4 `replay-simulation-isolation.md`, and the Memories `IndexSchemaDefinitions` convention. **Explicitly record the deferrals** (live Hexalith.Memories Redis-Vector/FalkorDB binding; periodic scheduler trigger) and that the M2 live binding is additive on this contract.

## Dev Notes

### What this story actually changes (and what already exists)

Story 9.5 **owns derived-store cross-tenant isolation as a built-and-proven contract** — the partition discipline + the synthetic probe that proves it — for the four derived-store classes FR55a/NFR9a name (vector indexes, embedding stores, prompt-context caches, candidate-ranking caches). It is **new seams wired into existing extension points**; the audit/probe machinery already exists in mature form (9.1/9.2/9.4) — **reuse, do not reinvent.**

**The single most important framing for the dev agent:** Hexalith.Memories' live Redis-Vector / FalkorDB backing is **not integrated into the ChatBot today** (architecture.md:324–325 marks vector/embedding/prompt-context as **planned via Hexalith.Memories — M2**; learned/AI candidate ranking is **M1**, architecture.md:306, 866). So — exactly like Stories 9.1–9.4's inert-control-floor — this story ships the **enforcement contract + seam + in-memory default + synthetic probe + tests + ADR**, and **defers** the live Memories/Redis binding and the periodic trigger. The seam is built so the M2 live binding is **additive** (a Redis/FalkorDB `IDerivedStore` whose partition is `DerivedStorePartition` / Memories `IndexSchemaDefinitions`), never a rewrite. **Do not** pull Hexalith.Memories into the ChatBot DI/AppHost in this story.

**Already exists — consume by reference:**

- **The `{tenant}:domain:id` store-partition convention is proven.** `GovernedOperationView.KeyFor(tenantId, noteId)` ⇒ `{tenant}:governed-operation:{noteId}`; projection stores take `tenantId` **first** and build the partition key, so a foreign-tenant read is a key miss at the store layer (`CrossTenantStorePartitioningTests` proves it). `DerivedStorePartition` is the **same idea, one helper, four derived-store classes**. [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs (KeyFor); tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantStorePartitioningTests.cs]
- **The tenant-partitioned in-memory store shape is solved.** `IOutboundTraceStore` + `InMemoryOutboundTraceStore` (a `Lock`-guarded `Dictionary<string, List<…>>` per tenant, with `EnumerateForTenant`/`EnumerateTenants`) is the **exact** shape to mirror for `IDerivedStore`/`InMemoryDerivedStore`. [Source: src/Hexalith.ChatBot.Server/Adapters/Mailbox/IOutboundTraceStore.cs]
- **The nightly-probe pattern is solved twice.** `ReplayIsolationProbeCoordinator` (Story 9.4) and `AuditChainVerificationCoordinator` (Story 9.1) are the **canonical templates**: pure verifier (`ReplayIsolationVerifier`/`WormAuditChainVerifier`) + fail-closed audit-then-deliver (`IAuditWriter.RecordPreCommitAsync` then **one** `IOperatorAlertSink.EmitAsync`); a throwing enumeration ⇒ `Unknown` breach signal; **no `BackgroundService`**; a `Sweep…Async` method returning a `…Outcome(…Probed, Breaches, Alerted)` the M2 release gate asserts against. **Clone this discipline exactly** — a reviewer will diff your coordinator against `ReplayIsolationProbeCoordinator` line-for-line. [Source: src/Hexalith.ChatBot.Server/Audit/ReplayIsolationProbeCoordinator.cs; ReplayIsolationVerifier.cs; ReplayIsolationVerificationResult.cs; AuditChainVerificationCoordinator.cs]
- **The define-once predicate lesson (Story 9.4 `ReplayTenantPolicy`).** A single authoritative helper, consumed everywhere (key construction **and** probe sweep), never two drifting checks. `DerivedStorePartition` is this story's equivalent. [Source: src/Hexalith.ChatBot.Server/Audit/ReplayTenantPolicy.cs]
- **The breach-alert plumbing.** `OperatorAlertKind` (extend with `DerivedStoreIsolationBreach`), `OperatorAlert(kind, reasonCode, tenantRef, alertName, correlationId, timestamp, locator)`, `IOperatorAlertSink.EmitAsync`, and the `AuditEnvelopeFactory.ReplayIsolationBreach`/`AuditChainBroken` factory shape (pre-commit, `Decision: "alert"`, metadata-only refs, `Worker` surface origin). [Source: src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs; AuditEnvelopeFactory.cs:419-462, 541-590]
- **The cross-tenant negative-test corpus (Story 1.12).** `CrossTenantLeakageCorpus.BoundTenant`/`ForeignTenant`/sentinels + `CrossTenantLeakageScanner` is the embedded sentinel harness to drive the conformance cross-tenant reads and scan derived-store outputs for foreign tokens. [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageCorpus.cs; CrossTenantLeakageScanner.cs]
- **Safe-token discipline.** `AuditMetadata.SafeOptionalToken / SafeCommandName / IsSafeStableIdentifier` gate every emitted token (the partition tenant id, sentinel resource id, probe locator, alert reason). [Source: src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs]
- **The M2 live-target convention (Hexalith.Memories).** Cite — do not duplicate — `IndexSchemaDefinitions` (`{tenantId}:memories:vec`, `{tenantId}:memories:vec:nl`, prefixes `{tenantId}:vec:`, `{tenantId}:mu:`) and `TenantIsolationVerifier` as the convention the deferred M2 live binding adopts. [Source: Hexalith.Memories/src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs; Hexalith.Memories/src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs]

**What you are adding (the real deliverables):** (1) `DerivedStorePartition` — the single tenant-partition contract for the four derived-store classes; (2) `IDerivedStore` + `InMemoryDerivedStore` + `DerivedStoreEntry` — the tenant-partitioned-by-construction store-access seam; (3) the synthetic cross-tenant probe — `DerivedStoreIsolationVerifier` + `DerivedStoreIsolationProbeCoordinator` + `DerivedStoreIsolationProbeOutcome` (M2 stop-ship gate) + `OperatorAlertKind.DerivedStoreIsolationBreach` + `AuditEnvelopeFactory.DerivedStoreIsolationBreach`; (4) DI wiring; (5) tests + ADR.

### Architecture constraints (must follow)

- **FR55a / NFR9a are the binding spec.** Vector indexes, embedding stores, prompt-context caches, candidate-ranking caches partitioned **per tenant at the store level**; cross-tenant query through the native API **must fail at the storage layer**; nightly synthetic cross-tenant probe; **probe failure is stop-ship**. [Source: epics.md:115 (FR55a), 195 (NFR9a), 2438-2452 (Story 9.5)]
- **NFR59 — periodic isolation probe / resilience validation produces evidence.** The probe is the NFR59 periodic isolation check for derived stores; resilience validation must prove no cross-tenant leakage with an evidence artifact (the `DerivedStoreIsolationProbeOutcome`). [Source: epics.md:269, 2624; FR55a "periodic isolation probe (NFR59)"]
- **Tenant isolation is physical, not filtered (Memories project-context; architecture cross-cutting #1, lines 128, 342-344).** "by construction at every layer incl. derived stores, caches, vector indexes" — M0 is single-tenant but tenant-partitioned **by construction** so M1/M2's additional tenants are additive, not a rewrite. [Source: architecture.md:128, 322-325, 342-344; Hexalith.Memories project-context "Tenant isolation is physical, not just filtered"]
- **D4 two-phase audit / WORM (NFR49a)** — this story adds no commit-time gate, never mutates the chain, leaves the canonical hash untouched; the probe is a read/seed-and-read out-of-band. [Source: architecture.md:345-347; Story 9.1/9.4 Dev Notes]
- **Boundary (NetArchTest-enforced)** — derived-store + probe internals are `internal` to `.Server`; no `.UI`/`.Cli`/`.Mcp` may reference them. [Source: tests/Hexalith.ChatBot.Architecture.Tests/Fitness/ReplayIsolationBoundaryFitnessTests.cs]
- **M2 increment** — derived-store store-layer isolation (NFR9a) is explicitly an M2-detail item; the probe **gates** the M2 release. [Source: architecture.md:306, 814, 866; epics.md:115, 195]

### Previous-work intelligence — apply directly

- **The probe shape is solved — match `ReplayIsolationProbeCoordinator` exactly.** Same fail-closed audit-then-deliver, `Unknown`-on-throw, no `BackgroundService`, structured `…Outcome` M2 gate, one alert per breach. A reviewer will compare line-for-line. The *only* semantic difference: this probe is an **active negative probe** (seed under owner, attempt read through intruder scope, a **successful** cross-tenant read = breach) rather than 9.4's scan-for-marked-records — make that difference explicit in the verifier doc.
- **Define-once is enforced.** Consume `DerivedStorePartition` for every derived-store key **and** the probe sweep — never inline a `{tenant}:` prefix twice (the 9.4 `ReplayTenantPolicy` lesson).
- **Bookkeeping drift is the #1 recurring review auto-fix across Epics 7–9** (stale test counts, **File List omissions** — 9.1/9.2/9.3/9.4 reviews each fixed these). Keep the **File List exhaustive** (every new + modified source/test/ADR) and every cited test count accurate against the live run.
- **Inert-control-floor honesty (the 9.4 deferral discipline).** The live Hexalith.Memories backing and the periodic scheduler are deferred — the seams are the deliverable. **State the deferrals explicitly in Completion Notes**; never let "the live Memories binding isn't wired" read as "isolation isn't enforced."
- **No-leak first.** Derived stores hold the most sensitive material in the system (embeddings, prompt context). The `DerivedStoreEntry` must be metadata-only by construction (digest/sentinel tokens, never raw vectors/prompt text) and every serialized type must pass the no-leak suite.

### Project Structure Notes

- **Server (all new internal types live here):**
  - `src/Hexalith.ChatBot.Server/Projections/DerivedStorePartition.cs` (the partition contract + `DerivedStoreClass` enum) — or a `DerivedStores/` subfolder; keep discoverable from both the store seam and the probe.
  - `src/Hexalith.ChatBot.Server/Projections/DerivedStores/IDerivedStore.cs` (+ `InMemoryDerivedStore`, `DerivedStoreEntry`).
  - `src/Hexalith.ChatBot.Server/Audit/DerivedStoreIsolationVerifier.cs`, `DerivedStoreIsolationVerificationResult.cs` (+ `DerivedStoreIsolationStatus` enum), `DerivedStoreIsolationProbeCoordinator.cs` (+ `DerivedStoreIsolationProbeOutcome`).
  - **Modified:** `Audit/OperatorAlertKind.cs` (+`DerivedStoreIsolationBreach`), `Audit/AuditEnvelopeFactory.cs` (new `DerivedStoreIsolationBreach` factory), `Gateway/CommandGatewayServiceCollectionExtensions.cs` (DI for the store + probe coordinator).
- **Tests:** `tests/Hexalith.ChatBot.Server.Tests/Projections/` (partition + store isolation), `tests/Hexalith.ChatBot.Server.Tests/Audit/` (probe), `tests/Hexalith.ChatBot.Conformance.Tests/` (cross-tenant negative, reuse Story 1.12 corpus), `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/` (boundary), the no-leak serialization suites.
- **Docs:** `docs/adrs/derived-store-cross-tenant-isolation.md`.
- No conflict with the unified structure: the `Projections/` + `Audit/` server seams and the `internal`-to-`.Server` boundary match the architecture's prescribed placement; no new top-level project is required (the live Memories binding, when wired at M2, is an additive `IDerivedStore` impl, possibly in a `.Server`-internal `DerivedStores/Redis` folder or a Memories-adapter project — out of scope here).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.5 (lines 2438-2452); Epic 9 (lines 580-582, 2358-2360)]
- [Source: _bmad-output/planning-artifacts/epics.md#FR55a (115, 491), NFR9a (195), NFR59 (269), NFR69 (285), NFR2 (no-leak floor)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Derived-store backing (322-325), Tenant isolation by construction (128, 342-344), M2 deferrals (306, 814, 866)]
- [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs (KeyFor); DaprAssociationProjectionStore.cs; InMemoryGovernedOperationProjectionStore.cs]
- [Source: src/Hexalith.ChatBot.Server/Adapters/Mailbox/IOutboundTraceStore.cs (InMemoryOutboundTraceStore tenant-partition shape)]
- [Source: src/Hexalith.ChatBot.Server/Audit/ReplayIsolationProbeCoordinator.cs; ReplayIsolationVerifier.cs; ReplayIsolationVerificationResult.cs; ReplayTenantPolicy.cs; AuditChainVerificationCoordinator.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs; AuditEnvelopeFactory.cs:419-462, 541-590; AuditMetadata.cs; IWormAuditStore.cs; IOperatorAlertSink/OperatorAlert]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantStorePartitioningTests.cs; Harness/CrossTenantLeakageCorpus.cs; Harness/CrossTenantLeakageScanner.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Audit/ReplayIsolationProbeCoordinatorTests.cs; Adapters/Mailbox/ReplayIsolationDependencyInjectionTests.cs; tests/Hexalith.ChatBot.Architecture.Tests/Fitness/ReplayIsolationBoundaryFitnessTests.cs]
- [Source: Hexalith.Memories/src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs; Tenants/TenantIsolationVerifier.cs; Hexalith.Memories/_bmad-output/project-context.md ("Tenant isolation is physical, not just filtered")]
- [Source: _bmad-output/implementation-artifacts/9-4-replay-and-simulation-isolation.md; 9-1-tamper-evident-worm-audit-chain.md]

## Dev Agent Record

### Agent Model Used

Opus 4.8 (claude-opus-4-8[1m])

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx` ⇒ 0 warnings / 0 errors (full solution).
- `dotnet test tests/Hexalith.ChatBot.Server.Tests` ⇒ 1295 passed / 0 failed (54 new Story-9.5 tests, incl. the QA gap-closing run's +18).
- `dotnet test tests/Hexalith.ChatBot.Conformance.Tests` ⇒ 76 passed / 0 failed (1 new cross-tenant negative test).
- `dotnet test tests/Hexalith.ChatBot.Architecture.Tests` ⇒ 39 passed / 0 failed (1 new boundary fitness test).

### Completion Notes List

- **AC1 (physical per-tenant partitioning at the store layer).** `DerivedStorePartition` is the single authoritative,
  define-once partition contract for the four derived-store classes — `KeyFor` ⇒ `{tenant}:{derived-class}:{resourceId}`
  and `PartitionPrefix` ⇒ `{tenant}:{derived-class}:`, tenant id always first (mirrors `GovernedOperationView.KeyFor`).
  Fail-closed: an empty/unsafe tenant id or resource id resolves no partition (throws), never a shared/global key. The
  `tenantId`-first `IDerivedStore` seam + `InMemoryDerivedStore` (tenant-first nested dictionaries) make a cross-tenant
  read structurally a key miss at the store layer — no shared collection is filtered; a foreign tenant gets a safe
  not-found. `DerivedStoreEntry` is metadata-only by construction (safe `ResourceId` + bounded `ContentDigest`; no field
  for raw vector/embedding/prompt/candidate content).
- **AC2 (synthetic cross-tenant probe; failure is stop-ship).** Pure `DerivedStoreIsolationVerifier` (active negative
  probe: a *successful* cross-tenant read = breach) + `DerivedStoreIsolationProbeCoordinator` modeled line-for-line on
  `ReplayIsolationProbeCoordinator` — for each ordered tenant pair it seeds a reserved `iso-probe:` sentinel into the
  owner's four partitions and reads them back through the intruder's scope; on breach it does fail-closed
  audit-then-deliver (`AuditEnvelopeFactory.DerivedStoreIsolationBreach` pre-commit, then exactly one
  `OperatorAlertKind.DerivedStoreIsolationBreach` alert). A seed/read that throws ⇒ `Unknown` (never a silent pass).
  `SweepAllTenantPairsAsync` returns `DerivedStoreIsolationProbeOutcome(PartitionsProbed, Breaches, Alerted)` — the M2
  release gate: zero breaches ⇒ release may proceed.
- **No-leak floor (NFR2/NFR42).** Every new serialized type (`DerivedStoreEntry`, the verification result, the outcome,
  the breach envelope) passes the no-leak suite; a sensitive-marker digest collapses to a safe fallback on construction.
- **Boundary (NetArchTest).** All new derived-store + probe internals are `internal` to `.Server`
  (`DerivedStoreIsolationBoundaryFitnessTests`); no `.UI`/`.Cli`/`.Mcp` reference.
- **Audit/WORM untouched (D4/NFR49a).** The probe emits through the existing `AuditEnvelopeFactory`/`IAuditWriter` path,
  adds no commit-time gate, never mutates the chain, and does not touch the canonical hash.
- **Runtime activation and remaining live-store deferral.** Story 12.14 wires
  `DerivedStoreIsolationProbeCoordinator.SweepAllTenantPairsAsync` into the existing
  `PeriodicEnforcementBackgroundService` through the independently gated nightly
  `derived-store-isolation-probe` evaluator. `DerivedStoreIsolationProbeOutcome.Breaches == 0` remains the stop-ship
  condition, and `m2_derived_store_isolation_missed_cadence` independently alerts on a stale run. The **live
  Hexalith.Memories Redis-Vector/FalkorDB `IDerivedStore` binding** remains deferred and additive on this contract
  (mapped onto Memories `IndexSchemaDefinitions`); Hexalith.Memories was not pulled into the ChatBot DI/AppHost.
- **The release gate is published and consumed (corrected 2026-07-31).** An earlier revision claimed this
  outcome "is now the running M2 release gate", asserted by the Story 12.14 coordinator tests. That was an
  overstatement — a unit-test call is precisely the "merely provable" state the AC set out to leave behind. Today the
  scheduler publishes the verdict on token-gated `/health/chatbot/periodic-enforcement/m2`, and `release.yml`'s
  required topology-acceptance job asserts it before `semantic-release`. The probe takes its tenant population from
  both the derived store and the independently populated WORM audit store, so an empty derived store cannot hide known
  active tenants; the live gate test establishes two authenticated tenants and requires real pair coverage.
- **Sentinel accumulation closed (2026-07-31).** Scheduling the probe turned Story 12.5's `[Low · noted]` per-run
  sentinel id into a live write-amplification defect (four never-overwritten entries per owner tenant per run). The
  sentinel id is now deterministic per (class, owner tenant) and the probe invalidates what it seeded in a `finally`.

### File List

**New — source (`src/Hexalith.ChatBot.Server/`):**

- `Projections/DerivedStores/DerivedStorePartition.cs` (partition contract + `DerivedStoreClass` enum)
- `Projections/DerivedStores/IDerivedStore.cs` (`IDerivedStore` + `InMemoryDerivedStore` + `DerivedStoreEntry`)
- `Audit/DerivedStoreIsolationVerificationResult.cs` (`DerivedStoreIsolationStatus` enum + result record)
- `Audit/DerivedStoreIsolationVerifier.cs`
- `Audit/DerivedStoreIsolationProbeCoordinator.cs` (coordinator + `DerivedStoreIsolationProbeOutcome` record)

**Modified — source:**

- `Audit/OperatorAlertKind.cs` (+`DerivedStoreIsolationBreach`)
- `Audit/AuditEnvelopeFactory.cs` (+`DerivedStoreIsolationBreach` factory)
- `Gateway/CommandGatewayServiceCollectionExtensions.cs` (DI: `IDerivedStore` + probe coordinator)

**New — tests:**

- `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStorePartitionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/InMemoryDerivedStoreTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/DerivedStoreIsolationDependencyInjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Audit/DerivedStoreIsolationProbeCoordinatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Audit/DerivedStoreIsolationLeakTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/DerivedStoreCrossTenantIsolationTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DerivedStoreIsolationBoundaryFitnessTests.cs`

**New — docs:**

- `docs/adrs/derived-store-cross-tenant-isolation.md`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Outcome:** Approve

Adversarial review against the live implementation and the `ReplayIsolation*` / `AuditChainVerification*` templates the story claims to mirror.

- **All ACs implemented, all `[x]` tasks genuinely done.** `DerivedStorePartition` is the single fail-closed `{tenant}:{class}:{resource}` contract (consumed by both the store seam and the probe sweep — define-once honored). `IDerivedStore`/`InMemoryDerivedStore` isolate structurally (tenant-first nested dictionaries, no shared-collection filter). `DerivedStoreIsolationVerifier`/`...ProbeCoordinator`/`...ProbeOutcome` are an active-negative probe modeled line-for-line on `ReplayIsolationProbeCoordinator` (fail-closed audit-then-deliver, `Unknown`-on-throw, no `BackgroundService`, structured M2 release-gate outcome). `OperatorAlertKind.DerivedStoreIsolationBreach` + `AuditEnvelopeFactory.DerivedStoreIsolationBreach` follow the `ReplayIsolationBreach` shape (pre-commit, `Decision: "alert"`, `Worker` origin, metadata-only refs, null `ReplayRunId`). DI wired with `TryAdd`/`AddSingleton` next to the 9.4 coordinator. ADR records the convention, the M2 `IndexSchemaDefinitions` target, and the deferrals.
- **Verified green (full rebuild + live run):** solution build 0 warnings / 0 errors; `Server.Tests` 1295/0, `Conformance.Tests` 76/0, `Architecture.Tests` 39/0.
- **[Medium · fixed] Bookkeeping drift** — Debug Log/Change Log test counts were stale (`1277`/`36 new`/`38 new`); corrected to the live `1295`/`54 new Server tests`/`56 new total` after the QA gap-closing run's +18.
- **[Low · noted] Resource-id validation asymmetry** — `InMemoryDerivedStore` validates the tenant id but not the resource id (unlike `DerivedStorePartition.KeyFor`); a latent raw-key vs sanitized-`DerivedStoreEntry.ResourceId` mismatch, unreachable by current safe callers. Left as-is (deliberate design; tightening risks the conformance corpus).
- **[Low · noted] Probe sentinels are not deleted** — `IDerivedStore` has no delete op; the story permits the "unambiguous probe artifact" path (test-asserted), but the deferred M2 live binding should add a delete seam to avoid `iso-probe:` accumulation.

No critical issues; the deferrals (live Hexalith.Memories Redis-Vector/FalkorDB binding, periodic scheduler trigger) are explicitly and honestly recorded. *(Corrected 2026-07-31: the periodic scheduler trigger is no longer deferred — Story 12.14 wired it. The live Hexalith.Memories binding remains deferred and is owned by Story 12.16. This conclusion predates that activation.)*

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-03 | 0.1 | Story drafted — derived-store cross-tenant isolation: single-source `DerivedStorePartition` contract for the four derived-store classes, tenant-partitioned-by-construction `IDerivedStore` seam + in-memory default, synthetic cross-tenant isolation probe (verifier + fail-closed coordinator + `DerivedStoreIsolationBreach` alert + `DerivedStoreIsolationProbeOutcome` M2 stop-ship gate), tests + ADR. Live Hexalith.Memories Redis-Vector/FalkorDB backing and periodic trigger deferred (inert-control-floor). | create-story (Opus 4.8) |
| 2026-06-03 | 1.0 | Implemented all tasks: `DerivedStorePartition` contract; `IDerivedStore`/`InMemoryDerivedStore`/`DerivedStoreEntry` tenant-partitioned seam; `DerivedStoreIsolationVerifier`/`DerivedStoreIsolationProbeCoordinator`/`DerivedStoreIsolationProbeOutcome` active-negative probe with fail-closed audit-then-deliver; `OperatorAlertKind.DerivedStoreIsolationBreach` + `AuditEnvelopeFactory.DerivedStoreIsolationBreach`; DI wiring; partition/store-isolation/conformance-cross-tenant/probe/no-leak/boundary/DI tests; ADR. 38 new tests, full solution green (1277 + 76 + 39 in touched projects). Live Memories binding + periodic trigger deferred. | dev-story (Opus 4.8) |
| 2026-06-03 | 1.1 | Senior Developer Review (auto): adversarial review against live implementation — all ACs/tasks verified done, build clean, all tests green. Auto-fixed stale test bookkeeping (Debug Log 1277→1295 / 36→54 new Server tests; total 38→56 new). Two Low observations noted (resource-id validation asymmetry; probe-sentinel delete seam for M2). Outcome: Approve. Status → done. | review (Opus 4.8) |
