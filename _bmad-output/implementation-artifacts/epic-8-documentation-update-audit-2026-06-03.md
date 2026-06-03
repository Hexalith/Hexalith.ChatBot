# Epic 8 Documentation Update Audit

**Project:** Chatbot
**Epic:** 8 - Operational Dashboards & Observability
**Audit mode:** Autonomous YOLO, code-grounded verification
**Date:** 2026-06-03

## Verification Method

Each candidate documentation update was checked against the actual implementation before being applied or discarded. Sources used:

- Sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml` (all 5 Epic 8 stories `done`).
- Story records: `8-1` … `8-5-*.md` (Dev Notes, review findings, completion notes, File Lists).
- Production code (grep-verified file paths):
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs:118–128` — control-state/rate-limit provider registrations.
  - `src/Hexalith.ChatBot.Client/IChatBotClient.cs` — client read surface.
  - `src/Hexalith.ChatBot.Server/Observability/` — meter, instruments, evaluators, projectors, scope resolver.
- Contract evidence: `git log` on `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Documentation candidates: `README.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/addendum.md`.

A "discrepancy" means the documentation makes a claim the implementation contradicts. Forward-looking planning intent that has not yet been falsified is not treated as a discrepancy.

## Proposed Documentation List

1. **README.md — project overview stops at Epic 7 (no Epic 8 summary).**
   - Proposed issue: the opening paragraph narrates Epics 1–5 and 7 but has no Epic 8 sentence, so a reader cannot tell the observability layer exists.
   - Code comparison: Epic 8 shipped `OperationalDashboardProjector`, `ChatBotMetrics` (the `Hexalith.ChatBot` meter + seven instruments), `OperatingBaselineCatalog.Published` (13 SLOs), the five NFR43 alert evaluators + `OperationalAlertWiringCoordinator`, and `DependencyScopeResolver`/`DegradedDependencyIncident`/runbook diagnostics — all present in `src/Hexalith.ChatBot.Server/Observability/` and `Contracts/Queries/`.
   - Decision: **Update required and applied.** Added an Epic 8 summary sentence to the overview.

2. **README.md — Aspire/DAPR note says control-floor activation is "expected in Epic 8."**
   - Proposed issue: line 50 promised the Epic 7 control-state/rate-limit projections and notification runtime callers "(expected in Epic 8)." Epic 8 is complete.
   - Code comparison: `CommandGatewayServiceCollectionExtensions.cs:118–128` still registers `AlwaysActiveServiceClientControlStateProvider`, `AlwaysActiveAiActorControlStateProvider`, `AlwaysActiveCommandCapabilityControlStateProvider`, `AlwaysActiveOutboundChannelControlStateProvider`, and the matching `AlwaysUnlimited…RateLimitProvider` defaults. No durable read-side projection replaces them. All five Epic 8 stories confirm the projections and the periodic evaluator trigger were not materialized.
   - Decision: **Update required and applied.** Corrected the promise: Epic 8 did not deliver it; the `AlwaysActive…`/`AlwaysUnlimited…` defaults remain; activation is carried forward as Epic 8 retro action items #1–#2.

3. **README.md — no caveat that Epic 8's own dashboards/alerts are not live-wired.**
   - Proposed issue: a reader could assume the new dashboards and alerts are live operator-facing surfaces.
   - Code comparison: `IChatBotClient.cs` exposes only `SubmitAsync`, `GetOperationStatusAsync`, `GetOperationAuditHistoryAsync`, `GetAssociationRoutingStatusAsync`, `GetProjectConversationAsync`, `GetTaskIntentReviewAsync` — no dashboard/SLO read method. Story 8.1 notes the UI assembles a fail-safe all-`Unknown` placeholder; 8.2 notes the lag gauge emits nothing under `UnavailableAuditProjectionLagSource`; 8.4/8.5 note the alert coordinator and weekly runbook sampler have no runtime trigger.
   - Decision: **Update required and applied.** Added an Epic 8 Aspire/DAPR caveat paragraph (emission always-on; read path + runtime triggers deferred), matching the per-epic caveat house style.

4. **architecture.md — "control state (deferred to Epic 8 alongside the operational-metric pipeline)."**
   - Proposed issue: line 201 states the control-state materialization is "deferred to Epic 8." Epic 8 did not do it.
   - Code comparison: same evidence as item 2 — the `AlwaysActive…`/`AlwaysUnlimited…` defaults are still registered after Epic 8. The forward-reference is now falsified.
   - Decision: **Update required and applied.** Corrected to: materialization was originally targeted for Epic 8, but Epic 8 delivered only the operational-metric pipeline and read-only surfaces; the projection and runtime trigger remain deferred beyond Epic 8.

5. **architecture.md — "status enums never derived from counts" (line 477).**
   - Proposed issue: did Epic 8's dashboards honor this?
   - Code comparison: Story 8.1 uses the stable `ChatBotHealthStatus` (`healthy|degraded|failed|unknown`) verbatim; AC2 explicitly forbids count-derived status, and its review confirmed no count-vs-enum violation.
   - Decision: **No update required.** Documentation matches implementation.

6. **architecture.md — "Observability & SLOs … OpenTelemetry; per-class latency/queue/lag metrics; published SLOs" and "emit structured signal always; visualize later" (lines 150, 401).**
   - Proposed issue: does the implementation match the principle?
   - Code comparison: Story 8.2 registers the `Hexalith.ChatBot` meter into the always-on `WithMetrics` pipeline with four latency histograms, two counters, and the audit-lag gauge; Story 8.3 publishes the SLO catalog. Emission is load-bearing; dashboards are the trim-able read side. Exactly the documented principle.
   - Decision: **No update required.** Documentation matches implementation.

7. **epics.md / addendum.md — FR67/FR94/NFR41/NFR42/NFR42a/NFR44 requirements and the §Operating Baselines table.**
   - Proposed issue: did Epic 8's implementation diverge from the stated requirements?
   - Code comparison: the six FR67 views, seven FR94 operation classes, four-element NFR42 degraded surface, NFR41 narrowest-scope incident, and NFR44 runbook diagnostics are all implemented as specified. The §Operating Baselines table is mechanically guarded by `OperatingBaselineAddendumDriftTests`, which fails if the doc metric-name set drifts from the code catalog.
   - Decision: **No update required.** Requirements documents match the implemented behavior; doc/code alignment on the baseline table is test-enforced.

8. **src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml — "no new public HTTP shapes" claim.**
   - Proposed issue: did the observability layer add public endpoints/schemas?
   - Code comparison: `git log` shows the yaml was last modified by `feat(story-7.26)` (2026-06-03); no Epic 8 commit touched it. The generated client and client-parity tests are unchanged. All Epic 8 reads ride existing `.Contracts`/`.Server` surfaces.
   - Decision: **No update required.** The contract surface is unchanged, as claimed.

## Applied Updates

- `README.md` — added an Epic 8 summary sentence to the project overview (item 1).
- `README.md` — corrected the Epic 7 control-floor caveat: removed the false "(expected in Epic 8)" promise, cited the still-registered `AlwaysActive…`/`AlwaysUnlimited…` defaults, and pointed to Epic 8 retro action items #1–#2 (item 2).
- `README.md` — added an Epic 8 Aspire/DAPR runtime caveat paragraph: emission is always-on, but the dashboard read path, audit-checkpoint feed, alert coordinator, and weekly runbook sampler are deferred (item 3).
- `_bmad-output/planning-artifacts/architecture.md` — corrected the "deferred to Epic 8" control-state forward-reference to reflect that Epic 8 delivered the metric pipeline/dashboards but not the projection or runtime trigger (item 4).

## Discarded Updates

- `architecture.md` status-enum claim (item 5) — implementation uses the enums verbatim; no edit.
- `architecture.md` observability/SLO principle (item 6) — implementation matches; no edit.
- `epics.md` / `addendum.md` requirements + §Operating Baselines (item 7) — match implementation; baseline table is drift-test-guarded; no edit.
- `openapi/hexalith.chatbot.v1.yaml` (item 8) — unchanged by Epic 8 as claimed; no edit.

## Residual Watch Items

- When a future epic finally materializes the control-state/rate-limit read-side projection and wires the runtime triggers (Epic 8 retro action items #1–#2), remove the "inert by default" / "deferred beyond Epic 8" caveats from both `README.md` and `architecture.md:201`.
- When the dashboard read endpoint is added to `IChatBotClient` and the audit-checkpoint feed is wired, drop the Epic 8 "read path deferred" caveat from `README.md`.
- If Epic 9 adds public HTTP shapes (e.g., the S9 compliance-query or export surfaces), update `openapi/hexalith.chatbot.v1.yaml` and the README contract claim accordingly.
- The `epic-6` retrospective is still missing and its aggregate row is still `in-progress`; the README overview also skips Epic 6. Reconcile when Epic 6 is closed.
