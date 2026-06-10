---
baseline_commit: d4b962f
---

# Story 1.12: Cross-tenant isolation harness

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a security owner,
I want a cross-tenant isolation harness that exercises negative access paths across the nine required actor personas,
so that every current and future ChatBot surface proves fail-closed behavior with zero data leakage before downstream feature work adds candidates, evidence, files, cursors, and richer actor flows.

## Acceptance Criteria

1. **Nine-actor negative matrix exists and is non-vacuous.** Given the required actor personas - human user, tenant admin, project admin/owner, service client, CLI client, MCP client, background worker, M365 event, and AI actor - when the isolation harness runs, then every persona has at least one executable negative case against a foreign tenant resource, each case declares its `ChatBotSurfaceOrigin` or adapter class explicitly, and the test suite fails if any persona or guarded channel has zero cases. The persona labels are test-harness concepts; do not expand the production `ActorType` enum unless a separate product story requires it. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.12; src/Hexalith.ChatBot.Contracts/Enums/ActorType.cs; src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs]

2. **Mutating command paths fail closed before durable work.** Given a principal bound to `tenant-alpha`, when each actor persona submits a command or shim intent targeting `tenant-beta` or carries stale/missing/ambiguous tenant context, then the gateway returns a catalog-backed metadata-only denial, records only the permitted authorization-failure fact, and performs zero durable work: no dispatcher call, no coarse-idempotency record, no pre/post commit audit envelope, no operation-status record, and no governed-operation projection. Assertions must inspect the gateway captures and stores, never just an HTTP status, CLI exit code, or MCP failure kind. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs; tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs; _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]

3. **Read surfaces collapse foreign, unknown, malformed, and stale context to indistinguishable safe denial.** Given tenant-scoped records seeded under `tenant-beta` for operation status, audit history, and governed-operation projection, when a `tenant-alpha` actor requests those records or requests unknown/malformed IDs, then the response status, correlation headers, and metadata-only problem body are indistinguishable after allowed per-request correlation normalization; the body must not reveal tenant IDs, note IDs, operation IDs for foreign records, candidate/evidence/file sentinels, cursor tokens, raw paths, exception text, or payload snippets. [Source: src/Hexalith.ChatBot.Server/Program.cs (`/api/v1/operations/{operationId}`, `/audit-history`, `/governed-operations/{noteId}`); src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs; tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs]

4. **Leakage corpus covers all required channels.** Given the Story 1.12 leakage corpus, when any negative case renders a problem, captured outcome, audit failure fact, status response, projection response, or test diagnostic, then the harness scans for sentinel values across candidates, evidence, files, pagination cursors, and error bodies. Current M0 code may only have command/status/audit/projection surfaces; candidate/evidence/file/cursor entries must still exist as sentinel channels in the corpus so future Epic 2/3 endpoints plug into the same gate rather than creating a parallel test style. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.12; _bmad-output/planning-artifacts/architecture.md#Format Patterns; Hexalith.Folders/_bmad-output/project-context.md#Critical Don't-Miss Rules]

