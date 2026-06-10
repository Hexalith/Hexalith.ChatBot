---
baseline_commit: 0a3f392
---

# Story 1.13: Tenant-scoped fixture and evaluation scaffold

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a QA owner,
I want tenant-scoped fixtures, sandbox data, and evaluation-dataset partitions,
so that later calibration and conformance tests are safe, repeatable, and unable to leak or replay tenant data across boundaries.

## Acceptance Criteria

1. **Versioned tenant-scoped fixture manifest exists.** Given the downstream FR92/FR93 fixture need, when Story 1.13 is complete, then a shared synthetic/redacted fixture manifest exists under `tests/fixtures/` with a schema version, owner, source classification (`synthetic`, `redacted`, or `consented`), tenant partitions, stable IDs, redaction expectations, expected outcomes, and regression-history slots for every declared case. The manifest must be embedded or otherwise fail-closed at test runtime if missing; a missing fixture file must never produce a vacuous green test. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.13; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR92; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR68]

2. **Evaluation partitions cover the A9a taxonomy without pretending to be the full dataset.** Given the A9a evaluation-dataset contract, when the scaffold is loaded, then it declares the required label taxonomy: `deterministic-match`, `ambiguous-match`, `no-match`, `unauthorized-project`, `cross-tenant-reference`, `duplicate`, `attachment-only`, `risky-ai-candidate`, and `inbound-authenticity-anomaly`. The scaffold must include at least one synthetic case for each label and explicit partition names for calibration, held-out regression, and adversarial examples, but it must state and test that this is a scaffold, not the full A9a 500-message M0 corpus. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#A9a; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence Thresholds]

3. **Fixture data remains tenant-scoped across all required workflow channels.** Given fixtures for mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior, when the fixture loader or sandbox harness builds a case, then every case carries a tenant ID and every tenant-owned resource ID is namespaced or paired with tenant scope. Cross-tenant examples are represented only as negative/adversarial cases, never as shared fixture state. Loader validation must reject blank tenant IDs, duplicate unscoped IDs, unknown tenant references, missing expected outcomes, and any case whose expected outcome does not declare redaction and audit expectations. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR93; _bmad-output/planning-artifacts/architecture.md#Authentication & Security; _bmad-output/planning-artifacts/architecture.md#Format Patterns]

4. **Sandbox harness reuses the existing gateway, conformance, and isolation assets.** Given Stories 1.11 and 1.12 already created the surface arms, gateway doubles, actor matrix, leakage corpus, and read-surface isolation host, when Story 1.13 adds sandbox support, then it reuses or extracts those helpers instead of creating a second gateway builder, second actor taxonomy, second leakage scanner, or second set of tenant constants. Any reusable fixture primitives that belong outside one test project should live in `src/Hexalith.ChatBot.Testing/`; server-only helpers must remain in test code or internal server seams via the existing `InternalsVisibleTo` path. [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs; tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantIsolationHarness.cs; tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageCorpus.cs; src/Hexalith.ChatBot.Testing/ChatBotTestConstants.cs]

5. **Replay/sandbox behavior cannot mutate production or call live external systems.** Given NFR69 and FR95a are broader M2 replay-isolation work, when this M0 scaffold runs, then its sandbox path is explicitly test-only: in-memory stores, synthetic mailbox/attachment/AI records, no Microsoft Graph calls, no outbound sends, no live AI/tool invocation, no production tenant IDs, and no production project/file/party payloads. It may submit the current trivial `RecordGovernedNote` through the in-process gateway as a command-execution fixture, but all other workflow channels are manifest/harness scaffold records until their feature stories ship real code. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR69; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Replay Isolation; _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md#Out of Scope]

6. **Non-vacuity and negative controls prove the scaffold can fail.** Given prior stories found vacuous-pass risk in architecture, conformance, and isolation gates, when Story 1.13 tests run, then they fail if any required label, workflow channel, tenant partition, expected outcome, redaction expectation, or regression-history field has zero cases. A deliberate negative-control fixture must prove that unscoped tenant data, a foreign-tenant sentinel, a missing fixture resource, and a missing expected outcome are rejected with diagnostics that name only metadata such as case ID, label, channel, and tenant partition. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results; _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md#QA Results; _bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md#Senior Developer Review (AI)]

