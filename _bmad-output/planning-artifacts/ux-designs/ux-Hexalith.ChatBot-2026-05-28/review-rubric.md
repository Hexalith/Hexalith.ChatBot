# Spine Pair Review — Hexalith.ChatBot

## Overall verdict

The spine pair is structurally disciplined and unusually complete at the surface, component, accessibility, and visual-inheritance levels, but it is **not yet safe to finalize as the downstream implementation contract**. The principal blockers are source-to-spine drift in canonical workflow states and unresolved or stale safety-governance language; downstream architecture and story authors could otherwise implement different state machines or approval behavior from the current PRD/addendum.

## 1. Flow coverage — adequate

Pass 1 extracted eight numbered source journeys plus the System Journey from the PRD. EXPERIENCE.md supplies nine corresponding Key Flows with named protagonists, numbered steps, explicit climax beats, and failure paths (`EXPERIENCE.md:269-382`); the misses are in exact source mapping and one association threshold boundary, not in the existence or narrative shape of the journeys.

### Findings

- **high** The association decision is incomplete for the score band from `T_low` through just below `T_high`: the UX repeatedly names only “below `T_low`” as the review condition (`EXPERIENCE.md:34,174,295`), while the normative source says every result below `T_high` enters `NeedsReview` and `T_low` affects ranking/presentation only (`addendum.md:22-26`; `prd.md:161`). A downstream consumer could infer an unintended automatic or undefined disposition for `[T_low, T_high)`. *Fix:* state `score < T_high` as the disposition rule everywhere and describe `T_low` separately as a reviewer-presentation/ranking threshold only.
- **medium** UJ1's flow mapping stops at `FR28` and names `S1, S3`, but the source trace and UI inventory extend it through `FR28f` and define the governed composer as `S1a` (`EXPERIENCE.md:44,273`; `prd.md:611,1160,1193,1242-1247`; `epic10-chat-surface-elaboration.md:12-29`). The behavior is present in the flow, yet an extractor cannot recover the current source identifiers. *Fix:* represent `S1a — Governed chat composer` explicitly in the IA crosswalk and map UJ1 to `FR21-FR28f · S1, S1a, S3`.

## 2. Token completeness — adequate

Pass 1 extracted 34 `components` entries and 34 `{components.*}` references. Every brace reference resolves, no local color tokens lack hex values, and the deliberate absence of local color/typography/spacing/radius maps is supported by explicit Fluent UI v5/FrontComposer inheritance plus concrete WCAG contrast commitments (`DESIGN.md:16-118,121-166`; `EXPERIENCE.md:81-116`).

### Findings

- **medium** All 34 component objects encode semantic inheritance/anatomy through `base`, `emphasis`, `distinction`, or `default`, but those are not recognized DESIGN.md component sub-tokens; the current `@google/design.md` v0.4.0 linter returns 0 errors, 68 warnings, and 1 informational result for these fields (`DESIGN.md:16-118`). Generic DESIGN.md consumers may therefore ignore the machine-readable metadata on which the `{components.*}` references depend. *Fix:* either declare and version these fields as a project extension supported by every downstream extractor, or move the semantic base/anatomy metadata into the Components prose and reserve frontmatter component properties for recognized visual token fields.

## 3. Component coverage — strong

Pass 1 extracted 34 product component identifiers from DESIGN.md frontmatter. Each has a substantive visual-contract row in DESIGN.md, an identically named behavioral-contract row in EXPERIENCE.md, and exactly one resolving `{components.*}` reference (`DESIGN.md:16-118,167-206`; `EXPERIENCE.md:77-116`). Fluent UI and FrontComposer primitives are explicitly inherited and need no duplicated product-level visual contract.

### Findings

- None.

## 4. State coverage — thin

Pass 1 walked the ten source surfaces represented by the eight consolidated IA/state rows. Applicable cold-load, empty, selection/focus, validation, permission/redaction, degraded dependency, evidence freshness, success, retry, conflict, and terminal behavior is generally present (`EXPERIENCE.md:42-59,169-194,233-253`), but the section labelled canonical cannot be safely source-extracted as the implementation state contract.

### Findings

- **high** “Canonical state families” renames source states, adds unsupported states, and omits multiple authoritative families. Examples include Association `Proposed`, Task Intent `under-review`, and AI Action `proposal-ready`/`execution-pending`/`retryable-failure`, whereas the source contracts use the exact association transitions, Task Intent `NeedsReview`, and AI proposal/approval/execution states such as `AwaitingApproval`, `RevisionRequested`, `Executing`, `Succeeded`, and `Failed`; participant resolution, attachment handling, governed chat, low-risk assistance, outbound email, exact command/audit-projection states, service-client permission, notification, data-erasure, and legal-hold families are also absent (`EXPERIENCE.md:134-148`; `prd.md:489-545,548-565`). The per-surface rows do not repair an enum-level mismatch labelled canonical. *Fix:* inherit the authoritative source matrices by exact enum name (or reference them directly), include every user-visible owned family, and place friendly presentation labels in a separate mapping column rather than presenting them as canonical states.