5. **Store-layer tenant partitioning is proven, not assumed.** Given an in-memory or DAPR-backed state store seeded with the same logical note/operation/cursor IDs under two tenants where feasible, when reads and writes are attempted through the harness, then keys remain tenant-prefixed (`{tenant}:...`), a foreign-tenant read returns safe-not-found, an authorized same-tenant read still succeeds, and duplicate/replayed events do not copy a foreign tenant's view into the caller's tenant. [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs#KeyFor; src/Hexalith.ChatBot.Server/Projections/InMemoryGovernedOperationProjectionStore.cs; src/Hexalith.ChatBot.Server/Projections/DaprGovernedOperationViewStore.cs; tests/Hexalith.ChatBot.Server.Tests/Projections/GovernedOperationProjectionTests.cs]

6. **Surface shims reuse the existing parity harness without stage replication.** Given Story 1.11's `ISurfaceArm` and gateway-level conformance harness, when CLI/MCP/background/mailbox/AI actor cases are added for M0, then they remain test shims that construct typed `IChatBotCommand` or read requests and submit through the same gateway/HTTP boundary; no production `Hexalith.ChatBot.Cli`, `.Mcp`, or `.Workers` project is created, and no shim references or duplicates `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`, tenant binding, or authorization logic. Story 1.10 NetArchTest rules must remain green. [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs; tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs; _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md; _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md]

7. **Negative controls prove the harness can fail.** Given the isolation harness, when a non-destructive meta-test deliberately uses a vulnerable probe (for example, a test-only store/read path that ignores tenant in its lookup key or a rendered body containing `tenant-beta` / `foreign-candidate-sentinel`), then the harness fails and names the leaking channel and persona. This guard must cover both "missing persona/channel" vacuity and "leakage scanner does not scan a rendered artifact" vacuity. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results; _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md#QA Results]

8. **Build and regression gates stay green.** Given the story adds a release-gate test harness, when implementation is complete, then `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` succeeds with 0 warnings/0 errors, no inline package versions are introduced, the compiled xUnit v3 binaries for Conformance, Server, Architecture, and Integration tests are green, and Tier-3 live legs remain env-gated with honest self-skips unless `HEXALITH_CHATBOT_TIER3=1` and the DAPR/Docker/Keycloak runtime are available. [Source: Directory.Packages.props; Directory.Build.props; _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md#Debug Log References]

## Tasks / Subtasks

- [x] Build the cross-tenant actor/persona matrix in the conformance harness (AC: 1, 2, 6, 7)
  - [x] Add a reusable test-only model such as `IsolationActorPersona` / `IsolationActorMatrix` under `tests/Hexalith.ChatBot.Conformance.Tests/Harness/`, with exactly nine personas: `human-user`, `tenant-admin`, `project-admin-owner`, `service-client`, `cli-client`, `mcp-client`, `background-worker`, `m365-event`, `ai-actor`.
  - [x] Map each persona to a stable surface origin or adapter posture: UI for human/admin/project-owner, API/service for service client, `Cli`, `Mcp`, `Worker`, `Mailbox`, and `Ai` for the remaining machine actors. Use `ChatBotSurfaceOrigins.ToWireValue(...)` for surface tokens.
  - [x] Keep persona role/authority labels test metadata only. Do not add production enum values or RBAC policy unless another story asks for it.
  - [x] Add a non-vacuity test that asserts all nine personas exist, all have at least one negative case, and all required leakage channels are represented.

- [x] Implement mutating-command negative tests through the real gateway lane (AC: 2, 6)
  - [x] Reuse the Story 1.11 gateway harness assets (`RecordingDispatcher`, `RecordingAuditWriter`, `InMemoryCoarseIdempotencyStore`, `InMemoryOperationStatusStore`, `ClaimsTenantBindingStage`, `CommandGateway`) instead of duplicating production behavior.
  - [x] For each persona, submit a tenant-mismatched command shape with a bound `tenant-alpha` principal and a target `tenant-beta` or `tenant-beta:chatbot:{id}` scoped identifier. A private test command implementing `IChatBotCommand` is acceptable; it must exist only in tests.
  - [x] Add stale/unresolved tenant-context variants: missing tenant claim, multiple tenant claims, unsafe tenant claim, and nested JSON/body tenant-target mismatch as covered by `ClaimsTenantBindingStage`.
  - [x] Assert the denial is metadata-only and fail-closed: dispatcher count `0`, idempotency record count `0`, no pre/post audit envelopes, no operation-status record, no projection view, and only the expected authorization-failure capture when the gateway reaches that recording path.
  - [x] Assert every negative path scans its serialized problem/outcome for all leakage sentinels, including `tenant-alpha`, `tenant-beta`, `foreign-candidate-sentinel`, `foreign-evidence-sentinel`, `foreign-file-sentinel`, `foreign-cursor-sentinel`, raw path fragments, and raw exception text.

- [x] Add HTTP/read-surface isolation coverage for current M0 read endpoints (AC: 3, 5)
  - [x] Use `WebApplicationFactory<Program>` or existing Server.Tests helpers to seed stores with a known `tenant-beta` operation status, audit history, and governed-operation view, then read them as a `tenant-alpha` actor using the same public endpoints the UI/client uses.
  - [x] Cover `/api/v1/operations/{operationId}`, `/api/v1/operations/{operationId}/audit-history`, and `/api/v1/governed-operations/{noteId}`. If these tests live in Conformance.Tests, add only a bare `Microsoft.AspNetCore.Mvc.Testing` package reference using central package management.
  - [x] Compare foreign-known, unknown, malformed, and stale/missing-tenant responses as indistinguishable after allowed correlation normalization. Do not compare only status codes.
  - [x] Keep response bodies metadata-only. The owning tenant ID is a read scope and must never be echoed into the body.
  - [x] Add a same-tenant positive control for each seeded store so the test proves the foreign record exists and the safe denial is not a false pass from an unseeded store.

- [x] Add the leakage corpus and scanner (AC: 4, 7)
  - [x] Create a shared test fixture such as `tests/fixtures/story-1-12-cross-tenant-leakage-corpus.json` or an equivalent strongly typed test-only fixture under Conformance.Tests.
  - [x] Include sentinel classes for candidates, evidence, files, pagination cursors, error bodies, tenant IDs, resource IDs, path fragments, raw provider snippets, and exception text.
  - [x] Implement a small scanner that accepts a persona label, channel label, and rendered artifact string, then fails with the persona/channel name and the matched sentinel class. Keep diagnostics metadata-only; do not dump the whole body if it contains a sentinel.
  - [x] Add a negative meta-test that passes a deliberately leaking string and asserts the scanner fails and names the leaking channel.

- [x] Prove store-key partitioning and projection isolation (AC: 5)
  - [x] Add direct store tests for `GovernedOperationView.KeyFor(...)`, `InMemoryGovernedOperationProjectionStore`, and the projection handler path to prove `{tenant}:governed-operation:{noteId}` is the only key shape used.
  - [x] Seed the same `noteId` under `tenant-alpha` and `tenant-beta` where possible and prove each tenant reads only its own view.
  - [x] Deliver duplicate/out-of-order projection notifications with tenant-specific envelopes and assert a foreign tenant notification cannot overwrite or advance the caller tenant's view.
  - [x] Treat DAPR-backed store validation as build/code inspection plus existing Tier-3 path unless the live runtime is explicitly available; do not make the main suite depend on Redis/DAPR.

- [x] Preserve architecture and dependency guardrails (AC: 6, 8)
  - [x] Do not create production `.Cli`, `.Mcp`, `.Workers`, M365, or AI adapter projects in this story.
  - [x] Do not widen `internal` gateway stages to `public`; if Conformance.Tests needs access, use the already-established `InternalsVisibleTo` path from Story 1.11.
  - [x] Do not add inline package versions; any new package gets a central `Directory.Packages.props` entry and a bare `<PackageReference>`.
  - [x] Re-run the compiled xUnit v3 binaries directly because VSTest `dotnet test` is sandbox-blocked in this workspace.

- [x] Verify and document results (AC: 8)
  - [x] Build the full solution with warnings-as-errors.
  - [x] Run Conformance.Tests, Server.Tests, Architecture.Tests, and IntegrationTests compiled binaries; broaden to the full ChatBot sweep if any shared source code changes.
  - [x] Record exact commands and counts in this story's Dev Agent Record.
  - [x] Update this story status through the normal dev workflow only after implementation and review gates pass.

## Dev Notes

### Source Artifact Analysis

Epic 1 is the safety floor for the first governed command. Story 1.12 is not feature work; it is a mechanical release-gate harness proving tenant isolation before later epics add real candidate lists, evidence panes, file surfaces, cursors, admin actions, service clients, CLI/MCP adapters, mailbox events, and AI actors. The epic acceptance criteria name nine actor types and require zero leakage across candidates, evidence, files, pagination cursors, and error bodies. [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: First Safe Governed Action & Command Spine; _bmad-output/planning-artifacts/epics.md#Story 1.12: Cross-tenant isolation harness]

Architecture requires tenant isolation by construction at command, query, store, projection, log, error-body, cache, cursor, and vector layers; tenant IDs come from authenticated claims only, never request body or surface input. Tier 2/3 tests must inspect durable state-store end-state, never only HTTP/exit/MCP response codes. [Source: _bmad-output/planning-artifacts/architecture.md#Project Context Analysis; _bmad-output/planning-artifacts/architecture.md#Authentication & Security; _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]

There are no separate PRD/UX shards in the active planning artifact patterns. The whole `epics.md` and `architecture.md` are the governing inputs. No UX-specific implementation is required for this test-harness story.

### Current Implementation State

Current M0 real surfaces and stores are:

- `CommandGateway` with stages `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> lifecycle-validation -> pre-commit-audit -> dispatch -> post-commit-audit`. Tenant binding is handled by `ClaimsTenantBindingStage` from `eventstore:tenant` / `tenant` claims, and it rejects command body / scoped-ID tenant targets that do not match the bound tenant. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs]
- HTTP command endpoint `POST /api/v1/commands`, operation-status endpoint `/api/v1/operations/{operationId}`, audit-history endpoint `/api/v1/operations/{operationId}/audit-history`, and governed-operation projection read `/api/v1/governed-operations/{noteId}`. These already contain safe-not-found behavior that Story 1.12 should generalize and harden. [Source: src/Hexalith.ChatBot.Server/Program.cs]
- Tenant-partitioned governed-operation read model keyed by `GovernedOperationView.KeyFor(tenantId, noteId)` -> `{tenant}:governed-operation:{noteId}` with in-memory and DAPR-backed stores. [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs; src/Hexalith.ChatBot.Server/Projections/InMemoryGovernedOperationProjectionStore.cs; src/Hexalith.ChatBot.Server/Projections/DaprGovernedOperationViewStore.cs]
- Existing endpoint tests already prove some cross-tenant collapse for command, status, and audit-history. Do not delete or weaken them; extract or mirror helper patterns if needed. [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs]

### Reuse Before Inventing

Story 1.11 created reusable in-process harness assets in `tests/Hexalith.ChatBot.Conformance.Tests/Harness/`: `ISurfaceArm`, `UiSurfaceArm`, `CliSurfaceArm`, `McpSurfaceArm`, `RecordingDispatcher`, `RecordingAuditWriter`, `DifferentialOracle`, and `GovernedCommandConformanceHarness`. For Story 1.12, extend or parallel these harness concepts for isolation; do not fork a second gateway builder with subtly different stores or redaction behavior unless a local helper is intentionally shared. [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs; tests/Hexalith.ChatBot.Conformance.Tests/Harness/ConformanceGatewayDoubles.cs; tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs]

Story 1.10 added NetArchTest rules that prevent adapters from referencing server governance stages and prevent stage replication. Those rules are a guardrail for Story 1.12's surface shims. If a test shim needs internals, prefer Conformance.Tests' existing `InternalsVisibleTo` from Story 1.11; do not make gateway seams public. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md; src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj]

The single current real command is `RecordGovernedNote`. It is useful for same-tenant positive controls and projection-store tests, but cross-tenant command-body mismatch can use a test-only command with `TenantId`, scoped identifiers, candidate/evidence/file/cursor sentinel fields, and `IChatBotCommand` so `ClaimsTenantBindingStage` sees the tenant mismatch. Keep this command in tests only.

### Actor Persona Guidance

Use the nine required actor types as explicit harness personas. Suggested mapping:

- `human-user`: authenticated user, surface `ui`, actor label `human`.
- `tenant-admin`: authenticated user with role metadata only, surface `ui`.
- `project-admin-owner`: authenticated user with project-owner metadata only, surface `ui`.
- `service-client`: service principal style `sub`, surface `api` or service label.
- `cli-client`: Story 1.11 CLI shim, surface `cli`.
- `mcp-client`: Story 1.11 MCP shim, surface `mcp`.
- `background-worker`: worker shim, surface `worker`.
- `m365-event`: mailbox/event shim, surface `mailbox`.
- `ai-actor`: AI shim, surface `ai`.

M0 does not have real RBAC for these personas. The point is not to simulate permissions broadly; the point is to prove that no declared actor category can cross tenant scope, and that all such attempts fail closed before durable state or leaked body text.

### Testing Requirements

- Use xUnit v3 `3.2.2` and Shouldly `4.3.0`; no new assertion library.
- No latest-package research is needed for this story: use the repository-pinned versions in `Directory.Packages.props` and do not upgrade Fluent UI, DAPR, Aspire, xUnit, or MVC testing packages as part of this harness.
- Prefer Conformance.Tests for the actor matrix and leakage scanner. Use Server.Tests if endpoint seeding is easier there; either way, name the harness artifacts so later stories can plug real Epic 2/3 channels into the same gate.
- Every negative test must include at least one positive control proving the foreign resource was actually seeded or the store/key existed.
- Do not compare only status codes. Compare store records, dispatch counts, audit captures, projection reads, and normalized response bodies.
- Add non-vacuity tests. Prior stories found vacuous passes in both architecture discovery and oracle include-set coverage; Story 1.12 must guard its matrix and scanner inputs the same way. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results; _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md#QA Results]
- VSTest `dotnet test` is sandbox-blocked (`Permission denied` socket). Build with `dotnet build ...` and run compiled xUnit v3 binaries directly, as in Stories 1.10 and 1.11.

### Out of Scope

- No production CLI/MCP/Workers/M365/AI adapter projects.
- No new product endpoints for candidates, evidence, files, or pagination cursors. Use sentinel channels and test seams now; downstream feature stories replace those with real endpoints.
- No broad RBAC implementation or policy editor.
- No `internal` to `public` weakening of gateway stages.
- No recursive submodule initialization or nested submodule work.
- No hand-editing generated client files unless a real OpenAPI change is intentionally made and regenerated.

### Project Structure Notes

- New conformance harness code should live under `tests/Hexalith.ChatBot.Conformance.Tests/Harness/` and test facts under `tests/Hexalith.ChatBot.Conformance.Tests/`.
- If HTTP endpoint tests require `WebApplicationFactory<Program>` in Conformance.Tests, add `Microsoft.AspNetCore.Mvc.Testing` as a bare package reference only. The version already exists centrally in `Directory.Packages.props`.
- Shared fixture data can live under `tests/fixtures/` if multiple test projects consume it. Keep fixture contents metadata-only and intentionally sentinel-based.
- Existing dirty worktree item `_bmad-output/story-automator/orchestration-1-20260530-160445.md` is unrelated automation output; do not revert or overwrite it.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.12: Cross-tenant isolation harness]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]
- [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md]
- [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md]
- [Source: _bmad-output/implementation-artifacts/1-11-differential-conformance-harness.md]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs]
- [Source: src/Hexalith.ChatBot.Server/Program.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/]
- [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs]
- [Source: tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs]
- [Source: Directory.Packages.props; Directory.Build.props; tests/Directory.Build.props]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.Folders/_bmad-output/project-context.md]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: Hexalith.Tenants/_bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- 2026-05-31: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> succeeded, 0 warnings, 0 errors.
- 2026-05-31: `dotnet tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests.dll -noLogo -noColor` -> 47 total, 0 failed, 0 skipped.
- 2026-05-31: `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -noLogo -noColor` -> 113 total, 0 failed, 0 skipped.
- 2026-05-31: `dotnet tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests.dll -noLogo -noColor` -> 33 total, 0 failed, 0 skipped.
- 2026-05-31: `dotnet tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests.dll -noLogo -noColor` -> 4 total, 0 failed, 2 skipped (Tier-3 live DAPR/Docker legs self-skipped because `HEXALITH_CHATBOT_TIER3` was not enabled).
- 2026-05-31: Broader compiled ChatBot sweep after shared server read-path change:
  AppHost.Tests 3/0 failed, Aspire.Tests 2/0 failed, Client.Tests 13/0 failed, Contracts.Tests 66/0 failed, ServiceDefaults.Tests 3/0 failed, Testing.Tests 1/0 failed, UI.Tests 8/0 failed.

