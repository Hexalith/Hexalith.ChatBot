---
title: 'Return Versioned Safe Outcomes'
type: 'feature'
created: '2026-10-12'
status: 'in-progress'
baseline_commit: 8437900622d5bf7a9d34ca5cc543cd802f653cb1
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Existing safe outcomes have English catalog copy and a wire version, but unknown codes can throw, French UI copy covers only part of the catalog, and public renderers can pass through untrusted error text. A user can receive a missing, unsafe, or unactionable explanation.

**Approach:** Make the existing catalog authoritative, resolve unknown or untranslated values to a terminal generic denial, and present English/French copy and permitted actions at public boundaries.

## Boundaries & Constraints

**Always:** Preserve OpenAPI 3.1 authority, metadata-only RFC 9457 problems, existence-neutral denial, trusted-context gates, and the v1 wire version unless a contract change requires regeneration. Each entry has code, version, headline (at most 80 characters), one safe sentence, terminality, applicable retry/escalate/dismiss/request-access action, disabled reason, and accessible description. Use FrontComposer and Fluent UI V5; keep state inline and deduplicate announcements. Keep precise causes in authorized diagnostics.

**Never:** Render raw exceptions, untrusted problem prose, restricted names or identifiers, secrets, or hidden owner details. Unknown codes cannot imply success or retry. No second wire contract, persistence path, or global banner for local failure.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Known outcome | Catalog code and English/French locale | Complete localized headline, sentence, terminality, safe action and accessible description | No fragments or unsupported actions |
| Unknown or untranslated | Null, malformed, absent code or missing locale entry | Generic terminal denial with dismiss action | Count uncategorized code without recording raw input |
| Public failure | API, UI, CLI, or MCP receives unsafe detail | Catalog-backed metadata-only response or rendering | Strip raw prose, path, secret, resource and exception data |
| State transition | Inline status changes or action unavailable | Persistent text and semantic status; deduplicated feedback; focusable action with disabled reason | No false success or global banner |

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCatalogEntry.cs` — 86 English entries; `Resolve` throws on unknown codes and entries lack terminality/version.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`, `Redaction/CoarseUserFacingRedactionStage.cs`, `Status/OperationStatusHttpResults.cs` — public failure paths; redaction copies supplied prose and unknown status actions can throw.
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`, `SharedResource.resx`, `SharedResource.fr.resx` — only 19 failure codes are translated; missing resources throw.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor` — reuse inline status and transition deduplication.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` and `ChatBotMessageCatalogEntry.cs` — add version/state/terminality/action/accessibility metadata and deny-safe resolution, preserving stable codes.
- [x] `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`, `SharedResource.resx`, `SharedResource.fr.resx`, `ChatBotUiTextKey.cs` — resolve all 86 codes in both locales with complete phrases and safe missing-translation fallback.
- [x] `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`, `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`, `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs` — catalog-normalize failures and unknown actions before serialization.
- [x] `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor` — render text plus semantic status and permitted focusable action, associate unavailable reasons, and reuse transition deduplication.
- [x] `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`, `src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs` — resolve problem code rather than forwarding wire prose; keep output metadata-only.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`, `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantLeakageScannerTests.cs`, `tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs`, `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`, `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` — test matrix, locale parity, assistive output, and sentinel leakage.

**Acceptance Criteria:**

- Given every code, when English and French entries are checked, then version, state, terminality, action, disabled reason, accessible description, headline limit, and one-sentence copy agree.
- Given any unknown, malformed, or untranslated code, when a public boundary resolves it, then the generic result is terminal, existence-neutral, and non-retryable, and raw input is absent from output and telemetry.
- Given refused, blocked, degraded, failed, denied, waiting, retryable, or unsupported outcomes, when API, UI, CLI, or MCP presents them, then catalog copy and actions contain no restricted sentinel across response, render, or telemetry.
- Given a UI transition in light, dark, forced-colors, reduced-motion, keyboard-only, and screen-reader use, when status renders, then state stays inline, text and semantic status identify it, deduplicated feedback announces the transition, and action or disabled reason is accessible.

## Implementation Notes

- The catalog now contains 87 entries including the terminal `generic_denial` fallback; OpenAPI v1 and the generated client gained the previously omitted codes without renumbering existing enum members.
- English and French resources cover every catalog entry. Server Problem Details and status results, UI failure components, CLI, and MCP resolve public copy and actions through the catalog. Unknown codes suppress caller prose, disallow retry, and expose only a dismiss action.
- Operation status intersects catalog actions with persisted per-operation permissions. Actual Razor render tests cover known and unknown localized failures, semantic status, announcement deduplication, and disabled action reasons.
- No persistence path, dependency version, or public wire version changed. The additive OpenAPI code enum required client regeneration and a refreshed client hash fixture.

## Spec Change Log

- 2026-10-12: Implemented Story 1.4 and added focused render, leakage, catalog, and boundary coverage. The broad Debug build remains blocked by existing sibling reference conflicts; see Verification.

## Review Triage Log

## Verification

**Commands:**

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1 /nr:false` — all changed projects compile with warnings as errors.
- Build and run native xUnit v3 assemblies individually for Contracts, Server, UI, CLI, MCP, and UI E2E — relevant suites pass with zero skips.
- Run OpenAPI/client freshness checks if the wire contract changes — schema and generated client agree.

**Results (2026-10-12):**

- Focused native xUnit v3 lanes passed with zero skips: Contracts catalog 16, Client generation 26, UI localization 19, CLI 24, MCP 30, Server gateway 395, Server operation status 12, Conformance leakage 6, and UI E2E 6 (534 total). The final French copy edit was rebuilt with `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --configuration Debug -m:1 /nr:false -v:q -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` and its real render lane passed 2/2.
- `git -c core.whitespace=cr-at-eol diff --cached --check` passed.
- Broad gate: `dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1 /nr:false` failed with 1590 errors in root-declared sibling projects, including duplicate `Hexalith.Commons.UniqueIds` assembly imports (CS1704) and missing EventStore/FrontComposer types.
- Narrow Server/Conformance Debug source-reference builds and tests passed with command-line `-p:NoWarn=CS0169%3BCS0414` for three generated Hexalith.Projects unused-field warnings. Without that suppression, the narrow build stops on those warnings; the package-reference Server build stops at pre-existing CS0246 (`BoundWorkloadCommandRequest` in `ChatBotStreamReadProofSource.cs:49`). This qualified evidence does not replace the broad gate.
- Light/dark, forced-colors, reduced-motion, and full screen-reader visual traversal were not rerun against a live AppHost because the broad source build is blocked. The real-component render lane covers semantic status, focusable action reason, localized copy, and announcement behavior.
