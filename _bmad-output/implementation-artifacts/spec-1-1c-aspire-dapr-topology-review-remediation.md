---
title: 'Story 1.1c — close the 2026-09-13 Aspire/DAPR topology review findings'
type: 'bugfix'
created: '2026-09-14'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '76f355a038c4abdb3b9fdb3fb836c25053a18fb0'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/1-1-scaffold-the-buildable-hexalith-chatbot-module.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story `1-1c-aspire-dapr-topology-and-local-run-verification` cannot leave `in-progress`: its 2026-09-13 re-verification left 16 open `[Review][Patch]` findings. The production DAPR ACL denies every EventStore→ChatBot route except `/process` under mTLS; seven projection subscriptions dead-letter into a topic nothing consumes; the AppHost's Keycloak seed-grant policy is undocumented and hand-counted; the required `topology-acceptance` CI gate passes on zero executed tests; and six assertions in the Tier-3 acceptance lane are vacuous, mis-ordered, or over-budget.

**Approach:** Work the findings as one closure pass over four surfaces — production ACL + its conformance guard, dead-letter subscription + its guard, AppHost Keycloak seed-grant policy + a consumer-level expiry test, and the CI/E2E acceptance lane — replacing each source-text guard with one that fails when the real thing drifts.

## Boundaries & Constraints

**Always:**
- Deny-by-default stays the production posture: only appId `eventstore`, only POST, only routes actually invoked cross-app. The environment split (`accesscontrol.yaml` production/mTLS-on vs `accesscontrol.local.yaml` local/mTLS-off) must not be blurred.
- Every new guard must be able to fail: assert observed values, not source text, wherever a seam exists. `tests/Hexalith.ChatBot.IntegrationTests` has `InternalsVisibleTo` on `Hexalith.ChatBot.Server`.
- Dead-letter logging is metadata-only — topic, pubsub, message/correlation id. Never the message body (Epic 1 redaction floor).
- Follow the recorded 2026-09-13 decisions in the story's review section; where a recorded list is factually incomplete, extend it and say so in Implementation Notes.

**Never:**
- Do not touch anything under `references/` — the EventStore submodule is read-only reference.
- Do not fix the four `[Review][Defer]` items or the four `deferred-work.md` entries (ci/release job duplication, `dotnet-version` 10.0.302 drift, `keyPrefix`, temp-path realm) — they are out of scope.
- Do not add production provisioning, rotation, or a deployment manifest to the AppHost; it stays `IsPublishable=false` local-only.
- Do not relax `timeout-minutes`, weaken a gate, or delete a failing assertion to get green.

**Decisions (2026-09-14):**
- **Verification depth:** implement against the verified static evidence and run build plus the non-live suites; the live Aspire/DAPR proof of the tightened `topology-acceptance` assertions is left to CI's own topology-acceptance job. Do not attempt a live Tier-3 run in this session. Consequence accepted: the required gate is not proven locally, so the tightened values (resource state text, per-resource endpoints, `Forbidden`) must be derived from evidence in the Code Map rather than guessed, and anything that cannot be grounded that way must be reported rather than asserted speculatively.
- **Scope:** all 16 open `[Review][Patch]` findings stay in this one spec despite its size; work surface-by-surface (ACL → dead-letter → AppHost/Keycloak → CI/MSBuild → E2E).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| EventStore invokes a mapped SDK route | `POST /replay-state` from appId `eventstore`, mTLS on | Allowed by `accesscontrol.yaml` | N/A |
| Any other caller or verb | `GET /process`, or appId `chatbot`/unknown | Denied by `defaultAction: deny` | N/A |
| SDK adds a new POST route | New `MapPost` literal in `EventStoreDomainServiceExtensions.cs` absent from the ACL | Conformance test fails, naming the route | Test failure |
| A third policy is added with `defaultAction: allow` | ACL gains an unlisted policy | Conformance test fails — every policy is checked, list is exhaustive | Test failure |
| Poison projection message | Message redelivered past retry to the configured dead-letter topic | Dead-letter subscriber receives it, logs Error with metadata only, returns 200 to drain | Body never logged |
| Realm renders a grant expiry | AppHost substitutes the expiry placeholder | Value is accepted by `AuditMetadata.IsSafeStableIdentifier` and parses as `DateTimeOffset` | Test fails if `+00:00` form returns |
| Realm client missing the expiry mapper | A `chatbot-service-client-grant-expiry` mapper without the placeholder | AppHost throws naming the offending client id | `InvalidOperationException` |
| `KeycloakPersistent=true` | Reused Keycloak container | AppHost warns on stderr that the validated expiry may not be the one Keycloak serves, pointing at `docker rm -f` | Warning, not failure |
| Topology test filter matches nothing | `topology-acceptance.trx` with `executed=0` | CI step fails | Assertion error |
| Unauthenticated submit, then read | Read of the never-created note with a valid token | `403 Forbidden` (safe-not-found), not merely "not 200" | N/A |

</frozen-after-approval>

## Code Map

