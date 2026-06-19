# ADR: AI-response streaming transport

## Status

Accepted (2026-06-19, Story 10.6a).

## Context

Epic 10 adds the governed interactive chat surface as a write surface on the existing CommandGateway spine. Story
10.6b must add progressive AI response rendering and an always-reachable Stop/Cancel control, but architecture left the
transport decision open: extend the existing SignalR projection-nudge model or introduce a dedicated streaming channel.

The existing UI transport posture is REST commands/queries plus SignalR projection nudges. SignalR pushed data is not
authoritative: clients re-query server state after a nudge and never trust payload content as durable state. The
streaming decision must preserve that safety floor, tenant isolation, authorization ownership, and the rule that writes
and cancellation authority do not bypass CommandGateway.

## Decision

### Extend the SignalR projection-nudge model

Story 10.6b must implement AI response streaming by extending the existing SignalR projection-nudge model with
metadata-only AI response progress nudges. It must not introduce a dedicated token/content streaming channel as the
default transport.

The transport contract is:

- AI response generation starts only after the initiating message or ask-AI request is admitted through CommandGateway.
- The server owns tenant, authorization, project, conversation, and generation-session binding before joining any client
  to a SignalR group or sending any nudge.
- SignalR messages are advisory nudges only. They may carry metadata such as project id, conversation id, response id or
  generation id, correlation id, server projection version, sequence number, and progress state.
- SignalR messages must not carry authoritative response text, raw provider chunks, prompts, workspace content, file
  content, mailbox data, hidden policy details, raw exceptions, stack traces, or other sensitive payloads.
- After each accepted nudge, the UI re-queries the typed server read endpoint for the current partial response projection
  and renders only that server-returned state.
- Durable completion is claimed only after a server query verifies a terminal state such as completed, stopped,
  cancelled, failed, or unavailable. A final SignalR nudge alone is not completion evidence.
- The client must fail closed on missing, stale, unauthorized, ambiguous, cross-tenant, cross-project, or
  session-mismatched state by stopping progressive rendering and showing a safe pending, unavailable, or retry state.

This keeps progressive rendering as a read/projection concern. The stream is a sequence of "state changed; re-query"
signals, not a second source of truth.

### Stop/Cancel semantics for 10.6b

Stop/Cancel is a governed user action, not a client-side stream abort with durable meaning.

- The Stop/Cancel control remains keyboard-reachable in a stable focusable position while generation is active.
- Activating Stop/Cancel submits the approved cancellation command/path through the same governed client and
  CommandGateway admission spine used by the composer.
- The UI may immediately disable duplicate cancellation attempts and show a cancelling/pending state, but it must not
  claim "stopped" until a server read verifies the cancellation terminal state.
- SignalR may nudge that cancellation was requested or observed, but the UI still re-queries and verifies.
- If cancellation authority is ambiguous, the generation/session is unknown, authorization cannot be verified, or the
  transport disconnects before server confirmation, the UI fails closed to pending/unavailable rather than fabricating a
  stopped state.

### Reconnect, resume, and stale-message handling for 10.6b

Story 10.6b must treat SignalR delivery as lossy and unordered.

- On reconnect, the client rejoins only server-authorized project/conversation groups and re-queries the latest
  conversation or response state before rendering.
- Missed nudges are acceptable because server state is the source of truth.
- Every nudge that affects a visible response must include enough correlation metadata for the client to match the
  selected tenant/project/conversation/session and the expected response id.
- The client ignores stale or out-of-order nudges whose version or sequence is not newer than the last rendered server
  state.
- A nudge with unexpected tenant, project, conversation, session, response, or version context is treated as unsafe; the
  UI stops using it and re-queries or degrades.

### Accessibility handoff for 10.6b

Story 10.6b must implement UX-DR32 independent of transport mechanics:

- progressive partial response rendering;
- Stop/Cancel always reachable by keyboard in a stable focusable position;
- a polite live-region announcement for "Response stopped" after server-verified stop/cancel;
- focus return to the composer or AI proposal panel after verified interruption;
- reduced-motion behavior and no motion-only status.

### Tests expected for Story 10.6b

Story 10.6b must land automated evidence for the contract above. The expected tests are:

- **Progressive rendering re-query test** — after a progress nudge, the UI renders only the server-returned partial
  projection and never renders nudge payload content as if it were authoritative response text.
