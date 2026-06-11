---
baseline_commit: 8607d52
---

# Story 10.1: FrontComposer Shell integration

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-11. -->

## Story

As a frontend engineer,
I want `Hexalith.ChatBot.UI` wired to the FrontComposer Shell,
so that the UI composes through the mandated FrontComposer layer instead of the temporary token-alias bridge.

## Acceptance Criteria

1. **FrontComposer Shell is a real UI dependency and the adapter boundary remains clean.** Given the UI project, when shell integration lands, then `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj` has a ProjectReference to `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj` from the root-level read-only submodule, and no Server, gateway-stage, DAPR, audit, idempotency, projection-store, or internal data-plane reference is introduced. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.1; _bmad-output/planning-artifacts/architecture.md#Frontend Architecture; .gitmodules]

2. **Startup follows the FrontComposer bootstrap order.** Given application startup, when services are registered in `src/Hexalith.ChatBot.UI/Program.cs`, then FrontComposer is wired in this order: `AddHexalithFrontComposerQuickstart(...)` -> `AddHexalithDomain<TMarker>()` -> `AddHexalithEventStore(...)`. The quickstart call owns the authoritative Fluxor store, storage service, localization, shell registry, and shell services. The domain marker is stable and ChatBot-owned. The EventStore client swap runs last and receives the configured gateway base address without bypassing existing governed ChatBot client paths. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs#AddHexalithFrontComposerQuickstart; Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidator.cs; Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs]

3. **Layout collapses to the framework shell with one provider tree.** Given the app layout, when it renders, then `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` reduces to `<FrontComposerShell AppTitle="Hexalith ChatBot">@Body</FrontComposerShell>` or the equivalent with explicit shell slots, and there is exactly one `<FluentProviders />` in the rendered tree. Because `FrontComposerShell` renders providers and `StoreInitializer`, remove the app-owned provider from `Components/App.razor` and do not add another Fluxor initializer. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor; Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs; src/Hexalith.ChatBot.UI/Components/App.razor; src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor]

4. **The Story 1.14 token alias bridge is retired or reconciled against shell tokens.** Given `chatbot.tokens.css` exists as a temporary inheritance bridge, when the shell lands, then the file is either removed where no longer needed or reduced to ChatBot-specific semantic aliases over Fluent/FrontComposer variables. The comment that calls the file temporary must be removed or updated to describe the remaining ChatBot-owned alias layer. No duplicate/raw-hex semantic color mapping is introduced. [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md#Acceptance Criteria; src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css; tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs]

5. **Architecture and UI tests prove the integration is non-vacuous.** Given the adapter boundary, when focused tests run, then architecture tests allow the FrontComposer Shell/Contracts reference while still rejecting Server/gateway/DAPR/audit/idempotency internals; UI tests prove the app no longer owns duplicate providers, the layout uses `FrontComposerShell`, and the token bridge is not a second design system. The build is Release-clean with warnings as errors, and the default focused test lane for UI + architecture is green. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs; tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs; Directory.Packages.props]

## Tasks / Subtasks

- [x] Add the FrontComposer Shell project reference without changing package versions (AC: 1)
  - [x] Update `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj` to reference `..\..\Hexalith.FrontComposer\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj`.
  - [x] Keep existing references to `Hexalith.ChatBot.Client` and `Hexalith.ChatBot.ServiceDefaults`.
  - [x] Do not add references to `Hexalith.ChatBot.Server`, Dapr packages, EventStore server packages, audit/idempotency internals, or gateway-stage implementations.
  - [x] Do not edit files inside `Hexalith.FrontComposer`; consume it as a root-level read-only submodule.

