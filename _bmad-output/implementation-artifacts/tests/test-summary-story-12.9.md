# Test Summary - Story 12.9

Date: 2026-06-21T22:08:16+02:00

## Scope

Story 12.9 re-verified the Epic 12 Fluent-migrated ChatBot UI surfaces for accessibility, visual conformance, localization, source governance, E2E fallback quality, and non-UI parity.

## Browser Availability

- Chrome path: `/usr/bin/google-chrome`
- Chrome version: `Google Chrome 148.0.7778.215`
- Bare smoke command (does NOT use the harness args): `/usr/bin/google-chrome --headless=new --no-sandbox --disable-setuid-sandbox --disable-gpu --disable-dev-shm-usage --dump-dom about:blank` → fails, exit code 133, `setsockopt: Operation not permitted (1)` from crashpad socket setup.
- BrowserHarness smoke command (the args the suite actually launches with): `/usr/bin/google-chrome --headless=new --no-sandbox --disable-setuid-sandbox --no-zygote --single-process --disable-gpu --disable-dev-shm-usage --disable-crash-reporter --disable-crashpad --dump-dom about:blank` → succeeds, exit code 0, DOM rendered. The `--single-process --no-zygote --disable-crashpad` flags avoid the crashpad socket call that the bare command trips on.
- E2E result: Chrome IS launchable through the harness. The full compiled suite executed through the real browser path with 0 skips; the compliance audit investigation test ran live against the rendered page and passed (it was NOT skipped). The honest no-browser skip branch still fires only when Chrome is genuinely absent — verified by forcing `CHROME_EXECUTABLE_PATH=/nonexistent/chrome` (→ 1 skipped, 0.07s) versus the default real-browser path (→ 0 skipped, passed, 0.83s).

## Commands

| Command | Result |
| --- | --- |
| `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false -v:minimal` | Passed, 0 warnings, 0 errors |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo` | Passed: 173 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none` | Passed through the real browser path: 134 total, 0 failed, 0 skipped (58.6s wall-clock confirms live Chromium launches) |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo` | Passed: 97 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -noLogo` | Passed: 24 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.Mcp.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Mcp.Tests -noLogo` | Passed: 30 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo` | Passed: 63 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotResponsiveTouchContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests -class Hexalith.ChatBot.UI.Tests.ChatBotSemanticTokenContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotGovernedPrimitiveContractTests` | Passed: 55 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -class Hexalith.ChatBot.UI.E2E.Tests.Story12FluentReleaseReadinessE2ETests` | Passed: 4 total, 0 failed, 0 skipped |
| `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -method Hexalith.ChatBot.UI.E2E.Tests.ComplianceAdministrationE2ETests.ComplianceAuditInvestigationShouldExposeMetadataOnlyTimelineAndSafeEscalation` | Passed through the real browser path: 1 total, 0 failed, 0 skipped (0.83s live render) |
| `rg -n "<(button\|input\|select\|textarea)(\\s\|/\|>)" src/Hexalith.ChatBot.UI/Components` | Passed: no matches |
| `rg -n -- "--chatbot-type-\|--chatbot-font-\|--chatbot-radius-\|\\.chatbot-button\|\\b(button\|input\|select\|textarea)([:.#\\s,{>+~\\[]\|$)" src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` | Passed: no matches |
| `git diff --check` | Passed |

## Notes

- The Story 12.9 E2E release-readiness matrix guards every Epic 12 migrated surface against silent coverage loss and checks that browser-unavailable fallbacks remain source-backed and non-vacuous.
- No production UI, CLI, MCP, backend, package, or submodule source changes were required.
- Browser evidence (corrected during code review 2026-06-21): the harness launch args (`--single-process --no-zygote --disable-crashpad`) successfully start Chrome in this environment, so the full E2E suite ran through the real browser path with 134 passed / 0 failed / 0 skipped, including a live compliance audit investigation render. An earlier draft of this summary recorded a "1 skipped / Chrome cannot launch" limitation derived from a bare smoke command that omitted those flags; that limitation did not reflect the harness behavior and has been corrected. The source-backed fallback assertions remain in place and continue to fire for CI environments that ship no Chrome install.
