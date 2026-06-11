---
baseline_commit: 084d964d53255789766878dc7815458604d17faf
---

# Story 11.1: Host-reuse ADR - DomainService SDK adoption decision record

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-11. -->

## Story

As a platform architect,
I want the host-layer reuse decision recorded as an accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md`,
so that SDK adoption is a dated, reviewable architecture decision instead of silent drift.

## Acceptance Criteria

1. **ADR records full DomainService SDK adoption.** Given readiness pass-2 Issue #1, when the ADR is authored, then it records full adoption of `Hexalith.EventStore.DomainService`, names it as the target host layer for ChatBot, and explicitly rejects continuing the current hand-rolled host as the default direction. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.1; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#Story 11.1]

2. **ADR preserves FR81a by making the admission hook a platform SDK capability.** Given the FR81a CommandGateway admission invariant, when the ADR describes the write path, then it states that the CommandGateway pre-commit admission chain mounts through a platform `Hexalith.EventStore.DomainService` hook owned by Story 11.2, not through a ChatBot-specific bypass or second command pipeline. The ADR must preserve fail-closed admission and must not weaken the existing CommandGateway spine. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.1; _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8 - added 2026-06-09, readiness pass-2)]

3. **ADR fixes the target SDK bindings and host shape.** Given the target architecture D8, when the ADR is accepted, then it names the target host shape and SDK contracts: `AddEventStoreDomainService()` plus admission-chain registration plus `UseEventStoreDomainService()`; `IDomainQueryHandler`; `IDomainProjectionHandler`; `IReadModelStore`/`ReadModelWritePolicy`; `IQueryCursorCodec`/`QueryCursorScope`; `AddEventStoreDomainTelemetry`; `AddEventStoreDomainStateStoreHealthCheck`; and `AddEventStoreDomainModule(...)` composition. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.1; Hexalith.EventStore/CLAUDE.md#Domain-Module Authoring (domain-centric); Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]

4. **ADR records binding migration order and gates dependent work.** Given Epic 11 sequencing, when the ADR is accepted, then it records the migration order `11.2 -> 11.3/11.4 -> 11.5 -> 11.6`, states that Stories 11.2-11.6 must not start before the ADR is accepted, and repeats that 11.5/11.6 land after Stories 8.7a/8.7b so host migration does not chase a moving enforcement seam. [Source: _bmad-output/planning-artifacts/epics.md#Epic 11: Minimal Technical Layer - DomainService SDK Host Adoption; _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]

5. **ADR contains an explicit exception boundary.** Given ChatBot may need a small amount of retained local-development composition, when the ADR is authored, then every allowed hand-rolled exception is listed with a dated justification, owner, and retirement/review trigger. If a thin umbrella local-dev AppHost is retained for multi-sibling topology, the ADR must say it is not the production domain-hosting pattern and must not preserve the current full `AppHost`/`Aspire`/`ServiceDefaults` ownership by default. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.1; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#4.5 New ADR]

6. **Architecture D8 links the accepted ADR without contradiction.** Given `_bmad-output/planning-artifacts/architecture.md` already contains D8, when Story 11.1 completes, then D8 and the ADR agree on SDK adoption, hook ownership, SDK bindings, sequencing, and exception boundary, and D8 contains a clear markdown link or path reference to `docs/adrs/domainservice-sdk-host-adoption.md`. [Source: _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8 - added 2026-06-09, readiness pass-2)]

7. **Verification is evidence-based and scoped to decision work.** Given this story is an ADR story, when implementation completes, then evidence includes source checks proving the ADR file exists with `Status` accepted, contains the required SDK names and migration order, `architecture.md` links it, and no Story 11.2-11.6 implementation or EventStore submodule modification was performed under this story. [Source: .agents/skills/bmad-create-story/checklist.md; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#5. Implementation Handoff]

## Tasks / Subtasks

- [x] Author the accepted ADR (AC: 1, 2, 3, 4, 5)
  - [x] Create `docs/adrs/domainservice-sdk-host-adoption.md` using the existing ADR style in `docs/adrs/*.md`.
  - [x] Set ADR status to `Accepted`, with date `2026-06-11` or the actual implementation date.
  - [x] Record the context: readiness pass-2 Issue #1, current ChatBot hand-rolled host, zero SDK-contract usage at planning time, current `Program.cs` host size, and module-owned `AppHost`/`Aspire`/`ServiceDefaults`.
  - [x] Record the decision: full DomainService SDK adoption, not a recorded permanent exception.
  - [x] State that the FR81a CommandGateway admission chain becomes a platform SDK pre-commit hook owned by Story 11.2.

- [x] Pin the SDK contract bindings and target host/composition shape in the ADR (AC: 2, 3, 5)
  - [x] Name `AddEventStoreDomainService()` and `UseEventStoreDomainService()` as the target Server host foundation.
  - [x] Name `IDomainQueryHandler`, `IDomainProjectionHandler`, `IReadModelStore`/`ReadModelWritePolicy`, `IQueryCursorCodec`/`QueryCursorScope`, `AddEventStoreDomainTelemetry`, and `AddEventStoreDomainStateStoreHealthCheck` as the target replacements for hand-rolled query/projection/read-model/cursor/telemetry/health surfaces.
  - [x] Name `AddEventStoreDomainModule(...)` as the target composition path for platform AppHost ownership.
  - [x] Document the canonical DomainService endpoints that must remain available: `/process`, `/replay-state`, `/query`, `/project`, and `/admin/operational-index-metadata`.

- [x] Record sequencing, gates, and out-of-scope boundaries (AC: 4, 7)
  - [x] State that Story 11.1 gates Stories 11.2-11.6.
  - [x] State that 11.2 is platform work in `Hexalith.EventStore` and requires explicit submodule approval before any EventStore edit.
  - [x] State that 11.3/11.4 are parallelizable after 11.2, and that 11.5/11.6 land after 8.7a/8.7b.
  - [x] State that Story 11.1 does not implement the platform hook, migrate endpoints/projections, reduce `Program.cs`, or remove `AppHost`/`Aspire`/`ServiceDefaults`.

- [x] Define and justify any allowed exception boundary (AC: 5)
  - [x] Decide whether any thin local-dev umbrella AppHost remains necessary for the multi-sibling topology.
  - [x] For each retained exception, include dated justification, owner, expected scope, and retirement/review trigger.
  - [x] Explicitly forbid exceptions from becoming a production-domain-hosting bypass or preserving the current full hand-rolled host indefinitely.

- [x] Update architecture D8 to link the ADR (AC: 6)
  - [x] Edit `_bmad-output/planning-artifacts/architecture.md` so the D8 ADR reference clearly points to `docs/adrs/domainservice-sdk-host-adoption.md`.
  - [x] Verify D8 and the ADR use the same migration order, SDK bindings, hook ownership, and exception boundary.
  - [x] Do not change PRD scope or add new FRs/NFRs; Epic 11 is platform-conformance work.

- [x] Add focused decision-evidence checks (AC: 7)
  - [x] Use `rg`/source checks or existing doc tests to prove the ADR contains all required SDK names and migration order.
  - [x] Prove `_bmad-output/planning-artifacts/architecture.md` links the ADR.
  - [x] Prove no `Hexalith.EventStore` submodule file changed in Story 11.1.
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded `sprint_status` from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key is `11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record`, currently `backlog`; `epic-11` is `backlog` and should move to `in-progress` when this story file is created.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`. Epic 11 is the M2 release-readiness closure for DomainService SDK host adoption. Story 11.1 gates all other Epic 11 stories.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`. D8 already states the target DomainService SDK direction, transitional current host state, and ADR path.
- No workflow-pattern PRD/UX files were loaded by the configured glob patterns for this story. They are not required for this ADR-only backend/platform decision.
- Loaded persistent project-context facts from sibling `_bmad-output/project-context.md` files. Relevant facts: .NET 10, warnings-as-errors, centralized package versions, xUnit v3/Shouldly, root-level-only submodule policy, metadata-only/no-leak floor, and EventStore DomainService authoring rules.

### Epic 11 Context

Epic 11 aligns the ChatBot host layer with the platform's domain-centric SDK (`Hexalith.EventStore.DomainService`). It introduces no new product FRs; it extends FR81/FR81a enforcement and closes readiness pass-2 Issue #1 by reducing host-layer boilerplate and making the existing CommandGateway admission invariant a platform hook rather than a domain-host bypass. [Source: _bmad-output/planning-artifacts/epics.md#Epic 11: Minimal Technical Layer - DomainService SDK Host Adoption]

Binding sequence:

- 11.1: accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md`.
- 11.2: platform pre-commit admission hook in `Hexalith.EventStore.DomainService`.
- 11.3: query endpoints to `IDomainQueryHandler` and `IQueryCursorCodec`.
- 11.4: projections, telemetry, and health to SDK contracts.
- 11.5: reduce Server host to SDK shape with CommandGateway admission hook.
- 11.6: retire or sharply reduce module-owned `AppHost`/`Aspire`/`ServiceDefaults` through `AddEventStoreDomainModule(...)`.

Stories 11.5 and 11.6 must land after 8.7a/8.7b so the host migration does not chase the durable enforcement seam while it is still moving. [Source: _bmad-output/planning-artifacts/epics.md#Epic 11: Minimal Technical Layer - DomainService SDK Host Adoption]

### Architecture and SDK Facts

EventStore's Domain-Module Authoring rule is explicit: a domain module must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults`, must not re-implement projection/query actors, DAPR wiring, telemetry sources, health checks, or event-subscription plumbing, and should add missing capabilities to the platform SDK rather than the domain. [Source: Hexalith.EventStore/CLAUDE.md#Domain-Module Authoring (domain-centric)]

`EventStoreDomainServiceExtensions` provides `AddEventStoreDomainService(...)`, `UseEventStoreDomainService()`, and `MapEventStoreDomainService()`. The SDK maps `GET /`, `POST /process`, `POST /replay-state`, `POST /query`, `POST /project` unless already mapped, and `POST /admin/operational-index-metadata`. It discovers and registers `IDomainQueryHandler` and `IDomainProjectionHandler` implementations from domain assemblies. [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]

`HexalithEventStoreDomainModuleExtensions.AddEventStoreDomainModule(...)` is the Aspire composition hook. It attaches a DAPR sidecar to a domain-module project, optionally with isolated resources, or references the shared EventStore state store and pub/sub for domains that persist read models or subscribe to events. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs]

`IReadModelStore` and `ReadModelWritePolicy` are the platform read-model persistence and optimistic-concurrency write helpers. `IQueryCursorCodec` and `QueryCursorScope` provide protected, scope-bound cursors. The ADR should bind future ChatBot migrations to these platform contracts instead of hand-rolled copies. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs; Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs; Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs; Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs]

The reference `Hexalith.EventStore.Sample` host shows the desired shape: `builder.AddEventStoreDomainService();` and `app.UseEventStoreDomainService();`, with a narrow, explicitly documented fault-injection exception mapped before the SDK yields `/project`. Use this as the model for any exception boundary: small, named, and justified. [Source: Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Program.cs]

### Current ChatBot State to Preserve Later

Story 11.1 does not modify the host code. It records the decision that later stories will implement.

Current state at story creation:

- `src/Hexalith.ChatBot.Server/Program.cs` is 1250 lines in the working tree. The readiness report cited 1221 lines when it was produced; the current file has grown since then.
- `Hexalith.ChatBot.Server.csproj` references EventStore `.Client` and `.Contracts`, not `.DomainService`.
- ChatBot still ships `src/Hexalith.ChatBot.AppHost`, `src/Hexalith.ChatBot.Aspire`, and `src/Hexalith.ChatBot.ServiceDefaults`.
- The existing host contains custom command gateway, auth, correlation, DAPR/cloud-events, projection subscription, health, workflow, periodic enforcement, domain-service endpoint, and multiple inline query routes. Later migration stories must preserve behavior while moving ownership to the SDK. [Source: src/Hexalith.ChatBot.Server/Program.cs; src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj]

### ADR Content Guidance

Use the existing ADR style in `docs/adrs/*.md`: title, `## Status`, `## Context`, `## Decision`, `## Consequences`, `## Alternatives Considered`, and `## Verification`.

The ADR should include these decision statements in direct language:

- "ChatBot adopts `Hexalith.EventStore.DomainService` as the target host layer."
- "The FR81a CommandGateway admission chain is a platform SDK pre-commit hook, not a ChatBot host bypass."
- "ChatBot's target Server host is `AddEventStoreDomainService(...)` plus admission-chain registration plus `UseEventStoreDomainService()`."
- "Queries, projections, read models, cursors, telemetry, health, and composition move to the SDK contracts named in this ADR."
- "Stories 11.2-11.6 are blocked until this ADR is accepted."
- "Any retained local-dev umbrella AppHost exception is dated, scoped, and not a production domain-hosting pattern."

Avoid vague phrasing like "consider", "evaluate later", or "where practical" for the core adoption decision. The sprint-change proposal already chose full SDK adoption over a permanent recorded exception. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#3. Recommended Approach]

### File Structure Notes

Expected Story 11.1 files:

- `docs/adrs/domainservice-sdk-host-adoption.md` - new accepted ADR.
- `_bmad-output/planning-artifacts/architecture.md` - D8 ADR link/reference only, unless minor wording is needed to keep D8 and the ADR consistent.
- `_bmad-output/implementation-artifacts/11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record.md` - update Dev Agent Record during implementation.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - dev-story/code-review workflows may update status according to normal BMAD flow.

Do not edit `Hexalith.EventStore` in this story. Story 11.2 owns the platform SDK hook and explicitly requires submodule approval before EventStore changes. The root-level submodule policy also applies: do not use recursive submodule commands and do not initialize nested submodules.

### Testing and Verification Notes

Minimum checks for Story 11.1:

- `rg -n "Status|Accepted|Hexalith.EventStore.DomainService|pre-commit admission|IDomainQueryHandler|IDomainProjectionHandler|IReadModelStore|ReadModelWritePolicy|IQueryCursorCodec|QueryCursorScope|AddEventStoreDomainTelemetry|AddEventStoreDomainStateStoreHealthCheck|AddEventStoreDomainModule|11.2 -> 11.3/11.4 -> 11.5 -> 11.6" docs/adrs/domainservice-sdk-host-adoption.md`
- `rg -n "domainservice-sdk-host-adoption.md|docs/adrs/domainservice-sdk-host-adoption.md" _bmad-output/planning-artifacts/architecture.md`
- `git diff --name-only -- Hexalith.EventStore` must be empty for this story.
- `git diff --check`

Run architecture/documentation tests only if the implementation adds or updates an existing test project for ADR references. Do not invent a broad build/test lane for a two-file documentation decision unless repository policy requires it.

### Git Intelligence

Recent commits are Epic 10 UI/readiness work:

- `084d964 feat(story-10.7): Cross-surface a11y visual parity re-verification`
- `b02e63a feat(story-10.5): Governed chat composer`
- `70a21e7 feat(story-10.4): Project Workspace landing route`
- `135aacc feat(story-10.3): Migrate operational surfaces onto shell`
- `07f8267 feat(story-10.2): Migrate M0 governed surfaces onto shell`

Actionable relevance: recent work kept story evidence concrete, updated BMAD artifact status, used targeted tests, and avoided unowned submodule changes. Story 11.1 should do the same: precise decision artifact, narrow architecture link, explicit verification, no hidden platform implementation.

### Latest Technical Information

No external web research was needed or used for this story. The relevant "latest" technical specifics are local source facts in the checked-out `Hexalith.EventStore` submodule and current planning artifacts. Network access is restricted in this environment, and this story should bind to the repository's SDK contracts rather than external package versions.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 11: Minimal Technical Layer - DomainService SDK Host Adoption]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 11.1: Host-reuse ADR - DomainService SDK adoption decision record]
- [Source: _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8 - added 2026-06-09, readiness pass-2)]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09-pass-2.md#Step 6 - Summary and Recommendations]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md]
- [Source: Hexalith.EventStore/CLAUDE.md#Domain-Module Authoring (domain-centric)]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainProjectionHandler.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetry.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprStateStoreHealthCheck.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs]
- [Source: Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Program.cs]
- [Source: src/Hexalith.ChatBot.Server/Program.cs]
- [Source: src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-11T21:31:23+02:00 - Marked Story 11.1 in progress in sprint status and story status; preserved existing `baseline_commit`.
- RED check: `test -f docs/adrs/domainservice-sdk-host-adoption.md && rg ... docs/adrs/domainservice-sdk-host-adoption.md` failed before ADR creation because the ADR did not exist yet.
- Evidence check: `rg -n "Status|Accepted|Hexalith.EventStore.DomainService|pre-commit admission|IDomainQueryHandler|IDomainProjectionHandler|IReadModelStore|ReadModelWritePolicy|IQueryCursorCodec|QueryCursorScope|AddEventStoreDomainTelemetry|AddEventStoreDomainStateStoreHealthCheck|AddEventStoreDomainModule|11\.2 -> 11\.3/11\.4 -> 11\.5 -> 11\.6" docs/adrs/domainservice-sdk-host-adoption.md` passed.
- Evidence check: `rg -n "domainservice-sdk-host-adoption.md|docs/adrs/domainservice-sdk-host-adoption.md" _bmad-output/planning-artifacts/architecture.md` passed.
- Evidence check: `git diff --name-only -- Hexalith.EventStore` returned no files.
- Evidence check: `git diff --check` passed.
- Test attempt: `dotnet test Hexalith.ChatBot.slnx --no-restore` failed before test execution because MSBuild could not create an out-of-process named-pipe node in this sandbox (`SocketException (13): Permission denied`).
- Test retry: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` built test assemblies but VSTest aborted because it could not open its TCP listener in this sandbox (`SocketException (13): Permission denied`).
- Build check: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` passed with 0 warnings and 0 errors.
- Test run (in-proc runner, bypasses blocked VSTest TCP listener): the architecture lane executed successfully via the xUnit v3 in-process runner — `tests/Hexalith.ChatBot.Architecture.Tests` passed (45 total, 0 failed, 0 skipped per `tests/test-summary.md`). VSTest/`dotnet test` remains blocked by the sandbox socket restriction, but the in-process runner is not.
- 2026-06-11 review re-verification: ran `./Hexalith.ChatBot.Architecture.Tests -class "Hexalith.ChatBot.Architecture.Tests.DomainServiceSdkHostAdoptionAdrTests"` from the build output — 4 total, 0 failed. All four ADR/D8 decision-evidence assertions pass against the authored ADR and architecture link.

### Completion Notes List

- Authored the accepted DomainService SDK host adoption ADR at `docs/adrs/domainservice-sdk-host-adoption.md`.
- Recorded full adoption of `Hexalith.EventStore.DomainService`, rejection of the hand-rolled host as the default direction, FR81a preservation through a platform SDK pre-commit admission hook owned by Story 11.2, target SDK contracts, canonical endpoints, migration order, and out-of-scope boundaries.
- Defined the only allowed retained exception as a dated, local-development umbrella AppHost for multi-sibling topology, with owner, scope, and Story 11.6 retirement/review trigger. The ADR forbids using that exception as a production domain-hosting bypass or indefinite preservation of the current full host.
- Updated architecture D8 to link the accepted ADR and align the exception boundary wording.
- Did not edit `Hexalith.EventStore` and did not implement any Story 11.2-11.6 host, endpoint, projection, telemetry, health, or AppHost migration work.
- Story-specific evidence checks and serial build passed. The new ADR decision-evidence tests in `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs` were executed and passed via the xUnit v3 in-process runner (the blocked path is VSTest/`dotnet test`, which needs a TCP listener; the in-process runner does not). Architecture lane: 45 total, 0 failed; the four ADR-specific assertions: 4 total, 0 failed.

### File List

- `_bmad-output/implementation-artifacts/11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/adrs/domainservice-sdk-host-adoption.md`
- `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs`

### Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-11 | 0.1 | Implemented Story 11.1: accepted DomainService SDK host adoption ADR, D8 ADR link, exception boundary, sequencing/gates, and evidence checks. Status -> review. | GPT-5 Codex |
| 2026-06-11 | 0.2 | Adversarial code review (auto-fix): added the two omitted File List entries (ADR test + test-summary.md); corrected the test-execution record (architecture tests actually pass via the in-process runner); re-verified the four ADR/D8 assertions (4/4 pass). 0 CRITICAL/HIGH. Status -> done. | Jerome (AI review) |

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-06-11 · **Outcome:** Approve (auto-fixed)

**Scope:** ADR-only decision story. Reviewed `docs/adrs/domainservice-sdk-host-adoption.md`, the D8 link in `_bmad-output/planning-artifacts/architecture.md`, and the new `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs`. `_bmad/` and `_bmad-output/` content excluded from code review per skill policy; File-List completeness checked against `git status`.

**AC validation:** AC1-7 all IMPLEMENTED.
- AC1 — ADR records full `Hexalith.EventStore.DomainService` adoption and explicitly rejects the hand-rolled host as default (ADR §Decision).
- AC2 — FR81a admission preserved as a platform SDK pre-commit hook owned by Story 11.2; fail-closed and CommandGateway spine not weakened (ADR §"Preserve FR81a…").
- AC3 — all required SDK bindings + canonical endpoints named (ADR §"Bind future migrations…").
- AC4 — migration order `11.2 -> 11.3/11.4 -> 11.5 -> 11.6`, 11.2-11.6 gated, 11.5/11.6 after 8.7a/8.7b (ADR §"Gate and sequence…").
- AC5 — dated/owned/scoped exception boundary with Story 11.6 retirement trigger (ADR §Exception Boundary).
- AC6 — D8 links the ADR and agrees on adoption/hook/bindings/sequencing/exception (architecture.md lines 305, 426-432).
- AC7 — evidence-based decision checks present; EventStore submodule untouched (`git diff --name-only -- Hexalith.EventStore` empty); `git diff --check` clean.

**Task audit:** every `[x]` task verified against the ADR/architecture/test artifacts — all genuinely done.

**Findings (all auto-fixed in this review):**
- 🟡 MEDIUM — File List omitted `DomainServiceSdkHostAdoptionAdrTests.cs` (new source) and `tests/test-summary.md` (modified). Both are this story's work; added to File List.
- 🟢 LOW — Dev Agent Record claimed full test execution was "blocked by sandbox socket restrictions in VSTest," but `test-summary.md` and review re-run show the architecture tests pass via the in-process runner. Record corrected.

**Observations (no change made — intentional/convention-matching):**
- `RepositoryRoot()` is duplicated across architecture test files; this matches the established per-file convention in the project, so no extraction was applied.
- ADR lists the 5 canonical POST endpoints required by AC3 and omits the SDK's `GET /` root probe — intentional and within scope.

**Verification performed:** `dotnet build tests/Hexalith.ChatBot.Architecture.Tests` → 0 warnings / 0 errors; `DomainServiceSdkHostAdoptionAdrTests` → 4/4 pass; `git diff --check` clean; no `Hexalith.EventStore` submodule changes. No CRITICAL or HIGH issues.
