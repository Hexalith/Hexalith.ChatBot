# Validation Report — Hexalith.ChatBot

- **DESIGN.md:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- **EXPERIENCE.md:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- **Run at:** 2026-09-13T13:48:18+02:00

## Overall verdict

The spine pair is broken as a current downstream contract despite strong structure and substantial behavioral detail. Its nine flows semantically cover the nine source journeys, all four frontmatter sources resolve, all 17 locally declared components pair across the spines, and visual-reference handling is explicit; however, the contract cannot be source-extracted deterministically because source names and S1–S10 mappings are not preserved, the DESIGN color map violates the required token type, and current safety requirements for expired evidence and correction propagation are absent.

The specialist reviews reinforce that the original June accessibility and governance foundation is unusually strong, but later source changes did not reach the peer spines. Accessibility is thin for current governed-chat semantics, while governance and safety are broken by an unresolved approval-boundary contradiction and missing evidence-freshness, correction, bounded-admin, and informed-approval contracts.

## Category verdicts

- Flow coverage — thin
- Token completeness — broken
- Component coverage — thin
- State coverage — broken
- Visual reference coverage — strong
- Bloat & overspecification — adequate
- Inheritance discipline — broken
- Shape fit — adequate

## Findings by severity

### Critical (3)

**[Rubric / Token completeness] — Color tokens violate the DESIGN.md schema (§ `DESIGN.md:12-27`)**

All 15 `colors` values are Fluent CSS-variable strings rather than the hex strings required by the DESIGN.md schema. The inherited Fluent posture is correct, but placing those references in a hex-valued machine contract is not.
Fix: remove unchanged inherited Fluent roles from local `colors` and explain inheritance in prose, or establish a repository-approved schema translation before retaining local color tokens.

**[Rubric / State coverage] — Evidence freshness and approval blocking are absent (§ `prd.md:1441`; `EXPERIENCE.md:89-140`)**

No `fresh`, `stale`, or `expired` evidence states exist, and no `evidence-expired` path blocks approval. A downstream implementation can therefore approve against expired evidence.
Fix: define freshness chips, timestamps, announcements, stale/expired treatments, and the disabled-with-reason approval path on every evidence-bearing surface.

**[Governance & Safety] — The authoritative sources contradict each other on approval boundaries (§ `prd.md:1215-1237`; `addendum.md:25-33,65-74`)**

Six boundary-crossing action classes are described both as necessarily approval-required and as tenant-downgradable to low-risk; the PRD also exposes denied/unsupported while the addendum defines a two-output classifier. Backend and UI teams can implement different safety boundaries while following different authoritative passages.
Fix: approve one canonical evaluation order and user-visible disposition taxonomy. Until reconciled, require human approval for all six boundary-crossing effect classes and align the PRD, addendum, policy schema, and both spines.

### High (16)

**[Rubric / Flow coverage] — Source journey names are not preserved (§ `prd.md:319-427`; `EXPERIENCE.md:256-355`)**

All nine UX flows are structurally complete, but none keeps the exact source journey heading or a stable UJ/System Journey identifier.
Fix: use the exact source headings and add a compact mapping from each flow to its UJ, FR/NFR ranges, and S-surface identifiers.

**[Rubric / Flow coverage] — S1–S10 do not map deterministically to the nine IA surfaces (§ `prd.md:519-539`; `EXPERIENCE.md:32-46`)**

Correction Surface, Outbound Approval, Cross-surface Attribution View, and Admin Queue Operations are implicit or merged, leaving surface closure unprovable.
Fix: add an explicit S1–S10 crosswalk and ensure each source surface's load-bearing action and failure path appears in a flow.

**[Rubric / Component coverage] — Current product-specific affordances are missing (§ `prd.md:1194-1201,1441`; `DESIGN.md:211-229`; `EXPERIENCE.md:63-85`)**

The spines lack paired visual and behavioral contracts for informational/actionable classification, AI-summary provenance, “why this project,” and evidence freshness.
Fix: add identically named components in both spines, including non-color distinctions, disclosure behavior, provenance, freshness, and approval blocking.

**[Rubric / State coverage] — Canonical correction states are missing (§ `prd.md:450-451,1324-1325`; `EXPERIENCE.md:98,114`)**

The UX jumps to corrected/completed and omits `Correcting` and `Correction-delayed`, propagation progress, AI-context blocking, p95 breach handling, owner, and escalation.
Fix: add both states across conversation, association/correction, files/context, queues, and audit, with feedback, focus, recovery, and AI-action gating.

**[Rubric / Bloat & overspecification] — Local pixel scales duplicate Fluent inheritance (§ `DESIGN.md:39-52,180-209`)**

