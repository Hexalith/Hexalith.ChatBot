# Epic 7 Documentation Update Audit

**Project:** Chatbot
**Epic:** 7 - Tenant Administration & Governance Policy
**Audit mode:** Autonomous verification after retrospective
**Date:** 2026-06-03

## Verification Method

Each proposed documentation update was checked against current implementation artifacts before editing. Verification was code-grounded — proposed updates were only applied where a doc/code discrepancy was confirmed by reading the implementation, and discarded where the doc already matched code.

- Sprint status and story records: `_bmad-output/implementation-artifacts/sprint-status.yaml`, story files `7-1` through `7-27`.
- Production code (grep-verified to exist):
  - `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`, `src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs`, `AdminRole.cs`.
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`, `GovernedOperationState.cs`.
  - `src/Hexalith.ChatBot.Contracts/Enums/{MailboxSource,ServiceClient,AiActor,CommandCapability,OutboundChannel}ControlState.cs`.
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` lines 117–127 (registers `AlwaysActive…ControlStateProvider` and `AlwaysUnlimited…RateLimitProvider` defaults).
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`, `AcceptedCommandDispatcher.cs`, `ServiceClientGrantValidator.cs` (per-subject enforcement seams).
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor` (S5 editor).
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionCommandAllowlist.cs`, `AiActionCommandMetadataProvider.cs` (allowlist v1).
- Contract evidence: `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` (6 HTTP paths total; control-floor and admin/policy command DTOs present as component schemas; no new queue/notification/escalation paths).
- Documentation candidates: `README.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, OpenAPI, PRD, PRD addendum, `AGENTS.md`, `Directory.Packages.props`.

## Proposed Documentation List

1. `README.md` — project overview paragraph
   - Proposed issue: the overview enumerated Epic 1 through Epic 5 and had no Epic 7 entry, despite all 27 Epic 7 stories being `done`.
   - Code comparison: `AdminAuthorityEvaluator`, `AdminScope`/`AdminRole`, `GovernedOperationAggregate`, the five `*ControlState` enums, `ChatBotTenantPolicyEditor.razor`, and the allowlist-v1 metadata all exist in source.
   - Decision: **Update required and applied.**

2. `README.md` — Aspire and DAPR runtime notes
   - Proposed issue: the per-epic runtime notes stopped at Epic 5 and did not describe where Epic 7 admin/governance runs, the per-subject enforcement seams, or the deferred-projection caveat.
   - Code comparison: `CommandGatewayServiceCollectionExtensions.cs` lines 117–127 register `AlwaysActive…`/`AlwaysUnlimited…` defaults, confirming the control floor is enforced-seam-ready but inert until a projection materializes control state. Enforcement seams confirmed in `ParticipantAuthorizationStage.cs`, `AcceptedCommandDispatcher.cs`, `ServiceClientGrantValidator.cs`.
   - Decision: **Update required and applied** (added an Epic 7 runtime note plus an explicit "wired but inert by default / do not describe as operationally active until Epic 8" caveat, matching the existing "do not describe as … until binding exists in code" pattern).

3. `_bmad-output/planning-artifacts/architecture.md`
   - Proposed issue: the doc describes admin/governance as a capability area (lines 56–59) but did not record that the Epic 7 control floor ships inert behind `AlwaysActive…`/`AlwaysUnlimited…` defaults — a reader could infer the floor is operationally active.
   - Code comparison: confirmed inert defaults in DI; the existing "M0 is a walking skeleton" bullet already carries an Epic 4 implementation-status breadcrumb, so an Epic 7 breadcrumb is the consistent home.
   - Decision: **Update required and applied** (appended a concise Epic 7 implementation-status sentence to the walking-skeleton bullet). The requirements-level capability descriptions (lines 56–59) were accurate and left unchanged.

4. `_bmad-output/planning-artifacts/epics.md`
   - Proposed issue: possible mismatch between Epic 7 story definitions and the implemented divergences (no `AdminScope.Security` role; notification persistence via generic command dispatch; per-action-class AI policy map).
   - Code comparison: the divergences satisfy the same acceptance criteria via different mechanisms — they are implementation choices, not spec errors. Epic 7 ACs and the Epic 8 preview match implemented and next-planned behavior.
   - Decision: **No update required.**

