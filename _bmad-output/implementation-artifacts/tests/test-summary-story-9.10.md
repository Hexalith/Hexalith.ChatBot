# Test Automation Summary — Story 9.10 (Consent and Lawful-Basis Metadata)

**Date:** 2026-06-03
**Engineer:** QA automation (BMAD `qa-generate-e2e-tests`)
**Framework:** xUnit + Shouldly (.NET 10) — the project's existing test stack. No new framework introduced.
**Feature under test:** Story 9.10 governed consent/lawful-basis decision/recording/gating layer
(`SubmitConsentLawfulBasisRecord` → `ParticipantAuthorizationStage` compliance gate → CommandGateway audit spine →
`AuditEnvelopeFactory` evidence; the pure `ConsentRequirementPolicy`/`ConsentGate`/`ConsentLawfulBasisRedactionPolicy`
decisions; the server `ConsentLawfulBasisAuthorizationPolicy` + `ConsentGateEvaluator` seams).

> Scope note: this is a .NET contracts/server/conformance solution with no browser UI surface (the live S-tagged consent
> surface is an explicit Story 9.10 deferral). "E2E" here = API/behavioral coverage of the governed command spine + the
> pure decision engine + the per-project no-leak read redaction, exercised through the same harnesses Stories
> 9.7/9.8/9.9 use.

## Coverage assessment (against the 4 ACs)

The feature shipped with a substantial, well-structured suite already (`DeletionErasure`/`TenantExport`-mirrored). The QA
pass diffed existing coverage against every AC, decision branch, schema invariant, and closed-set member — and against
the sibling stories' own test templates (`TenantExportAuthorizationPolicyTests`, `DeletionErasureRunnerTests`, the
per-class `SchemaVersions.IsKnown` convention) — to surface untested paths.

| AC | Theme | Pre-existing coverage | Gap found |
|----|-------|----------------------|-----------|
| AC1 | record model + closed token sets + requirement matrix + schema | five closed-set membership tests, `Published` bijection, `ValidateRecord` accept + 5 reject cases, `ValidateRequirementProfile` incomplete | **`SchemaVersions.IsKnown` untested** (every sibling schema-version class tests it); **`ValidateRecord` null / `RecordId` / `SubjectLocator` / `ProjectScopeRef` / `BasisSource` / non-UTC branches untested**; **`ValidateRequirementProfile` null/empty/invalid-key/invalid-value branches untested** |
| AC2 | per-project no-leak read redaction + authority projection | `ConsentLawfulBasisRedactionPolicy.Redact` authorized/unauthorized/no-scope (with a hand-built view); gateway role/actor-type denial | **`ConsentLawfulBasisAuthorizationPolicy.AuthorityFor` untested** — the only `ClaimsPrincipal`→view projection + `IsSafeStableIdentifier` grant filter (sibling `TenantExportAuthorizationPolicyTests` exists; consent had none) |
| AC3 | NFR50 audit evidence + opaque-locator no-leak | gateway audit-down ⇒ 503, committed-envelope evidence refs, subject-locator never emitted; cross-tenant leakage scan | none |
| AC4 | fail-closed requirement + gate decision | pure `ConsentRequirementPolicy.Evaluate` + `ConsentGate.Evaluate` (known/unknown/missing/relaxed, all status combinations) | **`ConsentGateEvaluator.EvaluateForGovernedAction` (server seam composing policy+gate) untested**; **`ConsentRequirementProfileMapper.ProfileFor` deferred seam untested** (sibling `DeletionErasureRunnerTests` exists; consent had none) |

## Gaps auto-applied (3 new test artifacts; +14 tests)

**New — `tests/Hexalith.ChatBot.Server.Tests/Audit/ConsentLawfulBasisAuthorizationPolicyTests.cs`** (AC2, mirrors
`TenantExportAuthorizationPolicyTests`):
- [x] `AuthorityForShouldProjectComplianceScopeAndSafeProjectGrants` — the `ClaimsPrincipal` → bounded
      `ConsentLawfulBasisAuthorityView` projection surfaces the compliance-scope flag + the actual per-project owner grants.
