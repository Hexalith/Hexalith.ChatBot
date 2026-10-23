---
title: Repair stale CI authority fixtures
type: bugfix
created: 2026-10-23
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: 0fe0c477e49e90d3edd2fb80229beac5132feda9
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/README.md'
---

<frozen-after-approval reason="user authorized CI repairs and bypassed human validation">

## Intent

Repair the ordinary integration-test CI failures caused by obsolete claims-only authority and overly broad source assertions, while preserving the current production authorization and atomic-execution refusal boundaries. The user authorized fixing CI/CD failures without additional human validation. Publishing and submodule synchronization are handled separately by the parent session.

## Boundaries & Constraints

Always use the existing trusted-authority test fixtures and real authorizer path to make the idempotency harness explicitly synthetic. Retain runtime rendering coverage for canonical UTC future service-grant expiry. Demonstrate that claims alone cannot provide authoritative grants. Keep generated mailbox secrets out of tracked files and logs.

Never weaken production authorization, allow atomic execution without proof, accept a failed test as success, skip failing ordinary tests, initialize nested submodules, or alter release acceptance policy. Do not edit production code. Work on main and preserve other session changes. Do not push or commit; the parent will review, validate commit messages and publish.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Equivalent repeat | Explicit synthetic owner authority and admission proof | First and repeated command accepted; one persisted coarse record and one dispatch | Assert gateway result before reading the single record |
| Claims only | Rendered future expiry with no authoritative grant dependencies | Resolver denies with service_client_grant_missing | Preserve fail-closed behavior |
| Realm rendering | AppHost renders service-client expiry and configured mailbox secret | Expiries parse as future canonical UTC values; mailbox secret is exactly the configured value | Missing/invalid secrets still refuse startup |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs`: old gateway constructor defaults now deny before reserving a coarse idempotency record.
- `tests/TrustedAuthority/SyntheticOwnerAuthorityProvider.cs` and `SyntheticLegacyAdmissionEvidenceValidator.cs`: existing shared synthetic evidence seams already linked into RecoverySandbox and exposed to IntegrationTests through its friend assembly; reuse through the existing project reference to avoid duplicate visible types.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs`: rendered expiry test wrongly calls a resolver without authorizer/context; mailbox source scan wrongly bans independent workload-secret generation. Preserve actual expiry, configured-secret rendering, and missing/invalid-secret behavioral tests.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsServiceClientGrantResolver.cs`: absent authorizer/request context must deny. Claims are not current owner authority.
- `src/Hexalith.ChatBot.Server/Authorization/ChatBotRequestAuthorizer.cs`: use the real authorizer with the explicit test-only provider for synthetic integration fixtures.
- `.editorconfig`, `tests/Directory.Build.props`, `Directory.Packages.props`: repository coding and build configuration. Shared package catalog was just synchronized upstream.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj`: preserve the existing RecoverySandbox project reference and reuse its linked friend-visible trusted-authority fixtures, without duplicate Compile links.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs`: wire explicit synthetic authority/admission dependencies and improve first failure assertions while preserving persistence checks.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs`: repair stale claims-only expectation, retain expiry serialization coverage, and replace whole-AppHost RNG prohibition with mailbox-specific behavior.

**Acceptance Criteria:**
- Given the repaired fixture, when equivalent commands repeat, then the coarse state and dispatch invariants pass.
- Given no authoritative grant provider, when a token contains a rendered future expiry, then it still cannot authorize the request.
- Given valid configured mailbox material, when the realm renders, then it contains precisely that configured secret and future canonical UTC grant expiries.
- Given the narrow changes, when focused integration classes and then the ordinary integration lane execute, then no test fails.

## Implementation Notes

The parent inspected Git state and guidance. Baseline before implementation is recorded above. Source submodules are clean and synchronized. A separate agent is currently probing the UI: coordinate with parent before building any shared Client/UI/AppHost outputs. Ask the parent for build readiness; make edits first. Prefer Debug local verification; the parent will run Release package-mode CI validation. The exact ChatBot AppHost is not running; do not touch the unrelated Builds AppHost. Complete the baseline Aspire start check only when no shared build is active, following the applicable Aspire skill; report missing configuration precisely instead of weakening it.

## Verification

- Build the integration project serially with the existing local development configuration.
- Invoke the built xUnit runner with `-class` for IdempotencyStateStoreIntegrationTests and RecoveryValidationTopologyContractTests, requiring real execution and no failures.
- Run the ordinary integration lane independently; preserve legitimate opt-in live-lane skips.
- `git diff --check`.

- Build discovery: direct fixture Compile links produced CS0436 because RecoverySandbox already exposes the same internal types to IntegrationTests. Reuse the existing linked definitions via the project reference; do not duplicate or rename the shared fixtures.

## Verification Results

- Debug/source integration build: 0 warnings, 0 errors.
- Focused repaired integration classes: 31 passed, 0 failed, 0 skipped.
- Ordinary integration lane: 433 passed, 0 failed, 7 opt-in live/replay skips; 440 total.
- AppHost configuration lane after OIDC correction: 19 passed, 0 failed, 0 skipped.
- Startup baseline: successful source build, then exact AppHost exit 2 because ChatBot:LiveRecoveryValidation:MailboxClientSecret is unconfigured; no resources started.
- Git diff whitespace check passed.

## Review Triage Log

All three review layers ran. Edge-case review returned no findings; verification review found no gaps. Blind review produced seven findings:

- low / patch: reject noncanonical core versions (leading zeros or fourth component) before packing, avoiding NuGet-normalized filenames that would fail the new exact-artifact check.
- low / patch: count symbol packages as well as packages; the publisher consumes both extensions, so the final inventory must contain exactly three of each.
- false / reject: a stubbed packer regression harness is unnecessary here; actual Release packing and an independent consumer restore/build verified project selection, prerelease assembly metadata, exact artifacts, and dependency closure. The verification review found no missing coverage.
- false / reject: the focused profile contract guards the reproduced standard-profile failure. The retained real browser acceptance path also exercises the actual requested scopes and fails when Keycloak rejects an unassigned future scope; a static future-scope change cannot silently pass all checks.
- low / patch: assert username mapper protocol and implementation as well as emitted-claim configuration; the wrong implementation could otherwise satisfy the focused configuration assertion.
- false / reject: the new claims-only case intentionally omits authoritative dependencies and verifies its matrix's denial. Existing TrustedAuthorityReviewTests.SyntheticMachineAdmissionAndAuditUseOwnerGrantInsteadOfCallerLabels exercises a real authorizer with owner evidence and forged token labels; production authority behavior was not altered.
- false / reject: package validation results were pending review finalization, not missing execution. They are recorded in the companion package repair spec below.

Review patches were verified with the AppHost lane (19 passed, no failures/skips), unsupported-version command checks, Bash syntax, whitespace checks, and actual prerelease package production. No findings deferred.

Completion posture: implementation and local verification passed; repository completion-evidence enrollment is not claimed. The plan remains in review under docs/story-evidence-integrity.md until its required evidence is available.
