---
title: 'Story 1.3: Bind Every Request to Trusted Tenant and Actor Context'
type: 'feature'
created: '2026-10-07'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: a8b421b6690ab9c0e66d7274ee5745b26a9e117c
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Commands trust token roles/Projects without current owner authority; SDK queries accept forged authorization flags. Binding differs across paths, and runtime revocation is unproven.

**Approach:** Bind immutable trusted context; authorize against current owners before protected access.

## Boundaries & Constraints

**Always:** Reject conflicting/malformed authenticated evidence. Keep actor class distinct from immutable declared origin. Require exact scope: admin needs current Tenants TenantOwner plus ChatBot grant; Projects need current Projects authority; trust-bearing identity needs Parties evidence. Machines need scoped grants, never human/admin authority. Cache age is at most five minutes; revocation recognition at most 60 seconds; sensitive mutations require current evidence. Outcomes are typed, metadata-only, existence-neutral, without owner PII.

**Never:** Grant from caller claims, query flags, origin, wildcard/global-admin labels, mirrors or operator credentials. Invent owner contracts, edit siblings, bypass gateway/SDK, or claim A13/release acceptance. Unsupported owner mappings deny in production; positives use synthetic owner evidence. Preserve Story 1.2 contracts, idempotency, dispatch fences and correlation. Durable attempts, policy and atomic audit remain later stories.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Allowed | Bound identity and current exact owner/grant evidence | Authorized operation within scope | Metadata-only decision |
| Ambiguous | Missing/conflicting subject, tenant, actor or owner evidence | No protected read or dispatch | Existing typed safe denial |
| Machine escalation | Human role/global-admin labels or forged origin | No human authority gained | Same normalized denial |
| Forged query | SDK payload claims authorization or different tenant/user | Caller flags ignored; envelope bound to trusted context | Deny before protected stores |
| Revoked/stale | Expired, future, unavailable or revoked evidence | No cached authority extension | Fail closed |
| Missing/forbidden | Equivalent valid requests | Identical public status/code/body shape | No existence or content disclosure |

</frozen-after-approval>

## Code Map

Server paths below are relative to `src/Hexalith.ChatBot.Server/`.

- `Gateway/Stages/ClaimsAuthenticationStage.cs`, `ClaimsTenantBindingStage.cs` — retain human-token `azp` exclusion and tenant-target checks.
- `Queries/ChatBotReadQueryHandler.cs` — SDK payload trusts authorization flags; status/audit/resource handlers lack actor scope.
- `Adapters/Projects/ProjectsProjectDirectory.cs` — operator directory reads cannot prove requester grants. AD-3 mappings remain A13-pending.
- Grant cache is unused in production; admin classification separately trusts human claims.

## Tasks & Acceptance

**Execution:**

- [x] `Authentication/ChatBotRequestContext.cs`, `ChatBotRequestContextResolver.cs`; both claims stages — immutable shared subject/tenant/actor binding. Ignore unauthenticated supplemental identities; retain origin separately.
- [x] `Authorization/ChatBotAuthorityCatalog.cs`, `IChatBotOwnerAuthorityProvider.cs`, `ChatBotOwnerAuthorityEvidence.cs`, `UnavailableChatBotOwnerAuthorityProvider.cs`, `ChatBotRequestAuthorizer.cs` — exhaustive command/query requirements; reject unknown/duplicate rows. Validate owner/principal/tenant/resource/operation/version/time; unsupported mappings deny.
- [x] `Gateway/Stages/ParticipantAuthorizationStage.cs`, `ServiceClientGrantValidator.cs`, `ClaimsServiceClientGrantResolver.cs`, `ServiceClientGrantProjectionCache.cs`; `Governance/Admin/AdminAuthorityEvaluator.cs` — shared decisions; scoped bounded refresh, no token-only refresh, immediate denial of known revocation. Remove machine-to-human escalation.
- [x] `Gateway/ChatBotCommandAdmissionPipeline.cs`, `ChatBotGatewayContext.cs`, `CommandGatewayServiceCollectionExtensions.cs` — mandatory authorizer/unavailable provider; safe references; preserve order/fences.
- [x] `Queries/ChatBotReadQueryHandler.cs`, `ChatBotReadAuthorization.cs`, `Gateway/ChatBotCompatibilityEndpointExtensions.cs` — authorize HTTP/SDK before handlers access stores. Payload flags/envelope IDs confer no authority; unresolved scope denies without content lookup.
- [x] `tests/Hexalith.ChatBot.Server.Tests/TrustedAuthorityTests.cs` — matrix, scoped positives, SDK forgery, mixed identities, clock bounds and zero protected effects.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/TrustedAuthorityParityTests.cs` — real actor contexts for UI/API, CLI, MCP, worker, mailbox, service and AI across two tenants; normalized parity and leakage scans of responses/telemetry/audit.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/TrustedAuthorityBoundaryTests.cs` — exhaustive operation requirements, mandatory registrations, internal seams and thin adapters.

