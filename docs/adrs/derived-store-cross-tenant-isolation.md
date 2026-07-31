# ADR: Derived-store cross-tenant isolation — single-source partition contract, a tenant-first store seam, and a fail-closed synthetic probe

## Status

Accepted (realized by Story 9.5, FR55a / NFR9a / NFR59 / NFR69 / NFR2 / NFR42). Builds on the Story 9.1 WORM chain
([worm-audit-backing.md](worm-audit-backing.md)) and the Story 9.4 replay/simulation isolation probe pattern
([replay-simulation-isolation.md](replay-simulation-isolation.md)). Gates the **M2** release.

## Context

FR55a / NFR9a require the four derived-store classes — **vector indexes, embedding stores, prompt-context caches, and
candidate-ranking caches** — to be partitioned **per tenant at the store level**, such that a cross-tenant query through
the store's native API **fails at the storage layer**, plus a **nightly synthetic cross-tenant probe** whose failure is
**stop-ship**. Tenant isolation here is **physical partitioning, not an application-side `WHERE tenantId = …` filter**
(architecture cross-cutting #1; the Hexalith.Memories project-context: "Tenant isolation is physical, not just filtered —
use tenant-scoped RediSearch indexes, Redis Vector indexes, FalkorDB databases/graphs").

The live Hexalith.Memories Redis-Vector / FalkorDB backing is **not integrated into the ChatBot today**
(architecture.md:324–325 marks vector/embedding/prompt-context as planned via Hexalith.Memories — **M2**; learned/AI
candidate ranking is **M1**, architecture.md:306, 866). So — exactly like Stories 9.1–9.4's inert-control-floor — this
story ships the **enforcement contract + store-access seam + in-memory default + synthetic probe + tests**, and **defers**
the live Memories binding and the periodic trigger. The seam is built so the M2 live binding is **additive**, never a
rewrite.

## Decision

1. **One authoritative partition contract — `DerivedStorePartition` (define-once).** A single helper builds and validates
   every derived-store key for all four classes: `KeyFor(cls, tenant, resource)` ⇒ `{tenant}:{derived-class}:{resourceId}`
   and `PartitionPrefix(cls, tenant)` ⇒ `{tenant}:{derived-class}:`. This mirrors the proven projection-key convention
   `GovernedOperationView.KeyFor` ⇒ `{tenant}:governed-operation:{noteId}` exactly — **tenant id always first** — and is
   the Story 9.4 `ReplayTenantPolicy` define-once lesson applied to derived stores: the helper is consumed both by the
   store seam and by the probe sweep, so there is never a second, drifting `{tenant}:` scheme. Each class maps to a
   stable, distinct segment (`vector-index`, `embedding-store`, `prompt-context-cache`, `candidate-ranking-cache`), so the
   same logical resource id under two tenants — or two classes — is never the same key. **Fail-closed:** the tenant id and
   resource id must be `AuditMetadata.IsSafeStableIdentifier`-safe bounded tokens; an empty/unsafe id resolves **no**
   partition (throws), never a shared/global key.

2. **Isolation lives at the store-access seam, below the application layer.** `IDerivedStore` is a `tenantId`-first
   interface (mirroring `IOutboundTraceStore` / `IGovernedOperationProjectionStore`) that owns the partition lookup. The
   in-memory default `InMemoryDerivedStore` nests storage tenant-first
   (`Dictionary<tenant, Dictionary<partition-prefix, Dictionary<resourceId, entry>>>`): a read under tenant B starts at
   B's own subtree and builds B's partition prefix, so it **physically cannot** reach tenant A's subtree — there is no
   shared collection scanned with a filter. A foreign/unknown tenant read yields a safe **not-found** (`null`/empty),
   never confirming another tenant's resource exists across the boundary. `DerivedStoreEntry` is **metadata-only by
   construction** — a safe `ResourceId` plus a bounded `ContentDigest` sentinel, sanitized via `AuditMetadata` on
   `Create` — and has **no field** for raw vector floats, embedding values, prompt text, or candidate payloads
   (NFR2/NFR42 no-leak floor).

3. **A synthetic cross-tenant probe — pure verifier + fail-closed audit-then-deliver coordinator.** Modeled **directly**
   on `ReplayIsolationProbeCoordinator`. The **only** semantic difference: this is an **active negative probe** — for each
   ordered tenant pair `(owner, intruder)` it seeds a reserved `iso-probe:` sentinel into each of the owner's four
   partitions, then reads those exact sentinel ids back **through the intruder tenant's scope**; a **successful**
   cross-tenant read (the intruder observes the owner's sentinel) is the breach. `DerivedStoreIsolationVerifier` is the
   pure evaluator (non-empty intersection of owner-sentinels and intruder-observable ids ⇒ `Breach`). On any breach the
   coordinator writes the metadata-only `AuditEnvelopeFactory.DerivedStoreIsolationBreach` pre-commit envelope **then**
   emits exactly one `OperatorAlertKind.DerivedStoreIsolationBreach` alert via `IOperatorAlertSink`. A seed/read that
   throws ⇒ `Unknown` (a breach signal), never a silent pass.

4. **M2 release gate.** `SweepAllTenantPairsAsync(runCorrelationId, ct)` returns a structured
   `DerivedStoreIsolationProbeOutcome(PartitionsProbed, Breaches, Alerted, TenantsEnumerated)` a CI/release gate asserts against: **zero
   breaches over non-zero coverage ⇒ the M2 release may proceed; any breach — or a sweep that examined nothing — is
   stop-ship** — identical contract to the Story 9.4 `ReplayIsolationProbeOutcome` gate. The trigger is wired: Story
   12.14 calls the sweep from the always-on `PeriodicEnforcementBackgroundService` once per cadence partition, and
   publishes the result on `/health/chatbot/periodic-enforcement/m2`. The required `release.yml`
   `topology-acceptance` job asserts that token-gated verdict before `semantic-release`, including real derived-store
   pair coverage from two independently authenticated tenants.

5. **Boundary (NetArchTest-enforced).** `DerivedStorePartition`, `DerivedStoreClass`, `IDerivedStore`,
   `InMemoryDerivedStore`, `DerivedStoreEntry`, the verifier/result/status/coordinator/outcome are all `internal` to
   `Hexalith.ChatBot.Server`; no `.UI`/`.Cli`/`.Mcp` may reference them (`DerivedStoreIsolationBoundaryFitnessTests`).

## M2 live target (documented, not duplicated)

The deferred live Hexalith.Memories Redis-Vector / FalkorDB binding adopts **this** contract. Its tenant-scoped
index/key convention is `Hexalith.Memories.Server.Infrastructure.IndexSchemaDefinitions` — semantic index name
`{tenantId}:memories:vec`, natural-language index `{tenantId}:memories:vec:nl`, key prefixes `{tenantId}:vec:` and
`{tenantId}:mu:` — all tenant-prefixed-by-construction, validated by Memories' own `TenantIsolationVerifier`. The M2
binding is an **additive** `IDerivedStore` implementation whose partition is `DerivedStorePartition` mapped onto
`IndexSchemaDefinitions`, **not** a new prefix scheme invented later. Cite this convention; do not duplicate it.

## Consequences

- Derived-store isolation is **built and proven now** (physical partition + synthetic probe + release-gate contract),
  independent of the live Memories wiring. "The live Memories binding isn't wired" must **never** read as "isolation
  isn't enforced".
- The audit two-phase / WORM chain is **untouched** (D4, NFR49a): the probe emits through the existing
  `AuditEnvelopeFactory` + `IAuditWriter` path, adds no commit-time gate, never mutates the chain, and does not touch the
  canonical hash. The probe is an out-of-band seed-and-read over the derived-store seam.
- The probe seeds into the live store, so its sentinels are deliberately a reserved, unambiguous probe artifact (the
  `iso-probe:` prefix and a metadata-only digest), never mistakable for production data. Since Story 12.14 put the probe
  on a schedule, the sentinel resource id is **deterministic per (class, owner tenant)** and the probe invalidates what
  it seeded in a `finally` block: a per-run id would have written four never-overwritten entries per owner tenant on
  every nightly run, turning a one-shot release-gate artifact into unbounded growth of live derived state. Cleanup is
  best-effort by design — a deterministic id means a failed delete is overwritten next run rather than accumulating,
  and cleanup failures must never mask the probe's verdict.
- The probe derives its population from the union of derived-store tenants and the independently populated WORM audit
  store. An empty or misbound derived store therefore cannot erase known active tenants and turn missing coverage into
  a pass; exactly one positively observed tenant is the only zero-pair structural exemption.

## Deferrals (inert-control-floor honesty)

- **Live Hexalith.Memories Redis-Vector / FalkorDB `IDerivedStore` binding** — additive on this contract at M2
  (Story 12.16).
- ~~**Periodic scheduler trigger**~~ — **wired by Story 12.14 (2026-07-21).** `SweepAllTenantPairsAsync` runs from the
  existing `PeriodicEnforcementBackgroundService` on a configurable cadence (default once per UTC day, gated by
  `ChatBot:PeriodicEnforcement:RunM2AuditRecoverySweeps`), preserving the coordinator's fail-closed breach path. No
  parallel `BackgroundService` or Dapr timer was introduced. The token-gated
  `/health/chatbot/periodic-enforcement/m2` endpoint returns HTTP 503 unless every latest result is successful, fresh,
  breach-free, and covered; `release.yml` consumes it through the required topology-acceptance job.

## References

- Story 9.5 — `_bmad-output/implementation-artifacts/9-5-derived-store-cross-tenant-isolation.md`
- `_bmad-output/planning-artifacts/epics.md` — FR55a (115, 491), NFR9a (195), NFR59 (269), Story 9.5 (2438–2452)
- `_bmad-output/planning-artifacts/architecture.md` — derived-store backing (322–325), tenant isolation by construction
  (128, 342–344), M2 deferrals (306, 814, 866)
- [worm-audit-backing.md](worm-audit-backing.md) (Story 9.1), [replay-simulation-isolation.md](replay-simulation-isolation.md) (Story 9.4)
- `references/Hexalith.Memories/src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs`;
  `references/Hexalith.Memories/src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
