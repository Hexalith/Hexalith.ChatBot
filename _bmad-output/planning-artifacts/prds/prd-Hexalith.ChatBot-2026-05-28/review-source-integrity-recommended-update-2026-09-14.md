---
title: Hexalith.ChatBot Recommended-Update Source Integrity Review
status: complete
created: "2026-09-14"
reviewer: source-and-downstream-integrity
counts:
  critical: 0
  high: 0
  medium: 1
  low: 0
---

# Source Integrity Review — Recommended PRD Update

## Verdict

**CONDITIONAL PASS — the refreshed manifest accurately binds the current root, all nine declared sibling revisions, every direct-input hash, and every consumed-contract hash; the PRD/addendum keep A13 open and do not convert the new Conversations/Folders candidates into owner acceptance.** One Medium provenance-link issue remains: the manifest's `recheckArtifact` points to the pre-refresh reconciliation whose conclusion says the manifest is stale, so a future reader cannot tell from that linked artifact that the subsequent refresh was independently verified. Counts: **Critical 0 · High 0 · Medium 1 · Low 0**.

No submodule was initialized or updated. No changes were made to `prd.md`, `addendum.md`, `.memlog.md`, or `source-manifest.md`.

## Critical

No Critical findings.

## High

No High findings.

## Medium

### M1 — The manifest's re-check pointer ends at the pre-refresh diagnosis, not the verified refreshed state

**Location:** `source-manifest.md` frontmatter `recheckArtifact`; `reconcile-recommended-final-2026-09-14.md` §§Remaining gaps / Final reconciliation result.

**Concrete evidence:** `source-manifest.md` now records root `5d5d5bb62c76b4a500a3f8c4d332161a34642b66`, current sibling revisions, the changed EventStore `AggregateActor.cs` hash, and the new Conversations/Folders correction rows. Its `recheckArtifact` still names `reconcile-recommended-final-2026-09-14.md`. That artifact was written before the manifest refresh and concludes that the manifest is stale and lacks the correction-owner rows. The refreshed values are correct, but the linked result describes the state that was repaired rather than verifying the repaired state.

**Impact:** this does not falsify a revision, hash, compatibility disposition, or A13 gate. It weakens the audit trail for why `status: current` is justified and could make a downstream reviewer treat the refresh as incomplete.

**Suggested fix:** after this reviewer pass is accepted, point `recheckArtifact` to this report (or to a short post-refresh reconciliation that cites it), and retain `reconcile-recommended-final-2026-09-14.md` as the input that triggered the refresh. Alternatively add distinct `refreshTriggerArtifact` and `refreshVerificationArtifact` fields. Do not change any A13 compatibility result.

## Low

No Low findings.

## Verification evidence

### Root and root-declared sibling revisions

- Root `HEAD` is exactly `5d5d5bb62c76b4a500a3f8c4d332161a34642b66`, matching `workspaceRevision`.
- Every initialized root-declared submodule worktree revision equals both its root gitlink and the manifest row:
  - Conversations `f0ea587f809defbe34e8458df051ec446ad73fec`
  - Projects `4f05a352edd67c4d5595913ee584539c1948dd58`
  - Folders `4c64f6165ecf01075352d9f03dd8d6e1bd17f4b1`
  - Parties `14d249fde316b0002aec84351d7a7cdf953d1d30`
  - Tenants `ff43dc941b01d4a68070f92dde0536f5ab1ef4df`
  - EventStore `555c904769db111c77a78674ee8d83ec64ffa6b9`
  - FrontComposer `b0ad2fb69bcf5e7aadd7b388d25415d0fba876d5`
  - Memories `5829db422522b79c0d164df85adde53a996ba114`
  - Commons `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6`
- Those nine sibling worktrees were clean during inspection. No nested submodule was consulted.

### Direct-input hashes and commit claims