- [x] Wire FrontComposer services in `Program.cs` in the required order (AC: 2)
  - [x] Add the necessary FrontComposer Shell extension namespaces.
  - [x] Replace the standalone `AddFluentUIComponents()` + `AddFluxor(...)` ownership with `AddHexalithFrontComposerQuickstart(static options => options.ScanAssemblies(typeof(Program).Assembly))`, unless implementation proves the granular `AddHexalithFrontComposer(...)` path is safer. Do not register a second Fluxor store.
  - [x] Add `AddHexalithDomain<TMarker>()` after quickstart. Prefer a stable ChatBot-owned marker type such as `ChatBotUiFrontComposerMarker` under `src/Hexalith.ChatBot.UI/Registration/` instead of relying on generated or internal types.
  - [x] Add `AddHexalithEventStore(...)` last. Resolve its base address from configuration/service discovery, keeping the existing `ResolveChatBotBaseAddress(...)` for `IChatBotClient`; do not redirect existing governed-operation services away from the typed ChatBot client unless a generated FrontComposer command descriptor explicitly owns that flow.
  - [x] Keep `AddLocalization()`, request localization, Razor components, `IChatBotClient`, and existing scoped UI services working.

- [x] Collapse layout into `FrontComposerShell` and remove duplicate providers (AC: 3)
  - [x] Update `src/Hexalith.ChatBot.UI/Components/_Imports.razor` with the FrontComposer shell layout namespace needed by Razor.
  - [x] Replace the custom header/skip-link/main wrapper in `Components/Layout/MainLayout.razor` with the shell wrapper around `@Body`. Use `AppTitle="Hexalith ChatBot"` so the product name remains visible.
  - [x] Remove `<FluentProviders />` from `Components/App.razor`; `FrontComposerShell` owns it.
  - [x] Do not add `<Fluxor.Blazor.Web.StoreInitializer />` anywhere in ChatBot UI; FrontComposerShell owns it.
  - [x] Preserve `Routes.razor`, current page routes, and existing M0/M1/M2 surfaces. Surface migration belongs to Stories 10.2 and 10.3.

- [x] Reconcile the Story 1.14 token bridge with shell ownership (AC: 4)
  - [x] Inspect `wwwroot/css/chatbot.tokens.css` after shell render assumptions change.
  - [x] Remove the temporary-bridge language from the file header or replace it with a comment that only describes ChatBot-owned semantic aliases still required by existing components.
  - [x] Keep any remaining ChatBot aliases mapped to Fluent/FrontComposer CSS custom properties only; no raw hex/rgb/hsl semantic color mappings.
  - [x] Keep forced-colors, non-color status cues, responsive/touch constants, and localization/accessibility classes that existing components still consume.

- [x] Strengthen tests for shell integration and adapter boundaries (AC: 1, 3, 4, 5)
  - [x] Update `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` so `ChatBotUiAdapterMustDependOnlyOnClientFacadeAndNeverServerInternals` permits the FrontComposer Shell reference and still forbids Server/gateway/DAPR/audit/idempotency references.
  - [x] Keep IL-level fitness tests non-vacuous: `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs` must still prove every adapter assembly does not depend on `Hexalith.ChatBot.Server`.
  - [x] Add or update UI contract tests so `MainLayout.razor` contains `FrontComposerShell`, `App.razor` has no `<FluentProviders />`, and no ChatBot-owned Fluxor initializer appears.
  - [x] Update the existing semantic-token test that currently expects one provider in `App.razor`; after this story it should assert provider ownership moved to FrontComposerShell, not duplicate providers in ChatBot.
  - [x] Add a negative check for raw semantic colors or duplicate provider markup if the current test coverage does not already catch it.

