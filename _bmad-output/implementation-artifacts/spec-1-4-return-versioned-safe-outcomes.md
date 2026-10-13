---
title: 'Return Versioned Safe Outcomes'
type: 'feature'
created: '2026-10-12'
status: 'in-review'
baseline_commit: 8437900622d5bf7a9d34ca5cc543cd802f653cb1
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Existing safe outcomes have English catalog copy and a wire version, but unknown codes can throw, French UI copy covers only part of the catalog, and public renderers can pass through untrusted error text. A user can receive a missing, unsafe, or unactionable explanation.

**Approach:** Make the existing catalog authoritative, resolve unknown or untranslated values to a terminal generic denial, and present English/French copy and permitted actions at public boundaries.

## Boundaries & Constraints

**Always:** Preserve OpenAPI 3.1 authority, metadata-only RFC 9457 problems, existence-neutral denial, trusted-context gates, and the v1 wire version unless a contract change requires regeneration. Each entry has code, version, headline (at most 80 characters), one safe sentence, terminality, applicable retry/escalate/dismiss/request-access action, disabled reason, and accessible description. Use FrontComposer and Fluent UI V5; keep state inline and deduplicate announcements. Keep precise causes in authorized diagnostics.

**Never:** Render raw exceptions, untrusted problem prose, restricted names or identifiers, secrets, or hidden owner details. Unknown codes cannot imply success or retry. No second wire contract, persistence path, or global banner for local failure.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Known outcome | Catalog code and English/French locale | Complete localized headline, sentence, terminality, safe action and accessible description | No fragments or unsupported actions |
| Unknown or untranslated | Null, malformed, absent code or missing locale entry | Generic terminal denial with dismiss action | Count uncategorized code without recording raw input |
| Public failure | API, UI, CLI, or MCP receives unsafe detail | Catalog-backed metadata-only response or rendering | Strip raw prose, path, secret, resource and exception data |
| State transition | Inline status changes or action unavailable | Persistent text and semantic status; deduplicated feedback; focusable action with disabled reason | No false success or global banner |

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCatalogEntry.cs` — 86 English entries; `Resolve` throws on unknown codes and entries lack terminality/version.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`, `Redaction/CoarseUserFacingRedactionStage.cs`, `Status/OperationStatusHttpResults.cs` — public failure paths; redaction copies supplied prose and unknown status actions can throw.
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`, `SharedResource.resx`, `SharedResource.fr.resx` — only 19 failure codes are translated; missing resources throw.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor` — reuse inline status and transition deduplication.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` and `ChatBotMessageCatalogEntry.cs` — add version/state/terminality/action/accessibility metadata and deny-safe resolution, preserving stable codes.
- [x] `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`, `SharedResource.resx`, `SharedResource.fr.resx`, `ChatBotUiTextKey.cs` — resolve all 86 codes in both locales with complete phrases and safe missing-translation fallback.
- [x] `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`, `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`, `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs` — catalog-normalize failures and unknown actions before serialization.
- [x] `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor` — render text plus semantic status and permitted focusable action, associate unavailable reasons, and reuse transition deduplication.
- [x] `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`, `src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs` — resolve problem code rather than forwarding wire prose; keep output metadata-only.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`, `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantLeakageScannerTests.cs`, `tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs`, `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`, `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` — test matrix, locale parity, assistive output, and sentinel leakage.

**Acceptance Criteria:**

- Given every code, when English and French entries are checked, then version, state, terminality, action, disabled reason, accessible description, headline limit, and one-sentence copy agree.
- Given any unknown, malformed, or untranslated code, when a public boundary resolves it, then the generic result is terminal, existence-neutral, and non-retryable, and raw input is absent from output and telemetry.
- Given refused, blocked, degraded, failed, denied, waiting, retryable, or unsupported outcomes, when API, UI, CLI, or MCP presents them, then catalog copy and actions contain no restricted sentinel across response, render, or telemetry.
- Given a UI transition in light, dark, forced-colors, reduced-motion, keyboard-only, and screen-reader use, when status renders, then state stays inline, text and semantic status identify it, deduplicated feedback announces the transition, and action or disabled reason is accessible.

## Implementation Notes

- The catalog now contains 87 entries including the terminal `generic_denial` fallback; OpenAPI v1 and the generated client gained the previously omitted codes without renumbering existing enum members.
- English and French resources cover every catalog entry. Server Problem Details and status results, UI failure components, CLI, and MCP resolve public copy and actions through the catalog. Unknown codes suppress caller prose, disallow retry, and expose only a dismiss action.
- Operation status intersects catalog actions with persisted per-operation permissions. Actual Razor render tests cover known and unknown localized failures, semantic status, announcement deduplication, and disabled action reasons.
- No persistence path, dependency version, or public wire version changed. The additive OpenAPI code enum required client regeneration and a refreshed client hash fixture.
- Review fixes preserve trusted local empty-state and banner guidance, make every unknown status reason terminal, respect explicit no-retry decisions, and count unknown or untranslated fallback through an input-free metric.