**Production ACL**
- `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml:27` — the single `/process` allow to widen; the mTLS rationale comment above it must be preserved and extended, not deleted.
- `tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs` — YamlDotNet-based; currently checks two *named* policies and never asserts the policy list is complete. `RepositoryRoot()` helper at the bottom is the path pattern to reuse.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` — read-only. Maps `GET /` (:210) plus 13 POST routes (:212–:295). Verified cross-app callers: `/process`, `/replay-state` (`DaprAggregateStateReconstructor.cs:113`), `/query`, `/project`, `/project/v2`, `/project/v2/reconcile` (`NamedProjectionDispatchCoordinator.cs:750`), `/project/rebuild/{stage,commit,abort,verify}/v1`, `/admin/operational-index-metadata`. `/project/rebuild/v1` and `/project/rebuild/shared/v1` are mapped with no EventStore-side caller; `GET /` is a banner endpoint with no cross-app caller. No route has a path parameter, so all match as ACL literals.

**Dead letter**
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs:123-134` — resolves pubsub/topic/dead-letter from config with hardcoded fallbacks, calls `MapSubscribeHandler()` then the seven mappers.
- `src/Hexalith.ChatBot.Server/Projections/*ProjectionEndpoints.cs` (Governed:27/67, MailboxIntake:13/49, Association:13/42, ParticipantResolution:13/42, AiOutcome:22/44, TaskIntent:13/35, Approval:13/35) — all seven already set `DeadLetterTopic`, all use `Dapr.AspNetCore` `.WithTopic(new TopicOptions { PubsubName, Name, DeadLetterTopic })`. Copy this shape for the new subscriber; the same `(pubSubName, topicName, deadLetterTopic)` signature and `ArgumentException.ThrowIfNullOrWhiteSpace` guards.
- `src/Hexalith.ChatBot.AppHost/Aspire/ChatBotAspireModule.cs:56` `DeadLetterTopicName = "deadletter.chatbot.events"` — no runtime consumer, referenced only as source text by two tests. `:60 GetTenantDeadLetterTopic` returns `deadletter.{tenant}.{PubSubTopicName}` and IS used, at `src/Hexalith.ChatBot.AppHost/Program.cs:98`. The two are deliberately different values.
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs:305-327` `AppHostShouldWireSubscriberDeadLetterRoutingIntoDaprDiscovery` — reads only three files and asserts five substrings; six subscribers unguarded. This project has **no** ProjectReference; it is source-text only via its own `RepositoryRoot()` at :391.

**AppHost Keycloak seed grants**
- `src/Hexalith.ChatBot.AppHost/Program.cs:185` `PrepareKeycloakRealmImport` — a static local function in top-level statements, called at :25, receives `builder.Configuration`. `expectedServiceGrantCount = 7` at :189; placeholder count check at :246-250; substitution at :252. Existing stderr warning style at :415.
- `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json` — 7 occurrences of `__HEXALITH_CHATBOT_SERVICE_GRANT_EXPIRES_AT__`, each at `clients[].protocolMappers[]` where `name == "chatbot-service-client-grant-expiry"` → `config."claim.value"` (lines 186, 340, 494, 648, 803, 957, 1111).
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsServiceClientGrantResolver.cs:17,:64-71,:113` — the consumer. `TryReadSingleSafe` requires `AuditMetadata.IsSafeStableIdentifier`, then invariant `DateTimeOffset.TryParse`; both failures collapse to `service_client_grant_missing`.
- `src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs:17,:25` — `internal static`; charset allows `. - _ : @ |` and rejects `+`. Canonical `"O"` UTC passes.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs:350-402` — the working seam: builds the AppHost via `DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, …)`, finds the rendered realm directory by owner marker, asserts on `hexalith-realm.json`, cleans up in `finally`. Reuse this shape.
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs:97` `AppHostShouldEmitSafeCanonicalUtcServiceGrantExpiryTokens` — the source-text guard to replace.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs:56-57` — read-only; documents that a reused container does NOT re-import the realm and that `docker rm -f` is the remedy. Key name: `HexalithEventStoreSecurityOptions.DefaultPersistentConfigurationKey` = `"KeycloakPersistent"`.

**CI / MSBuild**
- `.github/workflows/ci.yml:457-527` — the `topology-acceptance` job. `:500` writes `topology-acceptance.trx` unguarded; `:514` is the counter guard to mirror.
- `.github/workflows/release.yml:26-92` — same job; `:71` unguarded, `:85` is the guard. `semantic-release` depends on this job (`:273`).
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj:40-43` — the unconditional `AdditionalProperties` forward. `Directory.Build.props:4` defines `HexalithCommonsRoot` with an `Exists(...)` fallback.

