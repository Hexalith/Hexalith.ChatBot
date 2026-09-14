---
title: Adversarial Final Review - Hexalith.ChatBot PRD
status: complete
created: "2026-09-14"
reviewedArtifacts:
  - "prd.md"
  - "addendum.md"
comparisonBaseline:
  - "review-adversarial-general.md"
  - "review-rubric.md"
  - "validation-report.md"
  - "reconcile-latest-prd-reviews.md"
---

# Adversarial Final Review — Hexalith.ChatBot PRD

## Verdict

**STOP — not launch-ready and not yet safe as the sole implementation authority. Counts: Critical 1, High 12, Medium 4, Low 1.**

The 2026-09-14 reconciliation is a material improvement. It closes the broad tenant-policy approval bypass, replaces the AI denylist with an exact two-command set, moves store isolation and inbound authenticity to their first-use increments, separates the three classifiers, introduces stable operation/decision identities, integrates governed chat, names Hexalith.Conversations, and turns A6/A10/A11 into explicit gates instead of implied completion.

The artifact still contains one direct trust-boundary contradiction: several requirements continue to require audit durability only for “risky” or “security-sensitive” operations even though the new invariant requires an atomic canonical audit envelope for every durable mutation. An implementation can conform to those narrower clauses while silently mutating ordinary state without the canonical envelope. The new lifecycle matrix is also not executable as written, and duplicate scope sections still place authenticity and idempotency later than their release gates. Finally, the document correctly admits that privacy, recovery, and SLO evidence are absent; that honesty prevents an unsafe approval, but it also means neither pilot onboarding nor M2 release is currently authorized.

## Prior-finding closure audit

### Prior Critical findings

| Prior finding | Status | Adversarial result |
| --- | --- | --- |
| C1 approval classes could be downgraded | **Closed** | FR40–FR41, FR52, the addendum classifier, and `ai-action.low-risk-subtypes` now make the six boundary effects non-downgradable. |
| C2 AI allowlist was allow-by-default | **Closed at policy level** | A8 and addendum v1 pin exactly two commands and separate catalog, exposure, and AI membership. The actual Conversations command mapping remains incomplete under H4. |
| C3 post-commit audit could not fail closed | **Not closed** | FR81a/NFR15a define atomicity, but narrower audit clauses still authorize a contradictory implementation; see Critical C1. |
| C4 store isolation arrived after release | **Closed** | FR55a/NFR9a and increment gates now require native isolation at store introduction, with M2 limited to M2 stores and recurring probes. |

### Prior High findings

| Prior finding | Status | Adversarial result |
| --- | --- | --- |
| Recovery release evidence | **Contract repaired; evidence open** | A10 now stop-ships M2 honestly, but no qualifying evidence exists; see H6. |
| SLO catalog completeness | **Contract repaired; evidence/catalog open** | Missing values now block M2, but the catalog is still incomplete even as a schema; see H7. |
| Three classifiers conflated | **Closed** | Separate versioned contracts, datasets, outputs, and failures are defined. |
| Below-`T_low` outcome conflict | **Closed** | All non-auto outcomes enter `NeedsReview`; `Deferred`/`Rejected` are human decisions. |
| Acceptance guidance missing for most groups | **Closed** | Every FR group now has a group-level scenario-coverage row. |
| Tenant-policy schema incomplete | **Substantially closed** | The closed table is much stronger; remaining role/sensitivity and retention issues are M3 and H5. |
| Chat scope only a note | **Substantially closed** | S1a, FR28a–FR28f, M1, journeys, traceability, and WCAG now carry it; retry semantics remain broken under H8. |
| M0 authenticity delayed to M1 | **Partially closed** | The canonical increment and FRs move it to M0, but the Complete Feature Set still assigns it to M1; see H2. |
| Replay isolation insufficient | **Closed at contract level** | Composition-time credential denial, effectful adapter replacement, egress denial, and production invariance are required. |
| Mutation/decision idempotency weak | **Core contract closed; integration incomplete** | Lifetime identities and revisions are present; stale M2 placement and chat retry semantics remain under H2/H8. |
| Conversations ownership absent | **Partially closed** | Ownership is named, but the executable command mapping/version is not; see H4. |
| Lifecycle not executable | **Not closed** | The new matrix has unreachable states, missing commands, and an M0 dead end; see H1. |
| Brownfield baseline not reproducible | **Partially closed** | Revisions and hashes improved provenance, but exact consumed contracts and compatibility results are absent; see M2. |

