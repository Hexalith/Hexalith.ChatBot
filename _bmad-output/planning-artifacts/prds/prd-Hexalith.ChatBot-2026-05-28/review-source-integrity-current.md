---
title: Hexalith.ChatBot PRD Source and Downstream Integrity Review
status: complete
created: "2026-09-14"
reviewer: source-and-downstream-integrity
counts:
  critical: 0
  high: 2
  medium: 3
  low: 1
---

# Source and Downstream Integrity Review

## Verdict

**CONDITIONAL — source provenance and gate truthfulness are strong, but two High contract collisions prevent unambiguous story extraction for M0 policy storage and indeterminate AI-risk handling.** Direct product/architecture hashes match; A5, A6, A10, A11, and A13 remain honestly open; FR/NFR/A identifiers resolve; and the A11 target/evidence tables pair exactly. Counts: **Critical 0 · High 2 · Medium 3 · Low 1**.

## Critical

No Critical findings.

## High

### H1 — The M0 policy snapshot is simultaneously assigned to M1

- **Location:** `prd.md` §Increment M0 (line 297), §Shared Workflow Contract / Tenant policy (line 552), §Data Governance Surface (line 592), FR61 (line 1312), NFR15a (line 1440).
- **Concrete evidence:** M0 explicitly creates the “first immutable … M0 policy snapshot” through `UpdateTenantPolicy`; the workflow table assigns that command to M0/M1; M0 association, AI proposal, audit, and bootstrap paths consume or persist policy snapshots. The authoritative ChatBot-owned durable-record table nevertheless assigns `Policy snapshot` an owner increment of **M1**. First-store isolation, A6 retention, and story sequencing therefore have two incompatible first-use increments.
- **Fix:** Change the Data Governance Surface owner increment to M0 (with full editor/expanded policy administration remaining M1), or explicitly separate an M0 immutable bootstrap/decision snapshot record from a distinct M1 administration record and map both through ownership, retention, isolation, FR61, and the workflow table.

### H2 — Indeterminate risk classification has incompatible durable outcomes

- **Location:** `addendum.md` §Risk Classifier (line 46); `prd.md` §Shared Workflow Contract / AI action proposal (line 532); NFR15a AI-action-proposal row (line 1436); Glossary “Fail closed” (line 1144).
- **Concrete evidence:** The normative classifier appendix says missing tags, unknown effect surfaces, or undeclared authority make the action `approval-required` and “fail-closed to review.” The shared workflow's review path is a durable `ProposeAIAction` transition to `AwaitingApproval`. NFR15a instead names “risk classifier indeterminate” as a fail-closed condition for the AI-action-proposal write; the glossary defines fail-closed as a typed error with **no durable state**. Both are safe, but one creates a reviewable proposal and the other forbids it.
- **Fix:** Choose one canonical outcome. If review is intended, define a separate durable `NeedsReview`/unclassified workflow and command that cannot be approved until classification becomes determinate. If no-write refusal is intended, change the classifier appendix to a typed `classifier-indeterminate` failure and remove the `approval-required` wording. Add the chosen state, event, retry/remediation rule, and surface response to the workflow contract.

## Medium

### M1 — The “current” source manifest no longer describes the live checkout

- **Location:** `source-manifest.md` frontmatter and §§Sibling bounded-context revisions inspected / Re-check rule; `prd.md` §Project Classification (lines 126–130).
- **Concrete evidence:** The manifest records root revision `76f355a…` and pins Conversations `596cee6f…`, EventStore `7579b858…`, and Memories `d99bc963…`. The current checkout is root `2f40c476…` with those contexts at `589cdd71…`, `e8886ec4…`, and `5829db42…`. Only the Folders successor is disclosed. Inspection found no consumed public-contract change in these three deltas, so A6/A13 conclusions do not presently change, but the PRD says the manifest pins “the checked-out revisions” and the manifest remains `status: current`.
- **Fix:** Refresh the manifest with a dated non-material-delta section and current root/submodule identities, preserving the reviewed pins and hashes as the authoritative 2026-09-14 snapshot. Alternatively mark it explicitly `snapshot` and make the PRD say “reviewed snapshot revisions,” with a separate current-checkout/re-check record.

