# Test Automation Summary - Story 6.5

**Story:** 6.5 - On-behalf-of disambiguation and external-sender posture
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-02
**Framework:** xUnit v3 in-process runners and Shouldly.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - added `AssociationRoutingStatusEndpointShouldExposeExternalStrictnessPostureSafely`, covering external-sender posture, strictness policy, routing reason, and metadata-only API output.
- [x] Existing contract/API coverage in `tests/Hexalith.ChatBot.Contracts.Tests/*`, `tests/Hexalith.ChatBot.Client.Tests/*`, and `tests/Hexalith.ChatBot.Server.Tests/*` covers delegated sender posture contracts, OpenAPI/generated client shape, command routing, aggregate events, association scorer outcomes, outbound send-on-behalf symmetry, audit refs, and safe projection contracts.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` - added `ConversationStoreShouldPreserveDelegatedAndExternalPostureFromNewestSourceEmail`, covering source-email projection enrichment, stale replay resistance, delegated/principal posture, external-sender posture, and strictness metadata.
- [x] Existing in-process HTTP/API E2E-style tests cover mailbox intake, association routing status, project conversation projection reads, and cross-tenant denial surfaces. No new browser E2E was required because Story 6.5 posture is exposed through contract/query metadata rather than new visible UI labels.

## Coverage

- API/contracts: delegated sender, `principalFor`, external sender, party resolution state, strictness policy, routing reason, OpenAPI/generated client shape, and metadata-only serialization.
- Worker mapping: provider `sender`/`from` delegated-send authority, header/provider conflict handling, missing/malformed selected headers, repeated `Authentication-Results`, no body/subject forwarding, and foreign mailbox fail-closed behavior.
- Association routing: permissive/strict/paranoid external-sender routing, missing/invalid strictness defaulting to strict, unchanged deterministic scoring weights, and fail-closed/needs-review outcomes.
- Outbound symmetry: existing `SenderAuthorityClassifier` send-on-behalf behavior, `principal_for` retention, delegation mismatch, policy-blocked denial, and no second authority pipeline.
- Projection/audit/isolation: safe evidence refs, source-version replacement, stale replay ignore behavior, tenant partitioning, no raw header/body/provider payload leakage, and no cross-tenant posture leakage.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 512 tests.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E-style tests generated for the available HTTP/projection paths.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical stale replay and metadata leakage cases.
- [x] Tests use semantic HTTP/API/projection assertions; no hardcoded waits or sleeps.
- [x] Tests have clear descriptions.
- [x] Tests are independent and run without order dependency.
- [x] Test summary created with coverage metrics.

## Discovered Gaps Applied

- Added missing projection coverage proving delegated sender, `principalFor`, external sender, and strictness metadata survive newest source-email enrichment and are not overwritten by stale source-version replay.
- Added missing routing-status API coverage proving external-sender posture and strictness policy are exposed as finite safe fields without raw details.