## 5. Visual reference coverage — strong

Pass 1 found no `imports/`, `mockups/`, or `wireframes/` directories or files, so there are no orphaned or unspecific visual references. EXPERIENCE.md explicitly declares the package spine-only, identifies the IA and component/state tables as the implementation reference, and states once that future visuals are illustrative while the spines win on conflict (`EXPERIENCE.md:36`).

### Findings

- None.

## 6. Bloat & overspecification — adequate

Pass 2 found the length proportionate to the product's authorization, audit, safety, localization, accessibility, responsive, and cross-surface risks. The pair avoids inherited pixel/theme duplication; most dense material is expressed in crosswalks and tables, and the product-specific Governed Action Boundary earns its place by centralizing a load-bearing UX decision (`DESIGN.md:125-127`; `EXPERIENCE.md:118-132`).

### Findings

- None.

## 7. Inheritance discipline — broken

All three direct `sources`, all three `supplementalSources`, and `.memlog.md` resolve from both spines; the supplemental documents' declared source paths also resolve. Journey headings and all 34 product component names are consistent, and all EXPERIENCE.md token references resolve, but safety-critical source conflicts and stale naming prevent reliable inheritance.

### Findings

- **high** The normative sources still disagree about an indeterminate `ActionRiskClassifier` result. The addendum normalizes it to `approval-required`, while PRD NFR15a makes classifier indeterminacy a fail-closed condition for which no AI proposal record commits (`addendum.md:42-47`; `prd.md:1424-1449`, especially `prd.md:1436`). EXPERIENCE.md selects a third, conditional formulation—create a reviewable proposal only when all other inputs are safe—without an explicit approved source resolution (`EXPERIENCE.md:33,126,175,378`; `.memlog.md:5`). *Fix:* reconcile the two upstream normative clauses or record an explicit approved override, then make the source pair and UX pair state the same durable-write and review behavior.
- **high** The spines still frame mandatory approval for the six boundary effects as an interim rule that lasts only until upstream reconciliation completes (`DESIGN.md:215`; `EXPERIENCE.md:32,130`; `.memlog.md:6`). The current finalized PRD and approved addendum now make the rule unconditional and structurally non-downgradable (`prd.md:42-48,1278`; `addendum.md:42-47`). This stale condition implies a future downgrade point that the governing sources prohibit. *Fix:* append a memlog supersession noting that the upstream contract is reconciled, and make the six-effect mandatory-approval rule unconditional throughout both spines.
- **medium** Task Intent provenance is specified as `detector/kernel version`, but the source contract says `TaskIntentDetector` is independently versioned and does not share a kernel with association or action-risk classification (`EXPERIENCE.md:93`; `addendum.md:28-36,38-43`). *Fix:* name the exact `detector_version` or deployed detector artifact version and remove the ambiguous shared-kernel alternative.
- **low** The outbound-authority rule says “Drift returns” the safe failure codes, leaving an unrelated example-product name in a load-bearing Hexalith.ChatBot contract (`EXPERIENCE.md:226-231`). *Fix:* replace `Drift` with `Hexalith.ChatBot` or “the pre-send revalidation.”

## 8. Shape fit — strong

Pass 2 confirms DESIGN.md follows the canonical section order exactly: Brand & Style → Colors → Typography → Layout & Spacing → Elevation & Depth → Shapes → Components → Do's and Don'ts (`DESIGN.md:121-218`). EXPERIENCE.md contains every required default section, plus Responsive & Platform and Inspiration & Anti-patterns for their triggered conditions; the invented Governed Action Boundary section is justified by the product's safety contract (`EXPERIENCE.md:19-382`).

### Findings

- None.

## Mechanical notes

- Finding count: **0 critical, 4 high, 3 medium, 1 low**.
- Frontmatter source resolution: 3/3 direct sources, 3/3 supplemental sources, and 1/1 decision log resolve in each spine. The implementation reference named at `DESIGN.md:125` exists when interpreted as repository-root-relative.
- DESIGN.md lint: `npx --yes @google/design.md@0.4.0 lint DESIGN.md` returns 0 errors, 68 warnings, and 1 info; all warnings are the unrecognized component sub-token issue in §2.
- Token/component contract: 34 frontmatter component objects, 34 visual rows, 34 behavioral rows, and 34 unique resolving EXPERIENCE.md component references.
- Journey contract: eight numbered UJs plus the System Journey; all nine have named protagonists, numbered steps, climax beats, and failure paths.
- Visual artifacts: no `imports/`, `mockups/`, or `wireframes/` directories/files exist; the spine-only and spines-win posture is explicit.
- No Mermaid blocks, malformed brace references, or unresolved declared source paths were found.
