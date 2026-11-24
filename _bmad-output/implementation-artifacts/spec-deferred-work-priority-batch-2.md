---
title: 'Complete seven deferred ChatBot fixes'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4b123d24676de0a89ceb224d05ad4e7c25d72d08'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Verified, ChatBot-owned ledger defects remain open: six of seven projection subscriptions likely never receive published events, AI-execution recovery failures bypass the versioned problem catalog, sibling health routes lack anonymous-denial coverage, repeated or suppressed UI failure announcements misbehave, repeated conversation streams duplicate ids, and the local topology misses Memories' fixed sidecar ports.

**Approach:** Implement the next seven eligible items of `_bmad-output/implementation-artifacts/deferred-work.md` in priority order (production data flow, public failure contract, security-boundary coverage, accessibility, local topology). Each item ships with tests, its ledger resolution and its own commit. The parent session runs the full build and suite, then pushes to `main`.

## Boundaries & Constraints

**Always:** Reuse the contracts, factories, hosts and test patterns in the Code Map. One type per `.cs` file, repository `.editorconfig` style, XML docs. Record each completion as an indented `  resolution: Resolved 2026-11-24. <what changed and how it was verified>` line directly under its ledger bullet (the ledger is LF). Commit implementation, tests and that ledger line together, staging only your own paths, with a Conventional Commit whose body has `Deferred work: <ledger summary>` and the trailer `Refs: deferred-work.md@4b123d2#L<baseline line>`. Validate each exact message with `references/Hexalith.Builds/node_modules/.bin/commitlint --cwd . --edit <file>`.

