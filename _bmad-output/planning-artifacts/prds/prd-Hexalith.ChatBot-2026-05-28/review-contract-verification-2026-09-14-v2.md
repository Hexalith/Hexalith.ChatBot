---
title: "Final PRD contract and source verification v2"
status: complete
created: "2026-09-14"
reviewer: "targeted-contract-source-verifier"
verdict: fail
counts:
  critical: 0
  high: 2
  medium: 0
  checks_passed: 6
  checks_failed: 2
---

# Final Contract and Source Verification v2 — 2026-09-14

## Verdict

**FAIL — 0 Critical, 2 High, 0 Medium. Six of eight requested contract closures pass.**

The prior lifecycle, A6 gate-timing, approval-override, Conversations mapping, authority-mapping, and EventStore atomicity/fencing defects are closed at the artifact level. The unimplemented capabilities are consistently declared as owned A6/A13 pre-pilot stop-ship work and are not misrepresented as current platform behavior. At this review snapshot, finalization was blocked by a narrowed authoritative M0 representation of A13 and by a direct architecture input materially newer than the source manifest's reviewed hash.

This v2 report records the snapshot inspected before the root agent's subsequent live reconciliation. A focused v3 is required against that later working state.

## Scope and interpretation

Reviewed as one contract set:

- `prd.md`
- `addendum.md`
- `source-manifest.md`
- `qualification-evidence.md`
- `.memlog.md`
- `memlog-audit-2026-09-14.md`

The review also verified the current root revision, every sibling revision recorded in the source manifest, every declared consumed-contract SHA-256, and the current direct-input hashes. Explicitly owned implementation, producer-acceptance, and runtime-evidence gaps were treated as artifact-complete when their phase, disable condition, claim boundary, and owner were coherent. No canonical artifact was edited.

## Revision snapshot