## Critical findings

### C1 — Narrow audit clauses reopen an unaudited-mutation path

- **Location:** `prd.md` §Technical Constraints (“Audit-write failure must fail closed for risky actions”); §Audit Requirements; FR55; NFR50; versus FR81a, NFR15/NFR15a, and NFR50a. Also NFR50a is titled M2 even though its `100%` canonical-envelope invariant is required in M0.
- **Evidence:** FR81a and NFR15a require every durable mutation to atomically commit its domain event, idempotency state, policy/approval references, and canonical audit envelope. The older clauses still say only risky or security-sensitive actions need audit records/tests. An “ordinary” conversation, queue, notification, lifecycle, or policy-support mutation can therefore be classified outside FR55/NFR50 and implemented without the canonical envelope while still satisfying those clauses.
- **Impact:** The exact prior C3 failure survives under a different wording: ordinary mutations can be unaudited, and M0/M1 teams can read the `(M2)` label on NFR50a as permission to defer completeness. This breaks reconstructability, idempotency proof, and the non-negotiable release gate.
- **Fix:** Replace every “risky actions” / “security-sensitive events” audit scope with two explicit sets: (1) **all durable mutations**, which require the FR81a atomic canonical envelope in every increment; and (2) security-sensitive non-mutating attempts such as denials or privileged reads, which also require audit but do not participate in a domain mutation transaction. Make FR55 and NFR50 cover every NFR15a path plus the second set. Remove `(M2)` from canonical audit completeness; reserve M2 only for investigation-view availability, WORM/hash verification, and production SLOs.

## High findings

### H1 — The “authoritative” lifecycle matrix is still not executable

- **Location:** `prd.md` §Shared Workflow Contract, §Association Lifecycle and States, §Command and Query Contracts, FR87–FR89.
- **Evidence:** The summary says `Received -> Proposed`, but no matrix row reaches `Proposed`; `ProposeEmailProjectAssociation` jumps directly to `Associated` or `NeedsReview`. The same row requires the scorer to succeed while claiming to encode scorer-error and unauthorized-evidence outcomes. Catalog commands `AssociateEmailToProject`, `MarkEmailAssociationNeedsReview`, `ReprocessEmailAssociation`, and `QuarantineEmailAssociation` have no row. `Deferred` can resume only in M1 even though M0 lets users defer. “non-terminal -> Failed” includes `Associated` and `Corrected` without identifying which failing step may destroy their semantics. `Correction-delayed -> Corrected` and terminal reprocessing are described in prose but absent as command rows. FR87 additionally promises lifecycle states for participant, attachment, approval, AI-action, command, and audit workflows while the only executable table is association-only.
- **Impact:** Different surfaces can implement different transitions, an M0 deferred item can remain stuck until the next increment, and commands omitted from the matrix can mutate state without a canonical guard or audit event. The prior lifecycle High is not closed.
- **Fix:** Either remove unused/duplicate commands and states or provide a row for each. Split success, scorer-failure, authorization, quarantine, terminal-reprocess, and correction-delay paths. Add `ResumeEmailAssociationReview` to M0 or remove `Defer` from M0. Restrict failure transitions by current step. Then publish equivalent authoritative tables—or explicit blocked prerequisites—for every FR87 workflow family.

### H2 — Duplicate scope sections still move authenticity and idempotency after their gates