7. **Current and future harnesses have one plug-in path.** Given later Epic 2/3/4 stories will add real mailbox, association, attachment, approval, and AI mediation behavior, when those stories need fixtures, then they can consume the Story 1.13 manifest and fixture loader without forking schema, tenant IDs, labels, or redaction checks. The manifest must reserve extension fields for `kernelVersion`, `confidenceScore`, `thresholdBand`, `evidenceRefs`, `policySnapshotId`, `idempotencyKey`, `stateTransition`, and `auditExpectedFields` so later stories plug in behavior without a schema rewrite. [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR50; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk Classifier]

8. **Build and regression gates stay green.** Given this story adds shared test infrastructure, when implementation is complete, then `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` succeeds with 0 warnings and 0 errors, no inline package versions are introduced, the compiled xUnit v3 binaries for `Hexalith.ChatBot.Testing.Tests` and `Hexalith.ChatBot.Conformance.Tests` are green, and Server/Architecture/Integration test binaries are also run if their source, project references, or harness behavior are touched. Tier-3 live legs remain env-gated and self-skip unless `HEXALITH_CHATBOT_TIER3=1` and the DAPR/Docker/Keycloak runtime are available. [Source: Directory.Packages.props; tests/Directory.Build.props; _bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md#Debug Log References]

## Tasks / Subtasks

- [x] Add the Story 1.13 fixture manifest and schema contract (AC: 1, 2, 3, 6, 7)
  - [x] Create `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json` or an equivalent single shared fixture file. Keep it synthetic/redacted by default; do not include real mailbox content, file paths, email addresses, provider payload snippets, secrets, tenant names from a live customer, or production project/party identifiers.
  - [x] Include top-level metadata: `schemaVersion`, `datasetId`, `owner`, `sourceClassification`, `isScaffold`, `createdAt`, `redactionReviewStatus`, `tenantPartitions`, `workflowChannels`, `requiredLabels`, `partitions`, `cases`, and `regressionHistory`.
  - [x] Declare the A9a labels exactly: `deterministic-match`, `ambiguous-match`, `no-match`, `unauthorized-project`, `cross-tenant-reference`, `duplicate`, `attachment-only`, `risky-ai-candidate`, and `inbound-authenticity-anomaly`.
  - [x] Declare workflow channels exactly enough for FR93: mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior.
  - [x] Put at least one synthetic case under each label and each workflow channel; cases can be small metadata records now, but each must declare tenant partition, expected outcome, redaction expectation, and audit expectation.
  - [x] Include calibration, held-out regression, and adversarial partitions. Mark the file as a scaffold and test that it is not presented as the full A9a 500-message corpus.

- [x] Add reusable fixture primitives in the right ownership boundary (AC: 1, 3, 4, 7)
  - [x] Prefer `src/Hexalith.ChatBot.Testing/Fixtures/` for manifest records and validation logic that only depend on `Contracts` and BCL APIs. Suggested types: `TenantScopedEvaluationDataset`, `TenantScopedFixtureCase`, `TenantScopedFixturePartition`, `TenantScopedFixtureExpectedOutcome`, `TenantScopedFixtureRedactionExpectation`, and `TenantScopedFixtureValidator`.
  - [x] Keep server-specific seeders, gateway invocation, HTTP host overrides, and projection stores in `tests/Hexalith.ChatBot.Conformance.Tests/Harness/` or other test projects; do not make server internals public for fixture convenience.
  - [x] Add `InternalsVisibleTo` only if needed for `Hexalith.ChatBot.Testing.Tests`; do not widen production visibility beyond existing patterns.
  - [x] Use `System.Text.Json` with web/camelCase options. Do not add Newtonsoft usage or another fixture parser.

- [x] Wire the manifest into tests without copy-to-output vacuity (AC: 1, 6, 8)
  - [x] Embed the Story 1.13 fixture manifest as a test resource, following the Story 1.12 corpus pattern in `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj`.
  - [x] If `Hexalith.ChatBot.Testing.Tests` also validates the manifest, either embed the same file there with the same logical name or expose a small loader that fails closed when the manifest stream is absent. Avoid per-project fixture forks.
  - [x] Add a test that deliberately asks for a missing logical resource and proves the loader throws a clear metadata-only error rather than returning an empty dataset.
  - [x] Add tests that reject empty labels, empty channels, empty tenant partitions, duplicate case IDs, duplicate unscoped resource IDs, and unknown tenant references.

- [x] Build a tenant-scoped sandbox harness on top of the existing conformance/isolation assets (AC: 3, 4, 5, 7)
  - [x] Add a `TenantScopedFixtureHarness` or similarly named helper under `tests/Hexalith.ChatBot.Conformance.Tests/Harness/`.
  - [x] Reuse `IsolationActorMatrix`, `CrossTenantLeakageCorpus`, `CrossTenantLeakageScanner`, `RecordingDispatcher`, `RecordingAuditWriter`, `InMemoryCoarseIdempotencyStore`, `InMemoryOperationStatusStore`, and `GovernedCommandConformanceHarness` where their responsibilities overlap.
  - [x] Do not create a second set of tenant constants if `tenant-alpha`, `tenant-beta`, own/foreign IDs, and leakage sentinels from Story 1.12 satisfy the case. If the evaluation scaffold needs additional tenant IDs, declare them once in the manifest and validate them centrally.
  - [x] Add a sandbox command-execution fixture that can drive the current `RecordGovernedNote` in-process and read durable state-store evidence. Keep mailbox/association/attachment/approval/AI records as scaffold records until their feature code exists.
  - [x] Scan every rendered/serialized fixture artifact with the leakage scanner, excluding only intentionally own-tenant metadata where the test explains why.

- [x] Add evaluation partition and expected-outcome tests (AC: 2, 3, 6, 7)
  - [x] Add tests proving every A9a label appears in at least one case and every case belongs to at least one partition.
  - [x] Add tests proving every case has `expectedOutcome`, `redactionExpectation`, and `auditExpectedFields`; for command-execution cases, include expected idempotency and state-transition facts.
  - [x] Add tests proving confidence-related fields use the `[0.0, 1.0]` domain when present, `thresholdBand` values align with `ThresholdBand`, and no non-finite/blank score can pass validation.
  - [x] Add tests proving risk-classifier cases reserve `effectSurface`, `requesterAuthorityClass`, and expected risk classification, without invoking any live AI provider.
  - [x] Add tests proving the scaffold has explicit regression-history slots even if the first run history is empty.

- [x] Add negative controls and diagnostics (AC: 6)
  - [x] Add a deliberately invalid fixture case with no tenant scope and prove validation fails.
  - [x] Add a deliberately leaking fixture artifact containing a foreign sentinel and prove the leakage scanner catches it.
  - [x] Add a case with a missing expected outcome and prove validation fails with the case ID, label, channel, and partition only.
  - [x] Add a case that references a production-like or unapproved source classification and prove it is rejected unless explicitly marked `synthetic`, `redacted`, or `consented`.

- [x] Preserve project structure and dependency guardrails (AC: 4, 5, 8)
  - [x] Do not add production `.Cli`, `.Mcp`, `.Workers`, M365, attachment, approval, or AI adapter projects.
  - [x] Do not add new runtime package dependencies unless unavoidable; if one is unavoidable, add its version centrally in `Directory.Packages.props` and keep `.csproj` references bare.
  - [x] Do not hand-edit generated client files under `src/Hexalith.ChatBot.Client/Generated/`.
  - [x] Do not initialize or update nested submodules; use only root-level submodule policy if submodule work is needed.
  - [x] Keep diagnostics metadata-only. Test failure messages may name case IDs, labels, channels, tenant partition aliases, and validation rule IDs; they must not dump full fixture payloads if a sentinel is detected.

- [x] Verify and document results (AC: 8)
  - [x] Build the full solution with warnings-as-errors: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binaries directly for `Hexalith.ChatBot.Testing.Tests` and `Hexalith.ChatBot.Conformance.Tests`.
  - [x] Run Server, Architecture, and Integration compiled binaries if source/project references in those areas changed; keep Tier-3 live tests env-gated.
  - [x] Record exact commands and counts in this story's Dev Agent Record.
  - [x] Update this story status through the normal dev workflow only after implementation and review gates pass.

## Dev Notes

### Source Artifact Analysis

Epic 1 establishes the safety floor for later feature work. Story 1.13 is not a product endpoint and not the full A9a dataset; it is the scaffold that prevents later mailbox, association, attachment, approval, AI, command, and audit tests from inventing incompatible fixture formats or unsafe tenant data shortcuts. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.13: Tenant-scoped fixture and evaluation scaffold]

