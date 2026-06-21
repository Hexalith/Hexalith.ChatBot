# Story 12.5 Test Summary

Date: 2026-06-21

## Commands

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - Result: Passed
  - Notes: QA automation pass after fixture gap fixes completed with 0 warnings and 0 errors.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -class "Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests" -class "Hexalith.ChatBot.UI.E2E.Tests.ApprovalQueuePriorityE2ETests"`
  - Result: Passed
  - Summary: 35 total, 0 failed, 0 skipped.
  - Notes: Story 12.5 QA automation pass; approval/task-intent/why-project fixtures now use Fluent-style controls, and approval queue E2E coverage now asserts the current disabled governed batch action rather than future fan-out behavior.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -class "Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotInteractionGuardrailContractTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotApprovalQueuePriorityContractTests" -class "Hexalith.ChatBot.UI.Tests.AssociationReviewComponentContractTests"`
  - Result: Passed
  - Summary: 51 total, 0 failed, 0 skipped.

- `git diff --check`
  - Result: Passed
  - Notes: QA automation pass after test fixture updates.

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - Result: Passed
  - Notes: Build completed with 0 warnings and 0 errors.

- `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`
  - Result: Blocked by environment before test execution
  - Notes: VSTest failed with `System.Net.Sockets.SocketException (13): Permission denied` while opening its local socket channel.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -trait "Category=Governance"`
  - Result: Passed
  - Summary: 6 total, 0 failed, 0 skipped.
  - Notes: Socket-free xUnit v3 executable fallback for the required Governance filter.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -class "Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotInteractionGuardrailContractTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests" -class "Hexalith.ChatBot.UI.Tests.ChatBotApprovalQueuePriorityContractTests" -class "Hexalith.ChatBot.UI.Tests.AssociationReviewComponentContractTests"`
  - Result: Passed
  - Summary: 51 total, 0 failed, 0 skipped.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -class "Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests" -class "Hexalith.ChatBot.UI.E2E.Tests.ApprovalQueuePriorityE2ETests"`
  - Result: Passed
  - Summary: 35 total, 0 failed, 0 skipped.
  - Notes: Chrome candidate `/usr/bin/google-chrome` exists for the Playwright harness; runner output does not separately label browser-path versus fallback assertions.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo`
  - Result: Passed
  - Summary: 170 total, 0 failed, 0 skipped.

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo`
  - Result: Passed
  - Summary: 124 total, 0 failed, 0 skipped.

- `git diff --check`
  - Result: Passed

## Review Correction (2026-06-21, story-automator code review)

The pre-review E2E results above were produced on the **no-browser fallback path** and were misleading: on the real browser path (Chromium present in this sandbox), three of the rewritten E2E tests **failed**, which the fallback masked.

- `ApprovalQueuePriorityE2ETests.ApprovalQueuePriority_GroupedPriorityWorkflow_KeepsBatchApproveDisabledWithReachableReason` — FAIL: `ClickAsync()` on the `aria-disabled="true"` `<fluent-button>` timed out (Playwright actionability: "element is not enabled").
- `ApprovalQueuePriorityE2ETests.ApprovalQueuePriority_PartialAuthorityOutcome_FocusesStatusAndKeepsSafeReasonReachable` — FAIL: same cause.
- `ProjectConversationE2ETests.TaskIntentReviewPanelShouldExposeReviewConversionAndDispositionWorkflow` — FAIL: `FillAsync` on the zero-box `<fluent-text-field>` timed out ("element is not visible").

Fixes applied during review:

- Force-click the advisory-disabled governed batch action (`ClickAsync(new() { Force = true })`) — this correctly models a reachable activation of an `aria-disabled` (not natively disabled) governed control and still asserts no batch fan-out command is recorded.
- Gave the simulated `<fluent-text-field>` predecessor input a visible bounding box (`display:inline-block;min-width;min-height;border`) so `FillAsync` succeeds on the browser path.

Re-verified on the real browser path after the fixes:

- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo` — Passed, 124 total, 0 failed (≈22s, live Playwright/Chromium interaction, not the string fallback).
- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo` — Passed, 170 total, 0 failed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` — Passed, 0 warnings, 0 errors.

## Coverage Notes

- Story 12.5 target files are no longer in `RawControlMigrationBacklog`.
- `ChatBotApprovalConversationItem`, `ChatBotTaskIntentReviewPanel`, and `ChatBotWhyProjectPanel` have no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` tags.
- Focused source-contract tests assert Fluent button/input/label markers plus approval disabled-reason, task-intent duplicate validation, why-project metadata-only, and correction-link markers.
- Approval queue contract and E2E/source-fixture tests remained green without behavior changes.
- QA automation gap fixes updated isolated E2E fixtures to exercise Fluent-style custom elements for Story 12.5 surfaces instead of raw buttons/inputs/labels.
- Approval queue E2E coverage now matches the Story 12.5 product contract: grouped rows and partial-authority reason remain visible, while batch approve stays `DisabledWithReason` and does not simulate fan-out.
