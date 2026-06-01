# Epic 5 Documentation Update Audit

**Project:** Chatbot
**Epic:** 5 - Cross-Surface Parity: CLI & MCP
**Audit mode:** Autonomous verification after retrospective
**Date:** 2026-06-02

## Verification Method

Each proposed documentation update was checked against current implementation artifacts before editing:

- Sprint status and story records: `_bmad-output/implementation-artifacts/sprint-status.yaml`, `5-1` through `5-4` story files.
- Production code: `src/Hexalith.ChatBot.Cli`, `src/Hexalith.ChatBot.Mcp`, `src/Hexalith.ChatBot.Contracts`, `src/Hexalith.ChatBot.Client`, and service-client gateway code referenced by Story 5.1.
- Test evidence: `tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs`, `Epic5AdapterIntentParityTests.cs`, architecture tests, CLI tests, MCP tests, and story validation notes.
- Documentation candidates: `README.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, PRD, PRD addendum, OpenAPI, and package configuration.

## Proposed Documentation List

1. `README.md`
   - Proposed issue: README summarized Epic 1 through Epic 4 only and did not mention implemented Epic 5 CLI/MCP/service-client/conformance behavior.
   - Code comparison: `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs` submits with `ChatBotSurfaceOrigin.Cli`; `src/Hexalith.ChatBot.Mcp/ChatBotMcpService.cs` submits with `ChatBotSurfaceOrigin.Mcp`; conformance tests drive both production adapters.
   - Decision: Update required and applied.

2. `_bmad-output/planning-artifacts/architecture.md`
   - Proposed issue: several sections still described differential conformance as M0 thin-shim coverage and listed Keycloak service-account flows / full CLI/MCP harness wiring as future M1 detail.
   - Code comparison: `tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs` now states CLI and MCP arms execute production adapters; `Epic5AdapterIntentParityTests.cs` guards UI/API, CLI, MCP surfaces and Epic 5 intent coverage; Story 5.1 implemented service-account/grant validation.
   - Decision: Update required and applied.

3. `_bmad-output/planning-artifacts/epics.md`
   - Proposed issue: possible mismatch in Epic 5/6 definitions after implementation.
   - Code comparison: Epic 5 story text says Story 5.4 replaces M0 shims with the full harness, which matches implementation. Epic 6 remains planned sender-authority/authenticity work and does not claim implementation.
   - Decision: No update required.

4. `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
   - Proposed issue: possible stale M1 CLI/MCP parity language.
   - Code comparison: PRD describes product requirements and MVP increment goals. It does not claim CLI/MCP are absent in the current codebase, and its M1 scope matches Epic 5 implementation.
   - Decision: No update required.

5. `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
   - Proposed issue: shared command-pipeline invariant might diverge after adapters landed.
   - Code comparison: Addendum says adapters translate surface input into typed commands and must not replicate pipeline stages. CLI/MCP code and architecture tests match this invariant.
   - Decision: No update required.

6. `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
   - Proposed issue: API docs may need new CLI/MCP endpoints.
   - Code comparison: CLI and MCP wrap existing `IChatBotClient` operations and do not add new public HTTP shapes. Story 5.1 notes no OpenAPI/generated-client update was required; Story 5.2 through 5.4 confirm no generated-client changes.
   - Decision: No update required.

7. `Directory.Packages.props`
   - Proposed issue: configuration docs might need package version changes.
   - Code comparison: CLI project references central `System.CommandLine`; MCP project references central `ModelContextProtocol`; package pins are present as `System.CommandLine` 2.0.8 and `ModelContextProtocol` 1.3.0.
   - Decision: No update required.

## Applied Updates

- `README.md`
  - Added Epic 5 to the project overview.
  - Added runtime guidance that CLI and MCP are thin adapters over `Hexalith.ChatBot.Client`, not local governance/data-plane clients.
  - Added guidance that current parity verification uses production adapter-backed conformance, not only M0 shims.

- `_bmad-output/planning-artifacts/architecture.md`
  - Updated the API/communication parity section to distinguish historical M0 shims from the current Epic 5 production adapter-backed harness.
  - Updated pattern enforcement language for differential conformance.
  - Updated project-structure comments for `Hexalith.ChatBot.Conformance.Tests`.
  - Updated the deferred-work note so CLI/MCP adapters are no longer described as only future M1 work.
  - Updated the M1 gap list so implemented service-account flows and CLI/MCP harness wiring are no longer described as pending.

## Discarded Updates

- Epic definitions were not edited because their Epic 5 and Epic 6 story requirements match current implementation/planning state.
- PRD and addendum were not edited because they remain accurate requirements/specification documents rather than implementation-status reports.
- OpenAPI was not edited because Epic 5 added adapter behavior over existing client operations and did not change HTTP contract shapes.
- Package configuration was not edited because central pins already match the implemented CLI and MCP projects.

## Residual Watch Items

- After Epic 6, re-audit README and architecture for outbound authority and inbound-authenticity implementation claims.
- Keep architecture wording explicit when planned M1/M2 features move from future-state to implemented-state.
- Continue checking story File Lists, sprint-status rows, and test summaries before marking stories done.