| Repository | Current revision | Manifest result |
| --- | --- | --- |
| ChatBot workspace | `76f355a038c4abdb3b9fdb3fb836c25053a18fb0` | Matches `workspaceRevision`; the PRD update remains an uncommitted working set. |
| Hexalith.Conversations | `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | Match; clean. |
| Hexalith.Projects | `4f05a352edd67c4d5595913ee584539c1948dd58` | Match; clean. |
| Hexalith.Folders | `b409b03f9c3e35bc65c80ed03422c540e6201b20` | Revision match; dirty generated client files, while the consumed OpenAPI hash still matches. |
| Hexalith.Parties | `14d249fde316b0002aec84351d7a7cdf953d1d30` | Match; clean. |
| Hexalith.Tenants | `ff43dc941b01d4a68070f92dde0536f5ab1ef4df` | Match; clean. |
| Hexalith.EventStore | `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | Match; clean. |
| Hexalith.FrontComposer | `b0ad2fb69bcf5e7aadd7b388d25415d0fba876d5` | Match; clean. |
| Hexalith.Memories | `d99bc96371afbf55f8f37cd812c9e6cedba15b1d` | Match; clean. |
| Hexalith.Commons | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` | Match; clean. |

All 24 hashes declared across the 20 consumed-contract rows match the files at these revisions. Five of the six hash-bearing direct product-input rows match the live paths; the architecture row remains reproducible at its named commit but not at the current working path.

## Closure matrix

| Requested check | Result | Current evidence |
| --- | --- | --- |
| H1 lifecycle executability | **PASS** | `prd.md` §Shared Workflow Contract now makes its transition matrix authoritative, covers all association lifecycle-changing catalog commands, names actor/guard/destination/increment/concurrency/audit behavior, and supplies explicit successor semantics for terminal reprocessing. `Correcting` and `Correction-delayed` now have executable acknowledgement/delay rows. Compact contracts cover the remaining workflow families and distinguish public mutators from internal pipeline stages. |
| H2 current source lineage | **FAIL** | Root and sibling revisions now match, and all consumed-contract hashes match. The current direct architecture file does not match the manifest's pinned hash and contains material new decisions; see H2 below. |
| H3 A6 gate timing | **PASS** | `prd.md` Current Release Status and the M0/M1/M2 gate table put A6 before M0 pilot onboarding and revalidate it later. M0 scope, A6, NFR49a, `qualification-evidence.md`, and `.memlog.md` use the same pre-pilot owner, evidence, disable, and claim boundary. |
| Approval override wording | **PASS** | `addendum.md` §Risk Classifier states that normal approval authorizes but never reclassifies, and that the six boundary-crossing effect classes have no downgrade or override path. A later version may change eligibility only for a read-only/no-external-effect subtype. |
| Exact Conversations mapping | **PASS (open A13 implementation gate)** | `addendum.md` maps the stable product ID to `AppendMessageCommand` schema 1 and `MessageAppended`, specifies every current DTO field, separates requesting actor from governed-AI author, defines outcome/error translation, and records the missing expected revision, 24-hour owner default, production handler, audit, concurrency, and lifetime-idempotency gaps. PRD and manifest do not claim the target is executable. |
| Role/authority mapping | **PASS (open A13 owner-acceptance gate)** | `prd.md` §Owner-authority mapping is deny-by-default: ChatBot labels grant no owner authority; Tenants membership, Projects resource authority, EventStore claims/permissions, mailbox evidence, and client/delegation scope are separately required. Current owner/gateway authorization is mandatory for mutation. Exact owner acceptance and contract tests remain an explicit A13 stop-ship condition. |
| EventStore audit/fencing/atomicity contract | **PASS (open A13 implementation/evidence gate)** | PRD/addendum require per-aggregate mutation/audit co-commit, durable idempotency and policy/approval references in the same atomic boundary, signed tenant checkpoints, supported actor-only writes, storage ACLs, first-write fencing, and fork/reorder/rebuild tests. `source-manifest.md` accurately records that current EventStore can co-commit an event batch but not terminal idempotency, that its projection outbox is not an audit outbox, and that its public boundary lacks the full ChatBot contract. |
| A13 representation | **FAIL** | Current Release Status and the A13 row require the full pre-M0 cross-context closure, but the sole-authority M0 gate at the inspected snapshot narrowed its evidence to supported-write-path/fencing and named only System Architect + EventStore owner as A13 approvers. See H-A13. |

## High finding

### H2 — The manifest no longer identifies the current direct architecture input

**Location:** `source-manifest.md` lines 3, 6–8, 13, and 20; `prd.md` lines 24–27 and 47; current `ARCHITECTURE-SPINE.md`.

**Evidence:**

- At the initial review snapshot, the manifest recorded architecture commit `bab0218c14f5d5c4bc9513014535c5f22e84cde7` and SHA-256 `72f06150a77f6d51e1f52a4d7a85b58dd0459b0badfe91aa61e7466a6e1acaf1`. That hash is valid for the file at the named commit, while the live path then hashed to `41e49ee3de3ced4f8427d995ad34976bdc166facf6473a599e631e219f5af305`.
- During this verification, the manifest was advanced to the intermediate working-tree hash `41e49ee3de3ced4f8427d995ad34976bdc166facf6473a599e631e219f5af305`, while the architecture was refined again and hashed to `e7a031bec0be1af92d981d86d06342e227ddb109f50a0b19f9800db3c50a02e7`. The final observed architecture diff from the committed pin was 104 added and 40 removed lines. A synchronized current source snapshot was therefore not present during v2.
- The change is material to the PRD's recovery evidence boundary: it adds `activation: pending`, adopted AD-6 through AD-9, globally unique completion-check identity, disjoint diagnostic/authoritative artifact channels, cleanup receipt attestation, absolute closeout deadlines, immutable tool references, and destructive-run isolation.
- `prd.md` consumes the architecture by its live repository-relative path and says the update reconciles the current Epic 12 architecture, while `source-manifest.md` has `status: current`. Those claims do not disclose that the current file is a later, unreconciled contract.
- The root revision exactly matches `workspaceRevision` (`76f355a038c4abdb3b9fdb3fb836c25053a18fb0`). All nine recorded sibling revisions match their current checkouts, and all 24 SHA-256 values in the consumed-contract baseline match current source.
- Hexalith.Folders is currently dirty in two generated client files even though the manifest says no sibling checkout was dirty. Its consumed OpenAPI file remains unchanged and its declared hash matches, so this does not establish product-contract drift, but the cleanliness statement is not currently true.

**Impact:** The product contract remains internally coherent, but a final reviewer cannot reproduce a single source set described as both current and fully reconciled. The latest architecture additions affect exactly the recovery-evidence contract that `prd.md` names as a direct input.

**Required closure:** Reconcile the current architecture delta, update its manifest SHA/revision status, and record the material-change outcome; or explicitly retain the old commit/hash as the reviewed input while naming the newer working artifact and its reconciliation as pending. Correct the absolute sibling-cleanliness claim or make it revision/consumed-file scoped. Then rerun only the source-lineage check.

### H-A13 — The sole-authority M0 row narrows A13's closure and approvers

**Location at the inspected snapshot:** `prd.md` §Authority Map, §Minimum Release Slice M0 row, and A13; `qualification-evidence.md` §Required pre-pilot evidence.

**Evidence:**

- The Authority Map makes §Minimum Release Slice the sole authority for increment evidence and approvers.
- The inspected M0 evidence cell named only `A13 supported-write-path/fencing proof`, while A13 and qualification evidence also require the Conversations append/concurrency mapping, conversation-assignment ownership, closed Tenants/EventStore/Projects role mapping, and atomic-audit ownership.
- The inspected M0 owner cell assigned A13 approval only to System Architect + EventStore owner. A13 assigns Conversations, Projects, Tenants, and EventStore owners, with Security validating authority and fencing.
- Referring to A13 in the M0 disable condition does not repair the narrowed evidence/approver columns because the same table is declared authoritative for those fields.

**Impact:** A downstream gate implementation could approve M0 after EventStore fencing proof while the append, assignment, role, or audit-owner contracts remain unaccepted.

**Required closure:** Make the M0 evidence cell name the complete A13 closure and make its approver cell match A13/qualification evidence exactly. Preserve M1/M2 revalidation and the existing disable condition.

## Verified open gates that are not artifact defects

| Gate | Current state | Why it is artifact-complete |
| --- | --- | --- |
| A6 | Open; M0/M1 pilot onboarding and PII persistence blocked | Product decision, runtime KMS/protection/erasure/backup/export evidence, owners, revalidation, and prohibited claims are explicit in every authority location. Interface presence is explicitly insufficient. |
| A13 | Open; M0 blocked | Missing producer implementation, expected-revision/lifetime-idempotency equivalence, owner-approved role tokens, atomic audit persistence, ACL/fencing, and adversarial concurrency tests are all named, owned, and fail closed. |
| A10/A11 | Open M2 gates | Qualification evidence does not claim fresh recovery proof or complete SLO evidence, and the PRD blocks only the associated production/release-candidate claims. |

## Memlog audit

The audit enumerates all 40 current `.memlog.md` entries and maps them to the normative PRD, appendices, source manifest, or review artifact. The later A6/A13, per-stream audit, Conversations mapping, lifecycle, and structure entries resolve the older broad increment summaries without weakening their safety intent. No unmapped current decision was found.

## Gate recommendation

Do not mark the PRD/addendum final while H2 and H-A13 remain. No broader product-contract rewrite is required: align the authoritative M0 A13 row, refresh or qualify the changed architecture lineage, and rerun the focused checks.
