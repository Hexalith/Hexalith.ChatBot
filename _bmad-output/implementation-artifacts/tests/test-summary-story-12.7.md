# Story 12.7 Test Summary

Date: 2026-06-21

## Commands

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - Result: PASS
  - Notes: Debug solution build completed with 0 warnings and 0 errors.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ComplianceAuditSurfaceTests`
  - Result: PASS
  - Total: 10, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests`
  - Result: PASS
  - Total: 6, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.OperationalDashboardsComponentContractTests`
  - Result: PASS
  - Total: 4, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests`
  - Result: PASS
  - Total: 10, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests`
  - Result: PASS
  - Total: 15, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.ComplianceAdministrationE2ETests` (default agent command sandbox, post-review fix)
  - Result: PASS
  - Total: 3, Failed: 0, Skipped: 0 (audit-investigation, retention-validation, phone-fallback)
  - Browser path: real `/usr/bin/google-chrome` (`Google Chrome 148.0.7778.215`). Chrome launched and executed the live page under the **default** command sandbox (no sandbox-disable workaround needed). 0 skipped proves the real-browser assertions ran.

- Senior Developer Review (AI) correction — the original dev record above claimed `ComplianceAdministrationE2ETests` passed 3/3 on real Chrome. On re-execution during review the audit-investigation method **deterministically FAILED** on the real browser: `AssertAuditFilterFluentControlsAsync` called `page.GetByLabel("To")` without `Exact = true`, which substring-matches the accessible name "Actor" (Ac-**to**-r) too, so Playwright strict mode resolved 2 elements and threw. This is the `chatbot-e2e-nobrowser-fallback-trap` again — a real browser-only failure that the prior unconditional `StartAsync` hard-require would either expose (as here) or, in CI, convert into a hard build break. Review fixes applied:
  - Added `new() { Exact = true }` to the audit filter `GetByLabel` lookups so "To"/"From" no longer collide with "Actor"/other labels (root-cause fix; now 3/3 green on real Chrome).
  - Reverted the audit-investigation test from the unconditional `BrowserHarness.StartAsync` hard-require back to the repo-wide `TryStartAsync` pattern, but replaced the silent no-browser string fallback with a visible `Assert.Skip` so a browserless run skips honestly (CI runs `dotnet test Hexalith.ChatBot.slnx` with no Chrome-install step; the hard-require would have failed there) without re-introducing the masking fallback.
  - Removed the now-dead `AssertAuditInvestigationFixtureWithoutBrowser` helper and the vestigial `BrowserHarness.ChromeExecutable` property.

- Sandbox note: earlier `setsockopt: Operation not permitted (1)` / `SIGTRAP` crashpad failures attributed to the agent command sandbox did NOT reproduce during review — Chrome 148 launched and ran the full class under the default sandbox via the compiled E2E runner (the harness launches with `--no-sandbox --disable-setuid-sandbox --no-zygote --single-process --disable-crashpad`).

- `rg -n "<(button|input|select|textarea)(\\s|/|>)" src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor || true`
  - Result: PASS
  - Output: no matches.

- `rg -n "FluentDataGrid|chatbot-labelled-row-list|role=\"table\"|data-chatbot-dashboard-view|data-chatbot-freshness|data-chatbot-affected-scope|data-chatbot-next-safe-action|data-chatbot-slo-metric|data-chatbot-slo-burn|data-chatbot-queue-" src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
  - Result: PASS
  - Notes: Dashboard/governed pages still expose the expected labelled-row/table and data-marker contracts; no `FluentDataGrid` match.

- `git diff --check`
  - Result: PASS

## Browser Path

Real Chrome (`/usr/bin/google-chrome`, Google Chrome 148.0.7778.215) ran to full page execution under the default command sandbox after the Senior Developer Review fixes. The full `ComplianceAdministrationE2ETests` class passed 3/3 with 0 skipped on the real browser path. Review re-execution found and fixed two browser-only issues the earlier no-browser path had masked: the phone-fallback strict-mode assertion over multiple dense regions and the non-exact `"To"` label lookup colliding with `"Actor"`.

## QA Generate E2E Follow-up

Date: 2026-06-21
Workflow: `.agents/skills/bmad-qa-generate-e2e-tests`

Generated/strengthened test coverage:

- `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs` now verifies every migrated audit filter is reachable by accessible name on the browser path, that text filters render as `fluent-text-input`, that the limit filter renders as `fluent-number-input`, and that the expected Fluent filter element counts are present (`12` labels, `11` text inputs, `1` number input).
- The workflow-generated generic `_bmad-output/implementation-artifacts/tests/test-summary.md` drift was restored to its previous content; Story 12.7 verification is recorded in this story-specific summary.

Commands:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - Result: PASS
  - Notes: Debug solution build completed with 0 warnings and 0 errors.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ComplianceAuditSurfaceTests`
  - Result: PASS
  - Total: 10, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests`
  - Result: PASS
  - Total: 6, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.OperationalDashboardsComponentContractTests`
  - Result: PASS
  - Total: 4, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests`
  - Result: PASS
  - Total: 10, Failed: 0

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests`
  - Result: PASS
  - Total: 15, Failed: 0

- `DiffEngine_Disabled=true CHROME_EXECUTABLE_PATH=/does/not/exist tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -method Hexalith.ChatBot.UI.E2E.Tests.ComplianceAdministrationE2ETests.RetentionConfigurationValidationShouldFocusSummaryAndSubmitSafeSnapshotMetadata -method Hexalith.ChatBot.UI.E2E.Tests.ComplianceAdministrationE2ETests.CompliancePhoneFallbackShouldKeepReadOnlySummaryAndEscalationReachable`
  - Result: PASS
  - Total: 2, Failed: 0
  - Notes: This intentionally exercised the remaining no-browser fallback assertions after the browser-path attempt was blocked.

- `DiffEngine_Disabled=true CHROME_EXECUTABLE_PATH=/usr/bin/google-chrome tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.ComplianceAdministrationE2ETests`
  - Initial automate result: failed on the live browser before review because the non-exact `"To"` label lookup also matched `"Actor"`.
  - Final reviewed result: PASS, 3 total, 0 failed, 0 skipped on real Chrome 148 after adding exact label lookups and preserving visible skip behavior only when no browser is available.
