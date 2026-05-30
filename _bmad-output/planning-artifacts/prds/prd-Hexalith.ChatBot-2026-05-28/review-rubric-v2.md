# PRD Quality Review — Hexalith.ChatBot (v2, post-rewrite)

## Overall verdict

The v2 rewrite materially upgrades the PRD: all six prior Critical findings have been retired by concrete artifacts — the three-increment M0/M1/M2 sequencing replaces the unbuildable big-bang slice (C1, C6); the A9a evaluation-dataset assumption gives precision/recall provenance (C2); T_high/T_low have a score domain, signals, safe defaults and a guardrail in `addendum.md §Confidence Thresholds` (C3); NFR15a enumerates fail-closed code paths as an invariant (C4); FR81a + `addendum.md §Shared Command Pipeline` makes parity structural (C5); the Tenant Policy Schema, risk classifier, command allowlist, idempotency table, and replay isolation contract land as first-class artifacts (H1–H11 mostly resolved or downgraded). The PRD now reads as a buildable contract grounded in tested-or-falsifiable assumptions, with two `[NOTE FOR PM]` callouts at real tensions (CLI/MCP deferral, FR41/NFR46 fatigue) — meeting the rubric's prior tension-smoothing finding.

What still holds it back from a "Good" grade is the cost of rapid editing: dangling cross-references (`addendum.md §Operating Baselines` doesn't exist; `FR48 / M5` is a stale label; addendum line 112 references "M7" which is not a PRD section; `Decomposition guidance (M3)` introduces an undefined "M3" namespace orthogonal to M0/M1/M2 increments), one self-referential section (§Service Client Permissions points at itself for the enumeration it should contain), and Tenant Policy Schema knobs cited inline (`tenant-policy.audit.retention`, `tenant-policy.ai-context.retention`, `data.residency`, `mailbox.authenticity-strictness`, `tenant-policy.approval.priority-weights`) that aren't enumerated in the schema's M0/M1/M2 knob set. None of these are conceptually broken — they're rough edges of an editing pass that didn't fully reconcile its own additions. Grade: **Fair** (one High finding for dangling/self-referential structural refs; no Critical; multiple thin-but-fixable cross-refs).

## Decision-readiness — adequate

The v2 rewrite directly addresses the prior pass's most damning rubric finding: zero `[NOTE FOR PM]` callouts and zero Open Questions. v2 now carries three substantive `[NOTE FOR PM]` callouts at real tensions:

- Line 219: deferring CLI/MCP from M0 to M1 weakens the parity thesis between increments; mitigation named.
- Line 307: product name ("ChatBot") vs M0/M1 reality (no chat surface in M0); positioning decision deferred to pilot kickoff.
- Line 938: original 25-item must-have list was 6–12 person-year backlog packaged as one release; three-increment sequencing reconciles team to scope.
- Line 1065: team-size sensitivity (frontend / security engineer rotation) and the recovery rule (extend duration, not compress trust controls).
- Line 1212: FR41 + FR52 vs NFR46 (approval fatigue) tension; explicit MVP default with a measurable revisit condition.

The MVP-allowlist-is-one-command decision (M0 allowlist = `Project.AppendConversationMessage` only, per `addendum.md §Command Allowlist v0`) is stated as a decision, not buried — answering prior H4. The "Rejected/Failed are terminal" call (line 434, 437) explicitly disclaims the prior contradictory "unless reprocessed" wording, resolving H9.

The remaining gap: no explicit Open Questions section. The PRD chose to fold opens into A1–A11 (Open Assumptions table with revisit conditions and owners), which is defensible — assumptions with revisit triggers do the work an Open Questions section would, more durably. But a decision-maker scanning for "what's not yet decided" has to read the assumptions table; a one-line pointer at the top would help. The trade-off is also adequately named (line 215: "the cost of keeping CLI and MCP in the MVP is reduced breadth elsewhere…"), but no equivalent "what we gave up by sequencing M0 before parity" trade-off is named symmetrically.

### Findings

- **low** Open Assumptions table does the work of Open Questions but no pointer (§Executive Summary) — Decision-makers scanning for "what's not yet decided" have to find A1–A11 themselves. *Fix:* add a one-line pointer in §Executive Summary or §Success Criteria: "Open product decisions are captured in §Open Assumptions and Decisions (A1–A11) with owners and revisit triggers."

## Substance over theater — strong

The prior pass's "adjective-only NFRs at user-trust hot spots" Medium finding is resolved on every named NFR:

- NFR40 (degraded-state language): the adjective "user-appropriate language with next-action guidance" is now anchored to FR77's versioned message catalog with a `0` uncategorized-state count observable per release.
- NFR42 (degraded-state surface): names the four required display elements (state enum, scope, owner role, next safe action) and a synthetic-check observable.
- NFR44 (runbook-ready diagnostics): names the nine required fields and a sampling-based defect detection rule.
- NFR46 (approval fatigue): now has a prioritization formula, grouping rule, push-notification rate ceiling, per-reviewer backlog SLO, and a measurable rubber-stamp rate (`> 15% in rolling 7-day = fatigue present`).
- NFR48 (evidence freshness): names the `fresh/stale/expired` enum, the staleness window inheritance (NFR6), the approval-blocking rule, and a synthetic-check observable.

The Innovation section's "What Makes This Special" duplication (prior Low finding) is now bounded: line 70's "key product bet" paragraph carries an explicit disclaimer ("the product team's working thesis pending pilot validation… not an external market claim") and §Market Context & Competitive Landscape disclaims external competitive claims. The §Risk Mitigation under Innovation now points at the operational §Risk Mitigation Strategy rather than restating — duplication removed (line 636 explicitly).

NFR boilerplate that survived is mostly load-bearing and product-specific: NFR15a (10 enumerated code paths × fail-closed conditions); NFR50a (audit completeness as reconstructability, not field presence); NFR49a (tamper-evident = append-only WORM + hash-chained envelopes with named retention/redaction mechanism).

The seven user journeys retain real protagonists (Amira, Marc, Elena, Priya, Nora, Leo, Sofia + System Journey) and each drives capability decisions — no persona theater.

### Findings

(none — strong)

## Strategic coherence — strong

The three-increment sequencing (M0 = vertical thesis path, M1 = parity + governance breadth, M2 = operations + recovery) gives the PRD a coherent arc that maps to a unified thesis: "AI as a governed project actor proves out by sequencing trust foundations before breadth." Feature prioritization follows from the thesis, not from "what's easy first" — the parity bet (CLI/MCP) is deferred to M1 rather than dropped, the recovery objectives (RPO/RTO) are deferred to M2 rather than treated as nice-to-have, the WCAG 2.2 AA bar is scoped per-increment so it's reachable by one frontend engineer.

Success metrics validate the thesis rather than just measure activity:

- Association precision/recall (95%/90%) anchored to the A9a evaluation dataset with a named owner (Test Architect).
- Critical false-positive count (`0` involving unauthorized projects) calibrates the fail-closed claim.
- Approval-fatigue counter-metric (NFR46 rubber-stamp rate < 15%) named explicitly to prevent the governance promise from dying under volume.
- Misclassification rate (≤ 1% on evaluation, ≤ 2% on production-sampled disagreements) treats classifier accuracy as a first-class risk.
- Pilot adoption thresholds (`70%` evidence-resolution, `40%` time reduction, `30%` manual-update reduction) are labeled starter values per A11 with a 2–4 week baseline measurement window before M0 release — answering prior Low "pilot thresholds are not baselined" finding.

The MVP scope kind is clearly "problem-solving / platform" (governed AI project workspace), and the scope logic matches: deliberate narrowness (email only, one mailbox pattern, one allowlisted command in M0, deferred CLI/MCP) trades breadth for trust-foundation depth.

### Findings

(none — strong)

## Done-ness clarity — adequate

The prior "soft outcomes on FR23, FR26, FR27, FR42, FR67, FR76, FR77" High finding is resolved on every named FR by explicit **Accept when** clauses with enumerated required fields and observable conditions. Specifically:

- FR23 (line 1172): why-panel must display originating signal class, matched value, confidence score, threshold band, decision actor, decision timestamp, superseding-correction link.
- FR26 (line 1176): `informational | actionable` badge, next-action affordance, reproducibility from the same kernel.
- FR27 (line 1178): visual distinction + label `AI summary`, one-line provenance string, collapsible source, WCAG 2.2 non-color rule.
- FR42 (line 1214): proposed command name, input files with redaction state, recipients, sender authority class, risk classification with input tuple, policy snapshot ID, expected post-state, four-decision set.
- FR67 (line 1254): queue/health view fields (name, depth/status, oldest item age, owner role, item-detail link), `healthy/degraded/failed/unknown` enum, freshness timestamp.
- FR76 (line 1277): `enabled / disabled-with-reason / not-applicable-hidden` states with finite reason set.
- FR77 (line 1279): versioned message catalog with stable code, headline, one-sentence reason, safe next-action affordance.

The "Task-intent FRs lack a data contract" Medium finding is resolved: FR35 (line 1204) names the ten required record fields and explicit precision/recall targets (≥ 80% / ≥ 75% by M0 release, ratcheting to ≥ 90% / ≥ 85% by M1) tied to A9a.

NFR60 (line 1441) enumerates the in-scope WCAG surfaces per increment (M0: ambiguous association review, AI action approval, project conversation view; M1: rejection/defer, approval-policy config, admin operational view; M2: M2 dashboards) — answering the prior "core UI review workflows unenumerated" Medium.

The remaining concern: FRs **without** explicit Accept-when clauses still leave done-ness implicit on the merely-stated capability ("the system can…"). FR21, FR24, FR28, FR29–FR34, FR43–FR50 (excluding FR48a–d), FR51–FR75 (excluding FR75a–g), FR79–FR80, FR81–FR89 (excluding FR81a), FR90–FR96 — none carry Accept-when clauses. The Functional Acceptance Guidance section (line 1116) states that "each FR group must be decomposed into acceptance scenarios before story implementation" and lists minimum scenario coverage for four high-risk groups (FR1–12, FR39–46, FR55–63, FR81–89), which mitigates this. Story authors have a contract to lean on. But the acceptance-guidance matrix doesn't cover FR47–50 (outbound), FR64–75 (reliability/ops), FR76–80 (review queues — only FR76 has an Accept-when), or FR87–96 (workflow/test) — so for those groups the done-ness pressure shifts onto the next downstream pass.

The Functional Acceptance Guidance dependencies cite A1, A2, A3, A5, A6, A8, A9 — but A9 should now be A9a (the dataset with cardinality and refresh cadence). This is a glossary-style inconsistency, not a content error.

### Findings

- **medium** Functional Acceptance Guidance coverage is incomplete (§ lines 1133–1138) — Four FR groups have explicit acceptance-scenario matrices, but FR47–50 (outbound), FR64–75 (reliability), FR76–80 (review queue except FR76), FR81 baseline, FR90–96 (workflow/test) have no group-level acceptance matrix. Story authors will work harder on these. *Fix:* add minimum scenario rows for the missing groups, or label the omission explicitly ("these groups carry per-FR acceptance bullets and don't require a group matrix").

- **low** Acceptance Guidance cites A9 not A9a (§ line 1135) — A9a is the more specific assumption with cardinality and refresh cadence; A9 is the parent. *Fix:* change "depends on A1, A2, and A9" → "A1, A2, A9, A9a" (and similar for other rows).

## Scope honesty — strong

Non-Goals are explicit and located where they do real work:

- "Out of scope for MVP" list at §Product Scope line 278.
- "Explicitly out of scope for this release" list at §Project Scoping line 1024.
- "B2B SaaS Non-Goals" §line 910.
- Explicit non-goal callouts inside Increment M0 (line 243: "Outbound communication, CLI, MCP, multi-tenant rollout, and operational dashboards are explicitly out of scope for M0").

Assumptions are tagged inline AND indexed:

- 11 assumptions A1–A9 + new A9a, A10, A11 in the Open Assumptions table (line 1316) — each with owner and revisit condition.
- A9a has cardinality (`500` by M0, `2000` by M1), label taxonomy, refresh cadence, and adversarial-example protocol — earned the prior H5 fix.
- A10 explicitly tags RPO/RTO as starter values pending M2 continuity drill, with the drill mechanism specified — earned the prior H1 fix on those numbers.
- A11 explicitly tags pilot adoption thresholds as starter values pending 2–4 week baseline measurement.

Inline `[ASSUMPTION A9a]` and `[ASSUMPTION A11]` tags at lines 145–146, 180–183, 1204, 1302 cite the indexed assumption — round-trip works.

De-scoping is proposed honestly: line 1063 ("MVP is not shippable until M2 closes; M0 alone proves the thesis to the pilot cohort but does not constitute MVP completion"), the per-increment safety floor enumeration (line 225, line 1061), and the contingency rules (line 1063, line 1065 [NOTE FOR PM]).

Open-items density is calibrated to stakes: 11 assumptions and 5 `[NOTE FOR PM]` callouts across a 1,458-line PRD covering a high-stakes governed-AI product is appropriate — the prior pass's zero-callout pattern is fully reversed without overcorrecting.

### Findings

(none — strong)

## Downstream usability — adequate

Substantial improvements over the prior pass:

- **Glossary** now contains the prior-missing load-bearing terms: `Policy snapshot` (line 1096), `Operating baseline` (1095), `Idempotency key` (1091), `Allowlisted command` (1077), `Aspire` (1078), `MCP` (1093), `Approval-required` (1080), `Low-risk` (1092), `Evaluation dataset` (1085), `Risky AI action` (1097), `MVP parity set` (1094), `Tenant policy` (1100), `Fail closed` (1090), `Context package` with the synonym disambiguation (1084: "Context package" ≡ "Scoped AI context"). Resolves prior Medium and L5/L7.
- **Traceability table** (line 1104) maps every UJ + System Journey to primary FRs, primary NFRs, and validation focus.
- **UJs** each have a named protagonist; the System Journey is labeled as such. UI Surface Inventory (line 511) maps surfaces S1–S10 back to UJs and FRs, giving UX a clean handoff.
- **Lifecycle terminology** is now reconciled: the canonical state table (line 429) is in §Shared Workflow Contract; §Association Lifecycle and States (line 786) explicitly says "This section repeats the state list for the §Association Lifecycle frame; the canonical contract is the §Shared Workflow Contract table." Prior Low finding resolved.
- **Context Ownership** has a single canonical section (line 666) that says "This section is the **canonical** ownership listing for the PRD" and tells the §Executive Summary and §Shared Workflow Contract framings to point here. Resolves prior L6.
- **Data Governance Surface** table (line 486) lists every ChatBot-owned record class with retention class, redaction sensitivity, isolation surface, and owner increment — answering prior M2/M3.

Cross-reference integrity is **mostly** intact but has dangling targets introduced by the rapid editing pass:

- **NFR42a (line 1407) references `addendum.md §Operating Baselines`** — this section does not exist in the addendum. The addendum has 10 sections; "Operating Baselines" is not one of them. (NFR42a says it's "created during M2," which means the section is deferred — but that should be flagged inline, not cited as if it exists.)
- **§Service Client Permissions (line 721) is self-referential.** The section header says "permissions… are defined in the §Service Client Permissions section below; each service-client class has an enumerated scope and authorized command/query set." But the section IS the section it's pointing at, and contains no enumerated scopes. Either the enumeration was lost in editing, or this is a placeholder pointing at architecture work.
- **FR48 line 253 references "FR48 / M5"** — there is no M5 in this PRD. The increments are M0/M1/M2. M5 is a stale label from a prior outline.
- **FR22 line 1170 introduces `Decomposition guidance (M3)`**, and FR74 line 1262 also tags `Decomposition guidance (M3)`. "M3" is not defined anywhere in the PRD — increments stop at M2. This appears to be an artifact of mapping the prior adversarial finding M3 ("capability described at the noun level") into the FR body, but using "M3" as a tag confuses the reader because M0/M1/M2 are increment labels.
- **Addendum line 112 (ID Evolution Contract) says "Referenced from M7 and §Increment M1 / M2."** — M7 is the prior adversarial finding (M7. Stable identifiers across bounded contexts) but is being cited as if it's a PRD section.
- **Tenant Policy Schema knobs cited inline are not all in the schema's enumerated knob list.** Referenced inline but not enumerated in `addendum.md §Tenant Policy Schema`: `tenant-policy.audit.retention` (line 492), `tenant-policy.ai-context.retention` (line 501), `data.residency` (line 876), `mailbox.authenticity-strictness` (line 1227 / addendum 126), `tenant-policy.approval.priority-weights` (line 1412). The schema's M0 list is the five knobs at addendum lines 64–68; M1/M2 are described in prose but not enumerated, so these inline citations are forward references to schema entries that don't yet exist. For a versioned, change-controlled schema this is a contract gap.

### Findings

- **high** Dangling cross-references introduced by rapid editing (multiple locations) — Five separate dangling/stale refs: NFR42a → nonexistent `addendum §Operating Baselines`; FR48 → stale `M5` label; FR22/FR74 → undefined `(M3)` namespace; addendum line 112 → "Referenced from M7"; §Service Client Permissions self-referential. None are conceptually broken; all are editing rough edges. *Fix:* either (a) create the missing addendum §Operating Baselines stub with a "to be populated in M2" note, or label the citation `(deferred to M2 — not yet authored)`; (b) remove the `/ M5` from FR48 line 253; (c) replace "(M3)" with a non-increment label like "(decomposition note)" or move into the Functional Acceptance Guidance matrix where decomposition guidance belongs; (d) reword addendum line 112 to "Resolves the prior adversarial M7 finding on cross-context identifier evolution"; (e) author the §Service Client Permissions enumeration or replace with a forward pointer ("Service-client scopes are an architecture concern; the PRD requires that each service-client class has an enumerated scope and authorized command/query set — to be authored in the architecture solution").

- **medium** Tenant Policy Schema is missing inline-referenced knobs (§ Tenant Policy Schema in addendum) — Five knobs referenced inline (`tenant-policy.audit.retention`, `tenant-policy.ai-context.retention`, `data.residency`, `mailbox.authenticity-strictness`, `tenant-policy.approval.priority-weights`) aren't in the schema's enumerated M0/M1/M2 knob list. For a "first-class versioned artifact under change control," this is a contract gap that admins can't validate against. *Fix:* extend the schema's M1 and M2 enumerated knob lists in the addendum to include every knob referenced in the PRD body and NFR42a/NFR46.

- **low** FR group cross-references in §Functional Acceptance Guidance use legacy A9 (§ line 1135) — A9a is the operational dataset with cardinality and cadence; A9 is parent. *Fix:* update each row's dependency list to cite A9a where the contract actually rests.

## Shape fit — strong

The PRD's shape matches the product: a multi-stakeholder B2B SaaS with meaningful UX, where UJs with named protagonists carry weight (governed AI is an explicit cross-stakeholder concern — contributors, project owners, admins, compliance, automation builders, AI actors all have first-class roles). The seven UJs + System Journey are load-bearing — each one drives capability requirements that show up in the FR catalog, and the Traceability table makes the mapping explicit.

This is a chain-top PRD (feeds UX → architecture → stories), so downstream usability matters — and the PRD invests heavily in it (Glossary, Traceability, UI Surface Inventory, Data Governance Surface, Functional Acceptance Guidance).

Brownfield references are accurate: nine sibling Hexalith bounded contexts named in §Integration List with `(M1+)` annotations on CLI and MCP. The Material-change re-check protocol (line 92) names the System Architect as the trigger owner and gives a 5-business-day SLA — answering the prior M1 adversarial finding.

No over-formalization (e.g., no excessive UJ density for an internal tool — this is genuinely multi-stakeholder); no under-formalization (consumer-product-style hand-waving on a B2B governance product is absent).

### Findings

(none — strong)

## Resolution status of prior findings

### Prior Critical (6 → all retired)

- **C1 — Minimum Release Slice unbuildable.** RESOLVED. Three-increment M0/M1/M2 sequencing (line 221+), per-increment scope, per-increment WCAG, per-increment audit-event set, per-increment dependency-failure handling. Team load reconciled in §Resource Requirements (line 936) with a [NOTE FOR PM] explicit about the trade-off.
- **C2 — Precision/recall targets have no dataset.** RESOLVED. A9a (line 1325) gives cardinality, label taxonomy, refresh cadence, adversarial protocol; Test Architect named as single owner. Targets are now labeled calibration targets, not contractual commitments.
- **C3 — T_high/T_low fictional.** RESOLVED. `addendum.md §Confidence Thresholds` gives score domain `[0.0, 1.0]`, scoring kernel (deterministic-signals scorer), safe defaults (`T_high=0.90`, `T_low=0.60`), calibration protocol against A9a, guardrails on threshold changes (security-sensitive, can't lower below floor without documented evaluation).
- **C4 — Fail closed has silent escape hatches.** RESOLVED. NFR15a (line 1355) is an invariant table of 10 code paths × fail-closed conditions × audit-writer-down rule, with no path having an "audit unavailable → continue" branch. NFR15 + line 860 explicitly remove the prior "security-sensitive only" exception.
- **C5 — Parity enforced by aspiration.** RESOLVED. FR81a (line 1287) + `addendum.md §Shared Command Pipeline` define a single command-handling pipeline at the architectural layer. Surface adapters cannot replicate pipeline stages; "Parity violation = invariant violation." Contract tests verify; they do not enforce.
- **C6 — Single release unbuildable on the named team.** RESOLVED. Three-increment sequencing with named per-increment safety floors and a team-growth/team-shrink contingency rule (line 1065 [NOTE FOR PM]).

### Prior High (12 → 1 partial, 11 resolved or downgraded)

- **Done-ness clarity (FR23/26/27/42/67/76/77).** RESOLVED via explicit Accept-when clauses on each.
- **H1. Magic constants.** PARTIALLY RESOLVED. Pilot thresholds (A11), RPO/RTO (A10), NFR46 rate ceilings, NFR50a `99.5%`, NFR17a correction propagation latency, FR35 precision/recall, NFR60 per-increment surfaces — all explicitly labeled starter values with calibration mechanism OR derivations cited. The remaining constants (NFR24 2s p95, NFR25 10s p95, NFR26 5s/30s, NFR27 page size 100, NFR43 default thresholds) are still un-derived but are now anchored to NFR23 "operating baseline" with a quarterly-review obligation, which is acceptable for an MVP.
- **H2. Tenant policy is universal escape hatch.** RESOLVED via `addendum.md §Tenant Policy Schema` (versioned, change-controlled, sensitivity classes, M0/M1/M2 introduction). PARTIAL because the M1/M2 knob set is described in prose rather than enumerated (see Medium finding above).
- **H3. Risk classifier unnamed.** RESOLVED via `addendum.md §Risk Classifier` (M0 = tag-and-heuristic, misclassification fallback, reviewer-disagreement audit chain, error-rate tracked per NFR50a).
- **H4. Allowlist load-bearing without an allowlist.** RESOLVED via `addendum.md §Command Allowlist v0` (M0 = exactly one command: `Project.AppendConversationMessage`) and `§Command Allowlist v1` (M1 = full catalog minus tenant-tagged `disallowed-for-AI`).
- **H5. Evaluation dataset unowned.** RESOLVED via A9a (Test Architect = single owner; Product Lead consults).
- **H6. Audit completeness undefined.** RESOLVED via NFR50a (audit completeness = reconstructability ≥ 99.5% per rolling 7-day window per tenant).
- **H7. Idempotency unspecified per operation.** RESOLVED via `addendum.md §Idempotency Keys` (eight operation classes × key composition × replay window × equivalence rule × conflict response).
- **H8. Approval fatigue hand-waved.** RESOLVED via NFR46 (prioritization formula, grouping rule, push-notification rate ceiling, per-reviewer backlog SLO, rubber-stamp observable).
- **H9. Rejected/Failed contradiction.** RESOLVED via line 434/437 explicit "terminal; reprocessing creates a new workflow instance with `superseded_by_workflow`/`supersedes_workflow` audit linkage."
- **H10. WCAG on one frontend engineer.** RESOLVED via NFR60 per-increment scoping of in-scope surfaces.
- **H11. No-bypass admin + admin dashboards unworkable.** RESOLVED via FR75a–FR75g (tenant-admin permission model with see-only / operate / policy / mailbox / compliance scopes and audit obligation on every admin action).

### Prior Medium (17 → mostly resolved; remaining categories carried forward)

Mostly resolved. Remaining gaps surfaced as Medium findings above (Functional Acceptance Guidance coverage; Tenant Policy Schema enumeration).

### Prior Low (17 → mostly resolved or moot)

L2 (Aspire definition) RESOLVED via Glossary; L5/L7 (glossary load-bearing terms / MCP) RESOLVED via Glossary; L6 (ownership listed three ways) RESOLVED via canonical-ownership pointer pattern; L1 (ChatBot name vs email MVP) RESOLVED via the [NOTE FOR PM] at line 307; L4 (tamper-evident mechanism) RESOLVED via NFR49a; L8/L9 (innovation duplication / external claim) RESOLVED via disclaimer + Risk Mitigation merge.

## New issues introduced by the rewrite

### High

- **H-NEW-1. Five dangling or self-referential cross-refs.** Listed in detail under Downstream usability. The PRD is now substantively right but mechanically rough at the seams. None of these block decision-making, but they will produce confusion when story authors and architects pull sections out alone.

### Medium

- **M-NEW-1. Tenant Policy Schema knob enumeration incomplete.** Five knobs referenced inline in PRD/addendum aren't in the schema's enumerated M0/M1/M2 list. For a "first-class versioned artifact under change control," this is a contract gap.

- **M-NEW-2. Functional Acceptance Guidance covers four FR groups; five groups (FR47–50, FR64–75, FR76–80 ex-FR76, FR81 baseline, FR90–96) are not covered.** Most affected FRs have per-FR detail or are simple-enough capability statements that group-level coverage may not be needed — but the omission should be acknowledged rather than implicit.

### Low

- **L-NEW-1. A9 vs A9a inconsistency in §Functional Acceptance Guidance dependencies.**
- **L-NEW-2. No pointer from §Executive Summary to A1–A11 Open Assumptions** (could be moot given the table location, but a one-line breadcrumb would help.)
- **L-NEW-3. "M3" used as a label inside FR22 and FR74 decomposition guidance** alongside M0/M1/M2 increment labels — even if intended as "mechanical guidance row 3" or similar, the namespace collision with increments is confusing.

## Mechanical notes

- **FR ID continuity.** FR1–FR96 contiguous; FR13a, FR15a, FR17a, FR42a, FR48a, FR48b, FR48c, FR48d, FR49a, FR50a, FR55a, FR75a–g, FR81a, FR91a, FR95a are added without breaking the original sequence — clean.
- **NFR ID continuity.** NFR1–NFR70 contiguous; NFR9a, NFR13a, NFR15a, NFR17a, NFR42a, NFR49a, NFR50a are added without breaking — clean.
- **Assumptions Index roundtrip.** A1–A8 platform/product baseline; A9 + A9a (dataset); A10 (RPO/RTO); A11 (pilot thresholds). Inline `[ASSUMPTION A9a]` and `[ASSUMPTION A11]` tags at lines 145–146, 180–184, 1204, 1302 cite the indexed assumption; A1, A2, A3, A5, A6, A8 are cited in the Functional Acceptance Guidance rows. A4 and A7 are platform-baseline and not inline-cited (acceptable; matches prior pattern).
- **`[NOTE FOR PM]` callouts.** Five present (lines 219, 307, 938, 1065, 1212) — reverses the prior zero-callout finding. All at real tensions.
- **Glossary drift.** "Scoped AI context" ≡ "Context package" disambiguated in glossary (line 1084). "Approval-required" / "low-risk" / "denied" risk-class terms are stable across glossary, addendum §Risk Classifier, FR41/FR43 prose, and the Risk Classification Defaults table (line 1196).
- **Lifecycle state list.** Now consistent: §Shared Workflow Contract table (line 429) is canonical; §Association Lifecycle and States (line 786) explicitly defers to the canonical table.
- **UJ protagonists.** All named (Amira, Marc, Elena, Priya, Nora, Leo, Sofia + System Journey) — no floating UJs.
- **Cross-reference issues.** Five dangling/self-referential refs documented above. These are the main mechanical cost of the rapid rewrite.
- **Decision log.** Not re-examined in this pass; the prior validation report flagged it as sparse — the rewrite has surfaced rationale into the PRD body rather than the log, which is acceptable.