## Spec Change Log

- 2026-10-12: Completed the resumed parallel review, fixed permission and accessibility regressions, and verified all affected suites (4,016 tests, zero failures/skips). Added real locale/theme/keyboard browser coverage and recorded the environment limitation for full Aspire startup.
- 2026-10-12: Implemented Story 1.4 and added focused render, leakage, catalog, and boundary coverage. The broad Debug build remains blocked by existing sibling reference conflicts; see Verification.

## Review Triage Log

### Independent review of remaining patches (2026-10-12)

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| N-B1 | Success copy can appear in a denial | high | patch | A command_accepted code is accepted by redaction and both denial formatters even inside a refused response; normalize this demonstrated incompatible code to generic denial. |
| N-B2 | Nested prior outcome bypasses code and lifecycle guards | high | patch | ToWire copies prior reason/lifecycle/retry fields without validating them; a compiled probe serialized unknown reason 999 alongside pending acceptance. |
| N-B3 | Generic failure card retains successful/retry metadata | high | patch | The card reads FailureStatus, Retryable, retry counters and schedule directly after resolving its code to generic_denial; suppress or derive these values consistently. |
| N-B4 | A new same-code composer attempt is silent | medium | patch | SubmitAsync is the existing current-user attempt boundary, but banner keys contain only stable component identity and code; increment a private attempt sequence there. |
| N-B5 | Passive failure history acquires live alerts | medium | patch | ConversationStream renders shared background-worker records with no action callback; new card live regions announce these passive records. Use that existing passive/interactive distinction and test history rendering. |
| N-B6 | Failure codes fall through to denied state | medium | patch | memories_correction_failed and vector_reindex_failed have explicit failure copy but map to denied; classify those demonstrated terminal failures as failed. Outbound duplicate submission is a refused current send, so its denial is retained. |
| N-B7 | Unknown problem category is serialized | medium | patch | The redaction stage preserves an undefined category when a code is recognized; validate the enum at failure boundaries and use terminal safe denial for malformed metadata. |
| N-B8 | Missing action or fallback resources can throw | false | reject | All required action and disabled-reason resources are compiled and checked in both locales by the full localization suite; unsupported locales resolve through the present neutral fallback resources. A corrupted deployment or source deletion is not a demonstrated incoming-code case. |
| N-B9 | French UI gets an English accessible description | false | reject | No UI renderer consumes the domain entry AccessibleDescription property; the actual aria labels use localized full resource templates. French component and Chromium accessibility-tree cases passed. |
| N-B10 | Solution source inventory expands Release CI | maybe-false | defer | Unverified medium risk: Release has unconditional explicit sibling entries, but the pre-existing AppHost already includes source project closures. Compare exact Release project graphs before/after the inventory edit to establish a newly expanded build boundary. |
| N-B11 | Computed catalog properties inflate fallback counts | false | reject | carried R-B10: the metric counts fallback resolutions, not distinct operations; repeated input-free measurements do not violate its stated contract. |
| N-E1 | CLI advertises a mismatched valid status action | false | reject | carried prior adapter-action rejection: the production server intersects stored status actions with the catalog before the typed client receives them; the formatter does not widen that server result. |
| N-E2 | MCP advertises a mismatched valid status action | false | reject | carried prior adapter-action rejection: the same server permission intersection protects the MCP typed-client path. |
| N-E3 | Known legacy alias casing loses guidance | medium | patch | IsKnown compares legacy aliases without case, but CatalogCode uses a case-sensitive switch; TIMEOUT is recognized then becomes terminal generic denial. Normalize only the recognized alias lookup. |
| N-E4 | Persisted terminal failure loses terminal recovery copy | medium | patch | FailureStateProjectionTranslator accepts Terminal independently of failed_attachment; catalog-only IsTerminal drops the persisted terminal rule. Preserve the narrower persisted terminal state as well as catalog terminality. |
| N-E5 | Nested unknown reason keeps acceptance and retry | high | patch | Same verified root cause as N-B2; the public nested prior outcome is not included in knownReason. |
| N-V1 | Undefined-action tests bypass the formatter branch | medium | patch | Pre-verified gap: the current test also makes reason codes unknown, bypassing SafeAction. Add action-only malformed cases for valid status inputs in both adapters. |
| N-V2 | Optional server reason guards lack isolated tests | medium | patch | Pre-verified gap: isolate each of TerminalReason, FailureReasonCode, TerminalReasonCode and WorkflowLastFailureCode with a known primary reason; current tests mask three guards with other invalid fields. |

Grouped patches: incompatible denial metadata; nested prior-outcome guards (N-B2/N-E5); generic-card metadata; current-user attempt announcements; passive history announcements; demonstrated failure-state mappings; legacy alias casing; persisted terminal display; isolated verification gaps. All fixes use existing inputs and private state; no new public contract or persistence path is required.