- [x] Verify build and focused regression gates (AC: 5)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-build`.
  - [x] Run `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-build`.
  - [x] If startup wiring, service discovery, or AppHost config changes are needed to supply EventStore shell options, also run the relevant AppHost/Aspire tests.
  - [x] Record exact commands, pass/fail counts, and skipped checks in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Epic 10 is an M2 release-readiness closure epic. It is not optional or an appendix; it closes the documented gap between the product name/vision and the earlier M0 read-only conversation/review surfaces. Story 10.1 specifically closes the Story 1.14 deferred shell swap. [Source: _bmad-output/planning-artifacts/epics.md#Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption]

The safety model does not change in this story. The future governed chat composer remains a write surface on the existing CommandGateway spine; risky requests become Epic 4 AI-action proposals. Story 10.1 only installs the UI composition layer required before later surface migration and composer stories. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Vision (Future)]

Story 10.2 migrates S1/S2/S3, Story 10.3 migrates S8/S9/S10, Story 10.4 moves `/` to the Project Workspace, Story 10.5 builds the governed composer, Story 10.6a records the streaming transport ADR, Story 10.6b implements streaming + Stop/Cancel, and Story 10.7 re-verifies a11y/visual/parity. Do not pull those scopes into 10.1. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.2; _bmad-output/planning-artifacts/epics.md#Story 10.7]

### Current Implementation State

Current files likely to be updated:

- `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj` currently references only ChatBot Client and ServiceDefaults plus Fluent UI and Fluxor packages.
- `src/Hexalith.ChatBot.UI/Program.cs` currently registers `AddFluentUIComponents()` and `AddFluxor(...)` directly, then the generated `IClient`, `IChatBotClient`, and scoped UI services.
- `src/Hexalith.ChatBot.UI/Components/App.razor` currently registers `css/chatbot.tokens.css` and renders `<FluentProviders />` directly.
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` currently owns a custom header, skip link, and main wrapper.
- `src/Hexalith.ChatBot.UI/Components/_Imports.razor` currently imports ChatBot layout/components but not FrontComposer shell layout.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` still identifies itself as the temporary Story 1.14 inheritance bridge.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` currently expects UI ProjectReferences to be exactly Client + ServiceDefaults, so it must be updated with the new allowed FrontComposer reference.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` currently expects `<FluentProviders />` in `App.razor`, so it must be updated to the new provider ownership.

### FrontComposer Integration Intelligence

`FrontComposerShell` is the framework-owned shell composition point. Adopters wrap `@Body` inside it from their `MainLayout.razor`. It renders `FluentLayout`, skip links, optional navigation, theme/density watchers, projection and pending-command summaries, `StoreInitializer`, and exactly one `<FluentProviders />`. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor; Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs]

`AddHexalithFrontComposerQuickstart(...)` chains localization, shell localization, authorization core, Fluxor, storage, registry, command/query stubs, shell state/effects, and related services. It accepts a Fluxor configuration callback; use that callback to scan the ChatBot UI assembly instead of calling `AddFluxor(...)` separately. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs#AddHexalithFrontComposerQuickstart]

FrontComposer validates bootstrap order with markers. Required order is quickstart/foundational call first, optional domain registration second, optional EventStore swap last. Mis-ordering throws a named `InvalidOperationException` at host start. This story should rely on that guard and add ChatBot tests that catch obvious source-level mis-ordering before runtime. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapMarkers.cs; Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidator.cs]

`AddHexalithEventStore(...)` swaps FrontComposer's stub command/query clients for EventStore-backed clients and wires projection subscription/pending-command state. It requires an absolute `EventStoreOptions.BaseAddress`, configures named HTTP clients, and replaces the stub command service. Keep this separate from the existing ChatBot `IChatBotClient` base address, because current ChatBot UI services submit through the typed ChatBot client facade. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs]

### Previous Story Intelligence

Story 1.14 deliberately shipped a temporary ChatBot token alias bridge because the shell wrapper had not landed. That story's own acceptance criteria required either wrapping in FrontComposer where possible or documenting the alias layer as temporary until the shell wrapper lands. Story 10.1 is the explicit shell-wrapper story, so the temporary-bridge language must not survive unchanged. [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md#Acceptance Criteria]

Story 1.14 also established non-vacuous tests for semantic tokens, forced-colors behavior, and duplicate/raw color prevention. Preserve those tests as guardrails; update assertions only where ownership moves from ChatBot to FrontComposer Shell. [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md#Testing Requirements; tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs]

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- Fluent UI v5 is pinned to `5.0.0-rc.3-26138.1` in ChatBot and FrontComposer; do not upgrade or add inline package versions. [Source: Directory.Packages.props; Hexalith.FrontComposer/_bmad-output/project-context.md#Technology Stack & Versions]
- UI may reference Client, ServiceDefaults, and FrontComposer Shell/Contracts only. It must not reference Server, gateway internals, DAPR clients, audit/idempotency seams, or projection stores. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.1; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- The product posture remains a quiet operational SaaS command workspace, not a playful assistant, marketing landing page, or ungoverned chatbot. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md#Scope; _bmad-output/planning-artifacts/epics.md#UX Design Requirements]
- Root submodule policy applies: initialize/update only root-level submodules declared in `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md; .gitmodules; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs#CiShouldInitializeOnlyRootSubmodulesNonRecursively]