**Never:** Stage, edit, format or revert the five pre-existing uncommitted files in `tests/Hexalith.ChatBot.IntegrationTests/` (`LiveCatalogFixture.cs`, `Recovery/LiveRecoverySweep.cs`, `Recovery/RecoveryMailboxOwnerProvisioner.cs`, `Story132ProductionBrowserAspireE2ETests.cs`, `TrivialGovernedCommandAspireE2eTests.cs`); another session owns them. Never edit `.github/workflows/**`, `references/**`, the OpenAPI document or generated clients. No push. Do not weaken assertions except where the item's behavior change requires it.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Subscription discovery | sidecar GETs `/dapr/subscribe` with the app token | one subscription for the chatbot pubsub/topic (with its dead-letter topic) routed to the fan-out route, plus the dead-letter drain | N/A |
| Fan-out delivery | an approval, task-intent or ai-outcome event on the fan-out route | that family's read model holds the event; other families ignore it; 200 | N/A |
| Untrusted ingress | fan-out route without or with a wrong app token | 401; no store mutation | N/A |
| Projection fault | one family's handler throws | non-2xx so Dapr redelivers; families already applied stay idempotent | exception not swallowed |
| AI recovery hidden | unauthorized, blank, unknown or lapsed key | the same catalog-backed problem+json the read routes return for `safe_not_found` | no store call where it is not made today |
| AI recovery bad cursor | undecodable cursor | catalog-backed validation problem+json | no store call |
| Repeated association failure | the same failure code after a user retry | the banner announces again and focus returns once per attempt | N/A |
| Suppressed banner | a deduplicated banner remounts | no `role` and `aria-live="off"`; an announcing banner keeps its role | N/A |
| Two populated streams | two streams with identical items on one page | every id unique and every aria reference resolves | N/A |
| Memories port reuse | `Dapr:InternalGrpcPorts` includes Memories' daprd HTTP or gRPC port | topology validation fails with a collision | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs:134-160` -- maps seven projection endpoints with the same `WithTopic(pubsub, topic, deadLetter)`. Dapr.AspNetCore 1.19.1 `MapSubscribeHandler` keeps only the first default route per pubsub/topic (it logs "A default subscription to topic … already exists"), so only governed-operations receives events. Health routes are at :42, :54 and :64. `Denied` (:497-505) is the read-route safe-not-found result.
- `src/Hexalith.ChatBot.Server/Projections/{GovernedOperation,MailboxIntake,Association,ParticipantResolution,AiOutcome,TaskIntent,Approval}ProjectionEndpoints.cs` -- typed `Published*Event` lambdas. Translators return null for foreign events, handlers are idempotent per source version, and Association also checks the app token itself. `ChatBotDeadLetterProjectionEndpoints.cs` uses its own topic; leave it unchanged. Bodies are CloudEvent data unwrapped by `UseCloudEvents` (`Program.cs:151`). Bind with the host's `Microsoft.AspNetCore.Http.Json.JsonOptions`.
- Tests that post to per-route constants must keep passing: `GovernedOperationProjectionTests`, `AiOutcomeProjectionTests`, `ChatBotProjectConversationHubE2ETests`, `GovernedNoteDurableChainTests`, `AssociationProjectionAuthorityTests` (token-case pattern). The host-test pattern is `ServerBootstrapApiTests.ServerHostShouldExposeSdkCanonicalRoutesExactlyOnce` (~:470).
- `src/Hexalith.ChatBot.Server/Lifecycle/AiExecution/AiExecutionRecoveryEndpoints.cs` -- `SafeNotFound()` (:106) is a bare `Results.NotFound(new { code })`. The bad cursor (:41) is a bare 400. Tests: `TrustedAuthorityReviewTests` (:513-529), `AiExecutionCoordinatorTests.cs:680`, `AiExecutionCoordinatorTests.Authority.cs:52,94`.
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs:871-884` -- the anonymous 401 is pinned for `/health/chatbot` only. `/health/chatbot/periodic-enforcement` is tested only signed in (:954).
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:59-62,301-311,474` -- fixed failure key and no `AnnouncementActivation`. The `_announcedErrorCode` focus guard skips repeats. The pattern to copy is `GovernedOperations.razor:62` + `GovernedOperationsReducers.cs:19` (bff25fa). Tests: `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewStateTests.cs`, `tests/Hexalith.ChatBot.UI.E2E.Tests/VersionedSafeOutcomeRenderTests.cs` (`MountedBannerShouldActivateEachSameCodeAttemptAndSuppressRepeats`, :814).
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor` -- `AriaRole` follows `ResolvedPoliteness` while `aria-live` follows `EffectivePoliteness` (`_shouldAnnounceLiveRegion`). `ChatBotFailureStateConversationItem.razor:10-11` has the same pattern. E2E pattern: `SafeOutcomeInteractionTests.DashboardRefreshShouldAnnounceEachAttemptAcrossRemount` (:50-62).
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor:24-56,110-112` -- `_instanceId` scopes only the title and empty state. The ids built from `ItemId`/`AssociationId` are in Participant (:19,:71,:153), Approval (:19,:225), Failure (:8,:27,:87), AiOutcome (:17,:85), Decision (:22) and Email (:60), and flow through `ChatBotEvidenceChip.razor:95` and `ChatBotGovernedAction.razor:67-69`. Attachment's `IdScope`/`ScopeSuffix` (:152-156) is the pattern. Keep the failure dedup key (stream :139) unscoped. Test: `ConversationAccessibilityRenderingTests` (`RepeatedEmptyStreamsShouldUseUniqueStableHeadingsWithNestedReasonAtLevelThree`, :114, `RenderPairAsync`, `AssertUniqueIds`, `AssertReferencesResolve`).
- `src/Hexalith.ChatBot.AppHost/Aspire/ChatBotAspireModule.cs:77-86,154` -- `SidecarAppIds` and `ValidateUniqueInternalGrpcPorts` do not cover Memories. `src/Hexalith.ChatBot.AppHost/Program.cs:142-159` composes Memories with fixed daprd ports 3502/50002 (`memoriesSidecarOptions`). Tests: `RecoveryValidationTopologyContractTests.cs:789,874` (IntegrationTests), or `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs` if it can reach the method.

## Tasks & Acceptance

