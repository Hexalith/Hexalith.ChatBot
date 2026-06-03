# Test Automation Summary — Story 8.1 (Operational dashboards, S8/S10)

**Date:** 2026-06-03
**Author:** QA automation (bmad-qa-generate-e2e-tests)
**Framework:** xUnit v3 + Shouldly (server/contracts/UI), Fluxor reducer/service + string-contract tests (UI), Microsoft.Playwright (E2E a11y). Compiled in-process runners (`-parallel none`) per the story's testing notes — `dotnet test`/VSTest is avoided (sandbox `SocketException` risk).

## Scope

Story 8.1 was already implemented (status: `review`) with tests across all five layers. This run audited test coverage against the 10 acceptance criteria, then **auto-applied the discovered gaps** (tests only — no production/behaviour code changed).

## Existing coverage (verified passing)

| Layer | File | ACs |
| --- | --- | --- |
| Contracts | `OperationalDashboardContractTests.cs` | AC1/2/3/9/10 — wire-token round-trip, bounded-staleness classification, finite-token validation, full FR67 view coverage, status-as-enum, metadata-only serialization |
| Server | `OperationalDashboardProjectorTests.cs` | AC1/2/5/6/7/8/10 — six views + audit lag, worst-health enum (never count-derived), empty→Unknown, metadata-only redaction, audit-lag fail-safe, see-only allow / service+AI deny / fail-closed |
| UI | `OperationalDashboardsComponentContractTests.cs` | AC1–6 — governed primitives, non-color status, freshness, reachable detail, reflow, localization, no restricted markers |
| UI | `OperationalDashboardsReducersTests.cs` | load / loaded / failed reducer transitions |
| UI | `OperationalDashboardServiceTests.cs` | AC1/2/3/10 — `IChatBotClient` seam, `ui` surface origin, contract-valid metadata-only overview with fresh/stale/expired |
| E2E | `OperationalDashboardsAccessibilityE2ETests.cs` | AC4/10 — landmarks, keyboard rows, `aria-live` freshness dedup, non-color status (Playwright with source-contract fallback) |
| UI (pre-existing) | `ChatBotLocalizationContractTests.SharedResourcesShouldHaveCompleteEnglishAndFrenchCoverage` | AC4/10 — auto-covers every new `ChatBotUiTextKey` in EN+FR |

## Gaps discovered and auto-applied

Added to `tests/Hexalith.ChatBot.Server.Tests/Projections/OperationalDashboardProjectorTests.cs`:

1. **AC5 — `ReadPolicyShouldAllowEverySeeOnlyAdminRoleWithoutPerProjectMembership`** (Theory, 5 roles). The suite only proved `operations-admin` allow; AC5 names multiple see-only admin roles. Now every admin role (`tenant-admin`, `mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) is proven to read tenant-wide summaries without per-project membership.
2. **AC7 — `ReadPolicyShouldDenyHumanCallerWithoutAnAdminSeeOnlyScope`**. Deny was only proven for `service`/`ai` actor types. This covers the "callers without an admin see-only scope" branch: a human with a non-admin role and a human with no role claim are both denied with `authorization_denied` before state load.
3. **AC6 — `DashboardDetailLinksShouldNeverOpenRestrictedDetailAndAlwaysCarryASafeReason`**. No focused assertion existed that the dashboard never exposes an openable (`available`) detail link. Now proven: no view is `available`; queue views are `request-access` + `insufficient-authority`; aggregate views (AI outcomes, audit lag) are `open-detail-disabled` + `state-not-permitted`; every view carries ≥1 safe reason code (no resource-existence leakage).

## Coverage

- Acceptance criteria with automated coverage: **10/10**.
- Authorization matrix (AC5/AC7): allow paths 5/5 see-only roles; deny paths now include human-without-scope + no-role-claim (previously only service/AI).
- Detail-link redaction safe-states (AC6): all 6 views asserted.

## Results

| Suite | Total | Failed |
| --- | --- | --- |
| `Hexalith.ChatBot.Server.Tests` | 951 (+7) | 0 |
| `Hexalith.ChatBot.Contracts.Tests` | 279 | 0 |
| `Hexalith.ChatBot.UI.Tests` | 118 | 0 |

Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → succeeded, 0 warnings, 0 errors.

E2E (`Hexalith.ChatBot.UI.E2E.Tests`): the new `OperationalDashboardsAccessibilityE2ETests` passes; the suite also carries ~22 **pre-existing** Playwright strict-mode failures in untouched test classes (environment-fragile browser harness, flagged in the story) — not Story 8.1 regressions.

## Next steps

- Run suites in CI with a stable Chromium for the full Playwright E2E path.
- When the AI-action-outcome projection source is wired (Story 8.2+), extend the AI-outcomes view test beyond the M0/M1 `Unknown` default.
