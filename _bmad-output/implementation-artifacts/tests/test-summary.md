# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added SDK projection dispatch coverage through `DomainProjectionDispatcher`.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added `/project` HTTP coverage for successful ChatBot projection dispatch.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added `/project` unsupported-event no-op acknowledgement coverage.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added `/project` unknown-domain `404` coverage.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added host-level SDK telemetry and DAPR state-store health registration coverage.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/ReadModelProjectionStorePolicyTests.cs` - Added project-conversation read-model optimistic-conflict retry coverage using `IReadModelStore` and `ReadModelWritePolicy`.

## Coverage
- Story 11.4 acceptance criteria: 5/5 covered by focused API, host bootstrap, read-model policy, and architecture anti-regrowth tests.
- API endpoints: `/project`, `/health`, `/alive`, `/health/chatbot`, `/health/chatbot/workflows`, and `/health/chatbot/periodic-enforcement` covered by server bootstrap tests.
- Projection behavior: SDK dispatcher success, HTTP route success, unsupported-event no-op acknowledgement, unknown-domain not found, DAPR compatibility endpoints, duplicate replay, stale/out-of-order handling, and tenant partitioning covered.
- Read-model persistence: governed operation store conflict retry and project-conversation tenant/project index conflict retry covered against `IReadModelStore`.
- UI workflows: 0/0 applicable. Story 11.4 is a backend SDK-contract migration with no new browser flow.

## Validation
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` - passed, 1665 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - passed, 48 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.ChatBot.ServiceDefaults.Tests/bin/Debug/net10.0/Hexalith.ChatBot.ServiceDefaults.Tests` - passed, 5 total, 0 failed, 0 skipped.
- [x] `git diff --check` - passed.
- [x] `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` - attempted; VSTest aborted in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied` while creating its TCP listener.
- [x] `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false` - attempted; same sandbox VSTest TCP listener failure.
- [x] `dotnet test tests/Hexalith.ChatBot.ServiceDefaults.Tests/Hexalith.ChatBot.ServiceDefaults.Tests.csproj --no-restore -m:1 -nodeReuse:false` - attempted; same sandbox VSTest TCP listener failure.

## Next Steps
- Keep the new `/project` API tests in the server bootstrap lane so Story 11.5/11.6 host reduction cannot accidentally drop SDK projection dispatch.