The local radius and spacing scales compete with the stated FrontComposer/Fluent defaults and can lead to raw CSS or theme recreation.
Fix: remove inherited scales or retain only justified product deltas expressed through Fluent component parameters or Fluent 2 tokens.

**[Rubric / Inheritance discipline] — Risk taxonomy is not reconciled in the UX (§ `prd.md:1215-1224`; `addendum.md:25-33`; `EXPERIENCE.md:77,334-343`)**

The Risk chip and approval flow do not distinguish classifier output from user-visible dispositions such as denied and unsupported.
Fix: define the displayed vocabulary and map each source outcome to review, refusal, and execution behavior.

**[Accessibility] — Current accessibility rules are split across unreferenced peer artifacts (§ `DESIGN.md:4-11`; `EXPERIENCE.md:3-10`; `index.md:8-18`)**

The spines predate the first-class chat, current ten-surface inventory, and later binding UX addenda, so required accessibility behavior depends on discovery order.
Fix: run an Update pass that distills all binding accessibility deltas into the peer spines and records supplemental inputs as provenance.

**[Accessibility] — AI summaries are not programmatically distinguished from source evidence (§ `prd.md:1198-1201,1469-1472`; `DESIGN.md:217-223`; `EXPERIENCE.md:73-81`)**

Screen-reader users can encounter generated interpretation without first hearing its label, provenance, or source relationship.
Fix: add paired Source evidence and AI summary components with semantic headings, provenance before content, source evidence open by default, a keyboard disclosure, and non-color distinction.

**[Accessibility] — Evidence-expiry behavior is not accessible (§ `prd.md:1441`; `EXPERIENCE.md:76,101,115-139`)**

The contract lacks accessible timestamp/state associations, transition announcements, focus preservation, and an explained disabled approval control.
Fix: add a freshness pattern with text, timestamp, accessible name/description, forced-colors treatment, one-time expiry announcement, and `aria-disabled` approval reason.

**[Accessibility] — Correction progress and delay lack accessible states (§ `prd.md:450-451,1324-1325`; `EXPERIENCE.md:98,103,107,114-120`)**

Assistive-technology behavior is undefined while derived stores invalidate and AI use remains unsafe.
Fix: define programmatic state labels, meaningful progress semantics, deduplicated announcements, estimated completion, owner/next action, and focus-preserving updates.

**[Accessibility] — Incremental AI output lacks a bounded announcement policy (§ `EXPERIENCE.md:127-139,181,194-200`; `epic10-chat-surface-elaboration.md:37-47`)**

Nothing prevents streaming chunks from being placed in a live region and repeatedly interrupting screen-reader reading.
Fix: keep streamed content outside live regions and announce only deduplicated start, complete, stopped, and failure transitions from a separate status region.

**[Governance & Safety] — Evidence freshness is visual but not fail-closed (§ `DESIGN.md:147,218-224`; `EXPERIENCE.md:75-81,89-121`)**

Warning color may imply staleness, but no evidence timestamp, freshness state, expiry block, refresh path, or audit outcome is required.
Fix: make freshness explicit on every evidence-bearing surface and block approval/association on expired evidence with reason `evidence-expired`.

**[Governance & Safety] — Correction may appear complete before derived context is safe (§ `EXPERIENCE.md:98,114,293-301`; `prd.md:435-451,1322-1325`)**

The contract can expose contaminated derived context because it lacks acknowledgement from every store and a visible delayed-correction path.
Fix: add correcting/delayed states, store-progress or safe summary, owner/estimate/escalation, and block affected AI actions until all invalidations acknowledge.

**[Governance & Safety] — Bounded administration and two-person control are absent (§ `EXPERIENCE.md:43,120,303-311`; `prd.md:1288-1300`)**

Nora's flow lets one admin activate security-sensitive settings and does not separate aggregate queue visibility from project-detail authority.
Fix: add proposal, distinct second-admin approval, conflict, rejection, expiry/cancel, activation, justification, policy-version, scope, and audit-link states.

**[Governance & Safety] — Denial copy can disclose resource existence (§ `EXPERIENCE.md:52-61,106,111-116`)**

“You do not have access to this project” and a visible “unauthorized candidate suppressed” state reveal that a forbidden resource exists.
Fix: use existence-neutral copy and collapse unauthorized candidates into an indistinguishable no-safe-result state; retain precise causes only in authorized audit evidence.

**[Governance & Safety] — Approval review lacks authority and effect detail (§ `DESIGN.md:222-224`; `EXPERIENCE.md:79-81,334-343`; `addendum.md:124-149`)**

The reviewer cannot see sender-authority/delegation, classifier input/version, policy snapshot, expected post-state, side effects, and audit events needed for informed approval.
Fix: add all mandatory proposal fields and revalidate authority, policy, and effect set immediately before execution; changes require a refreshed proposal.

