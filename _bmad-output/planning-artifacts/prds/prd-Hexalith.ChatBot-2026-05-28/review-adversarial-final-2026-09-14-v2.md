---
title: Adversarial Final Review v2 - Hexalith.ChatBot PRD
status: complete
created: "2026-09-14"
reviewedArtifacts:
  - "prd.md"
  - "addendum.md"
  - "source-manifest.md"
  - "qualification-evidence.md"
  - "reconcile-current-sibling-contracts-2026-09-14.md"
  - "reconcile-sibling-contexts-2026-09-14.md"
reviewLens: "targeted hostile launch-readiness and prior-finding closure"
---

# Adversarial Final Review v2 — Hexalith.ChatBot PRD

## Verdict

**STOP — document defects: Critical 0, High 4, Medium 4.** The four prior Critical defects are closed as product contracts, and most of the thirteen prior High defects are closed. The current contract materially fixes universal atomic mutation audit, non-downgradable approval, deny-by-default AI membership, first-use tenant isolation, chat retry/stop concurrency, per-item batch approval, tenant-admin queue authority, and per-aggregate audit-chain concurrency.

The document is still not safe to finalize as the sole implementation authority. The lifecycle/catalog pair is not mechanically complete, the catalog omits several externally visible administrative mutations, the sole M0 gate truncates A13, and the A11 SLO artifact has no fields in which to record three of its own mandatory qualification dimensions.

Independently of those document defects, five deliberately explicit approval/evidence gates remain open: A5, A6, A13, A10, and A11. Those are not counted as defects where the documents accurately block the affected claim. They still make the current release **stop-ship**: A5/A6/A13 block M0/M1 onboarding and live AI; A10/A11 additionally block M2 production/release-candidate approval.

## Scope and test method

The review treated the current `prd.md` and `addendum.md` as the normative product contract, then tested their claims against `source-manifest.md`, `qualification-evidence.md`, and the current sibling reconciliation artifacts. The hostile checks were:

1. Trace every durable mutation class to the atomic FR81a audit boundary.
2. Round-trip each lifecycle-changing catalog command through an authoritative state transition.
3. Treat §Minimum Release Slice as the sole increment-gate authority and look for conflicting claims.
4. Resolve stable AI product IDs to versioned owner contracts without inventing a downstream command.
5. Separate document defects from explicit A5/A6/A13 and A10/A11 approval/evidence stop-ship gates.
6. Stress chat retry/stop/cancel races, batch approval, tenant-admin queue authority, and audit-chain concurrency.

No canonical artifact or existing review/reconciliation file was edited.

## High document-contract findings

### H1 — The lifecycle/catalog round-trip still has overlapping guards and unmapped commands

**Location:** `prd.md` §Shared Workflow Contract, especially lines 505, 510, 519–533; §Command and Query Contracts, lines 786–832; FR65.

**Evidence:**

