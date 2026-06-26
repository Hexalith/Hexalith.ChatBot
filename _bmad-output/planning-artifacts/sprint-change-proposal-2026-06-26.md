---
title: Sprint Change Proposal - AppHost EventStore Security Initialization
project: Chatbot
date: 2026-06-26
status: approved
mode: Batch
trigger: "Use HexalithEventStoreSecurityExtensions to initialize the shared security service in the ChatBot Aspire host."
scope_classification: Minor
recommended_approach: "Direct Adjustment - add a small Epic 11 follow-up story and implement the AppHost security wiring through the EventStore Aspire helpers."
owner: Jerome
prepared_at: 2026-06-26
approved_by: Jerome
approved_at: 2026-06-26
implementation_state: applied
---

# Sprint Change Proposal - AppHost EventStore Security Initialization

## 1. Issue Summary

The retained ChatBot local-development AppHost still initializes Keycloak and JWT bearer environment variables by hand:

- `src/Hexalith.ChatBot.AppHost/Program.cs` calls `builder.AddKeycloak("keycloak", 8180)` directly.
- The same file builds `realmUrl` manually and configures EventStore, Tenants, ChatBot, and EventStore Admin through a local `ConfigureJwt(...)` helper.
- EventStore Admin UI credentials are also wired manually through `EventStore__Authentication__*` environment variables.

The platform now already provides this AppHost security composition through `Hexalith.EventStore.Aspire.HexalithEventStoreSecurityExtensions`:

- `AddHexalithEventStoreSecurity(...)` adds the shared local Keycloak-backed security resource and returns `HexalithEventStoreSecurityResources`.
- `WithJwtBearerSecurity(...)` wires authority, issuer, audience, HTTPS metadata, and clears the signing-key fallback for OIDC mode.
- `WithEventStoreClientCredentials(...)` wires EventStore client credential settings for UI/admin clients.

This is a host-layer reuse correction. It does not change PRD scope, product behavior, UI/UX, or the FR81a command admission invariant. It keeps the Story 11.6 exception boundary intact: the AppHost remains only a local-development umbrella, but it should reuse the platform security helper instead of duplicating identity-provider composition.

## 2. Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | Triggered by completed Story 11.6 leaving the local AppHost shim with manual Keycloak/JWT setup while EventStore provides `HexalithEventStoreSecurityExtensions`. |
| 1.2 Core problem | [x] Done | Issue type: missed reuse of platform host-layer security helper. The AppHost duplicates reusable EventStore Aspire security wiring inside ChatBot. |
| 1.3 Evidence | [x] Done | `src/Hexalith.ChatBot.AppHost/Program.cs` contains `AddKeycloak`, `realmUrl`, `ConfigureJwt(...)`; `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs` contains the canonical helper set. |
| 2.1 Current epic completable? | [x] Done | Epic 11 is complete in sprint status, but this is a narrow follow-up under the same host-layer reuse theme. Do not reopen Story 11.6; add Story 11.7 or equivalent follow-up. |
| 2.2 Epic-level changes | [x] Done | Add one Epic 11 follow-up story: AppHost security-service initialization via EventStore Aspire helpers. |
| 2.3 Remaining epics impacted | [N/A] Skip | No product or UI epics are affected. Epic 10 launch paths should remain compatible. |
| 2.4 Obsolete / new epics | [N/A] Skip | No new epic needed. No existing epic becomes obsolete. |
| 2.5 Priority/order change | [x] Done | Implement before further AppHost topology work so later tests assert the platform helper path. |
| 3.1 PRD conflicts | [N/A] Skip | No PRD change. Identity and authorization requirements remain unchanged. |
| 3.2 Architecture conflicts | [x] Done | Architecture D8 and the ADR already require platform host-layer reuse. Add a small ADR/architecture note only if the team wants the security helper called out explicitly. |
| 3.3 UI/UX conflicts | [N/A] Skip | No UI flow, component, or accessibility change. |
| 3.4 Other artifacts | [!] Action-needed | Update `epics.md`, `sprint-status.yaml`, AppHost topology tests, and the AppHost project reference set after approval. |
| 4.1 Direct Adjustment | [x] Viable | Low effort, low risk. Replace manual security composition with platform helper calls and update tests. |
| 4.2 Potential Rollback | [N/A] Not viable | Reverting Story 11.6 would be disproportionate and would not solve security helper reuse. |
| 4.3 PRD MVP Review | [N/A] Not viable | MVP scope is unaffected. |
| 4.4 Recommendation | [x] Done | Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] Done | Issue, impacts, approach, edit proposals, and handoff are documented below. |
| 6.1-6.2 Final review | [x] Done | Proposal is internally consistent and implementation-ready pending approval. |
| 6.3 User approval | [!] Action-needed | Approval required before editing epics/sprint status or implementing. |
| 6.4 Sprint status update | [!] Action-needed | Add follow-up story to `sprint-status.yaml` only after approval. |
| 6.5 Handoff | [x] Done | Route to Developer agent after approval. |