- **Location:** `prd.md` §Minimum Release Slice; §Project Scoping; §Complete Feature Set, especially M1 authenticity and M2 idempotency bullets; NFR13a, FR48a–FR48d, and NFR65.
- **Evidence:** The canonical gate table and increment sections require inbound authenticity and durable idempotency in M0. The Complete Feature Set still lists FR48a–FR48d under M1 and “Idempotency contract per operation class” under M2; Project Scoping likewise presents idempotency as an M2 operations proof. The M0 must-have list omits the explicit authenticity floor and first-increment idempotency contract.
- **Impact:** Planning/story generation can legally schedule critical controls after pilot traffic begins, recreating prior H1/H3 timing failures even though the earlier section says the opposite.
- **Fix:** Make §Minimum Release Slice the sole increment authority. Update or replace later lists with references. Put the M0 authenticity floor and all M0 operation identities/decision slots in M0; extend the idempotency table whenever a new operation class appears rather than “completing” it in M2.

### H3 — The M0 association gate cites a metric with no pass threshold

- **Location:** `prd.md` §Measurable Outcomes and §Minimum Release Slice gate table.
- **Evidence:** M0 mandatory evidence cites SM1, SM6, SM-C1, and SM-C2. SM1 is only “association correctness” with no target; the actual `95%` precision / `90%` recall threshold is SM7, which the M0 gate omits. SM7 is also described as a calibration target rather than a tenant commitment, leaving its release-gate authority unclear.
- **Impact:** M0 can pass with any nonzero/undefined association correctness so long as unauthorized false positives are zero. The product can be unusably inaccurate without violating the stated gate.
- **Fix:** Cite SM7 directly in M0 mandatory evidence, state its exact dataset/version/sample prerequisites and pass/fail authority, and clarify whether 95/90 is a release threshold. Give SM1 a formula and target or keep it as a descriptive umbrella that cannot satisfy a gate.

### H4 — The allowlisted conversation command is not pinned to the owning context’s executable contract

- **Location:** `prd.md` §Context Ownership, §Command and Query Contracts, A8; `addendum.md` §Command Allowlist v0/v1; `source-manifest.md` sibling revision table.
- **Evidence:** The PRD declares `Project.AppendConversationMessage` to be a Hexalith.Conversations-owned contract “despite its legacy prefix,” but it provides no schema/version or mapping to the owning context’s canonical append command. The complete ChatBot operation catalog does not list either AI-allowlist member. The source manifest pins only the repository revision, not the exact consumed command contract. In the pinned local sibling source, the public contract is `AppendMessageCommand`, not `Project.AppendConversationMessage`.
- **Impact:** Architecture can implement the stable AI allowlist ID as a ChatBot wrapper, a Projects command, or a direct Conversations command, producing different authorization, idempotency, event, and atomic-audit behavior. Exact allowlist membership is not enough when the named member has no executable cross-context mapping.
- **Fix:** Define a versioned mapping from the stable product/allowlist ID to `Hexalith.Conversations.Contracts.Commands.AppendMessageCommand`, including schema version, field mapping, authority, expected revision/idempotency, returned outcomes, canonical audit ownership, and failure translation. List the wrapper and downstream command in the correct separate catalogs and pin the exact contract file/version in the source manifest.

### H5 — The privacy gate is omitted from M0 while unapproved retention defaults are already binding

- **Location:** `prd.md` §Minimum Release Slice gate table, §Data Governance Surface, §Compliance Requirements, A6, NFR49a, NFR53–NFR55; `addendum.md` §Tenant Policy Schema.
- **Evidence:** A6 and NFR49a say the data-class decision blocks pilot onboarding, but the M0 “Mandatory approval evidence” cell does not include A6; it appears only under M2. Meanwhile the data table already assigns a seven-year default and a 60-day candidate-ranking period. The configurable `data.retention-class` knob is introduced only in M2, after M0 starts retaining email-derived personal data.
- **Impact:** A team following the increment gate table can onboard a pilot under retention choices that the PRD simultaneously says are unapproved. Later M2 configuration cannot cure unlawful or over-retained M0 data.
- **Fix:** Put the approved A6 data-class contract in the M0 gate. Remove unapproved numeric defaults until that decision exists, or explicitly make the approved fixed M0 values part of A6. Introduce required retention/legal-hold/export/deletion controls when each data class first persists, not in M2 only.

