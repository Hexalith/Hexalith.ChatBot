---
title: Restore browser OIDC profile scope in the development realm
type: bugfix
created: 2026-10-23
status: in-review
route: oneshot
review_loop_iteration: 0
baseline_commit: 0fe0c477e49e90d3edd2fb80229beac5132feda9
context: []
---

<frozen-after-approval reason="user authorized CI repairs without human validation">

## Intent

Fix the reproducible Story 13.2 browser login failure: the UI requests the standard OpenID Connect profile scope, but the development Keycloak realm does not define or assign it. Keycloak rejects the pushed authorization request with invalid_request and Invalid scopes: openid profile, producing HTTP 500 before the login form. Add the canonical built-in Keycloak 26.6 profile scope and allow the browser client to request it. Preserve machine-client scope assignments and production authority boundaries. Validate initial real browser navigation against Keycloak and retain the full CI acceptance check.

</frozen-after-approval>

## Implementation Notes

- Investigation used the exact packaged Release UI and clean, task-owned Keycloak 26.6. The original realm returned HTTP 500. A temporary corrected realm retained PAR and returned HTTP 200 login with visible username and password controls.
- The canonical scope was read from the running Keycloak built-in profile definition, excluding generated IDs. Temporary source: `/tmp/chatbot-browser-auth-canonical-profile-scope.json`; proposed diff: `/tmp/chatbot-browser-auth-realm.patch`.
- Apply the profile declaration to `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json` and add only profile to hexalith-chatbot optionalClientScopes. Preserve all existing nonbrowser scope assignments.
- Add one focused configuration contract in `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs` to bind requested nonimplicit browser scopes to declared/assigned realm scopes; preserve mapper semantics for preferred_username. Refresh that file's stale cross-reference to the repaired service-grant expiry test.
- Do not mutate the realm while the integration topology contract tests are running, because their existing mutation scenarios restore the fixture.
- Baseline Aspire startup was attempted for the fixture repair and stopped at missing ChatBot:LiveRecoveryValidation:MailboxClientSecret after a successful Debug source build; no ChatBot resources started.
- Review this fix together with the independent CI authority fixture repairs. Run the AppHost test project independently and GitHub CI after the combined repairs are pushed. The full release still needs an explicit scope decision because M2 atomic participation is absent.

## Verification and Review

- Packaged Release UI + clean Keycloak 26.6 + headless Chrome: initial navigation reached HTTP 200 login with visible username/password fields; PAR remained enabled. Temporary resources were removed.
- AppHost project Debug build: exit 0, no warnings/errors. Built xUnit AppHost runner: 19 passed, 0 failed/skipped.
- Parsed original and corrected realm clients: every nonbrowser client remained structurally identical.
- Independent review triage is recorded in [the fixture repair spec](spec-ci-owner-authority-fixtures.md). Added exact mapper protocol/implementation assertions from review and reran the AppHost suite successfully. No findings deferred.

Completion posture: implementation and local verification passed; repository completion-evidence enrollment is not claimed. The plan remains in review under docs/story-evidence-integrity.md until its required evidence is available.
