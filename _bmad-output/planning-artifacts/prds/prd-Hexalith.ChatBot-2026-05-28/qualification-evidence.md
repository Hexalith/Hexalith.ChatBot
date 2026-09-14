---
title: Hexalith.ChatBot PRD Qualification Evidence
status: evidence-gap
created: "2026-09-14"
updated: "2026-09-14"
---

# Qualification Evidence

This artifact holds mutable, revision-specific implementation and hosted-run evidence. The PRD and addendum remain authoritative for product targets and evidence requirements.

## Current gate state

- A5 is open: no approved live-AI provider contract/evidence bundle for telemetry, training reuse, retention, region, and tenant-policy enforcement is attached. Live model invocation remains disabled.
- A6 is open: no approved data-class decision plus production KMS, Parties adapter, EventStore payload-protection/erasure, backup/restore, export/delete, and surviving-metadata runtime evidence is attached. Pilot data/PII onboarding remains blocked.
- A13 is open: the current Conversations append target has no production handler or accepted lifetime idempotency/concurrency/atomic-audit mapping; Tenants/EventStore/Projects role mapping and EventStore supported-write-path/fencing evidence also remain unaccepted. M0 remains blocked.
- No fresh hosted bundle identified by the reconciled sources passes the currently shipped four-job recovery gate.
- A10 remains provisional. The accepted Epic 12 recovery-provenance architecture is `activation: pending`; its current-run diagnostic and story-completion artifacts have no A10 authority. M2 cannot claim production/release-candidate readiness until the architecture activation preconditions are independently verified and a separate fresh exact-candidate controlled-loss bundle plus an RTO-capable full-window or separately retained production-shaped drill exist.
- A11 calibration is incomplete. SLO rows without numeric targets, error budgets, live signals, routing, and burn tests are `unsupported` for production-readiness claims.

## A11 candidate-bound SLO qualification

This table pairs one-to-one with `addendum.md` §Operating Baselines by metric name. `supported` is valid only when target, window, error budget, signal/provenance, alert route/receiver, burn-test result/date/locator, and exact M2 candidate revision are all present and independently verifiable. Any missing, stale, failing, or candidate-mismatched field derives `unsupported`.

| Metric name | Live signal / provenance | Alert route / receiver | Burn-test evidence / result / date | Exact M2 candidate | Gate state |
| --- | --- | --- | --- | --- | --- |
| `chatbot.command.execution.latency{command_class}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.shared_pipeline.overhead.latency{surface}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.candidate.generation.latency` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.operation.identity.latency` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.correction.propagation.latency{increment=m0|m1}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.correction.propagation.latency{increment=m2}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.audit.projection.lag` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.retry.exhausted.rate{operation_class}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.approval.queue.age{risk_class}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.mailbox.subscription.renewal` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.ingestion.latency` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.ambiguous.resolution.time` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.duplicate.suppression.rate{operation_class}` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.mailbox.failure.rate` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |
| `chatbot.ai.mediation.latency` | `unsupported-pending-a11` | `unsupported-pending-a11` | `unsupported-pending-a11` | `not-selected` | `unsupported` |

## Historical recovery bundle

| Field | Value |
| --- | --- |
| Required release run | `33066358280` |
| Candidate commit | `17aa94d` |
| Evidence run | `01M11EYSDMP1ZF38B7KZA1A6FA` |
| Run date | 2026-08-27 |
| Historical result | Nine manifests/reports, zero deviations, independent gate 1/1 at the gate revision then in force |
| Current validity | Historical only; missing the later `controlled-loss-path` job and expired under the eight-day freshness rule on 2026-09-04 |
| Measurable ceiling | 180 seconds; insufficient to test a four-hour RTO miss |
| RPO limitation | Ordinary no-loss paths reported constant `0s`; no cited fresh hosted controlled-loss measurement |

## Required fresh evidence

The qualifying operational bundle records the exact candidate revision, evidence-policy version, run locator, producer identity, timestamps, freshness calculation, controlled-loss persisted bounds, RTO drill duration, cleanup result, independent validation result, and stable failure reason. The Epic 12 transition diagnostic artifact and story-completion-authority artifact remain separate and cannot satisfy A10. The architecture's unique check identity, cleanup receipt, publication channels, immutable tool references, destructive-run isolation, and other activation preconditions require independent activation evidence before their authority may be claimed.

Evidence owners: DevOps for hosted runs and retention; Test Architect for independent validation; System Architect for candidate/policy binding; Product Lead for the M2 release claim.

## Required pre-pilot evidence

| Gate | Required evidence | Owner/approver |
| --- | --- | --- |
| A5 | Provider configuration and contract proving region, retention, telemetry, training/reuse, redaction, tenant binding, disable behavior, and negative tests for unauthorized context | Security + Architecture |
| A6 | Approved data-class matrix plus production KMS/secret custody, payload protection, key rotation/erasure, legal hold, backup/restore propagation, export/delete, surviving metadata, Parties integration, and independently witnessed runtime tests | Compliance/Data Protection + Architecture; Parties/EventStore owners supply implementation evidence |
| A13 | Owner-accepted versioned append/assignment/role/audit contracts; producer implementation; lifetime duplicate barrier or accepted append-only concurrency equivalent; current gateway authorization; provider ACL/ETag first-write fencing; fork/reorder/rebuild tests | System Architect + Conversations/Projects/Tenants/EventStore owners; Security approves authority and fencing |

Evidence must identify the exact candidate revision, contract/package versions, storage/provider profile, responsible producer, test runner, time, result, and expiry/reopen rule. A repository revision or interface-only contract does not satisfy these gates.
