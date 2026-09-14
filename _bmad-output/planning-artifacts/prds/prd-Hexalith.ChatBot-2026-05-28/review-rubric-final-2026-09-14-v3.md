# PRD Quality Review — Hexalith.ChatBot (Final Gate v3)

## Overall verdict

**Fail — targeted contract completion remains.** The PRD is strategically strong and honest about evidence: A5/A6/A13 are coherent pre-pilot gates, A10/A11 are coherent M2 gates, and none of their absent external implementation or runtime proof is counted as a document defect. The v2 rubric findings are closed: the sole M0 row now represents all of A13 and its approvers, SM2–SM5 are explicitly diagnostic, the missing lifecycle happy paths were added, parity wording is aligned, addendum approval metadata is no longer misleading, and all 41 memlog entries are audited.

Four high-severity internal gaps still prevent launch-grade chain-top use without downstream invention: lifecycle commands and guards do not fully round-trip, the declared complete operation catalog omits durable admin/governance mutations, the A11 catalog cannot record all evidence required to close A11, and the current architecture input does not match its manifest hash. Three medium inconsistencies remain in audit topology wording, increment status prose, and chat cancellation.

## Decision-readiness — adequate

The email-first wedge, governed-Project boundary, exact AI allowlist, non-downgradable approval effects, M0→M1→M2 order, and pilot-versus-production claims are explicit decisions. §Current Release Status, the sole-authority map, the gate table, the assumptions register, and `qualification-evidence.md` now agree on scope, owners, timing, disable conditions, and claim limits for A5/A6/A13 and A10/A11.

### Findings

- **medium** Repeated increment prose still conflicts with the sole gate (§Project Scoping → Risk Mitigation Strategy; §Minimum Release Slice) — the resource-risk summary says M2 starts only after M1 parity is “in production,” although the sole-authority gate permits only a governed cross-surface pilot claim at M1. The nearby dependency sentence names A2/A3/A8/A9/A10 but omits inherited A5/A6/A13 and M2 A11. *Fix:* say M2 starts after the M1 governed-pilot gate passes and refer to the gate table for all inherited and M2-specific conditions.

## Substance over theater — strong

The journeys remain load-bearing across contributor, external-party, owner, admin, automation, compliance, and governed-AI behavior. The innovation claim is an internal, measurable thesis. NFRs contain product-specific bounds, observables, and failure behavior. Open qualification rows are labeled `unsupported` rather than filled with invented evidence.

## Strategic coherence — strong

All increments serve one thesis: transform external email into authorized Project work whose association evidence, approval, command execution, and audit trail remain governed across human and machine surfaces. SM16/SM-C5 now measure useful AI output without rewarding artificial narrowing, and SM2–SM5 are correctly distinguished as diagnostics rather than unstated release gates.

## Done-ness clarity — thin

The PRD has strong acceptance scaffolding: stable FR/NFR IDs, per-group scenario requirements, typed policies, idempotency rules, association transitions, compact workflow families, and measurable security/reliability outcomes. However, the operation/lifecycle authority is not yet exhaustive, and A11 has no complete closure record. Those gaps would force architecture or story authors to invent contracts that the PRD says are already authoritative.

### Findings