**Acceptance Criteria:**

- Given sufficient current evidence, when a request executes, then only bound resources are accessible, without owner PII.
- Given invalid authority, when HTTP/SDK authorizes, then denial precedes protected access; forbidden/missing outcomes match.
- Given any named origin, when bound, then origin/class remain distinct and immutable; machines gain no human bypass.
- Given cached/revoked evidence, when limits expire, then access denies by five minutes/60 seconds; sensitive mutations revalidate.

## Implementation Notes

- Added a deep-copied, immutable authenticated subject/tenant/actor snapshot. Both claims stages and SDK read binding share the resolver; unauthenticated supplemental identities cannot alter it. Human OAuth `azp` remains separate from machine classification and declared origin.
- Added an internal closed catalog covering 64 commands and eight queries, exact owner requests/evidence, and a mandatory shared authorizer. Current Tenants TenantOwner and scoped ChatBot grants govern administration; every bound project and trust-bearing party reference, including nested plural scopes, requires exact owner evidence. Unsupported production mappings use the unavailable provider and deny. No owner contract, sibling, dependency version or public/generated contract was changed.
- Admission authorizes before downstream protected access, retaining established typed refusals, idempotency, dispatch fencing and correlation. HTTP and SDK reads use the same mandatory boundary; envelope tenant/user mismatches and caller authorization/global-admin flags never grant access. Compliance full detail checks current Projects authority after admitted metadata lookup and before returning detail.
- Machine grants come only from owner-observed evidence. Cache keys include the exact bound subject, tenant, resource, operation, actor class and origin; age/revocation bounds are five minutes/60 seconds, and mutations revalidate. Grant lists are frozen. Exact revocation tombstones prevent a later allowed re-fetch from reviving a revoked grant; revoked grant observations establish tombstones only for matching tenant/client/origin. Malformed grant lists, client classes and owner PII deny safely. Machines cannot acquire human administration through token roles, global-admin labels, wildcards or origin.
- Existing lower-level policy and gateway regressions now explicitly configure test-only synthetic owner personas. Positive project records are finite exact scopes; negative personas keep missing/wrong scopes. The default production provider remains unavailable. Required downstream security, audit, idempotency and fencing assertions remain, with canonical actor/scoped-role metadata and wildcard denial expectations updated for this story.

| Approved matrix row | Executed evidence |
| --- | --- |
| Allowed exact scope | `CurrentExactOwnerEvidenceAuthorizesWithoutTokenRoles`, `ExactSyntheticSdkAuthorityAccessesOnlyBoundResource`; real HTTP allowed reads in all 16 actor/origin/tenant rows perform exactly one protected read. |
| Ambiguous/malformed evidence | `ConflictingOrMalformedAuthenticatedEvidenceDenies`, `UnauthenticatedSupplementalEvidenceCannotChangeImmutableBinding`, `MissingRequiredOwnerDeniesAdministration`, `InvalidOwnerEvidenceDeniesBeforeProtectedRead`, `MalformedOwnerGrantDeniesWithoutThrowingOrCachingAuthority`; invalid evidence leaves protected read/write probes at zero. |
| Machine escalation | `MachineCannotGainHumanAuthorityThroughRolesOriginOrGlobalAdmin`, `HumanOAuthApplicationIdDoesNotClassifyAsMachine`, `ProvenanceNeverChangesActorClassOrHumanAdminEligibility`; service/AI classes retain no human administration across all declared origins. |
| Forged HTTP/SDK scope | `ForgedSdkEnvelopeNeverConfersAuthority`, `UnauthenticatedSdkFlagsDenyWithoutProtectedEffects`, `CallerProjectReadFlagCannotReplaceCurrentProjectsAuthority`, `NestedPluralOwnerReferencesRequireEveryExactCurrentGrant`; forged flags and tenant/user targets deny before protected stores. |
| Revoked/stale/unavailable | `SensitiveMutationRevalidatesWhileFreshOrdinaryReadMayUseObservedEvidence`, `KnownServiceGrantRevocationDeniesImmediatelyAndCannotBeRefreshedByToken`, `RevokedOwnerGrantObservationCreatesTombstoneBeforeLaterAllowedRefresh`, `ForeignRevokedGrantObservationCannotPoisonTheBoundTenant`, `UnsupportedProductionOwnerMappingsDeny`, and cache clock-bound regressions. |
| Missing/forbidden parity and leakage | `RealHttpActorsShareExactScopeExistenceNeutralityAndMetadataOnlyOutputs`: UI/API human, CLI/MCP/worker/mailbox/API service and AI, each across two tenants. Each row has a successful real HTTP read, identical forbidden/missing status and body, no further protected read, an actual denied command authorization audit fact, and response/audit/telemetry leakage scans. |
| Mandatory/exhaustive boundary | `CatalogCoversEveryCommandAndRegisteredQueryExactlyOnce`, `MandatoryRegistrationsFailClosedWithoutOwnerMappings`, `AuthoritySeamsRemainInternalAndAdaptersDependOnlyOnThePublicClient`. |

