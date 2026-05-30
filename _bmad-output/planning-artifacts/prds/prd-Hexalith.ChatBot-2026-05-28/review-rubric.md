# PRD Quality Review - Hexalith.ChatBot

## Overall verdict

After two prior validation cycles, this PRD has hardened into a usable contract for downstream architecture and story creation: the governed email-to-project loop is named consistently, the lifecycle state model is reconciled across §"Shared Workflow Contract" (lines 372-392) and §"Association Lifecycle and States" (lines 696-712), the FR/NFR catalog is contiguous and cross-referenced through the Traceability table (lines 972-983), and the minimum release slice (lines 213-229) gives engineering an executable first increment. What still holds it back from "strong" is twofold - the PRD reads as governance-saturated rather than thesis-driven (the "controlled acceleration" user value gets one paragraph in UJ1 while five pages elaborate audit fields), and several FRs at the heart of the user experience still rely on adjectival outcomes ("clear", "user-safe", "user-friendly", "next-step guidance") rather than measurable thresholds a story author can convert into acceptance criteria without re-asking product. The pilot-adoption thresholds are concrete but unbaselined and unowned.

## Decision-readiness - adequate

The PRD makes its big bet visible: governance-first MVP, CLI/MCP parity is intentional scope (lines 209-211), the cost of that bet is named ("reduced breadth elsewhere: no additional collaboration channels, no full task lifecycle..."), and the parity-bet validation condition is stated ("validated when pilot automation or AI-agent workflows use CLI or MCP for at least one association/status/audit path"). The "What Makes This Special" (lines 66-72) and "Key Product Risks to Validate Early" (lines 74-82) sections frame the thesis directionally. Assumptions A1-A9 (lines 1149-1159) are now explicit, owned, and have revisit conditions, and they are linked inline at the points where they affect downstream work (lines 207, 209, 229, 1003-1006).

Where the PRD smooths trade-offs to neutral: there are zero `[NOTE FOR PM]` callouts and zero Open Questions anywhere in the document. Every tension is presented as resolved. This is suspicious for a PRD that admits in line 211 that UI-only is "insufficient for the MVP thesis" - that is a real argument, but the counter-position (engineering effort cost of three surfaces, parity-test drag on velocity) is never surfaced as a live tension a reviewer could push back on. Similarly, the choice to require approval for all "externally visible, project-mutating, file-exposing, task-creating, tool-invoking, or participant-representing" AI actions (line 199, line 1073) is presented as obvious - but approval fatigue is acknowledged later (line 546) without naming the unresolved tension. The PRD acknowledges what is risky but does not name what was given up to make each choice.

### Findings

- **medium** No live tensions surfaced (§Executive Summary through §Scope) - The PRD presents every major choice (CLI/MCP parity, approval-required default, single release vs. phased, M365-first) as resolved. Zero `[NOTE FOR PM]` callouts and zero Open Questions. A2/A3/A8 are formally tagged as assumptions but they read as confirmed direction, not open tensions. *Fix:* add 2-3 `[NOTE FOR PM]` callouts at real tensions - e.g., at line 211 ("CLI/MCP parity cost vs. velocity"), at line 1073 ("approval-required default vs. approval fatigue threshold"), and at line 229 ("vertical slice sequencing if M365 permission grants delay pilot").
- **low** Approval fatigue named in §Innovation/Risk Mitigation (line 546) but not reconciled with FR41 default (line 1073) - FR41 makes approval mandatory for a broad list of action classes. The risk section says mitigation needs "tenant policy for low-risk read-only assistance," but FR41 and the risk-class table (lines 1057-1063) put almost every useful action in "approval-required." *Fix:* either tighten the policy escape hatch in FR41/FR52, or add a `[NOTE FOR PM]` admitting the MVP intentionally errs toward fatigue and will tune post-pilot.

## Substance over theater - adequate

The Vision (lines 254-258) is specific to this product family: "Email is the first wedge... reusable project-aware AI workers, multi-channel conversation capture, governed task execution." It would not swap into a generic B2B SaaS PRD. The "What Makes This Special" section names a real differentiator (governed AI as project actor with command-level boundaries) and earns it by referencing the bounded contexts that enforce it.

