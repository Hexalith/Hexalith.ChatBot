# PRD Quality Review — Hexalith.ChatBot

## Overall verdict

This is an adequate, unusually rigorous chain-top PRD: the product thesis, release gates, authority boundaries, success measures, and safety invariants are strong enough to guide decisions, and the document is candid that the artifact is final while M0/M1/M2 remain evidence-gated. It is not yet a clean implementation oracle, however: correction of already-stored attachments and the pre-pilot data-rights workflows lack complete actor/owner/surface contracts, while smaller state-machine and metric gaps would force downstream teams to invent behavior.

## Decision-readiness — strong

The PRD states decisions as decisions. It says that artifact finality does not waive gates and explicitly blocks M0/M1 on A5, A6, and A13 and M2 additionally on A10/A11 (`prd.md:98-103`, “Final artifact status does not waive any increment gate”). The authority map identifies which section controls scope, metrics, workflow states, public operations, ownership, and open evidence (`prd.md:105-119`), while the increment table binds audience, evidence, approvers, rollback conditions, and permitted claims (`prd.md:280-284`).

Trade-offs are also honest: CLI/MCP parity is delayed to M1 and paid for by removing channels, broad intelligence, packaging, and arbitrary integrations (`prd.md:268-270`), and resource pressure trims breadth before the safety floor (`prd.md:276-278`). The remaining open assumptions have owners and revisit conditions (`prd.md:1377-1396`), so a decision-maker can distinguish “approved product contract” from “not approved to pilot/release.”

### Findings

No substantive findings.

## Substance over theater — strong

The product thesis is specific and falsifiable: the bet is that email can become authorized project work, not merely that AI can summarize it (`prd.md:80-86`). The eight human journeys assign distinct protagonists, authority, failure modes, and value moments (`prd.md:376-465`), and the separate system journey explains the governed AI boundary (`prd.md:466-474`). These are not ornamental personas; they drive the UI inventory, RBAC, FR trace, and approval behavior.

The NFRs are product-specific rather than boilerplate. Examples include bounded cache staleness (`prd.md:1409`), p95 latency limits (`prd.md:1462-1465`), exact audit completeness (`prd.md:1503-1505`), and increment-scoped WCAG evidence (`prd.md:1522-1526`). The market section also avoids unsupported novelty claims (`prd.md:676-680`).

### Findings

No substantive findings.

## Strategic coherence — adequate

The arc is coherent: M0 proves the governed email-to-action thesis, M1 extends that same command model across surfaces, and M2 establishes production operations (`prd.md:286-339`, `prd.md:1073-1085`). Association correctness, safe routing, context lead time, manual-update reduction, AI usefulness, and explicit counter-metrics generally test the thesis rather than raw activity (`prd.md:169-237`).

### Findings

- **medium** The M1 machine-surface success gate proves one use, not the claimed business value (§Business Success, SM15, §Risk Mitigation Strategy; `prd.md:151`, `prd.md:206`, `prd.md:1111`) — cross-surface parity consumes a full increment and the PRD says it must validate business value, but SM15 passes when “at least one pilot workflow uses CLI or MCP.” A scripted demonstration or single incidental call could pass without showing repeat adoption, reduced effort, reliability benefit, or enough value to justify parity cost. *Fix:* Add a decision-grade M1 measure such as repeated governed machine-surface use across named pilot workflows/operators over a defined window, paired with a benefit measure (time, errors, or manual steps versus the UI/manual baseline) and a maintenance/governance counter-metric.

## Done-ness clarity — adequate

The strongest areas are close to executable specifications: the authoritative workflow tables name commands, actors, guards, states, terminality, successor rules, concurrency, and events (`prd.md:477-576`); high-risk FR groups have mandatory acceptance coverage (`prd.md:1172-1200`); and several requirements include exact acceptance clauses (FR23, FR27, FR42, FR67, FR76, FR77). The addendum supplies closed policy, idempotency, retry, and pipeline contracts.

Two gaps remain on paths that matter to M0 trust and story readiness.

### Findings

- **high** Correcting a wrong association has no authoritative contract for the attachment already stored in the wrong Project (§Journey 4, §Context Ownership, attachment workflow, FR91a; `prd.md:417-421`, `prd.md:530`, `prd.md:738`, `prd.md:1369`) — the journey promises the system “moves or relinks attachments according to project ownership rules,” but the attachment workflow makes `Stored` terminal, Hexalith.Folders owns storage and access, and FR91a names only derived-store invalidation. No Folders-owned correction/compensation command, authority check, failure state, or completion acknowledgement is defined. An implementation could mark the association `Corrected` while sensitive content remains governed by the wrong Project. *Fix:* Choose and specify the product outcome for stored attachments (owner-supported reassignment, revoke/quarantine plus a new governed copy, or another explicit compensation), map it to a versioned Folders contract and authority, include it in correction completion acknowledgements, and add failure/retry/audit acceptance scenarios; gate the owner mapping alongside A13 if it is not already supported.
- **medium** FR6 exposes user actions that the authoritative transition table either reserves for a worker or requires additional mandatory data (§Project Email Intake and Association, §Shared Workflow Contract; `prd.md:1209`, `prd.md:510-516`) — FR6 says an authorized user can “mark an item as needing review” and provide an “optional decision note.” The state table permits `MarkEmailAssociationNeedsReview` only for a worker from `Received`, requires an explicit reject-all reason, and requires both owner and revisit condition for defer. This leaves UI/CLI/MCP acceptance unclear. *Fix:* Make FR6 enumerate only the human commands and required fields allowed by the authoritative table, or add the missing authorized-user transition with its source states, guard, event, and audit rule.

