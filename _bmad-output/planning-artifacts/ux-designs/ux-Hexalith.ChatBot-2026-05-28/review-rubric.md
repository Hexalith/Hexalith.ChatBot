# Spine Pair Review — Hexalith.ChatBot

## Overall verdict

The spine pair is **broken as a current downstream contract** despite strong structure and substantial behavioral detail. Its nine flows semantically cover the nine source journeys, all four frontmatter sources resolve, all 17 locally declared components pair across the spines, and visual-reference handling is explicit; however, the contract cannot be source-extracted deterministically because source names and S1–S10 mappings are not preserved, the DESIGN color map violates the required token type, and current safety requirements for expired evidence and correction propagation are absent. The 2026-05-10 PRD validation report is stale context, not the current source verdict: the current PRD and approved addendum contents govern this review.

## 1. Flow coverage — thin

Extracted the nine source journeys at `prd.md:319-427`, the S1–S10 source surface inventory at `prd.md:519-539`, 111 current FR identifiers (FR1–FR96 plus suffixed requirements), and 77 current NFR identifiers (NFR1–NFR70 plus suffixed requirements). EXPERIENCE.md has nine numbered Key Flows, each with a named protagonist, numbered steps, an explicit climax, and a failure path (`EXPERIENCE.md:256-355`), but it does not preserve a machine-checkable journey/requirement/surface crosswalk.

### Findings

- **high** None of the nine Key Flow headings preserves its source journey heading verbatim or carries its UJ/System-journey identifier; for example, source `Journey 1: Business Contributor Requests AI Help From a Project Conversation` becomes `Flow 1 - Project contributor asks AI for help`. The only explicit mapping note links Flow 1 to Journey 8 rather than identifying Flow 1 as UJ1 (`prd.md:321-335`; `EXPERIENCE.md:258-270`). A consumer cannot deterministically join the flows to the current 188 requirement identifiers or tell which later suffixed requirements were incorporated. *Fix:* use each exact source journey heading (including `System Journey`) and add a compact `Source mapping` line per flow with its UJ, FR/NFR ranges, and S-surface identifiers.
- **high** The source declares ten UI surfaces, while the UX IA declares nine differently partitioned surfaces without a crosswalk; S4 Correction Surface, S6 Outbound Approval, S7 Cross-surface Attribution View, and S10 Admin Queue Operations are only implicit or merged. Flow 6 exercises CLI behavior but never lands on S7, and Flow 5 reviews queues but does not exercise S10 retry/requeue/quarantine/dismiss behavior (`prd.md:519-539`; `EXPERIENCE.md:32-46,303-321`). *Fix:* add an S1–S10-to-IA mapping and extend the relevant flows so each source surface is reached and its load-bearing action and failure path are exercised; add a flow if a merged surface cannot close cleanly.

## 2. Token completeness — broken

Extracted 15 color tokens, five typography roles, three radii, nine spacing tokens, 17 component token objects, and every `{path.to.token}` occurrence in DESIGN.md. All 35 unique brace references resolve, radii and spacing are valid CSS dimensions, component references resolve recursively, and load-bearing contrast targets are stated for light, dark, and forced-colors behavior (`DESIGN.md:12-129,153-166`).

### Findings

- **critical** All 15 `colors` values are `var(--...)` strings rather than the hex strings required by the DESIGN.md color schema; none supplies a light/dark hex pair (`DESIGN.md:12-27`; `design-md-spec.md:9-19`). The project correctly requires inherited Fluent roles instead of hard-coded product colors, but putting inherited CSS variables into a schema field whose values downstream consumers mirror as hex makes the machine contract invalid. *Fix:* do not redeclare unchanged Fluent roles as local `colors`; express inheritance in Brand & Style/Colors and keep only genuine product color deltas in the schema. If the downstream schema requires local colors, define a repository-approved translation that satisfies both the hex type and the no-theme-redefinition rule before retaining this map.
- **medium** Each typography role is defined only with `note`, although the typography object type permits `fontFamily`, `fontSize`, `fontWeight`, `lineHeight`, and `letterSpacing`; the semantic `note` exception is documented for native platform conventions, not an inherited web UI system (`DESIGN.md:28-38`; `design-md-spec.md:15-18,45-49`). *Fix:* omit unchanged Fluent typography roles and state inheritance in prose, or encode only actual brand-layer deltas with the supported fields.

