---
title: Adversarial Final Review v3 - Hexalith.ChatBot PRD
status: complete
created: "2026-09-14"
reviewedArtifacts:
  - "prd.md"
  - "addendum.md"
  - "source-manifest.md"
  - "qualification-evidence.md"
  - "reconcile-current-sibling-contracts-2026-09-14.md"
  - "reconcile-sibling-contexts-2026-09-14.md"
reviewLens: "post-v2-remediation hostile launch-readiness"
---

# Adversarial Final Review v3 — Hexalith.ChatBot PRD

## Verdict

**STOP — document defects: Critical 0, High 1, Medium 1.** The v2 remediation closes the eight named gate defects: the cataloged lifecycle commands now map to authoritative transitions, the ordinary governance/data-subject/retry mutations enter the command spine, the M0 row carries the full A13 bundle and producer approvers, A11 has a candidate-bound evidence schema, the audit topology is singular, repeated increment prose defers to the sole gate, governed-chat cancellation has a first-writer contract, and the source manifest contains a Projects row plus a current pointer.

One externally promised high-risk admin capability remains outside the supposedly complete stable operation catalog: FR74's disable/quarantine/rate-limit matrix is broader than the commands and policy schema that implement it. A smaller source-lineage inconsistency remains because the manifest's new `recheckArtifact` points to an intentionally narrow H4/H12 review while canonical A13 still names the older superseded full-sibling extract as an exact current gap source.

The product also remains **stop-ship on honest external/evidence gates**: A5, A6, and A13 block M0/M1 onboarding/live AI, while A10 and A11 additionally block M2 production/release-candidate approval. Those five open gates are not counted as document defects.

## High document-contract finding

### H1 — FR74 and FR75a still exceed the “complete” admin/governance command contract

**Location:** `prd.md` §Shared Workflow Contract lines 542–559; §Command and Query Contracts lines 806–883; FR74/FR75a lines 1304–1312; `addendum.md` §Tenant Policy Schema lines 91–113.

**Evidence:**

- FR74 promises administrators can **disable, quarantine, or rate-limit** mailbox sources, service clients, AI actors, and command capabilities. Its decomposition guidance then says there are five subject classes by adding outbound, producing a direct four-versus-five scope contradiction.
- The catalog/table now covers mailbox disable, service-client revoke, command allowlist removal, and generic mailbox/AI/command/outbound rate or circuit limits. It does not define how an AI actor is disabled, how any of those actor/capability subjects is quarantined, or whether outbound is actually the fifth subject. The closed Tenant Policy Schema has no quarantine or AI-actor-enable/disable knob from which `UpdateTenantPolicy` could derive those transitions.
- FR75a says ChatBot admin-role assignment is itself a security-sensitive audited mutation, and §Owner-authority mapping requires an explicit ChatBot admin-role grant. No stable ChatBot command/transition creates, changes, or revokes that grant. If the operation is owner-context-only, the PRD does not map it to an owner target under A13.

**Impact:** Downstream stories must invent authority-expanding and emergency-control operations despite the catalog's exhaustiveness claim. Different surfaces can implement different meanings for quarantine/disable, and admin-role assignment can become a hidden mutation path without a named transition, separation-of-duty rule, or stable conflict result.

**Required fix:** Decide whether FR74 has four or five subject classes. Add an explicit control matrix mapping every retained `(subject, disable|quarantine|rate-limit)` cell to one stable command, policy knob, actor/co-approver, state transition, event, and re-enable/release rule; remove unsupported cells instead of leaving them aspirational. Add a ChatBot admin-role grant/change/revoke command family or bind it explicitly to a versioned Tenants/identity owner contract under A13.

## Medium document-contract finding

### M1 — No single current reconciliation artifact supports the manifest/A13 pointer chain

**Location:** `source-manifest.md` frontmatter `recheckArtifact`, lines 41 and 71; `prd.md` A13; `reconcile-current-sibling-contracts-2026-09-14.md` frontmatter/scope; `reconcile-sibling-contexts-2026-09-14.md`.

The manifest correctly marks the initial five-gap extraction as historical and supersedes its former A8 shorthand. It now points `recheckArtifact` to `reconcile-current-sibling-contracts-2026-09-14.md`, but that artifact declares a narrow H4/H12/source-lineage scope and does not itself provide the full nine-context/A13 re-check. Meanwhile canonical A13 still says its exact gaps are in the older `reconcile-sibling-contexts-2026-09-14.md`, whose resolved PRD-wording findings and obsolete A8 label the manifest says not to use for current status.

