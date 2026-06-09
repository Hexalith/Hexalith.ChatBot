# Chatbot Planning Artifact Index

This index exists so readiness and planning workflows can discover nested PRD and UX artifacts without relying only on top-level `*.md` patterns.

## Primary Artifacts

- PRD: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics and stories: `_bmad-output/planning-artifacts/epics.md`

## UX Artifacts

- Design spine: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- Experience spine: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- M1/M2 surface elaboration: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`
- Epic 10 chat surface elaboration: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md`

## Current Readiness Notes

- `epics.md` is aligned to 11 epics and 128 assignable stories as of 2026-06-09 (readiness-blockers pass added Story 8.7 and split Story 10.6 → 10.6a/10.6b; host-reuse pass added Epic 11 and split Story 8.7 → 8.7a/8.7b).
- Epic 10 is part of the M2 release-readiness sequence and must close before MVP readiness sign-off.
- Stories 8.7a/8.7b (control-plane runtime activation) own FR74/FR75 runtime enforcement materialization; the Epic 7 control floor is wired but inert until they land (readiness CR-2).
- Epic 11 (DomainService SDK host adoption, architecture D8) closes readiness pass-2 Issue #1 and must close before MVP readiness sign-off; Story 11.1 (ADR → `docs/adrs/domainservice-sdk-host-adoption.md`) gates Stories 11.2–11.6, and Stories 11.5/11.6 land after 8.7a/8.7b. See `sprint-change-proposal-2026-06-09-host-reuse.md` + `implementation-readiness-report-2026-06-09-pass-2.md`.
- Story 10.6a (streaming transport ADR → `docs/adrs/ai-response-streaming-transport.md`) must be accepted before Story 10.6b is assigned (readiness CR-1).
