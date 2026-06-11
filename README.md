# Hexalith.ChatBot

Hexalith.ChatBot is the governed email-to-project orchestration module for the Hexalith platform. Epic 1 establishes the command spine, contract-first client, Aspire/DAPR topology, first governed UI command, and safety-floor test harnesses. Epic 2 adds Microsoft 365 mailbox intake, participant resolution, deterministic association scoring, S2 association review, decision/correction history, correction-propagation metadata, and duplicate/retry/failure status foundations. Epic 3 adds the S1 project-conversation projection, email/participant/attachment/decision/approval/failure/AI-outcome rendering, the "why this project" evidence panel, attachment capture and safety states, and an auditable AI-context package manifest for authorized files. Epic 4 adds governed AI action mediation: task-intent capture and review, deterministic AI-action risk classification, low-risk assistance routing, S3 approval records and decisions, metadata-only preview/inspection, M0 allowlisted approved execution, safe refusal cataloging, and corrected-context invalidation for stale AI proposals. Epic 5 adds service-client identities and scoped grants, the production CLI adapter, the governed MCP tool surface, and real UI/API + CLI + MCP differential-conformance verification over the shared command spine. Epic 6 adds governed outbound communication and inbound authenticity: sender-authority classification, local outbound draft creation, outbound approval and single-shot send records, provider authenticity/header passthrough, delegated-sender disambiguation, and external-sender strictness routing. Epic 7 adds tenant administration and governance policy: a bounded tenant-admin permission model with see-only vs operate scopes and finer roles (mailbox/policy/compliance/operations admin), the versioned Tenant Policy Schema editor (S5) with a two-person rule on security-sensitive knobs, per-action-class AI action policy, operational queue management, notification routing/escalation/throttling/backlog/rubber-stamp observability, a shared governance control floor (disable/quarantine/rate-limit across mailbox sources, service clients, AI actors, command capabilities, and outbound channels), and command allowlist v1 under change control with the completed lifecycle state matrix including the `Skipped` terminal state. Epic 8 adds the operational-observability and runtime-activation layer: read-only operational dashboards (S8/S10) over six FR67 health views with stable `healthy|degraded|failed|unknown` status enums and NFR6 freshness, always-on OpenTelemetry emission for seven FR94 operation classes via a single `Hexalith.ChatBot` meter with gap-detection, a published SLO catalog with error budgets and coarse burn (NFR42a), tenant-safe default alert wiring with fail-closed pre-commit audit and metadata-only payloads (NFR43/NFR2), degraded-state operability with narrowest-scope incident isolation and runbook-real per-item diagnostics (NFR41/NFR42/NFR44), hosted Dapr Workflow binding for correction propagation, projection-backed control-state/rate-limit enforcement for the server runtime seams, and a single periodic enforcement runtime for the deferred evaluator set. Epic 9 adds the tamper-evident audit, compliance-investigation, and recovery-validation capstone: a per-tenant append-only WORM hash-chained audit with GDPR-safe erasure by appended redaction record + key-shred + projection tombstone (never chain mutation), audit completeness measured as reconstructability (rolling-7-day, per-tenant, ≥99.5%) rather than field presence, the S9 compliance investigation surface (query and reconstruct with per-project redaction and read/escalate-only authority), replay/simulation isolation under a dedicated test tenant via a structurally-threaded `ReplayRunId`, derived-store (vector/embedding/prompt-context/candidate-ranking) cross-tenant isolation by physical per-tenant partitioning, correction-driven vector reindexing, the data-class inventory/retention/export/deletion/erasure/consent governance workflows, and the continuity-drill / projection-rebuild / scoped-outage recovery validation harnesses (NFR49a/50a, NFR9a, FR54/56, FR95a, NFR52–59).

## Local Setup

Initialize only root-level submodules:

```bash
git submodule update --init
```

Restore and build:

```bash
dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false
dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false
```

In this sandbox, `dotnet test` can fail because the VSTest runner opens a denied socket. Story validation uses the compiled xUnit v3 binaries directly, for example:

```bash
tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor
```

## Aspire and DAPR

The AppHost wires ChatBot with EventStore, Tenants, Keycloak, and DAPR sidecars.

Runtime component names are intentional:

- `statestore` is the canonical EventStore actor/status/archive/checkpoint store.
- `chatbot-statestore` is ChatBot's derived read-model and coarse-idempotency store.
- `chatbot-workflow-statestore` is ChatBot's hosted Dapr Workflow state store.
- `chatbot-pubsub` is the Redis pub/sub component carrying governed events.

The local self-hosted Aspire topology loads `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml` because mTLS is disabled. Production must use the deny-by-default `accesscontrol.yaml` with mTLS/Sentry enabled.

Epic 2 correction propagation now has hosted Dapr Workflow runtime binding for the live topology. `AddChatBotCorrectionPropagationWorkflow()` registers the workflow and activities, `Program.cs` enables it when `ChatBot:UseDaprWorkflowRuntime=true`, and the AppHost sets that flag for the ChatBot service. EventStore remains the lifecycle source of truth; Dapr Workflow coordinates the saga and must not be described as a replacement aggregate or sibling-context mutation path.

Epic 3 project conversation context is currently a ChatBot-owned read projection in `chatbot-statestore`, exposed through the contract spine and consumed by the S1 Blazor surface. Do not describe it as a `Hexalith.Conversations` adapter path unless that adapter is added in code.

