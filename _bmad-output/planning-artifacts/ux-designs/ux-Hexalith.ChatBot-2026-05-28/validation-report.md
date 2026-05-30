# Validation Report — Hexalith.ChatBot

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- **Run at:** 2026-05-28T18:46:01+02:00

## Overall verdict

The Update pass closed every prior medium and low finding cleanly: the authoritative inherited-token source is named, every component prose row carries its `{components.*}` reference, WCAG 2.2 AA touch-target minima are stated concretely (44×44 / 24×24 CSS px with destructive/approval carve-out), French expansion and column-collapse rules name what may collapse and what must not, the scope reconciliation is now a table, and the spine-only visual-reference decision is logged. All 17 components pair across both files; all 8 PRD user journeys plus the 1 system journey map to Key Flows with named protagonist, climax beat, and failure path. State coverage is the strongest part of the spine pair — 17 named states, per-surface coverage, and a state-to-feedback matrix tied to live-region politeness.

Two material things remain. A mechanical regression broke `sources:` resolution: both spines point at `../../prd.md` and `../../prd-validation-report.md` which no longer exist at that path (PRD moved into `prds/`; old validation report deleted). And the accessibility lens, while confirming the prior three findings all closed, surfaces five new mediums clustered around AI-specific accessibility: keyboard reach for streaming-stop, live-region politeness mapping for AI proposal/projection/rejection events, redaction enforcement on export/copy paths, focusability of disabled approval controls, and WCAG 2.1.4 single-key shortcut governance. None are critical; the contract still ships, but the sources fix should land before any consumer extracts from the spine.

## Category verdicts

- Flow coverage — **strong**
- Token completeness — **strong**
- Component coverage — **strong**
- State coverage — **strong**
- Visual reference coverage — **adequate**
- Bloat & overspecification — **adequate**
- Inheritance discipline — **adequate**
- Shape fit — **strong**
- Accessibility lens — **adequate**

## Findings by severity

### Critical (0)

No critical findings.

### High (0)

No high findings.

### Medium (7)

**[Component coverage]** — actor-badge actor-type list does not pair across spines (DESIGN.md:216 / EXPERIENCE.md:72)
DESIGN.md enumerates eight actor categories (human user, external party, service client, AI actor, background worker, CLI, MCP, mailbox event). EXPERIENCE.md drops five of them. Downstream dev will not know whether CLI / MCP / background-worker / mailbox-event / AI-actor share one visual or differentiate.
*Fix:* in EXPERIENCE.md Component Patterns row for Actor badge, enumerate the same eight actor types DESIGN.md does, and state whether they share one visual or differentiate.