### Medium (11)

**[Rubric / Token completeness] — Typography uses an unsupported semantic exception (§ `DESIGN.md:28-38`; `design-md-spec.md:15-18,45-49`)**

Every web typography role uses only `note`, although that semantic exception is documented for native platform conventions.
Fix: omit unchanged Fluent roles and document inheritance in prose, or encode genuine brand deltas using supported typography fields.

**[Rubric / Component coverage] — Inherited primitives with behavioral deltas are unnamed (§ `EXPERIENCE.md:123-172`)**

Busy replacement, error-summary focus, dialog/sheet containment, and queue filtering add product behavior but lack exact inherited-component mappings.
Fix: add rows only for primitives with behavioral deltas and name the exact FrontComposer/Fluent component inherited.

**[Rubric / Inheritance discipline] — Historical PRD validation is presented as binding (§ frontmatter `sources`; `prd-validation-report.md:1-21`)**

The fourth source validates an older PRD revision and can mislead consumers with a stale Critical verdict and obsolete requirement counts.
Fix: remove it from binding sources or mark it explicitly as historical/context-only with its target revision.

**[Rubric / Inheritance discipline] — FrontComposer implementation reference is broken (§ `DESIGN.md:142`)**

The cited path omits the root `references/` segment.
Fix: link to `references/Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md` and declare it as inherited UI-system evidence if consumers must load it.

**[Accessibility] — Classification and “why” disclosure lack accessible component behavior (§ `prd.md:1194-1201`; `EXPERIENCE.md:73-78`)**

Badge/message association, disclosure reading order, labelled facts, expanded state, focus return, and update-announcement rules are unspecified.
Fix: define accessible classification and association-explanation patterns in both spines.

**[Accessibility] — Mixed-language content lacks language metadata (§ `EXPERIENCE.md:224-230`)**

English/French translation rules do not set the page language or identify email, AI, and quoted content in a different language.
Fix: bind root `lang` to the UI locale, apply language-of-parts metadata when known, persist locale across navigation, and test EN/FR screen-reader output.

**[Accessibility] — Focus may be obscured by sticky chrome or panels (§ `DESIGN.md:164,180-190`; `EXPERIENCE.md:167-172,232-244`)**

Focus order and contrast are covered, but WCAG 2.2 Focus Not Obscured is not.
Fix: require focused controls to remain at least partially visible, define scroll margins, and test programmatic focus at 200% and 400% zoom.

**[Accessibility] — Association candidate radiogroup behavior is undefined (§ `implementation-conformance-addendum-2026-07-17.md:34-44`; `EXPERIENCE.md:75,95-96,115,199`)**

Selection, arrow-key movement, announced position/count, validation, focus recovery, and refresh behavior are left to inference.
Fix: define one named radiogroup and decision bar; selection must not commit, and invalid confirmation must focus the existing error summary.

**[Governance & Safety] — AI-summary provenance is missing (§ `DESIGN.md:214-217`; `EXPERIENCE.md:70-73,174-180`)**

Generated interpretation can be mistaken for authoritative mail, file, or command evidence.
Fix: add an AI-summary component with model/version/time/source IDs, non-color distinction, source-first default, audit linkage, and redaction-safe exported representations.

**[Governance & Safety] — Replay and conflict outcomes are underspecified (§ `EXPERIENCE.md:58-59,103-105,207-213`; `addendum.md:87-102`)**

Retry counts and duplicate-safety notes exist, but operation identity, prior-outcome reuse, already-decided/sent/corrected conflicts, attempt ceilings, and replay windows do not.
Fix: define a shared operation-status pattern with idempotency identity, original-outcome link, eligibility, ceiling, and stable status/reason codes.

**[Governance & Safety] — UI/CLI/MCP parity lacks a complete outcome map (§ `EXPERIENCE.md:44,121,240,313-321,345-355`; `prd.md:1307-1315`)**

The UX asserts parity, but only a CLI subset is exercised and MCP lacks explicit failure/recovery behavior.
Fix: link to the canonical parity set and add a UI/API–CLI–MCP outcome matrix covering normalized input, authorization, transition, redaction/reason, operation identity, audit origin, and long-running status.

### Low (2)

**[Rubric / Bloat & overspecification] — Touch-target rules are duplicated (§ `EXPERIENCE.md:201,244`)**

The same target-size contract appears in Accessibility Floor and Responsive & Platform.
Fix: keep the normative rule in Accessibility Floor and reference it from Responsive & Platform.

**[Rubric / Shape fit] — Product-Specific Concerns does not earn a separate section (§ `EXPERIENCE.md:224-230,250-254`)**

Its only row restates the fuller localization contract immediately above it.
Fix: remove it or expand it only for a distinct concern that cannot fit an existing section.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-governance-safety.md`
