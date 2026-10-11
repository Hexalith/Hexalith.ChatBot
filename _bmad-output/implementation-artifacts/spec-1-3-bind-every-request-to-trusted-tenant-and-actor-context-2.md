---
title: 'Story 1.3: Reconcile Trusted Context Completion Evidence'
type: 'bugfix'
created: '2026-10-11'
status: 'done'
baseline_commit: d29f7f28c128f86f7034caf4ca93453adc349673
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

## Approved Scope Amendment — 2026-10-11

The user explicitly approved completing the missing production A13 owner-authority mappings in this reconciliation. This supersedes the earlier *scope limitations* in the frozen block that kept A13 open and prohibited owning-sibling changes; the historical frozen text remains untouched. Use current, tenant-bound owner evidence and exact scoped grants from the owning modules. An authenticated token identifies the actor but its roles or grant claims cannot confer owner authority. Synthetic test providers and operator credentials cannot authorize the production path. Missing, stale, revoked, cross-tenant, mismatched, or unavailable owner evidence denies. Owning sibling source changes and local inner commits/root gitlinks are authorized; no push. Complete the original Story 1.3 only after all required builds, native tests, live Aspire primary, and Story Evidence Gate pass. Otherwise retain the original story and sprint entry at `review` and record the exact blocker.

### Additional owner-mapping work

- [x] `src/Hexalith.ChatBot.Server/Authorization/` and `Gateway/CommandGatewayServiceCollectionExtensions.cs` — implement and register the production owner provider for all closed-catalog ChatBot operation, service-grant, Projects project, Parties identity, and Tenants tenant-owner requests. Map create commands to verified parent/target grants, including canonical dispatch/fallback targets; use the request's exact tenant, principal, resource, operation, actor class, and origin. Deny every unsupported combination.
- [x] `references/Hexalith.Projects/src/`, `references/Hexalith.Parties/src/`, and `references/Hexalith.Tenants/src/` — where current owner contracts cannot return the necessary independently current grant evidence, add narrow owner-side read contracts and implementations in the owning repository. Read its guidance, configuration and tests first; use its persisted owner data and event/projection freshness rules. Make local Conventional Commits and update root gitlinks for changed siblings only.
- [x] `src/Hexalith.ChatBot.Server/Authorization/`, `tests/Hexalith.ChatBot.Server.Tests/`, and `tests/Hexalith.ChatBot.IntegrationTests/` — prove positive exact grants and negative token-only, operator-only, synthetic-only, cross-tenant, missing, revoked, stale, mismatched-resource/operation/origin, owner outage, and canonical target cases. The live primary must use the production registration and persisted owner grants.
- [x] `_bmad-output/implementation-artifacts/evidence/1-3-bind-every-request-to-trusted-tenant-and-actor-context.json`, original Story 1.3 spec, and `sprint-status.yaml` — update exact scope, tasks, native results and provenance; transition the original story to `done` only if every required gate passes, else leave it at `review` with command and result.

Additional acceptance:

- Given current exact owner grants in their owning modules, when an authenticated actor calls a supported ChatBot operation, then the production provider returns bounded, request-bound evidence and the command/read succeeds only within those grants.
- Given token roles or grant claims, synthetic providers, operator credentials, stale or revoked owner records, or any mismatched tenant, principal, resource, operation, actor class or origin, when authorization runs, then it fails closed without dispatch or protected disclosure.
- Given the complete candidate and live topology, when the required source/Release builds, native lanes, live Aspire primary, and Story Evidence Gate run, then all pass with zero skips before the original story or sprint entry becomes `done`.

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

- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` — align the central EventStore family to 3.120.0 and MessagePack to 3.1.11; validate Builds-owned catalog rules before updating the root gitlink.
- [x] `references/Hexalith.Parties/Directory.Packages.props` — remove the temporary 3.117.1 override so Parties follows the aligned shared catalog; validate the Parties package/source modes before updating its root gitlink.
- [x] `src/Hexalith.ChatBot.Server/Adapters/Folders/FoldersFolderStore.cs` and its focused tests — submit the generated add-file request accepted by both source and package clients; preserve payload, idempotency, and safe error behavior.
- [x] `src/Hexalith.ChatBot.AppHost/Program.cs` and `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` — start the existing topology and run its live primary assertions after source builds pass.
- [x] `_bmad-output/implementation-artifacts/evidence/1-3-bind-every-request-to-trusted-tenant-and-actor-context.json` — define the exact completion contract, current-run results and provenance, and map the original story's tasks and acceptance criteria.
- [x] `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` — record the user-approved scope exception and verification; transition to `done` only after the evidence gate passes.

**Acceptance Criteria:**

- Given the aligned sibling revisions, when Debug Server.Tests and Release IntegrationTests build, then neither duplicate assemblies, missing administrator types, MessagePack downgrades, nor Folders type mismatches occur.
- Given the AppHost and required Story 1.3 tests, when the live topology runs, then the required primary assertions pass with non-skipped native results bound to the exact candidate revision.
- Given a proposed Story 1.3 completion, when the repository evidence gate validates its contract, then exact scope, task mapping, primary results, and provenance pass before either status changes to `done`.
- Given any unresolved blocker, when verification is reported, then the original story and sprint entry remain `review` with the exact command and failure recorded.

## Implementation Notes

- The Builds catalog now pins the EventStore family at 3.120.0 and MessagePack at 3.1.11. Parties inherits that catalog without its former 3.117.1 override. Both inner revisions are local commits and the root gitlinks identify them; neither was pushed.
- The Folders adapter uses the generated typed upload request in both source and package modes. Focused tests exercise payload bytes, operation and idempotency identity, and safe error conversion.
- The AppHost supplies EventStore workload and channel credentials, and the live test starts an Aspire-managed Memories probe Dapr sidecar. The probe loads the live owner module with its own dependency context so that the runner's package-mode contracts do not replace the source-mode owner assembly.
- The ChatBot boundary policy delegates ChatBot-owned endpoints to their existing authorization guards while preserving the SDK fallback for other routes. The live Keycloak realm uses Keycloak's `oidc-sub-mapper` for a human access-token subject claim; the first attempt with a `basic` default scope still emitted no `sub` and was replaced.
- The production `UnavailableChatBotOwnerAuthorityProvider` still denies owner authority. A13 owner mappings and release acceptance remain open. No synthetic authority is installed for the primary acceptance path.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and route | Evidence |
| --- | --- | --- |
| Blind 1 — ChatBot source rejects persisted rejection events | high; patch | `ChatBotGrantSourceEvidence.FromStream` returns null for every unrecognized event, while EventStore persists `ChatBotAuthorityChangeRejected`. A failed command can make an otherwise active grant unreadable. |
| Blind 2 — Projects rejection set is empty | false; reject | `ProjectChatBotAuthorityQueryHandler.RejectionTypes` scans the full Contracts.Events assembly for `IRejectionEvent`; `ProjectChatBotAuthorityRejected` is in that assembly and implements that interface. It does not require `IProjectEvent`. |
| Blind 3 — expired ChatBot grant blocks renewal | medium; patch | `GovernedOperationAggregate.Handle(GrantChatBotAuthority)` rejects when a prior grant is unrevoked without comparing its expiry to the current time. |
| Blind 4 — expired Projects grant blocks renewal | medium; patch | `ProjectsDomainProcessor.ProcessChatBotAuthorityAsync` tests only `!prior.Revoked` when deciding whether the same scope can be reissued. `ProjectStateApply` also rejects that replacement on replay, so both admission and application need expiry-aware rules. |
| Blind 5 — owner HTTP timeout escapes bootstrap verifier | medium; patch | `ProjectOwnerBootstrapVerifier` catches transport and parsing exceptions but not `TaskCanceledException` from `HttpClient.Timeout`, so timeout does not become a normal fail-closed rejection. |
| Blind 6 — Projects bootstrap has no caller cancellation | medium; patch | The new remote checks receive `CancellationToken.None` because `IDomainProcessor.ProcessAsync` has no cancellation parameter. A bounded verifier timeout limits the resulting in-flight work; changing the EventStore processor interface is beyond this correction. |
| Blind 7 — nested service grant can disagree with source event | low; patch | The fold forwards `current.ServiceGrant` without rechecking its identifiers and expiry against the enclosing grant event. Current admission and aggregate validation prevent normal production issuance of such history, but a malformed source event should not produce authority evidence. |
| Blind 8 — unbounded owner reference count | medium; patch | `AuthorizeAsync` and `HasProjectAuthoritiesAsync` enumerate distinct payload references into sequential current-owner reads without a count limit. An admitted large payload can hold request work across many remote reads. |
| Blind 9 — EventStore complete-read cap can deny old streams | medium; defer | `AuthoritativeEventStreamReader` has an existing 10,000-event and 16 MiB cap; owner queries now use that bounded reader. Crossing the cap returns no authority, preserving fail-closed behavior but requiring a future bounded checkpoint or source summary for long-lived streams. |
| Blind 10 — production positive branches lack live coverage | medium; patch | The previous live primary exercised persisted human ChatBot grants, while project grant issuance/use, machine grant use, and revocation were absent from the connected path. |
| Edge 1 — rejection event lacks aggregate Apply method | high; patch | `GovernedOperationState` has Apply methods for grant and revoke but none for persisted `ChatBotAuthorityChangeRejected`; the EventStore replay resolver requires an Apply method for each event in the tail. |
| Edge 2 — rejection event discards active grant | high; patch | This is the separate reviewer report of Blind 1: the actual source has an `else return null`, so a rejection after an active grant causes a denied fold. Its proposed guard snippet was not present. |
| Edge 3 — live stability permits one success then denials | medium; patch | The stability loop retries HTTP 403 but ends after the time window without requiring a second successful authorized read, despite the comment saying a later read is required. |
| Verification 1 — no connected Projects grant issue/use test | medium; patch | Project tests supply grant events directly and ChatBot tests use a synthetic owner provider. A failed production bootstrap could still pass those tests. Add persisted grant issuance and a project-scoped ChatBot request to the live topology. |
| Verification 2 — no revocation admission test | medium; patch | Aggregate and dispatcher tests bypass admission; the live test previously seeded grants only. Exercise revocation through the API and verify the revoked grant no longer authorizes a request. |

## Verification

### A13 owner-source audit — 2026-10-11

The ChatBot aggregate now has a protected `GrantChatBotAuthority` / `RevokeChatBotAuthority` EventStore lifecycle, a deterministic exact-scope stream key, and strict source-history folding. Its separate bootstrap admission requires current Tenants `TenantOwner` and Parties issuer identity evidence; it grants no ordinary ChatBot authority to the issuer. The production `IChatBotOwnerAuthorityProvider` remains `UnavailableChatBotOwnerAuthorityProvider`, so this work is fail closed and not production-positive. Projects' `ProjectState` and `ProjectDetailItem` still contain no principal-scoped project grants, and its `GetProject` directory adapter authorizes the adapter's operator identity rather than the bound requester. Tenants persists membership roles; Parties has a source-backed identity query requiring signed admission evidence. The live AppHost starts Tenants but not Projects or Parties owner servers. EventStore's workload stream-read route requires tenant- and domain-bound assertions, while its OIDC `JwtWorkloadAssertionIssuer` refuses resource bindings; owner-side current query contracts and secure transport remain to implement. The original story and sprint entry remain `review`.

`dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 /nr:false` exited 0 with zero warnings and zero errors. `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story13-owner-audit/owner-audit.ctrf.json -result-trx TestResults/story13-owner-audit/owner-audit.trx -class Hexalith.ChatBot.Server.Tests.TrustedAuthorityTests` exited 0 with 70 passed, zero failed, zero skipped. These checks confirm the current fail-closed baseline; they do not satisfy the new production-positive acceptance.

After the partial A13 implementation, `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 /nr:false` exited 0 with zero warnings and zero errors. The native xUnit `ChatBotAuthorityGrantAggregateTests` lane passed 3/3 with zero skips (`TestResults/story13-grant/aggregate-final.ctrf.json`); `TrustedAuthorityTests` passed 73/73 with zero skips (`TestResults/story13-grant/authority-final.ctrf.json`). These checks cover exact grant replay, revocation, issuance-ID reuse, and bootstrap fail-closed behavior. They do not establish production-positive owner reads.

The current exact `attest` preflight with base `d29f7f28c128f86f7034caf4ca93453adc349673`, head `64afc6a857d3641467cd2b1ddb313ab0c2041f56`, and `TestResults` exited 2 with `scope_digest_mismatch` at `root:references/Hexalith.Projects`: that changed root gitlink is absent from the draft contract. A second local preflight with base and head both `64afc6a857d3641467cd2b1ddb313ab0c2041f56` exited 2 with `gitlink_scope_mismatch` at `references/Hexalith.Builds`, because the draft contract binds the earlier transition's inner revisions. The contract must be reconciled with the eventual exact candidate before attestation is retried. The prior live primary also remains failed with HTTP 403 `authorization_denied`, so no passing production owner path or completion provenance exists.

The Builds catalog validators and Parties package and source builds passed. Debug Server.Tests built without warnings or errors and its native lane passed 2,536/2,536 with zero skips. Release IntegrationTests and AppHost builds passed without warnings or errors. The Architecture native lane passed 123/123 with zero skips. Release Conformance built in package mode and its native lane passed 149/149 with zero skips. The focused Bootstrap and Folders tests passed 106/106 and 9/9 respectively.

Standalone `aspire run --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` exited 134 because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` was missing in that standalone environment (`/tmp/story13-reconcile-aspire.log`). The in-process primary test supplies its local secret and starts the topology.