### M2 — Governed-chat response vocabulary does not round-trip to the canonical lifecycle

- **Location:** `prd.md` FR28b (line 1243), §Shared Workflow Contract / Governed chat attempt (line 539), NFR32 (line 1473).
- **Concrete evidence:** FR28b exposes `accepted`, `needs-review`, `approval-required`, `denied`, `unsupported`, or typed failure. The canonical workflow exposes `Admitted`, `ApprovalRequired`, `Denied`, `Unsupported`, or `Failed`; it contains no governed-chat `NeedsReview` state. NFR32 requires stable state names across API, CLI, MCP, events, audit, projections, and replay fixtures. No mapping says that `accepted` is a presentation label for `Admitted` or what owns `needs-review`.
- **Fix:** Use the canonical lifecycle names in FR28b, or add an explicit response-to-state mapping. Either remove `needs-review` or define its guard, durable/non-durable status, event, next command, retry behavior, and relationship to `ApprovalRequired`.

### M3 — “Project Association context” is not placed in the ownership map

- **Location:** `prd.md` §Technical Architecture Considerations (line 726) and §Context Ownership (lines 732–742); product brief §Technical Approach.
- **Concrete evidence:** The PRD says email-to-project association logic “is owned by a Project Association context,” but the authoritative owner map names no such bounded context and says ChatBot owns association workflows/records while Projects owns Project identity/access and Conversations owns assignment. The product brief also frames association as ChatBot orchestration. Architecture authors must infer whether this is a ChatBot subcontext, a separately deployed bounded context, or only a logical capability.
- **Fix:** Rename it “the Hexalith.ChatBot Project Association subcontext/capability” if that is intended, or add a formal owner-map row defining its source-of-truth records, deployment/API boundary, command ownership, and relationship to ChatBot, Projects, and Conversations.

## Low

### L1 — The authoritative A10 chain names a four-job gate without locating its four jobs

- **Location:** `prd.md` §Increment M2 (line 330) and A10 (line 1393); `addendum.md` §Recovery-validation commitments (line 269); `qualification-evidence.md` §§Current gate state / Historical recovery bundle (lines 17, 46–54).
- **Concrete evidence:** The artifacts repeatedly require the “current” or “currently shipped four-job recovery gate,” but the authoritative PRD/addendum/evidence chain names only the later `controlled-loss-path` job. The other job identities and the policy/version that declares the four-member set are not enumerated or linked. The semantic requirements for fresh controlled-loss and RTO-capable evidence are present, so this does not weaken the stop-ship rule, but it makes exact evidence-bundle assembly depend on unstated repository knowledge.
- **Fix:** In `qualification-evidence.md`, list all four stable job IDs and the repository/policy locator that declares them, plus the required artifact for each. Keep those mutable implementation identities out of the product target text except for the locator.

## Verified strengths

- Product brief scope reductions are explicit: generic-provider breadth, user upload, scheduled/file triggers, general “subject” workspaces, full task lifecycle, and unrestricted automation are not silently lost.
- The current Epic 12 architecture hash exactly matches the manifest, and its `activation: pending` boundary is preserved throughout the PRD, appendices, memlog, and evidence ledger.
- The direct product brief hash and all four historical validation-input hashes match the manifest.
- A5/A6/A13 block M0/M1; A10/A11 additionally block M2; no pilot, GDPR, tamper-evident-completeness, or production-readiness claim escapes those gates.
- The 15 A11 metric names pair one-to-one between the normative target table and mutable qualification table.
- Explicit FR, NFR, and A identifiers are uniquely defined and all token references resolve.
