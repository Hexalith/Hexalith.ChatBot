---
title: 'Admit Commands Through the Single CommandGateway'
type: 'feature'
created: '2026-10-20'
status: 'in-progress'
baseline_commit: 'cd8314401488c7ba3453cec25bdf305e34e0d146'
story_key: '1-7-admit-commands-through-the-single-commandgateway'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Admission lacks profiles, allows indeterminate classification, and gates execution only with caller-supplied concurrency. Signed markers can bypass commit restrictions.

**Approach:** Select product-declared profiles centrally, contain unsafe decisions before effects, and require accepted atomic participation for every mutation.

## Boundaries & Constraints

**Always:** Follow Story 1.7 and the addendum's profiles, command map and qualification/approval rules. Universal order: authentication, trusted tenant binding, authorization, operation identity/concurrency, applicable profile checks, canonical envelope, atomic participation. Preserve origin, existence-neutral outcomes, cancellation and historical replay formats. Bookkeeping requires atomic participation.

**Never:** Allow caller/adapter/policy/model profile selection or effect downgrades. Misrepresent markers/preparation/SDK acceptance/synthetic evidence as owner execution. Enable open A5/A6/A9a/A13 capabilities, implement Story 1.8 storage or Story 1.9's durable attempt ledger, modify siblings, migrate or deploy. Existing redacted denial facts remain permitted.

## I/O & Edge-Case Matrix

| Input/state | Required behavior |
| --- | --- |
| Unknown/ambiguous command, subtype, effect or forged profile/risk | Safe denial before model/tool/mutation |
| Required classification missing, invalid, unqualified, failed or indeterminate | Terminal `classifier-indeterminate`; no proposal, approval, durable idempotency or effect; remediation requires a new linked request |
| Qualified declared read-only subtype and current policy/context/Project authority | Acceptance without mutation authority or proposal approval; unavailable evidence denies |
| Six mandatory effect classes | Current independent authorized human approval, presence, lifetime and exact scope/drift; policy cannot downgrade |
| Missing identity/revision/dependency or failed stage | Typed safe denial; no later stage/effect |
| A13/hook unavailable, any concurrency input, marked/unmarked SDK | Deny before mutation/reservation/dispatch; markers cannot bypass |

</frozen-after-approval>

## Code Map

Prefixes: `S=src/Hexalith.ChatBot.Server`, `T=tests/Hexalith.ChatBot.Server.Tests`, `A=tests/Hexalith.ChatBot.Architecture.Tests`, `C=src/Hexalith.ChatBot.Contracts`. Abbreviated filenames share the preceding directory.

- `S/Authorization/ChatBotAuthorityCatalog.cs`: exhaustive authority requirements and coverage pattern.
- `S/Governance/AiMediation/AiActionCommandMetadataProvider.cs`: trusted effect metadata; `S/Operations/GovernedOperationAggregate.cs`: preserve owner approval invariants.
- Story 1.6 remains in review; policy persistence/provider stay unavailable.

## Tasks & Acceptance

**Execution:**

