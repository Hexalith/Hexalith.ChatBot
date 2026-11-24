# Hexalith.ChatBot

Hexalith.ChatBot is the governed email-to-project orchestration module for the Hexalith platform. Epic 1 establishes the command spine, contract-first client, Aspire/DAPR topology, first governed UI command, and safety-floor test harnesses. Epic 2 adds Microsoft 365 mailbox intake, participant resolution, deterministic association scoring, S2 association review, decision/correction history, correction-propagation metadata, and duplicate/retry/failure status foundations. Epic 3 adds the S1 project-conversation projection, email/participant/attachment/decision/approval/failure/AI-outcome rendering, the "why this project" evidence panel, attachment capture and safety states, and an auditable AI-context package manifest for authorized files. Epic 4 adds governed AI action mediation: task-intent capture and review, deterministic AI-action risk classification, low-risk assistance routing, S3 approval records and decisions, metadata-only preview/inspection, M0 allowlisted approved execution, safe refusal cataloging, and corrected-context invalidation for stale AI proposals. Epic 5 adds service-client identities and scoped grants, the production CLI adapter, the governed MCP tool surface, and real UI/API + CLI + MCP differential-conformance verification over the shared command spine. Epic 6 adds governed outbound communication and inbound authenticity. Epic 7 adds tenant administration and governance policy. Epic 8 adds operational observability and runtime activation. Epic 9 adds tamper-evident audit, compliance investigation, and recovery validation. Epic 10 adds the governed chat surface and FrontComposer shell adoption. Epic 11 moves the Server host to `Hexalith.EventStore.DomainService`, migrates queries/projections/read models/cursors/telemetry/health to SDK contracts, and retires standalone ChatBot Aspire/ServiceDefaults packages. Epic 12 completed Fluent UI v5 component conformance remediation: the raw-control backlog and CSS primitive backlog are empty, and `ChatBotFluentConformanceTests` is the build-blocking guard. Epic 13 completed FrontComposer page-level layout-composition remediation: every routable page composes through `FcPageLayout` + `FcPageHeader` and Fluent layout/data components, the hand-rolled `.chatbot-*` page chrome and `<dl>` data dumps are gone, the sibling `ChatBotLayoutCompositionConformanceTests` guard's allowlists are empty, and the six surfaces are re-verified against the real rendered app.

## Local Setup

Initialize only root-declared submodules under `references/`:

```bash
git submodule update --init
```

Restore and build in the same package-mode Release configuration used by CI:

```bash
dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false
dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false
```

Package versions come exclusively from `references/Hexalith.Builds/Props/Directory.Packages.props`.
Local Debug builds use the pinned `hexalith-module prepare-build` workflow to select one source graph,
including Conversations. Run ordinary commands through the mapping wrapper from the repository root:

```bash
python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1
python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -m:1
python3 .github/scripts/with-source-mapping.py dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1
```

The wrapper restores the exact local tool pin, prepares the build-only declaration in
`module/build.module-manifest.v2.json`, and passes the producer's imports to the command and its children.
Machine-specific imports remain under ignored `.artifacts/source-mapping/` so detached Aspire resources
can still use them. This declaration selects build dependencies only; it does not enroll ChatBot's runtime
in Platform. The mapping keeps Conversations validation and package/source duplicate guards active.
Bare source-mode flags do not prepare a mapping. Use the same wrapper when starting the local AppHost or
running live recovery tests that may rebuild projects.

Release restore/build and release automation use published package dependencies.

The SDK version and Microsoft.Testing.Platform runner come from `global.json`. Test projects use the
xUnit runner pinned in the shared Builds catalog. Run each project individually after the Release build:

```bash
dotnet test --project tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --configuration Release --no-build --no-restore
```

Ordinary integration and UI tests require the shared Dapr and browser environment. The enabled
`after-unit-tests` action requires those prerequisites and sets the live topology flags so missing
runtime dependencies fail. Only `Hexalith.ChatBot.Recovery.Tests` discovers the destructive recovery
sweep; its harness contract and cleanup tests remain in ordinary integration. The Sunday UTC nightly
hook runs the complete recovery project with its 250-minute in-process deadline, 280-minute interrupt
and 300-minute step limit. Enable it only after the shared 330-minute job allowance has been released.
After CiCd adoption, run `python3 references/Hexalith.Builds/check.py` successfully before each push.