Persona count is reasonable - eight UJ protagonists (Amira, Marc, Elena, Priya, Nora, Leo, Sofia, plus the System Journey for AI) each drive distinct requirements rather than decorating the PRD. UJ4 (Priya/correction) drives FR7, FR60-FR63, and the `Corrected` lifecycle state. UJ6 (Leo/CLI) drives FR82-FR86 and NFR26. UJ7 (Sofia/compliance) drives FR54-FR58. The personas earn their keep.

NFR theater risk is moderate. Many NFRs are well-bounded with numeric thresholds: NFR6 (5-minute / 60-second staleness, line 1172), NFR24 (p95 2 seconds, line 1196), NFR25 (10 seconds p95, line 1197), NFR26 (5 seconds p95 / 30-second hold, line 1198), NFR43 (subscription expiry, 5-min audit lag, 2 business days approvals, line 1221), NFR56 (RPO 15min / RTO 4hr, line 1240). These are the strongest. But several still drift to adjective-only: NFR40 ("user-appropriate language with enough next-action guidance" - what is "enough"?); NFR42 ("preserve user trust" is not testable); NFR44 ("runbook-ready" is unbounded); NFR46 ("preventing approval fatigue by prioritizing, grouping, and suppressing" - threshold?); NFR48 ("evidence freshness indicators must exist" - within what window?).

### Findings

- **medium** Adjective-only NFRs at user-trust hot spots (NFR40, NFR42, NFR44, NFR46, NFR48 - lines 1218, 1220, 1222, 1224, 1226) - These NFRs sit exactly at the moments where the product's reputation rises or falls (degraded states, approval fatigue, evidence freshness), but they specify outcomes only in adjectives. *Fix:* attach at least one observable threshold to each - e.g., NFR46 "no actor receives more than N approval notifications per hour without grouping"; NFR48 "evidence is shown with freshness band: <5 min fresh, <1 hr aging, older = stale and requires re-confirmation."
- **low** §"Innovation & Novel Patterns / Detected Innovation Areas" (lines 510-516) restates "What Makes This Special" (lines 66-72) without adding a different angle. *Fix:* either trim and point back to the Exec Summary, or earn the duplication by surfacing a different pattern (e.g., the deterministic-evidence-first ordering vs. AI inference, which only appears in passing at line 118).

## Strategic coherence - strong

The PRD has a thesis ("AI as a governed project actor, not a disconnected assistant" - line 67) and the feature set follows from it. The "differentiating moment" (line 70) - an email becoming authorized project work via association → files → approval → command → record - maps directly onto the Minimum Release Slice (lines 213-229), which in turn maps onto FR groups via the Traceability table (lines 972-983). MVP scope is intentionally narrow on breadth (one mailbox pattern, one association path, one task-intent flow) to prove the thesis vertically; growth and vision (lines 242-258) honestly defer the broader platform.

Success Metrics validate the thesis rather than just measure activity. The pilot adoption thresholds (lines 174-180) - 4 consecutive weeks, 2 mailbox patterns, 5 active projects, 70% evidence-resolved ambiguous decisions, 40% time reduction, 30% manual update reduction, 10 governed AI action reviews - are exactly the right shape for "did governed acceleration actually happen?" rather than DAU/MAU theater. Counter-metric posture is implicit (reassignment rate, false-positive rate, time-to-resolve) and listed in Measurable Outcomes (lines 130-159).

The MVP scope kind is "experience proof + platform proof" (named explicitly at lines 841-845) and the scope logic matches. There is no "backlog with section headings" failure mode here.

### Findings

- **low** Pilot thresholds (lines 174-180) are not baselined - "reduced by 40% compared with the pilot tenant's current manual email-to-project update process" and "reduced by 30% for pilot projects" presume the pilot tenant has a measurable baseline. *Fix:* add an `[ASSUMPTION]` (or extend A9) that pilot onboarding will include a 2-4 week baseline measurement period before the workflow goes live, and name who measures it.
- **low** No named owner for measuring success metrics (§"Success Criteria", lines 130-180) - the outcomes are listed but not attributed. Architecture and QA will have to back-infer who runs the evaluation dataset and who reports pilot adoption. *Fix:* add a sentence in §"Validation outcomes" naming the responsible role (Product + QA + Tenant Admin lead?).

