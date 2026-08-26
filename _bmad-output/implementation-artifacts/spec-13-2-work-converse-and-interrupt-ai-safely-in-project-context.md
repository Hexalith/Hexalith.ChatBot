---
title: 'Work, converse, and interrupt AI safely in project context'
type: 'feature'
created: '2026-08-26'
status: 'in-progress'
review_loop_iteration: 1
baseline_commit: '397507bebd85a672988e960e167c8cab6d102630'
context:
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/implementation-conformance-addendum-2026-07-17.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The conversation route cannot safely deliver Story 13.2: healthy reads render degraded, secured UI calls lack the contributor token, cancellation targets the wrong aggregate and treats a request as terminal, synchronous AI execution is not stoppable, history/project switches lose authoritative state, and browser tests skip the real workflow.

**Approach:** Complete one conversation-centric, event-sourced lifecycle from authenticated UI admission through asynchronous AI execution, cancellation confirmation, typed projection refresh, accessible rendering, and real-route acceptance evidence.

## Boundaries & Constraints

**Always:** Authorize tenant/project before access; derive risk/authority server-side; persist lifecycle only through EventStore; treat SignalR as a metadata-only requery nudge; preserve Fluent V5, EN/FR, focus/live regions, redaction, exact concurrency, and warnings-as-errors.

**Ask First:** Any breaking public contract, identity-provider claim/client redesign beyond the existing `hexalith-chatbot` OIDC client, or change required inside a sibling submodule.

