---
title: Sprint Change Proposal - Chatbot UI via FrontComposer Shell & Governed Chat Surface
project: Chatbot
date: 2026-06-09
status: approved
mode: Batch
trigger: "chatbot-ui (http://localhost:5000/) does not contain a chatbot UI; it should use Hexalith.FrontComposer + Blazor Fluent UI v5"
scope_classification: Major
recommended_approach: Direct Adjustment (Hybrid - new epic + PRD positioning update)
owner: Jerome
prepared_at: 2026-06-09T17:21:56+02:00
approved_by: Jerome
approved_at: 2026-06-09T17:25:14+02:00
implementation_state: planning-artifacts-applied
---

# Sprint Change Proposal - Chatbot UI via FrontComposer Shell & Governed Chat Surface

## 1. Issue Summary

**Trigger (Jerome, 2026-06-09):** The web UI served at `http://localhost:5000/` (`Hexalith.ChatBot.UI`) does not present a chatbot UI. It should be a chatbot UI built on **Hexalith.FrontComposer** with **Blazor Fluent UI v5** components.

**Issue type:** Implementation drift from a documented architecture + a deferred-scope decision that was never followed up — surfaced during manual review of the running app. This is a planning correction plus a deliberate scope addition, **not** a code rollback.

### What the running site actually is (evidence from code)

| Expectation | Reality |
| --- | --- |
| Landing page is a chat/conversation surface | `/` routes to `Components/Pages/GovernedOperations.razor` — an operational-queue dashboard. |
| Built **via Hexalith.FrontComposer** | **FrontComposer is not referenced.** `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj` has no ProjectReference/PackageReference to FrontComposer; `Program.cs` calls neither `AddHexalithFrontComposerQuickstart()` nor `AddHexalithDomain<>()`; no `<FrontComposerShell>` in any layout. |
| Built on **Fluent UI v5** | ✅ Already true — `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1`, `AddFluentUIComponents()` wired, `<FluentButton>`/`<FluentBadge>` in use. |
| A conversational "chatbot" experience | The implemented surfaces are **read-only governed projections** (S1 conversation stream, S2 association review, S3 approval, S8 dashboards, S9 audit). There is **no composer** — no message input, send, AI-reply, or streaming surface. |

### Why this happened (blameless root cause)

1. **FrontComposer Shell adoption was explicitly deferred by Story 1.14 and the follow-up story was never created.** Story 1.14's spec (`implementation-artifacts/1-14-...md:19,45,118`) gave the developer an explicit escape hatch: *"either wrap the app in the FrontComposer shell … or create a thin ChatBot-owned token alias layer … documented as temporary until the shell wrapper lands"* and *"leave the runtime shell swap to a later, explicit story."* The developer correctly took the token-alias path (Story 1.14 met its acceptance criteria), but **the promised "later, explicit story" for the shell swap was never added to the epics or sprint plan.** That is the precise gap.

2. **The conversational chat surface was deliberately deferred out of M0/M1.** The architecture (`architecture.md:381-382`) states the conversation view is *"a read projection a future chat surface can write into via the same CommandGateway — chat becomes a new surface on the spine, not a new subsystem (no fake chat textbox)."* The epics (`epics.md:590`) and the PRD `[NOTE FOR PM]` record that *"M0 is a project-conversation view plus review/approval surfaces — not a native chat surface."* PRD adversarial review (`review-adversarial-general.md:234`) flagged this exact mismatch: *"Either rename the product or make the chat surface a first-class MVP concern."* The "ChatBot" name has always pointed at this **vision-state interactive surface**, which the UX already specifies (UX-DR16/17 composer, UX-DR32 streaming Stop/Cancel) but which was never scheduled.

**In short:** the architecture *mandated* FrontComposer and *anticipated* a governed chat surface; the implementation shipped a temporary bridge plus read-only surfaces and the two follow-up commitments fell through the cracks. This proposal schedules them.