- [x] `S/Gateway/Admission/ChatBotAdmissionProfile.cs`, `ChatBotCommandAdmissionCatalog.cs`: encode ten profiles and explicit stable-to-concrete mappings; deny unknown/duplicate mappings, effects and unsupported successors.
- [x] `S/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs`: validate exact current classifier qualification and trusted approval/policy evidence; default unavailable. Reuse pure owner rules; caller claims cannot supply proof.
- [x] `S/Gateway/ChatBotCommandAdmissionPipeline.cs`, `ChatBotGatewayContext.cs`: enforce profile selection, ordered checks and typed failures before writes; require commit participation regardless of concurrency. Preserve authorized historical reads.
- [x] `S/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`, `AiActionApprovalGate.cs`: contain invalid/indeterminate classification, reject undeclared read-only subtypes and enforce six-effect approval before policy permits execution.
- [x] `S/Gateway/ChatBotDomainServiceAdmissionStage.cs`, `CommandGatewayServiceCollectionExtensions.cs`, `CommandGateway.cs`: use the SDK hook only; apply commit restrictions to signed envelopes, register internal services and map safe outcomes without duplicate admission/dispatch.
- [x] `C/Messages/ChatBotMessageCatalog.cs`, `C/openapi/hexalith.chatbot.v1.yaml`: add terminal classifier failures and gateway factory mappings. Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` and `tests/fixtures/hexalith-chatbot-generated-client.sha256` through existing tooling.
- [x] `T/Gateway/CommandGatewayTests.AdmissionProfiles.cs`, `CommandGatewayAdmissionApiE2ETests.cs`, `ChatBotAdmissionMarkerTests.cs`: test the matrix across origins/transports; replace indeterminate-success expectations. Assert unchanged stores and absent effects; label synthetic evidence.
- [x] `A/Fitness/AdmissionProfileFitnessTests.cs`, `A/ScaffoldArchitectureTests.cs`: assert exhaustive unique mappings; use negative fixtures to reject competing pipelines, public stages, direct processor dispatch, adapter replicas and caller selectors non-vacuously.

**Acceptance Criteria:**

- Given any origin, when admission runs, then exactly one profile enforces universal/applicable stages without bypass or duplication.
- Given each matrix failure, when HTTP/SDK processes it, then safe results and unchanged-state assertions pass.

### Review Findings

Code review 2026-10-21 (bmad-code-review, full mode, baseline `cd83144` → working tree, 72 files). Four layers ran: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor; none failed. Of 61 raw findings, the 37 that survived became 10 decision and 10 patch entries; 24 were rejected and none deferred. Decisions were resolved with the user on 2026-10-21 using a revised pragmatic plan: 8 became patches (D1–D7, D10) and 6 items were deferred (D8, D9, and the D1/D2/D3/D5 follow-ups). That leaves 18 patches and 6 deferrals.

- [ ] [Review][Patch] D1: give the recovery sandbox explicitly labeled synthetic admission, and re-baseline the Story 13.2 live test to the fail-closed 503 contract [tests/Hexalith.ChatBot.RecoverySandbox/RecoveryDependencyExercise.cs:599; tests/Hexalith.ChatBot.IntegrationTests/Story132ProductionBrowserAspireE2ETests.cs:641] — resolved from decision (high). The production-wide refusal broke the IntegrationTests merge lane (3 of 4 recovery exercises) and the ci.yml/release.yml live acceptance. The sandbox follows the `Story132AcceptanceRiskClassifier` precedent; the live topology keeps production admission and asserts a safe refusal with nothing persisted.
- [x] [Review][Defer] D1 follow-up: restore the Story 13.2 M0 message→ask→approve→execute→stop assertions [tests/Hexalith.ChatBot.IntegrationTests/Story132ProductionBrowserAspireE2ETests.cs:641] — deferred: the flow needs commit participation, which Story 1.8 delivers.
- [ ] [Review][Patch] D2: exempt `DecideAiActionApproval` and `MarkAiActionProposalInvalidatedByCorrection` from per-request classification [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:203] — resolved from decision (medium). A recordless `PassThrough` made both a terminal 422 regardless of evidence, so the round-1 B2/E1 rationale was incorrect. The proposal already exists only after determinate classification, and execution-time `ValidateApproval` catches classifier-version drift (the addendum says approval "does not reclassify").
- [x] [Review][Defer] D2 follow-up: take approver authority for AI decision commands from owner evidence [src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs:42] — deferred to Story 1.10. The gate reads a `requester_authority_class` claim that the production `ChatBotAuthorityPrincipal` never carries, so decisions stay blocked as insufficient authority until approver authority comes from owner evidence.
- [ ] [Review][Patch] D3: lock the ten legacy-unmapped concrete commands in an explicit fitness-test list [tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdmissionProfileFitnessTests.cs:13] — resolved from decision (medium). Any new concrete command that no stable row reaches then fails the build. Denying them now would rewrite about 400 test references and switch off shipped features, with no safety gain while atomic participation refuses everything.
- [x] [Review][Defer] D3 follow-up: map or deny the ten legacy-unmapped concrete commands through a PRD/addendum decision [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotCommandAdmissionCatalog.cs:9] — deferred: this is a prerequisite before Story 1.8 lets anything commit. The doubtful existing rows (`ExecuteRetentionDisposition`, `UpdateTenantPolicy`, `ProposeEmailProjectAssociation` mappings) are part of the same decision.
- [ ] [Review][Patch] D4: make stage applicability per command in the catalog [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotCommandAdmissionCatalog.cs:278; src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:249] — resolved from decision (medium). Replace the hard-coded correction-manifest exemption with a catalog table of not-applicable stages, such as approval lifetime before any approval exists, or outbound provider and no-blind-retry evidence before a send. A fitness test checks each exemption is a subset of its profile's stages and that every profile stage applies to at least one command.
- [ ] [Review][Patch] D5: fail closed on outbound send execution that lacks structured approval evidence [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:286] — resolved from decision (medium). `ExecuteApprovedOutboundDraft` no longer passes on an opaque non-blank `human-approval-lifetime-drift` reference.
- [x] [Review][Defer] D5 follow-up: add structured outbound approval evidence [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:286] — deferred to the outbound owner-source story. It should reuse `ValidateOutboundSendApproval` plus the independence, presence and ≤10-minute lifetime checks; nothing produces this evidence yet.
- [ ] [Review][Patch] D6: take revision fields from the concrete contract's declared properties and reject undeclared `expected*` members [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:84] — resolved from decision (medium). This closes the undeclared `expectedFooRevision` owner-guard bypass. The only two multi-revision commands (`ExecuteApprovedAIAction`, `ExecuteApprovedOutboundDraft`) must pass the approval validators, which already compare both revisions (the D5 fail-closed covers the outbound one).
- [ ] [Review][Patch] D7: bump `AiActionRiskClassifier.CurrentVersion` to `chatbot.ai-action-risk-classifier.m0.v2` [src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionRiskClassifier.cs:9] — resolved from decision (medium). The categorical rules changed, so the version must change. Historical v1 records still replay, and approvals made under v1 correctly fail the drift check.
- [x] [Review][Defer] D8: marked-envelope revalidation is incoherent for future commit participation [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:61] — deferred: masked while atomic participation always refuses, and Story 1.8 owns the commit-participation contract. Story 1.8 must handle the internal producers that mint markers and call `SubmitCommandAsync` directly (`AiExecutionCoordinator`, `EventStoreCorrectionPropagationCommandWriter`, `AiActionProposalInvalidationCoordinator`) and skip authorization, classification and profile stages. It must also handle gateway envelopes whose `riskClassification`, added by the dispatcher, is rejected by `ValidateOperationAsync`.
- [x] [Review][Defer] D9: the atomic boundary sees a placeholder envelope, and the legacy non-atomic flow still follows the gate [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:261] — deferred: masked while atomic participation always refuses, and Story 1.8 owns the commit-participation contract. Story 1.8 must have atomic participation consume the lifecycle-validated envelope and replace the post-gate reservation, second `PreCommit` envelope, separate audit write and dispatch (AC2 order, no duplication).
- [ ] [Review][Patch] D10: document the spec-literal carve-out for the kebab-case catalog codes [tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs:439] — resolved from decision (low). Keep `approval-required` and `classifier-indeterminate` as the PRD's literal names, and explain the exception next to `MessageCodeRegex`.
- [ ] [Review][Patch] The redaction allowlist rewrites both new problem types to authorization-denied [src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs:63] — `KnownType` omits `ChatBotProblemTypes.ClassifierIndeterminate` and `ChatBotProblemTypes.ApprovalRequired`, so 422 and 409 responses carry `.../authorization-denied`. Add both, and assert `type` in the 409 and 422 API tests.
- [ ] [Review][Patch] The two new catalog codes have no UI copy, so the UI lane is red [src/Hexalith.ChatBot.UI/Localization/SharedResource.resx] — `ChatBotLocalizationContractTests.EveryCatalogCodeShouldHaveCompleteLocaleSpecificCopy` iterates every catalog entry and fails on `Failure_Catalog_approval-required`. Add the en and fr `Failure_Catalog_*` and `Failure_CatalogReason_*` keys for both codes.
- [ ] [Review][Patch] The UI adapter fabricates a risk classification, so every UI Ask-AI submission is rejected with 400 [src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs:266] — `SubmitAskAiAsync` sends `RiskClassification: new AiActionRiskClassificationRecord(...)`, which breaks AC1 (adapters cannot classify). `ValidateOperationAsync` returns `command_contract_invalid` for it, and the user is told to "Correct the request metadata". Remove the record and assert `RiskClassification` is null in `ServiceShouldSubmitAskAiAsApprovalRequiredAppendProposalThroughUiOrigin`.
- [ ] [Review][Patch] The fitness guards miss real and obvious violations [tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdmissionProfileFitnessTests.cs:93] — `Violates()` does not catch an adapter building `AiActionRiskClassificationRecord` (the live UI violation above passes), does not scan `.razor`, and misses `partial`, `record` or `struct` declarations in the competing-pipeline and public-stage regexes. Add these cases with negative fixtures. The `SubmitCommandAsync` bypass rule waits on the marked-revalidation decision.
- [ ] [Review][Patch] The failure matrix is not exercised over the SDK `/process` transport [tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.AdmissionProfiles.cs:122] — only the atomic-refusal row goes through `ChatBotDomainServiceAdmissionStage`. Add stage-level cases using synthetic sources for classifier-indeterminate, approval-required, forged selectors, an undeclared subtype and approval failure, asserting the rejection reason and unchanged stores. AC: "when HTTP/SDK processes it".
- [ ] [Review][Patch] The new subject and tenant guard on marked envelopes has no test [src/Hexalith.ChatBot.Server/Gateway/ChatBotDomainServiceAdmissionStage.cs:73] — deleting the `TenantMismatch` or `AuthenticationDenied` arm leaves every test green. Mirror the `PolicyPersistenceAdmissionTests` mismatch theory for a marked `RecordGovernedNote`, asserting the rejection reason, the audit reason, and zero operation and atomic validator calls.
- [ ] [Review][Patch] The real validator's command-not-allowlisted branches are untested [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:65] — the 403 assertions for an unknown intended command and a non-allowlisted `commandName` run only against `SyntheticLegacyAdmissionEvidenceValidator`. Deleting line 65 or lines 74-77 turns the production 403 into a 503 unnoticed. Add two direct `ValidateOperationAsync` cases.
- [ ] [Review][Patch] The declared requester-authority set is duplicated [src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:190] — the same seven-value list appears in `AiActionRiskClassifier.cs:134`. If only one is updated, the classifier's determinate output will be refused at qualification. Share one declared set.
- [ ] [Review][Patch] The dispatcher's routed-to-approval branch is now dead [src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs:694] — the pipeline returns 409 `approval-required` before dispatch, so the `RoutedToApproval` arm and `RoutedToApprovalRecord` can no longer run and imply a flow that no longer exists. Remove them.
- [ ] [Review][Patch] A per-action-class policy test now asserts the opposite of its name [tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs:132] — `PerActionClassPolicyShouldRequireEveryRequestedClassToBeAllowed` now expects routing even for the policy it calls `allowed`, because any action class now routes (mandatory effects). Rename the test and its variables to match the "policy cannot downgrade mandatory effects" contract.

Rejected:

- `false` — The OpenAPI 422/409 description is accurate: it already says every mutation fails closed with 503 until atomic participation exists, and describes 422/409 only as conditional dispositions.
- `low` — Idempotent replay after the atomic gate is unreachable. The frozen matrix requires denial before reservation, replay is coupled to the reservation write, and a read-only replay path would be new design in a pre-release product.
- `false` — The new denial paths writing no authorization-failure fact or counter is deliberate: classifier rejections were already undecorated, Story 1.9 owns the attempt ledger, and the intent forbids new bookkeeping.
- `false` — SDK rejection events and the unguarded `JsonDocument.Parse` are not defects: rejection events are permitted denial facts, gateway markers never reach `/process` today, and the marker digest covers a server-serialized JSON payload.
- `false` — The default-interface problem factory methods cannot diverge: `ChatBotProblemDetailsFactory` is the only implementation, it overrides both, and the pattern copies the existing `CreateValidationProblem` default.
- `low` — No test asserts the default host resolves the production validator. The pipeline's null fallback is that validator, so a missing registration still fails closed.
- `low` — The OpenAPI 409 reuses the generic Conflict response and the 422 has no examples. That is cosmetic, and fixing it means a contract plus client regeneration.
- `false` — Request-level `riskClass`/`actorType` are pre-existing published optional fields that admission never reads, so they cannot select or weaken a profile.
- `rejected` — `ChatBotAdmissionMarkerTests.cs` is listed in the task but unchanged (also raised by the edge-case claim check). This is real, but the only fix is editing the spec under review.
- `false` — The conformance lane's validator is labeled: `SyntheticLegacyAdmissionEvidenceValidator` is named and documented as synthetic-only, never qualification or owner evidence.
- `false` — A qualification-registry failure correctly returns `classifier-indeterminate`: the addendum maps missing or invalid classifier artifacts to `classifier-indeterminate`, and non-`approved-current` never falls back.
- `low` — A stale expected revision gets 400 instead of a typed conflict. It is unreachable until owner evidence exists, and choosing the conflict code is a contract decision.
- `low` — An undeclared assistance subtype returns `classifier-indeterminate` instead of a denial. Both outcomes are safe and terminal before policy, model or effect; aligning them means changing the classifier, the pipeline mapping and the tests.
- `low` — Marked commit failures are counted as authorization failures. Marked envelopes cannot reach `/process` today, and the fix adds reason routing.
- `false` — The gate's `Blocked(classifier-indeterminate)` is not misreported as 403: in the pipeline the classifier pre-empts those conditions (indeterminate, recordless, JSON or subtype failure) before the gate runs.
- `low` — A missing `policySnapshotId` returns `classifier-indeterminate`. That is defensible, because an incomplete classifier input is indeterminate per the addendum, and the UI always supplies it.
- `false` — There is no qualification sample-size minimum to enforce: the addendum defines none for the risk classifier, and the gate status is computed by the registry.
- `false` — The evidence reads have no unbounded-hang path: the only source returns synchronously, and the caller's cancellation still applies.
- `low` — A context-package caller mismatch maps to a retry-later code. It is unreachable until owner evidence exists, and stale-versus-malformed needs a contract decision.
- `false` — The pipeline's fallback validator ignoring the injected clock has no effect: the fallback always uses the unavailable source, which fails before any clock comparison.
- `false` — The unaudited not-allowlisted path follows the same pre-existing pattern as classifier-rejected not-allowlisted outcomes.
- `maybe-false` (would be `low`) — The fitness test's `/obj/` filter breaks on Windows separators, but no generated obj or bin source is shown to match a guard pattern.
- `rejected` — The edge-case claim check also found the unchanged `ChatBotAdmissionMarkerTests.cs` claim; as above, the only fix is editing the spec under review.
- `false` — Round-1 rejection of B3 is upheld as current behavior; the latent part is tracked in the marked-revalidation decision.

## Implementation Notes

- Approved by the user on 2026-10-20; frozen intent unchanged.
- Pre-edit baseline: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2. AppHost build passed (0 warnings/errors); startup exited 134 because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` was not configured. `aspire describe --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` confirmed no running ChatBot AppHost. No live-topology evidence is established.

