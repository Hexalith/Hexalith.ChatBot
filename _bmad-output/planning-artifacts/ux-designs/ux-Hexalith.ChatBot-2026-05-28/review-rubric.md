# Spine Pair Review — Hexalith.ChatBot

## Overall verdict

The Update pass closed the five medium and three low findings as claimed: an authoritative inherited-token source is named, every component prose row carries its `{components.*}` reference, WCAG 2.2 AA target sizes (44×44, 24×24 CSS px) are stated concretely, French expansion/truncation rules name the columns that may collapse and the ones that must not, the scope reconciliation is now a table, and the spine-only visual-reference decision is explicit. One previously-passing area regressed mechanically: `sources:` frontmatter in both spines points at `../../prd.md` and `../../prd-validation-report.md` which do not exist at that path — the real PRD lives under `prds/prd-Hexalith.ChatBot-2026-05-28/`, so source resolution fails for two of three references. Otherwise the spine pair is a clean, extractable contract: all 17 components are paired across both files, all 9 PRD journeys (8 user + 1 system) map to Key Flows with protagonist, climax, and failure path, and every state family the PRD implies is enumerated per-surface.

## 1. Flow coverage — strong

Cross-walked the 8 PRD user journeys + 1 System journey against EXPERIENCE.md §Key Flows. Every journey has a Key Flow with verbatim protagonist (Amira UJ1/UJ8, Marc UJ2, Elena UJ3, Priya UJ4, Nora UJ5, Leo UJ6, Sofia UJ7) and the system journey is captured as Flow 9. Each flow has numbered steps, a labeled **Climax** beat, and a `Failure:` path. Flow 1 even adds a back-reference (`Source journey mapping: Journey 8 covers the review step in more detail as Flow 8.`) which is good cross-spine hygiene.

### Findings

- **low** Flow 9 introduces a protagonist name "Ari" for the AI agent that does not appear in any source PRD or brief; the PRD names the actor only as "project-aware AI agent." Not wrong, but it is invented framing. (EXPERIENCE.md:350). *Fix:* either drop the name and call it "Project-aware AI agent" or note in `.decision-log.md` that "Ari" is a UX-side personification for the system actor.

## 2. Token completeness — strong

YAML frontmatter declares `colors` (15 keys, all `var(--colorFluent…)` indirection), `typography` (5 roles using the supported `note:` field), `rounded` (sm/md/lg), `spacing` (8 keys), and `components` (17 entries). Every `{path.to.token}` reference resolves. The frontmatter convention is the platform-inheritance pattern the design-md spec allows (line 48 of `design-md-spec.md`: "When inheriting from native platforms… use a `note` field instead of literal values"). Inherited authoritative source is now named in §Colors line 141: `Hexalith.FrontComposer` Fluent UI v5 integration + `Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md`. Contrast targets are explicit for all eight load-bearing pairs (lines 154–163).

### Findings

- **low** The contrast table cites `4.5:1` for the metadata pair "when text communicates status or recovery." For purely decorative muted helper text the table is silent. (DESIGN.md:157). *Fix:* add a one-line clause that purely decorative muted text follows Fluent's non-essential-text guidance, or remove the conditional.

## 3. Component coverage — strong

All 17 components in `DESIGN.md.components` frontmatter (project-context-header, conversation-shell, conversation-stream, composer-action-entry, actor-badge, evidence-chip, risk-chip, attachment-row, evidence-drawer, ai-proposal-panel, approval-controls, approval-panel, blocked-state, association-candidate-row, queue-row, audit-timeline, status-toast-banner) appear with prose rows in `DESIGN.md §Components` and with behavioral rows in `EXPERIENCE.md §Component Patterns`. Names match across both files. The "pair the prose with the frontmatter token" fix is applied: every DESIGN.md prose bullet ends with the corresponding `{components.<name>}` token reference (lines 212–228).

### Findings

