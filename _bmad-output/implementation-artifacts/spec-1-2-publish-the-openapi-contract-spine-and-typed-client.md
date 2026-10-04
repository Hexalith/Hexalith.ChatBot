---
title: 'Story 1.2: Publish the OpenAPI Contract Spine and Typed Client'
type: 'feature'
created: '2026-10-05'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The existing OpenAPI 3.1 spine and client cover submission and status, but callers cannot reuse an identity for retries. Accepted results omit retry/prior-outcome facts, server DTOs duplicate the contract, and current checks cannot prove generated or serialized parity.

**Approach:** Complete the contract and typed Client additively; make generation, exposure, serialization, and safe-outcome checks fail on drift.

## Boundaries & Constraints

**Always:** Keep OpenAPI 3.1 authoritative for touched HTTP routes/DTOs/errors. Retain typed `IChatBotCommand` submission, cancellation, correlation, and existing callers; accept caller-stable `commandId` for replay. Preserve status identity as `taskId ?? commandId`. Use camelCase, UTC `DateTimeOffset`, declared IDs, opaque cursors, and versioned safe codes. Emit metadata-only, existence-neutral outcomes. Keep exposure and authorization separate and deny-by-default.

**Never:** Hand-edit NSwag output, create competing wire DTOs, trust client tenant claims, weaken gateway/idempotency/audit rules, edit sibling submodules, or claim runtime parity or release readiness from fixtures.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Submit | Typed command, stable command ULID, correlation, cancellation | Request retains identity; response gives canonical status ID, state, reason, retry eligibility, optional prior outcome | Invalid ULID fails before transport |
| Replay | Same command and ULID | Same identity and stored outcome representation | Conflicting reuse yields safe versioned problem |
| Status/read | Authorized operation or page | Safe status; opaque cursor; UTC time | Unknown/unauthorized resources remain existence-neutral |

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` -- sole `SubmitCommand`/`GetOperationStatus` HTTP schema; extend additively.
- `src/Hexalith.ChatBot.Client/nswag.json`, `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` -- deterministic generation path; regenerate only.
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs`, `ChatBotClient.cs` -- facade and generated `IClient` bridge; preserve existing signature.
- `src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionWireRequest.cs`, `CommandGatewayHttpResults.cs`, `ChatBotCompatibilityEndpointExtensions.cs`, `CommandGateway.cs`, `Status/OperationStatusRecord.cs`, `ChatBotProblemDetailsFactory.cs`, `Idempotency/*CoarseIdempotencyStore.cs` -- duplicate DTOs, accepted/problem mapping, status identity, stored outcomes.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogVersion.cs`, `ChatBotMessageCodes.cs` -- safe version/code authority.
- `src/Hexalith.ChatBot.Mcp/ChatBotMcpToolCatalog.cs`, `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs` -- separately governed surface metadata; inspect, do not infer grants from OpenAPI.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` -- add compatible operation/accepted/status/prior-outcome and versioned RFC 9457 semantics; bind ID, UTC, and cursor rules.
- [ ] `src/Hexalith.ChatBot.Client/IChatBotClient.cs`, `src/Hexalith.ChatBot.Client/ChatBotClient.cs` -- add caller-stable `commandId` submission without breaking existing calls; validate before transport.
- [ ] `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, `tests/fixtures/hexalith-chatbot-generated-client.sha256` -- regenerate and retain deterministic provenance; change hash only after independent comparison.
- [ ] `src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionWireRequest.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`, `src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs` -- remove competing DTOs and serialize via contract-bound adapter machinery.
- [ ] `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`, `src/Hexalith.ChatBot.Server/Gateway/Idempotency/InMemoryCoarseIdempotencyStore.cs`, `src/Hexalith.ChatBot.Server/Gateway/Idempotency/DaprCoarseIdempotencyStore.cs` -- populate safe accepted/problem fields and preserve them in prior outcomes.
- [ ] `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs`, `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`, `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs`, `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` -- cover the matrix, reject duplicate DTOs, compare isolated regeneration, and bind existing MCP/CLI exposure metadata plus client signatures to OpenAPI operations.
- [ ] `tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOracleTests.cs`, `tests/fixtures/story-1-2-contract-spine-oracle.json` -- execute representative request, accepted, pending, replay/conflict, safe problem, status, and cursor round-trips with non-vacuous normalization assertions.

**Acceptance Criteria:**
- Given the HTTP boundary, when contract and architecture checks run, then OpenAPI 3.1 alone defines touched wire DTOs; handwritten competitors fail.
- Given the canonical spec, when generation/exposure checks run, then generated output, client signatures, and separate exposure metadata match; drift fails.
- Given a stable ULID and retry, when the facade submits, then both requests carry that ID with correlation/cancellation; invalid IDs fail before transport.
- Given accepted, pending, replayed, or denied outcomes, when oracle tests run, then state/reason/retry/prior-outcome round-trip as camelCase, UTC, metadata-only versioned RFC 9457 data without restricted or existence-revealing detail.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet restore Hexalith.ChatBot.slnx` -- restore succeeds with central package authority.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` -- solution compiles with warnings treated as errors.
- Build and run Contracts, Client, Architecture, and Conformance test projects individually; use their direct xUnit runner if needed -- affected tests execute with no required skips.
