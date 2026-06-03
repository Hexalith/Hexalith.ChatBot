# ADR: WORM audit backing — per-tenant hash chain, separate-KMS redaction, key-shred erasure

## Status

Accepted (realized by Story 9.1, NFR49a; resolves architecture cross-cutting concern #13 — WORM-vs-GDPR-erasure).

## Context

The architecture mandates a tamper-evident audit store (NFR49a): *"append-only store with hash-chained envelopes per
tenant; redaction via key-destruction with the redaction key in a separate KMS; nightly chain verification."* It also
flags **cross-cutting concern #13**: tamper-evidence says *never mutate the log*, while GDPR right-to-erasure says
*erase this person's data*. These pull in opposite directions and must be reconciled by an architecture decision, not a
policy footnote. This ADR was deferred (per the architecture) to be authored alongside the post-commit store — which
Story 9.1 builds.

## Decision

### Per-tenant hash chain (append-only, no-delete contract)

- Each tenant has its own ordered chain. Every appended `WormAuditChainRecord` carries the SHA-256 `RecordHash` of a
  **deterministic canonical serialization** of the `AuditEnvelope` (fixed field order, invariant culture, UTC), plus
  the `PredecessorHash` of the prior record in the **same tenant** chain and a per-tenant monotonic `Sequence`. The
  genesis record (sequence 0) uses a fixed all-zero sentinel predecessor, never `null` (which would be ambiguous with
  an untracked predecessor).
- **Deletion and in-place mutation are impossible at the contract layer:** `IWormAuditStore` exposes only `AppendAsync`
  + tenant-partitioned reads — there is deliberately no update/delete/remove member (asserted by a reflection test, not
  left to convention).
- **Tenant isolation by construction (NFR9a):** chains are partitioned per tenant at the store-access layer; a read for
  one tenant can never observe or link another's records. M0 is single-tenant but a second tenant is additive.
- **Determinism is a correctness requirement** (Epic 8 carry-forward): re-hashing the same logical record is
  byte-stable, so tamper detection is reliable.

### Nightly per-tenant verification + 5-minute breach alert

- `WormAuditChainVerifier` is a pure, deterministic function: it recomputes every record's hash and asserts predecessor
  linkage and sequence continuity, returning a metadata-only result (status + reason code + safe first-break locator).
- **Fail-closed (no-fabrication):** a verification that cannot complete (store unavailable / enumeration throws) is
  reported as `Unknown` — a breach signal — never a silent `Verified`.
- `AuditChainVerificationCoordinator` follows the existing *pure-evaluator + fail-closed audit-then-deliver coordinator*
  discipline: on any breach it writes a metadata-only pre-commit audit envelope, then emits exactly one
  `OperatorAlertKind.AuditChainBroken` operator alert through the existing `IOperatorAlertSink`, carrying tenant ref,
  reason code, first-break locator, and correlation. The detection→emit path is synchronous within a pass, so the
  **5-minute detection budget** (`WormAuditChainVerifier.DetectionToAlertBudget`) holds by construction.

### GDPR erasure: appended redaction record + key-shred + projection tombstone (cross-cutting #13)

Erasure operates over the immutable chain **without ever mutating or deleting it**:

1. The original envelope is preserved as **opaque ciphertext** (`IEncryptedAuditOriginalStore`), encrypted under a
   per-subject key minted in a **separate KMS** (`IKmsRedactionKeyStore` — keys live in the KMS, ciphertext lives
   elsewhere; that separation is the whole point).
2. A metadata-only **redaction record is appended** to the chain (a normal chained append — the chain grows, never
   shrinks) referencing the redacted record by safe locator, the reason code, and the redaction-key handle.
3. Erasure = **key-shred + projection tombstone**: destroying the KMS key renders the encrypted original unrecoverable
   (crypto-shredding), and the subject's read-model projection is tombstoned so reads collapse to safe-not-found. The
   chain bytes are untouched, so verification still passes end-to-end after erasure (AC2 still holds).

### Metadata-only / no-leak floor (NFR2/NFR42)

Every chain record, redaction record, broken-chain alert, and hash input carries only `AuditMetadata`-safe bounded
tokens (ASCII alnum + `.-_:@|`, sensitive-marker ban). The encrypted original is the **only** place raw content may
live, and it is opaque ciphertext at rest.

### Production backing target vs dev/test implementation

- **Production:** an immutable / WORM object store (e.g. object-lock / append-only storage) backing the per-tenant
  chain, with redaction keys held in a real managed KMS (e.g. cloud KMS / HSM) so key-shred is an irrevocable KMS
  operation.
- **M0 dev/test:** in-process, seam-first implementations behind the same interfaces — `InMemoryWormAuditStore`,
  `InMemoryKmsRedactionKeyStore` (AES-256-GCM, key removed on shred), `InMemoryEncryptedAuditOriginalStore`,
  `InMemoryRedactionProjectionStore`. The contract and the redaction flow are identical; only the backing swaps.

## Consequences

- Tamper-evidence and GDPR erasure coexist: the log is never mutated, yet a subject's content becomes irrecoverable on
  key-shred and reads return safe-not-found.
- The chain is forward-compatible with replay isolation (Stories 9.2/9.4): a future `replay_run_id`-bearing record is
  just another chained append; nothing in 9.1 blocks adding it.
- **Runtime scheduler is deferred** (consistent with the Epic 7/8 inert-control-floor pattern): the verifier,
  coordinator, alert path, and erasure flow are fully built and tested, but no always-on `BackgroundService` /
  Dapr-timer is wired. A scheduler need only call `AuditChainVerificationCoordinator.VerifyAllTenantsAsync` per tenant
  on its cadence. This deferral is explicit, not a silent skip.

## Alternatives Considered

- **Erase by mutating/removing chain records.** Rejected: destroys tamper-evidence — the whole point of the chain.
- **Keep the redaction key alongside the ciphertext.** Rejected: defeats crypto-shredding; the separate-KMS boundary is
  what makes key-destruction a real erasure.
- **Hash without canonicalization.** Rejected: non-deterministic hashing was an explicit Epic 8 defect and would make
  tamper detection unreliable.

## Verification

- `WormAuditChainTests` — predecessor linkage, monotonic sequence, deterministic hashing, tenant isolation, no
  update/delete member, tamper detection.
- `WormAuditChainVerifierTests` — intact verifies; mutated record / broken link / sequence discontinuity report broken
  with correct reason + locator.
- `AuditChainVerificationCoordinatorTests` — broken chain audits-then-alerts exactly once within the 5-minute budget;
  verified emits nothing; unavailable store fails closed to `Unknown`; fail-closed audit suppresses the alert.
- `AuditRedactionServiceTests` — redaction grows the chain; original recoverable before shred, unrecoverable after;
  projection tombstone yields safe-not-found; chain still verifies after erasure.
- `WormAuditLeakTests` — no banned markers in any record/alert/redaction field or hash input.
- `ChainedAuditWriterTests` — fail-open-then-reconcile mapping; pre-commit gate unaffected.