- **medium** `actor-badge` is broader in DESIGN.md (line 216: identifies "human user, external party, service client, AI actor, background worker, CLI, MCP, or mailbox event") than in EXPERIENCE.md (line 72: "Identifies actor type and resolved party/user/client"). The behavioral spec drops five of the eight named actor categories. Downstream dev will not know whether CLI/MCP/background-worker/mailbox-event/AI-actor need their own visual treatment or share one. (EXPERIENCE.md:72, DESIGN.md:216). *Fix:* in EXPERIENCE.md Component Patterns row for Actor badge, enumerate the same eight actor types DESIGN.md does, and state whether they share one visual or differentiate.

## 4. State coverage — strong

§State Patterns (lines 86–106) defines 17 named states. §Surface state coverage (lines 108–120) cross-walks every IA surface to its required states (cold load, empty, focus/active, error, retryable failure, terminal failure, unauthorized/redacted, dependency degraded are all named where they apply). The state-to-feedback matrix (lines 122–134) maps state families to feedback primitives (skeleton+`aria-busy`, polite toast, persistent banner, error summary with focus move, assertive announcement, non-interrupting "new updates" affordance). This is one of the strongest parts of the spine.

### Findings

- None at medium or above. The coverage is complete across the 9 surfaces and ties cleanly to FR67 / FR76 / FR77 / FR79 acceptance criteria.

## 5. Visual reference coverage — adequate

The workspace has empty `imports/` and `.working/` folders; no `mockups/` or `wireframes/` folder exists. The spine-only decision is logged explicitly in two places: EXPERIENCE.md:29 ("this update intentionally keeps the UX contract spine-only… spines win on conflict") and `.decision-log.md:66` ("the spine-only visual-reference decision: no mockups, wireframes, or imports are required for MVP handoff"). Not penalizing further — the decision is owned.

### Findings

- None.

## 6. Bloat & overspecification — adequate

DESIGN.md carries editorial voice without restating PRD prose ("the interface should feel closer to an enterprise command workspace than to a social chat feed" — that earns its place). EXPERIENCE.md prose stays operational. The §Inspiration & Anti-patterns section (lines 237–245) is borderline — five bullets, four of them already covered by the §Foundation paragraph and the §Voice and Tone do/don't table. §Product-Specific Concerns (lines 247–259) also overlaps thematically with §Foundation and §State Patterns.

### Findings

- **low** §Inspiration & Anti-patterns (EXPERIENCE.md:237–245) restates posture already established in §Foundation and §Brand & Style. Three of the four "Rejected:" bullets repeat behavioral rules that are already enforced in §Component Patterns (AI proposal panel, blocked state) and §Interaction Primitives (banned interactions). *Fix:* either delete this section or compress to one line that names the inspirations and points to the operative spec sections.

- **low** §Product-Specific Concerns table (EXPERIENCE.md:247–259) restates concerns the IA, Component Patterns, and State Patterns already encode. "Tenant isolation," "multi-actor conversation," "external participants," and "auditability" are already enforced by named components/states/flows. *Fix:* keep only rows that introduce a UX requirement not visible elsewhere ("Internationalization" qualifies because it pins the English+French scope); fold the rest.

## 7. Inheritance discipline — adequate

UJ names are verbatim from the PRD (Journey 1 → Flow 1, etc., though EXPERIENCE.md uses "Flow N" framing not "Journey N" — that is the convention this skill prefers). Component names are identical across the DESIGN.md frontmatter, the DESIGN.md §Components prose, and the EXPERIENCE.md §Component Patterns table. Glossary terms (Actor, Party, Evidence chip, Risk chip, Blocked state, Audit timeline, Command surface) carry the PRD glossary's intent. Inherited UI system is named twice (DESIGN.md:135, EXPERIENCE.md:16).

### Findings