## 3. Component coverage — thin

The 17 local component identifiers in DESIGN frontmatter all have a substantive visual bullet in DESIGN.md.Components and a substantive behavioral row in EXPERIENCE.md.Component Patterns; normalized names pair one-to-one (`DESIGN.md:53-129,211-229`; `EXPERIENCE.md:63-85`). Inherited Fluent/FrontComposer primitives may remain inherited when the product defines no delta, but the current sources introduce additional product-specific deltas that are not represented in either component inventory.

### Findings

- **high** Current source-mandated product components are missing from both component contracts: the `informational` / `actionable` classification badge with detected intent, the collapsed-by-default `AI summary` block with model/time/evidence provenance, the detailed “why this project” panel, and the per-reference freshness chip that blocks approval when expired (`prd.md:523-526,1194-1201,1441`; `DESIGN.md:211-229`; `EXPERIENCE.md:63-85`). These are load-bearing distinctions, not generic Fluent defaults, and a downstream builder cannot infer their visual anatomy and behavioral rules safely. *Fix:* add identically named component tokens/specs and behavioral rows for these four concepts, including non-color distinction, source-default expansion behavior, provenance, freshness states, and approval blocking.
- **medium** Several inherited primitives acquire product-specific behavior outside Component Patterns—skeleton busy-region replacement, error summary focus, dialog/sheet focus containment and single-level stacking, and queue filters—yet have no named local row or explicit inheritance-plus-delta mapping (`EXPERIENCE.md:123-140,150-172`). *Fix:* add rows for only those primitives with a behavioral delta, naming the exact FrontComposer/Fluent component to inherit; leave unchanged library components omitted.

## 4. State coverage — broken

Walked all nine UX IA surfaces against the general state table, per-surface state matrix, and feedback matrix (`EXPERIENCE.md:87-140`). Cold load, empty, selection/focus landing, validation, retryable and terminal error, degraded dependency, permission-denied/redacted, and most pending/success states are well covered, but two current source safety contracts are absent.

### Findings

- **critical** The experience has no `fresh` / `stale` / `expired` evidence-reference states and no `evidence-expired` approval block, although NFR48 requires a visible timestamp/state chip for every evidence reference and forbids approval against expired evidence (`prd.md:1441`; `EXPERIENCE.md:89-140`). “Stale filters” on Operational Queues is unrelated. *Fix:* add evidence freshness to Association Review, AI Action Review, Conversation Detail, and Audit Investigation; define the chip announcement, timestamp, stale treatment, expired treatment, and disabled-with-reason approval path.
- **high** The canonical correction substates `Correcting` and `Correction-delayed` are missing. EXPERIENCE.md jumps from “Corrected association” / “correction applied” to completion, omitting progress, estimated completion, the AI-context-use block, p95 breach handling, responsible owner, next safe action, and P2 escalation required by FR91a/NFR17a (`prd.md:450-451,1324-1325`; `EXPERIENCE.md:98,114,205-213`). *Fix:* add both states to Conversation Detail, Correction/Association Review, Files and Context, Operational Queues, and Audit Investigation as applicable, and map each to feedback, focus, recovery, and AI-action gating.

## 5. Visual reference coverage — strong

There are no files or directories under `imports/`, `mockups/`, or `wireframes/` in the UX workspace, so there are no orphans or unspecific artifact references. The spine-only decision explicitly names every affected IA surface and states once that future visual references may extend the handoff but the spines win on conflict (`EXPERIENCE.md:30`).

### Findings

- None.

## 6. Bloat & overspecification — adequate

Most prose carries downstream decisions rather than decorative narrative, and the detailed state/accessibility material is proportionate to the product's governance risk. The main overspecification is a local visual scale that duplicates the inherited UI system; the remaining repetition is minor.

### Findings

- **high** DESIGN.md declares a local 4/8/12px radius scale and nine spacing/density values while simultaneously saying buttons, inputs, menus, tabs, drawers, dialogs, cards, panels, radii, density, and spacing inherit Fluent/FrontComposer defaults (`DESIGN.md:39-52,136,180-190,203-209`). This creates two sources of truth and invites the raw CSS/theme recreation prohibited by the repository UX baseline (`references/Hexalith.AI.Tools/hexalith-ux-instructions.md:22-36`). *Fix:* remove inherited pixel scales or mark and justify only true product deltas, expressed through Fluent component parameters or Fluent 2 tokens.
- **low** The same touch-target contract is stated in Accessibility Floor and Responsive & Platform (`EXPERIENCE.md:201,244`). *Fix:* keep the normative thresholds in Accessibility Floor and reference that rule from Responsive & Platform.