### Latest Technical Notes

No dependency upgrade is required or desired for this story. The relevant technical facts are local and pinned: .NET SDK `10.0.300`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and FrontComposer Shell consumed from the root-level submodule. Treat external "latest" package availability as irrelevant unless a separate story authorizes version churn. [Source: Directory.Packages.props; Hexalith.FrontComposer/_bmad-output/project-context.md#Technology Stack & Versions]

### Suggested Implementation Shape

Prefer this shape:

```csharp
_ = builder.Services.AddHexalithFrontComposerQuickstart(static options =>
    options.ScanAssemblies(typeof(Program).Assembly));
_ = builder.Services.AddHexalithDomain<ChatBotUiFrontComposerMarker>();
_ = builder.Services.AddHexalithEventStore(options =>
    options.BaseAddress = ResolveEventStoreBaseAddress(builder.Configuration));
```

Keep the existing ChatBot client registration:

```csharp
_ = builder.Services.AddHttpClient<IClient, Client>(static (provider, http) =>
    http.BaseAddress = ResolveChatBotBaseAddress(provider.GetRequiredService<IConfiguration>()));
_ = builder.Services.AddScoped<IChatBotClient, ChatBotClient>();
```

Add a dedicated `ResolveEventStoreBaseAddress(...)` helper if needed. Prefer explicit configuration keys such as `EventStore:BaseAddress`, `services:eventstore:https:0`, or `services:eventstore:http:0` based on existing Aspire/service-discovery conventions in this repo. Do not overload `ResolveChatBotBaseAddress(...)` for EventStore.

Layout target:

```razor
@namespace Hexalith.ChatBot.UI.Components.Layout
@inherits LayoutComponentBase

<FrontComposerShell AppTitle="Hexalith ChatBot">
    @Body
</FrontComposerShell>
```

If navigation/sidebar auto-population renders unintended empty shell navigation because a domain manifest is registered before actual shell surface migration, use the documented FrontComposer escape hatch with an explicit empty `Navigation` fragment. Do not hand-roll the old shell around `FrontComposerShell`.

### Testing Requirements

- Use xUnit v3 and Shouldly; no new assertion library.
- Prefer source/XML tests for bootstrap/order/provider ownership and boundary references. These are deterministic and catch story-specific regressions cheaply.
- Keep architecture tests explicit about the new allowed FrontComposer reference and the still-forbidden ChatBot Server/data-plane references.
- If DI tests are added, build a host/service provider enough to prove FrontComposer services resolve without duplicating Fluxor providers. Avoid live EventStore calls in unit tests; options wiring can be asserted by service registration/source tests unless integration tests are explicitly added.
- Run broader AppHost/Aspire validation only if the implementation changes service discovery, `appsettings`, AppHost wiring, or runtime topology.

### Out of Scope

- Migrating S1 conversation, S2 association review, S3 AI approval, S8 dashboards, S9 audit, or S10 admin queues onto the shell beyond being renderable inside `@Body`.
- Changing the default `/` route to Project Workspace.
- Implementing the governed chat composer, ask-AI behavior, streaming transport ADR, streaming response rendering, or Stop/Cancel.
- Modifying `Hexalith.FrontComposer` submodule files.
- Upgrading Fluent UI, Fluxor, FrontComposer, .NET, xUnit, Playwright, Dapr, Aspire, or any other package.
- Adding a second design system, raw color palette, custom component library, or ungoverned/freeform chat textbox.

### Project Structure Notes

- UI project file: `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`
- UI startup: `src/Hexalith.ChatBot.UI/Program.cs`
- Layout: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`
- App HTML shell: `src/Hexalith.ChatBot.UI/Components/App.razor`
- Razor imports: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`
- Token stylesheet: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- Suggested marker: `src/Hexalith.ChatBot.UI/Registration/ChatBotUiFrontComposerMarker.cs`
- Architecture tests: `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` and `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/`
- UI contract tests: `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`
- FrontComposer shell reference: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md#Discover Inputs Protocol]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 10.1: FrontComposer Shell integration]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Vision (Future)]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md]
- [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapMarkers.cs]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidator.cs]
- [Source: src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj]
- [Source: src/Hexalith.ChatBot.UI/Program.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/App.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/_Imports.razor]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs]
- [Source: Directory.Packages.props]
- [Source: .gitmodules]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- 2026-06-11T17:09:06+02:00 - Added failing source/XML tests for FrontComposer shell ownership and adapter references; attempted focused test runs before implementation, but vstest aborted before executing tests because local socket creation is denied in this sandbox.
- 2026-06-11T17:09:06+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-11T17:09:06+02:00 - `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-build` aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` from vstest `SocketServer.Start`.
- 2026-06-11T17:09:06+02:00 - `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-build` aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` from vstest `SocketServer.Start`.
- 2026-06-11T17:09:06+02:00 - Used a disposable `/tmp` xUnit v3 in-process runner because vstest socket creation is denied; `Hexalith.ChatBot.UI.Tests` passed with 133 total, 0 failed, 0 skipped.
- 2026-06-11T17:09:06+02:00 - Used the same disposable in-process runner from the architecture test output directory; `Hexalith.ChatBot.Architecture.Tests` passed with 41 total, 0 failed, 0 skipped.
- 2026-06-11T17:09:06+02:00 - Final `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-11T17:09:06+02:00 - Final `dotnet build Hexalith.ChatBot.slnx -c Release --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added the root-level FrontComposer Shell project reference to the ChatBot UI without package version changes or submodule edits.
- Replaced app-owned Fluent UI/Fluxor startup ownership with `AddHexalithFrontComposerQuickstart(...)`, followed by `AddHexalithDomain<ChatBotUiFrontComposerMarker>()` and `AddHexalithEventStore(...)`.
- Collapsed `MainLayout.razor` to `FrontComposerShell AppTitle="Hexalith ChatBot"` and removed the duplicate app-owned `<FluentProviders />`.
- Reframed `chatbot.tokens.css` as ChatBot-owned semantic aliases over Fluent/FrontComposer variables and preserved the existing accessibility/responsive/status tokens.
- Updated UI and architecture contract tests for shell ownership, bootstrap order, adapter boundaries, and token-bridge wording.
- Updated the accessibility focus contract to stop pinning ChatBot-owned skip-link markup now owned by `FrontComposerShell`.