## 2. Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | Trigger is manual review of the running app, not a failed story. Affected records: Story 1.14 (deferred shell swap), Epics 3/4 (read-only surfaces), PRD `[NOTE FOR PM]` positioning. |
| 1.2 Core problem | [x] Done | (a) FrontComposer Shell never integrated (deferred, no follow-up story); (b) governed chat composer surface deferred out of MVP and never scheduled. |
| 1.3 Evidence | [x] Done | `Hexalith.ChatBot.UI.csproj`, `Program.cs`, `1-14-...md`, `architecture.md:280,378-382`, `epics.md:308,354-355,590,996-1003`, PRD adversarial reviews. |
| 2.1 Current epic impact | [x] Done | Epics 1-9 are `done`; none reopened. The work is additive (new Epic 10). Epic 8 stays `in-progress` (8.6 backlog). |
| 2.2 Epic-level changes | [x] Done | Add **Epic 10** (FrontComposer Shell adoption + governed chat surface). No existing epic redefined. |
| 2.3 Remaining epics | [x] Done | Epic 8 (8.6 Dapr Workflow binding) is independent. No resequencing of M0. |
| 2.4 Future epic validity | [x] Done | No epic made obsolete. One new epic needed to close the FrontComposer + chat-surface gap. |
| 2.5 Priority/order | [x] Done | Epic 10 sequences after the M0 spine (already done). Internal order: shell adoption → surface migration → composer → streaming → a11y re-verify. |
| 3.1 PRD conflicts | [!] Action-needed | MVP positioning (`[NOTE FOR PM]`: "no conversational chat surface in M0/M1") is **reversed** by pulling the governed chat surface into scope. PM sign-off required. |
| 3.2 Architecture conflicts | [!] Action-needed | Frontend section must record Shell adoption (was "deferred") and the governed chat write-surface; a streaming-transport decision (SignalR nudge vs streaming channel) needs an ADR. No conflict with the safety spine. |
| 3.3 UI/UX conflicts | [!] Action-needed | Composer states (UX-DR16/17/32) and Project Workspace landing (UX-DR5) need surface elaboration; the chat surface is not covered by the existing M1/M2 elaboration gate artifact. |
| 3.4 Other artifacts | [!] Action-needed | bUnit + Verify snapshots + Playwright a11y/visual e2e need updates; `sprint-status.yaml` adds Epic 10 after approval. AppHost topology unchanged. |
| 4.1 Direct Adjustment | [x] Viable | Add Epic 10 + stories within the existing structure; update PRD positioning. Effort High; risk Medium. |
| 4.2 Rollback | [N/A] Skip | Nothing to roll back; read-only surfaces and the token bridge are reused, not reverted. |
| 4.3 MVP Review | [x] Partial | This is an MVP-shape change (chat surface promoted), not a reduction. PM owns the positioning update. |
| 4.4 Recommendation | [x] Done | Direct Adjustment (Hybrid): new Epic 10 + PRD positioning update. |
| 5.1-5.5 Proposal components | [x] Done | This document. |
| 6.1-6.2 Final review | [x] Done | Internally consistent with architecture, epics, UX, PRD. |
| 6.3 User approval | [!] Action-needed | Pending Jerome approval. |
| 6.4 Sprint-status update | [!] Action-needed | Apply only after approval. |
| 6.5 Handoff plan | [x] Done | PM (positioning), Architect (shell boundary + streaming ADR), PO/Dev (backlog), Dev (build). |

## 3. Impact Analysis

### Epic Impact

| Epic | Impact | Required change |
| --- | --- | --- |
| Epics 1-9 | None reopened. Read-only surfaces (Epic 3) and the Story 1.14 token bridge are **reused** by the migration. | No edits; Story 1.14 gains a forward cross-reference to Story 10.1 (its deferred shell swap). |
| Epic 8 | Independent; remains `in-progress` (8.6 backlog). | None. |
| **Epic 10 (new)** | Owns FrontComposer Shell adoption + governed chat surface + Project Workspace landing. | Create epic + 7 stories (below). |

### Artifact Impact

