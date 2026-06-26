---
baseline_commit: 0ffe342
---

# Story 11.7: AppHost security-service initialization via EventStore Aspire helpers

Status: done

## Story

As a platform operator,
I want the retained ChatBot local-development AppHost to initialize the shared security service through `HexalithEventStoreSecurityExtensions`,
so that identity-provider and JWT wiring stay owned by the EventStore Aspire platform helpers instead of duplicated inside ChatBot.

## Acceptance Criteria

1. Given the retained `Hexalith.ChatBot.AppHost` local-development shim, when Keycloak-backed security is enabled, then the AppHost calls `AddHexalithEventStoreSecurity()` and uses the returned `HexalithEventStoreSecurityResources` to configure EventStore, Tenants, ChatBot Server, EventStore Admin Server, and EventStore Admin UI.
2. Given EventStore, Tenants, ChatBot Server, and EventStore Admin Server, when security is enabled, then each server resource is configured through `WithJwtBearerSecurity(...)` with the correct audience: `hexalith-eventstore`, `hexalith-tenants`, and `hexalith-chatbot`.
3. Given EventStore Admin UI, when security is enabled, then it uses `WithEventStoreClientCredentials(...)` and still receives `EventStore__AdminServer__SwaggerUrl`.
4. Given `EnableKeycloak=false`, when security is disabled, then the local symmetric-key fallback behavior is preserved and Admin UI still receives the Swagger URL.
5. `Program.cs` no longer contains direct `AddKeycloak`, manual `realmUrl` construction, or a local `ConfigureJwt(...)` helper.
6. AppHost topology tests assert the platform helper path and forbid regrowth of manual JWT wiring.

## Tasks / Subtasks

- [x] Add Story 11.7 to planning and sprint status artifacts.
- [x] Mark the approved 2026-06-26 sprint change proposal as approved.
- [x] Reference `Hexalith.EventStore.Aspire` from the ChatBot AppHost as a non-Aspire resource project reference.
- [x] Replace manual Keycloak/JWT setup in `Program.cs` with `AddHexalithEventStoreSecurity()`, `WithJwtBearerSecurity(...)`, and `WithEventStoreClientCredentials(...)`.
- [x] Preserve the `EnableKeycloak=false` Admin UI Swagger fallback.
- [x] Update AppHost topology tests to assert the platform security helper path and forbid `builder.AddKeycloak` / local `ConfigureJwt` regrowth.
- [x] Run focused verification.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Completion Notes List

- `src/Hexalith.ChatBot.AppHost/Program.cs` now initializes security through `Hexalith.EventStore.Aspire`.
- EventStore and EventStore Admin Server use the explicit `hexalith-eventstore` audience through `WithJwtBearerSecurity(...)`.
- Tenants and ChatBot Server keep their existing `hexalith-tenants` and `hexalith-chatbot` audiences.
- EventStore Admin UI uses `WithEventStoreClientCredentials(...)` and still receives the Admin Server Swagger URL.
- The ChatBot AppHost project reference to `Hexalith.EventStore.Aspire` is marked `IsAspireProjectResource="false"` to match sibling AppHost patterns.
- No `Hexalith.EventStore` submodule source files were modified.

### Verification

- `dotnet restore Hexalith.ChatBot.slnx -m:1 -nodeReuse:false` passed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` passed.
- `dotnet test tests/Hexalith.ChatBot.AppHost.Tests/Hexalith.ChatBot.AppHost.Tests.csproj --no-restore -m:1 -nodeReuse:false` passed: 11/11.
- `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false` passed: 63/63.
- `HEXALITH_CHATBOT_TIER3=1 dotnet test tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --filter FullyQualifiedName~TrivialGovernedCommandAspireE2eTests --no-restore -m:1 -nodeReuse:false` passed: 3/3.

### File List

- `_bmad-output/implementation-artifacts/11-7-apphost-security-service-initialization-via-eventstore-aspire.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/index.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-26.md`
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`
- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs`
