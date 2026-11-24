---
title: 'Complete ten deferred ChatBot fixes'
type: 'bugfix'
created: '2026-11-24'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '0e60659af7226f2e6c06cdaec67358e674cd491f'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/README.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete ten reproducible, ChatBot-owned deferred items and run the full build and test suite before pushing the resulting commits to `main`. The ledger has no global rank. This batch gives production behavior and verification of release and isolation boundaries priority over tooling ergonomics.

## Boundaries & Constraints

Use the existing contracts, test entry points and authorization checks. Record a concrete reason for each examined item that cannot proceed. Owner approval, upstream publication, external drills and retired behavior are grounds to advance to the next eligible item. Record each completed item in the active ledger and commit its implementation, tests and ledger update together.

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotIdentity.cs` defines the canonical wire identity validation for redaction.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs` governs snapshot evidence, unavailable decisions and cancellation.
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationStreamingSubscriber.cs` already requests a scoped refresh after reconnect. `ChatBotProjectConversationWorkspace` owns scope and disposal checks.
- `eng/ChatBot.Composition.props` selects dependency mode. `tests/PackageCatalogTestHelper.cs` already runs MSBuild from test code.
- `.github/scripts/run-xunit-v4.sh` checks that the recovery runner actually executed tests. The nightly release hook uses Microsoft.Testing.Platform directly.

## Tasks & Acceptance

1. [x] `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs` and `tests/Hexalith.ChatBot.Server.Tests/Gateway/SafeOutcomeProblemCompatibilityTests.cs`. Given malformed or missing problem identifiers, when the gateway redacts the problem, then it emits a valid correlation ULID, omits an invalid optional task ID and preserves valid input IDs.
2. [x] `src/Hexalith.ChatBot.Server/Governance/AiMediation/TenantAiPolicySnapshot.cs` and `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs`. Given mutable or throwing provider collections, when policy evidence is constructed and evaluated, then later mutation cannot change it and collection failures follow the redacted unavailable path. Preserve caller cancellation and existing malformed-evidence behavior.
3. [x] `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationStreamingSubscriber.cs` and `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationStreamingSubscriberTests.cs`. Given a projection change between the initial read and the tenant join, when the join succeeds, then the workspace refreshes authoritative state through the current scope. Cover failed joins, disposal and reconnect. Ledger reference U1-B7.
4. [x] `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`. Given a representative server project with default, explicit and CI configurations, when MSBuild evaluates it, then the selected mode and EventStore reference family agree. Run with the real project evaluation and existing shared catalog.
5. [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`. Given a configured M2 gate, when HTTP requests use an absent, wrong or correct secret, then only the correct secret reaches the gate logic. Pin the existing behavior when the gate is unmapped.
6. [x] `tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs` and `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs`. Given the retained local topology, when its Dapr ACL is parsed and selected, then the local policy remains policy-free and its config stays isolated from production composition.
7. [x] `.github/actions/nightly/run-release.sh`. Given a successful process with zero discovered tests, when the nightly release hook runs, then the test lane fails. Verify with the Microsoft.Testing.Platform minimum-count option and a controlled invocation. Retain the existing recovery guard evidence.
8. [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs` and `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eContractTests.cs`. Given a malformed token payload or missing required claim, when the provisioner checks identity, then it produces a bounded diagnostic that contains no bearer contents. Preserve the accepted and mismatched identity cases.
9. [x] `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs` and `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewEffectsTests.cs`. Given a stale blank-ID load, when it attempts to publish its failure, then it cannot overwrite a newer route. A current blank load still reports the required ID.
10. [x] `.github/scripts/with-source-mapping.py`. Given a successful restore and a failing tool version command, when the wrapper runs, then it forwards the tool's diagnostics and exit code and does not launch the build command. Exercise the child processes with a controlled tool shim.

## Implementation Notes

- Implement and commit the ten items in the stated order on local `main`. The parent session owns the final review, broad test run and push. Keep this draft out of the per-item commits. Validate every exact commit message with the pinned `@commitlint/cli@21.2.4` and repository config.
- The task numbers correspond to ledger summary lines 1252, 1328 and 1341 (one fix), 1957, 1480, 2209, 1100, 2217, 2117, 2185 and 2181 at the baseline. Use the distinctive summary text to locate lines after the ledger grows. Record each completion in the legacy entry and include its reference in the commit message.
- DW-8, DW-15, DW-40, DW-43, DW-44, DW-45, DW-47, DW-48, DW-54 through DW-59, DW-63, and DW-64 still need the approved external environments, product instrumentation, durable bindings, or full-window recovery evidence documented on their ledger entries.
- DW-79 concerns the structure of a historical commit. DW-81, DW-96, and DW-100 require work in the owning sibling repositories. DW-80 and DW-129 require Hexalith.CiCd ownership. DW-84 needs a separate human acceptance of regenerated visuals. DW-86 needs repeat-safe audit and alert side effects before proving durable shutdown and replay with the current cancellation policy. DW-92 needs package-owner ratification and has since advanced past the cited SDK version. DW-94 still requires a reusable EventStore shutdown API.
- Early workflow completion and post-receipt failure reconciliation remain open because status and recovery rules need a durability decision. The Story 13.2 browser acceptance needs current owner grants. The server metrics flake, incomplete external-owner overrides and live workflow health authentication already have fixes in `ca2e7e7`, `5c15ba0`, and `065ded0`.
- The review patches were folded into their originating commits. The catalog verification repair advanced the McpCli pointer to upstream `fab626ebc6c433ee8195ca7013b7aec5f16f0f53` and aligned the test expectations with the already-selected Projects 2.0.0 packages. The complete Architecture project passed after both repairs.
- The browser suite reproduced a click before Blazor had connected. The existing governed-note action now remains disabled until it can receive events, and the same Playwright case passes. New verification screenshots go to `TestResults/screenshots/story-13.9` and the reviewed screenshots remain in the repository.
- A workspace push published the ten items and the two verification repairs at `85b41d2192b6de325d8ad1346649370ad47d7dca` while the suites were running. A remote ref check verified the published revision.

## Spec Change Log

## Review Triage Log

| Review finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| Blind review found a reconnect notice on initial join. | Medium. The reconnect reducer sets the notice on the first successful group membership. | Patch the join and reconnect paths. |
| Blind review found a lost same-tenant route refresh. | Medium. A switch can clear the conversation until after the join callback has returned. | Keep a pending refresh for the mounted route. |
| Blind review found an older association result can still win. | Medium. A load continuation can pass the generation test and reach the reducer after a new route. | Extend the originating-load guard to its results. |
| Blind review found a malformed Unicode diagnostic escape. | Medium. Claim materialization can throw after JSON parsing succeeds. | Normalize the decoding failure and test it. |
| Blind review found that losing one of two release checks can go undetected. | Medium. One remaining test satisfies the zero-test guard. | Track exact test coverage as a follow-up. |
| Blind review found a signaled tool returns the wrong shell status. | Low. Python passes a negative subprocess status to `sys.exit`, which wraps it. | Map signal status and verify with a subprocess. |
| Blind review found that a shared publish rule can evade the Dapr test. | Medium. The original assertion reads raw project text. | Verify evaluated publish inputs. |
| Blind review found that ordinary authentication is untested at the M2 endpoint. | Medium. All requests in the newly added no-secret cases are anonymous. | Add an ordinary authenticated principal. |
| Blind review found that MSBuild filenames do not prove the reference paths. | Low. The same filenames are returned for a nonexistent configured root. | Assert the evaluated paths exist under the intended source root. |
| Blind review found untested snapshot copy paths. | Low. The initial tests cover constructor mutation and a throwing list only. | Cover record copies and a throwing map. |
| Blind review questioned request identifier provenance. | Low. The existing resolution omitted the trace through the gateway. A single header is parsed by `ChatBotCorrelationHttpContextExtensions`, the gateway forwards resolved identifiers, and public redaction parses again for factory calls with raw inputs. | Record the trace here. |
| Edge review found lost catch-up during a same-tenant load. | Medium. The current route can lack a conversation when the group join finishes. | Keep a pending refresh for the mounted route. |
| Edge review found that a join can borrow another connection's callback. | Medium. Replacement can update the global callback between the old connection check and the callback read. | Bind the callback to its connection. |
| Edge review found that required claim decoding can disclose bearer byte details. | Medium. An invalid UTF-8 sequence can escape with a nested decoder exception. | Return the fixed payload diagnostic. |
| Edge review found a wrong signal exit status. | Low. A direct shim returned 241 for SIGTERM. | Return 143 for SIGTERM. |
| Edge review found that the catch-up acceptance can fail. | Medium. The initial callback drops the refresh while the replacement read is still loading. | Exercise this interleaving in the mounted UI test. |
| Edge review found that the bounded mailbox diagnostic acceptance can fail. | Medium. `GetString` can surface a decoder exception outside the initial catch. | Verify malformed claim encoding produces a fixed diagnostic. |
| Verification review demonstrated an imported release rule could publish the local Dapr ACL. | Medium. Real MSBuild evaluation returned the local file while the direct project text stayed unchanged. | Check evaluated publish inputs. |
| Verification review found no retained automated regression of the nightly hook. | Low. The controlled zero-test invocation exercises the guard and the repository excludes tooling checks from module CI. | Keep the one-off qualification with the item. |
| Verification review found no retained test for the wrapper error path. | Low. The controlled child process exercises both output streams and checks that the build does not run. | Keep the one-off qualification with the item. |
| Verification review found a first-join notice that the recorded callback test misses. | Medium. The test bypasses the reducer that sets the user-visible notice. | Distinguish the neutral refresh from reconnect in the mounted test. |
| Verification review found that the mounted test misses a pending same-tenant route. | Medium. The test had already populated the new conversation before join. | Hold the new conversation load until after join. |
| Verification review found untested malformed UTF-8 and unpaired surrogates. | Medium. Runtime probes showed both reach string materialization after the JSON parse. | Exercise both malformed claim encodings. |
| Full Architecture suite caught the removed reconnect callback field. | Medium. `StoryTenSixBClient_ShouldSubscribeToChatBotHubAndReQueryViaNudge` expects the retained reconnect callback. | Preserve the real reconnect callback. |

## Verification

- `dotnet build Hexalith.ChatBot.slnx --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` passed with one existing Aspire deprecation warning. The final browser project rebuild also passed.
- Every test project was run separately using `.github/scripts/run-xunit-v4.sh`. The 11 non-browser projects in the ordinary rerun passed 5,508 tests. The full browser rerun passed 292 of 296 tests. The interactive test renderer needed explicit renderer metadata for the prerender fix. All four previously failing cases and the governed-note readiness case passed together after that repair.
- The integration project passed 586 tests, failed the Story 13.2 live test during EventStore writer protocol activation, and skipped four environment-dependent cases. Both native queue cases passed separately against disposable Dapr state. The replay and policy-preflight cases require their own selected inputs.
- The required live project passed the workflow health check. Its browser test stopped at a missing Tenants bootstrap receipt and its governed-command test stopped when the Memories tenant-provisioning workflow could not obtain EventStore workload credentials.
- Both release tests failed. The stop-ship sweep timed out while provisioning Memories and the origin-parity command received 403.
- The recovery test first needed its workflow-owned marker directory. A subsequent run on shared Redis stopped at a stale writer marker. Repeating with a private marker directory and disposable Redis reached `BootstrapGlobalAdmin`, where EventStore returned 503 because catalog authority could not admit the operation.
- Commitlint and diff whitespace checks passed. `git ls-remote origin refs/heads/main` confirmed the published ten items and two verification repairs. The working review stays open while the live checks fail.