### File List

- _bmad-output/implementation-artifacts/10-1-frontcomposer-shell-integration.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.UI/Components/App.razor
- src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor
- src/Hexalith.ChatBot.UI/Components/_Imports.razor
- src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj
- src/Hexalith.ChatBot.UI/Program.cs
- src/Hexalith.ChatBot.UI/Registration/ChatBotUiFrontComposerMarker.cs
- src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css
- tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/FrontComposerShellIntegrationE2ETests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/NotificationRoutingEditorE2ETests.cs

### Change Log

- 2026-06-11 - Implemented FrontComposer Shell integration for ChatBot UI and moved story to review.
- 2026-06-11 - Senior Developer Review (AI, auto-fix): fixed a browser-path test defect and an incomplete File List; re-verified build + focused lanes; moved story to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-06-11
**Outcome:** Approve (after auto-fixes)

### Verification performed

- `dotnet build Hexalith.ChatBot.slnx -c Release --no-restore -m:1 /nr:false` → **Build succeeded, 0 Warning(s), 0 Error(s)** (AC5 Release-clean, warnings-as-errors confirmed).
- `Hexalith.ChatBot.UI.Tests` (ran the xUnit v3 executable directly to bypass the vstest socket restriction the dev hit) → **133 passed, 0 failed, 0 skipped**.
- `Hexalith.ChatBot.Architecture.Tests` → **41 passed, 0 failed, 0 skipped**.
- `FrontComposerShellIntegrationE2ETests` with a real Chromium browser present → **3 passed, 0 failed** (after the fix below).
- AC1 boundary verified at the dependency level: `Hexalith.FrontComposer.Shell` transitively references only `Hexalith.FrontComposer.Contracts` + packages (Fluxor, Fluent UI, OpenIdConnect, SignalR.Client, NUlid, System.Reactive) — no Server, Dapr, gateway, audit, idempotency, or projection-store edge. `EveryAdapterAssemblyDoesNotDependOnServer` IL fitness test still green.
- AC2 bootstrap order (`AddHexalithFrontComposerQuickstart` → `AddHexalithDomain<ChatBotUiFrontComposerMarker>` → `AddHexalithEventStore`) confirmed against the submodule extension methods, which exist as used; EventStore base address resolved from config with an absolute fallback.
- AC3 confirmed: `App.razor` no longer renders `<FluentProviders />`, layout collapses to `<FrontComposerShell AppTitle="Hexalith ChatBot">@Body</FrontComposerShell>`, no duplicate `StoreInitializer`, and `chatbot-main-content` is fully removed from source. `chatbot.focus.js` is still consumed by `ChatBotStreamingStopControl.razor`, so it was correctly left in place.

