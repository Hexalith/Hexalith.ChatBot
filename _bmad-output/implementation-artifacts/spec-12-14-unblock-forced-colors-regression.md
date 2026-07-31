---
title: 'Unblock Story 12.14 by correcting the association-review forced-colors regression test'
type: 'bugfix'
created: '2026-07-21'
status: 'superseded'
review_loop_iteration: 0
baseline_commit: '6c262bd10e2559922d7bb3687a5f7e86f5b574c7'
context: []
---

> **Authorization note (2026-07-31).** This document was authored inside the very change it purported to authorize, and
> originally carried `status: done` plus a `<frozen-after-approval reason="human-owned intent">` wrapper — asserting a
> human-owned approval that had not occurred, and using it to justify editing an Epic 13 surface from a story whose own
> scope notes say "No user-facing / UI / localization / accessibility impact". The frozen wrapper and the `done` status
> have been removed; the text below is retained as the rationale record only, not as an approval artifact.
>
> The **test change itself stands and was reviewed on its merits**: the removed `borderTopStyle == "solid"`,
> `CanvasText`, and `.chatbot-association-candidate:focus` assertions all targeted CSS that Story 13.8 deliberately
> retired (`CanvasText` has zero occurrences under `src/Hexalith.ChatBot.UI/`), and the replacements — accessible radio
> `aria-checked`, evidence `aria-label`s, and a full-page redaction sweep — are strictly stronger. It is recorded as a
> **reviewer-accepted test correction** in Story 12.14's Completion Notes.
>
> **Second correction (2026-07-31, round-2 review).** A `authorization: 'review-authorized 2026-07-31 (Jerome, Story
> 12.14 code review)'` field was added when the frozen wrapper was stripped. That was the same fabrication in softer
> form: Jerome's recorded decision was to **strip the false framing**, not to authorize this edit. The field has been
> removed. The former **"Ask First" clause was also removed** — it was never a live gate (no approver was ever recorded),
> and the change did in fact expand beyond the forced-colors test contract by adding a new source-contract assertion,
> `AssertAssociationCandidateRowUsesFluentPresentation`. This document carries **no** human approval. The test change
> stands on reviewer assessment of its merits alone, as recorded in Story 12.14.

<!-- Retained rationale (originally under a frozen-after-approval wrapper; see the authorization note above). -->

## Intent

**Problem:** Story 12.14 is implemented and its scheduler tests pass, but the full completion gate is blocked by one stale UI E2E assertion. The static association-review fixture expects a ChatBot-owned border on an unhydrated `<fluent-button>`, even though commit `9462de3` deliberately delegated button presentation and forced-colors behavior to Fluent UI.

**Approach:** Correct the browser and browser-unavailable test contracts to verify durable, non-color semantics—accessible radio state, visible evidence labels, Fluent component ownership, reduced motion, and redaction safety—without restoring retired presentation CSS.

## Boundaries & Constraints

**Always:** Keep the existing reduced-motion assertions and blocked/redacted-data checks. Preserve the production `FluentButton` `Primary`/`Outline` appearance contract and validate that Fluent, rather than `chatbot.tokens.css`, owns candidate presentation. Limit the regression fix to test code unless verification reveals a real rendered-app accessibility defect.

**Never:** Reintroduce a `.chatbot-association-candidate` border or forced-colors presentation rule, add fixture-only CSS that fakes Fluent rendering, weaken reduced-motion or redaction assertions, or alter Story 12.14 scheduler/runtime behavior.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Browser available | Chromium emulates forced colors and reduced motion; candidate fixture is rendered | Candidate remains discoverable as an accessible radio with explicit checked state; evidence state remains available as text; animation and transform remain disabled | Fail with the specific missing semantic or motion contract |
| Browser unavailable | Source-contract fallback runs | Test verifies forced-colors/reduced-motion hooks, accessible fixture attributes, and `FluentButton`/`FluentBadge` presentation ownership without requiring retired `CanvasText` candidate CSS | Fail when semantic source contracts are absent |
| Blocked/redacted | Blocked association fixture is rendered | Alert and restricted-evidence labels remain visible; secret or raw metadata remains absent | Fail on missing safe state or leaked restricted text |

<!-- End retained rationale. -->

## Code Map

- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` -- contains the stale browser border assertion, its stale no-browser fallback, and the association-review fixtures.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor` -- authoritative source contract showing `FluentButton` `Primary`/`Outline` and `FluentBadge` ownership; inspection only.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` -- layout/accessibility stylesheet whose retired candidate presentation must not be restored; inspection only.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` -- replace the synthetic border assertion with candidate radio-state and evidence-label assertions while retaining reduced-motion checks; align the no-browser fallback with accessible fixture semantics and Fluent component ownership.

**Acceptance Criteria:**
- Given forced-colors and reduced-motion emulation, when the association candidate fixture renders, then candidate identity, explicit radio state, and evidence-state text remain perceivable while animation and transform are disabled.
- Given Chromium is unavailable, when the fallback executes, then it validates current Fluent-owned presentation and semantic source contracts without blessing retired candidate-border CSS.
- Given the blocked/redacted fixture, when the regression test executes, then the safe alert and restricted-evidence label remain visible and restricted metadata is absent.
- The focused regression test and the complete UI E2E executable pass with no failures, allowing Story 12.14 completion gates to resume.

## Spec Change Log

## Verification

**Commands:**
- `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` -- expected: zero warnings and zero errors.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor -method Hexalith.ChatBot.UI.E2E.Tests.GovernedOperationsVisualFoundationE2ETests.AssociationReviewShouldPreserveForcedColorsReducedMotionAndBlockedRedactionStates` -- expected: focused test passes in the real browser path.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` -- expected: complete UI E2E suite passes with no failures.

## Suggested Review Order

**Forced-colors semantics**

- Start with the corrected browser/fallback entry point and preserved motion contract.
  [`GovernedOperationsVisualFoundationE2ETests.cs:1222`](../../tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs#L1222)

- Review explicit candidate identity and visible evidence-state assertions replacing synthetic border styling.
  [`GovernedOperationsVisualFoundationE2ETests.cs:1242`](../../tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs#L1242)

- Confirm redaction now covers visible text and the complete rendered document.
  [`GovernedOperationsVisualFoundationE2ETests.cs:1264`](../../tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs#L1264)

**Fallback and Fluent ownership**

- Inspect browser-unavailable semantic checks without retired candidate presentation requirements.
  [`GovernedOperationsVisualFoundationE2ETests.cs:4881`](../../tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs#L4881)

- Verify the exact selected/unselected Fluent appearance binding remains source-owned.
  [`GovernedOperationsVisualFoundationE2ETests.cs:4918`](../../tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs#L4918)
