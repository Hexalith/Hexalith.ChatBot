# Epic 12 Documentation Update Audit

Date: 2026-06-21
Project: Chatbot
Scope: Post-retrospective verification of documentation drift after Epic 12 implementation.

## Proposed Documentation To Verify

1. `README.md`
2. `_bmad-output/planning-artifacts/epics.md`
3. `_bmad-output/planning-artifacts/architecture.md`
4. `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
5. `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
6. `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`

## Verification Method

- Read current documentation text for Epic 12 / Fluent v5 / raw controls / custom CSS.
- Compared against implementation evidence:
  - `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
  - `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
  - `src/Hexalith.ChatBot.UI/Components/App.razor`
  - Epic 12 story files and test summaries.
- Ran source scans:
  - `rg -n "<(button|input|select|textarea)(\\s|/|>)" src/Hexalith.ChatBot.UI/Components`
  - `rg -n -- "--chatbot-type-|--chatbot-font-|--chatbot-radius-|\\.chatbot-button|\\b(button|input|select|textarea)([:.#\\s,{>+~\\[]|$)" src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`

Both scans returned no matches.

## Updates Made

### `README.md`

Verified discrepancy:

- The README still described Epic 12 as owning future remediation and said the interior UI still used raw controls and a custom `chatbot.tokens.css` layer.
- Actual implementation has completed the remediation: raw-control backlog empty, primitive CSS backlog empty, `chatbot.tokens.css` reduced to Fluent-backed semantic aliases plus layout/accessibility hooks, and Story 12.9 release evidence green.

Update applied:

- Reworded Epic 12 summary as completed remediation.
- Updated the Epic 10 notes to say Epic 12 closed the component-conformance gap.

### `_bmad-output/planning-artifacts/epics.md`

Verified discrepancies:

- Story 12.6 shorthand referenced `FluentTextField` / `FluentNumberField`, but the pinned Fluent UI v5 RC exposes and the code uses `FluentTextInput` / `FluentNumberInput<int>`.
- Story 12.7 shorthand referenced `FluentSearch` and a `FluentDataGrid` rewrite, but implementation deliberately used `FluentTextInput`, `FluentNumberInput<int>`, `FluentLabel`, and `FluentButton`; dashboards/timeline kept semantic row/list structures because tests and accessibility contracts depended on them.

Update applied:

- Corrected Story 12.6 API names.
- Corrected Story 12.7 implementation outcome and recorded the rationale for avoiding `FluentSearch` / broad `FluentDataGrid` rewrite.

## Verified No Update Needed

### `_bmad-output/planning-artifacts/architecture.md`

Current text already matches implementation:

- Documents ChatBot UI Fluent-only conformance.
- Documents the no raw `<button>/<input>/<select>/<textarea>` rule.
- Documents no primitive CSS redefinition.
- Documents the guard and empty-carveout target.
- Documents the Epic 10 gap as historical and the Story 12.8 CSS retirement.

### PRD

No update needed:

- Epic 12 did not change product requirements, API behavior, command semantics, CLI/MCP parity semantics, or backend workflows.
- The implementation preserved UX-DR1/UX-DR2 and accessibility requirements rather than changing them.

### UX `DESIGN.md` / `EXPERIENCE.md`

No update needed:

- The visual chain and behavior requirements remain accurate: Fluent UI v5 -> FrontComposer -> DESIGN.md -> EXPERIENCE.md.
- Epic 12 implementation closed the conformance gap without changing the UX requirements.

## Discarded Candidates

- OpenAPI/API docs: no API contract changed.
- Configuration docs: no new runtime configuration or package pin changed.
- Historical readiness reports and sprint-change proposals: dated evidence artifacts should remain historical.
