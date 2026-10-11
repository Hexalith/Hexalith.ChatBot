---
title: 'Story 1.3: Reconcile Trusted Context Completion Evidence'
type: 'bugfix'
created: '2026-10-11'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md'
  - 'docs/story-evidence-integrity.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.3 is implemented but remains at `review`: its source and live checks encounter mismatched EventStore versions, an outdated Folders call, and a MessagePack downgrade.

**Approach:** Align the shared package catalog with the current EventStore source and its transitive dependencies, remove the conflicting Parties pin, adapt the ChatBot Folders call to the current typed contract, then run the source, integration, live, and evidence gates.

## Boundaries & Constraints

**Always:** Preserve Story 1.3's fail-closed owner authority, freshness, safe outcomes, and existing Release behavior. Preserve project references in Debug and package references in Release. Use the existing `.slnx`, native xUnit runners, and evidence gate. Keep A13 owner mappings and release acceptance explicitly open.

**Never:** Silence duplicate assembly errors by weakening the build, substitute synthetic harnesses for required live evidence, claim production owner authorization from the unavailable provider, or mark the original story `done` without a passing evidence contract. Preserve unrelated checkout changes.

**Scope decision (2026-10-11):** The user permitted sibling compatibility and dependency alignment. The pushed root commit now records the current EventStore and Parties revisions; preserve that baseline. A13 and release acceptance remain open.

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md` and `sprint-status.yaml` — approved work and `review` state; preserve the frozen block.
- `Directory.Build.props`, `Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props` — root uses Debug project references; shared catalog pins EventStore 3.112.0 and MessagePack 3.1.10. Align to EventStore 3.120.0 and MessagePack 3.1.11 in Builds, retaining central package authority.
- `references/Hexalith.Parties/Directory.Packages.props` — conditional EventStore 3.117.1 pin precedes the shared import and causes mixed contracts in Debug; remove after catalog alignment.
- `references/Hexalith.Tenants/src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs` and `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/` — Tenants' administrator types exist in current source and 3.117.1+ packages; expect no source edit unless aligned builds reveal one.
- `src/Hexalith.ChatBot.Server/Adapters/Folders/FoldersFolderStore.cs` and `tests/Hexalith.ChatBot.Server.Tests/Adapters/Folders/FoldersFolderStoreTests.cs` — Debug exposes `AddFileAsync` requiring generated `AddFileRequest`; retain operation ID, idempotency key, and streaming/denial mapping in the adapter.
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` and `tests/Hexalith.ChatBot.IntegrationTests/` — live topology and primary test path; no running AppHost exists after the failed build.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` — the former Conversations wrapper failure has cleared; current native Architecture lane passes 123/123 with zero skips.
- `docs/story-evidence-integrity.md`, `_bmad-output/implementation-artifacts/evidence/` — exact TRX, provenance, scope, and task mapping required for completion. No Story 1.3 contract exists.

## Tasks & Acceptance

**Execution:**

- [ ] `references/Hexalith.Builds/Props/Directory.Packages.props` — align the central EventStore family to 3.120.0 and MessagePack to 3.1.11; validate Builds-owned catalog rules before updating the root gitlink.
- [ ] `references/Hexalith.Parties/Directory.Packages.props` — remove the temporary 3.117.1 override so Parties follows the aligned shared catalog; validate the Parties package/source modes before updating its root gitlink.
- [ ] `src/Hexalith.ChatBot.Server/Adapters/Folders/FoldersFolderStore.cs` and its focused tests — submit the generated add-file request accepted by both source and package clients; preserve payload, idempotency, and safe error behavior.
- [ ] `src/Hexalith.ChatBot.AppHost/Program.cs` and `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` — start the existing topology and run its live primary assertions after source builds pass.
- [ ] `_bmad-output/implementation-artifacts/evidence/1-3-bind-every-request-to-trusted-tenant-and-actor-context.json` — define the exact completion contract, current-run results and provenance, and map the original story's tasks and acceptance criteria.
- [ ] `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` — record the user-approved scope exception and verification; transition to `done` only after the evidence gate passes.

**Acceptance Criteria:**

- Given the aligned sibling revisions, when Debug Server.Tests and Release IntegrationTests build, then neither duplicate assemblies, missing administrator types, MessagePack downgrades, nor Folders type mismatches occur.
- Given the AppHost and required Story 1.3 tests, when the live topology runs, then the required primary assertions pass with non-skipped native results bound to the exact candidate revision.
- Given a proposed Story 1.3 completion, when the repository evidence gate validates its contract, then exact scope, task mapping, primary results, and provenance pass before either status changes to `done`.
- Given any unresolved blocker, when verification is reported, then the original story and sprint entry remain `review` with the exact command and failure recorded.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 /nr:false` — expected: source-reference build succeeds without diagnostic overrides.
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release -m:1 /nr:false` — expected: integration project compiles.
- Run the Architecture native xUnit lane with zero skips — expected: 123 passing tests.
- `aspire run --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` — expected: topology starts for the live primary lanes.
- Run the exact `attest` and `validate` commands in `docs/story-evidence-integrity.md` with full Git revisions — expected: success only with bound evidence.
