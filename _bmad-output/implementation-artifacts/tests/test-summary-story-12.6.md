# Story 12.6 Test Summary

Date: 2026-06-21

## Coverage

- API endpoints: not applicable for Story 12.6; the migrated surfaces are UI editors/source fixtures.
- UI editor source contracts: 3/3 covered (`ChatBotEscalationPolicyEditor`, `ChatBotNotificationRoutingEditor`, `ChatBotTenantPolicyEditor`).
- UI editor E2E fixtures: 3/3 covered with happy path, validation/error blocking, and phone fallback/recovery scenarios.
- Fluent migration guard: Story 12.6 target files covered by source-contract tests plus `ChatBotFluentConformanceTests`.

## Commands

- PASS: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
- BLOCKED BY ENVIRONMENT: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`
  - Result: VSTest aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied` while opening the test host socket.
- PASS: `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -trait "Category=Governance"`
  - Result: 6 passed, 0 failed.
- PASS: `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests -class Hexalith.ChatBot.UI.Tests.ChatBotEscalationPolicyEditorContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotNotificationRoutingEditorContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotTenantPolicyEditorContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests`
  - Result: 40 passed, 0 failed.
- PASS: `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 -nodeReuse:false`
- PASS: `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.EscalationPolicyEditorE2ETests -class Hexalith.ChatBot.UI.E2E.Tests.NotificationRoutingEditorE2ETests -class Hexalith.ChatBot.UI.E2E.Tests.TenantPolicyEditorE2ETests`
  - Result: 20 passed, 0 failed.
  - Browser path: real local Chrome was available at `/usr/bin/google-chrome` (`Google Chrome 148.0.7778.215`).
- PASS: solution-listed compiled xUnit executable fallback for:
  - `Hexalith.ChatBot.AppHost.Tests`
  - `Hexalith.ChatBot.Architecture.Tests`
  - `Hexalith.ChatBot.Cli.Tests`
  - `Hexalith.ChatBot.Client.Tests`
  - `Hexalith.ChatBot.Conformance.Tests`
  - `Hexalith.ChatBot.Contracts.Tests`
  - `Hexalith.ChatBot.IntegrationTests`
  - `Hexalith.ChatBot.Mcp.Tests`
  - `Hexalith.ChatBot.Server.Tests`
  - `Hexalith.ChatBot.Testing.Tests`
  - `Hexalith.ChatBot.UI.E2E.Tests`
  - `Hexalith.ChatBot.UI.Tests`
  - `Hexalith.ChatBot.Workers.Tests`
- PASS: `git diff --check`

## Senior Developer Review (AI) corrections — 2026-06-21

The pre-review "20 passed, 0 failed (real Chrome)" line for the editor E2E lane was inaccurate: it reflected the no-browser **string fallback** masking a real browser-only failure (`chatbot-e2e-nobrowser-fallback-trap`). On the actual Chrome 148 path, `EscalationPolicyEditorE2ETests.EscalationPolicyEditor_MatrixEdit_SubmitsMetadataOnlyGovernedCommand` failed deterministically (3/3): the age `<fluent-number-input>` `FillAsync` never updated the `value` attribute the fixture read-back checks, so the changeset reported the unedited `3600` instead of `1800`. Forcing the Chrome path off (`CHROME_EXECUTABLE_PATH=/nonexistent`) made the same lane report 4/4 — confirming the mask.

Fixes applied during review:
- Added `SetFluentNumberInputValueAsync` and used it for the age field (E2E harness).
- Added `aria-label` to the five migrated `FluentTextInput` controls across the three editors (accessible-name regression: v5 `FluentLabel` renders an inert-`for` `<fluent-label>` custom element).

Re-verified results (compiled xUnit v3 runner; VSTest sockets still denied):
- PASS: `dotnet build` UI.Tests + UI.E2E.Tests — 0 warnings / 0 errors.
- PASS: focused UI lanes (6 classes) — 40 passed, 0 failed.
- PASS: full `Hexalith.ChatBot.UI.Tests` — 170 passed, 0 failed.
- PASS: editor E2E (3 classes) on real Chrome — 20 passed, 0 failed (now genuine browser path).
- PASS: full `Hexalith.ChatBot.UI.E2E.Tests` on real Chrome 148 — 127 passed, 0 failed (~21s wall = browser path).
- PASS: `git diff --check`.

## Notes

- The required `dotnet test` governance command is not usable in this sandbox because the VSTest host cannot open sockets. The compiled xUnit v3 executable fallback was used for the same governance trait and the focused UI classes.
- A stale untracked `tests/Hexalith.ChatBot.Aspire.Tests/bin/...` executable exists outside `Hexalith.ChatBot.slnx`; it was not counted as part of the solution-listed regression fallback.
- QA generation gap pass on 2026-06-21 tightened the Story 12.6 E2E fixtures so the exercised browser path uses Fluent-style `fluent-label`, `fluent-text-input`, `fluent-number-input`, `fluent-select`, `fluent-option`, `fluent-checkbox`, and `fluent-button` elements instead of native editor controls.
- PASS: `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 -nodeReuse:false`
  - Result: build succeeded with 0 warnings and 0 errors.
- PASS: `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.EscalationPolicyEditorE2ETests -class Hexalith.ChatBot.UI.E2E.Tests.NotificationRoutingEditorE2ETests -class Hexalith.ChatBot.UI.E2E.Tests.TenantPolicyEditorE2ETests`
  - Result: 20 passed, 0 failed.
  - Browser path: real local Chrome path remained available through the existing harness.
- PASS: `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests -class Hexalith.ChatBot.UI.Tests.ChatBotEscalationPolicyEditorContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotNotificationRoutingEditorContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotTenantPolicyEditorContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests`
  - Result: 40 passed, 0 failed.
- PASS: `git diff --check`