**Execution:**
- [ ] 1. Ledger L1208 -- projection files above plus a new fan-out endpoints type -- Add one sidecar-guarded fan-out route that carries the single `WithTopic`. It reads the body once and runs every family's existing handling in the current mapping order. Extract each lambda into a static method shared by its per-route endpoint and the fan-out. Remove `WithTopic` from the seven per-route endpoints and keep them mapped and guarded. Invalid JSON returns 400. A family whose DTO cannot bind is skipped. The route returns 200 only when every family succeeds, and propagates faults.
- [ ] 2. Ledger L1302 -- `AiExecutionRecoveryEndpoints.cs` and its tests -- Return the read routes' catalog-backed safe-not-found problem through `IChatBotProblemDetailsFactory`, with correlation from `HttpContext`. Return the factory's validation problem for a bad cursor. Search UI, Client, CLI and MCP for callers and keep them correct.
- [ ] 3. Ledger L2230 -- `ServerBootstrapApiTests.cs` -- Make the anonymous check a `[Theory]` over the three health routes, asserting 401 and `authentication_denied`, and keep the authenticated check.
- [ ] 4. Ledger L1296 -- `AssociationReview.razor`, its state and reducers -- Add a failure activation counter, pass it as `AnnouncementActivation`, and key the focus guard on code plus activation.
- [ ] 5. Ledger L1303 -- `ChatBotStatusBanner.razor` (and the failure item span if consistent) -- Derive `role` from the effective politeness. Prove it in a browser test that verifies the browser path actually ran.
- [ ] 6. Ledger L1924 -- the stream and item components -- Add an `IdScope` that defaults to the stream instance and propagate it to every id built from `ItemId`/`AssociationId`. Update the existing id assertions.
- [ ] 7. Ledger L2161 -- `ChatBotAspireModule.cs`, `Program.cs` and a topology test -- Include Memories' configured daprd HTTP/gRPC ports as reserved values in the uniqueness validation.

**Acceptance Criteria:**
- Given the server host, when `/dapr/subscribe` is read before task 1, then the new subscription test fails on the collision, and after task 1 it passes.
- Given each task, when its targeted test classes and the projects it touches are built and run, then they pass with 0 warnings, and the ledger and commit record it.

## Implementation Notes

- Commits, in priority order: L1208 `3d63cf9` (review follow-up `7c40c9c`), L1302 `44b7bd1`, L2230 `b585602`, L1296 `2a38f36`, L1303 `be80196` (review follow-up `4b5f048`), L1924 `d94ab6c` and L2161 `56837dc`. The ledger triage and review deferrals are a separate docs commit.
- About 24 ChatBot-owned candidates were verified before selection. Items that need an owner or product decision, a sibling repository, CiCd, or an external environment, and items already fixed, are recorded on their ledger bullets.
- A process outside this session pushed `3d63cf9` through `d94ab6c` to `origin/main` while the batch ran, so review fixes are follow-up commits, not amendments.
- L2230's live-probe half had already been fixed by `065ded0`. L1302 now answers hidden outcomes with the read routes' 403 `authorization_denied` problem; no repository caller depended on the old 404.
- The review confirmed that EventStore publishes events inside `payload` bytes while six projection families read flattened fields. That pre-existing defect is deferred as a high-priority entry, and the L1208 resolution says so.

## Spec Change Log

## Review Triage Log