All patch findings from this independent review were fixed and verified in 69 focused tests, then in the full 4,083-test affected suites. N-B10 remains the one newly deferred item, with medium impact if verified; prior ledger entries retain their original dispositions.

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| V1 | Failure item lacks a real unknown-code and unsafe-action render check | medium | patch | Existing render tests exercise other components; the failure item is only covered by source-shape checks, leaving its public Razor output unverified. |
| V2 | CLI/MCP status formatters lack undefined-enum fallback assertions | medium | patch | The formatters receive generated enum values and current tests do not exercise cast values outside the defined set. |
| V3 | Unresolved-association blocked state hides an empty conversation's specific guidance | medium | patch | `ChatBotConversationStream` passes localized `EmptyText` and an action, but `ChatBotBlockedState` always substitutes `generic_denial` for this reason. Server-supplied action text must still stay guarded. |
| V4 | Uncoded danger banner hides AssociationReview failure guidance | medium | patch | `AssociationReview` passes localized failure copy without `MessageCatalogCode`; `HasCatalogFailure` changes every uncoded danger banner to the generic denial. |
| B1 | New safe-action buttons have no production `OnNextAction` callbacks | false | reject | The components intentionally render the catalog action disabled with an accessible reason when no handler exists; other call sites already offer the executable retry where it is safe. The acceptance contract permits an action or a disabled reason. |
| B2 | Uncoded danger banners hide projection and correction copy | medium | patch | `GovernedOperations` and `ChatBotAssociationReviewActions` pass local resource text without codes, so the new unconditional danger fallback erases their explanations. Same cause as V4. |
| B3 | Composer announcement key is reused across different failures | medium | patch | The composer supplies a stable id but no announcement key; the banner default is only that id even when `StatusCatalogCode` changes. |
| B4 | AssociationReview emits raw failure token in warning body | false | reject | `AssociationReviewEffects` first applies `AssociationReviewFailureCatalog.SafeCode`; only fixed allowlisted codes reach `FailureBody`, so an arbitrary server token is not interpolated. The uncoded danger copy issue remains V4. |
| B5 | Failure item exposes category, scope, dependency and escalation metadata | medium | defer | The component still displays these projected fields and token filtering only restricts syntax, not sensitivity; these rows already existed at the baseline and this story did not introduce that exposure. |
| B6 | Redaction stage copies supplied correlation/task identifiers | medium | defer | `Apply` accepts arbitrary `ProblemDetails` and copies both IDs; the copy is unchanged from baseline. HTTP correlation middleware validates incoming IDs, but other internal callers need a separate provenance audit. |
| B7 | Unknown secondary status reason can coexist with success/retry | high | patch | `OperationStatusHttpResults` checks only the primary `ReasonCode` when setting completion and actions; secondary code parsers silently emit `generic_denial`. |
| B8 | Unknown stored completion or audit status throws | medium | defer | The generic enum parser throws before serialization, but this parser and those fields existed at baseline; investigate storage migration/forward compatibility separately. |
| B9 | CLI HTTP-status fallback uses codes absent from the catalog | medium | patch | `ReasonCodeForStatus` returns hyphenated values while catalog codes use underscores, so 401/403/validation lose their specific safe action on resolution. |
| B10 | MCP argument-denial codes lose validation guidance | medium | patch | `ChatBotMcpService` emits fixed `mcp.argument.*` codes, but formatter resolves them as unknown generic denials and dismisses them. |
| B11 | Evidence-expired authorization maps to general denial | medium | patch | Factory recognizes `EvidenceExpired` as known but its catalog-code switch falls through to `authorization_denied`, losing refresh guidance. |
| B12 | Accepted association rejection has denied state | low | patch | The catalog reason says the rejection was accepted for recording, while `State` falls through to denied; completion state is the coherent terminal outcome. |
| B13 | Blank localized resources bypass fallback | medium | patch | `FailureCatalogEntry` checks only for null resource values; whitespace headlines or reasons count as translated. |
| B14 | Unknown status/redaction codes are not counted | medium | patch | Only the authorization factory calls `RecordUncategorizedMessage`; the new catalog fallback in redaction and status paths has no shared count. The catalog can count unknown resolution without recording input. |
| E1 | Redaction turns explicit nonretryable problem into retryable | medium | patch | `CoarseUserFacingRedactionStage` derives retry from catalog metadata alone and ignores `problem.Retryable == false`; action must respect the narrower permission too. |
| E2 | Blank resource escapes generic fallback | medium | patch | Same localizer null-only check as B13; verified independently. |
| E3 | Uncoded danger banner loses localized text | medium | patch | Same unconditional `HasCatalogFailure` condition as V4; verified independently. |
| E4 | Uncatalogued blocked state loses reason/action | medium | patch | Same `ChatBotBlockedState` substitution as V3 for localized empty states; verified independently. |
| E5 | Default banner announcement key does not change with code | medium | patch | Same stable-id-only default as B3; verified independently. |
| E6 | Unknown secondary reason leaves known primary success/retry | high | patch | Same primary-only guard as B7; verified independently. |
| E7 | Unknown redaction/status fallback lacks telemetry | medium | patch | Same missing shared counter as B14; verified independently. |

