---
title: 'Story 1.3: Reconcile Trusted Context Completion Evidence'
type: 'bugfix'
created: '2026-10-11'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md'
  - 'docs/story-evidence-integrity.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.3's trusted tenant and actor binding is implemented and reviewed, but its mandatory source-build and live-runtime evidence cannot run against the current sibling versions. The existing story remains at `review`, and its approved scope excludes sibling and dependency changes.

**Approach:** Reconcile the current validation failures, repair the approved build boundary, and produce exact evidence for any completion transition. Keep Story 1.3 at `review` unless every required primary path and the repository evidence gate pass.

## Boundaries & Constraints

**Always:** Preserve the original Story 1.3 frozen intent, fail-closed owner authority, current owner freshness, safe outcomes, and existing Release behavior. Preserve project references in Debug and package references in Release. Use the existing `.slnx`, native xUnit runners, and evidence gate. Keep A13 owner mappings and release acceptance explicitly open.

**Never:** Silence duplicate assembly errors by weakening the build, substitute synthetic harnesses for required live evidence, claim production owner authorization from the unavailable provider, or mark the original story `done` without a passing evidence contract. Preserve unrelated checkout changes.

**Scope decision (2026-10-11):** Expand the original Story 1.3 boundary to permit coordinated compatibility changes in the owning Tenants and EventStore repositories and dependency alignment. No A13 owner mapping or release acceptance is implied.

</frozen-after-approval>

## Open Questions

- **Existing submodule checkout drift:** The root records older EventStore and Parties gitlinks, while both local checkouts point to newer commits. **Use current checkouts** treats those revisions as this run's baseline and may propose corresponding pointer changes. **Use isolated checkouts** starts from the root-recorded revisions without moving the user's current checkouts, then aligns dependencies there.

## Code Map

- `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md` and `sprint-status.yaml` — approved work and `review` state; preserve the frozen block.
- `Directory.Build.props`, `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`, `Directory.Packages.props` — Debug project-reference selection and package graph. Current Debug Server.Tests build imports EventStore.Contracts 3.117.1 and 3.112.0, failing with `MSB3243`/`CS1704`.
- `references/Hexalith.Tenants/src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs` and `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/` — Tenants expects administrator types absent from the selected EventStore source graph; current IntegrationTests and AppHost builds fail with `CS0246`. These are separate owning repositories.
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` and `tests/Hexalith.ChatBot.IntegrationTests/` — live topology and primary test path; no running AppHost exists after the failed build.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` — the former Conversations wrapper failure has cleared; current native Architecture lane passes 123/123 with zero skips.
- `references/Hexalith.Builds/Props/Directory.Packages.props` — root pins EventStore packages to 3.112.0; Tenants now needs newer DomainService administrator types.
- `docs/story-evidence-integrity.md`, `_bmad-output/implementation-artifacts/evidence/` — exact TRX, provenance, scope, and task mapping required for completion. No Story 1.3 contract exists.

## Tasks & Acceptance

**Execution:**

- [ ] `Directory.Build.props`, `Directory.Packages.props`, and `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` — trace and correct the mixed EventStore.Contracts graph within the approved ownership boundary; retain Debug source and Release package modes.
- [ ] `references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj` and the referenced EventStore DomainService API — if scope expands, make the owning-module compatibility change and verify it in each owner repository; otherwise record the exact unresolved blocker.
- [ ] `tests/Hexalith.ChatBot.IntegrationTests/` and `src/Hexalith.ChatBot.AppHost/` — rebuild, start the local topology, and execute the actual required primary paths once the source graph is healthy.
- [ ] `_bmad-output/implementation-artifacts/evidence/1-3-bind-every-request-to-trusted-tenant-and-actor-context.json` — define the exact completion contract, current-run results and provenance, and map the original story's tasks and acceptance criteria.
- [ ] `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md` and `sprint-status.yaml` — update verification facts; transition to `done` only after the evidence gate passes.

**Acceptance Criteria:**

- Given the current sibling revisions, when Debug Server.Tests and Release IntegrationTests build, then both complete without duplicate assemblies or missing administrator types.
- Given the AppHost and required Story 1.3 tests, when the live topology runs, then the required primary assertions pass with non-skipped native results bound to the exact candidate revision.
- Given a proposed Story 1.3 completion, when the repository evidence gate validates its contract, then exact scope, task mapping, primary results, and provenance pass before either status changes to `done`.
- Given any unresolved blocker, when verification is reported, then the original story and sprint entry remain `review` with the exact command and failure recorded.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 /nr:false` — expected: source-reference build succeeds.
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release -m:1 /nr:false` — expected: integration project compiles.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story13-architecture.ctrf.json -result-trx TestResults/story13-architecture.trx` — expected: all current Architecture tests pass.
- `aspire run --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` — expected: topology starts for the live primary lanes.
- Run the exact `attest` and `validate` commands in `docs/story-evidence-integrity.md` with full Git revisions — expected: success only with bound evidence.