## 3. Impact Analysis

### Epic Impact

Epic 11 remains the correct owning epic because the change is host-layer reuse and DomainService SDK alignment. The current Epic 11 summary already records the retained local AppHost shim. This correction should be expressed as a follow-up story instead of modifying completed Story 11.6 in place.

Recommended new story:

`11.7-apphost-security-service-initialization-via-eventstore-aspire`

### Story Impact

Affected completed story:

- Story 11.6 retained `src/Hexalith.ChatBot.AppHost` as a local-development umbrella. The implementation left manual security setup in that shim.

New follow-up story should cover:

- AppHost consumes `Hexalith.EventStore.Aspire`.
- AppHost uses `AddHexalithEventStoreSecurity()`.
- AppHost uses `WithJwtBearerSecurity(...)` for EventStore, Tenants, ChatBot Server, and EventStore Admin Server.
- AppHost uses `WithEventStoreClientCredentials(...)` for EventStore Admin UI.
- The local `ConfigureJwt(...)` helper and manual `AddKeycloak`/`realmUrl` plumbing are removed from ChatBot AppHost.
- Topology tests assert the platform helper path and prevent regrowth.

### Artifact Conflicts

No PRD conflict.

Architecture and ADR are already directionally aligned. Optional documentation updates can add one sentence under the Story 11.6 outcome/exception boundary saying the local AppHost shim must still reuse platform helpers for cross-cutting resources such as security.

Tests must be updated because existing AppHost tests currently assert manual wiring:

- `AppHostShouldWireKeycloakWithStartWait`
- `AppHostShouldAuthenticateEventStoreAdminThroughKeycloak`

### Technical Impact

Expected implementation files:

- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Likely project reference addition:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj" IsAspireProjectResource="false" />
```

No EventStore submodule source edit is required because the helper already exists.

## 4. Recommended Approach

Use Direct Adjustment.

This is a small technical alignment change, not a replan:

- The target helper already exists.
- Current manual code is localized to AppHost `Program.cs`.
- The AppHost project can reference `Hexalith.EventStore.Aspire` directly.
- Existing static topology tests can be updated to guard the new behavior.
- No product scope, data model, command contract, or UI behavior changes.

Risk level: Low.

Effort estimate: Low, likely one focused implementation pass plus build/test verification.

Timeline impact: No sprint resequencing required. Add as a backlog follow-up under Epic 11 and implement directly.

## 5. Detailed Change Proposals

### Story Change

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Epic 11

OLD:

```md
### Story 11.6: Retire module-owned `AppHost`/`Aspire`/`ServiceDefaults`; compose via `AddEventStoreDomainModule`
...
**Implementation result (Story 11.6, 2026-06-19):** standalone `.Aspire` and `.ServiceDefaults` projects were removed. `Hexalith.ChatBot.AppHost` remains as the ADR-scoped local-development shim, with internal Dapr wiring for `chatbot-statestore`, `chatbot-workflow-statestore`, and `chatbot-pubsub`. The deviation from full `AddEventStoreDomainModule(...)` composition is recorded because the current platform API cannot express those dedicated resources without an approved EventStore composition extension.
```

NEW:

```md
### Story 11.7: AppHost security-service initialization via EventStore Aspire helpers

As a platform operator,
I want the retained ChatBot local-development AppHost to initialize the shared security service through `HexalithEventStoreSecurityExtensions`,
So that identity-provider and JWT wiring stay owned by the EventStore Aspire platform helpers instead of duplicated inside ChatBot.

**Acceptance Criteria:**

**Given** the retained `Hexalith.ChatBot.AppHost` local-development shim
**When** Keycloak-backed security is enabled
**Then** the AppHost calls `AddHexalithEventStoreSecurity()` and uses the returned `HexalithEventStoreSecurityResources` to configure EventStore, Tenants, ChatBot Server, EventStore Admin Server, and EventStore Admin UI.

**Given** EventStore, Tenants, ChatBot Server, and EventStore Admin Server
**When** security is enabled
**Then** each server resource is configured through `WithJwtBearerSecurity(...)` with the correct audience: `hexalith-eventstore`, `hexalith-tenants`, and `hexalith-chatbot`.

**Given** EventStore Admin UI
**When** security is enabled
**Then** it uses `WithEventStoreClientCredentials(...)` and still receives `EventStore__AdminServer__SwaggerUrl`.

**Given** `EnableKeycloak=false`
**When** security is disabled
**Then** the local symmetric-key fallback behavior is preserved and Admin UI still receives the Swagger URL.

