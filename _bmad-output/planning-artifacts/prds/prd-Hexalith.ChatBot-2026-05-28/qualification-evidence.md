---
title: Hexalith.ChatBot PRD Qualification Evidence
status: evidence-gap
created: "2026-09-14"
updated: "2026-09-14"
---

# Qualification Evidence

This artifact holds mutable, revision-specific implementation and hosted-run evidence. The PRD and addendum remain authoritative for product targets and evidence requirements.

## Current gate state

- No fresh hosted bundle identified by the reconciled sources passes the currently shipped four-job recovery gate.
- A10 remains provisional. M2 cannot claim production/release-candidate readiness until fresh exact-candidate controlled-loss evidence and an RTO-capable full-window or separately retained production-shaped drill exist.
- A11 calibration is incomplete. SLO rows without numeric targets, error budgets, live signals, routing, and burn tests are `unsupported` for production-readiness claims.

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

The qualifying bundle records the exact candidate revision, evidence-policy version, run locator, producer identity, timestamps, freshness calculation, controlled-loss persisted bounds, RTO drill duration, cleanup result, single-writer attestation, independent validation result, and stable failure reason. It must satisfy the recovery completion and operational-evidence separation defined in `addendum.md`.

Evidence owners: DevOps for hosted runs and retention; Test Architect for independent validation; System Architect for candidate/policy binding; Product Lead for the M2 release claim.