- **high** Lifecycle commands and guards do not fully round-trip (§Shared Workflow Contract; §Command and Query Contracts; FR65) — `MarkEmailAssociationNeedsReview` permits `Deferred → NeedsReview` for a worker or reviewer with “resumed review,” overlapping the narrower `ResumeEmailAssociationReview` rule that requires the assigned reviewer, a met revisit condition, and refreshed evidence. `RetryWorkflowOperation` is mapped only to attachment retry although FR65 promises retry for mailbox, association, approval, command, and projection work. Audit projection permits an authorized rebuild, and the service-client table grants `Projection.Rebuild`, but the public catalog has no corresponding command. *Fix:* reserve deferred resumption for `ResumeEmailAssociationReview`; then map every public lifecycle command and retry/rebuild class to one exact source, destination, actor, guard, event, revision, idempotency result, and successor rule.
- **high** The “complete operation catalog” omits durable product mutations (§Command and Query Contracts; FR9, FR18–FR19, FR51–FR53, FR58, FR63, FR69–FR75g; NFR70) — no stable public commands are named for tenant-policy mutation, mailbox configuration, notification routing, rate/quota/circuit-breaker changes, opaque queue pause/resume or claim/assignment, retention/export/delete initiation, or decision supersession. FR55/FR81a ensure atomic audit in principle, but downstream teams cannot trace these externally visible mutations to identifiers, state changes, conflicts, or audit events. *Fix:* add the missing operations and compact transition families, or rename the list as a deliberately scoped catalog and bind every omitted mutation to an authoritative versioned owner contract.
- **high** A11's normative catalog cannot represent A11 closure (`addendum.md` §Operating Baselines; A11; NFR42a; `qualification-evidence.md`) — A11 requires a numeric target, error budget, live-signal provenance, alert route, and passing burn test for every row. The normative table has target and error-budget fields but no live-signal locator/provenance, accountable alert destination, or burn-test locator/result/candidate binding. Honest `unsupported-pending-a11` values do not supply a mechanical path to supported status. *Fix:* extend the table, or bind it to a per-row qualification ledger, with those fields and make any missing field derive `unsupported` for the exact M2 candidate.
- **medium** Governed-chat cancellation has no complete transition (§Shared Workflow Contract; FR28d–FR28f; §Command and Query Contracts) — stop/retry races are defined, but FR28d also promises cancellation of a pending request. The chat family has no `Admitted → Cancelled` path, while `CancelAIAction` belongs implicitly to the separate approval family. *Fix:* define which pending state and command FR28d means; add the expected-revision transition and race result, or state that cancellation applies only to an `AwaitingApproval` proposal.

## Scope honesty — strong

The MVP exclusions and product-brief deltas are explicit. The PRD does not treat pilot preview as production, does not claim GDPR or tamper-evident completeness before A6/A13, and does not claim recovery or SLO readiness before A10/A11. Each open gate has named evidence, owners, timing, disable behavior, and a bounded claim; these are appropriate product gates, not unresolved-document theater.

## Downstream usability — thin

The Authority Map, glossary, traceability matrix, source manifest, qualification evidence, and memlog audit provide unusually good extraction anchors. Requirement sequences are complete, suffixed IDs are explicit, NFR17a is correctly located, and all 41 memory entries are mapped. Current source lineage and the declared recheck pointer are nevertheless not reliable enough for a final brownfield handoff.

### Findings

- **high** The manifest does not pin the current direct architecture input (`source-manifest.md` §Direct product inputs; current `ARCHITECTURE-SPINE.md`) — the manifest records SHA-256 `41e49ee3…`, while the current file hashes to `e7a031be…`. The newer content is material to the recovery-evidence contract that the PRD says it reconciled. The manifest also designates `reconcile-sibling-contexts-2026-09-14.md` as the current recheck artifact even though that historical extract contains pre-remediation ownership/role conclusions and older A8/A13 labeling. *Fix:* freeze or hash the final architecture input, rerun the focused material-change comparison, update the manifest hash/status, and point `recheckArtifact` to a current reconciliation while retaining older reports as historical.

## Shape fit — strong

The shape fits a chain-top, multi-stakeholder B2B SaaS orchestration product with material security, compliance, integration, and operational risk. Named journeys remain useful; the main PRD holds outcomes, scope, actors, states, gates, and requirements, while normative appendices hold executable policy details and mutable evidence remains separate.

### Findings

- **medium** M2's WORM wording can create a second audit authority (§Increment M2; NFR49a; `addendum.md` §Shared Command Pipeline) — the scope bullet says the chain is implemented “as an append-only WORM store,” while the normative topology places hash-linked envelopes inside each aggregate command stream with separate signed tenant checkpoints. *Fix:* describe WORM as retention hardening for the canonical per-aggregate envelopes, or explicitly state that any secondary store is non-authoritative and cannot replace the atomic mutation boundary.

## Mechanical notes

- Primary FR1–FR96 and NFR1–NFR70 definitions are contiguous and unique; suffixed additions remain explicit.
- All inline assumption markers resolve to indexed A9a or A11 entries.
- `.memlog.md` and `memlog-audit-2026-09-14.md` both contain 41 entries, and the audit assertion now matches.
- The source-manifest architecture hash mismatch is counted above because reproducible brownfield lineage is substantive for this chain-top PRD, not a cosmetic checksum issue.
- Severity totals: **Critical 0 · High 4 · Medium 3**.