**And** `Program.cs` no longer contains direct `AddKeycloak`, manual `realmUrl` construction, or a local `ConfigureJwt(...)` helper.

**And** AppHost topology tests assert the platform helper path and forbid regrowth of manual JWT wiring.
```

Rationale: Story 11.7 keeps completed Story 11.6 intact while closing the remaining host-layer reuse gap.

### Sprint Status Change

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  11-6-retire-apphost-aspire-servicedefaults-and-compose-via-addeventstoredomainmodule: done
  epic-11-retrospective: done
```

NEW:

```yaml
  11-6-retire-apphost-aspire-servicedefaults-and-compose-via-addeventstoredomainmodule: done
  11-7-apphost-security-service-initialization-via-eventstore-aspire: backlog
  epic-11-retrospective: done
```

Rationale: Adds a follow-up without changing the accepted completion state of Story 11.6.

### AppHost Implementation Change

Artifact: `src/Hexalith.ChatBot.AppHost/Program.cs`

OLD:

```csharp
IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase))
{
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}
...
if (keycloak is not null && realmUrl is not null)
{
    ConfigureJwt(eventStore, keycloak, realmUrl, "hexalith-eventstore");
    ConfigureJwt(tenants, keycloak, realmUrl, "hexalith-tenants");
    ConfigureJwt(chatBot, keycloak, realmUrl, "hexalith-chatbot");
    ConfigureJwt(eventStoreAdmin, keycloak, realmUrl, "hexalith-eventstore");
    ...
}
...
static void ConfigureJwt(...)
```

NEW:

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
...
if (security is not null)
{
    _ = eventStore.WithJwtBearerSecurity(security);
    _ = tenants.WithJwtBearerSecurity(security, "hexalith-tenants");
    _ = chatBot.WithJwtBearerSecurity(security, "hexalith-chatbot");
    _ = eventStoreAdmin.WithJwtBearerSecurity(security);

    _ = eventStoreAdminUi
        .WithEventStoreClientCredentials(security)
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", adminSwaggerUrl);
}
else
{
    _ = eventStoreAdminUi.WithEnvironment("EventStore__AdminServer__SwaggerUrl", adminSwaggerUrl);
}
```

Rationale: Moves cross-cutting security resource setup back to the platform helper. Audience overrides preserve existing resource-specific JWT behavior.

### AppHost Project Change

Artifact: `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`

OLD:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore\Hexalith.EventStore.csproj" />
```

NEW:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore\Hexalith.EventStore.csproj" />
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Aspire\Hexalith.EventStore.Aspire.csproj" />
```

Rationale: The security helper lives in `Hexalith.EventStore.Aspire`.

### Test Change

Artifact: `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`

OLD:

```csharp
source.ShouldContain("AddKeycloak");
source.ShouldContain("WaitForStart(keycloak)");
source.ShouldContain("ConfigureJwt(eventStoreAdmin, keycloak, realmUrl, \"hexalith-eventstore\")");
```

NEW:

```csharp
source.ShouldContain("AddHexalithEventStoreSecurity");
source.ShouldContain("WithJwtBearerSecurity(security");
source.ShouldContain("WithEventStoreClientCredentials(security");
source.ShouldNotContain("static void ConfigureJwt");
source.ShouldNotContain("builder.AddKeycloak");
```

Rationale: Tests should protect the platform-helper path rather than the old manual path.

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation after approval.

Implementation tasks:

1. Add Story 11.7 to `epics.md`.
2. Add Story 11.7 to `sprint-status.yaml` as `backlog`.
3. Add the `Hexalith.EventStore.Aspire` project reference to ChatBot AppHost.
4. Replace manual security setup in AppHost `Program.cs` with `AddHexalithEventStoreSecurity()`, `WithJwtBearerSecurity(...)`, and `WithEventStoreClientCredentials(...)`.
5. Update AppHost topology tests.
6. Run focused verification.

Recommended verification:

```bash
dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false
dotnet test tests/Hexalith.ChatBot.AppHost.Tests/Hexalith.ChatBot.AppHost.Tests.csproj --no-restore -m:1 -nodeReuse:false
dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false
git diff --check
```

Tier-3 Aspire/Dapr verification is recommended on a prepared host because this changes AppHost security topology:

```bash
HEXALITH_CHATBOT_TIER3=1 dotnet test tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --filter FullyQualifiedName~TrivialGovernedCommandAspireE2eTests --no-restore -m:1 -nodeReuse:false
```

## 7. Approval

This proposal is ready for review.

Approval options:

- Continue: approve the proposal and route to implementation.
- Edit: revise story placement, acceptance criteria, or implementation scope before work starts.
