---
title: Hexalith.ChatBot PRD Validation Orientation — Current Source Extract
status: complete
created: "2026-09-14"
reviewer: source-and-downstream-integrity
---

# Validation Orientation — Current Source Extract

## Artifact under review

- PRD: `prd.md`, `status: final`, updated/finalized 2026-09-14.
- Normative appendices: `addendum.md`, `status: approved`, approved 2026-09-14.
- Canonical decision memory: `.memlog.md`, 46 append-only entries including the finalization event.
- Mutable evidence ledger: `qualification-evidence.md`, `status: evidence-gap`.
- Source registry: `source-manifest.md`, `status: current`, rooted at recorded workspace revision `76f355a038c4abdb3b9fdb3fb836c25053a18fb0`.

## Source identity and lineage

The four `inputDocuments` paths in `prd.md` exist. The two externally rooted direct-input hashes still match the manifest exactly:

| Source | Manifest SHA-256 | Current result |
| --- | --- | --- |
| Product brief | `d890417042c0329f168306936ae9b1004f2aa40c22f3e1d2845579db9b1ffd63` | exact match |
| Epic 12 recovery-provenance architecture spine | `e7a031bec0be1af92d981d86d06342e227ddb109f50a0b19f9800db3c50a02e7` | exact match |

The four historical validation inputs pinned in `source-manifest.md` also exist and match their recorded hashes: `orient-extract.md`, `review-adversarial-general.md`, `review-rubric.md`, and `validation-report.md`.

The checked-out root is now `2f40c47660a6e5bbe1f03e97d549261d2a3b6f9e`, rather than the manifest's recorded source snapshot. Projects, Parties, Tenants, FrontComposer, and Commons remain on their manifest pins. Folders is on the already-disclosed generated-client-only successor. Three additional checked-out dependencies have advanced without being disclosed in the manifest:

| Context | Manifest pin | Current checkout | Inspected delta |
| --- | --- | --- | --- |
| Conversations | `596cee6f…` | `589cdd71…` | planning/evidence scripts and nested pointer updates; no consumed Conversations contract file changed |
| EventStore | `7579b858…` | `e8886ec4…` | payload-protection planning/specification and evidence updates; no manifest-consumed public contract file changed |
| Memories | `d99bc963…` | `5829db42…` | architecture/memlog and nested pointer updates; no consumed ChatBot contract listed in the manifest |

These inspected deltas do not presently overturn the manifest's A6/A13 compatibility conclusions, but the manifest's `status: current` and PRD's present-tense “checked-out revisions” claim no longer describe the live checkout precisely.

## Authoritative product decisions extracted from the brief and memlog

- Product thesis: email-first governed AI collaboration for B2B project teams, because external collaborators already use email and should not need another portal.
- MVP proof loop: receive authorized project email, resolve a governed Project, store attachments, capture task intent, mediate AI action, approve risky effects, execute through bounded-context commands, and audit the outcome.
- Ownership: ChatBot owns orchestration and security-sensitive derived records; Projects, Conversations, Parties, Folders, Tenants, EventStore, and mail integration retain their declared source ownership.
- Association: deterministic evidence precedes AI inference; unresolved identity, scope, evidence, or confidence fails closed into authorized review.
- AI boundary: low-risk means only versioned read-only/no-external-effect subtypes; the six boundary-crossing effects always require human approval. The AI allowlist is deny-by-default.
- Delivery: one MVP release in dependency order M0 → M1 → M2. Safety, isolation, idempotency, atomic audit, and approval controls cannot be traded away for scope.
- Interactive chat: an M1 governed FrontComposer write surface through the shared command spine; no ungoverned free-form write path.
- Corrections: original associations remain immutable; affected derived context is invalidated, AI use is blocked during propagation, and completion requires store acknowledgements.
- Durable identity: operations and human decisions use lifetime-stable identities; request hashes are evidence or non-mutating suppression only.
- Product-brief scope changes are explicit rather than silent: general user upload, scheduled/file-addition automation, general “subject” workspaces, generic-channel breadth, unrestricted commands, and full task lifecycle are deferred or constrained.

## Current release and evidence state

- M0 and M1 are blocked by open A5, A6, and A13 gates.
- A5: no approved live-AI provider evidence bundle; live model invocation remains disabled.
- A6: no approved runtime data-class/KMS/payload-protection/erasure/backup/export-delete evidence; pilot data and external-party PII onboarding remain blocked.
- A13: Conversations execution/lifetime idempotency, Projects/Tenants/EventStore authority mapping, atomic-audit ownership, and write-path fencing remain unaccepted; M0 remains blocked.
- M2 is additionally blocked by A10 and A11.
- A10: recovery targets remain provisional; the Epic 12 architecture is accepted but `activation: pending`, and current-run completion evidence cannot substitute for operational recovery evidence.
- A11: all 15 candidate-bound SLO evidence rows remain `unsupported`; the addendum target rows and qualification rows pair one-to-one by metric name.
- Permitted claims are correctly limited to controlled pilot preview (M0), governed cross-surface pilot (M1), and production/release candidate only after M2 gates pass.

## Direct architecture input extracted

The current Epic 12 architecture contributes accepted product constraints without closing A10:

- AD-1 through AD-5 separate current-run completion evidence, diagnostics, and retained operational/A10 evidence; failures keep the transition check red.
- AD-6 assigns one globally unique pull-request completion-check identity and keeps activation claims conditional until branch protection and source identity are verified.
- AD-7 makes a complete aggregate cleanup receipt an attested input and rejects missing, duplicate, malformed, or incomplete cleanup observations.
- AD-8 pins repository-owned tool versions and immutable action references.
- AD-9 requires fresh-runner, job-local destructive-test isolation with no production credentials or canceling concurrency group.
- `activation: pending` is preserved consistently in the PRD, addendum, memlog, manifest, and qualification evidence.

## Normative contract extract for downstream work

- `prd.md` owns increment scope/gates, success metrics, workflow states, the stable operation catalog, context ownership, FRs/NFRs, and open decisions.
- `addendum.md` normatively owns association thresholds, detector/classifier detail, AI allowlists, the Tenant Policy Schema, the shared command pipeline, idempotency, replay isolation, ID evolution, authenticity/authority mapping, retry profiles, SLO target rows, and recovery qualification commitments.
- `qualification-evidence.md` reports mutable candidate/run evidence and cannot redefine product targets.
- `.memlog.md` is the durable decision/override history; it agrees with the final scope, gates, allowlist, ownership, and recovery-evidence separation.
- FR and NFR definition identifiers are unique; every explicit FR/NFR/A-token reference found in the PRD/addendum/evidence set resolves to a defined identifier.
- All 71 stable command names remain represented by a workflow or contract according to the prior finalized contract verification; spot checks of bootstrap, retry, correction, governed chat, data-subject, and notification families still resolve to the catalog.

## Reconciliation conclusion

Source intent, open-gate truthfulness, direct-input hashes, recovery-authority separation, stable identifiers, and the A11 row pairing are strong. The focused review found two high-severity downstream contract collisions plus three medium continuity/ownership issues and one low recovery-gate locator issue. They do not justify changing any open gate or claiming implementation evidence, but the two high items should be reconciled before generating or accepting implementation stories for M0 policy storage or AI-risk fallback.
