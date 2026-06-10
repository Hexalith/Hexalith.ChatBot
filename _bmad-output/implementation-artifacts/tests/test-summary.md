# Test Automation Summary

## Generated Tests

### API / Contract Tests
- [x] Existing `tests/Hexalith.ChatBot.Contracts.Tests/SenderAuthorityContractTests.cs` covers the five authority wire tokens, finite conflict reasons, metadata-only serialization, and secret-bearing public contract property guards.
- [x] Existing `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/SenderAuthorityClassifierTests.cs` covers all five successful mappings, all four stable fail-closed reasons, provider-posture-only denial, service-client grant interplay, shared-mailbox membership freshness, and metadata-only denial refs.
- [x] Existing `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs` and `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` cover adapter boundary guards so UI/CLI/MCP cannot reference server outbound classifier or gateway internals.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Governance/Outbound/SenderAuthorityClassificationWorkflowE2ETests.cs` covers the Story 6.1 integration/E2E boundary for classification result round-trip payloads.
- [x] Added `ApprovedServiceSendWorkflowShouldCarryGrantAndApprovalEvidenceAtBoundary` to prove successful `approved service-send` exposes both metadata-only `service-client:*` and paired `approval:*` evidence at the boundary.
- [x] No Playwright/browser E2E was added; Story 6.1 has no visible UI surface.

## Coverage
- Authority success mappings: 5/5 covered (`draft-only`, `authenticated-user send`, `shared-mailbox send`, `send-on-behalf`, `approved service-send`).
- Explicit conflict reasons: 4/4 covered (`policy-blocked`, `delegation-mismatch`, `membership-revoked`, `approval-missing`).
- Metadata-only denial/redaction sentinels: covered in contract, server, and integration/E2E tests.
- Service-client grant plus approval-chain interplay: covered for success, missing approval, and missing outbound grant.
- Shared-mailbox membership freshness/no downgrade behavior: covered.
- Adapter boundary/classifier replication guards: covered for UI, CLI, MCP, and future surface adapters.

## Validation
- [x] `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings/errors.
- [x] `./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none -class Hexalith.ChatBot.IntegrationTests.Governance.Outbound.SenderAuthorityClassificationWorkflowE2ETests` - passed, 14 tests.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings/errors.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 480 tests.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1557 tests.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 39 tests.

## Checklist Validation
- [x] API tests generated where applicable; Story 6.1 contract/server behavior is covered through xUnit v3 contract and server tests.
- [x] E2E tests generated where UI exists; no UI exists for Story 6.1, and the integration/E2E boundary round-trips classifier payloads.
- [x] Tests use standard xUnit v3, Shouldly, and project-local deterministic models.
- [x] Tests cover happy paths for all five authority classes.
- [x] Tests cover critical error cases for all four explicit conflict reasons.
- [x] All generated and relevant tests run successfully.
- [x] Tests use semantic contract/result fields rather than presentation strings or hardcoded waits.
- [x] Tests have clear descriptions and are independent.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to the existing integration test project.
- [x] Summary includes coverage metrics.

## Next Steps
- Keep the Story 6.1 E2E boundary matrix aligned with future outbound adapter/public command exposure.
