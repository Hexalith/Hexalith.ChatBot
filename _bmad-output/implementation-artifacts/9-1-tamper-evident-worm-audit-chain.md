---
baseline_commit: 85e79aa9e39cebf45c32c4279b3d360423bd0f77
---

# Story 9.1: Tamper-evident WORM audit chain

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance owner,
I want an append-only, hash-chained audit store with GDPR-safe redaction,
so that audit history is tamper-evident yet honors erasure.

## Acceptance Criteria

1. **WORM append + per-tenant hash chain (NFR49a).**
   **Given** the WORM audit store,
   **When** an audit envelope is written,
   **Then** it is appended to a per-tenant hash chain — each appended record carries the cryptographic hash of its predecessor in the same tenant's chain — into a store whose contract makes deletion and in-place mutation impossible at the storage layer (append-only; no update/delete operation exists).

2. **Nightly per-tenant chain verification + 5-minute breach alert (NFR49a).**
   **Given** the nightly chain verification,
   **When** it runs per tenant,
   **Then** it recomputes each record's hash and predecessor linkage across the tenant chain, and **a broken chain alerts the on-call security engineer within 5 minutes** of detection (a metadata-only operator alert through the existing alert sink, fail-closed: a verification that cannot complete is itself a breach signal, never silent success).