## 7. Inheritance discipline — broken

All four relative `sources:` paths resolve from both spines, the two spines use the same source list, the 17 local component names normalize consistently across their sections, and every explicit token reference resolves. The current contract still fails inheritance discipline because it neither resolves a load-bearing source conflict nor clearly distinguishes binding source material from historical validation metadata.

### Findings

- **high** The source pair conflicts on the risk taxonomy: the PRD exposes four user-relevant outcomes (`Low-risk read-only`, `Approval-required`, `Denied`, `Unsupported`), while the approved addendum says the classifier emits only `low-risk` or `approval-required` and rejects disallowed commands before classification. The Risk chip and AI Action Review say “risk class” without committing which taxonomy is displayed or how denied/unsupported outcomes are represented (`prd.md:1215-1224`; `addendum.md:25-33`; `DESIGN.md:219`; `EXPERIENCE.md:77,334-343`). *Fix:* record the UX reconciliation explicitly: distinguish classifier output from user-visible disposition/effect labels, define the chip vocabulary, and map each source outcome to review/refusal behavior.
- **medium** The fourth source is a 2026-05-10 validation report whose frontmatter and verdict describe an earlier PRD path and only 96 FRs / 70 NFRs; the current PRD records that those findings were addressed and now includes suffixed requirements (`DESIGN.md:7-11`; `EXPERIENCE.md:6-10`; `prd-validation-report.md:1-21,119-174`; `prd.md:42-50`). Listing the report beside binding PRD sources without a role marker invites downstream consumers to treat its stale `Critical` verdict as current contract evidence. *Fix:* remove it from binding `sources`, or classify it explicitly as historical/context-only validation evidence with its target revision.
- **medium** DESIGN.md names `Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md` as the authoritative implementation source, but that path does not resolve from the repository or spine location; the actual root-declared submodule path is `references/Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md` (`DESIGN.md:142`). *Fix:* use the resolving repository-relative path, preferably as an inline link, and add it as an explicitly inherited UI-system source if consumers are expected to load it.

## 8. Shape fit — adequate

DESIGN.md follows the canonical order exactly: Brand & Style → Colors → Typography → Layout & Spacing → Elevation & Depth → Shapes → Components → Do's and Don'ts (`DESIGN.md:132-240`). EXPERIENCE.md contains every required default in order, and both conditional sections are justified: Responsive & Platform is required by multi-form-factor use, while Inspiration & Anti-patterns is supported by the intake references (`EXPERIENCE.md:15-355`; `.decision-log.md:7-17`).

### Findings

- **low** `Product-Specific Concerns` contains only an Internationalization row that restates the fuller Localization contract immediately above it, so the invented section does not earn a separate place (`EXPERIENCE.md:224-230,250-254`). *Fix:* remove the section and keep English/French scope in Localization, or expand it only if a distinct product concern cannot live in an existing required section.

## Mechanical notes

- Both YAML frontmatter blocks parse structurally; DESIGN.md contains 15 colors, five typography roles, three radii, nine spacing tokens, and 17 component objects. The schema-type defects are reported in §2.
- All four source paths resolve from both spines: current PRD, approved addendum, product brief, and historical PRD validation report. The stale report is not used as the current source verdict.
- The current source catalog contains 111 distinct FR identifiers and 77 distinct NFR identifiers; the historical validation report's counts of 96 and 70 predate the suffixed requirements.
- All 35 unique `{path.to.token}` references in DESIGN.md resolve. EXPERIENCE.md contains no `{path.to.token}` references; it links visual behavior to DESIGN.md by normalized component name and the general `DESIGN.md.Components` statement.
- All 17 locally declared component names pair across DESIGN.md and EXPERIENCE.md after kebab-case/sentence-case normalization. Missing current source components are listed in §3.
- No `imports/`, `mockups/`, or `wireframes/` files exist, and the spine-only/spines-win decision is explicit.
- No Mermaid blocks are present in either spine.
- Journey labels and S1–S10 surface names are not preserved verbatim; see §§1 and 7. Canonical lifecycle names also drift to prose labels, with the load-bearing omissions called out in §4.
