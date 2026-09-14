# Validation Report — Hexalith.ChatBot

- **DESIGN.md:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- **EXPERIENCE.md:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- **Run at:** 2026-09-14T15:36:30+02:00
- **Reviewer records:** 27 findings — 0 critical, 11 high, 13 medium, 3 low. Counts include explicitly attributed overlaps between independent reviewers.

## Overall verdict

The spine pair is structurally disciplined and unusually complete at the surface, component, accessibility, and visual-inheritance levels, but it is **not yet safe to finalize as the downstream implementation contract**. The main blockers are source-to-spine drift in canonical workflow states and unresolved or stale safety-governance language; downstream architecture and story authors could otherwise implement state machines, approval behavior, or association thresholds that differ from the current PRD and addendum.

The additional lenses reinforce that verdict. Accessibility is broadly strong but the larger-screen handoff conflicts with the stated WCAG 2.2 AA reflow floor. Hexalith/Fluent conformance is only partial because several canonical component bases do not exist in the pinned package and the accordion ownership contract is not implementable per surface. The persisted governance review adds unsafe ambiguity around batching, queue authority, suppressed candidates, and correction commit timing.

## Category verdicts

- Flow coverage — **adequate**
- Token completeness — **adequate**
- Component coverage — **strong**
- State coverage — **thin**
- Visual reference coverage — **strong**
- Bloat & overspecification — **adequate**
- Inheritance discipline — **broken**
- Shape fit — **strong**

## Findings by severity

### Critical (0)

None.

### High (11)

**[Rubric · Flow coverage] — Association disposition omits the `[T_low, T_high)` band** (`EXPERIENCE.md:34,174,295`; `addendum.md:22-26`; `prd.md:161`)

The UX repeatedly names only scores below `T_low` as entering review, while the normative rule sends every score below `T_high` to `NeedsReview`; `T_low` affects ranking and presentation only.

Fix: State `score < T_high` as the disposition rule everywhere and describe `T_low` separately as a reviewer-presentation threshold.

**[Rubric · State coverage] — “Canonical state families” is not canonical to the sources** (`EXPERIENCE.md:134-148`; `prd.md:489-565`)

The section renames authoritative states, adds unsupported states, and omits several source-owned families. An enum-level consumer cannot safely implement it as written.

Fix: Inherit exact source enum names and transitions, include every user-visible owned family, and put friendly labels in a separate mapping column.

**[Rubric · Inheritance discipline] — Classifier-indeterminate behavior remains normatively contradictory** (`addendum.md:42-47`; `prd.md:1424-1449`; `EXPERIENCE.md:33,126,175,378`; `.memlog.md:5`)

The addendum routes an indeterminate classifier to approval-required, while PRD NFR15a prevents any AI proposal commit. EXPERIENCE.md introduces a third conditional formulation without an explicit approved override.

Fix: Reconcile the upstream clauses or record an approved override, then use one durable-write and review rule across all normative documents.

**[Rubric · Inheritance discipline] — Mandatory approval is still described as temporary** (`DESIGN.md:215`; `EXPERIENCE.md:32,130`; `.memlog.md:6`; `prd.md:1278`; `addendum.md:42-47`)

The current sources make all six boundary-effect classes unconditionally non-downgradable, but the spines still imply a future reconciliation point.

Fix: Append a memlog supersession and make the six-effect mandatory-approval rule unconditional throughout both spines.

**[Accessibility] — Larger-screen handoff contradicts WCAG 2.2 AA Reflow** (`DESIGN.md:149-155`; `EXPERIENCE.md:21-23,42-59,255-263`; `prd.md:1522-1526`)

The handoff can remove M1/M2 information or actions at 320 CSS pixels or 400% zoom. A bounded essential two-dimensional grid may scroll, but the whole workflow cannot require a larger viewport.

Fix: Make handoff optional; keep every in-scope task operable at 320 CSS pixels without page-level horizontal scrolling, and add 320px/400%-zoom acceptance per M1/M2 surface.

**[Hexalith/Fluent] — Canonical bases name unavailable or ambiguous components** (`DESIGN.md:71-73,95-97,113-118`; `Directory.Packages.props:226-227`)

The map names `FluentProgress` instead of the pinned `FluentProgressBar`, names a nonexistent `FluentToolbar`, and leaves “FrontComposer sheet” and “Fluent input controls” non-concrete.

Fix: Bind every pattern to exact available pinned-v5/FrontComposer components, or document an explicit no-equivalent fallback with required semantics.