## Scope honesty — strong

Scope omissions are explicit. M0/M1/M2 each state what is and is not shipped (`prd.md:286-339`), the MVP non-goals name autonomous project creation, general email replacement, full task lifecycle, broad intelligence, unrestricted execution, cross-tenant suggestions, and deferred trigger classes (`prd.md:341-352`), and the B2B section separately excludes commercial packaging and broad admin/product categories (`prd.md:1051-1061`).

The PRD also does not disguise missing evidence: A6 is a pre-pilot blocker, A10 is provisional, A11 has starter targets, and A13 identifies unsupported sibling guarantees (`prd.md:1387-1396`). Open-item density is high, but appropriate for the stakes because the artifact expressly blocks the affected claims rather than presenting assumptions as completed work.

### Findings

No substantive findings.

## Downstream usability — adequate

The glossary (`prd.md:1123-1155`), journey/FR/NFR/success trace (`prd.md:1156-1170`), stable catalogs, named protagonists, and authority map give UX, architecture, story, and test workflows strong extraction anchors. FR base IDs are unique and contiguous from FR1 through FR96; NFR base IDs are unique and contiguous from NFR1 through NFR70; SM1-SM16 and SM-C1-SM-C5 are present.

### Findings

- **medium** The same authoritative association contract describes incompatible correction paths (§Shared Workflow Contract, §Association Lifecycle and States; `prd.md:483-502`, `prd.md:519-521`, `prd.md:933-944`) — the summary says `Associated -> Corrected`, the actual transition matrix says `Associated -> Correcting -> Corrected` with an optional `Correction-delayed` path, and the later “Required association states” list omits both transient states while claiming to match the shared contract. Calling the table canonical reduces but does not remove extraction ambiguity for state enums, diagrams, generated stories, and tests. *Fix:* Use one canonical path everywhere (`Associated -> Correcting -> Correction-delayed | Corrected`, with the valid recovery edge), and either include the transient states in the required list or explicitly relabel that list as top-level dispositions.

## Shape fit — adequate

The overall shape fits a security-sensitive, multi-stakeholder B2B product that feeds UX, architecture, epics, and tests: named journeys are load-bearing, operational and compliance concerns have dedicated sections, and the normative addendum keeps detailed contracts out of the main narrative where possible.

### Findings

- **high** Mandatory pre-pilot data-rights workflows have neither a defined initiating actor nor a delivery surface/journey (§Shared Workflow Contract, §RBAC Matrix, §UI Surface Inventory, FR58; `prd.md:561-564`, `prd.md:601-622`, `prd.md:771-801`, `prd.md:1309`) — export, erasure, legal hold, and retention are marked “M0+ before first persisted pilot data,” yet their actor is only the undefined “authorized data-subject operator”; that role does not appear in the RBAC/owner-authority mapping. M0 UI contains only S1-S3, the compliance-investigation surface arrives in M2, and FR58 merely says reviewers can “access operational support.” Architecture and UX cannot tell how an authorized person initiates, reviews, receives, or recovers these gate-critical workflows. *Fix:* Add the initiating/reviewing roles and owner-side authority mapping, choose the M0 delivery surface explicitly (for example a governed operations API/provisioning interface or an M0 admin surface), and add a named journey or acceptance contract covering request, authorization, partial completion, hold precedence, result delivery, retry, redaction, and audit.

## Mechanical notes

- **low** Assumptions Index roundtrip is incomplete: A4, A7, and A12 occur only in the assumptions table (`prd.md:1386`, `prd.md:1389`, `prd.md:1395`) rather than at the requirement/scope statement they qualify. *Fix:* add inline `[ASSUMPTION A4]`, `[ASSUMPTION A7]`, and `[ASSUMPTION A12]` references at their operative statements, or classify them as standalone decisions rather than indexed assumptions.
- **low** The glossary definition of `Approval` includes “cancels” and “denies” (`prd.md:1133`), while the workflow makes cancellation a distinct requester/reviewer command and `Denied` a pre-classification disposition (`prd.md:533-539`, `prd.md:1262-1269`). *Fix:* define Approval as the review decision family and define cancellation/denial separately.
- **low** The addendum says “contract tests in FR82-FR86” verify the shared-pipeline invariant (`addendum.md:126`), but only FR86 defines contract tests; FR82/FR83 define surface capabilities, FR84 parity outcomes, and FR85 attribution (`prd.md:1356-1360`). *Fix:* cite FR86 alone or describe the range as parity requirements rather than contract tests.
- Base FR/NFR/SM IDs are contiguous and unique; letter-suffixed additions are unique. Range references should continue to be interpreted as including their letter-suffixed members.
- Every inline `[ASSUMPTION A9a]` and `[ASSUMPTION A11...]` marker resolves to a table entry, and every human journey has a named protagonist; the System Journey is appropriately separate.