## Done-ness clarity - thin

This is the dimension that most needs unforgiving review and is the weakest in the PRD. The PRD includes §"Functional Acceptance Guidance" (lines 984-1006) which sets a baseline: each FR group must produce acceptance scenarios with actor/surface/preconditions/transitions/audit fields, and the high-risk FR matrix (lines 1001-1006) explicitly demands matrices for FR1-12, FR39-46, FR55-63, and FR81-89 before stories. This pushes much of the done-ness work into the next phase (which is acceptable for an MVP at this stake) but leaves the FRs themselves frequently soft - and for the FR groups *outside* the high-risk matrix (notably FR21-FR28 "Project Conversation and Context" and FR64-FR80 "Reliability, Failure Handling, and Operations"), there is no explicit acceptance scaffolding at all.

Concrete examples of soft FRs that will produce ambiguous stories:

- FR23: "Authorized users can inspect why an email belongs to a project" - "inspect why" is not bounded. Does it require named evidence types? A confidence band? A signed policy snapshot?
- FR26: "distinguish informational project context from actionable requests" - by what mechanism? Visual treatment? A typed field? A classification label?
- FR27: "distinguish system-generated summaries from source evidence" - matches NFR64 but neither says how (color? badge? separate section?).
- FR36/37/38: task-intent capture/conversion/disposition - the verbs are clear but no FR says what fields a task-intent record carries or what minimum recall the detection must achieve.
- FR42: "Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected project resources, external recipients..." - the field list is good, but "summary" is not bounded (length? structure? required sections?).
- FR67 / FR69 / FR78: queue surfacing FRs name the queue types but not the columns/fields/filters required. NFR27 partially helps (page size 100, server-side filters list) but only for performance, not content.
- FR71: "next required human action" - good intent but no testable definition of what constitutes "next required."
- FR76 / FR77: "user-safe language", "next-step guidance" - adjectival.

NFRs are stronger on done-ness than FRs (good thresholds at NFR24-26, NFR43, NFR56). But NFR60 ("WCAG 2.2 AA" is fine but "core UI review workflows" is the set under test - which screens exactly?) and the adjective NFRs called out above leave gaps.

### Findings

- **high** Soft outcomes on user-facing FRs at the value moment (FR23, FR26, FR27, FR42, FR67, FR76, FR77 - lines 1038, 1041-1042, 1074, 1108, 1117-1118) - These FRs drive the contributor and reviewer experience, and they specify capability but not testable consequence. A story author will have to invent the field list and visual treatment, so the experience drift will happen at story time, after the PRD is "done." *Fix:* either add explicit acceptance bullets under each FR (preferred), or extend the §"Functional Acceptance Guidance" matrix (lines 1001-1006) to include FR21-FR28 as a required-scenario group, because UJ1 + UJ4 + UJ8 hang on these FRs.
- **medium** Task-intent FRs (FR35-FR38, lines 1067-1070) lack a data contract - the PRD names that task intent is "candidate action requests" and that it can be reviewed/converted/dispositioned, but does not say what the task-intent record contains, whether detection is rule-based or AI-derived, or what recall/precision threshold counts as success. *Fix:* add 1-2 lines to FR35 specifying minimum record fields (source message ID, candidate verb, affected resource, requester party, confidence) and reference NFR68 (evaluation datasets) for the precision/recall bar.
- **medium** NFR60 "core UI review workflows... must meet WCAG 2.2 AA" (line 1247) - "core" is not enumerated. Without a list, accessibility testing will either over-cover (slow) or under-cover (gaps). *Fix:* enumerate: "ambiguous association review, AI action approval/preview, attachment status, audit lookup, queue list/filter, refusal/denied message screens."
- **low** FR94 "measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag" (line 1141) lists metric names but not the publication shape (Prometheus? OpenTelemetry? dashboard?). *Fix:* point at NFR28 / NFR37 or specify the exposure surface.

## Scope honesty - strong