**Tier-3 acceptance lane** — `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs`
- `:71-80 RequiredTopologyResources` (7: security, eventstore, tenants, chatbot, chatbot-ui, eventstore-admin, eventstore-admin-ui); `:54 ChatBotResourceName`; `:63-69 IsolatedDaprHttpResourceNames` (4).
- `:2043 WaitForAndRecordRequiredTopologyAsync` — `foreach` + `WaitForResourceHealthyAsync(...).WaitAsync(TimeSpan.FromMinutes(5))` at `:2050-2054`; `:2071` State null-check only; `:2072-2075` endpoints asserted for `chatbot` alone. Two more 3-resource copies of the same 5-minute pattern at `:378-382` and `:447-451`. Unused `:93 SelectedResourceValidationTimeout`.
- `:174` call to `AssertNoDurableStateWasCreatedAsync` (defined `:2122`, two `ShouldNotBe(HttpStatusCode.OK)` at `:2139` and `:2147`) sits **above** `:186 WaitForChatBotDaprSidecarAsync`, whose own comment says the sidecar must be verified first.
- `:2152 AssertGovernedOperationViewRemainsStableAsync` — `:2170 current.GetRawText().ShouldBe(expectedBody)`; `expectedBody` supplied at `:251-256`.
- `:2031 DerivedRecordShape(JsonElement)` — six bare `.GetProperty(...)` at `:2035-2040`; sole call site `:420`.
- `:2584 IsTransientStatusCode` — `|| (int)statusCode >= 500` subsumes the enumerated 502/503/504 and sweeps in 501/505. Call sites `:2334` and `:2534`, each retrying for `StartupTimeout` (3 min, `:92`).
- Reference for the 403 posture: `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs:21-23` — `SafeNotFound` is not `AuthenticationDenied`, so it maps to `Status403Forbidden`. Corroborated by `:2207 WaitForChatBotDaprSidecarAsync`, which already accepts `Forbidden` or `OK` as a live sidecar.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml` -- replace the lone `/process` allow with POST-scoped allows for the 13 cross-app SDK routes; keep and extend the mTLS rationale comment, noting why `GET /` is excluded -- under mTLS everything but `/process` is currently denied.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs` -- assert the policy list is exhaustive (known appIds only) and that *every* policy has `defaultAction: deny`; assert the eventstore operation set equals the expected route set exactly; add a drift guard that parses the SDK's mapped POST route literals from `EventStoreDomainServiceExtensions.cs` and fails naming any route the ACL omits -- a third `defaultAction: allow` policy and an SDK route addition both pass today.
- [x] `src/Hexalith.ChatBot.Server/Projections/ChatBotProjectionSubscriptionDefaults.cs` -- new: hold the pubsub/topic/dead-letter default literals -- removes the unshared fallback duplication at the compatibility extension.
- [x] `src/Hexalith.ChatBot.Server/Projections/ChatBotDeadLetterProjectionEndpoints.cs` -- new: map a subscribed dead-letter endpoint on the configured dead-letter topic (no `DeadLetterTopic` of its own), log Error with metadata only via source-generated `LoggerMessage`, return 200 to drain -- poison messages currently accumulate silently in Redis.
- [x] `src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs` -- consume the new defaults class and wire the dead-letter subscriber alongside the seven projection mappers.
- [x] `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs` -- widen the dead-letter guard to read all seven projection endpoint files and assert each sets `DeadLetterTopic`; assert the dead-letter subscriber is mapped; replace the `:97` grant-expiry source-text guard with a pointer to the new behavioral test -- six subscribers can silently lose dead-letter routing today.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/` -- add a test tying `ChatBotAspireModule.DeadLetterTopicName` to the Server's effective default dead-letter topic -- makes the dead constant load-bearing instead of source-asserted.
- [x] `src/Hexalith.ChatBot.AppHost/Program.cs` -- derive the expected grant-expiry placeholder count from the realm's `chatbot-service-client-grant-expiry` protocol mappers instead of the literal `7`, failing with the offending client id; warn on stderr when `KeycloakPersistent` is true that the validated expiry may not be what Keycloak serves (`docker rm -f` to re-import); add the non-production seed-credential policy comment -- the literal has already drifted 6→7 and the gate passes against a never-applied value in persistent mode.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs` -- add a test that renders the realm through the AppHost, reads every substituted `chatbot:service-client-grant-expiry` claim back out, and asserts `ClaimsServiceClientGrantResolver` accepts it (not `service_client_grant_missing`) -- the `+00:00` rendering bug shipped because every existing test hand-writes `...Z` literals.
- [x] `README.md` -- record the local-development seed-credential policy: AppHost realm clients are local seeds only, the pre-expiry gate is the guardrail, production provisioning lives outside the AppHost.
- [x] `.github/workflows/ci.yml` + `.github/workflows/release.yml` -- add a counter guard for `topology-acceptance.trx` mirroring the existing `story132-topology-acceptance.trx` step -- a renamed or moved test turns the required gate green while proving nothing.
- [x] `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` -- condition the `HexalithCommonsRoot` forwarding on a non-empty value -- an empty global property cannot be overridden by a sibling's own fallback.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- assert observed state text and non-empty endpoints for every required resource, not just `chatbot`; move the fail-closed probe below `WaitForChatBotDaprSidecarAsync` and assert `Forbidden` rather than "not OK"; replace the three hardcoded per-resource 5-minute waits with one shared budget that fits inside `timeout-minutes: 30`; compare `DerivedRecordShape` instead of raw JSON in the stability assertion; give the six `GetProperty` calls messages naming field and origin; simplify `IsTransientStatusCode` so 501/505 are permanent and the dead 502/503/504 branches are gone.

**Acceptance Criteria:**
- Given the production ACL, when the conformance suite runs, then a policy carrying `defaultAction: allow`, an unlisted policy appId, or an SDK POST route missing from the ACL each fail with a message naming the offender.
- Given a poison projection message, when it lands on the configured dead-letter topic, then a subscribed endpoint receives it and logs Error carrying topic/pubsub/message identity and no message body.
- Given the AppHost renders the realm, when a test reads the substituted expiry claim back through `ClaimsServiceClientGrantResolver`, then the resolution is not `service_client_grant_missing`.
- Given a realm client whose `chatbot-service-client-grant-expiry` mapper lacks the placeholder, when the AppHost starts, then it throws naming that client id.
- Given the `topology-acceptance` CI step, when its filter matches no test, then the job fails.
- Given the whole topology wait path, when every required resource is slow, then the accumulated wait cannot exceed the job's `timeout-minutes` budget.

## Implementation Notes

### Surface 1 — production ACL

- `accesscontrol.yaml` now grants the eventstore appId **13 POST-scoped routes** instead of the lone `/process`. The mTLS rationale comment is preserved and extended with a ROUTE SET paragraph recording why `GET /` is excluded and why the whole `/project/rebuild/*` family is granted even though two of its members have no EventStore-side caller today.
- **The recorded 2026-09-13 decision list was extended, as the spec's Design Notes anticipated.** It named 12 routes; `POST /project/v2/reconcile` is also invoked cross-app (`NamedProjectionDispatchCoordinator.cs:750`) and was missing from it. Omitting it would have reproduced the exact bug the finding is about, so the ACL and the conformance expectation carry 13.
- `DaprAccessControlConformanceTests` was rewritten into three tests: exhaustive policy-list + per-policy `defaultAction: deny` + `trustDomain` checks; exact set equality between the ACL's eventstore operations and the expected route set (both directions, plus POST-scoping and no duplicates); and a drift guard that parses the SDK's own mapped POST route literals out of `references/Hexalith.EventStore/.../EventStoreDomainServiceExtensions.cs` and fails naming any route the ACL omits (or grants beyond the mapped surface). The parser covers the three literal-carrying shapes — `MapPost("/x", …)`, `MapNamedProjectionRebuildEndpoint(app, "/x", …)`, and the shared-rebuild `const string route = "/x"` — and is itself guarded against matching nothing, so it cannot pass vacuously if the SDK file changes shape.

### Surface 2 — dead letter