- **PRD** (`prds/prd-Hexalith.ChatBot-2026-05-28/`): `[NOTE FOR PM]` and MVP-increment positioning updated — the interactive chat surface moves from "vision-state, deferred" to "in-scope governed surface." Optionally add a surface/FR entry so Epic 10 traces cleanly (PM decision). This resolves the long-standing naming-vs-scope finding (`review-adversarial-general.md:234`).
- **Architecture** (`architecture.md` §Frontend Architecture, §Internal Decomposition): record Shell adoption as done-going-forward; restate the "no fake chat textbox" rule as **"the chat surface is the governed write surface — every message is admitted through CommandGateway; a risky request becomes a proposal, never a direct execution"**; add an open decision/ADR for the AI-response **streaming transport** (current spine has SignalR projection-nudge only).
- **UX** (`ux-designs/ux-Hexalith.ChatBot-2026-05-28/`): add composer / Project Workspace surface elaboration (states, proposal-on-risky flow, Stop/Cancel focus position, EN+FR, forced-colors) — UX-DR5/16/17/32 exist but are not yet elaborated to assignment-ready detail.
- **Tests:** bUnit component tests, Verify `.verified` snapshots, and the Playwright a11y/visual e2e (`tests/Hexalith.ChatBot.UI.E2E.Tests`) update as surfaces re-render through the shell.
- **sprint-status.yaml:** add `epic-10: backlog` + the seven `10-*` stories as `backlog` after approval.

### Technical Impact & De-risking

- **FluentUI pin already aligned:** ChatBot and FrontComposer both pin `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` (`Directory.Packages.props:41` / `Hexalith.FrontComposer/Directory.Packages.props:46`). The version-churn risk Story 1.14 hedged against does not exist — Shell adoption is clear.
- **Read-only submodule constraint:** `Hexalith.FrontComposer` is a git submodule. Per project rules, it must be consumed **read-only** (ProjectReference to `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`); do not edit submodule files.
- **Dependency direction preserved:** the UI adapter may reference Client, ServiceDefaults, and FrontComposer Shell/Contracts only — never Server, gateway internals, DAPR clients, or audit/idempotency interfaces. Architecture fitness tests must keep this boundary non-vacuous.
- **Single `<FluentProviders />`:** `FrontComposerShell` already renders it; remove the duplicate from `App.razor` when the shell lands.
- **Governance reuse:** the composer reuses the existing spine — CommandGateway admission, Epic 4 risk classifier + proposal/approval, and the M0 allowlisted `Project.AppendConversationMessage` write. Little new backend surface; the new work is UI + a thin command-submit path.
- **Open architecture question (Architect):** AI-response streaming (UX-DR32) may need a streaming channel beyond SignalR projection-nudge — decide and record in an ADR before Story 10.6.

## 4. Recommended Approach

**Selected: Direct Adjustment (Hybrid).** Add a new Epic 10 within the existing structure and update PRD MVP positioning. No rollback; no scope reduction.

**Scope classification: Major** — because Stories 10.5/10.6 reverse a PRD-level deferral and change the MVP positioning that was explicitly flagged for the PM. The FrontComposer-shell half (10.1-10.4, 10.7) is Moderate/conformance; the chat-surface half is Major. The combined change is therefore routed as Major (PM + Architect sign-off).

**Rationale:** The architecture already mandated FrontComposer and anticipated a governed chat surface "on the spine," so we are closing documented gaps rather than inventing direction. Reusing the read-only projections + token bridge + Epic 4 governance keeps risk Medium. The aligned FluentUI pin removes the main integration risk.

**Effort: High. Risk: Medium.** Risks: submodule read-only discipline, dependency-direction drift, streaming-transport unknown, and snapshot/e2e churn during surface migration.

## 5. Detailed Change Proposals

### 5.1 Add Epic 10 to `epics.md`

NEW (after Epic 9):

