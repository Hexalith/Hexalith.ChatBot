---
title: "PRD reconciliation: product brief and Epic 12 recovery provenance"
status: complete
created: "2026-09-14"
inputs:
  - "_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md"
  - "_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md"
targets:
  - "prd.md"
  - "addendum.md"
  - ".memlog.md"
---

# PRD Input Reconciliation — Product Brief and Epic 12 Recovery Provenance

## Gate Summary

The product brief is substantially reconciled into the canonical PRD. Its email-first positioning, governed AI actor, external-collaborator posture, shared UI/CLI/MCP command model, approval boundary, bounded-context ownership, and user-voice promise are all preserved. The major brief-to-PRD scope reductions are explicit and agree with the recovered decision log.

The recovery architecture introduces a trust-boundary contract that is not yet explicit in the PRD or addendum: current-run story-completion evidence and retained scheduled/release operational evidence have different purposes and cannot substitute for each other. The PRD's A10 status is also stale as of 2026-09-14: the cited 2026-08-27 bundle is historical, no longer satisfies the shipped four-job gate, and aged beyond its stated freshness window on 2026-09-04.

Recommended gate: update before re-finalizing the PRD. No source requires reopening the established email-first MVP decisions.

## Sources Reconciled

- Product brief: `_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md`
- Architecture spine: `_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md`
- Canonical PRD: `prd.md`
- Technical addendum: `addendum.md`
- Decision memory: `.memlog.md`

## Findings

### R1 — A10 recovery-evidence status is stale

**Priority:** High

**Source evidence:**

- The PRD A10 row says a passing hosted required recovery bundle exists, then notes that it predates the required `controlled-loss-path` lane and cannot carry authenticity forward.
- The addendum fixes the bundle's freshness end at 2026-09-04 and says the shipped gate now rejects it as `controlled-loss-path:missing_evidence`.
- The current reconciliation date is 2026-09-14.

**Current gap:** A10 remains correctly marked provisional, but its opening sentence can still be read as claiming currently valid hosted evidence. The M2 scope bullet similarly says Story 12.15 "supplies authentic hosted continuity-safety evidence" without immediately identifying that evidence as historical and insufficient for the current gate.

**Suggested targets:**

- `prd.md` → `Open Assumptions and Decisions` → `A10`
- `prd.md` → `Product Scope` → `Increment M2 — Operations, Recovery, Continuity`
- `addendum.md` → `Operating Baselines` → `Recovery-validation commitments`

**Suggested change:** State first that there is **no fresh hosted bundle that passes the currently shipped four-job recovery gate**. Preserve the 2026-08-27 run only as historical evidence, with its measured figures and limitations. Keep A10 provisional until a fresh hosted controlled-loss artifact and an RTO-capable lane or separately evidenced pre-production drill exist.

### R2 — Story-completion and operational recovery evidence are not explicitly separated

**Priority:** High

**Source evidence:** Architecture AD-1 and AD-3 require `recovery-primary` to be current-run story-completion evidence and state that scheduled/release bundles feed only the independent recovery/A10 gate. A retained source cannot satisfy `recovery-primary`, and the completion artifact cannot substitute for A10 ratification.

**Current coverage:** The addendum correctly describes retained scheduled/release evidence for A10 and its freshness limits. The PRD correctly keeps A10 provisional.

**Current gap / ambiguity:** Neither artifact names the two trust purposes side by side. Phrases such as "Story 12.15 supplies authentic hosted continuity-safety evidence" and "Story 12.15 retained" can therefore be misread as saying the story-completion `recovery-primary` artifact satisfies A10, which AD-3 explicitly forbids.

**Suggested targets:**

- `prd.md` → `A10` and the M2 recovery bullet
- `addendum.md` → add a short subsection immediately before `Recovery-validation commitments`, titled `Recovery completion evidence boundary`

**Suggested product-level wording:**

> Current-run story-completion evidence and retained scheduled/release operational evidence serve separate trust purposes. Only current-run evidence may satisfy the story-completion recovery gate; only the independently governed operational bundle may inform A10. Neither substitutes for the other.

Keep exact paths, selectors, writers, and policy grammar in the addendum/architecture reference rather than the PRD narrative.

### R3 — The release-quality contract omits the recovery evidence-integrity gate

**Priority:** High

**Source evidence:** Architecture AD-1 and AD-2 require exact candidate revision binding, pinned policy and scope validation, one active recovery consumer, collision-free paths, current-run preflight, cleanup before publication, single-writer attestation, independent validation, and fail-closed handling of producer, timeout, no-test, cleanup, projection, attestation, or validation failure.

**Current coverage:** `NFR65` lists general production release gates; `NFR69` covers replay isolation. Neither expresses the recovery-specific completion-evidence integrity outcome.

**Suggested target / ID:** Add `NFR65a — Recovery completion evidence integrity` under `Validation and Quality Gates`.

**Suggested requirement:**

> A transition-declared recovery completion check must be bound to the exact candidate revision and approved evidence policy, accept exactly one current-run recovery producer, and fail the required check on planning, production, timeout/no-test, environment cleanup, evidence projection, attestation, or validation failure. Retained scheduled/release operational evidence must not satisfy this completion gate.

**Addendum placement:** Put architecture-specific enforcement details—exact paths and locator, selector, attestor ownership, stable reason codes, and collision/preflight rules—in `addendum.md` under `Recovery completion evidence boundary`, sourced to AD-1/AD-2.

### R4 — Completion-artifact minimization and lane-specific retention are underspecified

**Priority:** Medium

