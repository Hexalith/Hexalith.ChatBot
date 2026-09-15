---
title: Existing UX Input Reconciliation — Hexalith.ChatBot
status: complete
created: "2026-09-14"
refreshed: "2026-09-14"
result: pass
sources:
  - DESIGN.md
  - EXPERIENCE.md
  - m1-m2-surface-elaboration.md
  - epic10-chat-surface-elaboration.md
  - implementation-conformance-addendum-2026-07-17.md
---

# Existing UX Input Reconciliation — 2026-09-14

## Verdict

**Supplemental-source pass: PASS.** The current spine pair incorporates every reproducible, non-superseded UX requirement from the three declared supplemental sources. The five gaps from the prior pass—Project Workspace landing/empty behavior with S1a qualification, composer shortcut suppression, approval-fatigue observables, real live-route/server-verified acceptance, and deterministic S10 sorting—are now closed.

This pass does not make the UX pair final. Product approval and architecture-reference freshness remain separately tracked in `reconcile-product-contracts-2026-09-14.md` and `review-rubric.md`.

## Source registration

Both spines declare all three supplemental sources and every path resolves (`DESIGN.md:11-14`; `EXPERIENCE.md:10-13`). The supplements' referenced UX/product/source-proposal files also resolve. `DESIGN.md` and `EXPERIENCE.md` remain the maintained spine pair; the supplements retain their assignment/conformance role and do not override newer product authority.

## Closed gaps from the prior pass

| Prior gap | Current spine evidence | Result |
| --- | --- | --- |
| `/` landing, authorized Project picker/recents, no-project-selected, empty conversation, and S1a qualification | S1 now owns the `/` landing and authorized picker/recents (`EXPERIENCE.md:66`, `:88`). Per-surface acceptance distinguishes no-project-selected from empty-project-conversation and exposes the composer only when S1a is qualified, otherwise showing an accessible M1 availability reason (`EXPERIENCE.md:328-329`). | Closed |
| Composer single-character/modifier-free shortcut suppression | The visual component contract prohibits modifier-free shortcuts competing with entry (`DESIGN.md:127`); the behavioral contract suppresses single-character/modifier-free application shortcuts without interfering with text entry or assistive technology (`EXPERIENCE.md:123`), and S1a acceptance verifies it (`EXPERIENCE.md:329`). | Closed |
| Approval-fatigue and approval-quality observables | Operational SLO dashboard anatomy includes informational approval-load/quality observations (`DESIGN.md:152`). Behavior names approval volume, queue age, reviewer load, rejection/revision, and rubber-stamp indicators and forbids approval bypass (`EXPERIENCE.md:148`); S6, S8, and S10 acceptance apply the observations (`EXPERIENCE.md:335`, `:337`, `:339`). | Closed |
| Real live route and server-verified primary-path success | Every delivered surface must run through the real application, prove server-verified primary-path success and direct load-bearing assertions, and may not substitute static fixtures, source scans, snapshots, or handler-only tests (`EXPERIENCE.md:324`). | Closed |
| S10 deterministic sorting | Queue filter anatomy exposes the active server-side sort (`DESIGN.md:164`). Behavior specifies explicit server-side sort, deterministic tie-break, omission-safe count/order, and focus/selection preservation (`EXPERIENCE.md:160`); S10 acceptance verifies it (`EXPERIENCE.md:339`). | Closed |

## Incorporated M1/M2 surface contract