Grouped patch entries: failure-item render verification (V1); enum fallback verification (V2); localized empty blocked state (V3/E4); uncoded trusted banner copy (V4/B2/E3); banner announcement key (B3/E5); secondary status code safety (B7/E6); CLI status mapping (B9); MCP argument mapping (B10); evidence-expired mapping (B11); association rejection state (B12); blank-resource fallback (B13/E2); uncategorized counting (B14/E7); explicit retry permission (E1). B5, B6 and B8 are pre-existing deferred work. B1 and B4 are refuted.

All patch entries were applied and checked in focused tests. Final inspection also caught and fixed an authorization fallback regression: a general catalog code outside the authorization allowlist now becomes `generic_denial`. A gateway test expectation for expired evidence was restored to its baseline result because that gateway path denies earlier for `insufficient-authority`; a direct factory test covers actual `evidence-expired` mapping.

### Resumed Review (2026-10-12)

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| R-B1 | CLI/MCP restore prohibited retries | high | patch | Typed audit_unavailable with Retryable=false and ClientAction=None is overwritten with retry-later; intersect catalog actions with supplied permissions. |
| R-B2 | MCP legacy HTTP and validation codes lose guidance | medium | patch | Untyped 401/403/422 and ArgumentException produce absent hyphenated codes; resolve canonical catalog constants. |
| R-B3 | Failure item ignores per-item retry permission | medium | patch | Item.Retryable=false and action none still render catalog retry guidance; preserve the narrower permission and accessible unavailable reason. |
| R-B4 | Safe-action callbacks are absent in production | false | reject | carried: earlier B1; actions intentionally show focusable disabled reasons without handlers, as allowed by acceptance. |
| R-B5 | Terminal catalog banner retains incompatible state family | medium | patch | Null or BlockedAction family does not acquire terminal semantics for unknown denial; normalize terminal failures except explicitly observed updates. |
| R-B6 | Default catalog banner bypasses deduplication | medium | patch | Null feedback contract short-circuits ShouldAnnounce; coded banners must apply the default deduplication rule. |
| R-B7 | Already-decided/corrected outcomes should be completed | false | reject | GovernedOperationAggregate returns InvalidAssociationCorrection or AssociationDecisionValidation.Invalid for these codes; the current request is denied, rather than completed. |
| R-B8 | CLI/MCP unknown secondary reason preserves success/retry | high | patch | Unknown reason is mapped independently while pending completion and RetryLater persist; deny the whole rendered outcome consistently. |
| R-B9 | UI fallback meter is not exported | medium | patch | UI records fallback measurements but its host metrics registration does not subscribe to the catalog meter. |
| R-B10 | Multiple resolver calls overcount distinct failures | false | reject | Counter is named message_catalog.fallbacks and counts fallback resolutions; neither the contract nor intent requires a once-per-operation failure count. Measurements contain no caller input. |
| R-E1 | CLI ignores supplied retry restriction | high | patch | Same confirmed cause as R-B1 in CLI. |
| R-E2 | MCP ignores supplied retry restriction | high | patch | Same confirmed cause as R-B1 in MCP. |
| R-E3 | MCP fallback status codes are uncatalogued | medium | patch | Same confirmed cause as R-B2. |
| R-E4 | Null banner state repeats terminal announcements | medium | patch | Same confirmed causes as R-B5/R-B6. |
| R-E5 | Fixed blocked-state identity can suppress a later failure | maybe-false | defer | No failure-generation identity is supplied at fixed workspace sites; a live trace distinguishing a new failed attempt from repeated polling is needed to establish incorrect suppression. |
| R-E6 | Undefined adapter reasons coexist with pending success | high | patch | Same confirmed cause as R-B8. |
| R-V1 | Composer submission rejection lacks actual render coverage | medium | patch | Direct banner tests bypass composer wiring and accepted-submission browser tests never reject; add known/unknown composer render checks. |
| R-V2 | MCP HTTP bucket mapping loses actions | medium | patch | Same confirmed cause as R-B2. |
| R-V3 | Both adapters overwrite typed retry restrictions | high | patch | Same confirmed cause as R-B1. |
| R-V4 | Changed markup and meter registration break source tests | medium | patch | Focused native UI tests fail on submission attribute and banner title; Architecture tests fail on old single-meter registration. Update the existing structural assertions and keep behavior covered by actual render tests. |

Grouped patches: adapter permission intersection; MCP canonical codes; failure-item retry permissions; banner terminality and deduplication; complete unknown-status fallback; UI meter registration; composer render verification; stale structural assertions. The implementation subagent from the prior run cannot be continued, so patches are applied in this session.