### H6 — M2 has no qualifying recovery evidence

- **Location:** `prd.md` §Increment M2, A10, NFR56–NFR59; `addendum.md` §Recovery completion evidence boundary and §Recovery-validation commitments; `qualification-evidence.md`.
- **Evidence:** The documents correctly state that the hosted bundle is expired, predates controlled-loss proof, and cannot exercise the four-hour RTO boundary. No successor is cited.
- **Impact:** M2 and production/release-candidate claims are presently prohibited. This is no longer a hidden document contradiction, but it remains a launch blocker.
- **Fix:** Produce a fresh exact-candidate hosted four-job bundle with positive controlled-loss RPO derived from persisted bounds, plus a full-window or separately retained production-shaped RTO drill, then independently validate and record the result. Wording changes cannot close this finding.

### H7 — The SLO catalog is both unevidenced and structurally incomplete

- **Location:** `prd.md` §Increment M2, A11, NFR42a–NFR43; `addendum.md` §Operating Baselines.
- **Evidence:** Most rows are intentionally `unsupported-pending-a11`, so M2 is blocked. In addition, the prose says the catalog covers command latency per command class, FR81a overhead per surface, and correction propagation including M2, but the table has one aggregate command-latency row, no shared-pipeline-overhead rows, and only the M0/M1 10-minute correction target—not the M2 60-minute target in NFR17a.
- **Impact:** Even after A11 supplies numbers, the existing table shape cannot prove all promised SLOs. Teams can fill every present placeholder and still violate NFR42a/addendum coverage.
- **Fix:** Add required rows/dimensions for each command class, each surface’s pipeline overhead, and increment-specific correction propagation. Define numeric units and budget semantics for event-style rows such as retry exhaustion and subscription expiry. Then supply live provenance, routes, and burn tests before M2.

### H8 — Governed chat “retry” can only replay the failed outcome

- **Location:** `prd.md` S1a, FR28a–FR28f, §Command and Query Contracts (`RetryGovernedChatMessage`); `addendum.md` §Idempotency Keys.
- **Evidence:** FR28f says retrying with the same `operation_id` returns the prior outcome or a conflict. That is duplicate suppression, not a retry. No state/command contract states how a stopped, failed, or partially streamed response creates a new attempt without duplicating a committed message/proposal, or how stop races completion.
- **Impact:** The UI promises retry but cannot generate a new attempt under the declared identity rule. Implementations may reuse the ID and do nothing, invent a new ID and lose causal linkage, or duplicate output during a stop/completion race.
- **Fix:** Define a stable chat request/root ID plus immutable attempt IDs, allowed retry source states, expected revision, predecessor/successor links, partial-output discard/resume rule, and stop-vs-completion winner. Reusing an attempt ID returns its prior result; a retry creates exactly one new linked attempt.

### H9 — Batch approval can group materially different risky actions

- **Location:** `prd.md` NFR46 Grouping; FR41–FR42; `addendum.md` §Risk Classifier and approval policy.
- **Evidence:** NFR46 groups by requester, command, and Project “when the items share the same input shape.” It does not require identical frozen values for recipients, files, content, sender authority, tool target, effect classification, policy version, or expected revision. One batch action can therefore approve many materially different boundary crossings.
- **Impact:** The approval-fatigue control becomes an approval bypass: a reviewer may inspect one representative shape while authorizing different destinations or data exposures.
- **Fix:** Treat grouping as presentation-only unless every security-relevant input value is identical and individually visible/frozen. Exclude irreversible, external-send, file-exposing, tool, and on-behalf actions from one-click batch approval by default. Require per-item authorization/revision revalidation and one explicit approval decision per item even when the UI submits them together.