- `MarkEmailAssociationNeedsReview` permits `Deferred -> NeedsReview` for a worker or reviewer with “resumed review” (line 505), while `ResumeEmailAssociationReview` defines the same transition only for the assigned reviewer after the revisit condition is met and evidence is refreshed (line 510). The broader command bypasses the narrower ownership, condition, and freshness guard.
- The catalog exposes `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, and `CancelAIAction`, but the AI-action family collapses them into “the matching decision command.” The authoritative contract does not map each command to its exact source/destination, guard, outcome, and retry result.
- `RetryWorkflowOperation` appears in the catalog and only has one transition use: attachment `Failed ->` a new `PendingScan` attempt. FR65 also promises retry for mailbox, association, approval, command, and projection work, but those source states, successor rules, and retry events are not bound to that command.
- The audit-projection family permits an “authorized rebuild,” and the service-client table exposes `Projection.Rebuild`, but the complete public operation catalog has no corresponding named rebuild command or authoritative authorization/transition row.

**Impact:** A worker can resume a deferred association without the assigned-reviewer/revisit guard, and surface teams can implement different approval/retry/rebuild commands while claiming the same lifecycle. This is a policy-bypass and conformance gap, even though FR81a would still require atomic audit for whatever command is implemented.

**Required fix:** Remove `Deferred` and “resumed review” from `MarkEmailAssociationNeedsReview`; reserve that transition for `ResumeEmailAssociationReview`. Expand the compact workflow families so every catalog lifecycle command has one exact row (or an explicit internal-only classification) with actor, guard, source, destination, event, expected revision, idempotency result, and successor rule. Define the operation classes covered by `RetryWorkflowOperation`, or replace it with family-specific commands.

### H2 — The “complete” operation catalog omits externally visible durable admin and governance mutations

**Location:** `prd.md` §Command and Query Contracts, lines 780–851; FR9, FR18–FR19, FR51–FR53, FR58, FR63, FR69–FR75g; NFR15a and NFR70; `addendum.md` §Tenant Policy Schema.

**Evidence:** The section calls its list the “complete operation catalog,” but no named commands exist for tenant-policy changes, mailbox configuration, notification routing, rate/quota/circuit-breaker changes, pause/resume of an opaque queue partition, queue claim/assignment, retention/export/delete workflow initiation, or superseding a reversible human decision. These are externally visible durable mutations in the FRs. NFR15a's detailed path inventory likewise omits participant, attachment, task-intent, governed-chat, service-client grant, queue/admin, annotation, retention/export/delete, and notification mutation paths even though FR55 and FR81a cover them universally.

**Impact:** The universal FR55/FR81a/NFR50a invariant closes the old unaudited-mutation permission at the product level, so this is not a new Critical bypass. But architecture, UI, CLI/MCP exposure, authorization tests, and NFR70 cannot trace the omitted mutations to stable command IDs, transitions, conflict responses, or audit events. Teams must invent public contracts in downstream work.

**Required fix:** Either rename the list as a scoped workflow catalog and point every omitted operation to a binding owner contract, or add stable commands and compact authoritative transition families for policy, mailbox/admin, queue assignment/control, data-subject operations, decision supersession, and notifications. Extend the NFR15a inventory by mutation family or explicitly state that it is illustrative while a machine-readable catalog is exhaustive.

### H3 — The sole M0 gate narrows A13 to EventStore fencing and omits required producer approvals

**Location:** `prd.md` §Authority Map lines 104–116; §Minimum Release Slice M0 row line 277; §Owner-authority mapping; A13 line 1320; `source-manifest.md` §Consumed contract baseline; `qualification-evidence.md` lines 16 and 46.

**Evidence:** A13 requires owner-accepted Conversations append/concurrency, Conversations assignment ownership, Projects/Tenants/EventStore role-permission mapping, atomic canonical-audit ownership, and supported-write-path ACL/fencing. The sole M0 gate names only “A13 supported-write-path/fencing proof” and assigns A13 approval only to the System Architect plus EventStore owner. It does not name Conversations, Projects, or Tenants approval. The source manifest also has no exact Projects contract/compatibility row even though A13 requires its owner-side resource-grant mapping and says closure must be recorded in the manifest.

**Impact:** A release reviewer following the declared sole gate table can approve M0 after EventStore fencing evidence without the executable Conversations producer, lifetime duplicate/concurrency contract, or owner-accepted authorization mapping. That defeats the otherwise honest A13 stop-ship gate.

**Required fix:** Expand the M0 evidence cell to “all A13 contracts and evidence defined in A13/qualification-evidence,” enumerate Conversations, Projects, Tenants, EventStore, Architecture, and Security approvers, and make missing/expired acceptance a disable condition. Add exact Projects consumed-contract and acceptance-status rows to `source-manifest.md`. M1/M2 should revalidate that complete bundle, not a narrowed shorthand.

### H4 — A11 cannot be closed in the SLO artifact because mandatory evidence fields are absent

**Location:** `prd.md` M2 gate line 331, A11 line 1318, NFR42a; `addendum.md` §Operating Baselines lines 193–227 and 251; `qualification-evidence.md` line 19.

**Evidence:** A11 and the M2 gate require every SLO row to have a numeric target, error budget, live signal provenance, alert route, and passing burn test. The normative SLO schema and table record only metric, target, measurement window, error budget, alert threshold, calibration source, and tenant scope. They have no fields for live-signal locator/provenance, alert destination/owner, or burn-test evidence/result. Several current values are correctly `unsupported-pending-a11`, but there is no defined place or binding referenced artifact in which the missing dimensions can become supported.

**Impact:** A team can fill every existing column and claim the normative catalog is complete while three release-gate requirements remain unrepresented. Conversely, a cautious reviewer has no mechanical way to prove A11 closure.

**Required fix:** Add columns (or a binding per-row qualification table) for live signal name/locator and provenance, alert route plus accountable receiver, burn-test locator/result/date/candidate revision, and gate state. Make `unsupported` derive from any missing required field and bind the completed row to the exact M2 candidate.

## Medium document-contract findings

### M1 — M2's “WORM store” wording competes with the canonical per-aggregate audit topology

**Location:** `prd.md` §Increment M2 line 327; NFR49a; `addendum.md` §Shared Command Pipeline line 122; `reconcile-current-sibling-contracts-2026-09-14.md` lines 64–88.

The M2 summary says the tamper-evident chain is implemented “as an append-only WORM store with hash-chained envelopes.” The normative topology says canonical envelopes are hash-linked inside each aggregate command stream, atomically with the domain mutation, and separately anchored by signed per-tenant checkpoints. A reader can reasonably implement a second post-commit WORM ledger, recreating the atomicity problem FR81a forbids.

**Fix:** Rewrite the M2 bullet as WORM/retention hardening and signed checkpointing for the already-canonical per-aggregate envelopes, or state the exact role of any secondary store and that it cannot be the canonical mutation boundary. Keep actual EventStore ownership, idempotency co-commit, and fencing as explicit A13 evidence work.

### M2 — Increment prose still says M1 runs “in production” and omits live gates from a dependency summary

**Location:** `prd.md` §Minimum Release Slice lines 271–279; §Project Scoping lines 999–1013 and 1039; line 333.

The sole gate table allows only a governed pilot claim for M1, but the resource-risk summary says M2 starts after M1 parity is “in production.” The increment summary at line 333 names A2/A3/A8/A9/A10 as sequencing dependencies while omitting open A5/A6/A11/A13 gates. The Authority Map prevents these summaries from overriding the table, but downstream plans may still copy the stale wording.

**Fix:** Replace both summaries with direct references to the gate table: M2 starts only after the M1 governed-pilot gate passes, and all A5/A6/A13 inherited gates plus A10/A11 M2 gates remain binding.

### M3 — The governed-chat cancellation promise is not a complete transition contract

**Location:** `prd.md` FR28d–FR28f, §Shared Workflow Contract governed-chat and AI-action rows, §Command and Query Contracts.

Retry and stop/completion racing are now precise: immutable attempt IDs, predecessor links, and first expected-revision commit make the race deterministic. But FR28d separately promises cancellation of a pending request. The chat lifecycle provides only `Streaming -> Stopped`; it has no `Admitted -> Cancelled` transition. `CancelAIAction` is only implied inside the separate approval-decision family, so the artifact does not say whether “pending request” means an admitted generation, an approval proposal, or both.

**Fix:** Define the cancellable source state(s) and command(s). If cancellation applies only to an `AwaitingApproval` AI proposal, say that explicitly and remove it from the chat-attempt promise; otherwise add an expected-revision chat cancellation row and race result.

### M4 — The canonical manifest points to a stale sibling reconciliation with obsolete gate IDs

**Location:** `source-manifest.md` frontmatter line 8 and line 70; `reconcile-sibling-contexts-2026-09-14.md` lines 12, 39, 59, and 97; `reconcile-current-sibling-contracts-2026-09-14.md`.

The manifest's `recheckArtifact` still points to a reconciliation that says the PRD assigns conversation ownership inconsistently and lacks a role mapping. The current PRD has repaired both document-level issues, leaving producer acceptance under A13. That reconciliation also says callable append mapping is A8 work, but current A8 is the decided two-command allowlist and the executable owner mapping is A13. `source-manifest.md` repeats the ambiguous “A8/A13” label. The newer current-sibling reconciliation correctly treats producer execution/audit as A13 work but is not the manifest's declared re-check artifact.

**Fix:** Preserve the old extract as historical, mark it superseded for current gate status, and point the manifest to a refreshed reconciliation that distinguishes resolved PRD wording from unresolved producer acceptance. Use A8 only for allowlist membership and A13 for executable append/authority/audit/fencing.

## Explicit stop-ship gates that are not document defects

| Gate | Current state | Claim correctly blocked | Why this is not itself a document defect |
| --- | --- | --- | --- |
| A5 | Open | Live AI and M0/M1 onboarding | The provider contract/evidence requirements, owners, disable behavior, and absence are explicit. No provider capability is inferred. |
| A6 | Open | Pilot data/PII onboarding and GDPR-satisfaction claims | The product requires a data-class decision plus production KMS, payload protection/erasure, Parties adapter, backup/restore, export/delete, surviving-metadata, and witnessed runtime proof. Interfaces alone are explicitly insufficient. |
| A13 | Open | M0/M1 execution and tamper-evident-completeness claims | The missing Conversations producer, lifetime idempotency/concurrency, owner authority mapping, atomic audit ownership, and storage fencing are reported honestly. H3 is about the gate table's truncated representation, not about the legitimate external gap. |
| A10 | Open/provisional | M2 production/release-candidate recovery claim | The historical bundle is correctly expired and insufficient; fresh controlled-loss and RTO-capable evidence is required. |
| A11 | Open | M2 SLO/production-readiness claims | Unsupported rows correctly block the claim. H4 is the missing closure schema, not the legitimate absence of current evidence. |

## Targeted trace results

| Trace | Result | Adversarial conclusion |
| --- | --- | --- |
| Durable mutation -> atomic canonical audit | **Pass at invariant level; catalog detail incomplete** | FR55, FR81a, NFR15/NFR15a, NFR50/50a, and the addendum consistently require all domain, idempotency, policy/approval, and canonical audit state to co-commit or none to commit. No public `RecordWorkflowAuditDecision` repair command remains. H2 covers omitted stable operation definitions, not permission for unaudited writes. |
| Lifecycle catalog command -> authoritative transition | **Fail** | H1/H2. Several exact commands are implicit, overlapping, scoped to only one of several promised families, or absent for externally visible mutations. |
| Increment claims -> sole gate table | **Partial** | The Authority Map and condensed Complete Feature Set establish the right precedence. H3 and M2 leave actionable contradictions in the gate and repeated prose. |
| AI product ID -> owner contract | **Pass as a blocked mapping** | `Project.AppendConversationMessage` is explicitly a stable product ID mapped to Conversations `AppendMessageCommand` v1 / `MessageAppended`; `ChatBot.ExecuteLowRiskAssistance` maps to ChatBot's shared pipeline. Missing producer/concurrency/audit proof is correctly A13, not silently assumed. |
| Chat retry/stop race | **Pass; cancel partial** | Retry/stop concurrency is deterministic and duplicate-safe. M3 covers the distinct undefined pending-cancel transition. |
| Batch approval | **Pass** | Grouping is presentation-only by default; one submission is allowed only for identical frozen security values, excludes high-risk effect classes, preserves per-item authority/revision/decision IDs/audit, and prevents one failure authorizing another. |
| Tenant-admin queue authority | **Pass at policy level** | Aggregate summaries are redacted; per-item detail and mutation require Project authority; tenant admins can only pause/resume opaque partitions without Project authority. Stable operation commands remain missing under H2. |
| Audit-chain concurrency | **Pass as a product topology; A13 open** | Per-aggregate serialization and predecessor assignment avoid the former per-tenant predecessor race; signed tenant checkpoints anchor heads; ACL/ETag/fork/reorder/rebuild tests are required. Current EventStore does not yet supply the atomic/idempotency/checkpoint contract, and A13 correctly blocks M0. M1 removes the residual WORM wording ambiguity. |

## Prior 4 Critical + 13 High closure audit

### Prior Critical findings

| Prior finding | Current result |
| --- | --- |
| Mandatory AI effects could be downgraded | **Closed.** FR41/FR52, the risk classifier, the policy schema, and batch rules have no downgrade/override path. |
| AI allowlist was allow-by-default | **Closed.** A8 and addendum v0/v1 name exact sets and separate catalog, exposure, and AI invocation. |
| Post-commit audit allowed unaudited mutation | **Closed as a document contract.** FR81a and the addendum define atomic durability; support evidence correctly leaves implementation ownership/atomicity open under A13. |
| Tenant isolation arrived after store/surface release | **Closed.** FR55a/NFR9a require native store/API isolation proof in the first increment that introduces the store. |

### Prior High findings

| Prior finding | Current result |
| --- | --- |
| Recovery release evidence unsupported | **Contract closed; A10 evidence gate intentionally open.** |
| SLO catalog incomplete | **Still partial — H4.** Unsupported status is honest, but the closure schema omits required evidence fields. |
| Three classifiers conflated | **Closed.** Separate versioned inputs, outputs, failures, and qualification partitions exist. |
| Below-`T_low` / scorer-failure lifecycle conflict | **Closed.** All non-auto outcomes enter `NeedsReview`; `Deferred`/`Rejected` require human decisions. |
| Acceptance guidance missing for most FR groups | **Closed.** Every FR range has minimum scenario coverage. |
| Tenant Policy Schema untyped/unsafe | **Closed at contract level.** The schema is closed, typed, deny-by-default, versioned, and non-downgradable; stable public mutation commands remain H2. |
| Governed chat only narrative | **Substantially closed.** M1, S1a, FR28a–f, lifecycle, identity, audit, and WCAG are present; pending cancellation remains M3. |
| M0 authenticity delayed | **Closed.** Provider verdicts, header discrepancies, delegated identity, external posture, and strict/paranoid routing are M0. |
| Replay isolation proves only one effect | **Closed.** Composition-time credential denial, effectful adapter replacement, default-deny egress, and store/external-ledger invariance are required. |
| Mutation/decision idempotency time-windowed | **Closed.** Durable/lifetime IDs, decision slots, expected revisions, and typed conflicts are explicit; proposal suppression alone is windowed. |
| Conversations ownership absent/inconsistent | **Closed in the PRD; external producer mapping intentionally open under A13.** |
| Lifecycle not executable | **Still partial — H1/H2.** The state families are much stronger but the command/guard round-trip is incomplete. |
| Brownfield baseline not reproducible | **Mostly closed; A13 traceability partial — H3/M4.** Revisions and hashes are pinned, but the Projects acceptance slot and declared current reconciliation need repair. |

## Required closure order

1. Repair H1 and H2 together: establish an exhaustive stable operation catalog and a bijective lifecycle/transition map, removing the deferred-review bypass.
2. Make the sole M0 gate represent all of A13 and all producer/security approvers; add the missing Projects consumed-contract/acceptance trace.
3. Extend the A11 catalog/evidence schema so every required qualification dimension is representable and candidate-bound.
4. Remove the M2 WORM/topology ambiguity, align repeated increment prose to the sole gate, and define pending chat cancellation.
5. Supersede the stale sibling reconciliation pointer and re-run a mechanical catalog/transition/gate trace.
6. Keep the PRD in `draft` and all affected release claims disabled until the five explicit approval/evidence gates pass. Closing document defects must not be mistaken for closing A5/A6/A13/A10/A11.
