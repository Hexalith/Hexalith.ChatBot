# Epic 5 Documentation Update Audit - 2026-06-10

**Project:** Chatbot
**Epic:** 5 - Cross-Surface Parity: CLI & MCP
**Mode:** Autonomous YOLO
**Purpose:** Verify documentation updates suggested by Epic 5 implementation learnings against current source code before editing docs.

## Evidence Read

- Sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Stories: `5-1-service-client-identities-and-scoped-grants.md`, `5-2-cli-adapter-and-workflow-parity.md`, `5-3-mcp-adapter-and-governed-tool-surface.md`, `5-4-cross-surface-equivalence-verification.md`
- Prior retrospective: `_bmad-output/implementation-artifacts/epic-4-retro-2026-06-10.md`
- Existing Epic 5 retrospective: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-02.md`
- Planning docs: `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- Current implementation/config: `README.md`, `Directory.Packages.props`, `src/Hexalith.ChatBot.Mcp/Hexalith.ChatBot.Mcp.csproj`, `src/Hexalith.ChatBot.Mcp/Program.cs`, `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs`, `src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs`, `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`, `tests/Hexalith.ChatBot.Conformance.Tests/Story54DenialParityTests.cs`, `tests/Hexalith.ChatBot.Conformance.Tests/Harness/DenialConformanceHarness.cs`

## Proposed Documentation Updates

1. README implementation notes for Epic 5 CLI/MCP behavior.
2. Architecture decisions for MCP package/transport and cross-surface conformance.
3. Epic planning artifact references to MCP package/transport.
4. PRD/addendum parity and shared-command-pipeline requirements.
5. OpenAPI command/query contract shape.
6. Configuration/package documentation.

## Verification Results

| Document | Verification | Result |
|---|---|---|
| `README.md` | Compared README Epic 5 notes with `ChatBotCliService`, `ChatBotMcpService`, `ChatBotMcpResultFormatter`, conformance denial tests, and adapter boundary tests. | Current. It accurately says CLI/MCP are thin `Hexalith.ChatBot.Client` adapters and that the conformance harness uses production CLI/MCP translation paths. No update required. |
| `_bmad-output/planning-artifacts/architecture.md` | Compared architecture MCP version/transport statements with `Directory.Packages.props`, `Hexalith.ChatBot.Mcp.csproj`, `Program.cs`, and architecture tests. | Update required and applied. Replaced stale `ModelContextProtocol 1.3.x` and `.AspNetCore` wording with repo-pinned `ModelContextProtocol 1.4.0` and current stdio transport. |
| `_bmad-output/planning-artifacts/epics.md` | Compared Epic 5 MCP planning text with actual adapter implementation and repo package pin. | Update required and applied. Replaced stale `1.3.x` / `.AspNetCore` wording with the implemented `ModelContextProtocol 1.4.0` stdio adapter over `Hexalith.ChatBot.Client`. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | Checked FR81a-FR86, NFR2, NFR4-NFR6, NFR32-NFR34 against CLI/MCP/conformance implementation. | Current as a requirements source. It does not pin MCP transport or package version. No update required. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | Checked shared command pipeline and outbound authority sections against adapter implementation and Epic 6 preparation needs. | Current. It correctly states adapters translate into typed commands and cannot replicate governance stages. No update required. |
| `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` | Compared Epic 5 story outcomes with client methods and adapter usage. | Current. Epic 5 added adapter behavior, service-client contracts, and conformance coverage without requiring new public HTTP paths for CLI/MCP. No manual update required. |
| `Directory.Packages.props` | Compared package config with source/test expectations. | Current. `System.CommandLine` is pinned to `2.0.8`; `ModelContextProtocol` is pinned to `1.4.0`; architecture tests assert the MCP pin. No update required. |

## Discarded Updates

- Do not update the PRD to mention `ModelContextProtocol 1.4.0`; the PRD is intentionally package-version agnostic for this area.
- Do not update OpenAPI for Epic 5; the implemented adapters use existing command submission and read methods.
- Do not edit historical retrospectives from June 2. They preserve their original context. The fresh June 10 retrospective records the corrected package/transport facts.

## Applied Updates

- `_bmad-output/planning-artifacts/architecture.md`
  - MCP surface table now names `ModelContextProtocol 1.4.0`.
  - Surface description now says current MCP implementation uses stdio transport.
  - Project structure now describes `Hexalith.ChatBot.Mcp` as a ModelContextProtocol stdio server.
  - Validation summary now names MCP SDK `1.4.0`.
- `_bmad-output/planning-artifacts/epics.md`
  - Technology line now names `ModelContextProtocol 1.4.0` and the current stdio adapter.
  - Story 5.3 wording now avoids the stale `.AspNetCore` transport claim.

## Follow-Up Watch Items

- Keep future retrospectives from copying historical `ModelContextProtocol 1.3.0` wording.
- If the MCP adapter later moves from stdio to an HTTP/AspNetCore transport, update `architecture.md`, `epics.md`, `README.md`, and `Program.cs` together with tests.
- Track the Story 5.1 low-severity follow-up: `ServiceClientGrantProjectionCache` proves bounded staleness/revocation semantics but is not on the live claims-direct admission path yet.
