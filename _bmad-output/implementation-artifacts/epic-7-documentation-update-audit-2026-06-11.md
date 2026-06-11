# Epic 7 Documentation Update Audit - 2026-06-11

**Project:** Chatbot
**Epic:** 7 - Tenant Administration and Governance Policy
**Mode:** Autonomous verification after retrospective re-run
**Result:** No documentation updates applied. Proposed updates were discarded where current docs matched implementation evidence.

## Verification Method

This audit followed the post-retrospective instruction to list potentially stale docs, read current doc content, compare against implementation code, and update only verified discrepancies.

Implementation evidence checked:

- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/*ControlContracts.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- Epic 7 story files in `_bmad-output/implementation-artifacts/7-*.md`
- Sprint status in `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Proposed Documentation Targets

### 1. `README.md`

**Why it was proposed:** README files often drift after epic implementation, especially around implemented-vs-deferred runtime behavior.

**Current doc content verified:** The README already describes Epic 7 tenant administration, bounded admin scopes, the Tenant Policy Schema, the shared control floor, command allowlist v1, and the `Skipped` lifecycle state. It also explicitly states that Epic 7 control-state/rate-limit enforcement is wired but inert by default through `AlwaysActive...` and `AlwaysUnlimited...` providers, and that notification/escalation/throttle/backlog/rubber-stamp evaluators have no periodic runtime trigger yet.

**Code comparison:** `CommandGatewayServiceCollectionExtensions` still registers:

- `AlwaysActiveServiceClientControlStateProvider`
- `AlwaysActiveAiActorControlStateProvider`
- `AlwaysActiveCommandCapabilityControlStateProvider`
- `AlwaysActiveOutboundChannelControlStateProvider`
- `AlwaysUnlimitedServiceClientRateLimitProvider`
- `AlwaysUnlimitedAiActorRateLimitProvider`
- `AlwaysUnlimitedCommandCapabilityRateLimitProvider`
- `AlwaysUnlimitedOutboundChannelRateLimitProvider`

The same file registers evaluator coordinators but no always-on production trigger for the Epic 7 evaluator loop.

**Decision:** No update needed. Current README matches code.

### 2. `_bmad-output/planning-artifacts/architecture.md`

**Why it was proposed:** Architecture decisions changed during Epic 7 from "control floor active" wording to a more precise "wired but inert until runtime activation" model.

**Current doc content verified:** The architecture now records that Epic 7 lands the admin/governance breadth and shared control floor, while control-state and rate-limit enforcement read from inert defaults until Stories 8.7a/8.7b materialize durable projection and periodic trigger ownership.

**Code comparison:** The runtime still uses inert defaults and does not register a production trigger for the deferred evaluator set. That matches the architecture.

**Decision:** No update needed. Current architecture matches implementation and planning ownership.

### 3. `_bmad-output/planning-artifacts/epics.md`

**Why it was proposed:** Epic requirements can drift when implementation splits parent stories or moves deferred runtime work into later epics.

**Current doc content verified:** The FR74/FR75 lines state that runtime enforcement activation is owned by Stories 8.7a/8.7b. Individual Epic 7 control-floor stories carry runtime-activation notes, and Epic 8 includes the control-plane activation parent plus split child stories 8.7a and 8.7b.

**Code comparison:** Epic 7 command/control schemas, aggregate behavior, and enforcement seams exist; durable provider replacement and periodic trigger do not. That matches the story split and runtime-activation notes.

**Decision:** No update needed. Current epics planning reflects actual implementation status.

### 4. `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

**Why it was proposed:** PRD wording can overstate implementation or claim ChatBot re-verifies provider authenticity.

**Current doc content verified:** The PRD defines FR74/FR75 requirements and FR75a-g admin scopes. It also states provider-supplied SPF/DKIM/DMARC passthrough without ChatBot re-verification for inbound authenticity.

**Code comparison:** Epic 7 implements bounded admin roles/scopes, policy-admin schema checks, and control-floor command schemas. Runtime activation is a planning execution detail now owned by epics/architecture, not a PRD contradiction.

**Decision:** No update needed. PRD remains valid as a requirements source.

### 5. `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`

**Why it was proposed:** Addendum owns the command allowlist, tenant policy schema, and low-risk AI policy details.

**Current doc content verified:** The addendum defines `ai-action.low-risk-allowed` as a per-action-class map, requires allowlist versioning and decision-log change control, and treats security-sensitive tenant-policy changes as audited admin changes.

**Code comparison:** `TenantPolicyContracts.cs` defines `AiActionLowRiskAllowed` as an `AiActionLowRiskMap` knob, includes `allowlist.version-pin`, and marks relevant knobs as security-sensitive. The command allowlist v1 is represented by internal metadata and decision-log evidence rather than a new public HTTP path.

**Decision:** No update needed. Addendum matches code and planning.

### 6. `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

**Why it was proposed:** API documentation often diverges when command schemas are added without new paths.

**Current doc content verified:** OpenAPI still has the generic command submission path and existing read paths. It includes component schemas for the Epic 7 admin/policy/control-floor commands and enums, including mailbox-source, service-client, AI-actor, command-capability, outbound-channel, rate-limit windows, tenant-policy knobs, and `Skipped`.

**Code comparison:** Contract files contain the matching command records and enum types. Epic 7 queue/notification/escalation surfaces ride command submission and metadata-only reads; they do not require dedicated public HTTP paths.

**Decision:** No update needed. OpenAPI matches the implemented public contract surface.

### 7. `docs/adrs/*.md`

**Why it was proposed:** ADRs can become stale when implementation learns a different architectural boundary.

**Current doc content verified:** Existing ADRs mainly cover Epic 9 audit/recovery/compliance decisions. They reference the inert-control-floor pattern where relevant and do not claim Epic 7 control-state enforcement is active.

**Code comparison:** No ADR currently contradicts the Epic 7 control-floor implementation or the deferred runtime activation model.

**Decision:** No update needed. No verified discrepancy.

### 8. Configuration and Setup Documentation

**Why it was proposed:** Runtime activation can require configuration or hosting guidance.

**Current doc content verified:** README setup keeps root-level submodule initialization, Aspire/DAPR component names, and runtime caveats current. `AGENTS.md` also forbids recursive submodule initialization.

**Code comparison:** AppHost/DAPR component names and README setup remain consistent with repo configuration. No new Epic 7 runtime config exists because activation remains backlog.

**Decision:** No update needed. Configuration docs match current code.

## Discarded Proposed Updates

- Add a new README warning about inert Epic 7 control providers: discarded because README already contains the warning and names the default providers.
- Add an architecture note assigning runtime activation to Stories 8.7a/8.7b: discarded because architecture already contains the assignment.
- Add Epic 8.7a/8.7b planning entries: discarded because epics and sprint status already contain them.
- Add OpenAPI paths for queue/notification/escalation/admin surfaces: discarded because implementation uses existing generic command submission and component schemas rather than new public paths.
- Add ADR updates for Epic 7 control floor: discarded because existing ADRs do not contradict the implementation and the architecture already carries the control-plane decision.

## Outcome

No verified documentation discrepancies were found. The repository documentation now matches the Epic 7 implementation state:

- Epic 7 governance commands, aggregates, audit behavior, and control seams are implemented.
- Runtime activation remains deferred to Stories 8.7a/8.7b.
- Public API docs expose added command schemas without inventing dedicated paths.
- README and architecture correctly warn against describing the control floor as production-active before activation lands.