### Completion Notes List

- Added the nine-persona cross-tenant actor matrix and non-vacuity gates for persona coverage and leakage-channel coverage.
- Added mutating command isolation tests that submit tenant-mismatched and stale/unresolved tenant-context commands through the real `CommandGateway` lane and assert zero durable work before denial.
- Added the shared Story 1.12 leakage corpus and scanner, including reserved candidate/evidence/file/cursor channels plus negative controls for leaking artifacts and empty scans.
- Added HTTP read-surface isolation coverage for operation status, audit history, and governed-operation projection reads, including same-tenant positive controls and foreign/unknown/malformed/stale/missing/ambiguous/unsafe context collapse.
- Added store partitioning tests for governed-operation key shape, same logical note IDs under multiple tenants, duplicate/stale notifications, and foreign notification isolation.
- Added a read-surface `ReadDenialReason` normalization so an authenticated-but-unresolved tenant on a read maps to `safe_not_found` while unauthenticated reads keep `authentication_denied`. NOTE (review-corrected): this is a behaviour-preserving defense-in-depth guard, not a behaviour change — `ChatBotProblemDetailsFactory.AuthorizationCatalogCode` already renders both `tenant_missing` and `safe_not_found` through the same `authorization_denied` catalog entry, so the rendered 403 body is byte-identical with or without it. It pins the read-boundary invariant so a future catalog change that gave `tenant_missing` its own surface text could not start distinguishing the unresolved-tenant case.