| Review finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| Blind B1 / Edge E2: the association family's inline token check gates every fan-out delivery. | Medium. `PublishedAssociationEvent` binds nearly any body and its comparison is untrimmed while the sidecar guard trims, so a padded token returns 401 for every event and stops later families. | Patch (task 1 follow-up). |
| Edge E1/E10: the AI-outcome family throws on an unbindable object body. | Low. Its handler deserializes the raw body without catching `JsonException`, returning 500 against the task's skip rule. | Patch (task 1 follow-up). |
| Blind B2: a family's own event that fails to bind is acknowledged silently. | Low. `TryBind` maps `JsonException` to a skip with no log; the governed-operation route used to answer 400. | Patch: warn with family and event type (task 1 follow-up). |
| Blind B3 / Edge E3: one failing family blocks later families and the fault does not name the family. | Low. Inherent to one subscription; the only deterministic non-200 is the association 401 patched above, and faults are retryable by design. | Reject; the binding warning names the family. |
| Blind B4 / Verification gap 2: mailbox-intake, association and participant-resolution are never delivered through the fan-out. | Medium. Only four families have delivery rows; the topology guard is a source-text check. | Patch: add the three rows (task 1 follow-up). |
| Blind B5: no test posts a CloudEvents-wrapped body. | Low. The fan-out parses the body itself; no test proves `UseCloudEvents` unwraps first. | Patch: one CloudEvent test (task 1 follow-up). |
| Verification gap 1: EventStore publishes a metadata envelope with `payload` bytes, but six families read flattened domain fields. | High. Verified in `EventEnvelope.cs` and `EventPublisher.cs:199-229`; only governed-operation decodes `payload`, so the other six ignore live events. Pre-existing and exposed by the routing fix. | Defer as a new ledger entry; reword the L1208 resolution. |
| Verification gap other: failure-state projections have no route or registration. | Low. Pre-existing and already recorded with the L1250 triage. | Fold into the envelope deferral. |
| Blind B6: the L1208 and L1303 resolutions overstate their evidence. | Medium for L1208 given the envelope finding; low for L1303, whose resolution states that Chromium's tree replaced NVDA/JAWS. | Reword L1208; keep L1303. |
| Blind B7: the banner drops its role on any later render, not only a suppressed remount. | Medium. `OnParametersSet` re-runs deduplication, so a still-mounted alert loses `role` on the next parent render. | Patch: keep the role once the instance has announced (task 5 follow-up). |
| Blind B8: the composer browser assertion accepts both states. | Low. `ShouldBeOneOf` pins nothing; it follows from B7. | Patch with B7. |
| Blind B9 / Edge E7: returning to the association review page refocuses the summary. | Low. Pre-existing: the old code-only guard also started empty per instance. | Defer. |
| Edge E5: repeated validation rejections do not advance the activation. | False. Validation rejections never render the focused failure summary: they set only `ValidationErrorCode` after the submit reducers clear `ErrorCode`, and they announce through the separate `association-review-validation` banner under its own once-per-attempt rule. | Reject. The related pre-existing stale focus target for a validation code after a failure is deferred. |
| Edge E6: a stale decision or correction failure can announce on another association. | Maybe-false, medium if true. The failed reducers do not check the originating association; that is pre-existing and only the activation is new. | Defer, unverified. |
| Blind B10: the default id scope is random per instance. | Low. The stream title already follows this pattern and no consumer relies on stable item ids. | Reject. |
| Blind B11: `IdScope` code is repeated in seven components. | Low. No named divergence, and a shared helper adds structure. | Reject. |
| Blind B12: the id test cannot detect regressions in decision, email and AI-outcome. | False. Their scoped chips are always `Available` with no `UnavailableReason` or `ActivationId`, so `ChatBotEvidenceChip` writes no id from them in any supported state. The suffix is kept for future reason states. | Reject. |
| Edge E8: an `IdScope` with whitespace breaks aria references. | Low. No caller supplies one. | Reject. |
| Blind B13: the bad cursor uses a command validation code instead of safe-not-found. | Low. The frozen matrix specifies a catalog-backed validation problem, and the bare 400 already distinguished bad cursors. | Reject per the frozen decision. |
| Blind B14: the safe-not-found helper copies the private `Denied` helper. | Low. Tests pin byte equality with a read route. | Reject. |
| Blind B15: the 404 to 403 change has no contract follow-up. | Low. These operator routes are not in the OpenAPI document; that is pre-existing. | Reject. |
| Edge E4: framework binding failures still bypass the catalog. | Low. Pre-existing minimal-API binding behavior shared by every typed route. | Reject. |
| Blind B16 / Edge E9: the port reservation is Memories-specific and partial. | Low. Memories pins only its daprd HTTP and gRPC ports, which are reserved. | Reject. |
| Blind B17: the health theory lists routes by hand; workflows lacks a signed-in host test. | Low. No unlisted route exists; the signed-in gap is pre-existing. | Reject. |

## Verification

**Commands:**
- `python3 .github/scripts/with-source-mapping.py dotnet build tests/<Project>/<Project>.csproj --configuration Debug -m:1`, then the built test executable with `-class <Fqn>` (`< /dev/null`) -- expected: 0 errors; targeted classes pass.
- `dotnet build Hexalith.ChatBot.slnx --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` -- expected: success (parent session, final).

**Results (2026-11-25):**
- The Release solution build passed with 0 errors. Its 7 warnings are existing Aspire `AddProject` CS0618 deprecations, and no NU1903 appeared.
- Each project was run separately with `.github/scripts/run-xunit-v4.sh`, and all passed: Cli 61, Client 143, Conformance 152, Contracts 539, Mcp 70, Testing 41, Workers 32, AppHost 18, Server 3743, UI 636, Architecture 126, UI.E2E 298 (real Chromium) and IntegrationTests 604, plus 5 environment-gated skips.
- Live.IntegrationTests (3), Recovery.Tests (1) and Release.Tests (2) fail closed without `HEXALITH_CHATBOT_TIER3=1`. With Tier-3 enabled, `GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin` failed before any ChatBot projection: the EventStore owner-command submit in `TrivialGovernedCommandAspireE2eTests.SubmitOwnerCommandAndWaitAsync` timed out after 15 seconds. Tenants domain-service calls were timing out and route-catalog identity errors were logged. That helper is in another session's uncommitted working files.