3. **GDPR erasure by redaction record + key-shred, never chain mutation (NFR49a, architecture cross-cutting #13).**
   **Given** a GDPR right-to-erasure request,
   **When** processed,
   **Then** redaction is an **appended redaction record** (the original envelope is preserved encrypted, with the per-record/per-subject redaction key held in a **separate KMS**), and erasure operates by **projection tombstone + key-shred** (destroying the redaction key renders the encrypted original unrecoverable) — **never by mutating or deleting the audit chain**. After erasure the chain still verifies end-to-end (AC2 still passes), and redacted content is no longer recoverable.

### Cross-cutting requirements that hold for every AC

- **Metadata-only / no-leak floor (NFR2, NFR42).** Every new record, alert payload, and projection carries only safe bounded tokens via the existing `AuditMetadata` discipline (ASCII alnum + `.-_:@|`, marker-ban on `secret`/`password`/`bearer`/`token`/`exception`/file-extension sentinels). The encrypted original is the *only* place raw content may live, and it is opaque ciphertext at rest. No raw item content, recipient PII, prompts, claims, headers, or secrets appear in any cleartext record, hash input, alert, or log.
- **Tenant isolation by construction (NFR9a).** The chain is per-tenant; chain reads/verification/redaction are tenant-partitioned at the store-access layer so no operation can observe or link another tenant's records. M0 is single-tenant but **partitioned by construction** — a second tenant must be additive, not a rewrite.
- **Deterministic hashing.** The hash input is a canonical, stable serialization of the envelope (fixed field order, invariant culture, UTC). Re-hashing the same logical record always yields the same digest — non-deterministic hashing is a defect (Epic 8 carry-forward, Story 8.x).
- **Two-phase audit honored (D4).** This story is the **post-commit WORM chain** (NFR49a) and is **fail-open-then-reconcile-from-event-log**: chaining must not block the commit, and a chain gap is recoverable by rebuilding from the durable event log / reconciliation queue. It must not turn the post-commit path into a fail-closed gate (that would re-introduce the NFR15a × NFR49a contradiction the two-phase model resolves).

## Tasks / Subtasks

- [x] **Task 1 — Append-only WORM store abstraction with per-tenant hash chaining (AC: #1)**
  - [x] Add an `IWormAuditStore` seam under `src/Hexalith.ChatBot.Server/Audit/` exposing **append** + **tenant-partitioned read/enumerate** only — deliberately **no update/delete** member (deletion impossible at the contract layer, not merely by convention).
  - [x] Define the chained record type (e.g. `WormAuditChainRecord`) wrapping the existing `AuditEnvelope` plus: `Sequence` (per-tenant monotonic), `RecordHash`, `PredecessorHash`, and the canonical-serialization version. Populate the envelope's already-existing `AuditEnvelope.PredecessorHash` field (today always `null`) from the prior tenant-chain head.
  - [x] Implement a deterministic canonical hash (SHA-256 over a fixed-field-order, invariant-culture, UTC serialization of the envelope + predecessor hash + sequence). Genesis record (sequence 0) uses a fixed sentinel predecessor (e.g. all-zero / `"genesis"`), not `null`-ambiguous-with-untracked.
  - [x] Provide an in-process append-only implementation (mirror the `InMemoryAuditWriter` / `InMemory*` seam-first pattern) that holds a per-tenant ordered chain behind a lock; expose it as `IWormAuditStore` (and keep it usable as the test/dev default).
  - [x] Wire the WORM store **behind** the existing `IAuditWriter` post-commit path (decorator or composition over `InMemoryAuditWriter`) so `RecordPostCommitAsync` appends to the chain **fail-open**: on store failure, return `AuditWriteResult.Unavailable(...)` so the gateway's existing `QueueReplayIntentAsync` + `PostCommitAuditReconciliationRequired` reconcile path fires (see `CommandGateway.cs:288-304`). Do **not** make the pre-commit gate depend on chain append.
  - [x] Register via DI in `CommandGatewayServiceCollectionExtensions` following the existing audit/operator-alert registration shape.

- [x] **Task 2 — Nightly per-tenant chain verification + 5-minute on-call alert (AC: #2)**
  - [x] Add a **pure, deterministic** verifier (e.g. `WormAuditChainVerifier`) that, given a tenant's enumerated chain, recomputes each `RecordHash` and asserts predecessor linkage + sequence continuity, returning a metadata-only result (`verified` | `broken` + reason code + safe locator token of the first break, **never** envelope content).
  - [x] Treat an *incomplete* verification (store unavailable, enumeration throws) as a breach signal (fail-closed verification), never silent `verified` — apply the Epic 8 no-fabrication doctrine (prefer `broken`/`unknown` over fabricated success).
  - [x] Add a new `OperatorAlertKind.AuditChainBroken` and emit a metadata-only `OperatorAlert` through the existing `IOperatorAlertSink` on any break, carrying tenant ref, reason code, first-break locator token, and correlation — mirror the fail-closed *audit-then-deliver* coordinator discipline of `OperationalAlertWiringCoordinator` / `ReviewerBacklogAlertCoordinator`.
  - [x] Encode the **5-minute detection budget** as the alert's stated SLA and assert it in tests (detection→emit path is synchronous within a verification run); if a runtime scheduler (`BackgroundService`/`PeriodicTimer`) is wired, it runs the verifier per tenant and feeds the coordinator. If the periodic runtime trigger is deferred (consistent with the Epic 7/8 inert-control-floor pattern), the verifier + coordinator + alert path **must still be built and fully tested**, and the deferral documented explicitly in Completion Notes (do not silently skip).
  - [x] Use `ISystemClock` for all timestamps (deterministic tests; 30 existing call-sites establish the pattern).

- [x] **Task 3 — GDPR erasure: appended redaction record + KMS key-shred + projection tombstone (AC: #3)**
  - [x] Add an `IKmsRedactionKeyStore` seam representing the **separate KMS** boundary: create/hold a per-record (or per-subject) redaction key, encrypt the original envelope payload under it, and **shred** (irrevocably destroy) the key on erasure. Provide an in-process implementation for dev/test; document the production KMS boundary in the ADR (Task 5).
  - [x] On a redaction/erasure request, **append** a `redaction record` to the WORM chain (it is a normal chained append — it advances the chain and carries its own predecessor hash) that references the redacted record by safe locator token, the redaction reason code, and the redaction-key handle — and store the encrypted original (ciphertext only, opaque at rest). Never mutate or remove the original chained record.
  - [x] Implement erasure as **projection tombstone + key-shred**: mark the read-model/projection for the subject as tombstoned (so reads return safe-not-found), and shred the redaction key so the encrypted original is unrecoverable. The chain bytes are untouched.
  - [x] Assert post-erasure invariant: `WormAuditChainVerifier` still returns `verified` for the tenant (AC2), and the redacted content is no longer recoverable (decrypt fails / key absent).
  - [x] Keep all redaction-record fields and the tombstone metadata-only (`AuditMetadata` / `CoarseUserFacingRedactionStage.MetadataOnlyDecision`); the ciphertext is the only carrier of original content.

- [x] **Task 4 — Tests (AC: #1, #2, #3)**
  - [x] Tier-1 unit tests under `tests/Hexalith.ChatBot.Server.Tests/Audit/`: chain append links predecessor hash; sequence is monotonic per tenant; **deletion/update is not expressible** (compile-time: no such member) and tampering a record is detected by the verifier; hashing is deterministic across repeated serialization; tenant chains are isolated (no cross-tenant linkage/read).
  - [x] Verifier tests: intact chain → `verified`; mutated record / broken link / missing sequence → `broken` with correct reason + first-break locator; unavailable store → `broken`/`unknown`, never silent success; broken chain emits exactly one `AuditChainBroken` operator alert with metadata-only payload within the asserted 5-minute SLA.
  - [x] Redaction tests: redaction appends (chain grows, never shrinks); original recoverable before shred, **unrecoverable after key-shred**; projection tombstone yields safe-not-found; **chain still verifies after erasure**.
  - [x] Leak tests: no record/alert/projection/hash-input field carries banned tokens (extend the existing serialization/no-leak assertions used across Epic 8 payloads).
  - [x] Fail-open test: a failing WORM append returns `Unavailable` and triggers the gateway reconcile/alert path **without** blocking the commit (assert pre-commit gate unaffected).

- [x] **Task 5 — ADRs + docs (AC: #1, #2, #3)**
  - [x] Author `docs/adrs/audit-two-phase.md` (currently `docs/adrs/` is empty though the architecture enumerates it) capturing the pre-commit fail-closed gate vs post-commit fail-open-then-reconcile decision as realized by this story.
  - [x] Author `docs/adrs/worm-audit-backing.md` — the deferred "WORM audit backing technology" ADR the architecture says to author **before** the post-commit store: record the per-tenant hash-chain design, the append-only/no-delete contract, the separate-KMS redaction-key boundary, key-shred + projection-tombstone erasure resolution of cross-cutting #13, and the production backing target (immutable/WORM object store) vs the in-process dev/test implementation.

## Dev Notes

### What this story actually changes (and what already exists)

This is the **first real WORM chain** — Epics 1–8 emitted audit *envelopes* but never chained, verified, or KMS-redacted them. Most of the seam already exists; do **not** reinvent it:

- **`AuditEnvelope` already carries `PredecessorHash`** (`src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs:21`) — it is wired through `AuditEnvelopeFactory` but **always set to `null` today** (see every `PredecessorHash: null` in `AuditEnvelopeFactory.cs`). This story makes that field real for chained appends; the factory continues to construct envelopes, the **WORM store** assigns the predecessor hash at append time. Do not change the factory's public construction shape unless chaining requires it.
- **`IAuditWriter`** (`src/Hexalith.ChatBot.Server/Gateway/Stages/IAuditWriter.cs`) is the post-commit seam: `RecordPostCommitAsync`. The chain hangs off this, not off a new pipeline.
- **`InMemoryAuditWriter : IAuditWriter, IAuditHistoryReader`** (`.../Stages/InMemoryAuditWriter.cs`) is the current default; it already partitions reads by tenant + command evidence ref. Compose/decorate it — keep `IAuditHistoryReader` working (it backs the Story 1.9 UI audit-history surface).
- **`AuditWriteResult`** (`.../Audit/AuditWriteResult.cs`): `Success` / `Unavailable(reasonCode)`. Reuse for fail-open append.
- **Fail-open-then-reconcile is already implemented in the gateway** (`CommandGateway.cs:288-304`): on a failed post-commit write it queues `AuditReplayIntentKind.PostCommitAuditReconciliation` and raises `OperatorAlertKind.PostCommitAuditReconciliationRequired`. The WORM append must slot into this existing path — return `Unavailable` and let the established reconcile machinery handle gaps. **Never** make the commit depend on the chain.
- **Operator alerting**: `IOperatorAlertSink` + `OperatorAlert` + `OperatorAlertKind` (`.../Audit/`). Add `AuditChainBroken` to the enum; emit through the sink. `InMemoryOperatorAlertSink` is the test/dev sink.
- **Clock**: `ISystemClock` (`.../Audit/ISystemClock.cs`, `SystemClock.cs`) — use for every timestamp; ~30 call-sites establish this for deterministic tests.
- **Metadata-only token hygiene**: `AuditMetadata` (`SafeOptionalToken`, `IsSafeStableIdentifier`, `SafeCommandName`, `SafeActorType`) + `CoarseUserFacingRedactionStage.MetadataOnlyDecision`. Every Epic 7/8 audit/alert payload uses these — follow exactly.
- **Coordinator pattern for the verification→alert path**: `OperationalAlertWiringCoordinator` and `ReviewerBacklogAlertCoordinator` (`src/Hexalith.ChatBot.Server/Notifications/`) are the canonical *pure-evaluator + fail-closed audit-then-deliver coordinator* shape — mirror it for the nightly verifier + broken-chain alert.

### Architecture constraints (must follow)

- **Location**: everything lands in the `Audit/` seam — `src/Hexalith.ChatBot.Server/Audit/` (the architecture marks it: *"[M0] seam: pre/post-commit, WORM hash-chain, replay traces [M2]"*). The KMS boundary is a port like the other adapter ports. Governance/audit stage interfaces stay `internal` to `.Server` (NetArchTest-enforced — no `*.Cli`/`*.Mcp`/`*.UI` type may reference `IAuditWriter` and friends). [Source: architecture.md#Internal Decomposition, #Architectural Boundaries]
- **Two-phase audit (D4)**: pre-commit = fail-closed gate; **post-commit WORM hash-chain = fail-open-then-reconcile** (event log is source of truth; chain rebuilt from it on recovery — *"cannot block-the-commit AND derive-the-chain on the same write"*). This story implements only the post-commit chain. [Source: architecture.md:369-372, :143-147]
- **Data boundary**: *"WORM audit chain in a dedicated append-only store with redaction keys in a separate KMS. Cross-tenant queries impossible at the store-access layer (NFR9a)."* [Source: architecture.md:693-696]
- **WORM backing (Infra)**: *"append-only store with hash-chained envelopes per tenant; redaction via key-destruction with the redaction key in a separate KMS (resolves WORM-vs-GDPR-erasure, cross-cutting #13); nightly chain verification."* [Source: architecture.md:392-394]
- **Cross-cutting #13 — WORM-vs-erasure (GDPR)**: tamper-evidence says "never mutate the log"; GDPR says "erase this person's data." Resolution = crypto-shredding / redaction-by-key-destruction / projection tombstones over an immutable chain — *an architecture decision, not a policy footnote*. [Source: architecture.md:167-169]
- **Envelope minimum fields** (already in `AuditEnvelope`): `tenantId, actorId, actorType, commandName, resourceId, decision, reasonCode, correlationId, timestamp, policySnapshotId, sourceEvidenceRefs, idempotencyKey?, stateTransition, redactionDecision, outcome, phase, schemaVersion, predecessorHash, surfaceOrigin`. The hash input must be a canonical serialization of these. [Source: architecture.md:522-525]
- **Replay note (forward-looking, not this story's scope)**: replay envelopes will carry `replay_run_id` and be excluded from production audit queries + NFR50a completeness (Stories 9.2/9.4, addendum §Replay Isolation). 9.1 just must not design the chain in a way that blocks adding a `replay_run_id`-bearing record later. [Source: addendum.md:102-107]

### Previous-work intelligence — Epic 8 retro lessons (apply directly)

- **No-fabrication / fail-safe doctrine is the spine of recent work.** Prefer `broken` / `unknown` over a fabricated `verified`. A verification that cannot complete is a breach, not a pass. (8.1 was caught fabricating health; every later story preferred no-data over invented data.)
- **Deterministic hashing.** Non-deterministic hashing was an explicit defect. Canonicalize serialization (fixed order, invariant culture, UTC) so re-hash is byte-stable.
- **Bookkeeping drift is the #1 recurring defect across Epics 7–8** — stale debug-log test counts and **File List omissions** recurred in 4 of 5 Epic 8 stories. Keep the **File List exhaustive** (every new + modified file, incl. test files) and any cited test counts accurate. This is the most common review auto-fix; pre-empt it.
- **Define-once / reuse.** Consume existing seams (`IAuditWriter`, `IOperatorAlertSink`, `ISystemClock`, `AuditMetadata`, the gateway reconcile path) by reference — do not re-derive thresholds, clocks, or token rules.
- **Inert-control-floor awareness.** Epics 7/8 repeatedly built evaluators/coordinators but deferred the periodic runtime trigger. For 9.1 the verifier + alert path are **core ACs and must be built + tested**; if the *scheduler* wiring is deferred, say so explicitly in Completion Notes — never let a deferral read as "done."
- **Metadata-only generalizes cleanly** — one `IsSafeToken`/marker-ban posture applied to every payload; serialization tests assert no secret sentinels leak. Extend the same assertions to the new chain record, redaction record, and broken-chain alert.

### Project Structure Notes

- New types belong in `src/Hexalith.ChatBot.Server/Audit/` (chain store, verifier, redaction/KMS port, chain record, new alert kind) and `src/Hexalith.ChatBot.Server/Notifications/` only if a coordinator is added (mirror existing coordinators). DI registration in `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`.
- Tests in `tests/Hexalith.ChatBot.Server.Tests/Audit/` (existing dir: `AuditEnvelopeFactoryOperationalAlertTests.cs`, `ComplianceAuditReadPolicyTests.cs`).
- ADRs in `docs/adrs/` (directory exists per architecture but is currently empty — this story seeds it).
- No conflict with unified structure detected: the `Audit/` seam and KMS port placement match the architecture's prescribed homes exactly.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.1 (lines 2362-2380)]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 9 (lines 2358-2360); NFR49/NFR49a (lines 254-255)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-cutting concern 13 (lines 167-169)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Two-phase audit (lines 143-147, 369-372)]
- [Source: _bmad-output/planning-artifacts/architecture.md#WORM audit backing / Infrastructure (lines 392-394)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data boundaries (lines 693-696); Audit envelope minimum fields (lines 522-525)]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Replay Isolation (lines 102-107)]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditWriteResult.cs; AuditCommitPhase.cs; ISystemClock.cs; IOperatorAlertSink.cs; OperatorAlert.cs; OperatorAlertKind.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/IAuditWriter.cs; InMemoryAuditWriter.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs (post-commit fail-open path, lines 288-317)]
- [Source: src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs; ReviewerBacklogAlertCoordinator.cs (coordinator pattern)]
- [Source: _bmad-output/implementation-artifacts/epic-8-retro-2026-06-03.md (no-fabrication, deterministic-hashing, bookkeeping-drift, inert-control-floor lessons)]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (Claude Opus 4.8, 1M context)

### Debug Log References

- `dotnet build src/Hexalith.ChatBot.Server` → succeeded, 0 warnings.
- `dotnet test tests/Hexalith.ChatBot.Server.Tests` (full server suite) → 1135 passed, 0 failed (47 of them new for Story 9.1).
- `dotnet test tests/Hexalith.ChatBot.Architecture.Tests` → 37 passed (internal-seam / boundary enforcement unaffected).
- `dotnet test tests/Hexalith.ChatBot.Conformance.Tests` → 75 passed (audit/no-leak conformance unaffected).

### Completion Notes List

- **AC1 (WORM append + per-tenant hash chain).** Added the `IWormAuditStore` seam (append + tenant-partitioned
  read/enumerate only — no update/delete member, asserted at compile-time via a reflection test), `WormAuditChainRecord`
  (envelope + per-tenant monotonic `Sequence` + `RecordHash` + `PredecessorHash` + canonical-serialization version), the
  deterministic `WormAuditChainHasher` (SHA-256 over a fixed-field-order/invariant-culture/UTC canonical envelope +
  predecessor + sequence; genesis uses a 64-zero sentinel, never `null`), and `InMemoryWormAuditStore`. The store sets
  the envelope's previously-always-`null` `PredecessorHash` at append time. Wired behind the post-commit seam via the
  `ChainedAuditWriter` decorator (preserves the Story 1.9 `IAuditHistoryReader` surface) and registered in
  `CommandGatewayServiceCollectionExtensions`.
- **AC2 (verification + 5-minute alert).** Added the pure `WormAuditChainVerifier` (recomputes each hash, asserts
  predecessor linkage + sequence continuity; metadata-only result with reason code + safe `seq:N` first-break locator),
  `OperatorAlertKind.AuditChainBroken`, `AuditEnvelopeFactory.AuditChainBroken`, and the
  `AuditChainVerificationCoordinator` (fail-closed audit-then-deliver: writes the pre-commit envelope, then emits exactly
  one metadata-only alert; an enumeration that throws is reported `Unknown` — a breach — never silent `Verified`). The
  5-minute SLA is encoded as `WormAuditChainVerifier.DetectionToAlertBudget` and asserted in tests.
- **AC3 (GDPR erasure).** Added the separate-KMS boundary `IKmsRedactionKeyStore` (in-process `InMemoryKmsRedactionKeyStore`
  uses AES-256-GCM and removes/zeroes the key on `Shred`), `IEncryptedAuditOriginalStore` (opaque ciphertext at rest,
  separate from the KMS), `IRedactionProjectionStore` (tenant-partitioned tombstone → safe-not-found), and
  `AuditRedactionService` + `AuditEnvelopeFactory.AuditRecordRedacted`. Redaction is an appended chained record (chain
  grows, never shrinks); erasure = key-shred + projection tombstone; the original is recoverable before shred and
  unrecoverable after; the chain still verifies end-to-end after erasure.
- **Cross-cutting.** All new records, alerts, and the hash input carry only `AuditMetadata`-safe tokens (leak tests
  assert no banned markers); the encrypted original is the only carrier of raw content. Chains are tenant-partitioned by
  construction (NFR9a). `ISystemClock` used for every timestamp.
- **Deferral (explicit, not a silent skip).** Consistent with the Epic 7/8 inert-control-floor pattern, **no always-on
  `BackgroundService` / Dapr-timer scheduler is wired**. The verifier, coordinator, alert path, and erasure flow are
  fully built and tested; a periodic runtime need only call `AuditChainVerificationCoordinator.VerifyAllTenantsAsync` on
  its cadence. Documented in `docs/adrs/worm-audit-backing.md`.
- **ADRs.** Seeded the previously-absent `docs/adrs/` with `audit-two-phase.md` (D4 pre-commit-fail-closed vs
  post-commit-fail-open-then-reconcile) and `worm-audit-backing.md` (per-tenant hash chain, separate-KMS redaction,
  key-shred + tombstone erasure resolving cross-cutting #13, production WORM/KMS target vs in-process dev/test).

### File List

**Added — production (`src/Hexalith.ChatBot.Server/Audit/`):**
- `WormAuditChainRecord.cs`
- `WormAuditChainHasher.cs`
- `WormAuditAppendOutcome.cs`
- `IWormAuditStore.cs`
- `InMemoryWormAuditStore.cs`
- `WormAuditChainVerificationResult.cs`
- `WormAuditChainVerifier.cs`
- `AuditChainVerificationCoordinator.cs`
- `IKmsRedactionKeyStore.cs`
- `InMemoryKmsRedactionKeyStore.cs`
- `IEncryptedAuditOriginalStore.cs`
- `InMemoryEncryptedAuditOriginalStore.cs`
- `IRedactionProjectionStore.cs`
- `InMemoryRedactionProjectionStore.cs`
- `AuditRedactionService.cs`

**Added — production (other):**
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChainedAuditWriter.cs`

**Modified — production:**
- `src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs` (added `AuditChainBroken`)
- `src/Hexalith.ChatBot.Server/Audit/OperatorAlert.cs` (added optional `FirstBreakLocator`)
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (added `AuditChainBroken` + `AuditRecordRedacted`)
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` (WORM/KMS/redaction DI wiring)

**Added — tests (`tests/Hexalith.ChatBot.Server.Tests/Audit/`):**
- `WormAuditTestData.cs`
- `WormAuditChainTests.cs`
- `WormAuditChainVerifierTests.cs`
- `AuditChainVerificationCoordinatorTests.cs`
- `AuditRedactionServiceTests.cs`
- `ChainedAuditWriterTests.cs`
- `WormAuditLeakTests.cs`
- `InMemoryKmsRedactionKeyStoreTests.cs`
- `InMemoryEncryptedAuditOriginalStoreTests.cs`
- `WormAuditChainDependencyInjectionTests.cs`

**Added — docs:**
- `docs/adrs/audit-two-phase.md`
- `docs/adrs/worm-audit-backing.md`

## Change Log

| Date       | Version | Description                                                                                          | Author |
|------------|---------|------------------------------------------------------------------------------------------------------|--------|
| 2026-06-03 | 0.1     | Implemented Story 9.1: tamper-evident WORM per-tenant hash chain, nightly verifier + 5-minute broken-chain alert, GDPR erasure by appended redaction record + separate-KMS key-shred + projection tombstone, two ADRs. All ACs satisfied; 47 new tests; full server suite 1135 green. | Amelia (dev agent) |
| 2026-06-03 | 0.2     | Adversarial code review (Story-automator): 0 critical/high. Fixed bookkeeping drift — File List was missing 3 test files (`InMemoryKmsRedactionKeyStoreTests`, `InMemoryEncryptedAuditOriginalStoreTests`, `WormAuditChainDependencyInjectionTests`) and the test counts were stale (28→47 new, 1116→1135 total). Build 0 warnings; server 1135 / architecture 37 / conformance 75 all green. Status → done. | Jérôme Piquot (review) |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Outcome:** Approved (auto-fix applied)

### Verdict

All three Acceptance Criteria are genuinely implemented and every task marked `[x]` is backed by real code and real assertions. Build is clean (0 warnings); the three claimed suites pass (server 1135, architecture 37, conformance 75). No CRITICAL or HIGH findings — no false `[x]`, no missing AC, no security/leak gap.

### AC validation (git reality vs claims)

- **AC1 — WORM append + per-tenant hash chain.** `IWormAuditStore` exposes only `AppendAsync` + tenant-partitioned reads (no delete/update member, enforced by `WormStoreContractExposesNoDeleteOrUpdateMember`). `WormAuditChainHasher` is deterministic SHA-256 over a fixed-order/invariant-culture/UTC canonical envelope + predecessor + sequence; genesis uses a 64-zero sentinel, never `null`. `ChainedAuditWriter` composes the chain behind the post-commit seam and preserves the Story 1.9 `IAuditHistoryReader`. Verified.
- **AC2 — nightly verification + 5-minute breach alert.** `WormAuditChainVerifier` (pure) re-hashes each record and checks predecessor linkage + dense sequence; `AuditChainVerificationCoordinator` is fail-closed audit-then-deliver (an enumeration that throws → `Unknown` breach, never silent `Verified`) and emits exactly one metadata-only `AuditChainBroken` alert. Verified.
- **AC3 — GDPR erasure by redaction record + key-shred.** `AuditRedactionService` appends a redaction record (chain grows, never shrinks), preserves the original as AES-256-GCM ciphertext under a separate-KMS key, and erases by key-shred + projection tombstone; `ChainStillVerifiesEndToEndAfterErasure` proves AC2 still holds post-erasure. Verified.
- **Cross-cutting** — metadata-only/no-leak (leak tests assert banned-marker absence on records, alert, and hash input), per-tenant isolation (ordinal-keyed chains/projections), deterministic hashing, and two-phase fail-open (`Unavailable` → existing reconcile path). Verified.

### Findings

- **[MEDIUM · fixed]** Bookkeeping drift (the exact Epic 7/8 recurring defect): File List omitted 3 committed test files and the Debug Log/Change Log test counts were stale (claimed 1116/28-new vs actual 1135/47-new). File List and counts corrected.
- **[LOW · noted, not changed]** `WormAuditChainHasher.CanonicalizeEnvelope` reuses the field separator (``) to join `SourceEvidenceRefs`. No exploitable collision exists (the single positional `IdempotencyKey` field always emits its own trailing separator, so the refs/idem boundary stays recoverable), but a distinct delimiter or length-prefix would be more robust. Not changed — altering it would rehash every record for no functional gain.
- **[LOW · noted]** `AuditEnvelopeFactory.AuditRecordRedacted` adds `redaction-subject:{subjectRef}` beyond the three fields AC3 enumerates (locator, reason, key-handle). It is a bounded `AuditMetadata`-safe token and an erasure log of *which* subject was erased is defensible, so left as-is.
- **[LOW · noted]** The AC2 five-minute SLA assertion is satisfied by-construction (synchronous detection→emit under a `FixedClock`), so the test is effectively tautological. Acceptable given the periodic scheduler is an explicitly-documented inert-control-floor deferral.

### Deferral confirmed

No always-on `BackgroundService`/Dapr-timer scheduler is wired; the verifier + coordinator + alert + erasure paths are fully built and tested, and a scheduler need only call `VerifyAllTenantsAsync` on its cadence. Documented in Completion Notes and `docs/adrs/worm-audit-backing.md` — consistent with the Epic 7/8 inert-control-floor pattern, not a silent skip.