- **Durable-completion gate test** — completed/stopped/cancelled/failed/unavailable is asserted only after a server read
  verifies the terminal state; a final nudge alone must not flip the UI to a completed state.
- **Fail-closed test** — missing, stale, unauthorized, ambiguous, cross-tenant, cross-project, or session-mismatched
  state stops progressive rendering and degrades to a safe pending/unavailable/retry state.
- **Stop/Cancel governance test** — activating Stop/Cancel submits the cancellation through the governed
  `IChatBotClient`/CommandGateway path; the UI shows cancelling/pending and never claims "stopped" before server
  verification; cancellation never bypasses CommandGateway.
- **Reconnect/resume test** — on reconnect the client rejoins only server-authorized project/conversation groups and
  re-queries server state before rendering; missed nudges do not corrupt rendered state.
- **Stale/out-of-order nudge test** — nudges whose version/sequence is not newer than the last rendered server state, or
  whose tenant/project/conversation/session/response context is unexpected, are ignored.
- **Metadata-only payload guard** — a source/contract test asserts nudges carry only metadata (ids, version, sequence,
  correlation, progress state) and never response text, raw provider chunks, prompts, workspace/file content, mailbox
  data, policy internals, raw exceptions, or stack traces.
- **Accessibility tests for UX-DR32** — Stop/Cancel keyboard-reachable in a stable focusable position, polite
  live-region "Response stopped" announcement after server-verified stop/cancel, focus return to composer/AI proposal
  panel, reduced-motion behavior, and no motion-only status.

## Consequences

- The transport decision preserves the current REST query plus SignalR nudge architecture instead of adding another
  browser communication subsystem.
- Pushed data remains metadata-only and non-authoritative, which keeps tenant and authorization decisions server-owned.
- Progressive rendering depends on an efficient read/projection shape for partial AI responses; Story 10.6b must throttle
  or coalesce re-queries if needed to avoid unnecessary server load.
- Reconnect and resume behavior is simple: rejoin authorized groups, query server state, and continue from verified
  projection state.
- Cancellation remains governed by CommandGateway. SignalR can improve responsiveness but cannot become a write path.
- The UI can show transient progress based on verified server read state, but completion, stopped, cancelled, failed, and
  unavailable states are durable only when confirmed by server reads.
- CLI and MCP parity are preserved because CLI/MCP do not need to consume the visual SignalR nudges or bypass
  `IChatBotClient`; they can keep using typed command/query paths.

## Alternatives Considered

- **Introduce a dedicated streaming channel.** Rejected as the default for Story 10.6b. A dedicated channel would require
  new authorization, tenant/session binding, reconnect, backpressure, observability, and stale-message rules parallel to
  the existing SignalR posture. It also creates pressure to stream raw provider chunks or content-bearing payloads to the
  browser, increasing the chance that pushed data becomes treated as authoritative. This can be reconsidered later only
  with a separate ADR and a proven need that the metadata-nudge model cannot satisfy.
- **Stream raw AI provider tokens directly to the UI.** Rejected. Raw provider streams are not governed conversation
  state, can expose prompts or policy/provider details, and would bypass the server-owned verification point needed before
  durable completion claims.
- **Poll only with no SignalR nudge.** Rejected as the default for 10.6b because UX-DR32 requires responsive progressive
  rendering and interruption feedback. Polling can remain a degraded fallback after disconnect or unsupported transport.

## Verification

Story 10.6a verification is documentation/source verification only:

- `rg -n "Status|Accepted|SignalR|projection-nudge|dedicated streaming channel|never trust payload|fail-closed|CommandGateway|10.6b|Stop/Cancel|Verification" docs/adrs/ai-response-streaming-transport.md`
- `rg -n "ai-response-streaming-transport.md|Story 10.6a|accepted ADR|AI-response streaming transport" _bmad-output/planning-artifacts/architecture.md`
- `git diff --name-only -- src Hexalith.FrontComposer` must be empty for Story 10.6a (no production or FrontComposer
  changes).
- The only allowed `tests/` changes for Story 10.6a are the ADR conformance guard
  (`tests/Hexalith.ChatBot.Architecture.Tests/AiResponseStreamingTransportAdrTests.cs`) and the Epic 10 readiness-guard
  update forced by moving `10-6a-streaming-transport-adr` to `review`
  (`tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs`); no streaming transport, hub, channel,
  client, service, or UI behavior code is added.
- `git diff --check`
