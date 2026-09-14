# PRD Quality Review — Hexalith.ChatBot (Final Gate v2)

## Overall verdict

**Conditional pass.** The current PRD is complete enough to drive UX, architecture, security, compliance, QA, and story creation: its product thesis, increment gates, source lineage, owner boundaries, acceptance guidance, and claim limits are unusually explicit. A5, A6, and A13 are valid M0/M1 evidence gates, and A10 and A11 are valid M2 evidence gates; their missing external proof is not a document defect because the PRD and qualification record name owners, timing, disable behavior, and prohibited claims.

Two high-severity internal inconsistencies still prevent an unqualified launch-grade verdict: the sole-authority M0 gate narrows A13's required evidence and approvers, and the authoritative workflow table omits valid participant-resolution and low-risk-AI paths while leaving `Corrected` nonterminal without a successor. Four medium findings affect metric interpretation, approval metadata, parity extraction, and decision-memory audit accuracy.

## Decision-readiness — adequate

The email-first Project boundary, M0→M1→M2 dependency order, non-downgradable AI effects, exact AI allowlist, and pilot-versus-production claims are stated as decisions. §Current Release Status and the Authority Map make the decision state visible early. `qualification-evidence.md` consistently reports A5/A6/A13 as pre-pilot blocks and A10/A11 as M2 blocks without presenting absent evidence as completed work.

### Findings

- **high** The sole-authority M0 gate understates A13 (§Minimum Release Slice; A13; `qualification-evidence.md` §Required pre-pilot evidence) — the M0 row asks for “A13 supported-write-path/fencing proof” and names only the System Architect and EventStore owner for A13 approval. A13 itself also requires owner-accepted Conversations append and assignment contracts, the Tenants/EventStore/Projects authority mapping, an approved atomic-audit owner, and Security validation. Because the Authority Map declares the gate table authoritative for release evidence and approvers, a reader could satisfy the row without satisfying A13. *Fix:* make the M0 evidence cell name the complete A13 closure, and name Conversations, Projects, Tenants, EventStore, and Security in the owner/approver cell; keep M1 revalidation and the current disable condition.
- **medium** The normative addendum has ambiguous approval metadata (`addendum.md` frontmatter; PRD §Current Release Status) — it is `status: draft`, was updated on 2026-09-14, and is explicitly normative, but retains `approvedAt: 2026-06-09` without identifying the approved revision or stating that later edits superseded that approval. The PRD correctly says the artifact is under final review, but tooling or a downstream reader could treat the revised addendum as already approved. *Fix:* remove the stale approval field until final approval, or bind it to an explicit approved revision/scope and add a current approval state.

## Substance over theater — strong

The user journeys drive distinct authority, recovery, and surface requirements rather than serving as persona decoration. The innovation claim is framed as a testable internal thesis, not a market assertion. NFRs use product-specific limits, gates, actor sets, failure behavior, and observables. The former duplicate feature catalog is now a short navigation pointer, and the normative addendum carries executable depth without pretending that open evidence exists.

## Strategic coherence — adequate

The feature set follows one coherent bet: turn external email into authorized Project work whose evidence, approval, command execution, and audit trail remain governed across human and machine surfaces. M0 proves the vertical trust loop, M1 proves governed parity, and M2 proves production operation. SM16 and SM-C5 now test whether governed AI output is useful without improving the number by shrinking the supported request mix.

### Findings

- **medium** Several “Primary MVP outcomes” are observations rather than decision thresholds (§Measurable Outcomes, SM2–SM5) — SM2, SM3, and SM4 have no target or gate interpretation, while SM5 substantially overlaps the later, bounded SM9. A11 explicitly covers starter targets for SM8–SM14 and SM16, not these unbounded primary metrics. *Fix:* either define formula, threshold, measurement window, and owning gate for SM2–SM4 and reconcile SM5 with SM9, or label them diagnostic measures that cannot determine release.

## Done-ness clarity — adequate

The association matrix, compact workflow-family table, command catalog, AI-entry mapping, FR acceptance guidance, idempotency table, typed failure rules, and quantified NFRs give downstream teams concrete test surfaces. The previous missing M0 mediator identity and missing non-association workflow families are now addressed. External producer implementation and evidence remain properly gated by A13 rather than being inferred.

### Findings

- **high** The authoritative workflow contract still omits valid paths (§Shared Workflow Contract; FR13, FR39–FR40, FR87–FR90; `addendum.md` §Command Allowlist v1) — participant resolution permits `none → Unresolved` and `Unresolved → Resolved`, but not the normal `none → Resolved` path for a known authorized Party. The AI-action family defines only the approval-required route and no state/result path for M1 `ExecuteLowRiskAssistance`. In addition, `Corrected` is marked nonterminal although no outgoing transition is defined. *Fix:* add the direct participant-resolution and low-risk-assistance transitions with events, terminality, concurrency, and audit behavior; then either make `Corrected` terminal for that workflow or define its permitted successor/correction path. Reconcile `Dismissed` with FR38's user-facing “not actionable” disposition in the same contract pass.

## Scope honesty — strong

The PRD explicitly excludes the general email client, non-Project workspaces, broad automation, unrestricted commands, general uploads, commercial packaging, and production claims before M2. It identifies product-brief deltas and gives open assumptions named owners and revisit conditions. A5/A6/A13 and A10/A11 have proportionate density for a draft of this risk level and do not conceal the work behind vague “TBD” language.

## Downstream usability — adequate

The Authority Map, glossary, traceability matrix, stable requirement IDs, source manifest with pinned revisions and hashes, qualification ledger, and memlog audit make extraction substantially safer than in the prior version. All human journeys retain named protagonists, primary FR1–FR96 and NFR1–NFR70 sequences are present with explicit suffixed additions, and NFR17a now sits in the NFR section.

### Findings

- **medium** One parity-test verb does not map to the singular parity set (§Measurable Outcomes, “Cross-surface parity outcomes”) — “Automated parity tests verify create” names no operation in the immediately preceding exit set or §Command and Query Contracts. FR86 instead requires equivalent Command records for each surface. *Fix:* replace `create` with the exact intended operation or command name and make the bullet enumerate the same singular set used by the M1 gate.
- **medium** The memlog audit's completeness assertion is stale (`memlog-audit-2026-09-14.md`) — its opening says all 38 entries are represented, but `.memlog.md` and the audit table now contain entries 39 and 40. The rows are present, so this is a count/assertion defect rather than missing decision coverage. *Fix:* update the asserted count to 40 and, if the file is generated, derive the count from parsed entries.

## Shape fit — strong

The shape fits a chain-top, multi-stakeholder B2B SaaS product with material security, compliance, integration, and operational risk. Named journeys remain load-bearing; the PRD holds capability, scope, actors, states, gates, and measurable outcomes, while the normative appendices hold executable policy and pipeline details. Supporting lineage and qualification artifacts are separated from product authority without weakening traceability.

## Mechanical notes

- The addendum title and PRD Authority Map now agree that the appendices are normative; only the frontmatter approval state remains ambiguous.
- Inline assumption markers resolve to A9a or A11, and both are present in the assumptions table. Other assumptions are referenced directly from scope and gates.
- The source manifest distinguishes compatibility results from producer acceptance and points to the material-gap reconciliations; no inaccessible sibling input is silently inferred.
- Severity totals: **Critical 0 · High 2 · Medium 4**.