## Spec Change Log

## Review Triage Log

Review round 1: all three independent reports were collected before triage. Each finding is classified individually below; repeated findings retain separate rows. Severity reflects the demonstrated admission-validator or verification defect; the unavailable production source and unconditional atomic refusal still prevent owner execution.

| ID / layer | Verdict | Route / group | Evidence and decision |
| --- | --- | --- | --- |
| B1 / blind: requester authority claim | false | reject | `ChatBotAuthorityPrincipal` deliberately removes caller authority claims. The proposed outcome requires a future production qualification/owner-evidence provider; `UnavailableChatBotAdmissionEvidenceSource` currently stops the operation before classification, and missing authority must remain terminal. Deriving an undeclared authority class now would invent authority while A5/A9a remain open. |
| B2 / blind: decision/invalidation recordless classification | false | reject | The classifier does return recordless `PassThrough` for these commands, and the pipeline correctly contains it as `classifier-indeterminate`. Neither command carries a current owner-qualified action record, and no production owner source can establish one. The claimed supported execution path requires unavailable evidence; bypassing required classification would violate the frozen matrix. |
| B3 / blind: enriched marked AI payload rejected | false | reject | Real admission cannot reach `AcceptedCommandDispatcher` enrichment: owner evidence is unavailable and atomic participation always refuses. Synthetic legacy SDK round trips explicitly replace that validator. No real accepted-and-enriched envelope was shown to reach this guard; rejecting supplied risk records remains required while no accepted commit participation exists. |
| B4 / blind: marked envelope omits concurrency | false | reject | The omitted concurrency cannot reject a currently admitted production command: real admission always refuses before dispatch. Marked revalidation requires either a payload revision or current owner concurrency proof and otherwise denies, as required. Preserving future commit evidence requires the still-unavailable A13 transport contract, which this intent expressly does not enable. |
| B5 / blind: approval identity binding | medium | patch / approval-identity | `ValidateApprovedExecutionApproval` previously relied on aggregate dictionaries keyed by command ApprovalId. Its new admission caller supplies independent records, and `ValidateApproval` omits the command/request/decision ID equality, so the pure validator accepts mismatched approval slots. Add those local equalities and regression cases. |
| B6 / blind: decision authorization/disabled/audit status | medium | patch / decision-status | The new pure approval validator accepts an Approve record with denied authority, a disabled reason, or unavailable audit evidence. Although the current owner producer emits authorized/enabled/available records, the validator's accepted input contract must reject these demonstrated contradictory decision states. Add direct status checks and cases; do not change owner event production. |
| B7 / blind: approver principal without approval authority | false | reject | `ChatBotHumanApprovalEvidence.Approver` explicitly requires a current operation-scoped owner-validated approver; `ChatBotAuthorityPrincipal` is internal and server-issued by exact owner authorization. The counterexample constructs a synthetic principal without meeting that contract; no production evidence producer or caller path supplies it. HasProject is an additional scope check, not a replacement for owner validation. |
| B8 / blind: current allowlist drift | medium | patch / allowlist-drift | The reused helper checks request versus command only. Setting both to an obsolete version passes profile validation despite current metadata/classification using the current catalog version. Compare execution/approval versions to trusted current command metadata and test joint drift. |
| B9 / blind: classification operation/effect binding | medium | patch / classification-binding | Qualification checks validate artifact identity and thresholds but accept a record with another correlation or a missing mandatory effect floor. An injected classifier can supply such a record to the common pipeline. Bind output and input to this request, reject unknown metadata and inconsistent current allowlist/policy/effect/risk values using the existing trusted metadata; add focused malformed-output cases. |
| B10 / blind: SDK atomic test stops early | medium | patch / marked-atomic-test | The signed/unsigned test uses the unavailable source, so the operation refusal masks removal of marked atomic revalidation. Keep real validation but supply a matching synthetic operation proof and assert that the atomic boundary is reached with unchanged bookkeeping. |
| E1 / edge: decision/invalidation stops before profile | false | reject | Same demonstrated recordless-output path as B2, classified independently: no qualified committed classification exists for these commands in the current production evidence seam. Terminal containment is the required behavior for missing classification, not an authorization bypass or an enabled execution regression. |
| E2 / edge: unrelated approval record | medium | patch / approval-identity | The existing helper's dictionary-lookup precondition is absent at the new admission call site. Matching proposal/scope/revisions do not bind ApprovalId; this is the same root defect as B5 and needs the same three-way equality. |
| E3 / edge: evidence exception becomes caller error | low | patch / source-failure | The broad operation try/catch maps JsonException/InvalidOperationException/ArgumentException from `ReadProfileEvidenceAsync` to `command_contract_invalid`. These are dependency failures rather than payload errors. Isolate the source read's exception mapping to canonical execution unavailable, preserving caller cancellation, and test those concrete exceptions. |
| V1 / verification gap: signed SDK atomic boundary | medium | patch / marked-atomic-test | Pre-verified report: changing marked revalidation to return only operationFailure leaves the existing tests green because owner proof is missing. Supply passing operation evidence with the real atomic refusal and assert unchanged stores; shares B10's root cause. |
| V2 / verification gap: approval-required gateway contract | medium | patch / approval-response-test | Pre-verified report: no gateway/API case reaches RoutedToApproval and asserts HTTP 409, `approval-required`, non-retryability and escalation; existing gate and 422 cases cannot catch mapping regression. Add a synthetic determinate gateway/API case through the real gate with absent proposal/replay/execution bookkeeping. |

