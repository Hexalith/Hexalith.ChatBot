
## Session 2026-06-03T19:46:45Z — Epic 9 completion (resume)
- Resumed at story 9.5; processed 9.5→9.13 (9 stories) create→dev→auto→review→commit, all single-cycle reviews (0 retries).
- Sprint-status was authoritative when the output parser couldn't find a cleaned-up log file (dev 9.5).
- Submodule guard: forward-only pointer bumps appeared on 9.5 (Conversations/EventStore), 9.9 (Conversations/Tenants), 9.10 (FrontComposer) — all descendants, committed safely; no backward restores.
- sprint-compare helper false-positives on slug vs dotted IDs (reported all earlier stories 'incomplete'); verified against sprint-status.yaml directly.

## Run: 2026-06-19T16:57:01Z

**Epic:** Hexalith.ChatBot - Epic Breakdown
**Stories:** 122 queued stories, resumed at 11.4 and completed 11.5-11.6 plus Epic 11 retrospective

### Patterns Observed
- Source-of-truth verification remained essential: several Codex/Claude sessions completed work but parked at a prompt, so direct `verify-step` / `verify-code-review` and sprint-status checks were more reliable than monitor output.
- High-complexity platform stories needed one review-to-dev feedback loop when review uncovered an architectural topology defect.
- Retrospective doc verification updated current-state planning docs and ADRs; historical dated evidence should remain unchanged.

### Code Review Insights
- Common issues: File List omissions, story-record transparency gaps, safety-critical rationale comments lost during relocation, and topology behavior not covered by real round-trip tests.
- Story 11.5 required a second dev pass after review found a CRITICAL double-admission defect; the final fix used a non-forgeable DataProtection admission marker and round-trip coverage.
- Story 11.6 review confirmed the retained AppHost shim is an explicit ADR-scoped exception until platform composition can express ChatBot's dedicated Dapr resources.
- Average cycles to clean for the resumed tail: 11.5 needed 2 review cycles; 11.6 needed 1 review cycle.

### Timing Estimates
- create-story: ~6 minutes for high-complexity Epic 11 stories.
- dev-story: ~15-25 minutes for high-complexity host/composition changes.
- code-review: ~7-12 minutes per cycle when review reruns build and focused suites.

### Recommendations for Future Runs
- Treat parked prompt sessions with verified sprint-status as successful after cleanup; avoid retrying solely because monitor output is `not_found`.
- Strengthen story templates/checklists to require File List updates for every test or doc touched by automation/review.
- Add a platform follow-up for an EventStore composition extension that can express dedicated Dapr resources without a module-owned Aspire package.
- Keep DataProtection key-ring persistence explicit in production readiness checks for admission markers and cursor codecs.

## Run: 2026-06-21T20:29:13Z

**Epic:** Hexalith.ChatBot - Epic Breakdown
**Stories:** 10.6a, 10.6b, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 12.9

### Patterns Observed
- Source-of-truth verification remained necessary because multiple child sessions completed but parked at an interactive prompt; sprint-status and story files were more reliable than monitor termination alone.
- Fluent custom-element stories need live Chromium verification when Chrome is available; string/source fallbacks repeatedly hid or misstated real browser behavior.
- The guard-first approach worked: raw-control and CSS-primitive backlogs were measured, burned down, and kept empty by build-blocking tests.

### Code Review Insights
- Common issues: inaccurate browser evidence, File List omissions, hidden/touch-target regressions after CSS retirement, and tests that were too permissive around labels or browser fallback paths.
- Average review cycles to clean: 14 total cycles across 11 stories; most stories completed in one review cycle, while early 10.6b and browser-sensitive Epic 12 stories needed extra scrutiny.

### Timing Estimates
- create-story: ~6-8 minutes for Epic 12 UI stories.
- dev-story: ~10-20 minutes for focused UI/test changes; longer when full E2E/browser validation was required.
- code-review: ~8-18 minutes per cycle when review rebuilt and reran full UI/E2E lanes.

### Recommendations for Future Runs
- Require every Fluent UI story to record whether the browser path actually ran, and verify the harness launch flags rather than a separate smoke command.
- Keep `ChatBotFluentConformanceTests` empty-backlog checks as release gates; do not replace them with informal review notes.
- When automation updates generic summaries such as `tests/test-summary.md`, require the story File List to include the file or have review explicitly revert it.

## Run: 2026-06-22T22:55:23Z

**Epic:** Hexalith.ChatBot - Epic Breakdown
**Stories:** 13.1-13.9

### Patterns Observed
- Direct source-of-truth checks remained more reliable than monitor output: multiple Claude sessions completed work and parked at an interactive prompt, so sprint-status/story files were authoritative.
- Create-story sessions repeatedly plateaued; manual/fallback story creation was needed for 13.9 after repeated non-output attempts.
- Real-render verification was decisive: source scans and static fixtures stayed green while the actual app initially rendered with missing scoped CSS.

### Code Review Insights
- Common issues: overly coarse DOM assertions, missing visual inspection, and evidence that did not prove the load-bearing CSS/layout invariant.
- Story 13.9 required review auto-fix: `App.razor` now links `Hexalith.ChatBot.UI.styles.css`, and the E2E lane asserts `.fluent-layout` computes to `display:grid`.
- Average cycles to clean: one final review cycle per completed story, with 13.9 requiring the most scrutiny because review found and fixed a production render defect.

### Timing Estimates
- create-story: ~5-15 minutes when the artifact was produced normally; plateau cases can exceed that and should be cut over to fallback earlier.
- dev-story: ~10-30 minutes for focused UI composition stories; longer for real-browser matrix work.
- code-review: ~8-25 minutes per cycle when review reruns live browser suites and inspects screenshots.

### Recommendations for Future Runs
- For UI composition stories, require at least one screenshot visual inspection plus a CSS-composition invariant, not just DOM presence checks.
- Treat scoped CSS bundle links as a release-critical app-shell requirement when adopting FrontComposer/Fluent RCLs.
- Keep story evidence and README/architecture docs synchronized after final epic retrospectives, especially when review discovers root-cause production fixes.