## Recovery claims in release notes

Before publication, the release owner sends draft recovery claims and their retained evidence
to Architecture/DevOps for sign-off. Architecture/DevOps records the release revision, artifact
locator, claimed scope and verdict in the [decision log](_bmad-output/planning-artifacts/.decision-log.md). A release may include
a sandbox result with its measured scope and limits. The following broader claims require closure
of the corresponding residual in the [recovery ADR](docs/adrs/live-recovery-validation-drivers.md).

| Claim | Required evidence |
| --- | --- |
| External Microsoft 365 or Graph disaster recovery | Close `RV-EXT-M365` with the retained, authorized Graph test-tenant drill. |
| Durable audit-chain, storage or KMS disaster recovery | Close `RV-DURABLE-WORM` with retained failure, restoration, chain verification and end-state evidence. |
| Kubernetes or production orchestration and replica coordination | Close `RV-PROD-CONTROL` with an authorized multi-replica platform drill and non-overlap evidence. |
| Production workload capacity, latency, provider coverage or isolation | Close `RV-PROVIDER-SCALE` with the approved production-shaped profile and end-state evidence. |
| Recovery or rebuild at the full target | Close `RV-MEASURABLE-CEILING` with retained full-window evidence and review the target's gate limitations. |

## Aspire and DAPR

The retained AppHost is a local-development umbrella only. It wires ChatBot with EventStore, Tenants, Keycloak, and DAPR sidecars while reusable hosting behavior stays in the EventStore DomainService SDK.

