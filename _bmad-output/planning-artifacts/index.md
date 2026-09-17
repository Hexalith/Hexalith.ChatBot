# Chatbot Planning Artifact Index

This index exists so readiness and planning workflows can discover nested PRD and UX artifacts without relying only on top-level `*.md` patterns.

## Primary Artifacts

- PRD: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics and stories: `_bmad-output/planning-artifacts/epics.md`
- Technical enablers: `_bmad-output/planning-artifacts/technical-enablers.md`
- Story-evidence gate proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md`
- Historical story evidence crosswalk: `_bmad-output/planning-artifacts/historical-story-evidence-crosswalk-2026-09-17.md`

## UX Artifacts

- Design spine: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- Experience spine: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- M1/M2 surface elaboration: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`
- Epic 10 chat surface elaboration: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md`

## Current Readiness Notes

- `epics.md` contains 13 canonical product epics and 146 assignable product stories: 11, 13, 13, 13, 10, 10, 12, 10, 11, 10, 10, 15, and 8 stories respectively. TE-1 and TE-2 are tracked in `technical-enablers.md` and excluded from product counts.
- Epic 13 owns the complete governed interactive workspace and UI-conformance outcome and must close before MVP readiness sign-off.
- Canonical Story 9.1 owns durable runtime control activation; Stories 9.2–9.6 own the user-value controls. Legacy Stories 8.7a/8.7b remain historical implementation evidence only.
- DomainService SDK host adoption is tracked outside the product hierarchy as TE-1 and is recorded complete in `technical-enablers.md`; it is not canonical Epic 11. Canonical Epic 11 owns operational dashboards and observability.
- AI-response streaming transport is resolved by `docs/adrs/ai-response-streaming-transport.md` and owned with the live interaction contract by canonical Story 13.2; legacy Stories 10.6a/10.6b remain historical evidence.
- Canonical Story 12.14 owns live compliance investigation and data-rights operations; canonical Story 12.15 owns exact-candidate recovery qualification without evidence substitution. Story 12.16 is not part of the active hierarchy. Superseded implementation files remain historical evidence through the crosswalk, not assignable backlog entries.
- TE-2 governs prospective `done` transitions. Its repository implementation is complete, but the technical-enabler record remains in review until the required protected check is active.
