---
baseline_commit: f321360
---

# Story 1.11: Differential-conformance harness

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a platform tester,
I want a differential-conformance harness that submits the **same semantic intent** through a UI arm and **thin in-test CLI/MCP shims** — each differing only by `ChatBotSurfaceOrigin` — and asserts an **identical event sequence + state-store end-state** (including the rejection and retry intents),
so that cross-surface parity is mechanically verified at M0 and any FR81a divergence surfaces as a defect *before* the real CLI/MCP surfaces ship in Epic 5 — proving Story 1.9's one governed command behaves identically regardless of surface.

## Acceptance Criteria

1. **Harness exists, surface-parametrized over the real command, no stage replication.** Given the existing `tests/Hexalith.ChatBot.Conformance.Tests` project and the one spine-allowlisted command `RecordGovernedNote(string NoteId) : IChatBotCommand`, when the differential-conformance harness submits the same semantic intent through a **UI arm** and **thin CLI and MCP test shims** (the arms differ **only** in the declared `ChatBotSurfaceOrigin` ∈ {`Ui`, `Cli`, `Mcp`} and how each parses its own input into the typed command), then every arm drives the **same shared pipeline** — `IChatBotClient.SubmitAsync(...)` / `CommandGateway.SubmitAsync(ChatBotCommandSubmission)` — replicating **no** gateway stage (the shims construct only an `IChatBotCommand`, honoring the Story 1.10 NetArchTest dependency/adapter-boundary rules), and **no** `Hexalith.ChatBot.Cli`/`.Mcp` production project is created (M0 uses in-test shims only). [Source: _bmad-output/planning-artifacts/epics.md#Story 1.11 (lines 866-874); _bmad-output/planning-artifacts/epics.md (line 338 — "exercised from M0 via thin CLI/MCP test shims so M1 parity debt surfaces early"); src/Hexalith.ChatBot.Contracts/Commands/RecordGovernedNote.cs; src/Hexalith.ChatBot.Client/IChatBotClient.cs#L9-14; src/Hexalith.ChatBot.Server/Gateway/Stages/ (IRiskClassifier/IApprovalGate/IAuditWriter/IIdempotencyStore are internal)]

2. **Identical admission event sequence across surfaces (origin is the only delta).** Given a successful submit of `RecordGovernedNote`, when the same intent runs through each surface arm, then the **ordered admission event sequence is identical** across arms — the `PreCommit` then `PostCommit` audit envelopes (both with `StateTransition == "Received->Proposed"`), the accepted lifecycle state `Proposed`, and the emitted domain event `GovernedNoteRecorded(NoteId)` — with equality computed over everything **except** the legitimately per-surface `surfaceOrigin` field and per-run-unique values (the minted `commandId`, ULIDs, timestamps such as `acceptedAt`/`recordedAt`/`lastUpdatedAt`). The `surfaceOrigin` delta is the **only** permitted difference, and each arm's audited origin is asserted to equal its declared origin (`ui`/`cli`/`mcp`). [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L56-68,L99-102 (stage order + PreCommit/PostCommit `Received->Proposed`); src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs#L219 (Proposed); src/Hexalith.ChatBot.Server/Operations/GovernedNoteRecorded.cs; src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs#L21-22 (SurfaceOrigin field); _bmad-output/planning-artifacts/epics.md#FR86 (line 163 — "equivalent input → same Command record after canonical normalization")]

3. **Identical durable state-store end-state across surfaces, read from the store (never the status code).** Given the same successful intent, when each arm completes, then the **durable state-store end-state is identical** across arms — the projected `GovernedOperationView` (`noteId` matches, `sourceVersion == 1`, `redactionState == "metadata_only"`, `sourceProvenance == "governed-command"`, plus `schemaVersion`/`retentionClass`/`derivationKernelVersion`; tenant-partitioned key `{tenant}:governed-operation:{noteId}`) plus the coarse-idempotency / operation-status records — and this end-state is **read from the state store** (`IGovernedOperationProjectionStore.GetAsync(tenant, noteId)` in-process, or `GET /api/v1/governed-operations/{noteId}` live), **never inferred from the HTTP 202 / CLI exit / MCP response code**. [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs#L19-44,L53-60; src/Hexalith.ChatBot.Server/Projections/IGovernedOperationProjectionStore.cs#L9-22; tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs#L141-144; tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs#L67-91; _bmad-output/planning-artifacts/epics.md#Story 1.11 AC2 (line 878); _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines ("Tier 2/3 inspect state-store end-state, never just HTTP/exit codes")]

4. **Rejection-intent parity.** Given a **rejection intent** — (a) a re-record of an already-recorded `NoteId` → fine-idempotency `GovernedNoteAlreadyRecordedRejection(NoteId) : IRejectionEvent` (returned, never thrown), and (b) a fail-closed rejection (e.g. a non-allowlisted command or unauthenticated submit) → a catalog-backed **redacted** problem — when submitted through each surface arm, then every arm yields an **identical** rejection outcome: the same rejection event / problem `category`+`code`+`reasonCode`, the same absence of additional durable effect (`GovernedOperationView.SourceVersion` unchanged; no extra dispatch; no state mutation on a fail-closed path), differing only in `surfaceOrigin`. The rejection is compared as a **first-class event/record in the sequence**, never as a bare error code. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs#L25-41; src/Hexalith.ChatBot.Server/Operations/GovernedNoteAlreadyRecordedRejection.cs; tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs#L33-49; _bmad-output/planning-artifacts/epics.md (line 305 — "rejections-as-events"); _bmad-output/planning-artifacts/architecture.md#Format Patterns (RFC 9457 metadata-only problem; redaction consistent across UI/CLI/MCP)]

5. **Retry / idempotent-replay-intent parity.** Given a **retry intent** — an equivalent duplicate submission of the same intent — when submitted through each surface arm, then every arm **replays the prior outcome without re-dispatching**: `DispatchCount == 1`, exactly one coarse-idempotency record, and `GovernedOperationView.SourceVersion` stays `1` (exactly one durable effect); the replayed outcome (lifecycle/accepted response and end-state) is **identical** across arms except `surfaceOrigin`. [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L72-106 (EquivalentDuplicateShouldReplayPriorOutcomeAndNeverDispatchAgain), #L268-284 (StateStoreEquivalentRepeatShouldMatchSingleSubmitEndState); tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs#L43-53; tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs#L148-152; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs#L105-124]

6. **AC2-discipline guard — state-store inspection, proven non-vacuous (FR86: divergence = invariant violation, not tolerance).** Given the rule that every Tier 2/3 conformance assertion reads state-store end-state and that "test failure is an invariant violation, not a tolerance threshold", when the harness runs, then (a) **no** test in the new harness asserts an outcome from only an HTTP 202 / CLI exit / MCP response code — every assertion reads the projected view, store records, or audit-envelope sequence; and (b) the differential oracle is proven **non-vacuous** by a committed, non-destructive **negative meta-test** that deliberately perturbs one arm's captured outcome (e.g. a different `NoteId`, a mutated end-state field, or an injected extra event) and asserts the oracle **fails and names the diverging field** — so a silently no-op equality (a vacuous pass) cannot give false confidence. [Source: _bmad-output/planning-artifacts/epics.md#FR86 (line 163); _bmad-output/planning-artifacts/epics.md (line 340); _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results (the "vacuous pass is a false pass" failure mode + non-vacuity guards)]

7. **Build-green, no regression, swappable shim for Epic 5.** Given the platform gates, when the harness is added, then: the full solution builds **0 warnings / 0 errors** under warnings-as-errors with **no inline package versions** (any new package version lands in `Directory.Packages.props`; the csproj carries only a bare `<PackageReference>`); the existing `ContractSpineOracleTests` and `DaprAccessControlConformanceTests` (and the full ChatBot test suite) stay **green** when run via the **compiled xUnit v3 in-process binary** (VSTest `dotnet test` is sandbox-blocked); the Tier-3 leg stays **env-gated** on `HEXALITH_CHATBOT_TIER3` and self-skips when the runtime is absent (this clause is satisfied by the **existing** `TrivialGovernedCommandAspireE2eTests` staying green — adding a *new* cross-origin Tier-3 arm, task 8, is a stretch goal, not an AC requirement); and the surface-driver is a **swappable abstraction behind a common assertion engine** so Epic 5 Story 5.4 can replace the M0 shims with the real `.Cli`/`.Mcp` adapters **without changing the oracle**. [Source: _bmad-output/planning-artifacts/epics.md (lines 1716-1718, Story 5.4 — "replacing the M0 shims (FR86 extension)"; line 563 — "verified not enforced by tests"); Directory.Packages.props; _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#Debug Log References (sandbox `dotnet test` block; compiled-binary sweep)]

## Tasks / Subtasks

- [x] Wire the `Conformance.Tests` project to drive the real pipeline in-process (AC: 1, 7)
  - [x] Add `<ProjectReference>`s to `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj` for `Hexalith.ChatBot.Server`, `Hexalith.ChatBot.Contracts`, `Hexalith.ChatBot.Client`, and `$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts` (for `DomainResult`/event-payload types). Today this csproj has **zero** ProjectReferences — both existing tests only read files off disk. Mirror how Story 1.10 added `Architecture.Tests` references and how `Server.Tests`/`IntegrationTests` reference Server. [Source: tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj; _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#File List (line 186); tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj]
  - [x] For the **gateway-level in-process lane** (recommended primary), add `<InternalsVisibleTo Include="Hexalith.ChatBot.Conformance.Tests" />` to `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` (it currently lists only `Server.Tests` + `IntegrationTests`) so the test can construct `internal CommandGateway` / `ChatBotCommandSubmission` and resolve the `internal IGovernedOperationProjectionStore`. This is the **only** `src`-tree behavioral-surface change and it is test-enablement, not runtime behavior. If you instead drive everything through the public HTTP surface (`WebApplicationFactory<Program>` + `GET /api/v1/governed-operations/{noteId}`), `Program` is also internal → IVT is still required. [Source: src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj#L10-11; src/Hexalith.ChatBot.Server/Projections/IGovernedOperationProjectionStore.cs#L9 (internal); src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs#L12 (internal sealed)]
  - [x] If the HTTP/`WebApplicationFactory` arm is used, add the `Microsoft.AspNetCore.Mvc.Testing` bare `<PackageReference>` (version already central via `Server.Tests`); do **not** add an inline version. No new third-party package is otherwise needed — reuse xUnit v3 (3.2.2) + Shouldly (4.3.0). [Source: tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj; Directory.Packages.props]
- [x] Build the surface-agnostic semantic-intent + swappable surface-driver abstraction (AC: 1, 7)
  - [x] Define one **semantic intent** = `RecordGovernedNote(noteId)` plus an identical context (`ClaimsPrincipal` with `sub=actor-alpha`, `eventstore:tenant=tenant-alpha`; one `CorrelationId`, one `TaskId`). [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs#L919-938 (TestPrincipalStartupFilter); tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L20-26 (fixed ULIDs)]
  - [x] Define an `ISurfaceDriver`/`SurfaceArm`-style abstraction whose **only** per-surface variation is the declared `ChatBotSurfaceOrigin` (and, for the real-wire arm, how raw input is parsed into the typed command). Provide three arms: `Ui`, `Cli`, `Mcp`. Keep the abstraction **swappable** so Epic 5 Story 5.4 can substitute real `.Cli`/`.Mcp` adapters behind the same interface without touching the assertion engine. The arm MUST construct only `IChatBotCommand` and submit through the shared pipeline — never replicate a stage. [Source: _bmad-output/planning-artifacts/epics.md (lines 1716-1718); src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs#L25-37 (the canonical UI arm: `SubmitAsync(command, origin: Ui)`); _bmad-output/planning-artifacts/architecture.md#Pattern Examples (UI vs CLI build the same command → identical event)]
  - [x] Lift the in-process gateway harness assets you need into `Conformance.Tests` (or a shared internal test-support file). **Distinguish two kinds of dependency — do not conflate them:** (a) the **private test doubles** in `CommandGatewayTests.cs` — the `Gateway(...)` builder, `RecordingDispatcher` (`.DispatchCount`), `RecordingAuditWriter` (`.Envelopes`), `PermissiveSpineCommandAllowlist`, `FixedClock` — are `private` to that test file; **duplicate or promote** them into a shared test-support file. (b) `InMemoryCoarseIdempotencyStore` (`.Records`) and `InMemoryOperationStatusStore` are `internal` **production** classes in `Server.Gateway.Idempotency`/`Server.Gateway.Status` — **consume them directly via the new IVT, do NOT duplicate** them (duplicating would diverge behavior). Use the `RecordingAuditWriter` from `CommandGatewayTests.cs` (it captures `.Envelopes`) — the `IdempotencyStateStoreIntegrationTests` copy is a **no-op stub with no `.Envelopes`** and cannot back the event-sequence oracle. (Note for 1.12/1.13: a shared shim/fixture may later be extracted; design for extraction but do not over-build it here.) [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L978-1302 (private doubles); src/Hexalith.ChatBot.Server/Gateway/Idempotency/InMemoryCoarseIdempotencyStore.cs; src/Hexalith.ChatBot.Server/Gateway/Status/InMemoryOperationStatusStore.cs (internal production); tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs#L72-110 (no-op RecordingAuditWriter stub — do not copy)]
- [x] Implement the differential equality oracle (AC: 2, 3, 6)
  - [x] Capture, per arm, an **outcome record** = (ordered admission audit-envelope sequence from `RecordingAuditWriter.Envelopes`: phase + `StateTransition` + `decision`/`reasonCode`/`outcome`; the accepted lifecycle state; the emitted domain event/rejection) **+** the durable state-store end-state (the `GovernedOperationView` and the coarse-idempotency/operation-status records). [Source: src/Hexalith.ChatBot.Server/Audit/IAuditHistoryReader.cs; tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L99-106; src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs#L19-44]
  - [x] Implement the equality projection with an explicit **exclude** set — `surfaceOrigin`, minted `commandId`, all freshly-minted ULIDs, and timestamps (`acceptedAt`/`recordedAt`/`lastUpdatedAt`) — and an explicit **include** set — ordered audit phases + `stateTransition` (`Received->Proposed`), the domain event/rejection identity, lifecycle (`Proposed`), `GovernedOperationView` derived-record fields + `sourceVersion`. Assert each arm's audited `surfaceOrigin` equals its declared origin (`ui`/`cli`/`mcp`) — the single allowed delta. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs#L21-22; src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigins.cs#L8-47; _bmad-output/planning-artifacts/architecture.md#Communication Patterns]
  - [x] AC2-discipline: ensure **every** assertion reads the captured outcome record (state-store / envelope sequence), **never** a bare `result.IsAccepted` / 202 / exit code. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.11 AC2 (line 878)]
- [x] Add the success-intent conformance tests across UI/CLI/MCP arms (AC: 1, 2, 3)
  - [x] A test that submits the success intent through each arm and asserts the captured outcome records are **equal under the projection** (event sequence + state-store end-state), with only `surfaceOrigin` differing. For the durable `GovernedOperationView` end-state use the `GovernedNoteDurableChainTests` projection lane (`IGovernedOperationProjectionStore.GetAsync(tenant, noteId)`) or the live `GET /api/v1/governed-operations/{noteId}`. [Source: tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs#L67-91; tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs#L141-144]
- [x] Add the rejection-intent conformance tests across arms (AC: 4)
  - [x] Re-record of an already-recorded `NoteId` → assert each arm produces `GovernedNoteAlreadyRecordedRejection` and **no** extra durable effect (`SourceVersion` unchanged), identical except `surfaceOrigin`. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs#L25-41; tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs#L33-49]
  - [x] A fail-closed rejection (non-allowlisted command and/or unauthenticated submit) → assert each arm returns the **same** catalog-backed redacted problem (`category`/`code`/`reasonCode`) and **zero** durable state mutation, read via the safe-not-found state read (never confirming existence). Keep the assertion metadata-only; run a leakage sentinel (`ShouldNotContain("tenant-alpha")`). [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md (allowlist 403 redacted problem; auth 401 fail-closed); tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs#L110-116,L154-158]
- [x] Add the retry/replay-intent conformance tests across arms (AC: 5)
  - [x] Equivalent duplicate submit per arm → assert `DispatchCount == 1`, exactly one coarse-idempotency record, `GovernedOperationView.SourceVersion == 1`, and the replayed outcome equal across arms except `surfaceOrigin`. [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L72-106,L268-284; tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs#L43-53]
- [x] Add the non-vacuity negative meta-test for the oracle (AC: 6)
  - [x] A committed, non-destructive test that feeds the oracle two outcome records that **differ in an in-scope field** (e.g. a different `NoteId` or a perturbed `sourceVersion`) and asserts the oracle reports **not equal** and **names the diverging field**. This proves the equality is not silently no-op (the same "a silently-misconfigured rule is a false pass" guard Story 1.10's QA pass added). Do **not** introduce a real cross-surface divergence in production code. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results; _bmad-output/planning-artifacts/epics.md#FR86 (line 163)]
- [x] (Optional, env-gated) Extend the Tier-3 Aspire E2E across origins (AC: 3, 7)
  - [x] If feasible without flakiness, parametrize the live `TrivialGovernedCommandAspireE2eTests` flow (or add a sibling) so the same fresh-ULID intent is submitted with `origin=ui` then `origin=cli`/`origin=mcp` against the real DAPR topology, asserting identical projected `GovernedOperationView` end-state. Keep the `Assert.SkipUnless(Tier3RuntimeIsAvailable())` gate (`HEXALITH_CHATBOT_TIER3=1` + docker + dapr). Use **fresh per-run ULIDs**, explicit-timeout polling, **no `Thread.Sleep`**. This is a stretch leg — the Tier-2 in-process arms satisfy AC1–AC6 on their own; do not block the story on Tier-3 if the sandbox cannot run it. [Source: tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs#L40-63,L67-74,L337-353; user memory tier3-live-dapr-run.md]
- [x] Verify locally (AC: all)
  - [x] `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false`
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — warnings-as-errors, no inline package versions; full solution 0 warnings / 0 errors.
  - [x] Run the compiled xUnit v3 binary directly (VSTest `dotnet test` is sandbox-blocked — socket `Permission denied`): `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests`, then a full ChatBot test sweep to prove no regression. Record exact commands + counts in Debug Log References. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#Debug Log References]

## Dev Notes

### Implementation Intent

This is a **test-only, behavior-preserving** story. It builds the **runtime** half of FR81a parity enforcement — the *differential-conformance harness* — which complements Story 1.10's **static** half (NetArchTest dependency/adapter-boundary fitness). 1.10 proves an adapter *cannot structurally* replicate a stage; **1.11 proves the same semantic intent produces the same events + state-store end-state regardless of surface.** Do **not** rebuild 1.10's NetArchTest checks here. [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns ("Parity enforced by construction + a differential-conformance harness, not by aspiration"); _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#Scope Boundary at M0]

The only edits outside `tests/` are: (1) adding `Hexalith.ChatBot.Conformance.Tests` to `Server`'s `InternalsVisibleTo` (test-enablement, not runtime behavior), and (2) — only if a package is added — a `Directory.Packages.props` version entry. No `src/**/*.cs` runtime behavior changes; no `internal`→`public`, no gateway-stage reordering, no DI changes.

### The two-layer outcome model (read carefully — this is the core design)

A surface arm's observable "outcome" has **two layers**, and the harness must compare both:

1. **Admission event sequence** (where `surfaceOrigin` actually appears): the ordered `RecordingAuditWriter.Envelopes` — `PreCommit` then `PostCommit`, each carrying `StateTransition == "Received->Proposed"`, `decision`/`reasonCode`/`outcome`, and the `surfaceOrigin`. Plus the accepted lifecycle (`Proposed`) and the emitted domain event/rejection. This is captured at the **gateway** level (the `CommandGatewayTests`/`IdempotencyStateStoreIntegrationTests` pattern). **`surfaceOrigin` lives here** — it is the one field that legitimately differs per arm.
2. **Durable projection end-state** (`GovernedOperationView`): materialized from the published `GovernedNoteRecorded` domain event, which carries **no** surface origin (origin is provenance in the audit envelope, not in the domain event). So the projection end-state is **surface-invariant by construction** — identical across arms trivially. Capture it via the `GovernedNoteDurableChainTests` projection lane (`IGovernedOperationProjectionStore.GetAsync`) or the live `GET /api/v1/governed-operations/{noteId}`.

> Consequence: the **meaningful** cross-surface comparison is the **admission path** (audit envelopes + lifecycle + idempotency/operation-status records), where origin appears and must be the *only* delta. The projection end-state must still be asserted (AC3) but is expected identical because the domain event is origin-free. State both facts in tests so a reviewer sees the parity is by construction, not coincidence. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedNoteRecorded.cs (payload-only, no origin); src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs#L21-22 (origin in audit, not event); tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs]

### The equality projection (the exact include/exclude contract)

- **EXCLUDE** (legitimately varies — never assert equal across arms): `surfaceOrigin` (assert it equals each arm's *declared* origin instead), minted `commandId`, every freshly-minted ULID, and timestamps `acceptedAt`/`recordedAt`/`lastUpdatedAt`.
- **INCLUDE** (must be byte-identical across arms): ordered audit phases + `stateTransition` (`Received->Proposed`) + `decision`/`reasonCode`/`outcome`/`redactionDecision`; the domain event / rejection identity; lifecycle state (`Proposed`); and the `GovernedOperationView` derived-record fields (`noteId`, `schemaVersion`, `sourceProvenance`="governed-command", `derivationKernelVersion`, `redactionState`="metadata_only", `retentionClass`, `sourceVersion`=1).
- Getting the exclude set wrong is the single biggest implementation risk — a too-broad exclude makes the oracle vacuous (caught by AC6's negative meta-test); a too-narrow exclude makes it falsely fail on `commandId`/timestamps. [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs#L19-44; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs#L105-124,L219]
- **`Cli`/`Mcp` are declared-but-reserved at M0** (the `ChatBotSurfaceOrigin` enum doc marks them for later surfaces); this harness is their **first intentional exercise**, which is expected — not a misuse of reserved members. [Source: src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs]

### Surface shim design — two proven styles (pick the gateway-level lane as primary)

Two in-repo patterns already exist; both are valid "thin shims" and neither replicates a stage:

- **Gateway-level in-process lane (recommended primary).** Each arm is a different `ChatBotSurfaceOrigin` on an otherwise-identical `ChatBotCommandSubmission(Principal, Request{RecordGovernedNote}, CorrelationId, TaskId, origin)`, submitted through **one** `CommandGateway` built with shared in-memory stores (`RecordingDispatcher`/`RecordingAuditWriter`/`InMemoryCoarseIdempotencyStore`/`InMemoryOperationStatusStore`). Gives **direct, ordered** access to the audit-envelope event sequence + store records — the cleanest differential. Requires the Server IVT entry. Mirrors `IntegrationTests/IdempotencyStateStoreIntegrationTests.cs`. [Source: tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs#L25-70; tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L978-1302]
- **HTTP/`WebApplicationFactory<Program>` real-wire lane (use for the UI arm fidelity).** Drive `WebApplicationFactory<Program>` POSTing the camelCase wire body with `"origin":"ui"|"cli"|"mcp"`, then read end-state from `IGovernedOperationProjectionStore.GetAsync(tenant, noteId)` — **never** the HTTP status. Exercises the real `/api/v1/commands` contract + origin resolution at the boundary. Mirrors `Server.Tests/Operations/GovernedNoteDurableChainTests.cs` + `ServerBootstrapApiTests.cs` (`AuthenticatedFactory`, `RecordGovernedNoteRequest(noteId, origin, surfaceOriginHeader)`). [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs#L793-867; tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs#L52-92]

Whichever you pick, keep the **assertion engine independent of the driver** — that is the swappability contract for Epic 5 Story 5.4 ("replacing the M0 shims"). [Source: _bmad-output/planning-artifacts/epics.md (lines 1716-1718)]

### The one real command, and an epics-vs-code discrepancy to ignore

The single spine-allowlisted command is **`RecordGovernedNote(string NoteId) : IChatBotCommand`** with domain event `GovernedNoteRecorded` and rejection `GovernedNoteAlreadyRecordedRejection` (all in `.Server`/`.Contracts`). **Use these.** Note the discrepancy: `epics.md` (line 92, FR43) names the M0 allowlist as `Project.AppendConversationMessage`, but Story 1.9 **deliberately** chose `RecordGovernedNote` as the trivial governed command instead, and `ChatBotSpineCommandAllowlist` (the only production `ISpineCommandAllowlist`) allowlists exactly that. The code is ground truth — do not introduce `Project.AppendConversationMessage`. [Source: src/Hexalith.ChatBot.Contracts/Commands/RecordGovernedNote.cs; src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs; _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md#Completion Notes (line ~301 — why RecordGovernedNote over Project.AppendConversationMessage)]

### Canonical stage order + rejection/retry intents

Canonical executed stage order (the "event sequence" each arm must reproduce identically), verbatim from the passing Tier-1 test:
```
auth → tenant-bind → authorize → risk-classify → approval-gate →
coarse-idempotency → lifecycle-validation → pre-commit-audit → dispatch → post-commit-audit
```
`risk-classify`/`approval-gate` are PassThrough stubs at M0 (recorded, not enforced); the durable `fine-idempotency → execute → persist → publish → project` segment runs inside EventStore behind `AcceptedCommandDispatcher` (replaced by a `RecordingDispatcher` double in-process). The mandated intents to cover (FR86 "including rejection and retry intents"): success, **fine-idempotency rejection** (`GovernedNoteAlreadyRecordedRejection`), **fail-closed rejection** (non-allowlisted/unauthorized), and **retry/replay** (equivalent duplicate → one durable effect). [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L56-68; src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs#L13-44]

### Reusable harness assets (exact paths — reuse, don't reinvent)

- Stage-order + audit-envelope capture: `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` (the `Gateway(...)` builder + recording doubles at `#L978-1302`; `SurfaceOriginDeclaredAtBoundaryShouldAppearImmutablyInEveryAuditEnvelope`; `StateStoreEquivalentRepeatShouldMatchSingleSubmitEndState`).
- Direct-gateway state-store equality: `tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs`.
- Projection end-state read + aggregate Handle rejection: `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedNoteDurableChainTests.cs`.
- HTTP arm + auth + wire body: `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` (`AuthenticatedFactory`, `RecordGovernedNoteRequest`, `TestPrincipalStartupFilter`, `AcceptingEventStoreGatewayClient`, `AllowAllSpineCommandAllowlist`).
- Tier-3 live E2E + state-store polling: `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` (`PollGovernedOperationViewAsync`, `Tier3RuntimeIsAvailable`, fresh-ULID discipline).
- Public read-back surfaces: `IChatBotClient.GetOperationStatusAsync` / `GetOperationAuditHistoryAsync`; `IAuditHistoryReader.GetPostCommitEnvelopes(tenantId, commandId)`.

### Architecture Guardrails

- **Shims replicate no stage.** Each arm constructs only an `IChatBotCommand` and submits through `IChatBotClient.SubmitAsync` / `CommandGateway.SubmitAsync`. The governance seams (`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) are `internal` to `.Server` — never reference them from a shim. This is exactly what Story 1.10's NetArchTest enforces; keep it green. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md; src/Hexalith.ChatBot.Server/Gateway/Stages/]
- **Central package management only.** Any new version in `Directory.Packages.props`; csproj gets a bare `<PackageReference>` (no inline `Version=`). `ProjectFilesShouldNotUseInlinePackageVersions` will fail on a regression. [Source: Directory.Packages.props; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- **Metadata-only / no leakage.** Assertion messages and any captured outcome must stay metadata-only (IDs/enums/counts/state names) — never embed payloads, secrets, or raw exception text. Run a `ShouldNotContain("tenant-alpha")`-style leakage sentinel on any rendered problem/body. [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]
- **Submodules.** EventStore is a root-level submodule reached via `$(HexalithEventStoreRoot)`; use only root-declared submodules, never `--recursive`, never initialize nested submodules. [Source: CLAUDE.md#Git Submodules]
- **Reuse stable vocabularies / fixtures.** xUnit v3 (3.2.2) `[Fact]` + Shouldly (4.3.0); hand-written recording doubles (no new mocking library — `NSubstitute` is not in the package set). Reuse the fixed test ULIDs (actor `actor-alpha`, tenant `tenant-alpha`/`tenant-beta`, correlation `01ARZ3NDEKTSV4RRFFQ69G5FAW`, …). Share `tests/fixtures/` — no per-project corpus forks. [Source: Directory.Packages.props; tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#L20-26; _bmad-output/planning-artifacts/architecture.md (lines 708-710)]

### Tier-3 live-run gotchas (only if you run the env-gated Aspire leg)

Carry forward from Stories 1.8/1.9 and user memory `tier3-live-dapr-run.md`:
- VSTest `dotnet test` is sandbox-blocked (socket `Permission denied`) — run the compiled xUnit v3 binary directly.
- Non-standard `dapr init` host ports: `HEXALITH_CHATBOT_TIER3=1 PATH=$HOME/.dapr/bin:$PATH Dapr__PlacementHostAddress=127.0.0.1:6050 Dapr__SchedulerHostAddress=127.0.0.1:6060`.
- Actor state store MUST be named `statestore` (EventStore hardcodes it); the read model + coarse idempotency use `chatbot-statestore`. Use IPv4 literal `127.0.0.1` (not `localhost`→::1 on WSL2).
- mTLS-off ACL split: self-hosted run loads default-allow `accesscontrol.local.yaml`; never `trustDomain:"*"`/`namespace:""`.
- Use **fresh per-run ULIDs** (Redis dedups a fixed command id → a replay returns a stale prior result and poisons the run); explicit-timeout polling, **no `Thread.Sleep`**; poll each app's `/health` (Aspire reports *Running* not *listening* under `IsProxied=false`). [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md#Completion Notes; user memory tier3-live-dapr-run.md]

### Scope Boundary at M0 (read carefully)

- This is the **M0 shims half of FR86 only**: contract/conformance tests proving "equivalent input → same events + state-store end-state" via **in-test** thin CLI/MCP shims. **FR84** (equivalent authorization outcomes across surfaces) and the **full** harness replacing the shims with real adapters belong to **Epic 5 Story 5.4** (M1). Do **not** scaffold `Hexalith.ChatBot.Cli`/`.Mcp` projects. [Source: _bmad-output/planning-artifacts/epics.md (lines 522-524 FR→epic mapping; lines 1706-1724 Story 5.4)]
- This is **not** the cross-tenant isolation harness (Story 1.12 — nine-actor negative tests) and **not** the tenant-scoped fixture/evaluation scaffold (Story 1.13). Design the surface-driver so 1.12's CLI/MCP *actor* shims and 1.13's tenant-scoped fixtures can later share it, but do not build their scope here. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.12, #Story 1.13]

### Previous Story Intelligence

- **Story 1.9** built the one governed command, the shared gateway, and the two reusable test harnesses (in-process `WebApplicationFactory` and Tier-3 Aspire) that 1.11's arms drive. It also wired surface-origin attribution end-to-end (`ChatBotSurfaceOrigin` closed enum with `Cli`/`Mcp` already present; origin captured once at the boundary, immutable downstream, in every audit envelope). Read `TrivialGovernedCommandAspireE2eTests.cs` and `GovernedNoteDurableChainTests.cs` first. [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md#File List; src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs]
- **Story 1.10** added the NetArchTest fitness layer that mechanically blocks stage replication and enforces `Contracts ← Client ← Server` + adapters→Client-only. The shims here must obey those rules (they will, as long as they construct only `IChatBotCommand`). 1.10's QA pass also established the "a silently-vacuous rule is a false pass" lesson — the source of AC6's non-vacuity meta-test. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results]
- Current dirty worktree: `_bmad-output/story-automator/orchestration-1-20260530-160445.md` is unrelated automation output — do not revert or overwrite it. [Source: git status]

### Testing Requirements

- xUnit v3 (3.2.2) + Shouldly (4.3.0); new tests live in `tests/Hexalith.ChatBot.Conformance.Tests/`. Run via the compiled in-process binary (VSTest blocked).
- **Every assertion reads state-store end-state / envelope sequence** (AC6) — a bare `result.IsAccepted`/202/exit-code assertion is a defect against AC2.
- **Add the non-vacuity negative meta-test** (AC6) — without it, a too-broad exclude set yields a silent always-pass.
- **Must stay green (regression):** `ContractSpineOracleTests`, `DaprAccessControlConformanceTests`, all 33 `Architecture.Tests`, and the full ChatBot suite (Server/Contracts/Client/UI/Integration/Aspire/AppHost/ServiceDefaults/Testing). This story adds tests; it must not flip an existing one. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#Debug Log References (the green sweep)]
- **Build gate:** full solution 0 warnings / 0 errors under warnings-as-errors, no inline package versions (AC7).

### Out of Scope

- No real `Cli`/`Mcp`/`Workers` project scaffolding (Epic 5). No FR84 cross-surface authorization-equivalence proof and no real-adapter harness (Story 5.4). No cross-tenant isolation harness (1.12). No tenant-scoped fixture/evaluation scaffold (1.13).
- No runtime/behavior change in `src/` beyond the `Server` `InternalsVisibleTo` entry (and a `Directory.Packages.props` version entry only if a package is added). No `internal`→`public` weakening, no new mocking library, no inline package versions, no nested/recursive submodule operations, no hand-editing generated client files.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: CLAUDE.md#Git Submodules]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: First Safe Governed Action & Command Spine]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.11: Differential-conformance harness (lines 864-878)]
- [Source: _bmad-output/planning-artifacts/epics.md#Cross-Surface Command Parity (FR81–FR86) (lines 155-163)]
- [Source: _bmad-output/planning-artifacts/epics.md (line 338 — differential-conformance harness definition; line 340 — Tier 2/3 inspect state-store end-state)]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.4: Cross-surface equivalence verification (lines 1706-1724)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines (Conformance tests: real-aggregate vs in-memory event-sequence equality; Tier 2/3 state-store)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns (parity by construction + differential-conformance harness)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure (Conformance.Tests = differential-conformance harness + parity oracle shims)]
- [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md]
- [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/RecordGovernedNote.cs; .../Commands/IChatBotCommand.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs; .../Enums/ChatBotSurfaceOrigins.cs]
- [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs; .../ChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; .../Gateway/ChatBotCommandSubmission.cs; .../Gateway/ChatBotSpineCommandAllowlist.cs; .../Gateway/Stages/AcceptedCommandDispatcher.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs; .../Operations/GovernedNoteRecorded.cs; .../Operations/GovernedNoteAlreadyRecordedRejection.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs; .../Projections/IGovernedOperationProjectionStore.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs; .../Audit/IAuditHistoryReader.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/ (ContractSpineOracleTests.cs, DaprAccessControlConformanceTests.cs, csproj)]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs; .../ServerBootstrapApiTests.cs; .../Operations/GovernedNoteDurableChainTests.cs]
- [Source: tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs; .../IdempotencyStateStoreIntegrationTests.cs]
- [Source: Directory.Packages.props; Directory.Build.props]
- [Source: user memory tier3-live-dapr-run.md (Tier-3 live DAPR run gotchas)]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (Claude Opus 4.8)

### Debug Log References

- Build (full solution, warnings-as-errors, no inline package versions):
  `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded. 0 Warning(s) / 0 Error(s)**.
- Tests run via the compiled xUnit v3 in-process binary (VSTest `dotnet test` is sandbox-blocked — socket
  `Permission denied`), e.g. `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests`.
- Full ChatBot sweep (final, all green — no regression):
  - Conformance.Tests: Total 13, Failed 0, Skipped 0 (10 new + 3 pre-existing `ContractSpineOracleTests`/`DaprAccessControlConformanceTests`).
  - Architecture.Tests: Total 33, Failed 0 (NetArchTest dependency/adapter-boundary rules stay green).
  - Server.Tests: Total 113, Failed 0. Contracts.Tests: 66. Client.Tests: 13.
  - IntegrationTests: Total 4, Failed 0, **Skipped 2** (both Tier-3 Aspire E2E legs self-skip without `HEXALITH_CHATBOT_TIER3=1`).
  - UI.Tests: 8, ServiceDefaults.Tests: 3, Testing.Tests: 1, AppHost.Tests: 3, Aspire.Tests: 2 — all Failed 0.

### Completion Notes List

- **Test-only, behavior-preserving.** The only `src/**` change is adding `Hexalith.ChatBot.Conformance.Tests`
  to `Server`'s `InternalsVisibleTo` (test-enablement, not runtime behavior). No `internal`→`public`, no DI/stage
  changes. **No new package was added** — reused the existing xUnit v3 (3.2.2) + Shouldly (4.3.0); the
  `Microsoft.AspNetCore.Mvc.Testing` subtask was N/A because the HTTP/`WebApplicationFactory` arm was not used.
- **Primary lane = gateway-level in-process.** Each arm submits an otherwise-identical `ChatBotCommandSubmission`
  through one real `CommandGateway` (built with shared recording doubles + the internal production
  `InMemoryCoarseIdempotencyStore`/`InMemoryOperationStatusStore` and the real `ChatBotSpineCommandAllowlist`),
  giving direct ordered access to the admission audit-envelope sequence + store records. The arms construct only
  an `IChatBotCommand` (no stage replicated), so Story 1.10's NetArchTest stays green.
- **Durable end-state read in-process without a DAPR runtime / WebApplicationFactory:** the success/retry/re-record
  lanes run the pure `GovernedOperationAggregate.Handle`, then the real
  `GovernedOperationProjectionTranslator` → `GovernedOperationProjectionHandler` →
  `InMemoryGovernedOperationProjectionStore`, and read the `GovernedOperationView` back via
  `IGovernedOperationProjectionStore.GetAsync(tenant, noteId)`. The domain event carries no origin, so the
  projection end-state is surface-invariant **by construction** (asserted explicitly in AC3 tests).
- **Two-layer outcome + equality oracle.** `ArmOutcome` captures (a) the admission sequence (phase +
  `Received->Proposed` + decision/reasonCode/outcome/redactionDecision) where `surfaceOrigin` legitimately appears,
  and (b) the durable `GovernedOperationView` derived-record shape. `DifferentialOracle.Project` flattens an
  outcome to an ordered (field, value) list honoring the explicit include set and dropping the exclude set
  (`surfaceOrigin`, minted `commandId`/`resourceId`, ULIDs, timestamps); `Compare` walks two projections and
  **names the first diverging field**. Each arm's audited origin is asserted `== ui/cli/mcp` — the only delta.
- **All four FR86 intents covered:** success, fine-idempotency `GovernedNoteAlreadyRecordedRejection` (returned,
  not thrown; `SourceVersion` unchanged), fail-closed non-allowlisted rejection (same redacted problem
  `category`+`code`+`reasonCode`, zero durable mutation read via safe-not-found, metadata-only leakage sentinel),
  and retry/replay (`DispatchCount == 1`, one coarse record, `SourceVersion` stays 1).
- **AC6 non-vacuity proven:** `DifferentialOracleNonVacuityTests` has a positive control (origin-only delta →
  equal) plus four perturbations (sourceVersion, noteId, an admission `stateTransition`, an injected extra event)
  that each assert the oracle reports **not-equal and names the diverging field** — so a too-broad exclude set
  cannot pass silently. Every conformance assertion reads the captured outcome (envelope sequence / store / view),
  never a bare `IsAccepted`/202/exit code (the only `IsAccepted` use is a harness guard that throws).
- **Swappable driver for Epic 5 Story 5.4:** `ISurfaceArm` (UI/CLI/MCP) parses each surface's own raw input
  (form value / argv / tool-call map) into the one typed command; the assertion engine is independent of the
  driver, so the real `.Cli`/`.Mcp` adapters can replace the M0 shims without touching the oracle.
- **Tier-3 stretch (task 8):** added a committed, env-gated, self-skipping cross-origin sibling
  (`GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin`) that submits the same intent
  with `origin=ui`/`cli`/`mcp` (fresh ULID per origin) against the live DAPR topology and asserts an identical
  projected derived-record shape. It self-skips without `HEXALITH_CHATBOT_TIER3=1` (sandbox cannot run live DAPR),
  so the runnable sweep stays green; per AC7 the Tier-3 clause is satisfied by the existing E2E staying green.

### File List

- `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj` (modified — added ProjectReferences to Contracts/Client/Server/EventStore.Contracts)
- `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` (modified — added `InternalsVisibleTo` for Conformance.Tests)
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/ConformanceGatewayDoubles.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/DifferentialOracle.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/GovernedCommandConformanceHarness.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/SuccessIntentParityTests.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/RejectionIntentParityTests.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/RetryIntentParityTests.cs` (new)
- `tests/Hexalith.ChatBot.Conformance.Tests/DifferentialOracleNonVacuityTests.cs` (new; QA-extended — +5 non-vacuity tests closing the include-set coverage gap)
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` (modified — origin-parametrized submit overloads + self-skipping cross-origin Tier-3 sibling test)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — 1-11 ready-for-dev → in-progress → review)

## QA Results

### QA gap-analysis pass — 2026-05-31 (bmad-qa-generate-e2e-tests, claude-opus-4-8)

One focused gap-analysis pass over the committed harness (`Harness/*` + the four parity/meta-test files) against
AC1–AC7 and `checklist.md`. AC1–AC5 and AC7 were already fully covered; one concrete gap was found in **AC6's
non-vacuity proof** and auto-applied.

**Gap (AC6 — oracle non-vacuity was under-proven).** `DifferentialOracle.Project` emits **25** included
(field, value) keys, but `DifferentialOracleNonVacuityTests` only proved **4** of them discriminate
(`view.sourceVersion`, `view.noteId`, `admission[0].stateTransition`, `admission.count`). The fields that
*directly back the other parity tests* were unproven — so a too-broad exclude silently dropping one of them (the
Dev-Notes-flagged "single biggest implementation risk") would have let a parity test pass **vacuously** with no
meta-test catching it:
- `domainOutcome` → backs AC2 (`GovernedNoteRecorded`) and AC4 (`GovernedNoteAlreadyRecordedRejection` / problem identity).
- `dispatchCount` + `coarseIdempotencyRecordCount` → back AC5 (retry/replay = exactly one durable effect).
- `view.present` → backs AC4 fail-closed (no durable view).

**Fix auto-applied (test-only, no src/harness change).** Added 5 tests to `DifferentialOracleNonVacuityTests.cs`:
1. `ProjectionShouldExposeEveryIncludedFieldSoNoFieldCanBeSilentlyExcluded` — a **coverage guard** pinning the
   exact ordered 25-key include-set, so any field removed from (or added to) `Project()` flips the test and forces
   a matching perturbation. This makes silent vacuity structurally impossible, not merely unlikely.
2–5. Per-field perturbation facts for `domainOutcome`, `dispatchCount`, `coarseIdempotencyRecordCount`, and
   `view.present`, each asserting the oracle reports **not-equal and names that exact field**.

**Verification (compiled xUnit v3 in-process binary; VSTest `dotnet test` is sandbox-blocked).**
- Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **0 Warning(s) / 0 Error(s)** (warnings-as-errors, no inline package versions — AC7 holds).
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` → **Total 18, Failed 0, Skipped 0** (was 13; +5 new).
- No-regression sweep of the most-coupled binaries: Architecture.Tests **33/0** (NetArchTest dependency/adapter rules stay green), Server.Tests **113/0**, IntegrationTests **4/0, Skipped 2** (both Tier-3 Aspire legs self-skip without `HEXALITH_CHATBOT_TIER3=1`).

**Checklist (`.agents/skills/bmad-qa-generate-e2e-tests/checklist.md`).** Tests use standard framework APIs
(xUnit v3 3.2.2 + Shouldly 4.3.0); happy path (success intent) + critical error cases (re-record rejection,
fail-closed rejection, retry/replay) all covered; all generated tests run successfully; clear descriptive names;
no hardcoded waits/sleeps (`FixedConformanceClock`, no `Thread.Sleep`); tests independent (each arm builds its own
gateway + in-memory stores). API/E2E "locators" item is N/A — this is a gateway-level differential harness, not a
browser/HTTP suite. **No src or harness production change in this pass — additive test-only.**

**Gate: PASS.** AC1–AC7 satisfied; no further concrete gaps found.

## Senior Developer Review (AI)

**Reviewer:** bmad-story-automator-review (claude-sonnet-4-6) — 2026-05-31

**Verdict:** APPROVED — no CRITICAL or HIGH issues. Two MEDIUM issues auto-fixed; one LOW issue auto-fixed.

### AC Validation

| AC | Status | Evidence |
|----|--------|---------|
| AC1 — Harness surface-parametrized, no stage replication | ✅ IMPLEMENTED | `ISurfaceArm`/`UiSurfaceArm`/`CliSurfaceArm`/`McpSurfaceArm`; each constructs only `IChatBotCommand`, submits through `CommandGateway`; no `Cli`/`Mcp` production project created |
| AC2 — Identical admission event sequence | ✅ IMPLEMENTED | `RecordingAuditWriter.Envelopes` → `AdmissionStep` capture; `DifferentialOracle` includes phase+stateTransition+decision/reasonCode/outcome/redactionDecision; `surfaceOrigin` excluded; each arm's audited origin asserted individually |
| AC3 — Durable state-store end-state read from store | ✅ IMPLEMENTED | `ProjectAndReadAsync` → `InMemoryGovernedOperationProjectionStore.GetAsync`; never reads HTTP 202; `EachArmDurableStateStoreEndStateShouldBeIdentical` asserts all 7 view fields |
| AC4 — Rejection-intent parity | ✅ IMPLEMENTED | Fine-idempotency: `RunReRecordRejectionAsync` → `GovernedNoteAlreadyRecordedRejection`, `SourceVersion=1`, zero extra effect. Fail-closed: `NonAllowlistedProbe` → same problem `category+code+reasonCode`, null view, zero dispatch; leakage sentinel passes |
| AC5 — Retry/replay-intent parity | ✅ IMPLEMENTED | `RunRetryReplayAsync` → `DispatchCount==1`, `CoarseIdempotencyRecordCount==1`, `SourceVersion==1`; replayed outcome equal across all three arms |
| AC6 — AC2 discipline + non-vacuity | ✅ IMPLEMENTED | No bare `IsAccepted`/202 assertion (only used as a guard that throws); 10 non-vacuity tests including a 25-key coverage pin and per-field perturbation facts for all oracle-backed fields |
| AC7 — Build green, no regression, swappable shim | ✅ IMPLEMENTED | Build 0/0 warnings-as-errors; no inline package versions; Conformance 18/0, Architecture 33/0, Server 113/0, Integration 4/0 (2 Tier-3 self-skip); `ISurfaceArm` swappable for Epic 5 Story 5.4 |

### Issues Found and Fixes Applied

**[M1 — MEDIUM] Missing `cli-mcp` oracle pair in `RetryIntentParityTests`** (`RetryIntentParityTests.cs`):
The retry test compared ui-cli and ui-mcp but omitted the cli-mcp transitive check — inconsistent with `SuccessIntentParityTests` which checks all three pairs. **Auto-fixed:** added `DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue()`.

**[M2 — MEDIUM] Missing `cli-mcp` oracle pair in `RejectionIntentParityTests` (re-record leg)** (`RejectionIntentParityTests.cs`):
Same omission as M1 in the re-record rejection test. **Auto-fixed:** added `DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue()`.

**[L1 — LOW] `RunReRecordRejectionAsync` passed `deliveries: 2` to `ProjectAndReadAsync`** (`Harness/GovernedCommandConformanceHarness.cs:145`):
The re-record rejection produces no new event — only the initial `GovernedNoteRecorded` from the first recording exists. `deliveries: 2` was double-applying that first event (implicitly testing projection idempotency, which is tested separately in the retry path). **Auto-fixed:** changed to `deliveries: 1` to accurately model the re-record scenario.

### Verification After Fixes

- Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **0 Warning(s) / 0 Error(s)**
- Conformance.Tests: **Total: 18, Failed: 0, Skipped: 0** — all passing
- Architecture.Tests: **33/0** — NetArchTest adapter-boundary rules green
- Server.Tests: **113/0** — no regression
- IntegrationTests: **4/0, Skipped: 2** — Tier-3 Aspire legs self-skip without `HEXALITH_CHATBOT_TIER3=1`

**Checklist (`.agents/skills/bmad-story-automator-review/checklist.md`):** All items satisfied.

## Change Log

- 2026-05-31: Senior Developer Review (bmad-story-automator-review, claude-sonnet-4-6) — auto-fixed M1/M2 (added cli-mcp oracle pair to `RetryIntentParityTests` and `RejectionIntentParityTests`), auto-fixed L1 (changed `deliveries: 2` → `deliveries: 1` in `RunReRecordRejectionAsync`). Conformance 18/0, Architecture 33/0, Server 113/0, Integration 4/0 (2 Tier-3 skips). Build 0/0 warnings-as-errors. Status → done.
- 2026-05-31: QA gap-analysis pass (bmad-qa-generate-e2e-tests) — closed the AC6 non-vacuity coverage gap by adding a 25-key include-set coverage guard plus per-field perturbation tests for `domainOutcome`/`dispatchCount`/`coarseIdempotencyRecordCount`/`view.present` to `DifferentialOracleNonVacuityTests.cs`. Test-only (no src/harness change). Conformance.Tests 13→18, all green; Architecture 33 / Server 113 / Integration 4 (2 Tier-3 skips) green; full solution builds 0/0 warnings-as-errors.
- 2026-05-31: Implemented the differential-conformance harness in `tests/Hexalith.ChatBot.Conformance.Tests` — a swappable UI/CLI/MCP surface-driver (`ISurfaceArm`) over the one allowlisted command `RecordGovernedNote`, a two-layer `ArmOutcome` (admission audit-envelope sequence + durable `GovernedOperationView` end-state) captured via the gateway-level in-process lane and the in-process projection lane, and a `DifferentialOracle` that compares under an explicit include/exclude projection and names the first diverging field. Added success/rejection(fine-idempotency + fail-closed)/retry intent parity tests and a non-vacuity oracle meta-test; added a self-skipping env-gated cross-origin Tier-3 sibling. Sole `src/` change: `Server` `InternalsVisibleTo` += Conformance.Tests; no new package. Full solution builds 0/0 warnings-as-errors; full ChatBot sweep green (Conformance 13, Architecture 33, Server 113, … ; IntegrationTests 2 Tier-3 legs self-skip). Status → review.
- 2026-05-31: Created Story 1.11 context (differential-conformance harness — surface-parametrized thin UI/CLI/MCP test shims over the one allowlisted command `RecordGovernedNote`, asserting identical admission event sequence + durable `GovernedOperationView` state-store end-state across surfaces with `surfaceOrigin` as the only permitted delta, covering success/rejection/retry intents, reading state-store end-state never HTTP/exit/MCP codes, with a non-vacuity oracle meta-test; swappable surface-driver so Epic 5 Story 5.4 can replace the M0 shims). Test-only, behavior-preserving (sole src-surface change: add Conformance.Tests to Server InternalsVisibleTo). Status set to ready-for-dev.