**[Hexalith/Fluent] — Accordion ownership is not implementable per surface** (`DESIGN.md:35-43,62-64,89-91,149-155`; `EXPERIENCE.md:55-59`; Hexalith UX instructions lines 43-51)

Section components map individually to `FluentAccordion`, while no contract identifies a single owning accordion or its `FluentAccordionItem` children. A literal implementation can create sibling or nested accordions.

Fix: Add a surface-composition table naming the one owner, every item, the default-expanded primary item, and content outside it; retain Association Review as the only carve-out.

**[Governance] — Batch approval exceeds the source's allowed scope** (`EXPERIENCE.md:203`; `prd.md:1535-1540`)

The UX omits frozen-field comparisons and allows effect classes for which one-click batching is forbidden.

Fix: Require the complete frozen-field comparison and exclude irreversible, external-send, file-exposing, external-tool, and on-behalf actions; preserve individual visibility, authorization, revision checks, decision slots, and atomic audit.

**[Governance] — Admin queue actions omit current Project-authority gating** (`EXPERIENCE.md:53,179,205,325-330`; `prd.md:1391-1397`)

The UX combines aggregate queue controls with per-item retry/requeue/quarantine/dismiss. The latter require current Project authority and full revalidation.

Fix: Separate opaque partition controls from per-item controls and bind every per-item action to current Project authority, requester authority, revision, policy, and audit checks.

**[Governance] — Parity language can disclose suppressed candidates** (`EXPERIENCE.md:95,111,214`; `prd.md:1361-1362,1469-1470`)

“Same suppressed unsafe set” can reveal identities, evidence, ordering, or cardinality that must remain existence-neutral.

Fix: Require equivalent omission/redaction semantics across UI, CLI, and MCP; never expose suppressed-set content or cardinality outside authorized audit.

**[Governance] — Correction commit timing is ambiguous on invalidation failure** (`EXPERIENCE.md:176,313-319`; `prd.md:1478-1494`)

The flow can be read as committing a correction before invalidation-queue and canonical-audit readiness, contrary to the fail-closed source contract.

Fix: Define a pre-commit failure state that leaves the predecessor authoritative; reserve `Correcting` and `Correction-delayed` for successfully committed corrections awaiting downstream acknowledgement.

### Medium (13)

**[Rubric · Flow coverage] — UJ1 omits source identifiers `S1a` and `FR28a-f`** (`EXPERIENCE.md:44,273`; `prd.md:611,1160,1193,1242-1247`; `epic10-chat-surface-elaboration.md:12-29`)

The governed composer behavior exists, but source extraction cannot recover its current surface and requirement identifiers.

Fix: Add `S1a — Governed chat composer` to IA and map UJ1 to `FR21-FR28f · S1, S1a, S3`.

**[Rubric · Token completeness] — Component frontmatter relies on unrecognized extension fields** (`DESIGN.md:16-118`)

The Google DESIGN.md linter reports 68 warnings for `base`, `emphasis`, `distinction`, and `default`; generic consumers may ignore the metadata.

Fix: Version these as a supported project extension for every extractor or move semantic anatomy/inheritance into prose and keep recognized visual token fields in frontmatter.

**[Rubric · Inheritance discipline] — Task Intent provenance uses an ambiguous shared-kernel term** (`EXPERIENCE.md:93`; `addendum.md:28-36,38-43`)

The source independently versions `TaskIntentDetector` and explicitly does not share its kernel with association or action-risk classification.

Fix: Use `detector_version` or the exact deployed detector artifact version.

**[Accessibility] — Operational auto-refresh lacks pause/apply-update behavior** (`EXPERIENCE.md:104-116,169-180,182-205,240`; `prd.md:1321-1322,1486`)

Queue/dashboard refresh can silently insert, remove, or reorder content while someone is reading or operating a row.

Fix: Accumulate changes behind an accessible update action or provide pause/manual refresh; preserve the active item and focus, announce a concise summary, and document any essential-live exception.

**[Accessibility] — Browser disconnect and response-loss recovery are unspecified** (`EXPERIENCE.md:83-116,138-148,169-194`; `prd.md:1001-1007,1355,1540`)

The contract lacks accessible states for disconnected-before-submit, disconnected-while-pending, and response-lost-after-admission.

Fix: Preserve drafts/selections/focus, announce connectivity once, retain stable operation identity, reconcile status on reconnect, and clearly distinguish not-sent, unknown/pending, and prior-outcome states.

