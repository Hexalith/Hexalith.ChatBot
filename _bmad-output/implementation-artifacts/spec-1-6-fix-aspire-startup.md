---
title: 'Restore Aspire Startup for Story 1.6'
type: 'bugfix'
created: '2026-10-25'
status: 'draft'
baseline_commit: '6e76a59fc87ef1f0fdb051264d48d8572100b310'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'README.md'
  - '_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.6 cannot establish its local Aspire baseline. The current AppHost builds successfully but exits because required authentication configuration is missing.

**Approach:** Resolve the missing local authentication setup, then verify the exact ChatBot AppHost and its resources through Aspire.

## Boundaries & Constraints

**Always:** Preserve authenticated owner APIs, independent mailbox and controller credentials, secret Aspire parameters, Debug source references, and existing user secrets. Keep Story 1.6 in `review` until its separate protection and native-isolation acceptance requirements are met.

**Never:** Substitute dummy credentials for authenticated services, weaken startup or authorization gates, print credentials, change unrelated running AppHosts, or represent topology startup as accepted native-isolation evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Missing configuration | Required credential or owner endpoint absent | Startup refuses the incomplete configuration | Report key names without values |
| Configured topology | Valid owner endpoint and credentials available | Exact AppHost starts and resource state can be inspected | Inspect unhealthy resources through Aspire logs |
| Recovery credential | Blank, short, or malformed mailbox secret | Existing fail-closed validation remains effective | Do not generate a hidden runtime fallback |

**Decision (2026-10-25):** Use existing authenticated services and local AppHost user secrets; do not implement a separate self-contained authentication setup.

</frozen-after-approval>

## Implementation Notes

This is a chronological record. The initial investigation-only and unchanged-credential observations describe the baseline before the user chose existing services. The user subsequently approved Platform owner-service integration. The Development profile and local mailbox credential are configured, and the expanded implementation and current verification are tracked in `spec-1-6-platform-owner-service-access.md`. Missing-configuration observations below describe the historical baseline.

- Investigation only; implementation awaits the setup choice. Required configuration is absent from both AppHost user secrets and relevant environment variables; only key names were inspected.
- `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2. Its Debug build succeeded with 0 warnings and 0 errors in 78 seconds; AppHost exited 134 because the recovery mailbox secret is missing. Logs: `/home/administrator/.aspire/logs/cli_20261025T101711_b1705937.log` and `/home/administrator/.aspire/logs/cli_20261025T101712692_detach-child_dcf54c11bdce4cd99e7a0332f23d5f24.log`.
- No code, credentials, dependencies, or submodules have been changed. Unrelated running AppHosts were preserved.
- All 19 AppHost topology tests passed with zero failures/skips using the catalog-pinned xUnit 4.0.2 runner. Command: `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests /tmp/chatbot-aspire-topology.ctrf.json -result-trx /tmp/chatbot-aspire-topology.trx`.
- Focused Integration validation is blocked before test execution. After restoring the missing Commons.UniqueIds and McpCli.Abstractions assets, `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug --no-restore -m:1 -v:minimal` exited 1 with two `CS1704` errors in EventStore.Contracts: multiple assemblies named `Hexalith.Commons.UniqueIds` are imported (source 1.0.0 and package versions 2.30.3/3.127.0). The command also reported two MSB3243 conflict warnings. Log: `/tmp/chatbot-aspire-investigation-rebuild.log`. No assertion, package catalog, or reference was changed to hide this separate validation blocker.

- User selected existing services. This narrows implementation to reversible local configuration and runtime verification, with no public API or source-code change. The missing endpoint/token source is a required runtime input rather than a new design choice. Route changed to `oneshot`.
- Existing-service discovery: the only currently advertised AppHost is FrontComposer, which contains neither Projects nor Memories resources. Relevant ChatBot, Projects, Memories and FrontComposer user-secret stores contain no Projects/Memories endpoint or service-token keys. No unrelated service was started or stopped.

- Provisioned the missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret` through `dotnet user-secrets set` using JSON on stdin and a cryptographically random 64-character hexadecimal value. Verified the saved value internally and verified every existing setting remained unchanged. The value was not printed or added to the repository. Existing Projects/Memories endpoint and credentials are still required; user was asked for their local source.

- Found and reproduced an independent startup configuration defect: AppHost has a UserSecretsId but no launch profile; default startup runs in Production and ignores the configured mailbox secret. `DOTNET_ENVIRONMENT=Development aspire start --no-build --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` passes the mailbox check and fails at the genuinely missing `ChatBot:Projects:Endpoint`. Added a standard HTTPS Development launch profile and documented its default dashboard address. This small configuration correction stays within the oneshot footprint; authentication logic is unchanged.
- User identified `Hexalith.Platform` as the likely existing-service configuration source; investigating its ordinary source and documentation without loading skills or initializing nested submodules.

