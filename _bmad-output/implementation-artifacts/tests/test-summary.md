# Test Automation Summary — Story 9.8 (Tenant export workflow)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only) · Jerome

**Framework detected:** .NET 10 / xUnit v3 + Shouldly + NetArchTest — the project's existing stack (no JS/Playwright present, none introduced). Story 9.8 ships a governed-decision/recording layer; the live S-tagged export UI and the byte-producing extraction runtime are explicit deferrals, so there is **no UI surface** — "E2E" coverage is API/behavioral/conformance level (the pure planner, schema invariants, fail-closed gateway gating, audit evidence, per-project no-leak authority).

**Mode:** Gap-fill against an already-implemented feature in `review` status. Audited every acceptance path against the existing tests, found genuine coverage gaps, and auto-applied tests for each. **No source code was changed — tests only.**

## Coverage map (existing tests, pre-run)

| AC | Existing coverage |
|----|-------------------|
| AC1 — data-class/redaction/correlation-aware plan | Closed-set membership (5 token sets); planner vs seed catalog (WORM excluded, redacted-export → redacted, included/none path); request-spec validation accept/reject |
| AC2 — per-project no-leak authority | Planner unauthorized-project → excluded/unauthorized no-leak; no-compliance-scope → exclude all; gateway fail-closed gating (deny non-human / non-compliance / invalid payloads) |
| AC3 — per-class success/failure + no-partial-exposure | `TenantExportFailureClassifier` retryable-vs-terminal; schema rejects eligibility/WORM/manifest/completeness violations |
| AC4 — audit evidence | `CommandGatewayTests` audit-down fail-closed + metadata-only evidence refs + no-leak; cross-tenant leakage scan |

## Gaps discovered and filled

All were genuine **untested branches/seams in shipped 9.8 code**. Tests only; no source changed.

### A. AC3 — the `partial-failure` / `failed` run-status model was 0% covered
The planner only ever emits `succeeded`/`completed`, so `RunStatusFor`/`ExpectedRunStatus` for `partial-failure` and `failed`, and the `export_run_status_inconsistent` validation error, had no test — yet AC3 is *per-class success/failure*.
- **Added** `ValidateRunResultShouldAcceptPartialFailureWithManifestOverSucceededClassesOnly` — one includable class fails (retryable) ⇒ `partial-failure`, failed class carries no artifact, manifest re-seals only the still-succeeded includable classes; the same shape mislabeled `completed` ⇒ `export_run_status_inconsistent`.
- **Added** `ValidateRunResultShouldAcceptFullyFailedRunWithEmptyManifestCoverage` — all includable classes fail ⇒ `failed`, no artifacts, manifest seals the empty set.

### B. AC1/AC3 — `ValidateRunResult` closed-set + structural rejections were unasserted
The existing reject test covered only eligibility-mismatch / WORM / manifest-partial / unprocessed. The top-level guard and per-class closed-set branches had no test.
- **Added** `ValidateRunResultShouldRejectClosedSetAndStructuralViolations` — `tenant_export_result_invalid` (null / bad run status / bad manifest fingerprint / unsafe run id / non-UTC), `export_disposition_invalid`, `export_redaction_decision_invalid`, `export_status_invalid`, `export_eligibility_invalid`, `export_exclusion_reason_invalid` (both branches), and result-level `export_class_duplicate`.

### C. AC2 — the all-or-nothing project-authority gate was only half covered
Existing tests covered empty-authority and no-compliance-scope (everything excluded), but never the **authorized** project-scope happy path nor the partial-authority gate (`.All(authorized)`).
- **Added** `PlanWithAuthorizedProjectScopeShouldProduceIncludableDispositions` — an authorized project scope lets exportable/redacted-export classes through (no `unauthorized`); WORM classes stay excluded.
- **Added** `PlanWithAnyUnauthorizedProjectInScopeShouldExcludeTheWholeRunWithoutLeaking` — a single unauthorized project ref in scope excludes the whole run and the hidden ref never reaches the serialized result (NFR2).