All survivors have direct internal corrections or focused test additions, add no public surface, and cover demonstrated states. No intent/spec loopback or deferred entry is required. Production qualification/owner sources and atomic participation remain unavailable.

## Verification

- Attempt the required Aspire baseline before code edits; record exact blockers.
- Build affected projects in Debug with `-p:UseHexalithProjectReferences=true -m:1 /nr:false`. Run focused tests and affected Server/Architecture/Contracts/Client/Conformance lanes individually with `.github/scripts/run-xunit-v4.sh`; retain TRX/CTRF, zero skips.
- Follow the baseline fallback ladder without weakening gates; apply `docs/story-evidence-integrity.md` before completion claims.

### Implementation verification — 2026-10-20

Implementation and in-process/synthetic verification only: no owner execution, live topology, completion transition, or A5/A6/A9a/A13 gate closure is claimed. The production evidence source and atomic participation remain unavailable. Historical reducer/replay fixtures use explicitly named synthetic admission/classifier evidence; those fixtures cannot enable production services. Frozen intent and task checkboxes are unchanged.

- Debug builds of Server.Tests, Contracts.Tests, Client.Tests and Conformance.Tests passed with zero warnings/errors using `dotnet build tests/<project>/<project>.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false`. Architecture.Tests passed its focused Debug build with zero warnings/errors using `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:BuildProjectReferences=false -m:1 /nr:false`, consuming the already-built Debug dependencies.
- Each lane ran individually with the catalog-pinned native runner: `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/<project>/bin/Debug/net10.0/<project> TestResults/story-1-7-<lane>.ctrf.json -result-trx TestResults/story-1-7-<lane>.trx`. Results and exact failing test identities are retained in the TRX/CTRF files below.