**Source evidence:** Architecture AD-4 requires least-privilege completion-job permissions and metadata-only published completion evidence. Raw TRX and tenant/test diagnostics remain outside completion upload paths. AD-3 separately permits scheduled/release operational lanes to retain their fuller A10 bundles.

**Current coverage:** `NFR10`, `NFR52`, and `NFR54` establish broad redaction, minimization, and retention principles. The addendum says scheduled/release lanes retain complete reports, manifests, and raw output for 30 days.

**Current gap / apparent conflict:** The lane qualifier makes the existing statements logically compatible, but the completion lane is not named. A later reader can incorrectly apply the operational-lane retention statement to `recovery-primary`, or assume the metadata-only completion artifact is eligible for A10.

**Suggested target / ID:** Add `NFR54a — Recovery evidence publication boundary` under `Auditability, Compliance, and Data Governance`, and mirror the mechanism in the addendum subsection proposed by R2.

**Suggested requirement:**

> Published story-completion recovery evidence must contain policy-allowed metadata only and must exclude raw test output and tenant diagnostics. Operational recovery artifacts are governed separately by the A10 evidence policy and explicit retention/freshness limits.

The exact GitHub permission set, sanitizer owner, and 30-minute publication reserve belong in the addendum or architecture, not the capability-level PRD.

### R5 — “Project or subject” remains an unresolved promise-to-scope ambiguity

**Priority:** Medium

**Source evidence:** The product brief twice promises collaboration around a "project or subject," including in the quoted user-voice anchor. The PRD preserves that quote, but every operative workflow, authorization boundary, association rule, and AI-context requirement requires a project.

**Current gap:** No explicit decision says whether a subject is (a) a plain-language synonym/label that must map to a governed project boundary, (b) a future non-project workspace type, or (c) wording that should be retired. The quoted promise therefore implies a capability that the functional contract cannot currently satisfy.

**Recommended resolution consistent with existing decisions:** Treat "subject" as plain-language framing only; any actionable collaboration subject must resolve to a governed project boundary. This does not add scope and preserves the project's safety model.

**Suggested targets:**

- `prd.md` → `Executive Summary`, immediately after the user-voice anchor
- `prd.md` → `Product Scope` → `Out of scope for MVP`, only if non-project subject workspaces are intentionally deferred
- `.memlog.md` → append the confirmed decision through `memlog.py`

### R6 — Source traceability metadata is outdated

**Priority:** Low

**Source evidence:** The PRD frontmatter still points to a Windows `D:/...` product-brief path, reports `projectDocs: 0`, and has `updated` / `lastEdited` values from 2026-05-28 despite later incorporated changes and the 2026-08-24 architecture source.

**Current gap:** A reviewer cannot determine from frontmatter that the current workspace product brief and recovery architecture were reconciled. The body contains newer evidence than the metadata advertises.

**Suggested targets:**

- `prd.md` frontmatter: normalize the product-brief path, add the architecture spine to `inputDocuments`, correct `documentCounts.projectDocs`, update `updated` / `lastEdited`, and append an `editHistory` entry.
- Preserve the correct workflow status for the update cycle; do not mark final until the PRD finalize gates are complete.

## Product-Brief Differences Already Resolved — Do Not Reopen by Default

| Product-brief statement | Canonical resolution | Decision-log consistency |
| --- | --- | --- |
| Both enterprise and generic email are MVP | One controlled Microsoft 365 / Exchange pattern is first; another provider is conditional on the same controlled mailbox contract (`A1`, MVP scope). | Consistent with the recovered M365-first decision. |
| Schedule and file-addition triggers are MVP | Conversation-triggered work remains in MVP; scheduled, file-addition, and explicit standing-instruction triggers are named post-MVP growth features. | Consistent with the recovered exclusion of broad trigger automation. |
| Trackable project tasks and task completion rate | MVP captures task intent and executes allowlisted AI actions; full task lifecycle is excluded. `AI-action-execution success rate` is the explicit replacement metric. | Consistent with the recovered narrow governed-action loop and full-task-lifecycle exclusion. |
| Broad document management automation | MVP limits this to governed attachment capture and uses `attachment auto-handling rate`; deeper document intelligence is post-MVP. | Consistent with the recovered broad-document-intelligence exclusion. |
| UI, CLI, and MCP expose the same command model | M0 proves UI; M1 adds CLI/MCP through the shared command pipeline; parity remains part of the single MVP release. | Consistent with the recovered M0/M1 sequencing and shared-pipeline decision. |
| External participants collaborate without adopting another internal tool | External parties remain tenant-scoped Parties and use email; authenticated external portal access is excluded from MVP (`A7`). | Consistent with the recovered external-participant posture. |

## Contradiction Check Against `.memlog.md`

No direct contradiction was found between the two inputs and the durable decisions in `.memlog.md`.

- The product brief's broader generic-email, trigger-automation, task-lifecycle, and document-intelligence statements have been intentionally superseded by later, narrower MVP decisions.
- Architecture AD-1 through AD-5 do not conflict with the M2 recovery decision; they refine the evidence-integrity contract needed to prove it.
- The only unresolved product-intent question is the meaning of "subject" outside a project boundary (R5).
- The recovery wording creates a semantic conflation risk, not evidence of an intentional decision reversal (R1–R4).

## Recommended Update Order

1. Correct the current A10/A10-addendum status and clearly label the 2026-08-27 bundle historical and stale.
2. Add the completion-versus-operational evidence boundary to the PRD and addendum.
3. Add `NFR65a` and `NFR54a`, keeping implementation details in the addendum/architecture reference.
4. Resolve the "subject" wording as a project-bound synonym unless Product explicitly wants a new workspace type.
5. Refresh frontmatter source traceability and append the accepted decisions/changes to `.memlog.md` using the required script.