### File List

- `_bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantActorMatrixTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantIsolationNegativeControlTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantLeakageScannerTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantMutatingCommandIsolationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantStorePartitioningTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantIsolationHarness.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageCorpus.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageScanner.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationActorMatrix.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationHttpHost.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationTestCommands.cs`
- `tests/fixtures/story-1-12-cross-tenant-leakage-corpus.json`

### Change Log

- 2026-05-31: Implemented Story 1.12 cross-tenant isolation harness, tightened read-path unresolved tenant collapse, and validated the required release gates plus the broader compiled ChatBot test sweep.
- 2026-05-31: QA automation pass added explicit stale tenant-context coverage to the mutating command and HTTP read-surface isolation harness, then revalidated build, Conformance, Server, Architecture, and Integration gates.
- 2026-05-31: Senior Developer Review (AI) — adversarial review of all 8 ACs and File List vs git. Outcome: Approve. Auto-fixed 2 findings (documented the `ReadDenialReason` defense-in-depth no-op in `Program.cs` and corrected the overstated Completion Note; added the missing own-tenant positive control to the audit-history isolation test). Re-ran build (0/0) + Conformance (47/0/0) + Server (113/0/0) green. No CRITICAL/HIGH issues.
- 2026-06-10: Senior Developer Review (AI) re-run (story-automator review workflow) against the current tree. Outcome: Approve. All 8 ACs re-verified against the real implementation; build 0/0 and all four compiled binaries green (Conformance 87/0/0, Server 1510/0/0, Architecture 39/0/0, Integration 18 total / 0 failed / 2 Tier-3 self-skipped). Counts exceed the 05-31 record because later stories (1.13, Epic 2/9 surfaces) advanced the same harness. No CRITICAL/HIGH/MEDIUM issues; no code changes required.