### Findings and resolutions

1. **[CRITICAL — fixed] Broken browser-path test shipped under a `[x]` task.** `FrontComposerShellIntegrationE2ETests.TokenAliasLayerShouldRemainThinOverFrontComposerAndFluentVariables` asserted that `getComputedStyle().getPropertyValue("--chatbot-color-info-background")` *contains* the literal string `var(--colorStatusInformationBackground1)`. The browser resolves the `var()` chain, so the computed value is the source color (`#eff6fc`), failing both the `ShouldContain("var(...)")` and `ShouldNotContain("#")` assertions. It passed only in the no-browser fallback the dev's sandbox forced, so it would break CI on any machine with Chrome. Fixed by asserting thin-alias equivalence at computed-value time — each `--chatbot-color-*` slot must compute to exactly its Fluent/FrontComposer source variable (with a non-empty guard to avoid a vacuous empty==empty pass). The raw-text "no `#`/`rgb`/`hsl` literal in source" guarantee remains enforced by `AssertTokenAliasLayerWithoutBrowser` and `ChatBotSemanticTokenContractTests`. Re-ran with a browser → 3/3 pass.
2. **[MEDIUM — fixed] Incomplete File List.** Four source test files changed by this story were absent from the Dev Agent Record File List: `FrontComposerShellIntegrationE2ETests.cs` (new), and the modified `GovernedOperationsVisualFoundationE2ETests.cs`, `EscalationPolicyEditorE2ETests.cs`, and `NotificationRoutingEditorE2ETests.cs`. Added them.

### Reviewed and deliberately left as-is

- The old-shell CSS classes (`.chatbot-layout`, `.chatbot-skip-link`, `.chatbot-shell-header`, `.chatbot-shell-brand`, `.chatbot-shell-main`) look orphaned after the layout collapse, but `GovernedOperationsVisualFoundationE2ETests` still builds fixtures using them and asserts their computed styling. They remain part of the validated token contract; removing them would break the E2E suite and belongs to the 10.2/10.3 surface-migration scope, not 10.1.
