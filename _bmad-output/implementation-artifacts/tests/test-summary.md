# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 1.15. The story adds governed UI primitives and preserves the existing UI command client boundary; no new API endpoint or service behavior was introduced.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - covers runtime token stylesheet registration, governed-command UI origin, happy-path pending/audit outcomes, backend failure danger alert rendering, forced-colors non-color cues, and deterministic no-browser fallback.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - adds Story 1.15 primitive coverage for actor badge accessible names, unresolved actor action context, native evidence chip keyboard activation, redacted evidence disabled reason, risk chip status role, blocked-state alert role, non-color cues, and redaction-safe fixture text.

### Contract Tests
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` - verifies governed primitive files, exact actor/evidence/risk/blocked enum coverage, evidence/risk chip source contracts, blocked/status role contracts, governed operations primitive usage, semantic-token CSS, Fluent badge cue composition, and forced-colors cues.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` - verifies the semantic token foundation and required status slot examples used by the governed operations page.

## Coverage
- API endpoints: not applicable for Story 1.15.
- UI primitive set: 7/7 required primitives covered by contract tests.
- Actor categories: 8/8 required categories covered by contract tests.
- Evidence states: 4/4 required states covered by contract tests.
- Risk classes: 6/6 required classes covered by contract tests.
- Blocked reasons: 5/5 required reasons covered by contract tests.
- Governed operations workflow: 1/1 current governed-command workflow covered by E2E/static browser-contract tests.
- Semantic slots exercised in browser-contract flow: `info`, `warning`, `danger`, and `success`; static token contract covers all six slots.

## Validation
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings/0 errors.
- [x] `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` - passed 20/20.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` - passed 5/5.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - passed 33/33.

## Notes
- The generated E2E tests run Playwright when browser startup is available and retain deterministic no-browser contract assertions for restricted environments.
- No packages, UI frameworks, or governed-command service behavior were changed.