## Senior Developer Review (AI)

**Reviewer:** Jerome (AI adversarial review) · **Date:** 2026-05-31 · **Outcome:** ✅ Approve (0 CRITICAL, 0 HIGH)

### Scope & method

Validated every Acceptance Criterion and every `[x]` task against the actual implementation, cross-referenced the story File List against `git` reality, independently re-ran the AC8 release gate, and inspected the production projection stores and message-catalog mapping for real (not assumed) isolation. Build and the four compiled xUnit v3 binaries were executed directly (VSTest is sandbox-blocked).

### Verified gate (independently re-run)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → succeeded, **0 warnings / 0 errors**.
- Conformance **47** / Server **113** / Architecture **33** total — 0 failed, 0 skipped. Integration **4** total, 0 failed, 2 self-skipped (Tier-3 env-gated; `HEXALITH_CHATBOT_TIER3` unset). Matches the Dev Agent Record exactly.

### AC-by-AC verdict

- **AC1 (nine-actor non-vacuous matrix):** PASS. Exactly nine personas, each with an explicit `ChatBotSurfaceOrigin`, each producing an executed fail-closed case; non-vacuity guards on personas and on required leakage channels.
- **AC2 (mutating paths fail closed before durable work):** PASS. Every persona × 7 tenant-context variants runs through the **real** `CommandGateway`/`ClaimsTenantBindingStage`; asserts metadata-only denial, single authorization-failure fact, and dispatch/idempotency/audit-envelope/operation-status counters at zero — inspecting captures/stores, not status codes. The tenant-mismatch denial reason comes from the real binding stage (`tenant_mismatch`), so it is non-vacuous.
- **AC3 (read surfaces collapse to indistinguishable safe denial):** PASS. Foreign/unknown/malformed/missing/ambiguous/stale/unsafe-tenant reads compared on status + correlation header + full body equality (not status alone) through the real `WebApplicationFactory<Program>`, with the body routed through the leakage gate.
- **AC4 (leakage corpus covers all channels):** PASS. Embedded (not copy-to-output) corpus with candidate/evidence/file/cursor/error-body sentinels reserved now; the mutating probe genuinely carries those sentinels so the denial is proven not to echo them.
- **AC5 (store partitioning proven, not assumed):** PASS. Confirmed by code inspection that **both** `InMemoryGovernedOperationProjectionStore` and `DaprGovernedOperationViewStore` key strictly via `GovernedOperationView.KeyFor` → `{tenant}:governed-operation:{noteId}`; behavioural tests prove same-note-id isolation across two tenants and that foreign (higher/duplicate/out-of-order) notifications never advance the caller's view.
- **AC6 (reuse parity harness, no stage replication):** PASS. Real gateway + real tenant-binding/authorization-failure stages reused; only pass-through stubs and recording doubles added; no production `.Cli/.Mcp/.Workers/M365/AI` project created; NetArchTest (Architecture 33) green.
- **AC7 (negative controls prove the harness can fail):** PASS. Tenant-ignoring vulnerable store + deliberately leaking body + empty-sentinel-set vacuity guard + missing-persona completeness guard all covered.
- **AC8 (build/regression gates green):** PASS (re-verified above).

