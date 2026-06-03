# ADR: Correction-driven vector reindexing — the named `ReindexVectors` M2 activity, a version-guard ledger, a derived-store delete seam, and a fail-closed P2 audit-then-deliver delay

## Status

Accepted (realized by Story 9.6, FR91a / NFR9a / NFR17a / NFR2 / NFR42). Plugs into the Story 2.8 correction-propagation
coordinator and the Story 9.5 derived-store partition seam
([derived-store-cross-tenant-isolation.md](derived-store-cross-tenant-isolation.md)); reuses the Story 9.1/9.4/9.5
audit-then-deliver discipline ([worm-audit-backing.md](worm-audit-backing.md),
[replay-simulation-isolation.md](replay-simulation-isolation.md)). Gates the **M2** release.

## Context

FR91a requires that, on an association correction, **every derived store** that referenced the *prior* association —
vector index, embedding store, prompt-context cache, candidate-ranking cache — is **invalidated and rebuilt**, so M2
derived stores never serve stale, misassigned project context (NFR9a). NFR17a sets the latency SLO: p95 ≤ **10 min**
(M0/M1, no vector index) and ≤ **60 min** (M2, incl. vector reindex); items beyond SLO surface `correction-delayed` with
owner role + next safe action, and an SLO breach is a **P2 incident**. The architecture names the operation verbatim
twice — `ReindexVectors(tenantId, correctionId, sourceVersion)` (architecture.md:190, 399) — and marks it an idempotent,
version-guarded **M2** activity.

Like Stories 9.1–9.5's inert-control-floor, the live **Hexalith.Memories Redis-Vector / FalkorDB reindex backing is not
integrated into the ChatBot today** (architecture.md:324–325 mark vector/embedding/prompt-context as planned via
Hexalith.Memories — **M2**). So this story ships the **contract + version guard + delete seam + coordinator wiring + SLO
contract + P2 delay path + tests**, and **defers** the live Memories binding, the async long-running reindex runtime, and
the periodic SLO-deadline sweep. The seam is built so the live binding is **additive** (a Memories-backed
`IVectorReindexer` whose partition is the Memories `IndexSchemaDefinitions` convention), never a rewrite.

## Decision

1. **`IDerivedStore.InvalidateAsync` — the delete seam (define-once).** The Story 9.5 `IDerivedStore` had
   Put/Get/Enumerate but **no delete op** (the 9.5 Senior Review flagged adding one as the M2 follow-up). Story 9.6 adds
   `ValueTask<bool> InvalidateAsync(cls, tenantId, resourceId, ct)`: tenant-first and fail-closed exactly like `GetAsync`,
   it **structurally removes** the entry from the tenant's partition (returns whether one was present), so "invalidate"
   means the entry is physically gone — never a filter flag the read side could forget. Idempotent re-invalidate ⇒
   `false`; a foreign tenant never touches another's subtree.