Full-suite follow-up found and patched additional story regressions: authorization allowlist omitted established disabled/quarantined control reasons; transient authorization factory values marked dependency denials nonretryable; retry eligibility incorrectly used user action presentation instead of the validated workflow schedule; UI blocked-state structural assertions and the root solution inventory assertion still reflected the prior markup and project inventory. Added direct control-denial and transient-retry regressions. New rendering tests explicitly select English and restore the caller culture, avoiding invariant-culture fallback in test setup.

Validation gate corrections: the existing imperative-name allowlist now recognizes the already implemented Grant/Revoke commands. The Dapr ACL and its reviewed route inventory include the current SDK's `/project/rebuild/part/v1` transport endpoint under the existing EventStore-only, verified-identity, POST-scoped rebuild-family grant documented in the policy. These corrections update existing gate expectations and preserve their exhaustive checks.

Browser verification exposed missing button semantics on the Fluent governed-action host; it now declares its button role explicitly, keeps the disabled reason on the focusable host, and preserves the status/alert role when repeated announcements are suppressed. The real-render test host now supplies a consistent authenticated tenant principal to prerender and interactive requests using the existing FrontComposer server helpers. New browser tests seed the supported scoped theme preference, then assert the selected theme, localized known/unknown rejection, keyboard focus, disabled action semantics and accessibility tree in all visual modes. All six existing real-render checks also pass with that host.

## Verification

**Commands:**

- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` followed by `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` — all changed projects compile with warnings as errors.
- Build and run native xUnit v3 assemblies individually for Contracts, Server, UI, CLI, MCP, and UI E2E — relevant suites pass with zero skips.
- Run OpenAPI/client freshness checks if the wire contract changes — schema and generated client agree.

**Results (2026-10-12):**

- Focused native xUnit v3 lanes passed with zero skips after review: Contracts catalog 17, Client generation 26, UI localization 20, CLI 29, MCP 32, Server gateway 398, Server operation status 13, Conformance leakage 6, and real UI render 5 (546 total). The UI E2E project built with `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --configuration Debug -m:1 /nr:false -v:q -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`.
- `git -c core.whitespace=cr-at-eol diff HEAD --check` passed.
- Source-mode solution restore and build passed with zero warnings and zero errors after the root solution included the required sibling projects and FrontComposer stopped emitting unused fields for enum-free StatusOverview projections. FrontComposer generator build and direct xUnit v3 `RazorEmitterExpandInRowTests` passed: 12 tests, zero skips.
- The final unqualified Debug command still fails with 241 errors, chiefly missing EventStore types in Projects and CS1704 in EventStore.Contracts, because EventStore and Parties intentionally default to package dependencies. Local source builds require the explicit solution-wide property above; no warning suppression is needed. Release package-mode restore passed, while its build still stops at pre-existing CS0246 (`BoundWorkloadCommandRequest` in `ChatBotStreamReadProofSource.cs:49`).
- Light/dark, forced-colors, reduced-motion, and full screen-reader visual traversal were not rerun against a live AppHost in this sibling-build follow-up. The earlier real-component render lane covers semantic status, focusable action reason, localized copy, and announcement behavior.

**Final verification from resumed review (2026-10-12):**

- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings and zero errors. Final UI E2E test-host changes also passed `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q`.
- Each affected test project was run independently using `dotnet tests/Hexalith.ChatBot.<Project>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Project>.Tests.dll`. Full suites passed: Contracts 506, Client 91, CLI 39, MCP 52, UI 308, Server 2,571, Conformance 150, Architecture 123, and UI E2E 176: **4,016 tests, zero failures, zero skips**.
- UI E2E includes 12 safe-outcome component render cases, 12 actual Chromium rejection cases across English/French and light/dark/forced-colors with reduced motion, keyboard focus and accessibility-tree checks, and six existing live cross-surface tests. Both known authorization denials and unknown sentinel codes are covered in the new browser matrix.
- `git -c core.whitespace=cr-at-eol diff --check` passed. Test-generated updates to tracked Story 13.9 screenshots were copied to `/tmp/bmad-story-1-4-vsaw4g8h/screenshots` and the historical files were restored to their pre-run contents.
- Full Aspire startup remains environment-blocked: `aspire start --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --no-build --non-interactive --format Json` exited 2 because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` was absent (child exited 134). The actual UI browser tests use a loopback Kestrel host with a deterministic typed-client seam; no live backend or manual screen-reader traversal was claimed.

### Review Findings

- [x] [Review][Patch] Expired evidence dismisses instead of inviting a retry [src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs:247]
- [x] [Review][Patch] Untyped HTTP 404 matches 403, and 409 dismisses [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:399]