**Never:** Edit the dirty EventStore/Tenants submodules; add direct persistence; trust client risk/tenant/actor claims; project a cancellation request as completed; ship fabricated project choices; clear drafts before admission; or claim acceptance from source scans, `SetContentAsync`, screenshots, or a diagnostic fallback alone.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Workspace | no project, empty, active, switch, history | Correct state; newest truth stays visible; older items merge without forced scroll | Opaque denial; degradation keeps the last safe same-project view |
| Submit | Message/Ask-AI, including version `0` | Authenticated admission; risky AI becomes proposal; draft clears after acceptance | Blank focuses validation; rejection preserves draft and safe action |
| Stop | active exact identity/version | Aggregate records Cancelling; executor confirmation becomes stopped | Wrong/stale/terminal/duplicate targets fail closed/no-op without fake success |
| Race/recovery | completion race, duplicate nudge, reconnect, late load | Typed requery wins; natural completion is not announced stopped; focus returns once | Stale responses cannot overwrite newer state; reconnect rebuilds/rejoins |
| Presentation | EN/FR; supported widths/modes | Attribution, redaction, status, action, focus, announcements remain usable | No color/motion-only meaning or clipped critical action |

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Server/Gateway/Stages/{ParticipantAuthorizationStage,AcceptedCommandDispatcher}.cs` -- project authorization, shared conversation routing, and admission only; never start provider work from a generic submit receipt.
- `src/Hexalith.ChatBot.Server/{Operations,Governance,Lifecycle,Adapters/AiProvider,Projections}/` -- exact lifecycle plus a ChatBot-owned durable coordinator/outbox driven by persisted Started truth, restart/multi-replica recovery, bounded retry, and production newest-first cursor reads/nudges.
- `src/Hexalith.ChatBot.UI/{Program.cs,Hosting,State/ProjectConversation,Services,Components,Localization,wwwroot}/` -- public-client OIDC using existing FrontComposer circuit identity/sign-out/token-lifecycle seams, typed independently-correlated current/history state, bounded verification, receipts, and accessible outcomes.
- `tests/Hexalith.ChatBot.{Server,UI,UI.E2E,IntegrationTests}/` -- aggregate/gateway/read-model invariants, framework auth and hub tests, a stateful component route, and a separate authenticated Aspire/Keycloak route using the production client and gateway.
- `references/Hexalith.EventStore/**` and `references/Hexalith.Tenants/**` -- read-only user changes; use existing APIs without edits.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.ChatBot.Server/Gateway/Stages/{ParticipantAuthorizationStage,AcceptedCommandDispatcher}.cs` and `Operations/{GovernedOperationAggregate,GovernedOperationState,ActiveAiResponseGeneration}.cs` -- unify conversation identity; authorize proposal/approval commands; accept version `0` only for the observed empty-project proposal baseline; reject overflow; and enforce tenant/project/conversation/response/generation/active/exact-stream-version invariants.
- [ ] `src/Hexalith.ChatBot.Server/{Lifecycle,Adapters/AiProvider,Governance,Projections}/` -- invoke providers only from persisted Started truth; durably and idempotently recover/route work and cancellation across restart/replicas using the full identity; bound concurrency and terminal retries; preserve natural completion/pending approval in Stop races; emit confirmed/failed outcomes; and prove production read-model nudges/cursors.
- [ ] `src/Hexalith.ChatBot.UI/{Program.cs,Hosting,State/ProjectConversation,Services,Components,Localization,wwwroot}/` -- forward only a valid contributor token through HTTP/SignalR; wire circuit user context and working sign-in/sign-out eviction without redesigning the public OIDC client; fix `Current`; independently correlate current/history loads; reject mismatched projects; purge omitted/redacted current-page items; bound cancellation verification through failures; use exact denial codes; repair composer behavior; and return focus with circuit-scoped exactly-once announcement.
- [ ] `tests/Hexalith.ChatBot.{Server,UI,UI.E2E,IntegrationTests}/` -- cover production admission-marker round trip, denied project commands, completion/failure handlers, restart/replica/race/backpressure, production DAPR-store cursor+nudge, concurrent current/history loads, redaction/mismatch, token expiry/sign-out/reconnect, live status/history/remount behavior, plus both stateful component proof and an authenticated production-client Aspire/Keycloak Message/Ask-AI/Stop route with zero skips and the locale/width/mode matrix.

**Acceptance Criteria:**
- Given any Story 13.2 route state, when the real route runs, then governed context, attribution, redaction, focus, status, and next action satisfy the matrix without ungoverned fallback.
- Given Message or Ask-AI, when submitted, then authenticated CommandGateway admission is authoritative and risky work is proposal-gated.
- Given active generation or every race/invalid case, when Stop/nudges/reconnect occur, then only executor-confirmed server state becomes terminal and the UI focuses/announces exactly as specified.
- Given all supported locales, modes, and widths, when browser invariants run, then no critical information/action is inaccessible or motion/color dependent.

## Spec Change Log

- 2026-08-26 review loop 1: Review found provider execution launched from a generic submit receipt, process-local cancellation with no restart/replica recovery, unsafe terminal/race retry behavior, incomplete FrontComposer auth lifecycle, current/history/redaction races, and browser evidence that replaced the production client. Amended the non-frozen code map, tasks, design, and verification to require persisted-event-driven durable coordination and direct production-topology evidence. Avoid the known-bad post-submit/process-local scheduler and fake-only acceptance lane. KEEP: shared conversation aggregate identity; server-derived risk/project authorization; Active→Cancelling→separate confirmed/failed events; only observed provider cancellation may announce stopped; typed SignalR requery; newest-first deterministic cursor intent; correlated safe-state reducers; contributor bearer relay; composer draft/focus repairs; circuit-scoped announcement dedupe; and the independently advancing stateful route as supporting UI proof.

## Design Notes

Use the started progress conversation id as the shared aggregate identity. A persisted Started event/outbox signal—not a gateway submit receipt—owns provider scheduling. The coordinator is idempotent, bounded, keyed by tenant/project/conversation/response/generation, resumes Started/Cancelling work after restart, and routes cancellation across replicas; terminal submissions survive transient failure. Cancellation moves Active to Cancelling; observed cancellation confirms stopped, while a normal provider return preserves natural completion and a provider approval result remains proposal-gated. Treat the stateful fake route as supporting UI proof only; production acceptance keeps the real client, authentication, CommandGateway, EventStore, and SignalR path.

## Verification

**Commands:**
- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1 -nodeReuse:false` -- expected: 0 warnings/errors.
- Build and invoke Server, UI, UI.E2E, AppHost, and Integration assemblies per project with focused `-class` lanes -- expected: every task invariant has a direct passing test.
- Run the stateful component route and authenticated production-client Aspire/Keycloak route in Chrome across EN/FR, supported widths, normal/reduced motion, and forced colors -- expected: Message/Ask-AI/Stop pass through real admission and browser lanes report 0 skipped.
- `git diff --check` -- expected: clean.