Implementation work is complete for the approved scope. The story remains **in-review**: independent review and all routed corrections are complete, while mandatory Debug project-reference and Aspire runtime evidence are blocked. Production owner mappings and A13/release acceptance are not claimed; durable attempts, policy and atomic audit remain outside this story.

## Spec Change Log

## Review Triage Log

All three independent layers completed. Every finding received a verdict before grouping; 12 grouped patch entries were corrected and verified. Three findings were rejected as documented below. Nothing was deferred. The frozen intent is unchanged.

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| B1 | high | patch | Resolver claim types are read through case-insensitive FindAll; the scratch probe accepted SUB/EVENTSTORE:TENANT. Architecture requires exact case-sensitive claims. Use ordinal claim-type matching and reject mismatches. |
| B2 | medium | patch | CanCorrectAssociation checks the original human claim, although the resolver binds default/user actors as human. The probe denied an owner-authorized bound human; use the immutable context in this downstream check. |
| B3 | high | patch | RequesterRef, ApproverRef and nested ResolvedPartyRef exist in tracked contracts but are omitted from Parties requests; downstream validation checks syntax rather than owner authority. The probe authorized a forbidden RequesterRef. Add the missing exact reference names. |
| B4 | high | patch | RequestFailedWorkflowRetry checks RetryId but never requests authority for its originating FailedEventId. The probe authorized a forbidden failed target; add the exact originating ChatBot resource check. |
| B5 | high | patch | Earlier owner evidence is not rechecked after subsequent awaits. The probe advanced the clock past the first evidence expiry and still received allowance; revalidate all acquired evidence before the final decision. |
| B6 | high | patch | Revocation tombstone recording incorrectly requires observation at or after request start. A one-second-old valid scoped revocation was observed but the prior cached grant remained usable in the probe. Record known revocation independently of positive-current freshness. |
| B7 | medium | patch | GetEvidenceAsync excludes all OperationCanceledException instances from normalization. The probe reproduced an owner timeout escaping with an active caller token; propagate caller cancellation and normalize owner timeout to unavailable evidence. |
| B8 | low | reject | Cache dictionaries retain keys without a global sweep, a retention limitation also present in the baseline cache. Current production uses the unavailable owner provider and cannot populate authority entries; everyday production harm is not demonstrated. Adding eviction/lifecycle policy is more than a direct correction and could weaken permanent revocation, so reject this low finding. |
| B9 | false | reject | The claim that adapter behavior remains untested is disproved by existing SurfaceArms invoking ChatBotCliCommands.InvokeAsync and ChatBotMcpService through IChatBotClient, Story11RecordGovernedNoteParityTests, and dedicated CLI/MCP/worker/client suites. This new matrix tests bound actor classes and declared origins at the shared HTTP boundary; no adapter behavior changed. |
| B10 | medium | patch | The new fixture stores only the allowed note, so both forbidden and missing IDs are absent and the foreign tenant is not simultaneously populated. Seed existing forbidden and foreign records in the protected fixture to make this specific parity/isolation check non-vacuous. |
| B11 | medium | patch | The new leakage scan never injects its owner PII sentinel and examines message-count telemetry rather than captured logs/traces. Introduce restricted owner/error/record sentinels and inspect captured response, audit, log and trace output. |
| E1 | high | patch | ExecuteApprovedAIAction and ProposeAIAction expose RecipientReferences, omitted by the Parties reference collector. Downstream project checks do not authorize those recipients. Add the exact contract field; grouped with B3. |
| E2 | false | reject | No production caller reuses an authority principal for direct stage admission. ChatBotCommandAdmissionPipeline always resolves/checks the bound context and invokes the shared authorizer before ParticipantAuthorizationStage, replacing the principal on every request. The alleged reuse requires a caller absent from the application; lower-level fixtures intentionally isolate downstream policy using synthetic owner principals. |
| E3 | high | patch | Confirmed B5: owner responses are checked individually and the completed evidence set is not revalidated after later awaits. Grouped with B5. |
| E4 | medium | patch | Confirmed B7: an owner-side OperationCanceledException escapes while request cancellation is not signaled. Grouped with B7. |
| E5 | high | patch | Grant scopes accept @ through IsSafeStableIdentifier, and AuditEnvelopeFactory emits those scopes as grant-scope references. An email-shaped owner scope can reach accepted audit metadata; reject email-shaped scopes consistently with other evidence identifiers. |
| V1 | medium | patch | Pre-verified regression gap: the compliance-detail endpoint test covers only one project. Its current all-project loop is correct, but reverting to first/Any authority would pass. Add a two-project partial-authority endpoint case with restricted metadata. |
| V2 | high | patch | The /process SDK command admission builds an authenticated principal from envelope UserId/TenantId; Program routes through the SDK and no transport comparison precedes that stage. Synthetic valid owner evidence therefore permits forged bound identity. Use the existing transport-context resolver and reject mismatched or unauthenticated envelopes; preserve the valid admission-marker short circuit. |

