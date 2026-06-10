# Epic 6 Documentation Update Audit - 2026-06-11

**Project:** Chatbot
**Scope:** Epic 6 implementation learnings and documentation drift verification
**Mode:** Autonomous source-backed audit

## Summary

The audit reviewed documentation that could plausibly diverge from Epic 6 implementation learnings: architecture decisions, API/OpenAPI documentation, README guidance, configuration documentation, PRD/addendum requirements, and epic planning text.

Three verified discrepancies were found and updated. Three candidate documents were verified current and left unchanged.

## Verification Basis

Implementation evidence checked:

- Epic 6 story records:
  - `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md`
  - `_bmad-output/implementation-artifacts/6-2-outbound-draft-creation-within-authority.md`
  - `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md`
  - `_bmad-output/implementation-artifacts/6-4-inbound-authenticity-passthrough-and-header-inspection.md`
  - `_bmad-output/implementation-artifacts/6-5-on-behalf-of-disambiguation-and-external-sender-posture.md`
- Sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- OpenAPI contracts:
  - `CreateOutboundDraft`
  - `RequestOutboundSendApproval`
  - `DecideOutboundApproval`
  - `ExecuteApprovedOutboundDraft`
  - `SenderAuthorityClassificationResult`
  - `MailboxAuthenticityMetadata`
  - `MailboxDelegatedSenderSnapshot`
  - `MailboxExternalSenderPosture`
  - `MailboxAuthenticityStrictnessPolicySnapshot`
- Source code:
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftAuthorityEvaluator.cs`
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundSendAuthorityEvaluator.cs`
  - `src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs`
  - `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- Tests:
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Association/Scoring/DeterministicAssociationScorerTests.cs`
  - `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`

## Documents Updated

### `README.md`

Verified discrepancy:

- The project summary listed Epic 1-5, then skipped directly to Epic 7. Sprint status and implementation evidence show Epic 6 is complete and has public contracts/source code for outbound communication and inbound authenticity.

Code comparison:

- OpenAPI includes outbound draft/send approval schemas and sender-authority result schemas.
- Worker/server code includes authenticity metadata, delegated sender posture, external-sender posture, and strictness routing.
- Story records and sprint status mark Epic 6 done.

Update applied:

- Added Epic 6 to the top-level project summary.
- Added a current implementation note stating that outbound authority decisions are owned by server evaluators, outbound drafts are local ChatBot records, outbound sends require approval and single-shot idempotency, and inbound authenticity is provider passthrough without ChatBot re-verification.

### `_bmad-output/planning-artifacts/architecture.md`

Verified discrepancy:

- Core architectural decisions still listed "outbound send + inbound authenticity (M1)" as deferred. Gap analysis still treated outbound sender-authority mapping enforcement as an M1 detail gap.

Code comparison:

- `SenderAuthorityClassifier`, outbound draft/send commands, outbound authority evaluators, inbound authenticity metadata, delegated-sender posture, and strictness routing are implemented and tested.
- Sprint status marks Stories 6.1-6.5 done.

Update applied:

- Changed deferred wording to say outbound send and inbound authenticity were planned for M1 and implemented in Epic 6.
- Changed the M1 detail gap to state outbound sender-authority mapping enforcement is implemented in Epic 6, while policy/editor/parity items are implemented across Epics 5 and 7.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

Verified discrepancy:

- The M1 summary said "DMARC/DKIM/SPF validation." The authoritative FR48a/addendum and implemented code use provider-supplied passthrough; ChatBot does not re-verify SPF/DKIM/DMARC.

Code comparison:

- `GraphMailboxIntakeWorker` parses selected provider-supplied headers into metadata and does not perform DNS or provider re-verification.
- Story 6.4 acceptance and reviews explicitly preserve passthrough and no re-verification.

Update applied:

- Replaced "DMARC/DKIM/SPF validation" with "provider-supplied DMARC/DKIM/SPF passthrough without ChatBot re-verification."

## Documents Verified Current

### `_bmad-output/planning-artifacts/epics.md`

Verification result:

- Epic 6 stories already describe sender-authority classes, draft creation, outbound approval, inbound authenticity passthrough, header inspection, delegated sender posture, and external-sender strictness consistently with the implementation.

Decision:

- No update applied.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`

Verification result:

- Inbound Message Authenticity correctly says ChatBot records provider verdicts and does not re-verify.
- Authority class mapping correctly lists the five FR48 classes and conflict rules.
- Idempotency Keys correctly lists outbound send as `tenant_id + outbound_draft_id + send_actor`.

Decision:

- No update applied.

### `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

Verification result:

- OpenAPI contains Epic 6 schemas and finite tokens for outbound draft/send approval, sender authority, authenticity metadata, delegated sender posture, external-sender posture, strictness policy, and association routing reasons.

Decision:

- No manual update applied.

## Discarded Candidate Updates

- Addendum strictness wording: discarded because it already matches the source and tests.
- Epics Epic 6 section: discarded because it already describes the intended behavior accurately.
- OpenAPI schema update: discarded because code and OpenAPI already expose the current Epic 6 contract surface.
- Configuration documentation: discarded because no Epic 6-specific configuration doc drift was found beyond README/architecture/PRD wording.

## Result

Documentation now reflects the current Epic 6 implementation state:

- Epic 6 is visible in the README project summary.
- Architecture no longer presents outbound/authenticity as unimplemented M1 work.
- PRD summary wording no longer implies ChatBot performs SPF/DKIM/DMARC validation.