- [x] [Review][Patch] High: server status can stay successful when the reason is generic_denial [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:31]
- [x] [Review][Patch] High: waiting outcomes become retry-eligible without a retry action [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:56]
- [x] [Review][Patch] High: non-authorization workspace failures keep an enabled Retry control [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:95]
- [x] [Review][Patch] High: authorization responses drop the specific catalog entry [src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs:187]
- [x] [Review][Patch] Unknown-outcome lifecycle text is `failed` instead of wire `Failed` [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:64]
- [x] [Review][Patch] Fixed announcement keys hide a later failure with a different catalog code [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:124]
- [x] [Review][Patch] Blocked or stale conversations replace trusted local guidance with generic denial [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor:79]
- [x] [Review][Patch] Already-recorded outcomes report state denied [src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs:15]
- [x] [Review][Patch] disabled-action renders as Unavailable [src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs:339]
- [x] [Review][Patch] Failure card prose and button disagree when retry is withheld [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor:78]
- [x] [Review][Patch] Association Review retries unknown failures and prints a code [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:53]
- [x] [Review][Patch] Blocked state heading is not the catalog headline [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor:20]
- [x] [Review][Patch] Composer presents authentication denial as request-access [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:157]
- [x] [Review][Patch] Null problem body skips catalog redaction [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:440]
- [x] [Review][Patch] Undefined status enums are printed as numbers [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:408]
- [x] [Review][Patch] Fallback counter test counts other tests in the process [tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs:101]
- [x] [Review][Patch] Authorization factory requires telemetry it never records [src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs:14]
- [x] [Review][Patch] Accessible description appends the raw action token [src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs:47]
- [x] [Review][Patch] Unknown status still copies retry and duplicate counts [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:89]
- [x] [Review][Patch] Empty conversation and association-review states are not rendered through their parents [tests/Hexalith.ChatBot.UI.E2E.Tests/VersionedSafeOutcomeRenderTests.cs:181]
- [x] [Review][Patch] Page failure banners are not rendered with a sentinel error [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:52]
- [x] [Review][Patch] Reason-only blocked states are not checked for a server action [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:116]
- [x] [Review][Patch] Disabled composer states are not rendered [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:152]
- [x] [Review][Patch] Unknown MCP argument denials do not assert category [tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs:374]
- [x] [Review][Patch] Redaction, status, and UI-host fallback counts are not measured [src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs:13]

- [x] [Review][Defer] Unknown completion or audit text still throws [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:29] — deferred: pre-existing parser; the fallback state still needs the schema and forward-compatibility decision already on the deferred-work ledger.
- [x] [Review][Defer] Failure items still render category, scope, dependency, and escalation fields [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor:45] — deferred: pre-existing projected fields; already deferred from the earlier Story 1.4 review.
- [x] [Review][Defer] Story commit advances EventStore, FrontComposer, and Memories gitlinks [references/Hexalith.EventStore:1] — deferred: maybe-false, medium if the move is unnecessary. Settle by building this story against the previous submodule commits `1b7b8ce`, `c7491c7`, and `b16ab01`.

#### Rejected

- `false` — CLI and MCP throw when `SafeNextActions` is null. The generated property defaults to an empty list, and `OperationStatusHttpResults.ToWire` always assigns a list.
- `false` — A retryable catalog code drops the failure-item alert role. Catalog `IsTerminal` is the specified terminality, so a retryable entry correctly uses `status`.
- `false` — `CommandGatewayHttpResults` constructs `CoarseUserFacingRedactionStage` directly. That class is stateless and is the only registered implementation, so `Apply` matches the scoped service.
- `false` — A known status whose stored actions do not match the catalog action returns an empty action list. That list is the stored-permission intersection; it does not invent a dismiss action.
- `false` — CLI and MCP forward unsupported status actions. The server already filters `SafeNextActions` to the catalog action before those formatters print the list.
- `false` — Banner and blocked-state buttons show catalog `retry-later` without an instance flag. Those components have no per-instance retry input, and the catalog action is the action the spec requires. The separate always-enabled workspace Retry control is a different finding.
- `false` — The spec frontmatter says `done` while sprint status says `review`. That is this review's starting state, not a product defect.
- `low` — A local empty blocked state can render a blank reason when the caller passes a blank string. Current callers pass non-empty resources, and the fix would add a fallback branch.
- `low` — A whitespace catalog code is treated as present. Callers pass real codes, and the fix would add a guard.
- `low` — The commit message says CLI tests now expect `generic_denial` instead of specific codes. `ChatBotCliCommandTests` still expects `authorization_denied` and `command_contract_invalid`, and correcting the message would rewrite history.
- `decision` — Coded banners stay catalog-only. A second child channel would keep the why-panel's unauthorized sentence on other failures, and raw error children must stay hidden.
- `decision` — CLI and MCP fallback counts stay in process. Those hosts have no metrics pipeline, and a one-shot process is not a scrape target. Server and UI already export the meter.

#### Src slice (2026-10-12)

Acceptance Auditor and Edge Case Hunter were interrupted and filed nothing. Blind Hunter and Verification Gap reviewed `src` only (`84379006...80853fd`).

