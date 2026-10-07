---
title: 'Story 1.3: Bind Every Request to Trusted Tenant and Actor Context'
type: 'feature'
created: '2026-10-07'
status: 'ready-for-dev'
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

- [ ] `Authentication/ChatBotRequestContext.cs`, `ChatBotRequestContextResolver.cs`; both claims stages — immutable shared subject/tenant/actor binding. Ignore unauthenticated supplemental identities; retain origin separately.
- [ ] `Authorization/ChatBotAuthorityCatalog.cs`, `IChatBotOwnerAuthorityProvider.cs`, `ChatBotOwnerAuthorityEvidence.cs`, `UnavailableChatBotOwnerAuthorityProvider.cs`, `ChatBotRequestAuthorizer.cs` — exhaustive command/query requirements; reject unknown/duplicate rows. Validate owner/principal/tenant/resource/operation/version/time; unsupported mappings deny.
- [ ] `Gateway/Stages/ParticipantAuthorizationStage.cs`, `ServiceClientGrantValidator.cs`, `ClaimsServiceClientGrantResolver.cs`, `ServiceClientGrantProjectionCache.cs`; `Governance/Admin/AdminAuthorityEvaluator.cs` — shared decisions; scoped bounded refresh, no token-only refresh, immediate denial of known revocation. Remove machine-to-human escalation.
- [ ] `Gateway/ChatBotCommandAdmissionPipeline.cs`, `ChatBotGatewayContext.cs`, `CommandGatewayServiceCollectionExtensions.cs` — mandatory authorizer/unavailable provider; safe references; preserve order/fences.
- [ ] `Queries/ChatBotReadQueryHandler.cs`, `ChatBotReadAuthorization.cs`, `Gateway/ChatBotCompatibilityEndpointExtensions.cs` — authorize HTTP/SDK before handlers access stores. Payload flags/envelope IDs confer no authority; unresolved scope denies without content lookup.
- [ ] `tests/Hexalith.ChatBot.Server.Tests/TrustedAuthorityTests.cs` — matrix, scoped positives, SDK forgery, mixed identities, clock bounds and zero protected effects.
- [ ] `tests/Hexalith.ChatBot.Conformance.Tests/TrustedAuthorityParityTests.cs` — real actor contexts for UI/API, CLI, MCP, worker, mailbox, service and AI across two tenants; normalized parity and leakage scans of responses/telemetry/audit.
- [ ] `tests/Hexalith.ChatBot.Architecture.Tests/TrustedAuthorityBoundaryTests.cs` — exhaustive operation requirements, mandatory registrations, internal seams and thin adapters.

**Acceptance Criteria:**

- Given sufficient current evidence, when a request executes, then only bound resources are accessible, without owner PII.
- Given invalid authority, when HTTP/SDK authorizes, then denial precedes protected access; forbidden/missing outcomes match.
- Given any named origin, when bound, then origin/class remain distinct and immutable; machines gain no human bypass.
- Given cached/revoked evidence, when limits expire, then access denies by five minutes/60 seconds; sensitive mutations revalidate.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1 /nr:false` — existing project-reference build passes.
- Run Server, Conformance and Architecture projects individually through `.github/scripts/run-xunit-v4.sh`, retaining CTRF/TRX and existing regressions.
- Inspect Aspire before code edits; record exact blockers/fallbacks. Follow `docs/story-evidence-integrity.md`; unavailable mandatory evidence leaves review open.