| Lane / project | Passed / executed | Failed | Skipped | Evidence prefix |
| --- | --- | --- | --- | --- |
| Server / Hexalith.ChatBot.Server.Tests | 3018 / 3018 | 0 | 0 | `TestResults/story-1-7-server` |
| Contracts / Hexalith.ChatBot.Contracts.Tests | 531 / 531 | 0 | 0 | `TestResults/story-1-7-contracts` |
| Client / Hexalith.ChatBot.Client.Tests | 94 / 94 | 0 | 0 | `TestResults/story-1-7-client` |
| Architecture / Hexalith.ChatBot.Architecture.Tests | 131 / 133 | 2 | 0 | `TestResults/story-1-7-architecture` |
| Admission fitness / Hexalith.ChatBot.Architecture.Tests | 10 / 10 | 0 | 0 | `TestResults/story-1-7-admission-fitness` |
| Conformance / Hexalith.ChatBot.Conformance.Tests | 150 / 151 | 1 | 0 | `TestResults/story-1-7-conformance` |

The focused admission-fitness command adds `-class Hexalith.ChatBot.Architecture.Tests.Fitness.AdmissionProfileFitnessTests` to the Architecture runner command; it passes without suppressing the full lane's failures.

Broad-gate blockers, left unchanged:

1. Full Architecture runner command above exits 1. `Hexalith.ChatBot.Architecture.Tests.LiveRecoveryValidationArchitectureTests.ReleasePublication_ShouldRequireFreshMainHeadImmediatelyBeforeSemanticRelease` expects `timeout-minutes: 30`; the existing release workflow sets 45. `Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests.RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog` finds ten local version declarations in EventStore, Parties and Works. Both failures are already recorded in Story 1.6's resumed verification; no workflow or sibling maintenance was performed.
2. Full Conformance runner command above exits 1. `Hexalith.ChatBot.Conformance.Tests.DaprAccessControlConformanceTests.EventStorePolicyMustGrantEveryMappedSdkPostRoute` finds five current SDK POST routes absent from the existing production ACL: `/project/rebuild/reclaim-guarded/v1`, `/project/rebuild/reclaim-recovery/v1`, `/project/source-guard/applied-receipt/v1`, `/project/source-guard/owner-proof/v1`, and `/project/source-guard/reconcile-applied-receipt/v1`. `git diff --exit-code -- tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs src/Hexalith.ChatBot.AppHost references/Hexalith.EventStore` exits 0: the guard, ACL/AppHost, SDK gitlink and sibling source were not changed by this implementation. The ACL was not broadened to hide the blocker.
3. The pre-edit Aspire baseline remains blocked by the missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret` recorded above; no live-topology evidence was established.

Client generation used the existing pinned tool from `src/Hexalith.ChatBot.Client`: `dotnet /home/administrator/.nuget/packages/nswag.msbuild/14.7.1/tools/Net100/dotnet-nswag.dll run nswag.json /variables:Configuration=Debug`, followed by the generated-client SHA-256 freshness fixture. Final Client tests confirm both byte-for-byte isolated regeneration and the freshness hash.

A transient Debug build failure reported `MSB3491` / `No space left on device` while writing the UniqueIds assembly-info cache. Only this task's redundant `/tmp/story-1-7-focused.log` (5,065,278 bytes) was removed; retained TRX/CTRF and pre-existing evidence were preserved. Subsequent builds passed, and verbose logs moved to `/dev/shm`.

### Root matrix-audit follow-up — 2026-10-20

Added 14 direct common-sequence cases covering AI read, AI effect, human effect and non-AI bookkeeping: required classification/qualification/approval stages run once, profile checks precede in-memory envelope construction and the real unavailable atomic gate, and operation/profile/qualification failures stop later calls before reservations, persisted envelopes or dispatch. Added 33 direct real-validator cases for missing/malformed operation identity and revisions, exact current owner-proof binding/unavailability, guarded revision paths, and one owner read cached across subsequent profile validations. The malformed-identity cases tighten the validator to the existing stable-identifier predicate. Production evidence and atomic participation remain unavailable.

The new test helpers each have a type-named source file and XML summaries. Corrected the two message-code XML summaries and aligned the synthetic-admission insertions in the three affected HTTP/projection fixtures.

- Final focused Debug build passed with zero warnings/errors: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:BuildProjectReferences=false -m:1 /nr:false`; it consumed dependencies rebuilt for the initial follow-up build.
- Final focused runner passed **85/85, zero skips**: `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story-1-7-admission-followup.ctrf.json -result-trx TestResults/story-1-7-admission-followup.trx -class Hexalith.ChatBot.Server.Tests.Gateway.ChatBotCommandAdmissionSequenceTests -class Hexalith.ChatBot.Server.Tests.Gateway.ChatBotOperationEvidenceValidatorTests -class Hexalith.ChatBot.Server.Tests.Gateway.ChatBotAdmissionEvidenceValidatorTests -class Hexalith.ChatBot.Server.Tests.Gateway.ChatBotHumanApprovalEvidenceTests`. Counts are 14 sequence, 33 operation, 27 classification/profile and 11 human-approval cases. TRX, CTRF and CTRF SHA-256 are retained. Broader lanes were not repeated, as requested by the root audit; their earlier results and blockers above remain recorded separately.
- `git -c core.whitespace=cr-at-eol diff --check` passes. Frozen intent, task checkboxes and workflow status remain unchanged.