5. `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
   - Proposed issue: API docs may need new endpoints for queues, notification routing, escalation, and the control floor.
   - Code comparison: the OpenAPI file has 6 HTTP paths total and already contains the admin/policy and control-floor command DTOs (`SubmitTenantPolicyChange`, `Submit/Approve{MailboxSource,ServiceClient,AiActor,CommandCapability,OutboundChannel}Disable|Quarantine`, `Submit…RateLimit`, plus `*RateLimitWindow` schemas) as component schemas riding the generic command-submission transport. Queue/notification/escalation/backlog/rubber-stamp reads added no new public paths.
   - Decision: **No update required** (the contract is already accurate; the "no new public HTTP shapes for the operational read surfaces" claim is verified).

6. `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`
   - Proposed issue: possible stale tenant-admin/governance language after implementation.
   - Code comparison: PRD and addendum are requirements/specification documents; they describe FR51–FR63/FR75a–g and the Command Allowlist v1 governance contract as requirements and do not claim a current implementation status that the code contradicts.
   - Decision: **No update required.**

7. `AGENTS.md`
   - Proposed issue: Epic 7 surfaced heavy submodule-gitlink-drift friction; build/submodule guidance might need revision.
   - Code comparison: the existing root-level, non-recursive submodule guidance is correct and is exactly what the Epic 7 reviews converged on. No instruction in the file is contradicted by Epic 7 implementation.
   - Decision: **No update required** (the recurring drift was an environment-discipline issue, not a docs error; captured in the retrospective action items instead).

8. `Directory.Packages.props`
   - Proposed issue: configuration docs might need new package pins for Epic 7.
   - Code comparison: Epic 7 added server-internal governance logic and contract DTOs only; no new third-party package dependency was introduced.
   - Decision: **No update required.**

## Applied Updates

- `README.md`
  - Added an Epic 7 sentence to the project overview (admin permission model, policy schema editor + two-person rule, per-action-class AI policy, operational queues, notification/escalation/throttling/backlog/rubber-stamp observability, the disable/quarantine/rate-limit control floor, and allowlist v1 + lifecycle completion with `Skipped`).
  - Added an Epic 7 runtime note describing where admin/governance runs, the per-subject enforcement seams, the allowlist-widened-last and audit-metadata-only invariants, and that most admin surfaces ride the generic command transport with no new HTTP paths.
  - Added an explicit caveat that control-state/rate-limit enforcement is wired but inert behind `AlwaysActive…`/`AlwaysUnlimited…` defaults and the notification evaluators have no periodic runtime trigger yet — not to be described as operationally active until Epic 8.

- `_bmad-output/planning-artifacts/architecture.md`
  - Appended a concise Epic 7 implementation-status note to the "M0 is a walking skeleton" bullet, recording that the admin/governance breadth and control floor land on the spine but ship inert behind provider defaults until Epic 8 materializes the read-side projection.

## Discarded Updates

- `epics.md` was not edited; Epic 7 acceptance criteria and the Epic 8 preview match implemented and next-planned behavior, and the implementation divergences satisfy the same ACs.
- OpenAPI was not edited; it already carries the Epic 7 admin/policy and control-floor command DTOs and the operational read surfaces correctly add no new public HTTP paths.
- PRD and addendum were not edited; they remain accurate requirements/specification documents.
- `AGENTS.md` was not edited; its non-recursive root-level submodule guidance is correct and aligns with the Epic 7 review conclusions.
- `Directory.Packages.props` was not edited; Epic 7 introduced no new package dependency.

## Residual Watch Items

- When Epic 8 materializes the deferred control-state/rate-limit projections and the notification/escalation runtime triggers, update the README and architecture "inert by default / not operationally active" caveats to reflect the now-active behavior.
- If Epic 8 introduces new public HTTP read endpoints for dashboards/telemetry, add them to the OpenAPI and re-audit the "rides the generic command transport" claim.
- Continue checking story File Lists, recorded test counts, sprint-status rows, and `git submodule status` before marking stories done (recurring Epic 7 bookkeeping defects).
</content>