- [x] [Review][Patch] AI invalidation stores retry-later so it matches the blocked-context catalog action [src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs:204]
- [x] [Review][Patch] Expired-evidence copy states that the decision stopped, and the action stays dismiss [src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs:246]

- [ ] [Review][Patch] Collapsed CLI and MCP status still prints the original retry count, and MCP drops a known code when the problem category is null [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:85]
- [ ] [Review][Patch] A generic denial is labeled authorization, and an unknown 401 or 503 becomes a 403 authorization dismissal [src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs:29]
- [ ] [Review][Patch] A catalog banner replaces an explicit blocked or degraded family with terminal policy failure [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:129]
- [ ] [Review][Patch] Already-submitted idempotency conflicts still report state denied [src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs:48]
- [ ] [Review][Patch] The banner renders a disabled catalog action beside the live retry [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:81]
- [ ] [Review][Patch] Historical failure items use role alert, and the stream never announces them [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor:10]
- [ ] [Review][Patch] Four French catalog sentences do not carry the English meaning [src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx:975]
- [ ] [Review][Patch] One render increments the fallback counter once per catalog property access [src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs:373]
- [ ] [Review][Patch] PascalCase Failure_Catalog resources are no longer read [src/Hexalith.ChatBot.UI/Localization/SharedResource.resx:538]
- [ ] [Review][Patch] Local transport sentinels are unknown codes, so load failures become terminal generic denials and count as fallbacks [src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs:16]
- [ ] [Review][Patch] Operational dashboards load failures are not rendered with a non-catalog error [tests/Hexalith.ChatBot.UI.E2E.Tests/VersionedSafeOutcomeRenderTests.cs:478]
- [ ] [Review][Patch] Association-review legacy aliases are not rendered on the page [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:53]
- [ ] [Review][Patch] Why-panel failures are not rendered [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:201]
- [ ] [Review][Patch] Workspace authorization codes are not rendered [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:62]
- [ ] [Review][Patch] CLI 408 and 429 denials are outside the status-bucket tests [tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs:552]
- [ ] [Review][Patch] CLI and MCP denial tests do not assert headline or catalog version [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:329]
- [ ] [Review][Patch] The server host does not prove the fallback meter is exported [src/Hexalith.ChatBot.Server/Program.cs:74]

##### Rejected (src slice)

- `false` — Treating `generic_denial` as an unknown status and collapsing that GET is the closed fix for a successful generic-denial status. Unknown completion and audit text still throwing is already deferred.
- `false` — CLI and MCP do not export the fallback meter. That was rejected: those hosts have no metrics pipeline.
- `false` — The catalog banner drops child content, including the association-review load sentence. That was rejected: coded banners stay catalog-only, and the child template interpolates the raw code.
- `false` — A later failure with the same catalog code is not announced again. The previous patch added the catalog code to the key so a different code still announces; repeating the same code is the specified dedup.
- `false` — Mapping HTTP 404 to `authorization_denied` hides not-found. The epic requires missing and forbidden to be indistinguishable, and the earlier patch accepted that mapping.
- `false` — `conflict` no longer offers retry. It maps to `invalid_lifecycle_transition`, whose catalog action is none.
- `false` — MCP replaces `SafeSuggestion` with the action token. The spec requires metadata-only output rather than caller prose.
- `false` — Locales other than English and French, and a missing resource, become generic denial and an input-free count. That is the specified fallback.
- `false` — The failure-reason row shows the catalog code. Public surfaces must not keep a raw reason code.
- `false` — An unknown workspace error loses its retry button. Unknown codes are specified to be terminal and non-retryable.
- `false` — This src diff contains no test updates. The story range does; the concrete gaps are the patch items above.

### Verification of remaining review patches (2026-10-12)

All 25 pending patch findings were implemented and verified. The full Debug source build passes with zero warnings and errors using `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false`; its matching source-mode restore also passed. Each affected native assembly ran independently with `-failSkips -noColor -result-trx <lane>-final.trx -result-ctrf <lane>-final.ctrf.json`.

| Suite | Passed | Failed / skipped |
| --- | ---: | ---: |
| Contracts | 511 | 0 / 0 |
| Client | 91 | 0 / 0 |
| CLI | 47 | 0 / 0 |
| MCP | 55 | 0 / 0 |
| UI | 309 | 0 / 0 |
| Server | 2,578 | 0 / 0 |
| Conformance | 150 | 0 / 0 |
| Architecture | 123 | 0 / 0 |
| UI E2E | 190 | 0 / 0 |
| Total | 4,054 | 0 / 0 |

Current machine results and build logs are in `/tmp/bmad-story-1-4-pvn5ey9g`. Test-generated screenshots were retained there separately; the 42 historical tracked screenshots were restored to their pre-run bytes. The 26 actual component-render cases and 12 Chromium rejection cases passed. Initial harness/analyzer failures were repaired; stale expectations were aligned to canonical lifecycle values and specific catalog reasons. Positive retry clock/schedule tests now supply a retryable catalog reason and explicit stored retry permission, while separate waiting-state tests reject invented permission.