The exact native primary command is `HEXALITH_CHATBOT_TIER3=1 PATH="$HOME/.dapr/bin:$PATH" XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/Hexalith.ChatBot.IntegrationTests TestResults/topology-primary/topology-acceptance.ctrf.json -result-trx TestResults/topology-primary/topology-acceptance.trx -method Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests.TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology -showLiveOutput`. It exited 1: one failed, zero skipped. Aspire/Dapr and four real persisted Memories owner commands passed their probes. `SubmitGovernedNoteUntilAcceptedAsync` then received HTTP 403 `authorization_denied` from ChatBot at line 2752 (`/tmp/story13-reconcile-topology-submapper.log`). The production owner provider remains unavailable until A13 maps the ChatBot resource authority; this is the exact acceptance blocker. The original story and sprint entry remain `review`.

`dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -- attest --contract _bmad-output/implementation-artifacts/evidence/1-3-bind-every-request-to-trusted-tenant-and-actor-context.json --base d29f7f28c128f86f7034caf4ca93453adc349673 --head d29f7f28c128f86f7034caf4ca93453adc349673 --results TestResults` exited 2 with `machine_results_invalid` for `aspire-dapr-primary`; no provenance sidecars were written. The matching `validate --target-status done` command exited 1 with `status_mismatch` / `story-transition`, as required while story and sprint remain `review`. The draft contract's scope digest is calculated from the exact candidate; it is not an assertion that completion evidence passed.