### Findings & dispositions

- **[MEDIUM — auto-fixed] Overstated production-change claim.** `Program.cs`'s `ReadDenialReason` is a behavioural no-op today: `AuthorizationCatalogCode` already collapses `tenant_missing` and `safe_not_found` to the identical `authorization_denied` 403 body, and the reason code never reaches the body. The original Completion Note/Change Log described it as a behaviour change. Fix applied: kept the code as explicit defense-in-depth, added a clarifying comment in `Program.cs`, and corrected the Completion Note.
- **[LOW — auto-fixed] Asymmetric positive control.** The audit-history isolation test lacked a self-contained own-tenant 200 read (its siblings had one; the path was only transitively covered via the shared status store). Added an own-tenant audit-history positive read.
- **[LOW — not auto-fixed, flagged] Undocumented submodule pointer bump.** `Hexalith.Folders` moved `fe2e1de → 1f8cd09` since the story baseline (`d4b962f`); it is not in the File List and submodule work is Out of Scope. Not reverted: CLAUDE.md prohibits submodule operations and the story marks it out of scope. **Action for author:** confirm this is intended repo drift unrelated to Story 1.12.
- **[LOW — observation, no change] AC2 "no governed-operation projection"** is asserted transitively (dispatch count 0; the mutating lane wires no projection store). Already explained in the harness comment; acceptable for M0.

