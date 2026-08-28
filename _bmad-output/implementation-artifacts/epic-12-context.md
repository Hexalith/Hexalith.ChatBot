# Epic 12 Context: Tamper-Evident Audit, Compliance Investigation & Recovery

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make compliance evidence defensible and operational recovery provable. This epic delivers tenant-isolated, tamper-evident audit with measurable reconstructability; safe investigation, replay, retention, export, erasure, and consent governance; derived-store isolation and correction handling; and live recovery exercises that validate continuity claims rather than relying on design-time assumptions.

## Stories

- Story 12.1: Tamper-evident WORM audit chain
- Story 12.2: Audit completeness as a production observable
- Story 12.3: Audit query and compliance investigation surface (S9)
- Story 12.4: Replay and simulation isolation
- Story 12.5: Derived-store cross-tenant isolation
- Story 12.6: Correction-driven vector reindexing
- Story 12.7: Data-class inventory and retention policy
- Story 12.8: Tenant export workflow
- Story 12.9: Deletion and erasure workflow
- Story 12.10: Consent and lawful-basis metadata
- Story 12.11: Continuity drill and RPO/RTO validation
- Story 12.12: Projection rebuild validation
- Story 12.13: Scoped outage degradation validation
- Story 12.14: Wire the M2 audit and recovery runtime scheduler
- Story 12.15: Stand up live recovery/continuity fault-injection drivers and recalibrate A10
- Story 12.16: Bind the live Hexalith.Memories derived-store backing

## Requirements & Constraints

- Audit history must be append-only and tamper-evident per tenant. Chain verification runs nightly, and a detected break must alert security operations within five minutes.
- Audit completeness means reconstructing each state-changing operation end to end from the audit chain, including inputs, decisions, resource references, policy snapshot, and outcome. The rolling seven-day target is at least 99.5%; a breach raises a P1 incident. Replay activity is excluded.
- Compliance investigation must support reconstruction across association, approval, command, correction, retry, and risky-AI activity while enforcing project-level authorization, safe redaction, and read/escalate-only compliance access.
- Every ChatBot-owned data class must have explicit ownership, retention, redaction sensitivity, minimization, export eligibility, and deletion behavior. Policy changes and consent or lawful-basis changes are authorized, versioned, and audited.
- Export and erasure are bounded, traceable workflows. Partial export files must never be exposed; incomplete operations remain visibly retryable or terminal. Erasure preserves immutable audit history through tombstones, appended redaction records, and key shredding, with proof retained for compliance.
- Replay, recovery, and fault injection must use dedicated test tenants, intercept all external effects, and prove that production state and outbound channels remain untouched. Derived stores must reject cross-tenant access at the storage layer; isolation-probe failure is stop-ship.
- Recovery targets are an RPO of at most 15 minutes and RTO of at most four hours, but remain provisional until supported by a retained hosted live-run locator. Projection rebuilds must be deterministic, tenant-scoped, complete within four hours for the baseline dataset, and require no mailbox re-ingestion.
- Dependency outages must remain scoped to the affected tenant, mailbox, operation, client, command surface, or workflow item. Recovery resumes from visible recoverable state without unauthorized mutation, silent loss, or duplicate side effects.

## Technical Decisions

- Audit uses two phases: pre-commit audit is a fail-closed admission gate; post-commit WORM audit is hash-chained and fail-open-then-reconcile from the immutable EventStore log. The event log remains the source for rebuilding audit and projections.
- GDPR erasure never mutates the audit chain. Personal data is made inaccessible with projection tombstones and cryptographic key destruction; redaction keys are held in a separate KMS and redaction itself is appended as evidence.
- Tenant identity comes from authenticated claims. Store keys, projections, caches, vector indexes, graphs, queries, logs, and error bodies enforce tenant isolation by construction rather than application filtering. Logs and telemetry contain metadata only.
- Derived records carry tenant, provenance, derivation-kernel version, redaction state, retention class, and schema version. Rebuilds resolve historical data as of the source version instead of querying current sibling-context state.
- Replay records carry a replay-run identity, remain outside default production queries and completeness calculations, and use an outbound adapter that records would-have-sent envelopes instead of sending them.
- DAPR delivery is at-least-once and unordered, so scheduled work, event handlers, probes, and rebuilds must be tenant-scoped, idempotent, observable, and order-tolerant. Missed cadences are themselves operational failures.
- Correction-driven reindexing is idempotent and source-version guarded. Production derived storage uses the Hexalith.Memories Redis-Vector/FalkorDB conventions, including deletion of isolation-probe sentinels.
- Live continuity evidence runs against the Aspire/DAPR sandbox with real fault injection. Current-run story-completion evidence is distinct from scheduled or release recovery evidence; raw results are staged outside retention and only sanitized, metadata-only canonical results are retained.

## UX & Interaction Patterns

- The audit investigation surface provides authorized search and reconstruction without mutation capability. Restricted resources are redacted without confirming their existence, while a safe escalation path remains available.
- Long-running export, deletion, rebuild, and recovery work exposes stable operation identity, current state, retry count, terminal reason, correlation context, and next safe action; partial failure is never presented as success.
- The compliance surface must meet WCAG 2.2 AA expectations for its increment, use non-color status cues, support keyboard and screen-reader operation, and preserve English/French parity.

## Cross-Story Dependencies

- Audit completeness and investigation build on the WORM chain and the shared audit envelope; erasure also depends on the chain's immutable redaction model.
- Retention inventory and policy classification precede export and erasure behavior; consent and lawful-basis rules can block governed AI or retention actions.
- Replay isolation and derived-store isolation become real release gates only after the durable M2 scheduler activates their tenant-scoped probes. That scheduler depends on the runtime governance control-plane loop.
- Continuity, rebuild, and scoped-outage coordinators require live Aspire/DAPR fault-injection drivers before their targets can be confirmed. Unreproducible production-scale scenarios remain explicit residual gaps.
- Derived-store isolation and correction reindexing depend on the live Hexalith.Memories binding; the asynchronous reindex decision determines whether a periodic overdue-work sweep is required.