Epic 4 AI mediation is currently implemented inside the ChatBot command spine. `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate` are the registered gateway stages, `ExecuteLowRiskAIAssistance` uses the scoped context-package contract, and approved M0 execution is constrained by `ai-action-command-allowlist.m0` to `Project.AppendConversationMessage`. The current conversation writer is metadata-only; do not describe it as a durable sibling `Hexalith.Conversations` write until that binding exists in code.

Epic 5 CLI and MCP surfaces are implemented as thin adapters over `Hexalith.ChatBot.Client`. The CLI uses `System.CommandLine`, the MCP server uses `ModelContextProtocol`, and both submit typed commands through `IChatBotClient.SubmitAsync` with their surface origin. They must not be described as direct DAPR/EventStore/projection clients or as local authorization/governance layers.

The Epic 5 differential-conformance harness now drives the production CLI and MCP adapter translation paths plus the UI/API client seam. Do not describe current parity verification as only M0 CLI/MCP shims.

Epic 6 outbound and authenticity behavior is implemented inside the ChatBot command spine. `SenderAuthorityClassifier`, `OutboundDraftAuthorityEvaluator`, and `OutboundSendAuthorityEvaluator` own sender-authority decisions; outbound drafts are ChatBot-local records; outbound sends require the outbound approval commands and single-shot `outbound-send` idempotency before the outbound mailbox sender seam is reached. Inbound Microsoft 365 authenticity is provider passthrough only: ChatBot records selected SPF/DKIM/DMARC/composite-authentication/header metadata, delegated-sender posture, and external-sender strictness signals without re-verifying DNS or trusting raw headers over provider identity.

Epic 7 tenant administration and governance run inside the ChatBot command spine. `AdminAuthorityEvaluator` owns the bounded admin-scope model (`AdminScope`/`AdminRole`), and the shared `GovernedOperationAggregate` drives the two-person submit→approve pattern for policy changes and the disable/quarantine/rate-limit control floor. The control-floor enforcement seams differ by subject (worker intake for mailbox sources, the service-client grant validator for service clients and AI actors, the top of `ParticipantAuthorizationStage` for command capabilities, and the send seam in `AcceptedCommandDispatcher` for outbound channels). The command allowlist is always widened last, after validation/authorization/audit, and Epic 7 admin operations keep audit metadata-only. Most Epic 7 admin surfaces (queues, notification routing, escalation, throttling, backlog, rubber-stamp) ride the existing generic command-submission transport and add no new public HTTP paths; only the admin/policy and control-floor command DTOs were added to the OpenAPI component schemas.

Epic 7 control-state and rate-limit enforcement has been activated for the server runtime seams by Epic 8.7a/8.7b. `CommandGatewayServiceCollectionExtensions` now registers projection-backed service-client, AI-actor, command-capability, and outbound-channel control/rate-limit providers, and the architecture tests guard against reintroducing `AlwaysActive…` or `AlwaysUnlimited…` providers on that path. The mailbox worker path still has only the projection-backed provider foundation and static stubs; do not describe mailbox-source runtime enforcement as fully live until a hosted worker path consumes `GovernedControlStateView`.

Epic 8 observability emission is always-on, and the deferred evaluator set now has one owned periodic runtime. The `Hexalith.ChatBot` meter and its seven instruments are registered into the `ServiceDefaults` metrics pipeline. `Program.cs` enables `AddChatBotPeriodicEnforcementHostedService()` when `ChatBot:UsePeriodicEnforcementRuntime=true`, and the AppHost sets that flag. The periodic coordinator drives escalation, notification throttling, reviewer backlog, rubber-stamp, operational alerts, weekly runbook sampling, audit completeness publication, audit-projection-lag publication, and control-state freshness heartbeats. Remaining caveats are explicit: the S8 UI still assembles a fail-safe all-`Unknown` placeholder until a dashboard read endpoint is wired, and `IAuditProjectionLagSource` is swapped to `CheckpointBackedAuditProjectionLagSource` only to publish measured checkpoints when available; the default checkpoint source still returns no readings because governed-operation projection positions are not exposed yet. Do not describe audit lag as live-measured until that checkpoint feed is backed by real positions.

Epic 9 follows the same build-the-logic / defer-selected-live-drivers pattern. The WORM audit chain append rides the existing commit seam, and Story 9.2 audit completeness now has a periodic publication path through the Epic 8.7b runtime. The nightly chain verifier, replay-isolation and derived-store-isolation probes, correction-reindex SLO-deadline sweep, and continuity-drill / projection-rebuild / scoped-outage validation harnesses (9.11–9.13) remain coordinator/release-gate seams without live fault-injection / rebuild / Hexalith.Memories vector drivers (`IContinuityDrillScenarioRunner`, `IProjectionRebuildDriver`, `IScopedOutageInjectionDriver`, and the live `IDerivedStore` binding are deferred or in-memory by default). The NFR56–59 recovery targets (RPO ≤ 15 min, RTO ≤ 4 hr) remain A10 [ASSUMPTION] values not yet recalibrated against a live drill. Unlike Epics 7 and 8, Epic 9 added new public HTTP paths: Story 9.3 wired `POST /api/v1/compliance/audit/search` and `GET /api/v1/compliance/audit/{auditRecordRef}` (the S9 investigation surface) over the existing OpenAPI compliance schemas, widening the audit `filterKey` set with `message-id` and `surface`. Those paths and filter keys are now reflected in `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; the client still carries the hand-written `ComplianceAuditTransport`, so a contract-first client regeneration remains the follow-up (Epic 9 retrospective action item #6).

Run the AppHost after local DAPR/Redis prerequisites are available:

```bash
dotnet run --project src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj
```
