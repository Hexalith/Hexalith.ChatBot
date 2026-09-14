# Spine Pair Review — Hexalith.ChatBot

## Overall verdict

The updated spine pair is **not ready to finalize as a downstream contract**. It now has strong end-to-end structural coverage—nine complete source journeys, explicit S1–S10 closure, all 34 product components paired across both spines, a valid Google DESIGN.md document, and an explicit spine-only visual posture—but safety-critical threshold wording, canonical state vocabulary, and an unresolved classifier fallback conflict remain ambiguous for architecture and story implementation.

## 1. Flow coverage — adequate

Pass 1 extracted all nine source journeys and their requirement/surface mappings. EXPERIENCE.md provides nine corresponding Key Flows, each with an exact journey identifier, a named protagonist, numbered steps, an explicit climax, and a failure path (`EXPERIENCE.md:271-382`). The two remaining misses affect exact extraction rather than journey existence.

### Findings

- **high** The Foundation decision, Association Review state inventory, and UJ2 failure path say scores below `T_low` enter `NeedsReview`, but the normative threshold contract says **every** score below `T_high` enters `NeedsReview`; `T_low` controls presentation only (`EXPERIENCE.md:34,174,295`; `addendum.md:22-23`; `prd.md:158`). The current wording leaves the `[T_low, T_high)` band without an explicit disposition in the UX contract. *Fix:* replace the disposition condition with `score < T_high` everywhere, and state separately that `T_low` only filters or orders reviewer presentation.
- **medium** UJ1 maps `FR21–FR28` and surfaces `S1, S3`, while the source maps through `FR28f` and defines the governed composer as `S1a` (`EXPERIENCE.md:273`; `prd.md:313,568,1084,1117,1166`). S1a behavior is present and folded into S1 in the IA, but its source identifier and suffixed requirement endpoint are not extractable from the flow. *Fix:* map UJ1 to `FR21–FR28f` and include `S1a` explicitly in the source-surface crosswalk and UJ1 mapping.

## 2. Token completeness — adequate

Pass 1 found no unresolved token reference, missing color value, or invalid frontmatter structure. The absence of local color, typography, radius, and spacing maps is deliberate: DESIGN.md delegates unchanged values to Fluent UI v5 and FrontComposer and states the required 4.5:1 text, 3:1 non-text/focus, forced-colors, light, and dark behavior (`DESIGN.md:121-166`). The official `@google/design.md` linter reports 0 errors, 68 warnings, and 1 informational result.

### Findings

- **medium** All 34 component objects use two extension fields drawn from `base`, `emphasis`, `distinction`, and `default`; these fields are accepted but unrecognized by the Google DESIGN.md consumer, producing all 68 warnings (`DESIGN.md:16-118`). The file is schema-valid, but generic machine consumers will ignore the component metadata that carries the inheritance/anatomy distinction. *Fix:* either document and version these fields as a project extension understood by downstream consumers, or move this metadata into the Components prose and reserve frontmatter component properties for recognized DESIGN.md visual fields when a local delta exists.

## 3. Component coverage — strong

Pass 1 extracted 34 component identifiers from DESIGN.md frontmatter. Every identifier has a substantive visual specification in DESIGN.md Components, a substantive behavioral specification in EXPERIENCE.md Component Patterns, and a resolving `{components.*}` reference; the two inventories pair exactly (`DESIGN.md:16-118,167-206`; `EXPERIENCE.md:61-105`). Fluent UI and FrontComposer primitives with no product delta are explicitly inherited rather than redundantly redefined.

### Findings

- None.

## 4. State coverage — thin

Pass 1 walked every IA surface. The per-surface matrix covers applicable loading, empty, focus/selection, validation, permission/redaction, stale/expired evidence, offline/degraded dependency, success, retryable, conflict, and terminal states across S1–S10 (`EXPERIENCE.md:42-53,169-180`). The shared state-family table is not, however, safe to consume as the canonical implementation contract.

### Findings