**[Inheritance discipline]** — `sources:` frontmatter paths broken in both spines (DESIGN.md:7–10 / EXPERIENCE.md:6–9)
Both spines list `- ../../prd.md` and `- ../../prd-validation-report.md`. From the spine location, `../../` resolves to `_bmad-output/planning-artifacts/` — only the product brief actually lives there. The current PRD lives at `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`; the root-level `prd-validation-report.md` was deleted (visible in `git status`). Source-extracting consumers will hit dead links.
*Fix:* update `sources:` to `- ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, keep the product-brief path, drop the deleted `prd-validation-report.md` entry or repoint to `prds/prd-Hexalith.ChatBot-2026-05-28/review-rubric-v2.md` if validation traceability is wanted.

**[Accessibility]** — No keyboard rule for interrupting a streaming AI response (EXPERIENCE.md:71, :113, :178)
Reduced-motion acknowledges streaming text but does not specify that Stop/Cancel must be keyboard-reachable while streaming, occupy a stable focusable position (no inline appear/disappear that steals focus), and announce cancellation via the state-to-feedback matrix.
*Fix:* add a bullet under "Reduced motion and auto-scroll" or "Conversation and audit semantics" requiring an always-keyboard-reachable stop/cancel affordance during streaming, with a polite "Response stopped" announcement and stable focus return to the composer or AI proposal panel.

**[Accessibility]** — Live-region politeness not pinned to AI-specific events (EXPERIENCE.md:100–102, :124–134)
State-family politeness is defined, but the async AI events ("AI proposal ready," "Command accepted, projection pending," "Approval rejected") leave the screen-reader experience underspecified — likely to over-announce on each projection-pending tick, or under-announce a rejection on the user's own action.
*Fix:* add a small mapping table: AI proposal ready → polite (one announcement on user's own request); projection pending → polite (one announcement, not repeated on each poll); approval rejected on the current user's submitted action → assertive; rejection observed in a queue for someone else → polite or none.

**[Accessibility]** — No accessibility rule for PII redaction on export / copy (EXPERIENCE.md:60, :105, :118, :194, :251)
Redaction is enforced on screen, but exports, audit copy actions, and "copy email body" affordances are unconstrained. A screen reader announcing the unredacted text from a copy, or a transcript download containing identities the on-screen view suppressed, would break tenant isolation specifically for AT users.
*Fix:* add an Accessibility Floor bullet stating that any export, copy, or "read aloud" affordance applies the same redaction as the visual surface, and the export's accessible name/description must not contain the redacted source text. Pair with a screen-reader-equivalent affordance for "this export is redacted; full detail requires escalation."

**[Accessibility]** — Disabled approval controls undermine the "tooltip-only is insufficient" rule (EXPERIENCE.md:79, :164)
EXPERIENCE.md:164 rejects tooltip-only explanations for disabled state, but :79 still lets the disabled approval button itself be the explanation surface. Fluent UI v5 disabled buttons are non-focusable by default — a screen-reader user tabbing through Approve/Reject/Revise/Cancel will silently skip the reason.
*Fix:* require disabled approval/association/correction controls to either remain focusable with `aria-disabled="true"` plus an announced reason, OR be paired with an adjacent focusable "Why unavailable?" affordance.

**[Accessibility]** — No rule on single-key shortcuts (WCAG 2.1.4) (EXPERIENCE.md:145)
Command-palette and keyboard shortcuts are permitted for developers/operators, but the spine does not require single-character or modifier-free shortcuts to be remappable, disable-able, or only active on focus. Inside a chat composer, this is a known accessibility footgun for speech-input users.
*Fix:* add a bullet requiring single-character/modifier-free shortcuts to be disabled by default inside text-entry controls and remappable or globally disable-able in user settings; document where the toggle lives.

### Low (10)

**[Flow coverage]** — Flow 9 invents a name ("Ari") for the system actor (EXPERIENCE.md:350)
The PRD names the actor only as "project-aware AI agent." Inventing "Ari" is not wrong, but it is unsourced framing.
*Fix:* drop the name and call it "Project-aware AI agent," or log "Ari" in `.decision-log.md` as a UX-side personification for the system actor.

**[Token completeness]** — Contrast table silent on purely decorative muted text (DESIGN.md:157)
The metadata pair cites 4.5:1 "when text communicates status or recovery"; decorative muted helper text has no rule.
*Fix:* add a one-line clause that purely decorative muted text follows Fluent's non-essential-text guidance, or remove the conditional.

**[Bloat & overspecification]** — §Inspiration & Anti-patterns restates posture already established (EXPERIENCE.md:237–245)
Five bullets, four covered by §Foundation and the Voice and Tone do/don't table; three of the "Rejected:" bullets repeat rules already enforced in §Component Patterns and §Interaction Primitives.
*Fix:* delete the section or compress to one line that names the inspirations and points to the operative spec sections.

**[Bloat & overspecification]** — §Product-Specific Concerns table mostly restates encoded concerns (EXPERIENCE.md:247–259)
Tenant isolation, multi-actor conversation, external participants, and auditability are already enforced by named components, states, and flows.
*Fix:* keep only rows that introduce a UX requirement not visible elsewhere (Internationalization qualifies — it pins the en/fr scope); fold the rest.

**[Inheritance discipline]** — EXPERIENCE.md §Foundation does not state the inheritance chain (EXPERIENCE.md:16)
It calls DESIGN.md "the visual identity reference" but does not say the FrontComposer/Fluent UI v5 platform is itself the inherited source. The chain is Fluent UI v5 → FrontComposer → DESIGN.md → EXPERIENCE.md.
*Fix:* add a one-line inheritance chain in §Foundation matching DESIGN.md:141.

**[Accessibility]** — Repeated landmark roles need unique accessible names (EXPERIENCE.md:159)
Landmarks are listed but no rule says repeated roles within a surface need unique `aria-label`. Conversation Detail + Evidence drawer + AI proposal panel can all read as "complementary."
*Fix:* add a sentence requiring unique `aria-label` on repeated landmark roles within a single surface.

**[Accessibility]** — aria-busy cleanup and focus preservation undefined (EXPERIENCE.md:126)
Skeleton/`aria-busy` rule does not say when `aria-busy` is cleared, or whether focus inside a busy region is preserved across the skeleton-to-real-content swap.
*Fix:* require `aria-busy` to be cleared on the same node, focus preserved or moved to a labelled landing point, and newly-loaded historical content to not announce.

**[Accessibility]** — Dark mode and forced-colors contrast not pinned (DESIGN.md:154–163, :165)
Contrast table covers the required pairs but does not say which apply in dark mode or Windows High Contrast / forced-colors. The "inherited from Fluent UI/FrontComposer" escape clause is correct but leaves QA without a test target; status chips that rely on background fill lose meaning under forced-colors.
*Fix:* add a row or sentence pinning the same minima (4.5:1 text, 3:1 non-text) explicitly to light and dark, and stating that status-chip meaning must survive forced-colors via icon, text label, or border — not background fill alone.

**[Accessibility]** — Form validation needs explicit error-association rule (EXPERIENCE.md:131, :204)
Focus-move to summary is required, but `aria-describedby` from each invalid input to its message and `aria-invalid` on the field are not.
*Fix:* add the standard `aria-describedby` / `aria-invalid` association requirement to Accessibility Floor or Tenant Configuration recovery.

**[Accessibility]** — Actor-type label not required to precede message text in accessible name (EXPERIENCE.md:169)
Actor type and identity are exposed per message but the order is not pinned. Screen-reader users may hear the AI proposal text before learning it is from AI.
*Fix:* require the actor-type label to precede message content in the accessible name/description.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
