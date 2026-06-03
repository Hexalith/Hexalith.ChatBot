# ADR: Two-phase audit — pre-commit fail-closed gate vs post-commit fail-open-then-reconcile

## Status

Accepted (realized by Story 9.1, NFR49a / NFR15a; resolves architecture decision D4).

## Context

The chatbot must satisfy two requirements that, naively combined on a single write, contradict each other:

- **NFR15a — fail-closed governance:** a governed command must not produce a durable, observable side effect unless its
  authorization/policy decision was durably audited first. If audit is unavailable, the command is denied.
- **NFR49a — tamper-evident WORM audit chain:** every committed operation must be appended to a per-tenant,
  hash-chained, append-only audit store so the audit history is tamper-evident.

You **cannot** both *block the commit on audit* and *derive the hash chain from the committed write* on the same write:
the chain entry depends on the commit having happened, but a fail-closed gate would block the commit until the audit
(now including the chain entry) succeeded. That circularity is the contradiction architecture decision D4 names.

## Decision

Split audit into **two phases**, each with its own failure posture:

1. **Pre-commit = fail-closed gate.** Before a command produces any durable/observable side effect, its policy decision
   is written through `IAuditWriter.RecordPreCommitAsync`. If that write is unavailable, the command is **denied** — no
   side effect, no delivery. This is the NFR15a gate (already implemented across Epics 1–8; alerts via
   `OperatorAlertKind.AuditUnavailable`).

2. **Post-commit = fail-open-then-reconcile.** After the command commits, its post-commit envelope is recorded and
   appended to the WORM hash chain (`IWormAuditStore`, behind the `ChainedAuditWriter` decorator). Appending to the
   chain **must not block the commit**: on a chain-append failure the post-commit path returns
   `AuditWriteResult.Unavailable(...)`, which drives the gateway's existing reconcile machinery
   (`AuditReplayIntentKind.PostCommitAuditReconciliation` + `OperatorAlertKind.PostCommitAuditReconciliationRequired`,
   see `CommandGateway.cs:288-304`). The durable **event log is the source of truth**; a chain gap is recoverable by
   rebuilding the chain from the event log / reconciliation queue.

The hash chain therefore hangs off the **post-commit** seam (`RecordPostCommitAsync`), never the pre-commit gate. The
pre-commit gate is intentionally independent of chain append — verified by `ChainedAuditWriterTests`.

## Consequences

- The NFR15a × NFR49a contradiction is resolved: the gate stays fail-closed for policy decisions, while the chain stays
  fail-open so a transient WORM-store outage degrades to a reconcilable gap rather than a denial-of-service on commits.
- Recovery is well-defined: the event log reconstructs any missing chain records; the chain is derived state, not the
  primary record.
- A regression that makes the post-commit chain append fail-closed would re-introduce the contradiction. The
  decorator's fail-open mapping (`Unavailable` → reconcile, never throw, never deny) is the guardrail.

## Alternatives Considered

- **Single fail-closed audit covering both phases.** Rejected: re-introduces the D4 circularity and turns a WORM-store
  blip into a commit outage (violates the availability intent behind the two-phase split).
- **Best-effort post-commit with no reconciliation.** Rejected: a dropped chain entry would be a silent, permanent gap —
  incompatible with the tamper-evidence guarantee and the Epic 8 no-fabrication doctrine.

## Verification

- `tests/Hexalith.ChatBot.Server.Tests/Audit/ChainedAuditWriterTests.cs`: a failing WORM append returns `Unavailable`
  (reconcile path), the inner history write is unaffected, and the pre-commit gate never touches the chain.
- The existing gateway post-commit reconcile path (`CommandGateway.cs:288-317`) and its tests remain green.