- Default startup verification after adding the profile: `aspire start --no-build --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` now loads the configured mailbox secret and exits 2 at `ChatBot:Projects:Endpoint`, with no shell environment override. AppHost logs: `/home/administrator/.aspire/logs/cli_20261025T104715036_detach-child_e5c7896524344aae93cc095e9b4974cf.log`. This proves the user-secret loading correction but does not establish a healthy topology.
- Re-ran all 19 AppHost topology tests after the launch-profile and README changes: 19 passed, 0 failed, 0 skipped. Exact runner command matches the earlier one with `/tmp/chatbot-aspire-profile-topology.ctrf.json` and `/tmp/chatbot-aspire-profile-topology.trx`; log is `/tmp/chatbot-aspire-profile-topology.log`.
- Platform investigation checked both `references/Hexalith.Platform` and the standalone Platform checkout. `src/Hexalith.Platform.Composition/PlatformServerBindings.cs` lists EventStore, Tenants, Parties, and Memories but no Projects or ChatBot. README describes run-owned credentials and no Keycloak in reference compositions; `PlatformLocalProfile` and `PlatformAppModel` use run-scoped authentication, not a persistent ChatBot token handoff. No supported source for the missing Projects endpoint and Projects/Memories bearer tokens was found. No Platform resource, source, dependency or credential was modified.
- Remaining dependency: configure legitimate `ChatBot:Projects:Endpoint`, `ChatBot:Projects:ApiToken`, and `ChatBot:Memories:ApiToken`, or separately design Platform enrollment/authentication integration. The startup repair remains `in-progress`; neither this repair nor Story 1.6 is declared complete.

- Independent review identified and corrected the profile propagation issue: the host profile is now named `Development`, which matches none of the nine child project profiles. A static effective-profile audit against the actual child launch settings confirms all nine retain their prior default profile and application URLs. Evidence: `/tmp/chatbot-aspire-profile-selection.json`; selection rules: https://aspire.dev/integrations/dotnet/launch-profiles/. This is static endpoint/profile evidence, not a started resource model.
- Final post-review verification: all 19 AppHost topology tests passed, zero failed/skipped (`/tmp/chatbot-aspire-development-profile.{ctrf.json,trx,log}`). Default `aspire start --no-build --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` still passes mailbox validation and exits 2 at the unavailable Projects endpoint; AppHost process exits 134. Final log: `/home/administrator/.aspire/logs/cli_20261025T105225380_detach-child_5cccd6a3b144402bbe28427980ebbaca.log`.
- Full resource readiness and authenticated owner operations cannot be verified without the remaining legitimate configuration. No native-isolation or completed-story claim is made. Changes remain uncommitted under the repository's explicit Git-operation authorization rule.

## Review Triage Log

| Finding | Verdict | Handling and evidence |
| --- | --- | --- |
| R1: A host profile named `https` changes matching child selections | medium | Patched: rename it `Development`; none of the nine children has that profile, so documented fallback preserves existing selections. |
| R2: Early implementation notes appear to contradict later provisioning | low | Patched: introduced the chronological record with an explicit baseline/current-state distinction; historical evidence remains intact. |
| R3: Start instructions are distant from mandatory credential setup | low | Patched: added a nearby prerequisite link and a named local-service-credentials section. |
| R4: Existing topology tests miss the profile propagation behavior | medium | Grouped with R1 and patched using a nine-project static effective-profile/application-URL comparison plus final default CLI execution. Full model readiness remains explicitly blocked by missing genuine service configuration; no claim that the existing 19 tests exercise profile selection. |


## Code Map

- `src/Hexalith.ChatBot.AppHost/Properties/launchSettings.json` — completed local Development-profile correction, preserving child profile selection.
- `references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformServerBindings.cs` — current supported bindings omit Projects and ChatBot; changing a sibling would expand the approved scope.
- `references/Hexalith.Platform/src/Hexalith.Platform.Composition/PlatformLocalProfile.cs` and `PlatformAppModel.cs` — run-owned authentication configuration; no supported persistent bearer-token handoff for this ChatBot topology.
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesDerivedStoreServiceCollectionExtensions.cs` — existing owner-service configuration consumer.

## Open Questions

- Resolved by explicit user approval: expand into reusable Platform enrollment and authenticated access to the existing Projects/Memories services. The approved expanded plan is `spec-1-6-platform-owner-service-access.md`; its explicit sibling-edit authorization supersedes the earlier restriction for this repair.

## Replanning Trigger

- The authorized local configuration correction is implemented, independently reviewed and narrowly verified. Investigation exhausted the user-identified Platform source without finding the remaining configuration. Completing a live baseline would now require either actual existing-service inputs or a larger cross-repository integration change. Returned to `draft`/`dispatch` under the oneshot workflow's scope-growth rule; no additional sibling work is authorized or implemented.

- User approved Platform integration. The earlier sibling exclusion is superseded for this repair. Expanded implementation is now tracked in `spec-1-6-platform-owner-service-access.md`; the local profile correction and all historical evidence above remain intact.
