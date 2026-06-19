# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added SDK `/process` admission coverage for unauthenticated envelopes, cross-tenant payloads, lifecycle-invalid commands, idempotency conflicts, duplicate replay, audit-unavailable fail-closed behavior, and malformed payload rejection.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Verified typed `DomainServiceWireResult` rejection payloads and asserted rejected SDK admissions do not emit processor events.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added route ownership coverage that asserts SDK canonical routes (`/process`, `/replay-state`, `/query`, `/project`, `/admin/operational-index-metadata`) and the temporary public command adapter are each mapped exactly once.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Exercised the in-process host through HTTP `POST /process` against the SDK endpoint and ChatBot admission hook.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs` - Existing anti-regrowth tests validate SDK host shape, admission registration, removed custom DomainService endpoint classes, and route ownership boundaries.

## Coverage
- Story 11.5 acceptance criteria: 6/6 covered by API host tests, SDK admission-path tests, and architecture anti-regrowth tests.
- API endpoints: `/process`, `/query`, `/project`, `/replay-state`, `/admin/operational-index-metadata`, `/health/chatbot`, `/alive`, and compatibility routes covered by server bootstrap, route ownership, and architecture tests.
- Admission paths: accepted, unauthenticated, cross-tenant, not-allowlisted, lifecycle-invalid, idempotency-conflict, duplicate replay, audit-unavailable, and malformed payload rejection covered.
- UI workflows: 0/0 applicable. Story 11.5 is a backend SDK host/admission migration with no new browser flow.

## Validation
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` - passed, 0 errors; one existing `StackExchange.Redis` version-conflict warning in `Hexalith.Tenants`.
- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests` - passed, 69 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayAdmissionApiE2ETests` - passed, 50 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Architecture.Tests.DomainServiceSdkHostAdoptionAdrTests` - passed, 8 total, 0 failed, 0 skipped.
- [x] `git diff --check` - passed.
- [x] `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` - build passed; VSTest aborted in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied` while creating its TCP listener.
- [x] `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false` - build passed; same sandbox VSTest TCP listener failure.

## Next Steps
- Keep the `/process` SDK admission cases in the server bootstrap lane so future host reduction cannot bypass ChatBot governance or regress typed admission rejection behavior.