### Root task and matrix audit — 2026-10-20

Read the complete implementation diff through the temporary-index snapshot and the subsequent audit delta; the real Git index is unchanged. All eight implementation tasks are complete. Matched 19 canonical test-method identities through TRX TestDefinitions/testId bindings to passing results in `TestResults/story-1-7-server-final.trx`; all six matrix rows have executed coverage:

| Matrix row | Passing coverage |
| --- | --- |
| Unknown/forged metadata or subtype | `CallerAdmissionSelectorsShouldFailBeforeReservation`, `CallerCannotForgeTrustedCommandRiskOrEffectMetadata`, `UndeclaredAssistanceSubtypeMustStopBeforePolicyEvaluation` |
| Missing/invalid/unqualified/failed/indeterminate classifier | `RequiredClassificationShouldContainLiteralNullRecordlessFailedAndIndeterminateOutputs`, `ProductionAiAdmissionShouldTerminateWithoutCurrentQualificationAndNeverCreateProposal`, `QualificationMustMatchEveryCurrentBindingAndThreshold` |
| Qualified read-only profile | `SyntheticQualifiedReadProofsRequireCurrentPolicyContextAndProjectAuthorityWithoutProposalApproval`; pure profile validation succeeds only with matching synthetic proofs, while the real atomic gate remains unavailable |
| Six effects and independent human approval | `MandatoryEffectMustStopBeforeEvenAPermissivePolicyCanDowngradeIt`, `IndependentHumanApprovalMustMatchPresenceLifetimeAndEveryScopeBinding` |
| Identity/revision/dependency/stage failure | `ChatBotOperationEvidenceValidatorTests`, `CommonAdmissionFailureShouldStopBeforeLaterStagesEnvelopeOrReservation`, `CommonAdmissionShouldRunEachRequiredStageOnceBeforeTheUnavailableAtomicBoundary` |
| A13, concurrency and marked/unmarked SDK | `ProductionAdmissionShouldRejectBookkeepingWithoutAtomicParticipationForEveryOrigin`, `SdkAdmissionShouldRejectSignedAndUnsignedBookkeepingWithoutCommitParticipation`, `CanonicalConcurrencyHttpRequestShouldFailClosedBeforeReservationAuditOrDispatch` |

