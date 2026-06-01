# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` - Added MCP tool-catalog contract allowlist coverage proving every exposed tool maps to the bounded command/query name and the expected state-changing classification.
- [x] `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` - Added typed backend `ProblemDetails` denial coverage proving catalog metadata is preserved while raw backend payloads remain redacted.
- [x] `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` - Added invalid number/list boundary coverage proving malformed MCP arguments fail closed before command submission.
- [x] `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` - Added direct JSON object/list payload coverage proving non-text identifiers and non-string list members fail closed without echoing restricted payload content.

### E2E Tests
- [x] No browser E2E test was applicable for Story 5.3. The MCP adapter has no visible UI surface; automated coverage uses xUnit v3 service-level MCP tool invocation, client-facade substitution, architecture fitness rules, AppHost realm tests, and conformance gates.
- [x] Existing MCP tests retained coverage for state-changing tools, read-only tools, safe denials, partial-success operation status, and focused CLI/MCP construction parity.

## Coverage

- MCP exposed tools: 12/12 pinned to explicit `mcp-exposed` descriptors and bounded command/query contract names.
- MCP state-changing tools: 7/7 submit typed `IChatBotCommand` records with `ChatBotSurfaceOrigin.Mcp`.
- MCP read tools: 5/5 route through `IChatBotClient` read methods and never submit commands.
- Critical error cases: unknown tool, missing argument, unsupported argument, invalid enum, invalid number, invalid list, direct JSON object/list payloads, stale credential, revoked grant, wrong surface, tenant mismatch, safe not found, and typed backend authorization denial.
- Safety/redaction: metadata-only denial shape is covered for boundary validation, raw backend denials, and typed catalog `ProblemDetails`; restricted project names, bearer tokens, raw claims, and provider payloads are asserted absent.
- UI/browser E2E: not applicable for this story because MCP is a machine-facing stdio adapter.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Mcp.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Mcp.Tests -parallel none` - 25 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 37 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 58 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` - 4 passed, 0 failed.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where a surface exists; no browser UI surface applies to MCP.
- [x] Tests use standard xUnit v3, NSubstitute, and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use semantic MCP tool names/descriptors instead of timing or sleeps.
- [x] Tests have clear descriptions and no order dependency.
- [x] Test summary created with coverage metrics.

## Next Steps

- Keep these MCP service-level tests aligned with Story 5.4 when the full UI/CLI/MCP differential harness replaces the current focused parity fixture.