```text
## Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption

Goal: Deliver the "ChatBot" vision-state interactive surface as a governed write
surface on the existing CommandGateway spine, and adopt the Hexalith.FrontComposer
Shell as the UI composition layer (closing the Story 1.14 deferred shell swap).
Preserves the safety model: no fake/freeform textbox — every message is admitted
through CommandGateway; risky requests become Epic 4 proposals. Inherits the
Fluent UI v5 -> FrontComposer -> DESIGN.md visual chain (no new design system).

### Story 10.1: FrontComposer Shell integration (closes Story 1.14 deferred shell swap)
As a frontend engineer, I want Hexalith.ChatBot.UI wired to the FrontComposer Shell,
so the UI composes through the mandated FrontComposer layer instead of the temporary token bridge.
Acceptance Criteria:
- ProjectReference to Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell (read-only submodule); no Server/gateway/DAPR refs introduced.
- Program.cs wires AddHexalithFrontComposerQuickstart() -> AddHexalithDomain<TMarker>() -> EventStore client swap, per the FrontComposer startup order.
- App layout reduces to <FrontComposerShell>@Body</FrontComposerShell>; exactly one <FluentProviders /> in the tree.
- The Story 1.14 thin token-alias layer is retired or reconciled against the shell's --fc-color-* tokens with no duplicate/raw-hex mappings.
- Architecture fitness tests updated so the UI adapter boundary stays non-vacuous (UI excludes Server/gateway internals).
- Build is Release-clean (TreatWarningsAsErrors) and the default test lane is green.

### Story 10.2: Migrate M0 governed surfaces (S1/S2/S3) onto the shell
As a frontend engineer, I want S1 conversation, S2 association review, and S3 AI approval rendered through the FrontComposer shell,
so existing governed surfaces use the mandated composition layer without behavioral regression.
Acceptance Criteria:
- ChatBotConversationStream + item components, AssociationReview, and the AI-approval surface render within the shell, preserving governed semantics, semantic tokens, a11y labels, and non-color status.
- bUnit + Verify snapshots updated intentionally; e2e a11y/visual green.
- No change to read-projection semantics ("not a chat transcript" preserved for read views).

### Story 10.3: Migrate operational surfaces (S8 dashboards, S9 audit, S10 admin queues) onto the shell
Acceptance Criteria:
- OperationalDashboards, ComplianceAuditInvestigation, and admin queue surfaces render through the shell with stable filters and degraded-dependency states intact.
- Snapshots/e2e updated; WCAG 2.2 AA preserved.

### Story 10.4: Project Workspace landing route (UX-DR5)
As a user, I want the app to open on the Project Workspace, so the landing experience is the project-centered conversation, not the ops queue.
Acceptance Criteria:
- "/" routes to the Project Workspace (project picker/recents when none selected; selected-project conversation + context + files when selected); GovernedOperations moves to its own route.
- Persistent shell navigation; cold-load, no-project, empty-project, and unauthorized/redacted states per UX-DR5.

### Story 10.5: Governed chat composer (UX-DR16/17)
As a user, I want a composer to send messages and AI requests in the Project Workspace, so I can interact conversationally while every write stays governed.
Acceptance Criteria:
- Composer accepts a user message and an "ask AI" request; submission is admitted through CommandGateway (no direct write, no fake textbox).
- A request implying a risky action creates an Epic 4 AI-action proposal (approval-required) instead of executing; approved AI messages land via Project.AppendConversationMessage.
- Empty/cold/active/unauthorized/degraded states; character-key-shortcut suppression inside the text entry (UX-DR34); EN+FR.

### Story 10.6: Streaming AI response + Stop/Cancel (UX-DR32)
Acceptance Criteria:
- AI proposal/response renders progressively; a Stop/Cancel control is always keyboard-reachable in a stable focus position (no focus-stealing appear/disappear).
- Activation announces "Response stopped" politely (live region) and returns focus to the composer or proposal panel; reduced-motion respected.
- Streaming transport conforms to the architecture decision recorded for this story (SignalR nudge vs streaming channel).

### Story 10.7: Cross-surface a11y / visual / parity re-verification
Acceptance Criteria:
- WCAG 2.2 AA across all shell-composed surfaces (light/dark/forced-colors); EN+FR localization intact.
- Playwright a11y/visual e2e gate green for the migrated and new surfaces.
- CLI/MCP parity unaffected (composer is a UI surface over the same spine; backend state transitions unchanged).
```

### 5.2 Update PRD MVP positioning (PM)

Artifact: `prds/prd-Hexalith.ChatBot-2026-05-28/` (`[NOTE FOR PM]` + MVP increment section).

OLD (intent): "The 'ChatBot' name reflects the vision-state interactive surface, not the M0/M1 MVP shape … there is no conversational chat surface."

NEW (intent): "The interactive chat surface is now in scope as a **governed write surface** delivered in Epic 10. Every message is admitted through CommandGateway; risky requests become AI-action proposals. This resolves the prior naming-vs-scope gap (`review-adversarial-general.md:234`). It does not weaken the safety floor and does not introduce a freeform/ungoverned textbox."

