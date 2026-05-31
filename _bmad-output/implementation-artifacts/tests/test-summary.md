# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 1.20. The story establishes UI-owned English/French localization infrastructure and does not add API endpoints or backend service behavior.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - covers English and French governed-operations rendering, localized headings/actions/labels, unchanged operation IDs/status codes/audit metadata, and no horizontal overflow at phone width.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - adds French critical-label expansion coverage for actor, risk, state, confidence, next-action, and safe recovery reason text across desktop/tablet/phone widths.
- [x] Existing governed-operations E2E coverage remains in place for semantic token loading, UI-origin command dispatch, live-region deduplication, reduced-motion behavior, retryable failure status, forced-colors cues, responsive/touch behavior, accessibility landmarks/focus, validation focus, and streaming stop focus return.

### Contract Tests
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` - covers supported cultures, English/French resource completeness, real `IStringLocalizer<SharedResource>` resolution, missing-key failure, localized governed labels, phrase-level accessible labels, culture-aware display formatting, stable machine identifiers, package pins, and localization source contracts.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` - covers localized safety-critical default primitive copy for risk policy reason, blocked-state reason, safe next action, disabled reason, and the governed command path landmark.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` - adds interaction guardrail localization coverage so guardrail display labels resolve through stable English/French resource keys.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs` - now asserts each UX-DR33 guardrail has a stable localization resource key.

## Coverage
- API endpoints: not applicable for Story 1.20.
- Supported UI cultures: 2/2 covered (`en`, `fr`) with English default/fallback contract.
- Resource keys: all keys in `ChatBotUiTextKey.All` covered for English and French resource presence.
- Governed UI localized text: actor categories, evidence states, risk classes, feedback kinds, blocked reasons, interaction guardrails, status labels, disabled reasons, stop/cancel labels, and governed-operations fixture labels covered.
- Stable machine identifiers: operation ID, command ID, correlation ID, lifecycle state, completion status, audit status, safe next actions, audit metadata, slots, and icon text covered for non-localization under French culture.
- French expansion: critical actor/risk/state/confidence/next-action/recovery labels covered at 1280px, 800px, and 390px with hidden-overflow and page-overflow checks.

## Validation
- [x] `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` - passed 63/63.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` - passed 17/17.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `git diff --check` - passed with no whitespace errors.

## Notes
- Compiled xUnit v3 executables were used for test execution, matching the story validation guidance.
- `dotnet test` via VSTest is blocked in this sandbox by `System.Net.Sockets.SocketException (13): Permission denied`; the compiled xUnit v3 runners completed successfully.
- Tests explicitly exercise `en`, `fr`, `en-US`, and `fr-FR` culture paths.
- No API tests were generated because Story 1.20 has no API surface.