## Verification

Post-review checks were executed by the root workflow against the corrected tree. The unchanged Release package configuration provides passing local implementation evidence; these results are not a lifecycle/TE-2 attestation. Production owner mappings and A13/release acceptance remain unclaimed.

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Release -m:1 /nr:false` — passed, zero warnings/errors.
- `dotnet build tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj --configuration Release -m:1 /nr:false` — passed, zero warnings/errors.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --configuration Release -m:1 /nr:false` — passed, zero warnings/errors.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Release/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story-1-3-reviewed-server.ctrf.json -result-trx TestResults/story-1-3-reviewed-server.trx > TestResults/story-1-3-reviewed-server.run.log 2>&1` — **2,270/2,270 passed**, zero failures/skips; includes 68 trusted-authority cases, real SDK transport binding, existing admission/audit/fencing regressions, and two-project compliance detail. CTRF and canonical TRX counters were independently reconciled.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Release/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-3-reviewed-conformance.ctrf.json -result-trx TestResults/story-1-3-reviewed-conformance.trx > TestResults/story-1-3-reviewed-conformance.run.log 2>&1` — **149/149 passed**, zero failures/skips; includes all 32 actor/origin/tenant cases, populated forbidden/foreign records, injected owner/error/record sentinels, and nonempty actual log/trace captures. CTRF and canonical TRX counters were independently reconciled.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-3-reviewed-architecture.ctrf.json -result-trx TestResults/story-1-3-reviewed-architecture.trx > TestResults/story-1-3-reviewed-architecture.run.log 2>&1` — **122/122 passed**, zero failures/skips; includes exhaustive operation catalog, mandatory fail-closed registrations, internal authority seams, and thin adapters. CTRF and canonical TRX counters were independently reconciled.
- `git -c core.whitespace=cr-at-eol diff --check` — passed. Tracked line endings are preserved; new files follow `.editorconfig`.

Review corrections retain the original denial audit assertion and protected admission-marker short circuit. Ordinal claim-type matching, normalized human corrections, missing party/originating retry references, final evidence-set freshness (including machine grants), recent revocation tombstones, owner timeout/caller cancellation, and email-shaped grant-scope denial all have passing targeted cases in the full Server lane. Optional `ResolvedPartyRef=null` remains absent identity evidence. The compliance endpoint requires every referenced project's current authority before exposing metadata; partial authority returns `escalation-required`, `restricted-detail`, `request-access`, and no visible references.

Mandatory evidence blockers:

- Pre-edit and post-review `aspire run --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` built AppHost, then exited **134** because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` was not configured. `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` reported **no running AppHost** after both attempts. No credential was invented; no live runtime acceptance is claimed.
- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1 /nr:false` — failed with **1,586 errors / two warnings** in existing sibling/project-reference dependency wiring: Projects missing EventStore/Conversations/FrontComposer types, Conversations converter base/override failures, and duplicate `Hexalith.Commons.UniqueIds` package/project imports (`CS1704`). No sibling or dependency edit was made.
- `dotnet build src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj --configuration Debug -m:1 /nr:false --no-restore` — blocked by duplicate Memories package/project assets (`CS1704`). Without `--no-restore`, duplicate EventStore.Contracts package/project imports fail (`CS1704`, `MSB3243`). Explicit source-reference diagnosis also exposed the existing `Adapters/Folders/FoldersFolderStore.cs:61` generated API mismatch (`FileMutationRequest` versus `AddFileRequest`, `CS1503`); that unrelated source was left untouched.

The repository's `docs/story-evidence-integrity.md` requires unavailable mandatory evidence to leave a story in review/in-progress. The spec remains `in-review` and its sprint entry is `review`; the build workflow's automatic `done` transition was therefore withheld. No completion contract, provenance, passing TE-2 result, or release acceptance is asserted.
