# Hexalith.ChatBot

Hexalith.ChatBot is the governed email-to-project orchestration module for the Hexalith platform. Epic 1 establishes the command spine, contract-first client, Aspire/DAPR topology, first governed UI command, and safety-floor test harnesses. Epic 2 adds Microsoft 365 mailbox intake, participant resolution, deterministic association scoring, S2 association review, decision/correction history, correction-propagation metadata, and duplicate/retry/failure status foundations.

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

Run the AppHost after local DAPR/Redis prerequisites are available:

```bash
dotnet run --project src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj
```