**Commands:**

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 /nr:false` — expected: source-reference build succeeds without diagnostic overrides.
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release -m:1 /nr:false` — expected: integration project compiles.
- Run the Architecture native xUnit lane with zero skips — expected: 123 passing tests.
- `aspire run --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` — expected: topology starts for the live primary lanes.
- Run the exact `attest` and `validate` commands in `docs/story-evidence-integrity.md` with full Git revisions — expected: success only with bound evidence.

### A13 production candidate verification — 2026-10-11

The production ChatBot owner provider now reads exact persisted ChatBot grants and bound current owner queries from Tenants, Parties, and Projects. EventStore verifies signed workload proofs over the exact requester and command/query/stream, and the distinct grant bootstrap rechecks Tenants TenantOwner and Parties identity without giving the issuer ordinary ChatBot authority. Projects persists scoped grant and revoke events. The Aspire graph starts those owner services and their sidecars. Local owning commits are EventStore `819cc1223598f02eee510cfa751f37424936e1cf`, Tenants `6515be441763d368110fe042e16748b2ce2b8467`, Parties `7816099158bfb252658b78f69cecafecc853228f`, Projects `ffe1db5295d19f6637b62749f561d3ccb4266045`, and Conversations `a570783f589649e9ea884efd8ef48917d85032e6`; none was pushed.