- `ChatBotProjectionSubscriptionDefaults` is the new single owner of the pubsub/topic/dead-letter literals **and** their configuration keys, with a `Resolve(IConfiguration)` seam that returns the effective `ChatBotProjectionSubscriptionBinding`. Blank configured values fall back rather than reaching the subscription mappers' `ArgumentException.ThrowIfNullOrWhiteSpace` guards.
- `ChatBotDeadLetterProjectionEndpoints` maps `POST /chatbot/events/dead-letter` subscribed to `(pubSubName, deadLetterTopic)` with **no `DeadLetterTopic` of its own** (a DLQ subscription that dead-letters on failure can loop), logs one source-generated `LoggerMessage` Error, and always returns 200 to drain.
- Metadata-only logging is **structural, not merely disciplined**: the bound DTO `DeadLetteredChatBotEvent` declares only `messageId` and `correlationId`, so the rest of the envelope — including the persisted event payload — is discarded by the JSON reader before the handler runs and cannot be logged. Both identity values are additionally reduced through `AuditMetadata.SafeOptionalToken` (falling back to `unknown`), because a dead-lettered message is by definition one the projection path rejected.
- `AppHostShouldWireSubscriberDeadLetterRoutingIntoDaprDiscovery` now **discovers** the subscriber files from disk (`*ProjectionEndpoints.cs` minus the dead-letter one), pins the count at 7 so an eighth subscriber is a deliberate decision, asserts each sets `DeadLetterTopic`, asserts the drain is mapped and declares no dead-letter topic of its own, and asserts the call site consumes the shared defaults rather than re-hardcoding the literal.
- Beyond the spec's task list, three **behavioral** tests were added in `tests/Hexalith.ChatBot.Server.Tests/Projections/ChatBotDeadLetterProjectionEndpointsTests.cs` (real `WebApplicationFactory<Program>`, recording `ILoggerProvider`): drain returns 200 and logs the metadata identity; no log entry at any level or category contains the message body or the tenant id; a malformed identity still drains without the raw value reaching a sink. The spec's acceptance criterion ("a subscribed endpoint receives it and logs Error carrying topic/pubsub/message identity and no message body") is not provable from source text alone.

### Surface 3 — AppHost Keycloak seed grants

- `expectedServiceGrantCount` is gone. `CountRealmServiceGrantExpiryMappers` derives the expected placeholder count from the realm's own `chatbot-service-client-grant-expiry` protocol mappers and throws **naming the offending client id** when one carries something other than the placeholder, or when the realm declares no such mapper at all. The raw occurrence count is still compared against the derived count, so a placeholder sitting outside a grant-expiry mapper also fails.
- A stderr warning now fires when `KeycloakPersistent` is true, naming the rendered expiry and pointing at `docker rm -f`, because a reused container does not re-import the realm and the pre-expiry gate can otherwise pass against a value Keycloak never received. Warning, not failure, per the recorded decision.
- The non-production seed-credential policy is recorded as a comment directly above `PrepareKeycloakRealmImport` and in `README.md#Local-development seed-credential policy`: shared expiry is intentional for re-minted seeds, the pre-expiry gate is the guardrail, production provisioning stays outside the AppHost.
- `RecoveryValidationTopologyContractTests` gained two tests. `RenderedRealmServiceClientGrantExpiryIsAcceptedByTheClaimsGrantResolver` renders the realm through the AppHost, reads every substituted claim back out, and asserts it individually against both gates the consumer applies (`AuditMetadata.IsSafeStableIdentifier`, invariant `DateTimeOffset.TryParse`) **and** end to end through `ClaimsServiceClientGrantResolver`, asserting the resolution is not `service_client_grant_missing`. `PrepareKeycloakRealmImportNamesTheClientWhoseGrantExpiryMapperLostItsPlaceholder` blanks one client's placeholder on disk (restoring it in `finally`) and asserts AppHost startup throws naming that client.
- The `AppHostTopologyTests` source-text guard at `:97` was replaced by `AppHostShouldDeriveServiceGrantPolicyFromTheRealmAndWarnOnReusedKeycloak`, whose doc comment points at the behavioral test. It keeps only the AppHost-side policy shape the consumer test cannot see (derived count, persistent-container warning, recorded seed policy).

### Surface 4 — CI / MSBuild

- Both workflows gained a `Require one executed topology acceptance test and zero skips` step mirroring the existing `story132-topology-acceptance.trx` guard verbatim, placed immediately after the step that writes `topology-acceptance.trx`. `ScaffoldArchitectureTests` now asserts both guards by name and by their distinct TRX paths in both workflows, so neither can be deleted or pointed at the wrong file.
- The AppHost's `HexalithCommonsRoot` forwarding `ItemGroup` is conditioned on `'$(HexalithCommonsRoot)' != ''`. `AdditionalProperties` pins a **global** property that a referenced project's own `Condition="'$(HexalithCommonsRoot)' == ''"` fallback cannot override, so forwarding an empty value converted an undetected root into a hard break.

### Surface 5 — Tier-3 acceptance lane