2. **`CorrectionPropagationSlo` — one two-target SLO contract (define-once).** A single helper holds both NFR17a
   targets — `M0M1P95Target` (10 min, aliasing the coordinator's existing constant) and `M2P95Target` (60 min) — plus
   `DeadlineFor(scope, startedAt)` and `IsBreached(deadline, now)`. Neither the coordinator nor the reindexer inlines a
   second `FromMinutes(60)` (the Story 9.4 `ReplayTenantPolicy` / 9.5 `DerivedStorePartition` define-once lesson). The
   boundary (`now == deadline`) is on-time, not breached.

3. **`ReindexVectors` — `IVectorReindexer` + a version-guard `IVectorReindexLedger` (define-once).** The named operation
   lives behind `ReindexVectorsAsync(tenantId, correctionId, sourceVersion, affectedResourceIds, startedAt, ct)` and
   returns a metadata-only `VectorReindexOutcome` (counts + flags + deadline; never content). For each of the four
   `DerivedStorePartition.AllClasses`, the in-memory default consults the **single version-guard authority** — a
   tenant-partitioned ledger recording the last-applied correction `sourceVersion` per `DerivedStorePartition` key.
   `TryAdvance` returns `false` when `sourceVersion <= lastApplied` (order-tolerant last-writer-wins, mirroring
   `GovernedOperationProjectionHandler`'s `existing.SourceVersion >= notification.SourceVersion ⇒ Ignored`), so a
   re-delivered/older correction is a no-op (`VersionGuardSkipped`). Otherwise it `InvalidateAsync`-es each affected
   resource id and **rebuilds** the corrected entries as metadata-only `DerivedStoreEntry` values (a correction-stamped
   digest). A store/ledger throw is a fail-closed `vector_reindex_failed`, never a silent success.

4. **M2-scope wiring into the existing coordinator (not a parallel coordinator).** A
   `VectorReindexCorrectionPropagationStoreActivity : ICorrectionPropagationStoreActivity` (StoreKey =
   `vector-reindex`) calls the reindexer and maps the outcome onto a `CorrectionPropagationActivityResult` —
   `success` for a clean reindex (and for an idempotent version-guard skip within SLO), `failed` +
   `vector_reindex_slo_exceeded` for a completed-but-late reindex, `failed` + `vector_reindex_failed` for a throw. The
   affected resource id is derived deterministically from the correction identity (association id + prior project id) —
   the richer "every entry that referenced the prior association" enumeration is left to the live M2 binding if it needs
   a store-side index. `CorrectionPropagationStoreKeys.RequiredM2 = RequiredM0 + VectorReindex`; the coordinator runs the
   **M2 scope when the vector-reindex activity is registered**, and the unchanged **M0 scope** otherwise, so an M0
   deployment behaves exactly as Story 2.8 did (`IsReady`, the fan-out set, and the completion/delay path all stay
   backward-compatible).

5. **Fail-closed P2 audit-then-deliver delay (net-new).** The prior delayed path wrote only the delay command + a bare
   `CorrectionDelayed` alert (no audit, no severity). Story 9.6 upgrades `MarkDelayedAsync` to the 9.1/9.4/9.5 standard:
   `AuditEnvelopeFactory.CorrectionPropagationDelayed(...)` (pre-commit, `Decision: "alert"`, `Worker` origin,
   metadata-only refs incl. a **`correction-propagation-severity:p2`** marker mirroring 9.2's
   `audit-completeness-severity:p1`, plus owner role / next safe action / reason) is written **first**; only on a
   successful audit write is the **single** `OperatorAlertKind.CorrectionDelayed` alert emitted (carrying a safe P2
   incident locator in `FirstBreakLocator`). A failed audit write **suppresses** the alert (fail-closed), never the
   reverse. The delay fires on an SLO breach (`vector_reindex_slo_exceeded`) **and** the existing store-invalidation
   failure path; the reused owner role is `operations` and the next safe action is `escalate-to-operations`. No new alert
   kind is added — `CorrectionDelayed` is reused.

6. **No-leak floor / WORM untouched.** Derived stores hold the most sensitive material in the system; every new emitted
   token (reindex outcome, ledger key, activity result, delay alert payload, P2 locator) is an `AuditMetadata`-safe
   bounded token, and rebuilt entries stay metadata-only by construction (NFR2/NFR42). Audit is emitted through the
   existing two-phase path: no new commit-time gate, no chain mutation, the reindex + delay records are out-of-band
   pre-commit `Decision: "alert"` (D4, NFR49a).

7. **Boundary (NetArchTest-enforced).** All new reindex/SLO internals — `IVectorReindexer`/`InMemoryVectorReindexer`/
   `VectorReindexOutcome`, `IVectorReindexLedger`/`InMemoryVectorReindexLedger`,
   `VectorReindexCorrectionPropagationStoreActivity`, `CorrectionPropagationSlo`, the new `IDerivedStore.InvalidateAsync`
   — are `internal` to `Hexalith.ChatBot.Server`; no `.UI`/`.Cli`/`.Mcp` reference. The
   `DerivedStoreIsolationBoundaryFitnessTests` assertion is extended to pin them.

## Deferrals (inert-control-floor honesty)

- **The live Hexalith.Memories Redis-Vector / FalkorDB reindex binding** — an additive `IVectorReindexer` whose partition
  maps onto the Memories `IndexSchemaDefinitions` convention (`{tenantId}:memories:vec`, prefixes `{tenantId}:vec:`).
- **The async / long-running reindex runtime** — the in-memory reindex is synchronous.
- **The periodic SLO-deadline sweep trigger** — a scheduler need only call the contract on its cadence.

The `ReindexVectors` contract, the version-guard ledger, the `IDerivedStore.InvalidateAsync` delete seam, the
correction-propagation wiring, the `CorrectionPropagationSlo` deadline contract, and the P2 audit-then-deliver delay path
**are built and tested**. "The live Memories reindex isn't wired" must never be read as "stale material survives
correction": invalidation + rebuild + version guard are real and exercised.

## Consequences

- Correction propagation now reaches the derived stores idempotently and version-guarded, with physical tenant isolation
  preserved (a reindex under one tenant can never touch another's partition).
- The M2 SLO is enforceable and testable via an injectable clock / reindexer test-double; SLO breaches and reindex
  failures both surface as fail-closed P2 `correction-delayed` incidents.
- The live Memories binding is a drop-in additive implementation behind the same seam, not a rewrite.

## References

- `_bmad-output/planning-artifacts/epics.md` — Story 9.6 (2454–2468), Story 2.8 (1190–1212), FR91a (172, 530),
  NFR17a (209).
- `_bmad-output/planning-artifacts/architecture.md` — `ReindexVectors` M2 activity (190–191, 399–400), correction
  propagation (187–189, 395–398), derived-store backing M2 (322–325), tenant isolation by construction (128, 342–344).
- Story 2.8 — `DaprCorrectionPropagationCoordinator`, `ICorrectionPropagationStoreActivity`,
  `CorrectionPropagationStoreKeys`.
- Story 9.5 — [derived-store-cross-tenant-isolation.md](derived-store-cross-tenant-isolation.md) (`IDerivedStore`
  seam + the delete-seam review action item).
- Story 9.1/9.4 — audit-then-deliver discipline ([worm-audit-backing.md](worm-audit-backing.md),
  [replay-simulation-isolation.md](replay-simulation-isolation.md)).
- `Hexalith.Memories/src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` — the M2 live-target
  convention.