**[Hexalith/Fluent] — Named contingency reference targets stale RC2 APIs** (`DESIGN.md:125`; `fluent-ui-v5-contingency.md:5,42,97-100,209-212`; `Directory.Packages.props:226-227`)

The implementation reference instructs RC2 while the shared catalog pins RC5.

Fix: Refresh the guide to the current central pin/APIs or replace the reference with a current version-bound component reference.

**[Hexalith/Fluent] — Local contract incompletely states reuse-over-hand-rolling** (`DESIGN.md:125-127,149-151`; `implementation-conformance-addendum-2026-07-17.md:27-30`; Hexalith UX instructions lines 10-17)

Raw CSS and four HTML controls are covered, but equivalent JavaScript widgets and third-party component substitution are not explicitly rejected.

Fix: Restate the full equivalence rule and extend conformance guards to equivalent third-party/JavaScript controls, with reviewed no-equivalent exceptions only.

**[Governance] — Approval wording is temporary and its actor scope is broad** (`DESIGN.md:215`; `EXPERIENCE.md:32,86,130`; current PRD/addendum)

The rule should be permanent for AI-mediated effects without accidentally routing direct authorized human writes through the AI-proposal approval flow.

Fix: State both boundaries explicitly.

**[Governance] — Detector-unavailable behavior and detector version are incomplete** (`EXPERIENCE.md:93,142,173`; `addendum.md:18-20,28-43`)

The UX uses “detector/kernel version” and does not fully bind detector-unavailable to authorized review before any risk classification.

Fix: Use `detector_version`; add `detector-unavailable`/`NeedsReview` and prohibit classifier invocation until authorized task intent exists.

**[Governance] — Admin audit invariant is incomplete** (`EXPERIENCE.md:102-103,177,205,321-332`; `prd.md:1391-1397`)

Not every allowed/rejected admin operation or qualifying dashboard read is explicitly audited, and prohibited actors are not universally excluded from admin assignment.

Fix: Add one tenant-admin/dashboard invariant covering actor, scope, opaque affected items, outcome, timestamp, rejection, and the service-client/AI assignment prohibition.

**[Governance] — Audit provenance lacks identity-evolution and annotation semantics** (`EXPERIENCE.md:107,180,313-317,350-356`; `addendum.md:154-160`; `prd.md:1365-1368`)

The contract does not show unresolved successor identities or distinguish appended human rationale from canonical evidence.

Fix: Preserve original ID, expose unresolved-current-identity status and safe review, and mark notes as non-authoritative annotations.

**[Governance] — Operational diagnostics are not runbook-complete** (`EXPERIENCE.md:104-106,179`; `prd.md:1533`)

No single authorized per-item pattern guarantees tenant ID, mailbox ID, workflow item ID, and last transition actor/from-state.

Fix: Add an authorized diagnostic disclosure with the complete NFR44 field set and bounded-admin redaction.

**[Governance] — Surface parity is not separated from the AI allowlist** (`EXPERIENCE.md:207-224`; `addendum.md:58-73`; `prd.md:1408-1414`)

MCP parity can be misread as AI invocability, although product operation catalog, surface exposure, and AI allowlist are separate memberships.

Fix: Show all three memberships and state that parity applies only after actor/surface authorization.

### Low (3)

**[Rubric · Inheritance discipline] — Unrelated product name remains in outbound authority rule** (`EXPERIENCE.md:226-231`)

The rule says “Drift returns,” importing an example-product name into a load-bearing contract.

Fix: Replace it with `Hexalith.ChatBot` or “the pre-send revalidation.”

**[Hexalith/Fluent] — Product Brief input registry is not fully reproducible** (`product-brief-Hexalith.ChatBot.md:6-14`)

Some inputs omit the repository `references/` prefix and two historical Party/Tenant brief paths are absent.

Fix: Normalize paths and provide immutable archived references or mark the historical evidence unavailable.

**[Governance] — SLO surface does not distinguish qualification backlog from publication** (`DESIGN.md:196`; `EXPERIENCE.md:51,106,179,328`; `addendum.md:189-249`)

Before A11 completes, the surface can read as a published SLO catalog even though it is only qualification evidence.

Fix: Label it “SLO qualification backlog” until every required target, budget, signal, route, and burn test passes.

## Reviewer files

- `review-rubric.md` — current selected rubric walker
- `review-accessibility.md` — current selected accessibility lens
- `review-fluent-conformance.md` — current selected Hexalith/Fluent UI V5 lens
- `review-governance-safety.md` — persisted reviewer artifact included because synthesis consumes every `review-*.md`