Non-Goals appear in three places (MVP §, lines 231-241; Growth §, deferring breadth; B2B SaaS Non-Goals at lines 821-826) and they do real work - they name autonomous project creation, full task lifecycle, document intelligence, cross-tenant suggestions, subscription tiers, and general email client replacement as explicit non-goals rather than letting a reader infer them. The "Out of scope for MVP" list (lines 231-241) is tightly written.

The Open Assumptions table (lines 1149-1159) is properly indexed: nine assumptions A1-A9, each with owner and revisit condition. The most consequential (A1 M365-first, A2 single mailbox pattern, A3 partial CLI/MCP parity, A5 AI provider compliance, A8 fixed command allowlist) are linked inline where they bite (lines 207, 209, 229, 1003-1006). Cost of CLI/MCP scope inclusion is named honestly at lines 211-212.

De-scoping logic ("If resources are constrained, trim dashboards, advanced mailbox inference..." at lines 227, 945) is stated honestly. The Minimum Release Slice (lines 213-229) names what cannot be trimmed.

Open-items density is moderate (9 assumptions plus 5 risk-mitigation paragraphs), which is appropriate for a B2B SaaS green-light-to-build PRD with significant integration surface. No Open Questions or `[NOTE FOR PM]` exist, which actually reads as scope-honesty *under-rotation* on tensions (see Decision-readiness finding).

### Findings

- **low** No `[NOTE FOR PM]` callouts anywhere - already noted under Decision-readiness; counted here because it leaves deferred decisions invisible at the points where downstream artifacts will hit them. *Fix:* see Decision-readiness finding.

## Downstream usability - adequate

This PRD chains into UX, architecture, and story creation (frontmatter `workflowType: 'prd'`, line 37), so downstream usability matters.

Strengths:
- Glossary present (lines 953-968) defining major domain nouns (Actor, AI actor, Approval, Association, Candidate project, Command surface, Context package, Evidence, External party, Party, Projection, Risky AI action, Service client, Source record).
- FR1-FR96 and NFR1-NFR70 IDs are contiguous and unique.
- Traceability table (lines 972-983) maps each journey to FR and NFR ranges - exactly what story creation needs.
- Lifecycle states are reconciled across the Shared Workflow Contract table (lines 380-391) and the §"Association Lifecycle and States" required-states list (lines 700-712). Good consistency on the names.
- Command and Query Contracts (lines 647-693) are enumerated as concrete operation names, giving architecture a starting catalog. This is a meaningful improvement over the prior partial list.

Weaknesses:
- Glossary drift: the body uses several domain terms that are *not* in the glossary. Notably "Policy snapshot" (FR61, NFR9, NFR50), "Operating baseline" / "Tenant or deployment profile" (NFR23-NFR26), "Idempotency key" (NFR13, NFR18, NFR50, FR90). The pair "Context package" (glossed) vs "Scoped AI context" / "AI context package" (NFR9, NFR48) are likely the same thing but the PRD does not say so.
- §"Required association states" list (lines 700-712) reorders the same states relative to the canonical table on lines 380-391 (`Received → Proposed → Associated | Rejected | Deferred | NeedsReview | Failed | Skipped`). Names match; order does not. Cosmetic now, but invites copy-paste drift in architecture.
- UJ Traceability "Validation focus" column is a short phrase; a story-creation agent still needs to read the UJ prose to extract acceptance hooks, and the UJ prose itself is dense (5-8 paragraphs each).
- FR cross-references in the Traceability table look correct on spot-check, but have not been machine-verified.

UJ protagonists are all named (Amira, Marc, Elena, Priya, Nora, Leo, Sofia, plus System Journey actor) - no floating UJs.

### Findings

