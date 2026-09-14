---
title: Hexalith.ChatBot PRD Editorial Review
status: complete
created: "2026-09-14"
lenses: [structure, prose]
reader: humans
styleGuide: Microsoft Writing Style Guide
---

# Editorial Review — Structure and Prose

Purpose/audience read: this artifact helps product, architecture, UX, engineering, security, compliance, and QA agree what may be built and what gates release. Structure model: Strategic/Context (Pyramid).

## Structure findings

| Pass | Original Text | Revised Text | Changes |
| --- | --- | --- | --- |
| structure | Draft/approved metadata and late A6/A10/A11/A13 blockers | Add an early Current Release Status block and make normative authority explicit | QUESTION, MOVE — accepted; critical status now precedes classification/scope. |
| structure | Authority disclaimers repeated across the PRD/addendum | Add one Authority Map and refer to the sole authority for each decision surface | MERGE, CONDENSE — accepted. |
| structure | §Complete Feature Set (~1,157 words) repeats scope, journeys, and exclusions | Keep only the unique sequencing/cross-reference rule | CUT, PRESERVE — accepted; reduced by about 1,050 words. |
| structure | Nine journey requirement tails plus §Journey Requirements Summary | Keep the narratives and §Traceability Overview | CUT — accepted; reduced by about 450 words. |
| structure | Workflow contracts and three informal operation lists inside User Journeys | Promote Workflow and Operation Contracts; remove the duplicate informal lists | MOVE, CUT — accepted; retained normative tables/catalog. |
| structure | Domain/governance sections repeat context and constraints | Consolidate into four groups | MERGE, CONDENSE — partially accepted; the authority map resolves precedence while detailed governance tables remain stable for downstream references. |
| structure | Strategy, risk, and increment definitions repeat | Keep a short strategy and the canonical release-slice reference | CONDENSE — partially accepted; the duplicated feature index was removed, while risk reasoning remains for human decision context. |
| structure | Binding addendum described as downstream detail | Rename it Normative PRD Appendices and state its authority | QUESTION, MERGE — accepted. |
| structure | NFR17a in the FR area; recovery qualification nested under SLOs | Move NFR17a beside NFR17 and promote Recovery Qualification | MOVE, PRESERVE — accepted. |
| structure | Glossary, traceability, gates, assumptions, FR/NFR catalogs, contract tables | Preserve stable navigation and identifiers | PRESERVE — accepted. |

Initial combined word count was 33,626 words (PRD 28,924; appendices 4,702). The accepted structural changes remove the largest duplicate sections while preserving binding contract detail.

## Prose findings

| Pass | Original Text | Revised Text | Changes |
| --- | --- | --- | --- |
| prose | Abstract “subject” and increment framing | State Project boundary and M0/M1/M2 sequence directly | Accepted. |
| prose | “The cost of keeping CLI and MCP…” | “Including CLI and MCP… requires narrower scope…” | Accepted; direct language. |
| prose | Self-referential canonical ownership wording | State authoritative ownership boundaries directly | Accepted. |
| prose | Dense A6 slash-delimited blocker | Split approval statement from enumerated evidence | Accepted. |
| prose | Dense reviewer-disagreement condition | Separate the two trigger cases | Accepted. |
| prose | Mixed positive/negative M0 append constraints | Use parallel sentences for scope and prohibitions | Accepted. |
| prose | Ambiguous atomic-commit sentence | State that all elements become durable together in one transaction | Accepted. |
| prose | Admin permissions/prohibitions in one sentence | Separate allowed partition controls from forbidden per-item mutation | Accepted. |
| prose | Nested NFR15 audit-unavailable explanation | State fail-closed rule, then rationale | Accepted. |
| prose | Awkward weekly diagnostic sample modifier | State the sampling procedure in two sentences | Accepted. |
| prose | Long WCAG capacity sentence | Lead with per-increment release requirement | Accepted. |

No product decision was changed by the editorial lenses; substantive changes came from the separately recorded rubric, adversarial, contract, and source-reconciliation passes.

## Post-gate polish

After the final contract remediations, the structure pass kept the early release status and authority map, placed the exhaustive workflow/admin transition tables beside the stable operation catalog, and retained mutable A11 evidence outside the normative target schema. The prose pass normalized actor, guard, state, event, retry, and successor wording across the new rows; distinguished disable, quarantine, and rate limiting; and made the single current A13 reconciliation pointer explicit. No additional product scope was introduced during this polish.
