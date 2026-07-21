---
title: 'Unblock Story 12.14 by correcting the association-review forced-colors regression test'
type: 'bugfix'
created: '2026-07-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: '6c262bd10e2559922d7bb3687a5f7e86f5b574c7'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 12.14 is implemented and its scheduler tests pass, but the full completion gate is blocked by one stale UI E2E assertion. The static association-review fixture expects a ChatBot-owned border on an unhydrated `<fluent-button>`, even though commit `9462de3` deliberately delegated button presentation and forced-colors behavior to Fluent UI.

**Approach:** Correct the browser and browser-unavailable test contracts to verify durable, non-color semantics—accessible radio state, visible evidence labels, Fluent component ownership, reduced motion, and redaction safety—without restoring retired presentation CSS.

## Boundaries & Constraints

**Always:** Keep the existing reduced-motion assertions and blocked/redacted-data checks. Preserve the production `FluentButton` `Primary`/`Outline` appearance contract and validate that Fluent, rather than `chatbot.tokens.css`, owns candidate presentation. Limit the regression fix to test code unless verification reveals a real rendered-app accessibility defect.

**Ask First:** Stop for approval if a hydrated live association-review surface lacks a perceivable candidate state, the fix requires product component or stylesheet changes, or the repair expands beyond the forced-colors test contract and Story 12.14 completion evidence.

**Never:** Reintroduce a `.chatbot-association-candidate` border or forced-colors presentation rule, add fixture-only CSS that fakes Fluent rendering, weaken reduced-motion or redaction assertions, or alter Story 12.14 scheduler/runtime behavior.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Browser available | Chromium emulates forced colors and reduced motion; candidate fixture is rendered | Candidate remains discoverable as an accessible radio with explicit checked state; evidence state remains available as text; animation and transform remain disabled | Fail with the specific missing semantic or motion contract |
| Browser unavailable | Source-contract fallback runs | Test verifies forced-colors/reduced-motion hooks, accessible fixture attributes, and `FluentButton`/`FluentBadge` presentation ownership without requiring retired `CanvasText` candidate CSS | Fail when semantic source contracts are absent |
| Blocked/redacted | Blocked association fixture is rendered | Alert and restricted-evidence labels remain visible; secret or raw metadata remains absent | Fail on missing safe state or leaked restricted text |

</frozen-after-approval>

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