- Product brief SHA-256 matches `d890417042c0329f168306936ae9b1004f2aa40c22f3e1d2845579db9b1ffd63`; the file at claimed commit `eceaa02ac63769eba580b8f29b8b23e048003a16` has the same hash, and the commit is an ancestor of current `HEAD`.
- Epic 12 architecture working-tree SHA-256 matches `e7a031bec0be1af92d981d86d06342e227ddb109f50a0b19f9800db3c50a02e7`; claimed base commit `bab0218c14f5d5c4bc9513014535c5f22e84cde7` exists and is an ancestor of current `HEAD`. The manifest correctly calls this a reviewed working-tree snapshot over that commit rather than claiming the base-commit file has the current hash.
- `orient-extract.md`, `review-adversarial-general.md`, `review-rubric.md`, and `validation-report.md` all match their declared hashes exactly.

### Consumed-contract hashes

Every declared SHA-256 in the consumed-contract table matches the current file. This includes all append/idempotency/ownership rows, all five files in the new Conversations correction row, the Folders OpenAPI row, Projects/Parties/Tenants contracts, all EventStore contracts, and FrontComposer/Commons.

The refreshed EventStore `AggregateActor.cs` hash is correctly updated to `8cb98de791376bc2ed3c322fd865d818a8cc5ddef2ed6ae09e77f3a84acda06f`; the manifest does not claim that its no-op result-payload change closes atomic-audit or idempotency gaps.

## New correction-contract verification

### Conversations

The five-file row is accurate:

- `ReassignConversationProjectCommand` exposes an optional `ExpectedCurrentProjectId` guard.
- `ConversationProjectAssignmentOperation` closes the public operation vocabulary to `Assign` and `Clear`.
- `ConversationProjectChanged` records previous/current Project IDs.
- `ReassignConversationProjectCommandHandler` exists behind tenant access and optional idempotency execution.

The row is also honest about what this does **not** establish. The inspected contract does not itself prove dual source/destination Project authority, lifetime idempotency, ChatBot's atomic canonical-audit boundary, correction-impact correlation/acknowledgement, compensation semantics, or producer acceptance. Labeling it `Open A13; candidate only, not accepted` is correct.

### Folders

The declared OpenAPI hash is exact. It exposes current `AddFile`, `ChangeFile`, and `RemoveFile` operations but no correction-specific cross-Project reassignment, quarantine/revoke/governed-copy acknowledgement, immutable source/destination linkage, or correction-manifest terminal-outcome contract. The `Open A13; missing required producer contract` disposition is correct and appropriately avoids inferring support from generic file mutations.

## PRD/addendum lineage and A13 truthfulness

- PRD `inputDocuments` includes the product brief, Epic 12 architecture, source manifest, and qualification evidence at valid paths.
- §Project Classification correctly describes the manifest as refreshed on 2026-09-14 and names the same nine contexts.
- A13 explicitly says the current revisions do not supply every claimed guarantee, adds Conversations/Folders correction contracts and acknowledgements, names their owners, and keeps M0 stop-shipped pending versioned owner acceptance and contract tests.
- The M0 gate likewise requires complete A13 closure; it does not treat the refreshed hashes as approval.
- FR91a says ChatBot only orchestrates owner-supported commands and explicitly leaves Conversations/Folders commands and acknowledgements open under A13.
- The append mapping in `addendum.md` remains `Blocked by A13`; no compatible transport/event fragment is presented as executable acceptance.
- `source-manifest.md` explicitly distinguishes compatible targets, conditional compatibility, blocked contracts, and open candidates, and says repository revision alone cannot close A6, A12, or A13.

No false A13 closure claim was found.

## Commands used

Read-only checks included `git status --short --branch`, `git remote -v`, `git log`, `git submodule status`, root/submodule `git rev-parse`, root gitlink comparison via `git ls-tree`, per-sibling `git status --short`, `git cat-file`, ancestor checks, `sha256sum` over every declared path, and targeted `rg`/file inspection for the Conversations/Folders correction contracts. All required targets were present and readable.