The default AppHost launch profile selects `Development`, which loads the AppHost user secrets (`hexalith-chatbot`). Configure the [mailbox secret](#local-service-credentials) before starting; external Projects/Memories endpoints and tokens are optional overrides. Start it from the repository root with `aspire run`, or use `aspire start --non-interactive` for a detached agent session. The dashboard uses `https://localhost:17278`.

Runtime component names are intentional:

- `statestore` is the canonical EventStore actor/status/archive/checkpoint store.
- `chatbot-statestore` is ChatBot's derived read-model and coarse-idempotency store.
- `chatbot-workflow-statestore` is ChatBot's hosted Dapr Workflow state store.
- `chatbot-pubsub` is the Redis pub/sub component carrying governed events.

The local self-hosted Aspire topology loads `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml` because mTLS is disabled. Production must use the deny-by-default `accesscontrol.yaml` with mTLS/Sentry enabled.

Production ChatBot deployments must configure a shared DataProtection key ring with `ChatBot:DataProtection:KeyRingPath`, or explicitly set `ChatBot:DataProtection:SingleReplicaOnly=true`. The admission marker and query cursor key ring cannot be ephemeral for restart-surviving or multi-replica claims.

Epic 2 correction propagation now has hosted Dapr Workflow runtime binding for the live topology. `AddChatBotCorrectionPropagationWorkflow()` registers the workflow and activities, `Program.cs` enables it when `ChatBot:UseDaprWorkflowRuntime=true`, and the AppHost sets that flag for the ChatBot service. EventStore remains the lifecycle source of truth; Dapr Workflow coordinates the saga and must not be described as a replacement aggregate or sibling-context mutation path.

Epic 3 project conversation context is currently a ChatBot-owned read projection in `chatbot-statestore`, exposed through the contract spine and consumed by the S1 Blazor surface. Do not describe it as a `Hexalith.Conversations` adapter path unless that adapter is added in code.

Epic 4 AI mediation is currently implemented inside the ChatBot command spine. `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate` are the registered gateway stages, `ExecuteLowRiskAIAssistance` uses the scoped context-package contract, and approved M0 execution is constrained by `ai-action-command-allowlist.m0` to `Project.AppendConversationMessage`. The current conversation writer is metadata-only; do not describe it as a durable sibling `Hexalith.Conversations` write until that binding exists in code.

The retained compatibility Epic 5 CLI and MCP surfaces are implemented as thin adapters over `Hexalith.ChatBot.Client`. The CLI uses `System.CommandLine`, the MCP server uses `ModelContextProtocol`, and both submit typed commands through `IChatBotClient.SubmitAsync` with their surface origin. They must not be described as direct DAPR/EventStore/projection clients or as local authorization/governance layers.

The Epic 5 differential-conformance harness now drives the production CLI and MCP adapter translation paths plus the UI/API client seam. Do not describe current parity verification as only M0 CLI/MCP shims.

Epic 6 outbound and authenticity behavior is implemented inside the ChatBot command spine. `SenderAuthorityClassifier`, `OutboundDraftAuthorityEvaluator`, and `OutboundSendAuthorityEvaluator` own sender-authority decisions; outbound drafts are ChatBot-local records; outbound sends require the outbound approval commands and single-shot `outbound-send` idempotency before the outbound mailbox sender seam is reached. Inbound Microsoft 365 authenticity is provider passthrough only: ChatBot records selected SPF/DKIM/DMARC/composite-authentication/header metadata, delegated-sender posture, and external-sender strictness signals without re-verifying DNS or trusting raw headers over provider identity.

Epic 7 tenant administration and governance run inside the ChatBot command spine. `AdminAuthorityEvaluator` owns the bounded admin-scope model (`AdminScope`/`AdminRole`), and the shared `GovernedOperationAggregate` drives the two-person submit→approve pattern for policy changes and the disable/quarantine/rate-limit control floor. The control-floor enforcement seams differ by subject (worker intake for mailbox sources, the service-client grant validator for service clients and AI actors, the top of `ParticipantAuthorizationStage` for command capabilities, and the send seam in `AcceptedCommandDispatcher` for outbound channels). The command allowlist is always widened last, after validation/authorization/audit, and Epic 7 admin operations keep audit metadata-only. Most Epic 7 admin surfaces (queues, notification routing, escalation, throttling, backlog, rubber-stamp) ride the existing generic command-submission transport and add no new public HTTP paths; only the admin/policy and control-floor command DTOs were added to the OpenAPI component schemas.

Epic 7 control-state and rate-limit enforcement has been activated for the server runtime seams by Epic 8.7a/8.7b. `CommandGatewayServiceCollectionExtensions` now registers projection-backed service-client, AI-actor, command-capability, and outbound-channel control/rate-limit providers, and the architecture tests guard against reintroducing `AlwaysActive…` or `AlwaysUnlimited…` providers on that path. The mailbox worker path still has only the projection-backed provider foundation and static stubs; do not describe mailbox-source runtime enforcement as fully live until a hosted worker path consumes `GovernedControlStateView`.

Epic 8 observability emission is always-on, and the deferred evaluator set now has one owned periodic runtime. The `Hexalith.ChatBot` meter and its instruments are registered by the Server host alongside `AddEventStoreDomainTelemetry("chatbot")`. `Program.cs` enables `AddChatBotPeriodicEnforcementHostedService()` when `ChatBot:UsePeriodicEnforcementRuntime=true`, and the AppHost sets that flag. The periodic coordinator drives escalation, notification throttling, reviewer backlog, rubber-stamp, operational alerts, weekly runbook sampling, audit completeness publication, audit-projection-lag publication, and control-state freshness heartbeats. Remaining caveats are explicit: the S8 UI still assembles a fail-safe all-`Unknown` placeholder until a dashboard read endpoint is wired, and `IAuditProjectionLagSource` is swapped to `CheckpointBackedAuditProjectionLagSource` only to publish measured checkpoints when available; the default checkpoint source still returns no readings because governed-operation projection positions are not exposed yet. Do not describe audit lag as live-measured until that checkpoint feed is backed by real positions.

Epic 9 follows the same build-the-logic / defer-selected-live-drivers pattern. The WORM audit chain append rides the existing commit seam, and Story 9.2 audit completeness now has a periodic publication path through the Epic 8.7b runtime. The nightly chain verifier, replay-isolation and derived-store-isolation probes, correction-reindex SLO-deadline sweep, and continuity-drill / projection-rebuild / scoped-outage validation harnesses (9.11–9.13) remain coordinator/release-gate seams without live fault-injection / rebuild / Hexalith.Memories vector drivers (`IContinuityDrillScenarioRunner`, `IProjectionRebuildDriver`, `IScopedOutageInjectionDriver`, and the live `IDerivedStore` binding are deferred or in-memory by default). The NFR56–59 recovery targets (RPO ≤ 15 min, RTO ≤ 4 hr) remain A10 [ASSUMPTION] values not yet recalibrated against a live drill. Unlike Epics 7 and 8, Epic 9 added new public HTTP paths: Story 9.3 wired `POST /api/v1/compliance/audit/search` and `GET /api/v1/compliance/audit/{auditRecordRef}` (the S9 investigation surface) over the existing OpenAPI compliance schemas, widening the audit `filterKey` set with `message-id` and `surface`. Those paths and filter keys are now reflected in `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; the client still carries the hand-written `ComplianceAuditTransport`, so a contract-first client regeneration remains the follow-up (Epic 9 retrospective action item #6).

Epic 10 adds the governed Project Workspace chat surface on the same command spine, plus FrontComposer Shell ownership for the UI. User messages use metadata-only `RecordProjectConversationMessage`; ask-AI requests route through the Epic 4 proposal path; Stop/Cancel uses `CancelAiResponseGeneration` through `IChatBotClient.SubmitAsync` and CommandGateway. AI response progress is exposed through typed project-conversation read models (`AiResponseProgress`) and nudged by a ChatBot-owned metadata-only SignalR hub at `/hubs/chatbot/project-conversation-changes`. The hub is enabled by `ChatBot:ProjectionChangeNotifications:Enabled=true`; the AppHost sets this for local runs. SignalR carries only a tenant-scoped "project conversation changed" signal, never response text, raw provider chunks, prompts, hidden policy detail, or exception content; the UI always re-queries typed server state before rendering progress or terminal Stop/Cancel results. Epic 10 also surfaced a UX-DR1/UX-DR2 component-conformance gap: Shell adoption landed correctly, but interior UI components still used raw controls and a custom `chatbot.tokens.css` layer. Epic 12 closed that gap by migrating ChatBot UI controls to Fluent v5 components, retiring the custom CSS primitive layer to Fluent-backed semantic aliases plus layout/accessibility hooks, and proving the empty guard through UI, E2E, conformance, CLI, MCP, and architecture lanes. Epic 13 then closed the page-level composition gap Epics 10 and 12 left open: pages had still hand-rolled chrome with `.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar` CSS that overlapped the FrontComposer shell top bar, boxed content in a 1px border, and rendered primary data as monospace `<dl>` dumps. Each page was re-composed through `FcPageLayout` + `FcPageHeader` with Fluent layout/data components (`FluentDataGrid`, `FluentStack`, `FluentCard`, `FluentAccordion`), mirroring `Hexalith.Tenants.UI`; the retired layout CSS and the empty allowlists are enforced by the sibling `ChatBotLayoutCompositionConformanceTests` guard. Story 13.9 re-verified the six surfaces against the real rendered app (live Kestrel + Chromium), closing Story 12.9's static-fixture gap — and in doing so caught a production defect where `Components/App.razor` linked only `chatbot.tokens.css` and not the scoped `Hexalith.ChatBot.UI.styles.css` bundle, so the Fluent/FrontComposer layout grid never loaded; `App.razor` now links the scoped bundle and the real-render gate asserts `.fluent-layout` resolves to `display:grid`.

### Local service credentials

The AppHost renders the Keycloak realm on every run — including ordinary local development unrelated to Story 12.15's live-recovery lane — and fails closed if `ChatBot:LiveRecoveryValidation:MailboxClientSecret` is not configured. Supply a secret (ASCII letters/digits/`-`/`_` only, at least 32 characters) before running, e.g.:

```bash
dotnet user-secrets set ChatBot:LiveRecoveryValidation:MailboxClientSecret "REPLACE-THIS-WITH-32+-RANDOM-CHARS" --project src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj
```

Normal Development startup binds the existing Projects and Memories resources through Platform's reusable owner-service
helper. Each run enrolls separate `chatbot-projects-owner` and `chatbot-memories-owner` Keycloak clients with secret
Aspire parameters and the explicit `tenant-alpha` binding. Their only optional scopes use `platform-owner-audience.`
and `platform-owner-operation.`: Projects receives `hexalith-eventstore` / `projects:read`; Memories receives
`hexalith-memories-server` / `memories:derived-store`. These REST identities do not gain SDK workload caller access.
Owner tokens carry `tenant_id` but never `eventstore:tenant`: Projects shares EventStore's `hexalith-eventstore` audience,
and EventStore's tenant-scoped endpoints authorize on that claim alone. Projects and Memories derive the tenant from `tenant_id`.
The operation scopes identify the adapter's token requests and cache entries; they do not add receiver authorization.
Projects requires the `eventstore:permission=projects:read` mapper at its claim-transformation authorization gate.
Memories natively grants authenticated tenant membership, including access to its supported
owner API routes for that tenant, and does not enforce `eventstore:operation` or `eventstore:permission`. Projects
continues to enforce its own permissions, tenant provisioning and project access gates.
ChatBot reuses the EventStore SDK's token acquisition, cache, and refresh with separate owner options and no signing-key
fallback. Foreign destinations, redirects, insecure non-loopback HTTP and cross-tenant Project-context requests are denied.

To retain an external/static setup, configure all three `ChatBot:Projects:Endpoint`, `ChatBot:Projects:ApiToken` and
`ChatBot:Memories:ApiToken` values through AppHost user secrets or environment variables. Optionally set
`ChatBot:Memories:Endpoint`; otherwise it uses the existing Memories resource. Any partial or mixed override stops startup.
Use distinct static tokens for the two owners. Tokens must match each receiver's issuer/audience and tenant claims.
Standalone Server deployments may instead configure
`ChatBot:{Projects|Memories}:OwnerAuthentication` with `Authority`, `Issuer`, `TokenEndpoint`, `ClientId`, `ClientSecret`,
`TenantId`, `Audience` and `Operation`. The token endpoint must remain within the configured authority, and HTTP is accepted
only for Development loopback endpoints. A static Projects JWT supplies its single tenant claim; opaque tokens require
an explicit `ChatBot:Projects:TenantId` for Project-context reads. Every recognized Projects JWT tenant selector must
agree on one tenant; an override cannot narrow an ambiguous JWT. External Memories static credentials may retain
multiple tenant claims because Memories checks the requested URL tenant against that membership list.
Unbound or mismatched Project-context requests stop before network access.
Memories continues to reject anonymous requests and tenants outside its claims. Register the target tenants through Memories'
supported provisioning API before expecting derived-store probes to succeed; a ChatBot/Keycloak tenant claim does
not create an owner tenant registry entry.
Projects additionally requires the owner service-account principal in the target tenant's `Principals` registry.
Keycloak assigns that principal's `sub` randomly on every fresh realm import. Until the current run's principal is
provisioned through Projects' [managed live fixture workflow](references/Hexalith.Projects/tests/e2e/README.md#live-apphost-route)
and [production identity contract](references/Hexalith.Projects/docs/runbooks/projects-production-identity-contract.md),
default local Project-context reads remain denied with an
existence-neutral 404. Owner client enrollment creates neither principal grants nor tenant records.

The local umbrella gives Memories' existing sidecar the same `accesscontrol.local.yaml` configuration as the other
sidecars, including disabled component hot reload, and forwards `Dapr:PlacementHostAddress` and
`Dapr:SchedulerHostAddress` when configured. The required live test uses fresh task-owned control-plane/state
resources, transient service credentials scoped to its two synthetic tenants, and the owner provisioning workflow.
For this fixture, Memories' supported `EventStoreIntegration:CommandGateway:BaseAddress` points to a task-owned
loopback relay that adds a real Keycloak service bearer and forwards requests and responses to the real EventStore
API. That identity can submit only tenant lifecycle commands for the two fixture tenants; the test verifies that
EventStore rejects other tenants and domains. Local/deployed Memories gateway authentication still needs its own
configured service transport; the fixture does not expand EventStore's internal caller trust or replace the owner
command store.

The current local Memories composition does not host a `memories-tenants` domain processor. The test supplies a
loopback SDK domain transport for only the same two tenant lifecycle commands. It executes the existing owner
aggregate's `Handle` methods and reconstructs state with SDK replay and owner `Apply` methods; real EventStore
admission, actor persistence, and the supported owner provisioning workflow remain in the path. The transport
loads only the owner domain DLL beside the live Aspire Memories executable and records that exact path, version,
and SHA-256, binding the module to the live Aspire project and its explicit Release launch arguments. This is
bounded local test composition; the default AppHost and deployment still require
an authenticated owner command gateway and a hosted tenant domain processor. Passing topology evidence does not
authorize any release readiness claim.

After configuring those values and the local DAPR/Redis prerequisites, run and inspect the AppHost through Aspire:

```bash
aspire run --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj
aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive
```

### Local-development seed-credential policy

Every client, user, and secret the AppHost renders into `KeycloakRealms/hexalith-realm.json` is a **local-development seed**, not a provisioned credential. The AppHost is `IsPublishable=false` and is never a production hosting path, so:

- **The service-client grants deliberately share one expiry.** They are seeds re-minted on every non-persistent run, not credentials with independent lifecycles, so staggering them would model a rotation story this host does not own. Configure the expiry with `ChatBotServiceGrants:ExpiresAtUtc`, or its lifetime with `ChatBotServiceGrants:LifetimeDays` (default 90 days).
- **The pre-expiry gate is the guardrail.** `PrepareKeycloakRealmImport` refuses to start the topology when the grant expiry is less than `ChatBotServiceGrants:MinimumRemainingDays` (default 30) in the future — the failure mode a stale checked-in realm actually produces. The expected placeholder count is derived from the realm's own `chatbot-service-client-grant-expiry` protocol mappers, so adding a realm client cannot silently opt that client out of the gate, and a mapper that lost its placeholder fails naming the offending client id.
- **`KeycloakPersistent=true` weakens that gate.** A reused Keycloak container does not re-import the realm, so the expiry validated at startup may not be the one Keycloak is serving. The AppHost warns on stderr in this mode; remove the Keycloak container (`docker rm -f`) to force a re-import after changing the realm or the configured expiry.
- **The recovery validator is a static test fixture.** The committed `recovery-validator` user and `recovery-validator-pass` password belong only to the local sandbox realm. `RecoveryValidatorIdentity` supplies the same values to recovery token requests; they are intentionally not rotated per run. The topology contract tests verify that the user stays in the reserved recovery tenant, separate from both control tenants, and that the token request matches the realm. This documented fixture residual is not production credential or rotation evidence.
- **Production provisioning and rotation live outside the AppHost**, in deployment configuration. Do not add provisioning, rotation, or a deployment manifest to `Hexalith.ChatBot.AppHost`.

## Legacy compatibility surfaces

These are versioned compatibility binaries. They are not retired and stay outside every Hexalith.Platform composition.

- `Hexalith.ChatBot.Cli` (CLI): removal gate "ChatBot inventory gates" ([technical-tools inventory 1.0.4](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/technical-tools-migration-inventory-v1.0.4.md) PS12; [ChatBot inventory 1.1.0](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/chatbot-migration-inventory-v1.1.0.md)). Technical inventory SHA-256: `94985d72232864ab8f40ba212cd0fc73dbd0c5497856073cfd19c2972b89e8b8`. ChatBot inventory SHA-256: `3d801aed7bd80a9a6f3ec225a694ed1aa9e9a9ce7fa263745d90184af5ced49c`.
- `Hexalith.ChatBot.Mcp` (stdio MCP server): removal gate "ChatBot inventory gates" ([technical-tools inventory 1.0.4](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/technical-tools-migration-inventory-v1.0.4.md) PS13; [ChatBot inventory 1.1.0](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/chatbot-migration-inventory-v1.1.0.md)). Technical inventory SHA-256: `94985d72232864ab8f40ba212cd0fc73dbd0c5497856073cfd19c2972b89e8b8`. ChatBot inventory SHA-256: `3d801aed7bd80a9a6f3ec225a694ed1aa9e9a9ce7fa263745d90184af5ced49c`.

## Shared McpCli access (reconciliation candidate)

The module CLI and stdio MCP adapter are retained compatibility sources. ChatBot has no operation enrolled in the current shipped McpCli cohort. Eligible reads/proposals need decorated Contracts, handler/admission and enrollment proof first. Association decisions, AI-action approval, execution and confirmation-required behavior stay on confidential UI; human-delegated MCP, presence, actorType or confirm claims never grant public-agent authority.

For the verified 2.0.1 examples, install `dotnet tool install --global Hexalith.McpCli --version 2.0.1`.
Run `hexalith modules`, `hexalith operations tenants` or
`hexalith operations parties`, and `hexalith describe tenants.list-tenants`
for offline discovery of the installed cohort. Start the MCP server with
`hexalith mcp --transport stdio`; it offers five generic tools, never a tool or
CLI subcommand per operation. Configure a McpCli profile with the documented
`hexalith config --help` flow before authorized `query` or `send` submission.
Do not copy the Admin `profiles.json`. Offline Catalog discovery proves neither
Gateway authorization, live availability nor retirement. A missing operation
fails safely; confidential human actions hand off to the owning UI, never to
an alternative module endpoint. No Gateway call is needed for this setup check.

