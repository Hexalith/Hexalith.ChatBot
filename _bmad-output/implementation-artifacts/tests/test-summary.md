# Test Automation Summary

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs` - OpenAPI 3.1 Contract Spine foundation, command-submission operation, local `$ref` resolution, tenant-authority exclusion, Hexalith extension naming, happy-path `202`, and critical failure responses `400/401/403/409/500`.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs` - RFC/Hexalith problem metadata fields, `details.visibility`-only nested metadata, and synthetic metadata-only examples.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs` - `IChatBotCommand` marker shape, stable enum wire names, ULID-only identity helpers, and command/event/rejection naming guardrails.
- [x] `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` - NSwag input/config shape, generated output provenance, nullable optional DTO generation, generated-client freshness hash, facade signature, facade metadata validation, command-name validation, and pre-compile generation target.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOracleTests.cs` - Story 1.2 oracle fixture alignment with operation ID, adapter input shape, forbidden authority fields, and metadata-only failure categories.

### E2E Tests

- [x] API/contract conformance coverage is the applicable Story 1.2 automation lane because this story has no ChatBot UI route or browser workflow.
- N/A: Browser E2E tests are not applicable until a UI surface is introduced by a later story.

## Coverage

- API contract operations: 1/1 command-submission operation covered.
- API success responses: 1/1 implemented success response covered (`202` accepted command).
- API critical error responses: 5/5 implemented critical error responses covered (`400`, `401`, `403`, `409`, `500`).
- Shared contract types: `IChatBotCommand`, 4 enum contracts, and 3 ULID identity helpers covered.
- Generated client guardrails: NSwag config, generated output location/provenance, nullable optional DTO generation, freshness hash, facade signature, and facade input validation covered.
- Conformance oracle: 1/1 Story 1.2 command-submission oracle fixture covered.
- UI features: 0/0 applicable for Story 1.2.

## Validation

- `dotnet restore Hexalith.ChatBot.slnx` exited 1 without normal console diagnostics under default parallel restore; `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` and focused project restore for the changed client graph passed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore` exits 1 under default parallel solution build; diagnostic output points at AppHost `Aspire.AppHost.Sdk` resolver behavior. `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` passed: 19 total, 0 failed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -noColor` passed: 8 total, 0 failed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -noColor` passed: 3 total, 0 failed.
- `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-build /m:1 /nr:false` remains blocked by the sandbox TCP listener restriction in VSTest (`System.Net.Sockets.SocketException (13): Permission denied`).

## Next Steps

- Add browser E2E coverage when a ChatBot UI workflow exists.
- Keep the Story 1.2 oracle fixture synchronized with future command-submission contract changes.