PM decision: whether to add an explicit surface entry (e.g. extend S1 Project Workspace, or a new surface id) and/or an FR for the conversational composer for clean Epic-10 traceability.

### 5.3 Update Architecture (Architect)

Artifact: `architecture.md` §Frontend Architecture, §Internal Decomposition.

- Frontend stack note: FrontComposer Shell is **adopted** in `Hexalith.ChatBot.UI` (Story 10.1), no longer a temporary token bridge.
- Restate `architecture.md:382`: "(no fake chat textbox)" → "the chat surface is the governed write surface; messages are admitted through CommandGateway and risky requests become proposals — no freeform textbox that bypasses governance."
- Add an open decision / ADR: **AI-response streaming transport** for UX-DR32 (SignalR projection-nudge vs a streaming channel), resolved before Story 10.6.

### 5.4 Add composer / Project Workspace UX elaboration (UX)

Artifact: `ux-designs/ux-Hexalith.ChatBot-2026-05-28/` (new addendum or extend EXPERIENCE.md).

- Composer states (UX-DR16/17), proposal-on-risky flow, Project Workspace landing states (UX-DR5), Stop/Cancel focus position + announcement (UX-DR32), keyboard-shortcut suppression in text entry (UX-DR34), EN+FR, forced-colors. Bring the chat surface to the same assignment-ready bar as the M1/M2 elaboration gate.

### 5.5 sprint-status.yaml (after approval)

Add:

```yaml
  epic-10: backlog
  10-1-frontcomposer-shell-integration: backlog
  10-2-migrate-m0-governed-surfaces-onto-shell: backlog
  10-3-migrate-operational-surfaces-onto-shell: backlog
  10-4-project-workspace-landing-route: backlog
  10-5-governed-chat-composer: backlog
  10-6-streaming-ai-response-and-stop-cancel: backlog
  10-7-cross-surface-a11y-visual-parity-reverification: backlog
```

Story 1.14 keeps its `done` status (it met its escape-hatch AC); add a comment noting its deferred shell swap is now owned by Story 10.1.

## 6. Implementation Handoff

**Scope classification: Major.**

| Recipient | Responsibility |
| --- | --- |
| Product Manager (`bmad-prd`) | Update `[NOTE FOR PM]` + MVP positioning; decide on a composer surface/FR entry; resolve the naming-vs-scope finding. |
| Architect (`bmad-create-architecture`) | Confirm Shell-adoption boundary (UI must not reference Server); author the streaming-transport ADR; restate the chat-on-spine rule. |
| Product Owner / Dev (`bmad-create-epics-and-stories` / `bmad-create-story`) | Land Epic 10 + 7 stories in `epics.md`; update `sprint-status.yaml`; cross-reference Story 1.14. |
| Developer (`bmad-dev-story`) | Implement Epic 10 in dependency order (10.1 → 10.2/10.3 → 10.4 → 10.5 → 10.6 → 10.7), each with `bmad-code-review` before Done. |

**Success criteria:**

- `Hexalith.ChatBot.UI` references and composes through `FrontComposerShell`; UI adapter boundary tests stay green.
- `/` opens on the Project Workspace with a governed composer; sending a message routes through CommandGateway; risky requests become proposals; approved AI messages append via `Project.AppendConversationMessage`.
- Streaming responses have a keyboard-reachable Stop/Cancel; WCAG 2.2 AA + EN/FR hold across all surfaces; a11y/visual e2e green.
- PRD positioning and architecture reflect the governed chat surface; no fake/ungoverned textbox exists.
- Epics 1-9 implementation evidence is untouched; `sprint-status.yaml` reflects Epic 10 as `backlog`.

## 7. Approval and Routing

Approval status: **pending Jerome approval.**

On approval, the following are applied as planning edits (mirroring the 2026-06-05 proposal pattern): `epics.md` (Epic 10), `architecture.md` (Frontend note + streaming ADR stub), PRD positioning note, UX composer elaboration, and `sprint-status.yaml` (Epic 10 backlog). Implementation then proceeds story-by-story via `bmad-dev-story` + `bmad-code-review`.