### H10 — Tenant admins can mutate queues for Projects they are forbidden to inspect

- **Location:** `prd.md` FR75b–FR75c, RBAC Matrix, NFR1/NFR2/NFR7, and §Tenant Model.
- **Evidence:** FR75b lets admins see aggregate queue summaries without Project membership. FR75c then lets them retry, requeue, quarantine, or dismiss items they can “see-only,” while also saying they cannot mutate Project-level records. Retry can trigger downstream work; quarantine/dismiss can suppress or hide a Project workflow item. No rule requires Project authority, re-evaluates the original actor, or limits these operations to non-content operational envelopes.
- **Impact:** A tenant admin who is explicitly not a superuser gains a cross-Project denial-of-service or indirect-execution surface. The shared pipeline cannot simultaneously honor FR75c and reject the admin for lacking Project authority.
- **Fix:** Separate tenant-health controls from item mutation. Admins without Project authority may pause a mailbox/client/queue partition or request investigation using opaque identifiers, but may not retry, dismiss, or quarantine a Project item. Per-item operations require Project authority or a narrowly defined emergency workflow with independent approval, reason, bounded scope, and audit; retries must revalidate the original request’s current authority and revision.

### H11 — A public `RecordWorkflowAuditDecision` command undermines atomic audit authority

- **Location:** `prd.md` §Command and Query Contracts, FR55/FR59/FR81a, NFR15a/NFR50a; `addendum.md` §Shared Command Pipeline.
- **Evidence:** The complete operation catalog exposes `RecordWorkflowAuditDecision`, while FR81a says adapters cannot write audit and the canonical envelope is created and committed atomically with the mutation. The PRD gives the standalone command no actor, purpose, allowable source state, or distinction from canonical audit.
- **Impact:** An implementation can backfill missing audit after mutation, fabricate an authoritative decision, or create a second competing audit path—exactly what the atomicity rewrite was intended to forbid.
- **Fix:** Remove it from the public operation catalog. If human annotations are needed, rename and define them as non-authoritative append-only annotations linked to an existing canonical envelope, with strict authorization and no ability to satisfy completeness or repair a missing atomic audit record.

### H12 — Per-tenant hash chaining and per-operation atomicity lack a concurrency contract

- **Location:** `addendum.md` §Shared Command Pipeline; `prd.md` §Context Ownership, NFR49a, NFR50a.
- **Evidence:** Every domain mutation must atomically include its canonical envelope, while canonical envelopes are hash-linked per tenant. Concurrent mutations can occur in independent aggregate streams, but no authority chooses the single predecessor, no tenant sequence/partition rule is defined, and no atomic multi-stream capability or serialization strategy is required.
- **Impact:** Two concurrent operations may use the same predecessor and fork the “chain,” or the hash link may be projected post-commit and therefore not part of the atomic canonical envelope. Either outcome violates NFR49a or FR81a.
- **Fix:** Define the ledger topology and concurrency oracle. Choose a per-tenant serialized audit stream with an atomic multi-stream/outbox guarantee, or change the invariant to independently verifiable per-stream chains anchored by a tenant checkpoint. Add concurrent-write, fork-detection, recovery, and reordering acceptance tests and pin the required EventStore capability in the architecture/source contract.

## Medium findings

### M1 — `ExecuteLowRiskAssistance` is called read-only but may create a proposal

- **Location:** `addendum.md` §Command Allowlist v1 and §Risk Classifier; `prd.md` FR28e, FR37, FR40–FR41.
- **Evidence:** The command is described as read-only/no-external-effect yet may produce “an attributed assistant response or proposal.” A durable proposal is a ChatBot workflow mutation, while the classifier says “modifying state” is always approval-required and FR41 more narrowly says Project-state mutation.
- **Impact:** The boundary between harmless workflow bookkeeping and approval-requiring business effects is ambiguous. An implementer can either require approval to create the approval proposal (deadlock) or treat an overly broad “proposal” as low-risk.
- **Fix:** Define effect classification as business/domain/external effects, explicitly exempt only the canonical proposal/audit records needed to request approval, and forbid proposals from carrying already-materialized boundary-crossing output. Alternatively split read-only assistance from proposal creation into distinct commands.