**Impact:** The source-manifest table is sufficient to keep the release blocked, so this is not a gate bypass. It does make the re-check provenance non-single-valued: a reviewer cannot follow one declared current artifact for all append, assignment, Projects/Tenants authority, EventStore audit/fencing, and A6 boundary conclusions.

**Required fix:** Produce or designate one refreshed full-sibling/A13 reconciliation, point both `source-manifest.md` frontmatter and PRD A13 to it, and leave the two older extracts explicitly historical/narrow. Do not revive the old A8 executable-mapping label.

## Targeted remediation verification

| Target | Result | Evidence |
| --- | --- | --- |
| Lifecycle/catalog bijection | **Pass for listed commands** | All 63 catalog commands appear in an authoritative association, owned-workflow, or governance transition. `Deferred` resume is exclusive to `ResumeEmailAssociationReview`; approval decisions, family-specific retries, and projection rebuild are explicit. |
| Exhaustive admin/governance commands | **Fail at FR74/FR75a boundary** | Core policy, mailbox, queue, service-client, decision, export/erasure/hold/retention, notification, and retry paths are present. H1 identifies the retained unmatched promises. |
| Full M0 A13 bundle and approvers | **Pass** | The sole M0 row names append/concurrency, assignment, role/permission mapping, atomic-audit ownership, ACL/fencing, Conversations/Projects/Tenants/EventStore owners, Architecture, and Security. Missing approval disables onboarding/live AI. |
| A11 closure schema | **Pass as an honest open evidence gate** | The addendum defines all required dimensions and binds each metric one-to-one to `qualification-evidence.md`, which has signal/provenance, alert receiver, burn-test evidence, exact candidate, and derived gate state. All rows are currently `unsupported`, correctly blocking M2. |
| Canonical audit topology/concurrency | **Pass as a product contract; A13 open** | Domain event, durable idempotency, policy/approval references, and canonical envelope co-commit. Envelopes hash-link per aggregate under serialized/fenced writes; tenant checkpoints anchor heads; a secondary WORM ledger cannot replace the atomic boundary. Current platform support remains explicitly blocked by A13. |
| Increment prose | **Pass** | The M0/M1/M2 summary and resource prose now point to the sole gate and retain inherited A5/A6/A13 plus A10/A11 timing. |
| Governed-chat cancellation | **Pass** | `Admitted -> Cancelled` uses `CancelGovernedChatRequest`; cancellation-versus-streaming and stop-versus-completion use expected revision and first-commit-wins; `CancelAIAction` is separately scoped. |
| Source-manifest Projects row/pointer | **Partial** | The Projects contract/hash/status row is present and A13-scoped. M1 covers the remaining pointer topology defect. |

## Explicit open stop-ship gates — not document defects

| Gate | Honest current disposition |
| --- | --- |
| A5 | No approved live-provider telemetry/training/reuse/retention/region/tenant-policy evidence; live AI and pilot onboarding remain disabled. |
| A6 | No approved data-class plus production KMS/payload protection/erasure/backup/export/delete/surviving-metadata runtime proof; pilot PII/data onboarding and GDPR claims remain blocked. |
| A13 | No accepted executable Conversations append/lifetime concurrency, full owner authority, atomic audit/idempotency ownership, or provider fencing proof; M0 remains blocked. |
| A10 | The historical recovery bundle is expired and cannot prove controlled loss or the four-hour RTO boundary; M2 remains blocked. |
| A11 | Every candidate-bound SLO evidence row is `unsupported`; M2 SLO/production-readiness claims remain blocked. |

## Prior-finding closure statement

The original **4 Critical** findings are closed as document contracts. Of the original **13 High** findings, the substantive safety contracts are closed or deliberately represented as open external evidence gates; the earlier lifecycle and source-baseline findings are materially repaired. H1 is a residual exhaustiveness defect exposed by the remediation's new “complete catalog” claim, not a revival of the old atomic-audit or tenant-isolation Criticals.

## Gate recommendation

Keep the PRD in `draft`. Resolve H1 before treating the operation catalog as implementation-complete, repair the current reconciliation pointer chain, then rerun a mechanical command/transition and FR-to-command trace. Even after those document repairs, do not advance M0/M1 or M2 until their explicit external/evidence gates pass.