- [x] `AuthorityForShouldDropUnsafeProjectClaimValues` — `IsSafeStableIdentifier` filters an unsafe project-claim value
      out of the bounded view (no malformed ref leaks into the authorized set).
- [x] `NonComplianceAndNonHumanActorsShouldNotBeAbleToRecordConsent` — `CanRecordConsentLawfulBasis` + `AuthorityFor`
      fail closed for non-compliance roles, service clients, and AI actors (human-compliance-only).

**New — `tests/Hexalith.ChatBot.Server.Tests/Audit/ConsentGateEvaluatorTests.cs`** (AC4, mirrors
`DeletionErasureRunnerTests` as the server-seam pin):
- [x] `EvaluateForGovernedActionShouldFailClosedAgainstThePublishedProfile` (5 cases) — the seam composing
      `ConsentRequirementPolicy.Evaluate`+`ConsentGate.Evaluate`: a required kind is satisfied only by an `active` basis;
      `null`/`withdrawn`/`expired` ⇒ `blocked-missing-basis`; an **unknown subject kind** biases to required ⇒ blocked.
- [x] `EvaluateForGovernedActionShouldSatisfyANotRequiredKindWithoutABasis` — a tenant-relaxed `not-required` kind
      satisfies the gate without any active basis.
- [x] `ProfileMapperShouldReturnThePublishedRegulatoryDefault` (2 cases) — the **deferred**
      `ConsentRequirementProfileMapper.ProfileFor` returns `ConsentRequirementMatrix.Published` unchanged for any (or
      null) snapshot id, pinning the M2-deferral contract.

**Extended — `tests/Hexalith.ChatBot.Contracts.Tests/ConsentLawfulBasisContractTests.cs`** (AC1):
- [x] `SchemaVersionsIsKnownShouldRecognizeOnlyTheShippedVersion` — `IsKnown(V1)`/unknown/null (the per-story convention).
- [x] `ValidateRecordShouldRejectNullUnsafeIdentifiersAndNonUtcTimestamps` — null record ⇒ `consent_record_invalid`;
      unsafe `RecordId`/`SubjectLocator`/`ProjectScopeRef`/`BasisSource` tokens; non-UTC `RecordedAtUtc`.
- [x] `ValidateRequirementProfileShouldRejectNullEmptyAndMalformedEntries` — null/empty ⇒
      `consent_requirement_profile_invalid`; out-of-set key ⇒ `consent_requirement_subject_kind_invalid`; out-of-set
      disposition value ⇒ `consent_requirement_disposition_invalid`.

## Test execution

| Suite | Before | After | Result |
|-------|--------|-------|--------|
| `Hexalith.ChatBot.Contracts.Tests` | 451 | **454** | ✅ Passed (0 failed, 0 skipped) |
| `Hexalith.ChatBot.Server.Tests` | 1365 | **1376** | ✅ Passed (0 failed, 0 skipped) |
| `Hexalith.ChatBot.Conformance.Tests` | 81 | 81 | ✅ Passed (existing consent leakage scan; no new test needed) |
| `Hexalith.ChatBot.Architecture.Tests` | 39 | 39 | ✅ Passed (boundary fitness + scaffold legacy-literal check; **no new allowlist entry**) |

Consent-focused slice: `--filter FullyQualifiedName~ConsentLawfulBasisAuthorizationPolicyTests|ConsentGateEvaluatorTests`
→ **11 passed**. Consent contract slice → **35 passed** (was 32).

New baseline across the four suites: **1950** (was 1936) — all green.

## Next steps

- Run all four suites in CI to confirm the full baseline stays green.
- When the deferred surfaces land, add:
  - server-side tests driving the live `ConsentGate` wiring into the `ProposeAIAction` / retention **execution** call
    sites (currently only the `ConsentGateEvaluator.EvaluateForGovernedAction` decision seam is exercised);
  - a `ConsentRequirementProfileMapper` test asserting a real tenant-policy-snapshot → `ConsentRequirementProfile`
    override merge once the mapper stops returning `Published` unchanged;
  - browser E2E (Playwright) for the S-tagged consent-metadata admin surface and the per-project redacted read view.
