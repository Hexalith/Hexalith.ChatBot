# Epic 10 — Interactive Chat Surface & Project Workspace UX Elaboration

> Created by `sprint-change-proposal-2026-06-09.md` (approved). Brings the governed chat
> composer and the Project Workspace landing surface to assignment-ready detail, matching the
> bar set by `m1-m2-surface-elaboration.md`. Visual chain is binding: Fluent UI v5 →
> Hexalith.FrontComposer → DESIGN.md → EXPERIENCE.md. No new design system; the product reads
> as a quiet operational SaaS command workspace, not a playful assistant/consumer messaging app
> (UX-DR1).

## Scope

Covers the surfaces introduced/changed by Epic 10 stories 10.4–10.6:

- **Project Workspace landing** (UX-DR5) — the default route `/`.
- **Governed chat composer** (UX-DR16, UX-DR17) — message + AI-request entry.
- **Streaming AI response + Stop/Cancel** (UX-DR32) — progressive render and interruption.

Governing principle: the composer is a **governed write surface on the CommandGateway spine**.
There is no fake/freeform textbox; a risky request becomes an Epic 4 AI-action proposal
(approval-required) rather than a direct execution. An approved AI message lands via
`Project.AppendConversationMessage`.

## Surface map (PRD/UX → Epic 10)

| Surface | PRD / UX anchor | Epic 10 story | Required elaboration |
| --- | --- | --- | --- |
| Project Workspace landing | UX-DR5; PRD S1 region | 10.4 | Project picker/recents (no marketing hero); selected-project conversation + context + files; persistent shell navigation. |
| Governed chat composer | UX-DR16, UX-DR17, UX-DR34 | 10.5 | User-message vs ask-AI entry; risky → proposal; CommandGateway admission; in-entry single-key-shortcut suppression; EN+FR. |
| Streaming AI response | UX-DR32 | 10.6 | Progressive render; always-reachable Stop/Cancel in a stable focus position; "Response stopped" live-region announce; reduced-motion. |

## Project Workspace states (UX-DR5)

- **Cold load** — skeleton/loading without layout shift; no hero.
- **No project selected** — project picker + recents; entry to project switcher/deep link.
- **Empty project conversation** — explains there is no activity yet; composer available.
- **Active conversation** — read projection stream + composer; context + files panel.
- **Dependency degraded** — non-blocking banner; composer disabled or queued with a reason; no silent failure.
- **Unauthorized / redacted** — redaction-safe; no leakage; escalation path where applicable.
- **Project-switch success** — focus and context update announced.

## Composer states & behavior (UX-DR16, UX-DR17, UX-DR34)

- **Empty / idle** — placeholder communicates governed intent (not "chat with a bot").
- **Composing** — text entry; single-character/modifier-free shortcuts disabled inside the entry (UX-DR34); EN+FR.
- **Submitting a user message** — admitted through CommandGateway; optimistic state only after admission, never before.
- **Ask-AI request** — distinct affordance; a request implying a risky action produces a proposal, surfaced in the S3 AI-action approval flow (Epic 4), not executed inline.
- **Unauthorized** — composer disabled with a stable reason code; no bypass affordance.
- **Degraded** — composer queued/disabled with a recoverable, visible state.

## Streaming & interruption (UX-DR32)

- AI proposal/response renders **progressively**.
- **Stop/Cancel** control is **always keyboard-reachable** in a **stable focusable position** — no inline appear/disappear that steals focus.
- On activation: announce **"Response stopped"** politely via a live region; return focus to the composer or the AI proposal panel.
- **Reduced-motion** respected; no motion-only status.

## Accessibility & localization (inherited floor)

- WCAG 2.2 AA in light/dark/forced-colors; status survives forced-colors via icon/text/border, not fill.
- EN + FR parity for all new strings (voice/tone per Story 1.7 catalog).
- Live-region parity for streaming and stop/cancel; focus management on project switch, send, and proposal surfacing.

## Open dependency

- **AI-response streaming transport** is an open architecture decision (see `architecture.md`
  §Frontend Architecture) — SignalR projection-nudge vs a dedicated streaming channel. The UX
  contract (progressive render + interruptible Stop/Cancel) holds regardless; the transport must
  preserve the "never trust payload" / fail-closed posture. Resolve before Story 10.6.