- Resource state: every required resource is now asserted as `KnownResourceStates.Running`, not merely "State is not null". **Grounding** (per the spec's verification-depth decision, no live run in this session): Aspire's own XML documentation for `WaitForResourceHealthyAsync` states a resource is considered healthy once it reaches `KnownResourceStates.Running`, and `CustomResourceSnapshot` computes `HealthStatus` only for a Running resource (`ComputeHealthStatus`, present in `Aspire.Hosting` 13.5.3). The wait already implies Running; the assertion turns recorded evidence into a gate. Corroborated in-file: this same suite's `HasPublishedRunningHttpEndpoint` (`:1731`) and `GetExactAssignedHttpEndpoint` (`:1742`) already treat `State.Text == KnownResourceStates.Running` (with `HealthStatus is null or Healthy`) as the ready condition.
- Endpoints: `ShouldNotBeEmpty` now applies to all seven resources, not only `chatbot`.
- Ordering: `AssertNoDurableStateWasCreatedAsync` moved **below** `WaitForChatBotDaprSidecarAsync`, with a comment recording why.
- `Forbidden`, not "not OK": grounded on `ChatBotProblemDetailsFactory` (only `AuthenticationDenied` maps to 401; every other reason, `SafeNotFound` included, maps to 403) and corroborated in-file by `WaitForChatBotDaprSidecarAsync`, whose own comment calls the absent-note read "a 403 safe-not-found". Both the governed-operations read and the operation-status read take the same `ExecuteReadQueryAsync` → `Denied(..., SafeNotFound)` path.
- Wait budget: `TopologyReadinessBudget` (10 minutes) is one shared wall-clock budget consumed across the whole resource loop via `WaitForResourceHealthyWithinBudgetAsync`, which passes the REMAINING budget to each wait and fails naming the resource that ran the clock out. All three former 5-minute-per-resource sites now use it (the 7-resource recorder and both 3-resource loops). Worst case is now 10 minutes rather than 35, inside `timeout-minutes: 30`.
- Stability: `AssertGovernedOperationViewRemainsStableAsync` compares `DerivedRecordShape` plus the note identity instead of raw JSON, so a volatile field or property reorder cannot flake a required gate.
- `DerivedRecordShape(view, origin)` routes its six field reads through `RequiredDerivedField`, which throws naming the field, the origin, and the present field names (names only — never values, so the diagnostic stays metadata-only).
- `IsTransientStatusCode` is now `RequestTimeout`/`TooManyRequests` plus 5xx **except** `NotImplemented` (501) and `HttpVersionNotSupported` (505); the dead enumerated 502/503/504 branches are gone.

### Corrections to the spec's Code Map

- **`SelectedResourceValidationTimeout` is NOT unused.** The spec's Code Map records it as unused at `:93`; it is consumed at `TrivialGovernedCommandAspireE2eTests.cs:1546` as the deadline for `ValidateTopologyAttemptAsync`'s selected-port log validation. It was kept (with a doc comment distinguishing it from the new `TopologyReadinessBudget`) rather than repurposed or deleted.
- The recorded ACL route list was extended from 12 to 13 (see Surface 1).

### Verification results (2026-09-14)

Environment note: the whole-solution build is run as
`dotnet build Hexalith.ChatBot.slnx --no-restore -p:HexalithCommonsFromSource=false -m:1 -nodeReuse:false` after an
explicit `dotnet restore` with the same property. Suites are executed as compiled xUnit v3 runners
(`tests/<project>/bin/Debug/net10.0/<project>`), not through `dotnet test`.

| Check | Result |
|---|---|
| `dotnet build Hexalith.ChatBot.slnx` (see environment note) | **0 errors, 1 warning** (pre-existing `ASPIRE010` on the AppHost), once the two blockers below are neutralized |
| `Hexalith.ChatBot.Conformance.Tests` (full) | **100 / 100 pass** |
| `Hexalith.ChatBot.Conformance.Tests` ACL class only | **3 / 3 pass** |
| ACL drift guard proof — `/project/v2/reconcile` temporarily deleted from `accesscontrol.yaml` | **2 tests FAIL as designed**, the drift guard naming the route: `"...the production ACL does not grant... /project/v2/reconcile"`. ACL restored and re-run green. |
| `Hexalith.ChatBot.AppHost.Tests` (full) | **15 / 15 pass** |
| `Hexalith.ChatBot.Server.Tests` (full) | **1917 / 1917 pass** |
| `ChatBotDeadLetterProjectionEndpointsTests` (new, behavioral) | **3 / 3 pass**; observed log line: `EventId 110301, Error, Category Hexalith.ChatBot.Server.Projections.ChatBotDeadLetterProjectionEndpoints, "ChatBot projection message was dead-lettered. PubSub=chatbot-pubsub DeadLetterTopic=deadletter.chatbot.events MessageId=... CorrelationId=..."` and HTTP 200 |
| `Hexalith.ChatBot.Architecture.Tests` (full) | **103 / 105**; 2 failures are PRE-EXISTING and untouched by this work (`CorrectionPropagationWorkflowArchitectureTests.DaprWorkflowTypesStayInsideServerWorkflowRuntimeLayer` on `Adapters/Memories/IngestionBindingGetStatusActivity.cs`, and `LiveRecoveryValidationArchitectureTests.RunSettingsIsAcceptedByTheTestPlatform`, which fails on "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK"). `ScaffoldArchitectureTests` alone: **30 / 30 pass**. |
| `ChatBotDeadLetterSubscriptionTopologyTests` (new) | **3 / 3 pass** |
| `RecoveryValidationTopologyContractTests` two new cases | **2 / 2 pass** |
| SDK route parser, cross-checked independently in Python against `EventStoreDomainServiceExtensions.cs` | returns exactly the 13 expected routes |
| `python3 -c` counter guard from `ci.yml` against synthetic TRXs | `executed=0` → **fails** (AssertionError printing the counters); `executed=1 passed=1 notExecuted=0` → **passes**; `notExecuted=1` → **fails** |
| Live Tier-3 run | **Not run** (recorded decision) |

### Blockers found (pre-existing, NOT introduced here, NOT fixed here)

1. **`src/Hexalith.ChatBot.AppHost/Program.cs:149-153` does not compile against the pinned EventStore submodule.**
   `WithEventStoreClientCredentials(security, clientId:, username: "admin-user", password: "admin-pass")` passes
   `string` where `Hexalith.EventStore.Aspire.HexalithEventStoreSecurityExtensions` (at the recorded gitlink
   `7579b858`, committed 2026-09-14 09:01) now requires `IResourceBuilder<ParameterResource>`:
   `error CS1503: Argument 4: cannot convert from 'string' to
   'Aspire.Hosting.ApplicationModel.IResourceBuilder<ParameterResource>'` (and Argument 5). The same call exists
   verbatim at `HEAD`, so this is a pre-existing break introduced by today's EventStore submodule bump, not by this
   spec. It blocks `Hexalith.ChatBot.AppHost` and therefore `Hexalith.ChatBot.IntegrationTests`, i.e. the required
   `topology-acceptance` job. It was neutralized only locally and temporarily (swapping in the security resource's
   own parameter builders) to verify the integration tests here, and that local patch was reverted.
2. **`references/Hexalith.Folders` is checked out at `4c64f61` while the superproject gitlink records `b409b03`.**
   The drifted commit (also dated 2026-09-14) ships
   `src/Hexalith.Folders.Client/Generated/HexalithFoldersIdempotencyHelpers.g.cs` with
   `error CS0037: Cannot convert null to 'PathMetadataPathPolicyClass'` (x3), which blocks
   `Hexalith.ChatBot.Server` and everything downstream. Verification was performed with the submodule temporarily at
   the recorded gitlink; its original checked-out commit was restored afterwards. Nothing under `references/` was
   modified: both submodule working trees were clean before and after.

### Pre-existing red tests

`RecoveryValidationTopologyContractTests` cases that need `DistributedApplicationTestingBuilder.CreateAsync` to
SUCCEED fail before reaching their assertions with
`InvalidOperationException : ChatBot:Projects:Endpoint must identify the authorization-filtered Projects server when
the live Memories topology is enabled` — the AppHost gained a fail-closed Projects endpoint/token gate that those
tests do not configure (e.g. `PrepareKeycloakRealmImportWritesTheRenderedRealmWithOwnerOnlyPermissionsAtAnUnpredictablePath`).
This is pre-existing and untouched. The new `RenderedRealmServiceClientGrantExpiryIsAcceptedByTheClaimsGrantResolver`
deliberately supplies its own complete argument set (`RenderedRealmArgs`) so it actually runs rather than inheriting
that red.

### Not done / out of scope

- No live Tier-3 run (recorded decision). The tightened Tier-3 assertions are proven only by construction and by the grounding above; CI's `topology-acceptance` job is their first live execution.
- The four `[Review][Defer]` items and the four `deferred-work.md` entries were not touched.
- The story artifact `1-1-scaffold-the-buildable-hexalith-chatbot-module.md` was not edited: it is not in this spec's task list, and self-ticking its review findings is the practice that section's own "Rejected" note calls out.

### Reviewer verification (main session, 2026-09-14)

Judged against the diff since `baseline_commit`, not the implementation report.

- **Independently reproduced the ACL drift guard.** Ran the guard's own regex over
  `EventStoreDomainServiceExtensions.cs` outside the test: it extracts exactly 13 route literals, matching the ACL's
  granted set and `ExpectedEventStoreOperations` exactly, in both directions. The guard is real and non-vacuous.
- **`Hexalith.ChatBot.AppHost.Tests`: 15/15** on a fresh Debug build (the only test project with no ProjectReference,
  so the only one buildable while blocker 2 stands). Note for future runs: a first attempt appeared to fail, but had
  executed a 2026-09-02 **Release** binary carrying the pre-change test code — always run the freshly built
  configuration, not whatever `find` returns first.
- **Both blockers independently confirmed.** `WithEventStoreClientCredentials`'s 5-arg overload
  (`HexalithEventStoreSecurityExtensions.cs:345`) does require `IResourceBuilder<ParameterResource>` for username and
  password; `git diff` confirms ChatBot's call is unchanged since `baseline_commit`, so it is pre-existing. A clean
  solution build reproduces exactly 3 distinct errors, all `CS0037` in the drifted `Hexalith.Folders` generated
  client, and nothing from this change set. The drifted Folders checkout `4c64f61` is dated 2026-09-14 10:16 —
  NEWER than the recorded gitlink `b409b03` (2026-09-13 11:03) — i.e. active in-flight work in that submodule, not
  stale drift, and therefore deliberately left untouched.

**Verification NOT completed, and why:** the full solution build is red from blocker 2, and the AppHost is red from
blocker 1, so `Conformance.Tests`, `Architecture.Tests`, `Server.Tests` and `IntegrationTests` could not be built or
run from this session. 8 of the 10 I/O matrix rows have covering tests that exist in the diff but were not executed
here. The implementation session's reported results for those suites were obtained against a locally patched tree
(both patches reverted), so they are evidence of intent, not of the committed state. Re-run them once the two
blockers are resolved.

### Matrix test audit (main session)

| # | Matrix row | Covering test | Ran here |
|---|---|---|---|
| 1 | Mapped SDK route allowed | `EventStorePolicyMustGrantExactlyTheInvokedPostRouteSet` | no (blocked) — regex equivalent reproduced by hand |
| 2 | Other caller/verb denied | `ChatBotAccessControlMustBeDenyByDefaultAndNotCopyFoldersAllowPolicy` | no (blocked) |
| 3 | SDK adds a route → fail | `EventStorePolicyMustGrantEveryMappedSdkPostRoute` | no (blocked) — extraction reproduced by hand |
| 4 | Third `defaultAction: allow` policy → fail | exhaustive appId + per-policy deny loop | no (blocked) |
| 5 | Poison message drained, metadata-only | `ChatBotDeadLetterProjectionEndpointsTests` (3 tests) | no (blocked) |
| 6 | Rendered expiry accepted by consumer | `RenderedRealmServiceClientGrantExpiryIsAcceptedByTheClaimsGrantResolver` | no (blocked) |
| 7 | Missing mapper → throws naming client | `PrepareKeycloakRealmImportNamesTheClientWhoseGrantExpiryMapperLostItsPlaceholder` | no (blocked) |
| 8 | `KeycloakPersistent=true` → warns | **source-text only** — see gap below | yes (15/15) |
| 9 | Empty TRX → CI step fails | `ScaffoldArchitectureTests` (workflow source text) + guard expression run against synthetic TRXs | no (blocked) |
| 10 | Unauthenticated read → 403 | `AssertNoDurableStateWasCreatedAsync` (Tier-3 live only) | no (live lane) |

**Open gap — matrix row 8 has no behavioral guard.** No test anywhere references `KeycloakPersistent`; the only
coverage is `AppHostShouldDeriveServiceGrantPolicyFromTheRealmAndWarnOnReusedKeycloak` asserting the AppHost SOURCE
contains `docker rm -f` and the config-key symbol. That is the precise shape this spec's Boundaries forbid ("assert
observed values, not source text, wherever a seam exists"), and a seam does exist: the same
`DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_ChatBot_AppHost>(args)` path the neighbouring
tests use, driven with `--KeycloakPersistent=true` and a captured `Console.Error`. It was not closed here because
`IntegrationTests` cannot compile while the two blockers stand, and shipping an uncompilable test is worse than
recording the gap. Close it in the same pass that re-runs the blocked suites. Mitigating factor: the warning is
advisory, not a gate — a silent regression degrades a diagnostic rather than weakening the pre-expiry check itself.

### Review pass 1 patches applied (2026-09-14)

Eight of ten patch-routed entries were applied; two were deferred because they require NEW test code that cannot be
compiled while the build blockers stand (recorded in `deferred-work.md`).

- `RecoveryValidationTopologyContractTests.cs` — the tracked-realm restore is now synchronous and uncancellable, so a
  killed or cancelled run can no longer leave `hexalith-realm.json` mutated. (high)
- `ChatBotDeadLetterProjectionEndpoints.cs` — the body is read defensively from `HttpRequest` instead of bound as a
  required complex parameter, so an empty, non-JSON or non-CloudEvent dead-letter body still drains with 200 rather
  than failing minimal-API binding with 400 and redelivering forever. The caught exceptions are deliberately not
  logged: their messages can quote the offending payload. (high)
- `Hexalith.ChatBot.IntegrationTests.csproj` — `HexalithCommonsRoot` forwarding split into non-empty and empty
  variants, matching the AppHost fix; `UseHexalithProjectReferences` still forwards in both. (medium)
- `Story132ProductionBrowserAspireE2ETests.cs` — five independent 5-minute resource waits replaced by one shared
  8-minute budget, mirroring `TopologyReadinessBudget`; both suites are steps of the same 30-minute job. (medium)
- `TrivialGovernedCommandAspireE2eTests.cs` — the fail-closed probe now tolerates transient answers within its window
  while never tolerating OK, and asserts both reads reached a conclusive answer so the window cannot end on
  transients alone and pass vacuously. (medium)
- `TrivialGovernedCommandAspireE2eTests.cs` — the stability assertion regained breadth: `status` and `surfaceOrigin`
  are compared alongside the derived shape, plus field-set equality so a projection field added later cannot escape
  the gate by not being on the list. Still name-based, so a volatile timestamp cannot flake a required gate. (medium)
- `AppHostTopologyTests.cs` — subscriber discovery is now recursive (`SearchOption.AllDirectories`);
  `Projections/DerivedStores/` already exists. (low)
- `Program.cs` — README cross-reference repointed to the anchor that resolves. (low)

Re-verified after patching: `Hexalith.ChatBot.AppHost.Tests` 15/15, 0 warnings. The other suites remain unbuildable,
so the six patches touching `Server` and `IntegrationTests` are unverified for the same reason as the original change
set — they must be exercised in the pass that resolves the blockers.

## Spec Change Log

## Review Triage Log

### Pass 1 — 2026-09-14 (blind-hunter, edge-case-hunter, verification-gap)

| Verdict | Finding | Evidence |
|---|---|---|
| high | Realm-mutation test can corrupt a tracked file (blind, edge E4, vgap) | Confirmed `RecoveryValidationTopologyContractTests.cs:728` writes `src/.../KeycloakRealms/hexalith-realm.json`; `:739` restores with `TestContext.Current.CancellationToken`, already cancelled on a killed run, so the restore throws and the tracked realm stays mutated. No parallelism disabled in the assembly. |
| high | Malformed dead-letter body returns 400, not 200 (blind, edge E1) | Confirmed `ChatBotDeadLetterProjectionEndpoints.cs:58` binds `DeadLetteredChatBotEvent` as a required complex parameter; empty/non-JSON fails minimal-API binding before the handler. Contradicts the file's own "Always 200" comment and matrix row 5. |
| medium | `HexalithCommonsRoot` forwarding fixed at one of two sites (blind, vgap) | Confirmed `tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj:10` still forwards unconditionally — the same empty-global-property hard break the AppHost comment describes. |
| medium | Topology budget half-applied inside the same CI job (blind) | Confirmed `Story132ProductionBrowserAspireE2ETests.cs:379-380` still waits `FromMinutes(5)` per resource; both suites are steps of the same `timeout-minutes: 30` job. |
| medium | Strict 403 applied to a route the suite never warms (blind, edge E5) | `WaitForChatBotDaprSidecarAsync` warms `/api/v1/governed-operations/{id}` only; `AssertNoDurableStateWasCreatedAsync` now hard-asserts 403 on `/api/v1/operations/{taskId}` too, with no transient tolerance inside its poll loop. |
| medium | Stability assertion narrowed from whole-body to 7 fields (blind, edge E11) | `AssertGovernedOperationViewRemainsStableAsync` now compares `DerivedRecordShape` + `noteId`; a duplicate delivery mutating status, body, or any later-added field escapes the gate permanently. |
| medium | Dead-letter subscription metadata never executed or observed (vgap, blind) | Pre-verified by the gap layer: no test reads `/dapr/subscribe` or `ITopicMetadata`; deleting `.WithTopic(...)` leaves all three new tests green. |
| medium | `KeycloakPersistent` warning has no behavioral guard (blind) | Confirmed zero tests reference the key; coverage is `ShouldContain("docker rm -f")` on source text. Also `bool.TryParse` silently skips the warning for `"1"`/`"yes"`. Matches the gap recorded in the Matrix test audit above. |
| low | Subscriber discovery is prefix- and depth-coupled (blind, edge E9) | `Directory.EnumerateFiles(projectionsDirectory, "*ProjectionEndpoints.cs")` is top-level only; `Projections/DerivedStores/` exists, so a subscriber added there escapes the `DeadLetterTopic` loop. |
| low | Broken README anchor in the AppHost comment (blind) | `Program.cs:192` cites `README.md#Aspire-and-DAPR`; GitHub lower-cases anchors and the new content is `### Local-development seed-credential policy`. |
| false | Drift guard blind to an SDK verb change (blind) | Refuted: the regex matches only `MapPost`/rebuild-helper literals, so a `MapPost`→`MapPut` change removes the route from `mappedRoutes` and the `ungrantable` assertion (granted ⊄ mapped) fails. The guard catches it. |
| false | `origin: "replay"` mislabels the stability call site (blind) | Refuted: `AssertGovernedOperationViewRemainsStableAsync` runs after the idempotent replay, so "replay" is accurate at `:278`, `:2285` and `:2288`. |
| low (rejected) | Unauthenticated drain endpoint (edge E2) | All seven existing projection subscribers are equally unauthenticated; this is the established sidecar-local convention, not introduced here, and the fix adds guards. |
| low (rejected) | `ExpectedEventStoreOperations` is a third route copy (blind) | Real duplication, but the drift guard asserts set equality both ways, so divergence fails loudly rather than drifting silently; the fix restructures the test. |
| low (rejected) | No metric/counter on the drain (blind) | Alerting design choice beyond the recorded intent; fix adds public surface. |
| low (rejected) | Dead-letter log omits tenant/aggregate/event type (blind) | The spec's Always rule enumerated topic, pubsub, message and correlation id; the implementation followed it. Operability nicety, not a defect in what was asked. |
| low (rejected) | TRX guard one-liner duplicated four times (blind) | Workflow duplication is already recorded in `deferred-work.md` as cosmetic; extraction adds a script plus wiring. |
| low (rejected) | Topic == dead-letter topic collision unguarded (edge E3) | Reachable only by misconfiguration; fix adds a branch. |
| low (rejected) | `JsonDocument.Parse` / `ValueKind` unguarded on the realm (edge E7, E8) | The realm is a tracked, controlled file; a malformed one is developer error and the raw parser error names the position. |
| maybe-false | Widened endpoint assertion may fail on URL-activation timing (edge E6) | Cannot tell from static evidence whether `WaitForResourceHealthyAsync` guarantees an active URL for `security`/`chatbot-ui`/`eventstore-admin-ui`. A live Tier-3 run recording `ASPIRE_RESOURCE_EVIDENCE` settles it. Deferred. |
| medium (deferred) | Workflow assertions cannot see step order (edge E10) | Real and repo-wide pre-existing: `ShouldContain` cannot tell that the counter guard follows its own `dotnet test` step in the same job. Not caused by this story. |
| medium (deferred) | Other independent waits still sum past the job budget (edge E12) | `StartupTimeout` 3 min, `SelectedResourceValidationTimeout` 5 min and a 6-min M2 deadline remain outside the shared budget. Pre-existing; outside the recorded intent, which targeted the 7x5-minute resource loop. |
| low (deferred) | `accesscontrol.local.yaml` remains ungoverned (blind) | Pre-existing: its whole coverage is two `ShouldContain` strings. Not caused by this story. |

## Design Notes

**Why the recorded ACL route list is extended.** The 2026-09-13 decision named 12 routes. `POST /project/v2/reconcile` is also invoked cross-app (`NamedProjectionDispatchCoordinator.cs:750`) and was missed; omitting it reproduces exactly the bug this finding is about, so the ACL carries 13. `GET /` is excluded — it is a banner endpoint with no cross-app caller, and the policy is POST-scoped. `/project/rebuild/v1` and `/project/rebuild/shared/v1` have no EventStore-side caller today but are kept, per the recorded decision's "six `/project/rebuild/*` routes", because splitting the rebuild family is the brittle failure mode this finding describes.

**Why `Forbidden`, not `NotFound`.** The finding asks for `NotFound`. This spine deliberately does not reveal resource existence: `SafeNotFound` maps to `Status403Forbidden`. `Forbidden` is equally non-vacuous — it excludes the 500/503/timeout results that make `ShouldNotBe(OK)` greenest when the read path is broken — and is the value the lane's own sidecar probe already treats as live.

**Why the dead-letter subscriber has no dead-letter topic of its own.** A DLQ subscription that dead-letters on failure can loop. It drains and logs instead.

## Verification

**Commands:**
- `dotnet build Hexalith.ChatBot.slnx --no-restore -p:HexalithCommonsFromSource=false -m:1` -- expected: 0 warnings, 0 errors under warnings-as-errors.
- Build then run the xUnit v3 assemblies directly (project-level `dotnet test --filter` is unreliable under Microsoft.Testing.Platform here): `Hexalith.ChatBot.Conformance.Tests`, `Hexalith.ChatBot.AppHost.Tests`, `Hexalith.ChatBot.Architecture.Tests` -- expected: all green, and the new ACL drift guard fails when a route is removed from the ACL (prove it by temporary deletion, then restore).
- `Hexalith.ChatBot.IntegrationTests` non-live cases -- expected: green; Tier-3 live cases skip unless `HEXALITH_CHATBOT_TIER3=1`.
- `python3 -c` counter-guard expression from `ci.yml` against a locally produced `topology-acceptance.trx` -- expected: passes on a real run, fails on an `executed=0` TRX.
- No live Tier-3 run in this session (recorded decision). The live proof of the tightened assertions is CI's `topology-acceptance` job, which emits the `ASPIRE_RESOURCE_EVIDENCE` lines showing observed state text and endpoints for all 7 resources.

**Manual checks:**
- 16 pre-existing red tests are expected on a Debug/package-mode build; confirm the count does not grow.
- Confirm no file under `references/` is modified (`git status` clean for submodule gitlinks).
