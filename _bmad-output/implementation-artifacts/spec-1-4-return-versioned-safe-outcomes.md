---
title: 'Return Versioned Safe Outcomes'
type: 'feature'
created: '2026-10-12'
status: 'done'
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