The root audit added two undeclared-subtype cases and contained their observed JSON conversion failure in the approval stage before policy evaluation. The qualification containment test now supplies determinate synthetic classifier input so it actually reaches the real missing-qualification check. Added missing XML documentation on new test/helper declarations.

- Debug Server test build passed with zero warnings/errors. The root focused matrix runner passed 87/87, zero skips (`TestResults/story-1-7-root-matrix.{ctrf.json,trx}`).
- After all root code/test changes, the full Server lane passed **3067/3067, zero skips** with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story-1-7-server-final.ctrf.json -result-trx TestResults/story-1-7-server-final.trx`.
- Status is `in-review`, not a completion transition. The three previously documented broad-lane failures and the missing-secret AppHost startup blocker remain; no owner execution, live topology or evidence-gate closure is claimed.

### Post-review fixes and final verification — 2026-10-20

Collected all three independent review reports before triage. The 15 individual findings above produced seven patch groups; all seven are resolved and none was deferred. The root read the complete 512-line review-fix delta after implementation. Approval validation now binds all three ApprovalIds, requires an authorized/enabled/audit-available decision, and checks current trusted allowlist metadata. Classification validation binds output and input to the request's correlation, command, policy and current allowlist, preserves the mandatory effect/risk floor, and rejects blank or undeclared authority. Owner-reader exceptions map to dependency unavailability while caller cancellation propagates.

The signed/unsigned SDK and seven-origin tests now pass real operation validation with explicitly synthetic proofs and assert exactly one call to the real unavailable atomic gate, with no persisted admission or dispatch. The new HTTP case uses explicitly synthetic determinate classification, the real qualification validator and real approval gate; it asserts HTTP 409, `approval-required`, non-retryability and escalation, and empty idempotency/audit/replay/execution state. It does not add authority claims or enable production evidence.

- Implementation-agent focused verification passed **118/118, zero skips** in `TestResults/story-1-7-review-fixes-{validators,gateway}.{ctrf.json,trx}`. Server and Server.Tests Debug builds passed with zero warnings/errors.
- Root rebuilt Architecture.Tests and Conformance.Tests individually with `dotnet build tests/<project>/<project>.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:BuildProjectReferences=false -m:1 /nr:false`; both builds passed with zero warnings/errors and consumed the updated Debug Server output.
- Root reran every affected verification lane individually using `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/<project>/bin/Debug/net10.0/<project> TestResults/story-1-7-<lane>-reviewed.ctrf.json -result-trx TestResults/story-1-7-<lane>-reviewed.trx`.

| Final lane | Passed / executed | Failed | Skipped |
| --- | --- | --- | --- |
| Server | 3103 / 3103 | 0 | 0 |
| Contracts | 531 / 531 | 0 | 0 |
| Client | 94 / 94 | 0 | 0 |
| Architecture | 131 / 133 | 2 | 0 |
| Conformance | 150 / 151 | 1 | 0 |

The three broad-lane failures are exactly the unchanged release-timeout, sibling package-version and five missing SDK ACL-route failures already detailed above. The full Architecture TRX also contains **10/10 passing admission fitness cases**. Client regeneration/freshness checks pass. TRX/CTRF files and checksum sidecars are retained; no failing lane was filtered or weakened.

Root reconciled canonical TRX TestDefinitions/testId bindings for all review-patch cases in `TestResults/story-1-7-server-reviewed.trx`: eight approval identity/status/allowlist cases; 22 classifier binding/effect/authority cases; four owner-reader failure cases; caller cancellation; both signed/unsigned SDK cases; seven origins; and the HTTP approval-required case all passed. Existing six-row matrix coverage remains executed and passing in that same full Server run.

The spec stays `in-review` and the sprint entry stays `review`. The baseline requires all existing/new tests to pass before completion, and `docs/story-evidence-integrity.md` forbids completion when mandatory environment evidence is unavailable. Therefore the workflow's completion/status and completion-commit steps were not executed. The recorded AppHost missing-secret blocker has no replacement live-topology evidence; A5/A6/A9a/A13 remain open, production owner/qualification sources and atomic participation remain unavailable, and no owner execution, deployment or readiness is claimed. No commit, push, dependency update, sibling edit or real-index staging was performed.
