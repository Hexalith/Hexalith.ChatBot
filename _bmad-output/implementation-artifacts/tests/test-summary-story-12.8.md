# Test Summary: Story 12.8

Date: 2026-06-21

## Commands and Results

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - Result: Passed, 0 warnings, 0 errors.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests`
  - Result: Passed, 6 total, 0 failed, 0 skipped.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotSemanticTokenContractTests`
  - Result: Passed, 8 total, 0 failed, 0 skipped.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotGovernedPrimitiveContractTests`
  - Result: Passed, 7 total, 0 failed, 0 skipped.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests`
  - Result: Passed, 10 total, 0 failed, 0 skipped.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests`
  - Result: Passed, 15 total, 0 failed, 0 skipped.

- Solution-built non-E2E xUnit executable regression fallback:
  - `Hexalith.ChatBot.AppHost.Tests`: Passed, 9 total.
  - `Hexalith.ChatBot.Architecture.Tests`: Passed, 63 total.
  - `Hexalith.ChatBot.Cli.Tests`: Passed, 24 total.
  - `Hexalith.ChatBot.Client.Tests`: Passed, 36 total.
  - `Hexalith.ChatBot.Conformance.Tests`: Passed, 97 total.
  - `Hexalith.ChatBot.Contracts.Tests`: Passed, 484 total.
  - `Hexalith.ChatBot.IntegrationTests`: Passed, 22 total, 3 skipped.
  - `Hexalith.ChatBot.Mcp.Tests`: Passed, 30 total.
  - `Hexalith.ChatBot.Server.Tests`: Passed, 1690 total.
  - `Hexalith.ChatBot.Testing.Tests`: Passed, 41 total.
  - `Hexalith.ChatBot.UI.Tests`: Passed, 170 total.
  - `Hexalith.ChatBot.Workers.Tests`: Passed, 32 total.
  - Aggregate: 2698 total, 0 failed, 3 skipped.

- Affected E2E fixture classes:
  - `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 -nodeReuse:false`
    - Result: Passed, 0 warnings, 0 errors.
  - `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.Story12CssRetirementE2ETests`
    - Result: Passed, 3 total, 0 failed, 0 skipped.
  - `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests`
    - Result: Passed, 32 total, 0 failed, 0 skipped (re-verified 2026-06-21 after review remediation; see "Senior Review Remediation" below — the initial dev-recorded "0 failed" was inaccurate: this lane had 1 real failure caused by the CSS retirement).
  - `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.GovernedOperationsVisualFoundationE2ETests`
    - Result: Passed, 39 total, 0 failed, 0 skipped (re-verified 2026-06-21 after review remediation; the initial dev-recorded "0 failed" was inaccurate: this lane had 1 real failure caused by the CSS retirement).

- `rg -n -- "--chatbot-type-|--chatbot-font-|--chatbot-radius-|\\.chatbot-button|\\b(button|input|select|textarea)([:.#\\s,{>+~\\[]|$)" src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
  - Result: Passed, no matches.

- `rg -n "<(button|input|select|textarea)(\\s|/|>)" src/Hexalith.ChatBot.UI/Components`
  - Result: Passed, no matches.

- `git diff --check`
  - Result: Passed.

## QA Automation Addendum

- Added `tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs` to lock the discovered Story 12.8 gap: retired presentation classes must not return as source or E2E fixture hooks, and high-risk controls must expose Fluent parameters plus ids/ARIA/`data-chatbot-*` contracts instead.

## Senior Review Remediation (2026-06-21)

An adversarial review re-ran every affected lane and found the original handoff had recorded two
E2E lanes as "0 failed" when each had **1 real failure** introduced by the CSS retirement. Both
were genuine production accessibility regressions, not test-only artifacts. Fixed and re-verified:

1. **`hidden` panels stayed visible** (`ProjectConversationWhyProjectPanelShouldOpenFromEmailAndDecisionRowsAndRemainMetadataOnly`).
   The retirement deleted `.chatbot-why-project-panel[hidden] { display: none }` while keeping
   `.chatbot-why-project-panel` in the `display: grid` group, so the author `display` overrode the
   user-agent `[hidden]` rule and `hidden`-toggled panels never closed. Fixed with a single
   user-agent reset `[hidden] { display: none !important; }` (AC2-allowed reset; protects every
   display-grouped element, not just the why-project panel).
2. **Touch targets collapsed below the 44px / 24px minimums**
   (`TouchTargetsShouldMeetPrimaryAndDenseMinimumsAtPhoneAndTabletWidths`; observed 21px). The
   retirement deleted the `.chatbot-governed-action button` / `.chatbot-streaming-stop button`
   (44px) and `.chatbot-actor-badge__action` (24px) sizing because they were native-control
   selectors, but never re-applied a conformance-safe replacement. Fixed by applying the existing
   `.chatbot-touch-target-primary` / `.chatbot-touch-target-dense-secondary` utility classes to the
   governed-action, streaming-stop, and actor-badge buttons in production components
   (`ChatBotGovernedAction.razor`, `ChatBotStreamingStopControl.razor`, `ChatBotActorBadge.razor`)
   and in the affected E2E fixtures.

Post-remediation re-verification (compiled xUnit v3 runner; real Chromium executed):

- `Hexalith.ChatBot.UI.Tests` (full): Passed, 170 total, 0 failed, 0 skipped.
- `Hexalith.ChatBot.UI.E2E.Tests` (full suite): Passed, 130 total, 0 failed, 0 skipped.
- Governance/semantic/primitive guards, `ChatBotFluentConformanceTests` (6), `ChatBotSemanticTokenContractTests` (8), `ChatBotGovernedPrimitiveContractTests` (7): all green.
- `rg` forbidden-primitive and raw-control checks: no matches; `git diff --check`: clean.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`: 0 warnings, 0 errors.

## Environmental Limitations

- `DiffEngine_Disabled=true dotnet test Hexalith.ChatBot.slnx --no-build -m:1` was attempted and aborted by VSTest socket creation failures (`System.Net.Sockets.SocketException (13): Permission denied`). The direct xUnit v3 executable fallback above was used for regression evidence.
- Full cross-surface browser/a11y visual re-verification was not run because Story 12.9 owns that final verification pass. Story 12.8 ran only the affected E2E fixture classes touched by the CSS retirement.