FR92 allows authorized product/QA users to maintain internal evaluation datasets derived from consented, redacted, or synthetic examples with expected outcomes, redaction expectations, and regression history. FR93 requires tenant-scoped fixtures/sandbox data for mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior. NFR68 adds versioning, reproducibility, redaction verification, expected outcomes, and regression result history. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR92; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR93; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR68]

A9a defines the future evaluation dataset: at least 500 labeled messages by M0 release and at least 2000 by M1, with labels for deterministic match, ambiguous match, no match, unauthorized project, cross-tenant reference, duplicate, attachment-only, risky AI candidate, and inbound authenticity anomaly. This story should create the taxonomy and partition scaffolding, not claim to satisfy the full cardinality. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#A9a]

The PRD addendum sets score-domain and calibration context: association confidence is in `[0.0, 1.0]`, M0 defaults are `T_high = 0.90` and `T_low = 0.60`, calibration targets include precision >= 95%, recall >= 90%, and zero critical false positives into unauthorized projects. Risk classifier fixtures should reserve fields for command, tenant-policy classification, effect surface, requester authority class, expected classification, classifier version, and reviewer disagreement, but must not call a live AI provider. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence Thresholds; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk Classifier]

### Current Implementation State

Current reusable harness assets:

- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs` drives semantic intents through the real in-process `CommandGateway`, captures audit envelopes, and reads durable `GovernedOperationView` state. Reuse this for command-execution fixture proof rather than creating a new command lane.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/ConformanceGatewayDoubles.cs` owns shared `RecordingDispatcher`, `RecordingAuditWriter`, `FixedConformanceClock`, `NoOpReplayIntentQueue`, and `NoOpOperatorAlertSink`. Reuse these instead of duplicating private doubles again.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantIsolationHarness.cs` and `IsolationActorMatrix.cs` define the nine actor personas and tenant-context variants. Do not widen production `ActorType` for fixture labels.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageCorpus.cs` loads the Story 1.12 corpus from an embedded resource, validates non-empty required channels and sentinels, and exposes own/foreign tenant constants. Follow this fail-closed loading pattern for the Story 1.13 manifest.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageScanner.cs` is the existing leakage gate. Do not fork another scanner.
- `src/Hexalith.ChatBot.Testing/` currently only exposes `ChatBotTestConstants`. It is the right place for contract-level fixture records/validators if they do not need `Server` internals.

Current real M0 behavior is limited. The only current governed command is `RecordGovernedNote`; real mailbox intake, association, attachment handling, approval, and AI mediation code is not present yet. Story 1.13 should represent those channels as schema cases and sandbox records now, then later stories replace the placeholders with executable behavior. [Source: src/Hexalith.ChatBot.Contracts/Commands/RecordGovernedNote.cs; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs; tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs]

### Previous Story Intelligence

Story 1.12 completed the cross-tenant isolation harness at baseline `0a3f392`. It added a nine-persona matrix, leakage corpus, scanner, mutating-command negative tests, read-surface isolation tests, and store partitioning tests. Its senior review auto-fixed two issues and approved all ACs. Carry forward the review lessons:

- Non-vacuity must be explicit. Tests should fail if a required persona/channel/label/partition has zero cases.
- Positive controls matter. Where a safe-not-found is asserted, also prove the own-tenant record or fixture exists.
- Diagnostics must be metadata-only and must not dump a leaking body after a sentinel match.
- The dirty worktree item `_bmad-output/story-automator/orchestration-1-20260530-160445.md` was already noted as unrelated automation output; do not revert or overwrite it. [Source: _bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md#Senior Developer Review (AI); git status on 2026-05-31]

Story 1.11 explicitly called out Story 1.13 as future work and designed the surface-driver for later fixture sharing. Preserve that direction: use a common assertion/fixture path that real CLI/MCP adapters can later consume, but do not create real adapter projects in this story. [Source: _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md#Out of Scope]

### Architecture and Security Guardrails

- Tenant IDs come from authenticated context; fixture records can carry tenant scope as data, but test execution must not treat payload tenant values as authority. [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- Every derived record shape should carry `tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, and `schemaVersion`. Fixture schema should reserve these fields even if some cases are scaffold-only. [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]
- Problem/error responses and diagnostics are metadata-only. Raw provider payloads, file paths, exception text, email bodies, project names, party details, and evidence snippets must not appear in fixture failure messages or logs. [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns; Hexalith.Folders/_bmad-output/project-context.md#Critical Don't-Miss Rules]
- Replay/sandbox runs must be isolated from production mutation, external email sends, live AI tool execution, and live command execution beyond explicit test harness invocation. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR69]
- Do not add recursive submodule commands. Repository policy allows root-level submodule initialization only, and this story should not need submodule changes. [Source: AGENTS.md instructions; Hexalith.EventStore/_bmad-output/project-context.md#Development Workflow Rules]

### Suggested Fixture Schema

Use this as implementation guidance, not a mandatory exact JSON shape if the resulting validator preserves the same contract:

```json
{
  "schemaVersion": "1.0",
  "datasetId": "story-1-13-tenant-scoped-evaluation-scaffold",
  "owner": "test-architect",
  "sourceClassification": "synthetic",
  "isScaffold": true,
  "tenantPartitions": [
    { "tenantId": "tenant-alpha", "role": "own" },
    { "tenantId": "tenant-beta", "role": "foreign" }
  ],
  "requiredLabels": [
    "deterministic-match",
    "ambiguous-match",
    "no-match",
    "unauthorized-project",
    "cross-tenant-reference",
    "duplicate",
    "attachment-only",
    "risky-ai-candidate",
    "inbound-authenticity-anomaly"
  ],
  "workflowChannels": [
    "mailbox-intake",
    "association",
    "authorization",
    "attachment-handling",
    "approval",
    "ai-mediation",
    "command-execution",
    "audit"
  ],
  "partitions": [
    { "name": "calibration", "purpose": "threshold tuning scaffold" },
    { "name": "held-out-regression", "purpose": "stable regression scaffold" },
    { "name": "adversarial", "purpose": "cross-tenant and reviewer-disagreement scaffold" }
  ],
  "cases": [
    {
      "caseId": "case-deterministic-match-001",
      "tenantId": "tenant-alpha",
      "labels": ["deterministic-match"],
      "workflowChannels": ["mailbox-intake", "association", "audit"],
      "partition": "calibration",
      "sourceClassification": "synthetic",
      "expectedOutcome": {
        "state": "Associated",
        "reasonCode": "deterministic-match",
        "redactionState": "metadata_only"
      },
      "auditExpectedFields": ["tenantId", "actorId", "commandName", "correlationId", "outcome"],
      "regressionHistory": []
    }
  ]
}
```

Keep names and wire values stable, but do not overfit the implementation to this sample if a strongly typed record model gives clearer validation.

### Testing Requirements

- Use xUnit v3 `3.2.2` and Shouldly `4.3.0`; no new assertion library.
- Use repository-pinned package versions in `Directory.Packages.props`; no latest-package research or package upgrades are required for this story.
- Prefer direct compiled xUnit v3 binary execution because VSTest `dotnet test` has repeatedly failed in this sandbox with TCP listener `Permission denied`.
- Add the narrowest tests first: `Testing.Tests` for pure manifest validation and `Conformance.Tests` for harness integration. Broaden to Server/Architecture/Integration only if code there changes.
- Keep Tier-3 live tests honest: self-skip unless `HEXALITH_CHATBOT_TIER3=1` and Docker/DAPR/Keycloak are available.

### Out of Scope

- Building the full A9a 500-message M0 evaluation corpus.
- Real Microsoft 365/Graph mailbox ingestion, attachment scanning/storage, association scoring, approval workflow, AI provider calls, outbound send, or replay/simulation against a dedicated test tenant.
- Production UI for product/QA users to maintain datasets.
- New production endpoints or persistent stores for datasets.
- Real CLI/MCP/Workers/M365/AI adapter projects.
- Relaxing gateway internals from `internal` to `public`.
- Adding real tenant/customer data, production mailbox samples, raw email bodies, attachment contents, file paths, provider payloads, secrets, or unauthorized evidence snippets to fixtures.

### Project Structure Notes

- Shared fixture file: `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`.
- Pure fixture contract/validation code, if added: `src/Hexalith.ChatBot.Testing/Fixtures/`.
- Pure fixture validation tests: `tests/Hexalith.ChatBot.Testing.Tests/`.
- Server-aware harness integration tests: `tests/Hexalith.ChatBot.Conformance.Tests/` and `tests/Hexalith.ChatBot.Conformance.Tests/Harness/`.
- Existing conformance fixture embedding pattern to follow: `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj`.
- Do not fork the Story 1.12 leakage corpus into a Story 1.13-only scanner or tenant-constant file. Extend or reuse the existing corpus where practical.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.13: Tenant-scoped fixture and evaluation scaffold]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR92]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR93]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#A9a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR68]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR69]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence Thresholds]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk Classifier]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Replay Isolation]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]
- [Source: _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md]
- [Source: _bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/ConformanceGatewayDoubles.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantIsolationHarness.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageCorpus.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageScanner.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationActorMatrix.cs]
- [Source: src/Hexalith.ChatBot.Testing/ChatBotTestConstants.cs]
- [Source: Directory.Packages.props; Directory.Build.props; tests/Directory.Build.props]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.Folders/_bmad-output/project-context.md]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: Hexalith.Tenants/_bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- 2026-05-31T09:52:07+02:00 - Story moved to `in-progress`; existing `baseline_commit: 0a3f392` preserved.
- `dotnet build tests/Hexalith.ChatBot.Testing.Tests/Hexalith.ChatBot.Testing.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.ChatBot.Testing.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Testing.Tests.dll` - passed: 18 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests.dll` - passed: 51 total, 0 failed, 0 skipped.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll` - passed: 113 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests.dll` - passed: 33 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests.dll` - passed: 4 total, 0 failed, 2 skipped Tier-3 live tests.
- 2026-06-10T03:47:15+02:00 - BMAD dev-story revalidation found no unchecked tasks or review follow-ups; story and sprint status already `done`.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.ChatBot.Testing.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Testing.Tests.dll` - passed: 41 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests.dll` - passed: 87 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests.dll` - passed: 39 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll` - passed: 1510 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests.dll` - passed: 18 total, 0 failed, 2 skipped Tier-3 live tests.

### Completion Notes List

- Added a shared synthetic tenant-scoped evaluation scaffold manifest with exact A9a labels, required workflow channels, calibration/held-out/adversarial partitions, expected outcomes, redaction expectations, audit fields, regression-history slots, and future extension fields.
- Added reusable `System.Text.Json` manifest records, fail-closed embedded-resource loading, and strict validator rules under `src/Hexalith.ChatBot.Testing/Fixtures/`.
- Embedded the same fixture manifest in Testing and Conformance test projects to avoid copy-to-output or per-project fixture forks.
- Added pure validation tests for non-vacuity, missing-resource fail-closed behavior, tenant scope, duplicate IDs, unknown tenants, expected outcomes, confidence/threshold fields, risk-classifier reserved fields, and metadata-only diagnostics.
- Added a tenant-scoped conformance sandbox harness that reuses the existing governed-command conformance harness and cross-tenant leakage scanner; mailbox, association, attachment, approval, and AI channels remain scaffold records until their feature stories ship executable behavior.
- No new runtime package dependencies, generated client edits, adapter projects, production payloads, live external calls, or submodule changes were introduced.
- Revalidated the completed story on 2026-06-10; no open story tasks, review follow-ups, or checkbox updates were required.

### File List

- `_bmad-output/implementation-artifacts/1-13-tenant-scoped-fixture-and-evaluation-scaffold.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedEvaluationDataset.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedFixtureConstants.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedFixtureManifestLoader.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedFixtureValidationException.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedFixtureValidator.cs`
- `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`
- `tests/Hexalith.ChatBot.Testing.Tests/Hexalith.ChatBot.Testing.Tests.csproj`
- `tests/Hexalith.ChatBot.Testing.Tests/Fixtures/TenantScopedFixtureManifestTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/TenantScopedFixtureHarness.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/TenantScopedFixtureHarnessTests.cs`

### Change Log

- 2026-05-31 - Implemented Story 1.13 tenant-scoped fixture manifest, reusable fixture loader/validator, sandbox harness, non-vacuity/negative-control tests, and validation evidence.
- 2026-05-31 - Senior Developer Review (AI): adversarial multi-agent review (6 dimensions, per-finding verification). 9 findings (5 Medium, 4 Low; 0 Critical/High) auto-fixed. Aligned `thresholdBand` with the canonical `ThresholdBand` contract enum, made the validator fail closed on missing JSON arrays with metadata-only diagnostics, made the positive leakage scan non-vacuous, guarded the command sandbox against an unbound tenant, derived the command-execution assertion from the fixture, added own-tenant zero-case coverage enforcement, reserved-field round-trip and validator-bypassing negative tests. All gates re-verified green. Status → done.
- 2026-06-10 - Re-ran BMAD dev-story validation for Story 1.13; no unchecked tasks remained, story and sprint status were already `done`, and all requested build/test gates passed.
- 2026-06-10 - Story-automator adversarial re-review (auto-fix mode): re-read every File List artifact plus the downstream Story 4.1-4.3 extensions, re-validated all 8 ACs against the actual code, and re-ran every gate (build 0/0; Testing.Tests 41/41; Conformance.Tests 87/87, 0 skipped). No new Critical/High/Medium/Low findings; the 9 findings from the 2026-05-31 review remain fixed. The extra `corrected-stale-evidence` label and the `TaskIntent*` records/case fields are intentional Story 4.1-4.3 plug-ins through the AC7 extension path (confirmed via git history), not 1.13 defects, and were left intact. Status unchanged: done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (story-automator adversarial review) · **Date:** 2026-05-31

### Outcome: Approved (all findings auto-fixed)

Method: adversarial multi-agent workflow — 6 independent review dimensions (AC 1-4, AC 5-8, validator bugs, test vacuity, reuse/redaction, manifest data) fanned out, then every surfaced finding was adversarially verified by an independent skeptic. 16 raw findings → 5 refuted → **11 confirmed (deduped to 9 unique)**. No CRITICAL or HIGH issues; the implementation was sound. Per the autonomous-fix invocation, all 9 (5 Medium + 4 Low) were fixed without prompting.

### Acceptance Criteria

All 8 ACs validated as IMPLEMENTED against the actual code and the embedded manifest. No AC was missing or partial. The fixes below harden how some ACs are *enforced/proved*, not whether they were met.

### Findings fixed

| # | Sev | Finding | Fix |
|---|-----|---------|-----|
| F1 | Medium | Reserved `thresholdBand` forked the canonical `ThresholdBand` contract enum (`low/review/high` vs `below/within/above/critical`); the "alignment" test only checked a self-defined constant (AC7 / AC2 task). | `TenantScopedFixtureConstants.ThresholdBands` now derives from `Hexalith.ChatBot.Contracts.Enums.ThresholdBand` `[EnumMember]` values; fixture band values updated to `below/within/above`; added a test that pins the constants to the contract enum independently. |
| F7 | Medium | A missing required JSON array (e.g. omitted `labels`/`cases`) crashed the validator with a raw `ArgumentNullException`/`NullReferenceException`, bypassing the AC6 metadata-only diagnostic contract. | Added `RequireListPresent` guards for top-level collections and made `CaseIdentity.From` null-tolerant, so a missing array now yields a controlled `TenantScopedFixtureValidationException`; added 3 negative theory cases. |
| F2 | Medium | Positive leakage scan was structurally vacuous — `SerializeCaseMetadata` stripped `TenantId`/`TenantOwnedResources`, the only fields that could carry a sentinel, so the scan could never fail. | Added `SerializeCaseScope`/`DeclaredTenants`; the scan now covers the tenant/resource surface and excludes only legitimately-declared tenants via the reused `CrossTenantLeakageCorpus.SentinelsExcluding`, so a smuggled foreign token trips it. Exclusion is documented. |
| F3 | Medium | Command sandbox ignored `fixtureCase.TenantId` and ran every case under hardcoded `tenant-alpha` — a latent tenant-scope bypass for any future foreign-tenant command case (AC3/AC5). | `RunCommandExecutionFixtureAsync` now fails closed (metadata-only) if the case tenant ≠ the bound sandbox tenant; added a negative test. |
| F8 | Medium | Two positive manifest tests re-asserted invariants the loader already enforces, so their assertions could never fail; coverage / `forbiddenPayloadClasses`-non-empty rules had no validator-bypassing negative test. | Added validator-bypassing negative theory cases (zero-coverage label, empty `forbiddenPayloadClasses`) that exercise `ValidateCoverage` and `RequireNonEmpty` directly. |
| F4 | Low | Command-execution test asserted hardcoded literals; the fixture's `expectedOutcome.state`/`stateTransition` were decorative. | Test now derives the expected lifecycle/transition target from the fixture so the manifest is the source of truth. |
| F5 | Low | AC6's "tenant partition has zero cases" non-vacuity dimension was unenforced by the validator. | `ValidateCoverage` now fails if any `own`-role tenant partition owns zero cases (foreign tenants intentionally own none per AC3); added a negative test. |
| F6 | Low | Reserved fields `kernelVersion`/`evidenceRefs`/`policySnapshotId` had no round-trip assertion, so a property-name regression would silently deserialize to null. | Added `ReservedExtensionFieldsShouldRoundTripFromTheManifest`. |
| F9 | Low | Metadata-only diagnostics test spot-checked only 2 of ~16 forbidden payload-class tokens. | Test now asserts the full union of declared `forbiddenPayloadClasses` tokens is absent from every validator diagnostic. |

### Verification (all re-run after fixes)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — **0 warnings, 0 errors**.
- `Hexalith.ChatBot.Testing.Tests` — **37/37** pass (was 28; +9 hardening assertions/cases).
- `Hexalith.ChatBot.Conformance.Tests` — **52/52** pass.
- `Hexalith.ChatBot.Server.Tests` — 113/113 · `Hexalith.ChatBot.Architecture.Tests` — 33/33 · `Hexalith.ChatBot.IntegrationTests` — 4 (2 Tier-3 env-skipped).

Note: the Dev Agent Record above recorded "18 total" for `Testing.Tests`; the binary actually reported 28 pre-review (the invalid-dataset theory expanded) and 37 post-review.

### Git vs File List

No discrepancies in scope. All fixes edited files already present in the Dev Agent Record File List (no new files added). The only git-modified files outside that list are under `_bmad-output/` (excluded from review): `tests/test-summary.md` and the unrelated `story-automator/orchestration-1-20260530-160445.md` automation output (preserved, per the Story 1.12 carry-forward note).

---

### Re-review — 2026-06-10 (story-automator adversarial, auto-fix mode)

**Reviewer:** Jérôme Piquot (story-automator) · **Outcome: Approved — no new findings**

Method: re-read every File List artifact, the embedded manifest, and the reused conformance/isolation assets; re-validated all 8 ACs against the actual code; and re-ran the gates rather than trusting recorded counts.

- **AC validation:** all 8 ACs confirmed IMPLEMENTED against current code. AC4 reuse is genuine (`GovernedCommandConformanceHarness`, `CrossTenantLeakageCorpus`/`Scanner`, `BoundTenant`/`ForeignTenant`/`SentinelsExcluding`); fixture primitives live in `src/Hexalith.ChatBot.Testing/Fixtures/`, server-aware harness in `tests/.../Harness/`. AC6 non-vacuity has teeth (validator-bypassing negative theories for zero-coverage label, empty `forbiddenPayloadClasses`, own-tenant zero-case). AC8 verified by live build + binary execution.
- **Gates re-run:** `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **0 warnings / 0 errors**; `Hexalith.ChatBot.Testing.Tests` → **41/41**; `Hexalith.ChatBot.Conformance.Tests` → **87/87** (0 skipped). Counts match the Dev Agent Record. No inline package versions in the two embedding csproj files.
- **Downstream evolution (not a defect):** the 10th label `corrected-stale-evidence`, the `TaskIntentEvaluation*` records, and the `taskIntent*`/risk-classifier case fields were added by `feat(story-4.1/4.2/4.3)` (confirmed via `git log`/pickaxe), extending the 1.13 scaffold through the AC7 plug-in path exactly as designed. These belong to the later stories' File Lists, keep 1.13's tests self-consistent (assertions pin the `TenantScopedFixtureConstants` source of truth), and were intentionally **left intact** — reverting them would break Stories 4.1-4.3.
- **Git vs File List:** no discrepancies for files under review. Working-tree changes are limited to excluded `_bmad-output/` artifacts.

**Findings fixed this pass:** 0 (the 9 prior findings remain fixed; no new Critical/High/Medium/Low surfaced). **Status:** done.