- **high** The section titled “Canonical state families” changes authoritative state names and omits owned families. Examples include an unsupported Association `Proposed` state; Task Intent `under-review` and `not-actionable` instead of `NeedsReview` and `Dismissed`; and presentation-oriented AI states such as `proposal-ready`, `execution-pending`, and `retryable-failure` instead of the source `AwaitingApproval → Approved/Rejected/RevisionRequested/Cancelled → Executing → Succeeded/Failed`. Participant resolution, attachment handling, low-risk assistance, governed chat, outbound email, exact command execution, audit projection, service-client permission, and resolution annotation are not preserved as families (`EXPERIENCE.md:136-148`; `prd.md:483-545`). *Fix:* reproduce or directly reference the authoritative state families and exact enum names, then put any user-facing labels in a separate presentation-label column so UI copy cannot be mistaken for domain state.

## 5. Visual reference coverage — strong

Pass 1 found no `imports/`, `mockups/`, or `wireframes/` files or directories, so there are no orphaned or unspecific visual references. EXPERIENCE.md explicitly declares the package spine-only and states that future visuals are illustrative while the spines win on conflict (`EXPERIENCE.md:36`).

### Findings

- None.

## 6. Bloat & overspecification — adequate

Pass 2 found the detail proportionate to the product's authorization, audit, accessibility, recovery, and cross-surface risks. The update avoids duplicating inherited pixel scales or theme values; dense behavior is mostly normalized into crosswalks, component/state tables, and compact flows rather than decorative narrative.

### Findings

- None.

## 7. Inheritance discipline — broken

All three `sources`, all three `supplementalSources`, and `.memlog.md` resolve from both spines; nested frontmatter sources in the supplemental documents also resolve. Component names are identical across frontmatter and both component sections, and the approved interim rule that all six boundary-crossing effect classes require approval is recorded consistently. Two source/vocabulary decisions remain unsafe to inherit.

### Findings

- **high** The addendum says an indeterminate risk-classifier result is normalized to `approval-required`, while PRD NFR15a lists `risk classifier indeterminate` as a fail-closed condition under which no AI action proposal record is written (`addendum.md:46`; `prd.md:1356`). The PRD Authority Map delegates classifier semantics to the addendum but also delegates quality requirements to the NFR catalog; `.memlog.md` contains no explicit reconciliation of this write/no-write conflict. EXPERIENCE.md silently chooses proposal creation when other inputs are safe (`EXPERIENCE.md:33,126,175,378`). *Fix:* reconcile the two normative sources or record an explicit approved override in the decision log, then make PRD, addendum, and both spines state the same durable-write behavior.
- **medium** EXPERIENCE.md introduces `allowed-read-only` as a user-visible disposition and labels intent provenance `detector/kernel version`, while the source vocabulary is `low-risk` / admitted `accepted` and the TaskIntentDetector is explicitly independent rather than a shared kernel (`EXPERIENCE.md:93,97,124,377`; `addendum.md:29-46`). *Fix:* use the canonical source terms, or add an explicit mapping table from internal state/classification to user-facing label; rename provenance to the exact detector/artifact version owned by the source contract.

## 8. Shape fit — strong

Pass 2 confirms DESIGN.md follows the canonical order exactly: Brand & Style → Colors → Typography → Layout & Spacing → Elevation & Depth → Shapes → Components → Do's and Don'ts (`DESIGN.md:121-218`). EXPERIENCE.md contains every required default section, with Responsive & Platform and Inspiration & Anti-patterns present for their triggered conditions; the added Governed Action Boundary section earns its place by centralizing a load-bearing safety invariant.

### Findings

- None.

## Mechanical notes

- Both YAML frontmatter blocks parse structurally and carry matching source, supplemental-source, decision-log, status, and project metadata.
- Google DESIGN.md lint result: 0 errors, 68 warnings, 1 info; the warnings are the component-extension issue reported in §2, not schema-invalid component objects.
- Component contract: 34 frontmatter objects, 34 visual rows, 34 behavioral rows, and 34 unique resolving EXPERIENCE.md component references.
- Journey contract: nine source journeys and nine flows; all flows have named protagonists, numbered steps, climax beats, and failure paths.
- IA contract: S1–S10 are all represented and state-covered; the folded S1a identifier miss is reported in §1.
- Every declared direct source, supplemental source, decision log, and nested supplemental frontmatter source resolves. No broken brace reference or Mermaid block was found.
- No visual-reference directories or files exist; the spine-only and spines-win posture is explicit.