The shared Builds catalog now pins SwaggerUI 10.3.0 as required by EventStore.Gateway 3.120.0 (local Builds revision `3d931f3f0fd47708579e0ad644aad67df58420bc`). The exact Tenants Server.Tests Debug package-mode build that initially failed NU1109 passed after this correction with zero warnings/errors; its owner query test passed 1/1. The Parties owner query and Projects owner query/grant lifecycle tests each passed 1/1. The Builds central package validator passed all 304 entries. The Debug Server.Tests build passed with zero warnings/errors and its native lane passed 2,546/2,546 with zero skips. The Release package-mode IntegrationTests build passed with zero warnings/errors using the local packages from the edited owner sources. EventStore's bound workload command tests passed 7/7 and bound owner query test passed 1/1, both zero skips. Conformance passed 149/149, Architecture passed 123/123, and hosting-assets UI E2E passed 4/4, all zero skips. The Story Evidence Gate self-test lane passed 219/219 with zero skips. The final-candidate live primary passed 1/1 with zero skips in 172.361 seconds in `TestResults/topology-primary/topology-acceptance.trx`: both persisted tenant/party chains, five exact ChatBot grants, governed commands, status, view, audit, and replay were exercised in the real Aspire/Dapr topology. Earlier local live attempts saw retryable owner transport or idempotency 503 responses; the test retains fail-closed behavior and uses bounded same-command-ID retry during grant seeding.

The post-catalog live primary passed 1/1 with zero skips in 107.119 seconds after a separate local attempt encountered a retryable owner transport 503 during grant seeding. The original Story 1.3 and sprint entry are the proposed `done` transition, subject to the exact Story Evidence Gate report for the committed candidate. No remote publication is claimed.

### Final A13 review and verification — 2026-10-11

The review corrections now fold persisted ChatBot rejection events without discarding an active grant, allow expired exact grants to be renewed, reject malformed nested service grants, bound owner-reference fanout, and replay rejection events in the aggregate. Projects bounds bootstrap verification, permits expired grant replacement, and advertises its owner query as operational metadata so EventStore routes it to the owning handler. EventStore gives verified internal callers bounded rate-limit partitions while unverified callers retain the anonymous limits. The existing complete-stream read cap remains a fail-closed deferred item in `deferred-work.md`.

The live primary now persists a Projects grant, reads the protected project conversation, revokes the grant, and verifies HTTP 403 after revocation. Local diagnostic runs also found an EventStore terminal infrastructure rejection with no stored event during ChatBot grant seeding. The fixture changes command ID only after reading that definitive no-event status from EventStore; uncertain or committed outcomes retain the original ID. It never uses token roles, operator credentials, or a synthetic owner provider to grant authority.

Final local owning revisions are EventStore `937d77df1fa473f091cc1099ad20240dbc500707` and Projects `7aef021a0c7cacd84962a1451b3956d94468e9dc`. The Projects focused server tests passed 18/18 and aggregate test passed 1/1. EventStore's verified-sidecar and anonymous rate-limit integration tests each passed 1/1, and its rate-limit unit tests passed 3/3. The final root builds passed with zero warnings/errors for Debug Server.Tests and Architecture.Tests and Release Conformance.Tests, UI.E2E.Tests, StoryEvidenceGate.Tests, and IntegrationTests. Native lanes passed Server **2,553/2,553**, Conformance **149/149**, Architecture **123/123**, hosting-assets primary **4/4**, and gate self tests **219/219**, all with zero skips. Release package builds used the exact local A13 owner packages. The final clean live Aspire/Dapr primary passed **1/1**, zero skips, in **102.871 seconds** at `TestResults/topology-primary/topology-acceptance.trx`; it exercised persisted tenant, party, ChatBot and Projects owner chains, protected read, revocation denial, governed command, status, view, audit, and replay. The exact Story Evidence Gate report is the authoritative completion result.