| Frozen matrix row | Passing evidence in this run |
| --- | --- |
| Known outcome | `EveryCatalogEntryHasCompleteSafeMetadata`; both `EveryCatalogCodeShouldHaveCompleteLocaleSpecificCopy` cases |
| Unknown or untranslated | `UnknownCatalogCodesResolveToTerminalGenericDenial`; `UnavailableLocaleShouldResolveToGenericTerminalCopy`; CLI/MCP `UnknownStatusReasonShouldDenyEntireOutcome` |
| Public failure | `RedactionStageShouldReplaceUntrustedProblemProseAndActions`; `ArbitraryMcpArgumentCodeShouldRemainGenericDenial`; `PageFailureShouldHideSentinelAndWithholdRetry`; input-free boundary and UI-host metric tests |
| State transition | `UnknownCodeShouldRenderLocalizedTerminalFallbackAndAnnounceOnlyOnce`; `WorkspaceFailureShouldOnlyEnableCatalogRetry`; all 12 `RejectedComposerShouldRemainSafeAndAccessibleAcrossVisualModes` cases |

The exact-target Aspire startup check remains blocked: `aspire start --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --no-build --non-interactive --format Json` exits 2 (child exit 134), missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret`. Browser tests use the existing loopback Kestrel typed-client seam. They do not establish live backend topology or manual screen-reader traversal. Story completion remains subject to the repository story-evidence gate; this run does not claim an attested completion transition.

### Final verification after independent review (2026-10-12)

- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings and errors after all review patches.
- All nine full native suites passed independently with `-failSkips -noColor -result-trx <lane>-review-final.trx -result-ctrf <lane>-review-final.ctrf.json`. Results and build logs are retained in `/tmp/bmad-story-1-4-pvn5ey9g`.

| Suite | Passed | Failed / skipped |
| --- | ---: | ---: |
| Contracts | 513 | 0 / 0 |
| Client | 91 | 0 / 0 |
| Cli | 50 | 0 / 0 |
| Mcp | 58 | 0 / 0 |
| UI | 314 | 0 / 0 |
| Server | 2,590 | 0 / 0 |
| Conformance | 150 | 0 / 0 |
| Architecture | 123 | 0 / 0 |
| UI.E2E | 194 | 0 / 0 |
| Total | 4,083 | 0 / 0 |

The UI E2E suite includes 29 real component-render cases and 13 Chromium cases for safe outcomes. New regressions isolate incompatible acceptance/category metadata, malformed nested outcomes, all four optional reason guards, undefined adapter actions, contradictory generic-card retry/success fields, passive versus interactive announcements, repeated same-code submissions, terminal retry restrictions, and case-insensitive legacy aliases. All frozen matrix rows remain covered. The 42 generated historical screenshots were preserved under `screenshots-review-generated` in the results directory and restored to their original bytes.

Story-evidence gate self-tests passed independently: 219 tests, zero failures or skips. The completion preflight command below exits 1 and reports `passed: false`, reason `scope_digest_mismatch`, subject `1-4-return-versioned-safe-outcomes.json`, because Story 1.4 has no evidence contract:

```bash
dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll validate --story _bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md --contract _bmad-output/implementation-artifacts/evidence/1-4-return-versioned-safe-outcomes.json --target-status done --base 8437900622d5bf7a9d34ca5cc543cd802f653cb1 --head 08fc07d61559cf93e3ef03a7f00bcf1d37d1a073 --results /tmp/bmad-story-1-4-pvn5ey9g --report /tmp/bmad-story-1-4-pvn5ey9g/story-evidence-preflight.json
```

The repository's required completion gate takes precedence over the workflow's done transition. The spec remains `in-review` and the sprint entry moves to `review`. Completion requires the story evidence contract, exact scope and assertion mappings, attested machine evidence and any triggered primary lanes, followed by a passing gate report; the Debug suite pass does not replace that preflight. Normal AppHost startup also remains blocked by the missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret` key described above. No successful live AppHost startup or manual screen-reader run is claimed.

## File List

- `Hexalith.ChatBot.slnx`
- `README.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml`
- `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`
- `src/Hexalith.ChatBot.UI/Hosting/ChatBotUiHostDefaultsExtensions.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewFailureCatalog.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantLeakageScannerTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/SafeOutcomeFallbackMetricTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/SafeOutcomeProblemCompatibilityTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationProgressReadTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/OperationStatusWorkflowStatusSinkTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/FakeChatBotClient.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/LiveChatBotUiHost.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/RealRenderCrossSurfaceE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeRenderDispatcher.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeRenderState.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeTestAuthenticationStartupFilter.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/VersionedSafeOutcomeBrowserTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/VersionedSafeOutcomeRenderTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewFailureCatalogTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/SafeOutcomeHostMetricTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/SafeOutcomeMetricExporter.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