### D. AC2 — the `ClaimsPrincipal → TenantExportAuthorityView` projection was only exercised indirectly
`TenantExportAuthorizationPolicy.AuthorityFor` / `CanRequestTenantExport` (the **only** place a principal touches the export decision) had no direct test — the gateway covered gating but not the grant projection / unsafe-claim filtering.
- **Added** `tests/.../Audit/TenantExportAuthorizationPolicyTests.cs` (new): `AuthorityForShouldProjectComplianceScopeAndSafeProjectGrants`, `AuthorityForShouldDropUnsafeProjectClaimValues` (filters `IsSafeStableIdentifier` failures), `NonComplianceAndNonHumanActorsShouldNotBeAbleToRequestExport` (human-only, fail-closed).

## Generated tests

| Layer | File | New tests |
|---|---|---|
| Planner + schema invariants (AC1/AC2/AC3) | `Contracts.Tests/TenantExportContractTests.cs` (extended) | +5 methods (23 → 28) |
| Per-project no-leak authority projection (AC2) | `Server.Tests/Audit/TenantExportAuthorizationPolicyTests.cs` (new) | +3 methods |

## Coverage

| Acceptance criterion | Status |
|---|---|
| AC1 — eligibility→disposition, redaction, correlation, WORM | Covered (closed-set + planner matrix + every schema rejection branch) |
| AC2 — per-project no-leak authority | Covered (planner authorized/partial-authority/no-scope + **direct policy projection & unsafe-claim filtering** + gateway gating) |
| AC3 — per-class success/failure + no-partial-exposure manifest + stable run id | Covered (**partial-failure & fully-failed run-status model now exercised** + retry taxonomy + manifest invariant) |
| AC4 — audit evidence, metadata-only | Covered by dev story (`CommandGatewayTests`); re-verified green |
| Cross-cutting — no-leak / cross-tenant / boundary | Covered by dev story (`TenantExportLeakageScanTests`, fitness suite); re-verified green |

- `TenantExportSchema.ValidateRunResult` distinct error codes asserted: now includes the closed-set + structural + run-status-consistency branches (was eligibility/WORM/manifest/completeness only).
- `partial-failure` / `failed` run-status paths: **covered** (was 0%).
- `TenantExportAuthorizationPolicy` projection: **covered directly** (was indirect only).

## Test run results (after changes)

| Suite | Filter | Result |
|---|---|---|
| `Hexalith.ChatBot.Contracts.Tests` | `~TenantExport` | **28 passed** (was 23), 0 failed |
| `Hexalith.ChatBot.Contracts.Tests` | full | **389 passed** (was 384), 0 failed |
| `Hexalith.ChatBot.Server.Tests` | `~TenantExport` | **11 passed** (was 8), 0 failed |
| `Hexalith.ChatBot.Server.Tests` | full | **1352 passed** (was 1349), 0 failed |

All run suites: **Passed — 0 failed, 0 skipped.** No regressions to the 7.4 / 9.3 / 9.7 compliance, gateway, audit, or architecture suites.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API/behavioral tests generated · [x] E2E n/a (no UI surface — live export surface is a documented deferral); planner + gateway-integration + policy + conformance cover the behavioral path
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly) · [x] Happy path · [x] Critical/negative cases (every closed-set rejection, malformed header, partial/total failure, all-or-nothing authority)
- [x] All generated tests run successfully (28/389 + 11/1352, 0 failed) · [x] Semantic assertions, no hardcoded waits/sleeps (deterministic seed constants, fixed UTC; manifests re-sealed by construction, never forged) · [x] Clear descriptions · [x] Tests independent (pure functions over the immutable seed catalog; claim-built principals, no shared state)
- [x] Summary created · [x] Tests saved to appropriate directories · [x] Coverage metrics included

## Next steps

- Run the new tests in CI alongside the existing Epic 9 suites (no new test project — Contracts tests extend the existing csproj; the policy test lands in `Hexalith.ChatBot.Server.Tests`).
- When the deferred storage-layer extraction runtime lands, add live per-class failure-injection E2E driving real `partial-failure`/`failed` runs through `TenantExportFailureClassifier`.
- When the deferred S-tagged tenant-export UI ships, add E2E for the surface → `SubmitTenantExportRequest` round-trip.