### Re-validation after fixes

Build **0/0**, Conformance **47/0/0**, Server **113/0/0** — all green; the Conformance count is unchanged because the new assertions were added inside the existing audit-history fact.

_Reviewer: Jerome on 2026-05-31_

---

## Senior Developer Review (AI) — re-run 2026-06-10

**Reviewer:** Jerome (story-automator review workflow) · **Date:** 2026-06-10 · **Outcome:** ✅ Approve (0 CRITICAL, 0 HIGH, 0 MEDIUM)

### Scope & method

Independent adversarial re-review of all 8 ACs and every `[x]` task against the current tree (not the 05-31 baseline), cross-referenced the File List against `git` reality, independently re-ran the AC8 gate, and re-read the real `ClaimsTenantBindingStage`, `Program.cs` read endpoints, the leakage corpus/scanner, and the negative controls. The branch history was rebased since 05-31 (HEAD is labelled `story-1.11` but its tree already contains the 1.12 harness plus later-story surfaces); all 14 File List files exist and the working tree matches `HEAD` exactly — only `_bmad-output/` automation artifacts are dirty.

### Verified gate (independently re-run)

- `dotnet build Hexalith.ChatBot.slnx -m:1 /nr:false` → succeeded, **0 warnings / 0 errors**.
- Conformance **87/0/0**, Server **1510/0/0**, Architecture **39/0/0**, Integration **18 total / 0 failed / 2 self-skipped** (Tier-3 env-gated; `HEXALITH_CHATBOT_TIER3` unset, honest skip message). Counts exceed the 05-31 record (47/113/33/4) because later stories advanced the same harness/server — not a regression.