- S4 and S6-S10 are individually named in IA, composition, and per-surface acceptance (`EXPERIENCE.md:71`, `:73-77`, `:93`, `:95-99`, `:333`, `:335-339`), matching the supplemental surface map (`m1-m2-surface-elaboration.md:30-39`).
- Exact canonical state, delayed/blocked/retry/terminal behavior, reachable disabled reasons, focus, responsive behavior, English/French parity, and redaction-safe failure are defined globally and applied per surface (`EXPERIENCE.md:162-254`, `:306-340`), covering the gate and story-acceptance rules (`m1-m2-surface-elaboration.md:18-28`, `:52-62`).
- S6 exposes frozen recipients, sender authority, classifier reason, expiry, revision/cancel, send uncertainty, reconciliation, and no-resend behavior (`EXPERIENCE.md:73`, `:95`, `:170-176`, `:198`, `:292-294`, `:335`, `:438-447`).
- S7 separates operation membership from parity and retains source origin, normalized outcome, authorization, redaction, and audit attribution (`EXPERIENCE.md:266-286`, `:336`, `:414-423`).
- S8/S10 define qualified SLO/freshness and approval-load presentation, controlled refresh, bounded partition versus Project-item authority, diagnostics, filters, deterministic sorting, pagination, and no infinite-list behavior (`DESIGN.md:151-164`, `:177`; `EXPERIENCE.md:148`, `:160`, `:246-254`, `:264`, `:300-304`, `:337`, `:339`).
- S9 distinguishes canonical evidence, projections, replay, annotation, correction, and authorized redaction (`EXPERIENCE.md:149`, `:195-200`, `:216-231`, `:338`, `:425-436`).

## Incorporated Epic 10 contract

- S1 Project conversation and S1a governed composer are distinct increment-aware surfaces; S1 owns the default landing while composer availability follows M1 qualification (`EXPERIENCE.md:41-47`, `:66-67`, `:88-89`, `:328-329`).
- Governed human-message and AI-request entry use the command spine, and every six-effect AI request becomes a human proposal rather than direct execution (`DESIGN.md:127`, `:172-173`; `EXPERIENCE.md:123`, `:162-176`, `:354-364`).
- Admission/attempt identity precedes streaming; partial output, metadata-only progress, typed-state re-query, immutable retry, stable Stop/Cancel, deterministic focus return, scoped polite live status, and reduced motion are explicit (`EXPERIENCE.md:233-244`, `:306-316`, `:329`, `:354-364`). These satisfy the transport/interruption source (`epic10-chat-surface-elaboration.md:41-55`, `:63-69`).

## Incorporated implementation conformance

- Fluent UI v5 → FrontComposer → DESIGN → EXPERIENCE inheritance, no theme/control clone, exact component bases, and a reviewed no-equivalent exception are explicit (`DESIGN.md:66-76`, `:98-104`, `:116-164`).
- Every route uses one FrontComposer shell with `FcPageLayout`/`FcPageHeader`; semantic Fluent composition, one accordion owner, primary expansion, and the sole Association Review carve-out are committed (`DESIGN.md:98-104`; `EXPERIENCE.md:82-100`).
- Scoped CSS is layout/product-only and the stylesheet has a load-bearing live computed-style assertion (`DESIGN.md:76`, `:100`).
- Surface-local live-route, server success, loading/empty, validation, unauthorized/redacted, degraded, retry/terminal, keyboard, focus, responsive, forced-color, reduced-motion, and English/French acceptance is explicit (`EXPERIENCE.md:306-340`).
- DESIGN/EXPERIENCE take precedence over future conflicting visual references (`DESIGN.md:74`; `EXPERIENCE.md:31`).

## Superseded or non-gaps

- The older Association Review `escalate` wording (`implementation-conformance-addendum-2026-07-17.md:64-67`) does not authorize an invented durable transition. Current UX correctly uses source-owned Resume-only `Deferred` behavior and safe escalation as guidance.
- Broad older queue-action wording does not override current Project authority for per-item actions.
- Older `idempotency windows` wording does not replace current stable operation/successor semantics.
- Pre-update findings for S1a/FR28a-f, streaming re-query, canonical states, association thresholds, classifier failure, batch approval, correction timing, controlled refresh, connectivity, optional larger-screen handoff, diagnostics, omission parity, localization, component identities, and accordion ownership no longer reproduce.

## Disposition

There are **zero remaining supplemental-source gaps**. Keep the three supplemental files declared for traceability and story-level conformance; no further UX change is required from this supplemental pass. The spine pair remains `in-review` only for the upstream product-approval/finalized-architecture inheritance blocker.