- **medium** Both spines' `sources:` frontmatter is broken. DESIGN.md:7–10 and EXPERIENCE.md:6–9 list `- ../../prd.md`, `- ../../product-brief-Hexalith.ChatBot.md`, `- ../../prd-validation-report.md`. From the spine location, `../../` resolves to `_bmad-output/planning-artifacts/` — only the product brief actually lives there. The current PRD lives at `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`; the root-level `prd-validation-report.md` formerly at that location has been deleted (visible in `git status` as ` D _bmad-output/planning-artifacts/prd-validation-report.md`). Source-extracting consumers that try to follow these references will fail. *Fix:* update `sources:` to `- ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, keep the product-brief path, drop the deleted `prd-validation-report.md` entry or repoint it to the current `prds/prd-Hexalith.ChatBot-2026-05-28/review-rubric-v2.md` if validation traceability is wanted.

- **low** `EXPERIENCE.md §Foundation` (line 16) calls DESIGN.md "the visual identity reference" but does not say that the visual identity of the FrontComposer/Fluent UI v5 platform is itself the inherited source; a reader landing on EXPERIENCE.md first could think DESIGN.md is the authoritative palette. The chain is: Fluent UI v5 → FrontComposer → DESIGN.md (semantic narrowing) → EXPERIENCE.md (behavioral spec). *Fix:* add a one-line inheritance chain in §Foundation matching what DESIGN.md:141 already states.

## 8. Shape fit — strong

DESIGN.md canonical order is followed exactly: Brand & Style (131) → Colors (139) → Typography (167) → Layout & Spacing (179) → Elevation & Depth (191) → Shapes (202) → Components (210) → Do's and Don'ts (230). EXPERIENCE.md required defaults are all present: Foundation (14), Information Architecture (31), Voice and Tone (47), Component Patterns (62), State Patterns (86), Interaction Primitives (136), Accessibility Floor (182), Key Flows (261). Required-when-applicable §Responsive & Platform (223) is present because the PRD names mobile/triage scope. Invented sections: §Inspiration & Anti-patterns and §Product-Specific Concerns (see Pass 2 §6 above) — they exist but partially restate.

### Findings

- None at medium or above; the shape is correct.

## Mechanical notes

- **Source frontmatter cross-refs broken** — two of three `sources:` paths in both DESIGN.md and EXPERIENCE.md do not resolve. See Finding §7.1 for the fix. This is the main mechanical hit.
- **No Mermaid blocks present** in either spine — nothing to lint.
- **`prd-validation-report.md` referenced in both spines + `.decision-log.md` line 22 has been deleted** (visible in repo `git status` at session start as ` D _bmad-output/planning-artifacts/prd-validation-report.md`). Any consumer following the citation chain will dead-end.
- **Glossary parity not formally restated** — DESIGN.md and EXPERIENCE.md both use PRD-glossary terms ("evidence chip," "risk chip," "blocked state," "actor badge," "approval panel," "audit timeline") consistently, but neither spine carries a duplicated Glossary section. The skill default is to inherit from the PRD glossary, which is acceptable; not a finding, just a confirmation.
- **Frontmatter completeness** — DESIGN.md frontmatter has `name`, `description`, `status: final`, dates, sources, colors, typography, rounded, spacing, components: complete. EXPERIENCE.md frontmatter has `name`, `status: final`, dates, sources: complete. EXPERIENCE.md does not duplicate token tables, which matches the convention that DESIGN.md owns visual tokens.
- **Component name normalization** — DESIGN.md frontmatter uses kebab-case (`composer-action-entry`, `ai-proposal-panel`); the prose uses sentence-case with slashes (`Composer/action entry`, `AI proposal panel`); EXPERIENCE.md uses the same sentence-case. The pairing relies on a name-equality check that humans can do but a strict machine resolver might miss. Not a finding given the explicit `{components.composer-action-entry}` style references in the DESIGN.md prose disambiguate the mapping, but flagging for the consumer-side resolver to be aware.
- **Touch target citation duplicated, scope justified** — EXPERIENCE.md §Accessibility Floor line 193 and §Responsive & Platform line 235 both state the 44×44 and 24×24 thresholds. The two paragraphs scope to different audiences (a11y reviewer vs. responsive implementer), so the repetition is justified.