### AC-by-AC verdict

- **AC1–AC8:** all PASS, re-confirmed against the live code. Reason codes the mutating tests assert (`TenantMismatch` for body/scoped-id/nested-JSON/stale target mismatch; `TenantMissing` for missing/ambiguous/unsafe claims) are emitted by the **real** `ClaimsTenantBindingStage`, so they are non-vacuous. The leakage scanner refuses an empty sentinel set, the corpus is embedded (exact `LogicalName`, never copy-to-output), and the negative-control store genuinely leaks so the isolation assertion is proven discriminating. AC8 guardrails hold: no inline package versions; `Microsoft.AspNetCore.Mvc.Testing` is a bare reference centrally pinned at `10.0.8`.

### Findings & dispositions

- No new CRITICAL/HIGH/MEDIUM issues. The two real issues from the 05-31 pass remain correctly fixed.
- **[LOW — observation, no change]** The 05-31 Debug Log References record point-in-time counts (47/113/33/4); they are an accurate historical record for the story's scope at that date and are intentionally left untouched (the current counts live in this re-run entry and in `tests/test-summary-story-1.12.md`).
- **[LOW — observation, no change]** AC2's "no governed-operation projection" remains asserted transitively (dispatch count 0; the mutating lane wires no projection store). Acceptable for M0, already documented in the harness.

No code changes were required; status remains **done** and sprint-status already records `1-12-cross-tenant-isolation-harness: done`.

_Reviewer: Jerome on 2026-06-10_