- **medium** Glossary missing load-bearing terms (§Glossary, lines 953-968) - "Policy snapshot," "Operating baseline" / "Tenant or deployment profile," "Idempotency key," and the disambiguation of "Scoped AI context" / "AI context package" vs "Context package" are used across FRs and NFRs but not glossed. *Fix:* add these 4-5 terms; clarify whether "Scoped AI context" and "Context package" are synonymous.
- **low** Lifecycle state list (§"Association Lifecycle and States", lines 700-712) is in a different order than the canonical table (lines 380-391). Not a contradiction. *Fix:* reorder the lines 700-712 list to match the canonical sequence.
- **low** UJs lack a "key acceptance hooks" extract per journey (§UJ1-UJ8, lines 262-368) - story creation will have to re-read prose to find the testable bits. *Fix:* optional - add a 3-5 bullet "what story creation should extract" at the end of each UJ, or rely on the Traceability table being sufficient.
- **low** FR/NFR cross-reference verification (§Traceability Overview, lines 972-983) - spot-check passed but no automated check has been done. *Fix:* before architecture starts, machine-validate that every FR/NFR ID cited in the table resolves.

## Shape fit - strong

The PRD shape matches the product: B2B SaaS, multi-stakeholder (contributor, reviewer, project owner, tenant admin, developer/automation, compliance), chain-top (PRD → UX → architecture → stories). UJs with named protagonists are load-bearing here and they are present and substantive. The CLI/MCP-as-MVP-scope choice is justified at lines 209-211 by the actual nature of the product (automation builders and AI agents as first-class actors), so the over-formalization concern does not apply - the parity rigor is earned.

Brownfield/integration context is correctly handled: the PRD is greenfield for ChatBot but explicit about depending on existing Hexalith bounded contexts (Hexalith.Projects, Hexalith.Parties, Hexalith.Folders, Hexalith.Tenants, Hexalith.EventStore, Keycloak, Aspire, Hexalith.FrontComposer, Hexalith.Memories, Hexalith.Commons - lines 88-90). The Context Ownership section (lines 428-435 and 581-589) draws boundaries cleanly. The source-context-date footnote (line 90) is exactly the right move for a chain-top PRD: "If any sibling bounded-context contract, PRD, or project-context artifact changes materially after this date, architecture must re-check..."

Single-release MVP shape is appropriate given the thesis is "the full vertical path or it does not prove the bet" (lines 938-945).

### Findings

- (none of meaningful severity)

## Mechanical notes

- **Glossary drift / missing terms:** "Policy snapshot," "Operating baseline" / "Tenant or deployment profile," "Idempotency key," and the disambiguation of "Scoped AI context" / "AI context package" vs "Context package" need glossary entries. (See Downstream usability finding.)
- **ID continuity:** FR1-FR96 and NFR1-NFR70 appear contiguous with no gaps or duplicates on review.
- **Cross-reference resolution:** Traceability table (lines 972-983) and Functional Acceptance Guidance matrix (lines 1001-1006) FR ranges appear to resolve, but no automated check was performed.
- **Assumptions index roundtrip:** A1-A9 defined in §"Open Assumptions and Decisions" (lines 1149-1159). Inline references appear at lines 207 (A1), 209 (A3), 229 (A2, A3, A8, A9), 1003 (A1, A2, A9), 1004 (A5, A8), 1005 (A6), 1006 (A3). A4 (operating baselines) and A7 (no external portal) are defined but not inline-cited - acceptable since they are platform-baseline statements rather than scoped to a specific FR group.
- **UJ protagonist naming:** All eight UJs and the System Journey have named protagonists with carried context (Amira appears in UJ1 and UJ8 consistently). Clean.
- **State model naming:** The transition arrow at line 374 and the state table at lines 380-391 are aligned. The list at lines 700-712 reorders the same states - cosmetic only.
- **Risk class table (lines 1057-1063):** Internally consistent with FR39-FR46. The "Mixed requests inherit the strictest applicable risk class" rule (line 1065) is a good downstream contract.
- **Command/query catalog (lines 647-693):** Concrete and contiguous. Closes the prior validation finding about underspecified operations.
- **Required sections present:** Executive Summary, Project Classification, Success Criteria, Product Scope (with Minimum Release Slice), User Journeys, Domain-Specific Requirements, Innovation, B2B Governance, Project Scoping, Functional Requirements (Glossary, Traceability, Acceptance Guidance), Open Assumptions, Non-Functional Requirements covering Security/Privacy, Reliability, Performance, Integration, Operability, Auditability, Recovery, Accessibility, and Validation. For an "adequate / B2B SaaS / chain-top / single-release MVP" stake level, no required section is missing.
