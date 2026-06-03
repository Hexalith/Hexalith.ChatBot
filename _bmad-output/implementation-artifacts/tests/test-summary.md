# Test Automation Summary — Story 9.7 (Data-class inventory and retention policy)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only) · Jerome

**Framework detected:** .NET 10 / xUnit v3 + Shouldly + NetArchTest — the project's existing stack (no JS/Playwright present, none introduced). Story 9.7 is a contracts + gateway-gating + audit-evidence feature; the live Data Governance editor UI is an explicit deferral, so there is **no UI surface** — "E2E" coverage is API/behavioral/conformance level (validation, fail-closed gateway gating, audit evidence, cross-tenant no-leak).

**Mode:** Gap-fill against an already-implemented feature in `review` status. Audited every acceptance path against the existing tests, found genuine coverage gaps, and auto-applied tests for each. **No source code was changed — tests only.**

## Coverage map (existing tests, pre-run)

| AC | Existing coverage |
|----|-------------------|
| AC1 — full classification tuple, closed dimension sets | Closed-set membership for the 3 new dimensions; schema accepts seed, rejects missing / duplicate / unknown-redaction / audit-hard-delete |
| AC2 — fail-closed compliance-admin gating + audit | `DataClassInventoryAuthorizationTests` (allow human compliance-admin; deny mailbox/policy/operations/service/AI; reject invalid/stale payloads); `CommandGatewayTests` fail-closed-on-audit-down |
| AC3 — change records actor/old-new/timestamp/policy snapshot | `CommandGatewayTests` per-class audit evidence refs across both pre/post envelopes |
| AC4 — versioned artifact + completeness | Seed-catalog bijection over the canonical class set; audit-records WORM constraint |

## Gaps discovered and filled

All four were genuine **untested branches in `DataClassInventorySchema`** — added to `tests/Hexalith.ChatBot.Contracts.Tests/DataClassInventoryContractTests.cs`. **Tests only; no source changed.**

### A. AC1 — five per-dimension rejection error codes were unasserted
The existing rejection test covered only `redaction_sensitivity_invalid` + completeness + audit-hard-delete. The validator's other branches — `owner_role_invalid`, `retention_class_invalid`, `deletion_behavior_invalid`, `export_eligibility_invalid`, `minimization_rule_invalid` — had no test, so a regression in any one (e.g. a dropped closed-set check) would slip through.
- **Added** `SchemaShouldRejectEachInvalidClassificationDimension` — flips exactly one field on a single non-audit class (`source-email-metadata`) so completeness stays satisfied and only the targeted error code surfaces; each dimension's branch is exercised independently.

### B. AC4 — the `Validate(DataClassInventory)` header path was 0% covered
The contract tests only ever called `ValidateChangeSet`. The artifact-header guard in `Validate(inventory)` (unsafe owner/version, unknown schema version, non-UTC last-reviewed, null) was never exercised — a complete classification set with a malformed header would have validated silently in test.
- **Added** `ValidateShouldRejectAMalformedVersionedArtifactHeader` — null / unsafe owner / unsafe version / unknown schema version / non-UTC `LastReviewedAtUtc` ⇒ `data_class_inventory_invalid`.

### C. AC4 — the versioned-artifact header was never directly asserted on the seed
AC4 requires the inventory to carry **owner, version, last-reviewed date, schema version**; this was only implied by the bijection test.
- **Added** `SeedCatalogShouldExposeTheVersionedArtifactHeader` — asserts `Owner == compliance-admin`, `Version == data-class-inventory-v1`, `SchemaVersion == V1`, `LastReviewedAtUtc == SeedLastReviewedAtUtc` and UTC (quarterly-review clock).

### D. AC1/AC4 — null / empty change set edge untested
- **Added** `ValidateChangeSetShouldRejectNullAndEmptyClassificationSets` — `ValidateChangeSet(null)` and the empty set ⇒ `data_class_inventory_invalid`.

## Generated tests

| Layer | File | New tests |
|---|---|---|
| Inventory schema validation (AC1/AC4) | `DataClassInventoryContractTests` | +4 methods (18 → 22) |

## Coverage

| Acceptance criterion | Status |
|---|---|
| AC1 — full classification tuple over closed dimension sets | Covered (closed-set membership + **every per-dimension rejection branch** + completeness + WORM) |
| AC2 — fail-closed compliance-admin gating + audited rejection | Covered by dev story (`DataClassInventoryAuthorizationTests`, `CommandGatewayTests`); re-verified green |
| AC3 — actor/old-new/timestamp/policy-snapshot recording | Covered by dev story (per-class audit evidence refs); re-verified green |
| AC4 — versioned artifact + completeness invariant | Covered (bijection + **versioned-artifact header assert + header-validation rejection path**) |
| Cross-cutting — no-leak / cross-tenant / boundary | Covered by dev story (`DataClassInventoryLeakageScanTests`, fitness suite); re-verified green |

- Distinct `DataClassInventorySchema` error codes asserted: **9/9** (was 4/9).
- `Validate(DataClassInventory)` header path: **covered** (was 0%).

## Test run results (after changes)

| Suite | Filter | Result |
|---|---|---|
| `Hexalith.ChatBot.Contracts.Tests` | `~DataClassInventory` | **22 passed** (was 18), 0 failed |
| `Hexalith.ChatBot.Server.Tests` | `~DataClassInventory` | **4 passed**, 0 failed |
| `Hexalith.ChatBot.Conformance.Tests` | `~DataClassInventory` | **1 passed**, 0 failed |

All run suites: **Passed — 0 failed, 0 skipped.**

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API/behavioral tests generated · [x] E2E n/a (no UI surface — live editor is a documented deferral); gateway-integration + conformance cover the behavioral path
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly) · [x] Happy path · [x] Critical/negative cases (every closed-set rejection, malformed header, null/empty set)
- [x] All generated tests run successfully (22/4/1, 0 failed) · [x] Semantic assertions, no hardcoded waits/sleeps (deterministic seed constants, fixed UTC) · [x] Clear descriptions · [x] Tests independent (pure functions over the immutable seed catalog, no shared state)
- [x] Summary created · [x] Tests saved to appropriate directories · [x] Coverage metrics included

## Next steps

- Run the new tests in CI alongside the existing Epic 9 suites (no new test project — all land in the existing `Hexalith.ChatBot.Contracts.Tests` csproj).
- When the deferred live S-tagged Data Governance editor UI ships, add E2E coverage for the editor → `SubmitDataClassInventoryChange` round-trip (out of scope here per the inert-control-floor deferral).
