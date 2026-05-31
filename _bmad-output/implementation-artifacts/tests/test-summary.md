# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 1.18. The story establishes UI-owned accessibility and focus-management contracts and governed UI fixture coverage; it does not add API endpoints or backend service behavior.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - covers governed operations runtime token loading, UI-origin command behavior, semantic status summaries, backend failure rendering, forced-colors cues, governed primitive accessibility, disabled critical-action reasons, and streaming Stop/Cancel focus return.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - covers the current governed operations skip-link/main focus path, heading visibility, named primary and complementary regions, unique landmark role/name pairs, and keyboard focus on the governed action.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - adds Story 1.18 fixture coverage for busy-region focus preservation, including `aria-busy` set/clear on the same labelled region and keyboard focus preservation after content replacement.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - adds Story 1.18 fixture coverage for validation failure behavior, including summary focus, `aria-invalid`, `aria-describedby`, and `aria-errormessage` association.

### Contract Tests
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` - verifies the accessibility floor contract list, keyboard operation metadata, visible-order focus sequence, repeated landmark uniqueness, disabled-action explanation metadata, overlay focus return, busy-region behavior, validation error association, current shell/page focus semantics, and package pin preservation.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs` - preserves disabled governed action, streaming Stop/Cancel, shortcut, overlay, and queue guardrail coverage.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` - preserves shared governed primitive coverage for accessible actor/evidence/risk/blocked/status components and page primitive usage.

## Coverage
- API endpoints: not applicable for Story 1.18.
- Accessibility floor contracts: 7/7 covered, including the review-added typed disabled-action explanation contract.
- Current governed operations UI surface: 1/1 covered for landmark/focus path and command behavior.
- Overlay focus policy kinds: modal dialog, modal sheet, popover, evidence drawer, review panel, and complementary region covered by contract tests.
- Disabled governed actions: reachable reason, `aria-disabled`, no native `disabled`, no tooltip-only explanation, and no disabled activation covered.
- Busy-region and validation focus rules: contract tests plus Playwright/static fixture coverage.

## Validation
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` - passed 40/40.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` - passed 12/12.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - passed 33/33.
- [x] `git diff --check` - passed with no whitespace errors.

## Notes
- `dotnet test` was attempted first, but VSTest could not open its local socket in this sandbox (`SocketException 13: Permission denied`). The compiled xUnit v3 runners were used instead, matching the story validation guidance.
- No package versions, UI framework versions, backend commands, API endpoints, or governed-command service behavior were changed.