### M2 — The source manifest pins repositories, not the consumed contracts

- **Location:** `source-manifest.md`; `prd.md` §Project Classification and §Integration Contracts.
- **Evidence:** Sibling rows contain repository revisions and roles, but no exact file/API/event/schema paths, package versions, dirty-worktree hashes, or compatibility/re-check outcome. The manifest therefore cannot reproduce which contract was reviewed—most visibly for the conversation append mapping in H4.
- **Impact:** A revision identifies millions of possible facts but not the dependency assumptions the PRD claims were validated.
- **Fix:** Add one row per consumed contract with repository-relative path, symbol/schema version, revision/hash, compatibility result, reviewer/date, and unresolved mismatch. Record working-tree dirtiness or content hashes for uncommitted local inputs.

### M3 — Policy sensitivity and mutator rules disagree at row level

- **Location:** `addendum.md` §Tenant Policy Schema; `prd.md` FR74–FR75e.
- **Evidence:** `mailbox.authenticity-strictness` is security-sensitive but assigns a mailbox-admin plus policy-admin approval, while FR75d says policy-admin mutates schema knobs and all security-sensitive knobs require a second admin. `operational.limits` is security-sensitive in the schema, but FR74 calls rate limiting a standard policy mutation. The required two-person rule is not repeated consistently across all security-sensitive schema rows.
- **Impact:** Authorization tests cannot derive one expected result, and a supposedly standard rate-limit edit can disable a surface or create a denial-of-service condition without the stronger approval path.
- **Fix:** Make the schema row authoritative for each knob’s initiating role, second approver, separation-of-duty rule, and audit event. Align FR74/FR75d/e and explicitly distinguish mailbox connection management from authenticity-policy mutation.

### M4 — “Published” and “complete” labels still describe blocked artifacts

- **Location:** `addendum.md` §Operating Baselines (“Published SLO catalog”); `prd.md` §Complete Feature Set and several repeated scope summaries.
- **Evidence:** The table is correctly called a qualification backlog and uses `unsupported`, but its immediate heading still says “Published.” The Complete Feature Set repeats superseded increment assignments and outdated recovery wording. Data rows still say “scorer kernel” and “task-intent kernel” after the contracts were separated.
- **Impact:** Automated extractors and hurried downstream readers can select stale labels instead of the newer canonical prose.
- **Fix:** Rename the table “M2 SLO qualification backlog,” replace duplicated scope lists with references to the gate table, and remove shared-kernel terminology. Perform a final canonicality sweep only after the substantive fixes.

## Low finding

### L1 — Product-market evidence remains entirely internal

- **Location:** frontmatter `research: 0`; §Market Context & Competitive Landscape; SM8–SM15/A11.
- **Evidence:** The PRD is explicit that it makes no competitive claim, but no customer transcript, pilot baseline, or completed adoption dataset is cited.
- **Impact:** This does not weaken the security contract, but launch value and the chosen thresholds remain hypotheses.
- **Fix:** Keep the disclaimer and attach the A11 pilot baseline/adoption report before making product-market or efficiency claims. Do not relabel internal architecture evidence as customer research.

## Gate recommendation

Do not finalize the PRD or authorize pilot onboarding from the current pair. First close Critical C1 and High H1–H5, H8–H12 as document-contract defects. A6 must then be approved before any pilot data is onboarded. H6 and H7 require external evidence and remain legitimate stop-ship gates rather than prose fixes. After edits, run a targeted adversarial re-review that traces every mutating command to one atomic envelope, every lifecycle command to one transition row, every increment claim to the single gate table, and every cross-context command to a pinned executable contract.
