# Hexalith.ChatBot

Hexalith.ChatBot is the governed email-to-project orchestration module for the Hexalith platform. Epic 1 establishes the command spine, contract-first client, Aspire/DAPR topology, first governed UI command, and safety-floor test harnesses. Epic 2 adds Microsoft 365 mailbox intake, participant resolution, deterministic association scoring, S2 association review, decision/correction history, correction-propagation metadata, and duplicate/retry/failure status foundations. Epic 3 adds the S1 project-conversation projection, email/participant/attachment/decision/approval/failure/AI-outcome rendering, the "why this project" evidence panel, attachment capture and safety states, and an auditable AI-context package manifest for authorized files. Epic 4 adds governed AI action mediation: task-intent capture and review, deterministic AI-action risk classification, low-risk assistance routing, S3 approval records and decisions, metadata-only preview/inspection, M0 allowlisted approved execution, safe refusal cataloging, and corrected-context invalidation for stale AI proposals. Epic 5 adds service-client identities and scoped grants, the production CLI adapter, the governed MCP tool surface, and real UI/API + CLI + MCP differential-conformance verification over the shared command spine. Epic 7 adds tenant administration and governance policy: a bounded tenant-admin permission model with see-only vs operate scopes and finer roles (mailbox/policy/compliance/operations admin), the versioned Tenant Policy Schema editor (S5) with a two-person rule on security-sensitive knobs, per-action-class AI action policy, operational queue management, notification routing/escalation/throttling/backlog/rubber-stamp observability, a shared governance control floor (disable/quarantine/rate-limit across mailbox sources, service clients, AI actors, command capabilities, and outbound channels), and command allowlist v1 under change control with the completed lifecycle state matrix including the `Skipped` terminal state.

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
- `chatbot-pubsub` is the Redis pub/sub component carrying governed events.

The local self-hosted Aspire topology loads `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml` because mTLS is disabled. Production must use the deny-by-default `accesscontrol.yaml` with mTLS/Sentry enabled.

Epic 2 correction propagation currently runs through the ChatBot server's coordinator/activity seam and durable EventStore lifecycle events. Do not describe it as hosted Dapr Workflow runtime behavior until that binding exists in code.

Epic 3 project conversation context is currently a ChatBot-owned read projection in `chatbot-statestore`, exposed through the contract spine and consumed by the S1 Blazor surface. Do not describe it as a `Hexalith.Conversations` adapter path unless that adapter is added in code.

Epic 4 AI mediation is currently implemented inside the ChatBot command spine. `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate` are the registered gateway stages, `ExecuteLowRiskAIAssistance` uses the scoped context-package contract, and approved M0 execution is constrained by `ai-action-command-allowlist.m0` to `Project.AppendConversationMessage`. The current conversation writer is metadata-only; do not describe it as a durable sibling `Hexalith.Conversations` write until that binding exists in code.

Epic 5 CLI and MCP surfaces are implemented as thin adapters over `Hexalith.ChatBot.Client`. The CLI uses `System.CommandLine`, the MCP server uses `ModelContextProtocol`, and both submit typed commands through `IChatBotClient.SubmitAsync` with their surface origin. They must not be described as direct DAPR/EventStore/projection clients or as local authorization/governance layers.

The Epic 5 differential-conformance harness now drives the production CLI and MCP adapter translation paths plus the UI/API client seam. Do not describe current parity verification as only M0 CLI/MCP shims.

Epic 7 tenant administration and governance run inside the ChatBot command spine. `AdminAuthorityEvaluator` owns the bounded admin-scope model (`AdminScope`/`AdminRole`), and the shared `GovernedOperationAggregate` drives the two-person submit→approve pattern for policy changes and the disable/quarantine/rate-limit control floor. The control-floor enforcement seams differ by subject (worker intake for mailbox sources, the service-client grant validator for service clients and AI actors, the top of `ParticipantAuthorizationStage` for command capabilities, and the send seam in `AcceptedCommandDispatcher` for outbound channels). The command allowlist is always widened last, after validation/authorization/audit, and Epic 7 admin operations keep audit metadata-only. Most Epic 7 admin surfaces (queues, notification routing, escalation, throttling, backlog, rubber-stamp) ride the existing generic command-submission transport and add no new public HTTP paths; only the admin/policy and control-floor command DTOs were added to the OpenAPI component schemas.

Epic 7 control-state and rate-limit enforcement is wired but inert by default: the gateway registers `AlwaysActive…ControlStateProvider` and `AlwaysUnlimited…RateLimitProvider` defaults, so committed `…Disabled`/`…Quarantined`/`…RateLimitConfigured` events change no production behavior until a durable read-side projection materializes a tenant's control state. Likewise the Epic 7 notification/escalation/throttle/backlog/rubber-stamp evaluators are pure and clock-injected with no periodic runtime trigger yet. Do not describe the Epic 7 control floor or notification routing as operationally active until those projections and runtime callers exist in code (expected in Epic 8).

Run the AppHost after local DAPR/Redis prerequisites are available:

```bash
dotnet run --project src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj
```
