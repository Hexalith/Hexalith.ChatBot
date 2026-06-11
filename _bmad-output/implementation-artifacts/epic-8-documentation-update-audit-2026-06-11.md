# Epic 8 Documentation Update Audit - 2026-06-11

**Scope:** Documentation that may need updates after the Epic 8 retrospective re-run.
**Method:** Read current documentation, compare against story files and implementation code, update only verified discrepancies, discard proposed updates where code and docs already align.

## Candidate Documents Reviewed

- `README.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `docs/adrs/*.md`

## Implementation Evidence Checked

- Sprint status: Epic 8 and Stories 8.1-8.7b are `done`; `epic-8-retrospective` was already `done`.
- Story 8.6: hosted Dapr Workflow binding implemented through `AddChatBotCorrectionPropagationWorkflow()`, `Program.cs` configuration, AppHost activation, workflow health endpoint, and validation evidence.
- Story 8.7a: server runtime control/rate-limit providers replaced `AlwaysActive...` and `AlwaysUnlimited...` defaults with projection-backed providers. Architecture tests guard the runtime registrations.
- Story 8.7a review caveat: mailbox-source path has provider foundation only; no hosted worker path consumes `GovernedControlStateView`.
- Story 8.7b: `PeriodicEnforcementCoordinator` and optional `PeriodicEnforcementBackgroundService` drive the deferred evaluator set when `ChatBot:UsePeriodicEnforcementRuntime=true`; AppHost sets the flag.
- Story 8.7b review caveat: `IAuditProjectionLagSource` uses `CheckpointBackedAuditProjectionLagSource`, but its registered checkpoint source is still `UnavailableAuditProjectionCheckpointSource`, so audit lag has no real measured checkpoint feed yet.
- OpenAPI: no Epic 8.6/8.7 public HTTP shapes were added; runtime work is internal DI/host/projection/runtime behavior.

## Updates Applied

### `README.md`

Verified discrepancy:

- README still said correction propagation must not be described as hosted Dapr Workflow until the binding exists.
- README still said Epic 7 control-state/rate-limit enforcement remained inert because gateway DI used `AlwaysActive...`/`AlwaysUnlimited...` defaults.
- README still said Epic 8 alert/runbook runtime triggers were deferred and no periodic runtime existed.
- README component list omitted `chatbot-workflow-statestore`.

Update applied:

- Corrected correction propagation to describe hosted Dapr Workflow as live topology coordination while preserving EventStore lifecycle ownership.
- Corrected Epic 7/Epic 8 runtime claims to state server runtime activation is implemented, with explicit mailbox-source and audit-checkpoint caveats.
- Added the workflow state-store component to the Aspire/DAPR component list.
- Adjusted the Epic 9 note so audit completeness publication is not incorrectly described as lacking any periodic path.

### `_bmad-output/planning-artifacts/architecture.md`

Verified discrepancy:

- The architecture still stated hosted Dapr Workflow binding was pending.
- The architecture still stated the control floor remained wired-but-inert until Stories 8.7a/8.7b land.

Update applied:

- Updated correction-propagation architecture to reflect Epic 8.6 hosted Dapr Workflow binding.
- Updated the M0/M1/M2 architecture finding to reflect 8.7a/8.7b server runtime activation and periodic enforcement ownership.
- Added explicit residual caveats for mailbox-source enforcement and audit-projection-lag checkpoint publication.

### `_bmad-output/planning-artifacts/epics.md`

Verified discrepancy:

- The Epic 8 planning section captured the intended 8.7a/8.7b release gate but did not record the current post-implementation caveats discovered in story reviews.

Update applied:

- Added an implementation note under Story 8.7 that 8.7a/8.7b landed the server runtime activation path while mailbox-source enforcement and audit-projection-lag checkpoint measurement remain follow-ups.

## Verified Current - No Update Applied

### PRD

Files:

- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

Decision:

- No update applied. The PRD states requirements for FR67, FR74, FR75, FR94, NFR41, NFR42a, NFR43, and NFR44. It does not claim that every runtime path is already fully implemented.

### Addendum

Files:

- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`

Decision:

- No update applied. No verified code/doc divergence was found during this pass. Operating baseline and requirement details remain requirement-level guidance, not implementation-status claims.

### OpenAPI

Files:

- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

Decision:

- No update applied. Epic 8.6/8.7 changes did not add public command/query HTTP endpoints or schemas requiring OpenAPI edits.

### ADRs

Files:

- `docs/adrs/*.md`

Decision:

- No update applied. The current ADR set focuses mostly on Epic 9 audit/recovery decisions. No ADR was found that claims the old Epic 8 runtime state or conflicts with current implementation.

## Discarded Proposed Updates

- Do not mark mailbox-source FR74 enforcement as fully live. Code still has only worker-side static/provider foundation and no hosted worker consumption of `GovernedControlStateView`.
- Do not mark audit-projection-lag as fully measured. The measured source exists, but the registered checkpoint source still returns no checkpoints.
- Do not update OpenAPI for 8.6/8.7. No public API surface changed for these stories.
- Do not alter PRD requirements to match implementation caveats. The PRD is a requirements document and remains valid.

## Result

Documentation now matches the verified implementation state:

- Hosted Dapr Workflow is live for correction-propagation saga coordination in the AppHost/live topology.
- Server runtime control/rate-limit enforcement and periodic evaluator ownership landed in Epic 8.7a/8.7b.
- Mailbox-source runtime enforcement and audit-projection-lag checkpoint measurement remain explicit follow-ups.
