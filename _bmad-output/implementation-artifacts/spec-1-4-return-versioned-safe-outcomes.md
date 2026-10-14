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

- [x] `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs`, `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs`, `docs/story-evidence-integrity.md` — accept the supported `in-review` base spelling while retaining exact sprint review and rejecting unreviewed, missing or terminal base states; user-authorized completion prerequisite.

**Acceptance Criteria:**

- Given every code, when English and French entries are checked, then version, state, terminality, action, disabled reason, accessible description, headline limit, and one-sentence copy agree.
- Given any unknown, malformed, or untranslated code, when a public boundary resolves it, then the generic result is terminal, existence-neutral, and non-retryable, and raw input is absent from output and telemetry.
- Given refused, blocked, degraded, failed, denied, waiting, retryable, or unsupported outcomes, when API, UI, CLI, or MCP presents them, then catalog copy and actions contain no restricted sentinel across response, render, or telemetry.
- Given a UI transition in light, dark, forced-colors, reduced-motion, keyboard-only, and screen-reader use, when status renders, then state stays inline, text and semantic status identify it, deduplicated feedback announces the transition, and action or disabled reason is accessible.

## Implementation Notes

- The catalog now contains 94 entries including the terminal `generic_denial` fallback; OpenAPI v1 and the generated client gained the previously omitted codes without renumbering existing enum members.
- English and French resources cover every catalog entry. Server Problem Details and status results, UI failure components, CLI, and MCP resolve public copy and actions through the catalog. Unknown codes suppress caller prose, disallow retry, and expose only a dismiss action.
- Operation status intersects catalog actions with persisted per-operation permissions. Actual Razor render tests cover known and unknown localized failures, semantic status, announcement deduplication, and disabled action reasons.
- No persistence path, dependency version, or public wire version changed. The additive OpenAPI code enum required client regeneration and a refreshed client hash fixture.
- Review fixes preserve trusted local empty-state and banner guidance, make every unknown status reason terminal, respect explicit no-retry decisions, and count unknown or untranslated fallback through an input-free metric.
- Localization scope (review decision 2026-10-14): English/French copy is resolved at the UI boundary. API Problem Details, CLI and MCP expose the stable catalog code and version with English default copy; clients localize by code.

## Spec Change Log

- 2026-10-14: Resumed all three workflow review layers, applied four patch groups, and added the user-authorized reviewed-status gate compatibility prerequisite. Completion evidence uses the current refinement diff from `c8da8a5b3aeaf057586aacd30f9c3e5e7bf37947`; the original implementation remains committed at that base.

- 2026-10-14: Completed the three-layer workflow review, fixed ten grouped findings, and verified 4,367 tests with zero failures/skips. Preserved quiet observer feedback, recorded two pre-existing UI-state deferrals, and retained review status pending completion evidence.

- 2026-10-14: Closed all 17 latest review patches, verified 4,334 affected tests with zero failures/skips, and audited passing matrix assertions. Formal completion remains subject to the repository evidence gate and its exact scope/primary-lane requirements.

- 2026-10-13: Completed the workflow review, applied six follow-up patch groups, and verified all nine affected suites (4,164 tests, zero failures/skips). Kept the story in review because the repository completion evidence gate still fails for the absent contract.
- 2026-10-13: Closed the four contracts and server review patches. Policy blocks now retain escalation guidance, generic denials use `about:blank`, and catalog/OpenAPI/generated enum parity is checked. The five affected suites passed 3,487 tests; completion evidence remains outstanding.
- 2026-10-13: Closed the 17 remaining source-slice patches and the follow-up independent review. Preserved specific idempotency conflicts while denying unrelated success codes, added a mounted stream transition test, and verified 4,111 affected tests. Completion evidence remains outstanding.
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

### Workflow review of the resumed build (2026-10-13)

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| D-B1 | Explicit generic denial loses dismiss when an action mismatches | medium | patch | Redaction treats a known generic_denial as an ordinary permission intersection; RetryLater becomes None instead of the generic fallback's Dismiss. Normalize explicit and resolved generic denials identically. |
| D-B2 | Failure and blocked surfaces admit unrelated acceptance codes | high | patch | FailureStateProjectionTranslator accepts any known catalog code, and both renderers select command_accepted copy directly. Reject incompatible success codes while retaining the existing failure-history lifecycle outcomes. |
| D-B3 | Known terminal failure displays a resolved status | medium | defer | The baseline already displayed Item.FailureStatus alongside the existing terminal_failure headline and reason. The contradiction predates this catalog expansion; reconcile status/code producer semantics separately. |
| D-B4 | Workspace duplicates live announcements of one failure | medium | patch | Its banner and blocked state now render the same catalog explanation under different keys. Their existing OncePerFailureKey rules can share one key so only one announces the transition. |
| D-B5 | Association review suppresses a later same-code attempt | medium | defer | The baseline already used association-review-failed plus the unchanged error code, without association or attempt identity; new catalog normalization preserves that pre-existing same-code suppression. |
| D-B6 | Unknown stored completion or audit values throw | medium | defer | carried B8/N-B12: the pre-existing enum parser and the logged storage forward-compatibility decision are unchanged. |
| D-B7 | Adapter status omits primary catalog copy | medium | defer | carried C-B6: the pre-existing output shape still needs the separately recorded output-contract decision. |
| D-B8 | Failure card exposes optional projected metadata | medium | defer | carried B5/C-B13: the pre-existing token-shaped projected fields still need the recorded producer-provenance audit. |
| D-B9 | Release source inventory expands | maybe-false | defer | carried N-B10/C-B11: no exact before/after Release graph has established an expansion beyond the existing AppHost closure. |
| D-B10 | Accessible-tree checks do not prove manual screen-reader delivery | false | reject | Component and browser tests assert live activation, roles, reachable actions and accessible text; the spec explicitly disclaims manual screen-reader traversal and attested completion. The concrete changed-attempt verification gap is D-V2. |
| D-V1 | Primary stored lifecycle guard has no isolated server regression | medium | patch | Pre-verified: undefined nested lifecycle and adapter cases do not exercise the primary stored lifecycle guard. Add an otherwise-valid record with only that primary value undefined. |
| D-V2 | Same-code attempt test checks keys but not live activation | medium | patch | Pre-verified: replacing ShouldAnnounce's argument with stable identity plus code leaves its displayed key assertions passing. Mount a banner and assert both fresh attempts announce while a repeat stays quiet. |
| D-V3 | Browser tests miss ordinary CI's installed Chrome | medium | patch | Reproduced with CHROME_EXECUTABLE_PATH unset and an empty Playwright cache. Ordinary CI provides system Chrome, which RealRenderFixture already discovers; reuse that discovery in these tests. |
| D-V4 | Failure card renders command acceptance inside terminal failure | high | patch | Same verified root cause as D-B2; the projection's catalog-membership guard does not reject acceptance codes. Preserve legitimate retry_queued and reprocess_created history. |
| D-E1 | Mixed-case terminal kind can re-enable retry | false | reject | The supported server projection takes FailureStateKind as a closed enum and ProjectConversationItemView emits the canonical terminal-failure token; it cannot emit TERMINAL-FAILURE. Existing canonical/PascalCase inputs and terminal FailureStatus remain guarded. |
| D-E2 | Retry text advertises a withheld action | medium | defer | carried final contracts/server review deferral: the same RetryableText expression still reads the stored flag independently of the action intersection. The existing ledger entry is retained. |
| D-E3 | A later workspace retry uses consumed keys | maybe-false | defer | carried R-E5: fixed workspace keys and the original live attempt-identity follow-up are unchanged. This review traces RetryLoad clearing ErrorCode and replacing CurrentLoadRequestId; retain that evidence for the previously deferred mounted/live retry check. |

Grouped patches: generic-denial action parity; failure/blocked compatibility (D-B2/D-V4); workspace duplicate announcements; primary lifecycle regression; mounted same-code attempt regression; shared browser discovery. Carried findings retain their existing disposition without new duplicate ledger entries.

### Workflow review of the 2026-10-14 continuation

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| F-B1 | Paginated insertion reuses another failure's announcement text | high | patch | The new after-render summary is component state, and the stream's list entries lack keys. The compiled mounted probe prepended history and moved existing summaries to different item IDs; key the existing list entries and verify the actual lifecycle. |
| F-B2 | Explicit task cancellation without a canceled token becomes a timeout | medium | patch | `TaskCompletionSource.SetCanceled()` produces a canceled task whose exception token can be unset. The new heuristic returns true; require the demonstrated positive timeout cause and retain explicit cancellation. |
| F-B3 | Shared UI mapper preserves aliases that other callers cannot localize | medium | patch | The shared mapper returns recognized `TIMEOUT`, while the workspace localizer requires a canonical catalog code and returns generic denial. Normalize recognized aliases at the shared mapper. |
| F-B4 | Code-only UI failures lose backend retry restrictions | medium | defer | The baseline effects already reduced typed problems to `Result.Code`, and the baseline workspace enabled non-authorization retries without preserving instance permissions. Carrying those permissions requires a separate UI-state decision; this transport refactor preserves the pre-existing omission. |
| F-B5 | Code-only UI failures lose category validation | medium | defer | The baseline effects also discarded problem category before public rendering. The new helper consolidates that pre-existing behavior; define malformed metadata handling at this UI state boundary separately. |
| F-B6 | Concise live summary omits explanation and recovery guidance | medium | patch | The newly announced `AccessibleLabel` contains only headline, status and timestamp, while the old atomic article included its reason and action. Include localized reason/action and any unavailable guidance in the concise summary. |
| F-B7 | Unknown status fallback violates maxAttempts minimum | medium | patch | The explicit unknown-reason branch emits zero, while authoritative OpenAPI requires at least one. This is a demonstrated public fallback input covered by the frozen matrix; emit one while retaining failed state, dismiss and false retry eligibility. |
| F-B8 | Recursive role assertion cannot establish article semantics | false | reject | `UnknownBlockedAccessibleLabelShouldUseVisibleHeadline` separately inspects the actual rendered article tag and rejects role, aria-live and aria-atomic. The mounted assertion checks the announcement role; article semantics already have direct coverage. |
| F-B9 | Changed effect callers lack timeout verification | medium | patch | The load-only matrix cannot detect independently restored unconditional cancellation catches in submission, Stop, polling and why-panel consumers. Add focused caller regressions, including accepted-mutation preservation and real reducer state for Stop. |
| F-B10 | Repeated browser submission lacks live activation assertion | medium | patch | Its real Fluxor/browser path checks keys and text while mounted component tests use a deterministic callback. Assert actual browser live activation for each repeated attempt. |
| F-E1 | Stored completion or audit text throws before reason fallback | medium | defer | carried B8/D-B6: the same pre-existing parser and storage forward-compatibility decision remain recorded; no duplicate deferral or patch. |
| F-E2 | CLI emits catalog-incompatible defined status actions | false | reject | carried N-E1/C-E5: the production endpoint intersects persisted actions with the catalog before the typed client formatter sees them; a hand-built contradictory status bypasses that guard. |
| F-E3 | MCP emits catalog-incompatible defined status actions | false | reject | carried N-E2/C-E6: the same production status intersection protects this typed-client path. |
| F-E4 | UI failures lose explicit no-retry permission | medium | defer | Same verified pre-existing omission as F-B4; group under one new deferral. |
| F-E5 | Generic denial retains DependencyDegraded semantics | medium | patch | The render probe supplies an unknown code and explicit DependencyDegraded family; the terminal generic copy still receives status/polite semantics. Normalize generic denial consistently while preserving specific known families. |
| F-E6 | Missing locale can leave Association Review retry enabled | medium | patch | The localizer deliberately returns terminal generic denial for an unavailable locale, but this control uses the non-localized catalog. The frozen missing-locale fallback must govern the permitted action too. |
| F-E7 | Deleted composer resource remains required by no-browser test | medium | patch | The focused existing test fails at `ProjectConversationE2ETests.cs:8173` with Chrome intentionally unavailable. Update its source assertion for the approved catalog-based composer behavior. |
| F-V1 | Stop timeout can leave cancellation pending without coverage | medium | patch | Pre-verified: restoring the old unconditional cancellation catch passes existing Stop tests but skips the failure reducer. Add both timeout shapes and assert pending clears with dependency_degraded through the actual reducer; group with F-B9. |

Grouped patches: keyed pagination; positive timeout evidence; canonical aliases; complete concise announcements; schema-valid fallback attempts; caller-specific timeout verification (F-B9/F-V1); browser activation; generic terminal semantics; localized retry permission; no-browser cleanup assertion. All use existing inputs, private state or tests and introduce no public contract. Prior carried findings retain their dispositions.

### Completion review and status compatibility (2026-10-14)

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| G-B1 | Current banners retain supported retry/duplicate/reprocess feedback | false | reject | The shared resolver deliberately preserves these lifecycle outcomes; actual render regressions cover the failure card, blocked state and banner. Production refused Problem Details rejects waiting/completed codes before the typed UI client receives them; no failed-read producer emitting this code was demonstrated. |
| G-B2 | Dashboard Refresh remains enabled after terminal or untranslated denial | medium | patch | The header checks loading alone and can dispatch a fresh read after generic denial. Intersect Refresh with the localized retry action and verify the actual control cannot dispatch for unknown, authorization or unavailable-locale failures. |
| G-B3 | Governed Operations attempt keys collide after remount | medium | patch | Its newly introduced local counter restarts while circuit deduplication survives. Use a private instance identity for fresh submissions, preserve initial-load deduplication, and assert mounted live activation and repeat suppression. |
| G-B4 | Adapter transport failures lose known dependency guidance | medium | patch | HttpRequestException and TimeoutException reach the generic formatter fallback while bodyless dependency HTTP failures and UI transport failures retain retry guidance. Map these demonstrated transport classes to dependency_degraded and verify catalog copy, retry action and absence of exception prose. |
| G-B5 | UI code reduction discards per-instance retry restrictions | medium | defer | carried F-B4/F-E4: the baseline state already omitted instance permissions; the recorded separate UI-state decision remains required. |
| G-B6 | UI code reduction discards category validation | medium | defer | carried F-B5: the baseline reduction already omitted category, and the existing deferred state-boundary decision is unchanged. |
| G-B7 | Failure-card Retryable text disagrees with withheld action | medium | defer | carried D-E2 and the contracts/server retry-text deferral: the stored flag remains independent of the action intersection, as it did before this story. |
| G-B8 | Workspace and Association Review reuse fixed failure keys | medium | defer | carried R-E5/D-E3/D-B5: fixed keys and the existing attempt/route identity follow-ups remain; no duplicate ledger entry or patch. |
| G-B9 | Generic failure card renders optional projected metadata | medium | defer | carried B5/C-E7: the pre-existing projected-field provenance audit remains deferred. |
| G-B10 | Unknown stored completion or audit text throws | medium | defer | carried B8/D-B6: the pre-existing storage forward-compatibility decision remains deferred. |
| G-V1 | Retry waiting classifications lack denial-boundary regression coverage | medium | patch | Pre-verified: removing RetryQueued/RetryAccepted from the waiting switch leaves prior tests passing while three denial consumers admit queued/accepted copy. Pin waiting classification and add both codes to the existing server, CLI and MCP denial theories. |

The edge-case layer returned no findings. Four patch groups were applied in this session; carried findings retain their recorded disposition. The user additionally authorized a narrowly tested gate compatibility fix: product completion accepts reviewed story spellings `review` and `in-review`, while requiring an exact `review` sprint base and preserving all source, result, provenance and mapping checks.

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

- [x] [Review][Patch] Collapsed CLI and MCP status still prints the original retry count, and MCP drops a known code when the problem category is null [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:85]
- [x] [Review][Patch] A generic denial is labeled authorization, and an unknown 401 or 503 becomes a 403 authorization dismissal [src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs:29]
- [x] [Review][Patch] A catalog banner replaces an explicit blocked or degraded family with terminal policy failure [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:129]
- [x] [Review][Patch] Already-submitted idempotency conflicts still report state denied [src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs:48]
- [x] [Review][Patch] The banner renders a disabled catalog action beside the live retry [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:81]
- [x] [Review][Patch] Historical failure items use role alert, and the stream never announces them [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor:10]
- [x] [Review][Patch] Four French catalog sentences do not carry the English meaning [src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx:975]
- [x] [Review][Patch] One render increments the fallback counter once per catalog property access [src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs:373]
- [x] [Review][Patch] PascalCase Failure_Catalog resources are no longer read [src/Hexalith.ChatBot.UI/Localization/SharedResource.resx:538]
- [x] [Review][Patch] Local transport sentinels are unknown codes, so load failures become terminal generic denials and count as fallbacks [src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs:16]
- [x] [Review][Patch] Operational dashboards load failures are not rendered with a non-catalog error [tests/Hexalith.ChatBot.UI.E2E.Tests/VersionedSafeOutcomeRenderTests.cs:478]
- [x] [Review][Patch] Association-review legacy aliases are not rendered on the page [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:53]
- [x] [Review][Patch] Why-panel failures are not rendered [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:201]
- [x] [Review][Patch] Workspace authorization codes are not rendered [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:62]
- [x] [Review][Patch] CLI 408 and 429 denials are outside the status-bucket tests [tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs:552]
- [x] [Review][Patch] CLI and MCP denial tests do not assert headline or catalog version [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:329]
- [x] [Review][Patch] The server host does not prove the fallback meter is exported [src/Hexalith.ChatBot.Server/Program.cs:74]

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

### Independent review of 2026-10-13 continuation

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| C-B1 | Outbound duplicate send is completed | medium | patch | The aggregate rejects the second send; the catalog's completed state can describe the refused current request as success. |
| C-B2 | Invalidated AI proposal suggests retry | false | reject | Retry-later refers to waiting for corrected context propagation; the AI outcome row has no callback that retries the invalidated proposal. |
| C-B3 | Unexpected UI exceptions advertise dependency retry | medium | patch | Broad exception handlers use dependency_degraded even for programming failures with no established retry permission. |
| C-B4 | Denial surfaces accept other catalog success codes | high | patch | Redaction and CLI/MCP guards exclude command_accepted alone; association_decision_accepted reaches denial copy. Known idempotency conflicts must retain their specific denial code. |
| C-B5 | Unknown MCP tool loses correct-request guidance | medium | patch | UnknownTool emits mcp.tool.unknown with correct-request, but the formatter does not map this fixed code. |
| C-B6 | Status formatters omit primary reason copy | medium | defer | The omission predates this story; adding catalog fields to both public status shapes needs a separate output-contract decision. |
| C-B7 | Status formatters pass incompatible actions | false | reject | carried N-E1/N-E2: the production status endpoint intersects persisted actions with catalog permission before either typed client receives them. |
| C-B8 | Generic composer denial keeps BlockedAction family | medium | patch | The terminal generic fallback should carry terminal-policy semantics while a specific blocked code retains its explicit family. |
| C-B9 | Blocked and stale composer gets dependency retry copy | medium | patch | The workspace passes degraded for all three context states and the new composer catalog mapping advertises dependency retry for blocked and stale. |
| C-B10 | Localizer cache undercounts repeated fallback | false | reject | A cache hit does not run another catalog resolution; the meter counts underlying fallback resolutions without raw input, while cache bounds work per scope. |
| C-B11 | Release solution graph expands | maybe-false | defer | carried N-B10: compare exact Release project graphs before and after the source inventory edit. |
| C-B12 | Unknown stored completion or audit text throws | medium | defer | carried B8: the parser predates this story and needs a storage forward-compatibility decision. |
| C-B13 | Failure card exposes optional projected metadata | medium | defer | carried B5: the fields existed before this story and still require a provenance audit. |
| C-B14 | Redaction copies correlation and task IDs | medium | defer | carried B6: copying IDs predates this story and needs a provenance audit for non-HTTP callers. |
| C-E1 | Server denial accepts a completed code | high | patch | Same verified cause as C-B4 at the redaction boundary. |
| C-E2 | CLI denial accepts a completed code | high | patch | Same verified cause as C-B4 in the CLI formatter. |
| C-E3 | MCP denial accepts a completed code | high | patch | Same verified cause as C-B4 in the MCP formatter. |
| C-E4 | Failure banner accepts a catalog success code | medium | patch | A danger or warning banner currently renders waiting or completed catalog prose as failure guidance. |
| C-E5 | CLI status action mismatch | false | reject | carried N-E1: server-side action intersection prevents this mismatch on the typed-client path. |
| C-E6 | MCP status action mismatch | false | reject | carried N-E2: the same server-side intersection protects the MCP typed-client path. |
| C-E7 | Generic failure card exposes optional metadata | medium | defer | carried B5: this is the pre-existing projected-field provenance issue. |
| C-E8 | Repeated dashboard refresh failure stays silent | medium | patch | The user can refresh twice and the fixed code-based key suppresses the second attempt's failure announcement. |
| C-E9 | Invalidated AI proposal retry guidance | false | reject | Same refutation as C-B2: retry-later waits for context propagation and does not re-execute the invalidated proposal. |
| C-V1 | Current-page stream arrival lacks transition test | medium | patch | The direct card test bypasses the stream Announce parameter, so a new current-page failure could remain silent undetected. |

### Verification of source-slice review patches and independent review (2026-10-13)

- The final Debug source build passed with zero warnings and errors: `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q`.
- Full native suites passed independently: Contracts 516, Client 91, CLI 53, MCP 60, UI 320, Server 2,595, Conformance 150, Architecture 123, and UI E2E 203: **4,111 tests, zero failures or skips**. The final UI E2E run included 38 safe-outcome render cases and 13 Chromium cases.
- The server host exporter test proves the actual meter provider emits a tag-free catalog fallback. CLI/MCP tests verify collapsed retry counts, catalog headlines/versions, and rejection of unrelated success codes. Parent renders verify dashboard, association aliases, why-panel, workspace authorization, and catalog success codes inside failure banners.
- `StreamShouldAnnounceNewCurrentPageFailureOnceAndKeepHistoryQuiet` mounts a stream, adds a new current-page failure and old history after the initial render, then proves the new item announces once while history stays quiet. Repeated dashboard refreshes now use an attempt-specific announcement key.
- Known idempotency conflict problems retain their specific codes and safe copy; an outbound duplicate send remains a denied current request. A non-catalog exception now produces terminal generic denial instead of implying dependency retry.
- The historical Story 13.9 screenshots were restored to their pre-run bytes; generated copies are in `/tmp/chatbot-story-1-4-final-generated-screenshots`. The completion evidence contract and live AppHost secret limitations described above remain outstanding.
- The follow-up completion preflight `dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll validate --story _bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md --contract _bmad-output/implementation-artifacts/evidence/1-4-return-versioned-safe-outcomes.json --target-status done --base 8437900622d5bf7a9d34ca5cc543cd802f653cb1 --head 3fd47d3de1bf8efe5434948d53fdd917d625a412 --results /tmp/chatbot-story-1-4-final-generated-screenshots --report /tmp/chatbot-story-1-4-evidence-preflight.json` exited 1 with `scope_digest_mismatch` for the absent Story 1.4 evidence contract. The spec stays `in-review` and sprint status stays `review`; no attested completion is claimed.

### Verification of contracts and server review patches (2026-10-13)

- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q` passed with zero warnings or errors. The final client test correction also passed `dotnet build tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q`.
- The focused gateway and problem compatibility lane passed 424 tests. Full native assemblies then passed independently with `-failSkips -noColor -result-trx <lane>.trx -result-ctrf <lane>.ctrf.json`. Final results are retained in `/tmp/chatbot-story-1-4-contracts-server-20261013`; use `client-final.trx` and the four other `*-full.trx` reports. The initial client test used default enum serialization; the corrected assertion uses the same string enum converter as the generated wire properties.

| Suite | Passed | Failed / skipped |
| --- | ---: | ---: |
| Contracts | 517 | 0 / 0 |
| Client | 92 | 0 / 0 |
| Server | 2,605 | 0 / 0 |
| Conformance | 150 | 0 / 0 |
| Architecture | 123 | 0 / 0 |
| Total | 3,487 | 0 / 0 |

- `PolicyAdministrationDenialShouldKeepSpecificCatalogProblem` verifies policy-block, notification-routing, and escalation-policy denials through CommandGateway, including safe code/copy/action/version and zero dispatch. Generic conflict regressions verify `about:blank` for unknown, explicit generic, and incompatible completed codes while recognized conflicts retain their existing specific type. Catalog parity tests cover the complete OpenAPI and generated enum sets, uniqueness, and JSON wire-value round trips; no OpenAPI or generated client edit was needed.
- The required startup baseline command `aspire start --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --no-build --non-interactive --format Json` still exits 2 (child exit 134) because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` is absent. Story 1.4 still has no completion evidence contract. The story remains `in-progress`; no attested completion, live topology, or manual screen-reader run is claimed. The UI retry-text finding remains deferred as recorded below.

### Final verification after workflow review (2026-10-13)

- All six new patch groups in the workflow triage were applied and verified. Generic denials consistently dismiss; failure/blocked views reject unrelated acceptance copy while retaining supported retry, duplicate, reprocess and idempotency history; paired workspace explanations share one announcement key without a new public parameter. Browser tests reuse the existing system Chrome discovery.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q` passed with zero warnings and errors.
- All nine affected native assemblies passed independently with `-failSkips -noColor -result-trx <lane>.trx -result-ctrf <lane>.ctrf.json`. Current results, complete execution logs and the build log are retained in `/tmp/chatbot-story-1-4-contracts-server-20261013` under `final-*`; all results below have zero failures or skips.

| Suite | Passed |
| --- | ---: |
| Contracts | 517 |
| Client | 92 |
| Cli | 53 |
| Mcp | 60 |
| UI | 320 |
| Server | 2614 |
| Conformance | 150 |
| Architecture | 123 |
| UI.E2E | 235 |
| Total | 4,164 |

- Final machine results cover all four frozen matrix rows. UI E2E includes 70 actual component-render cases and 13 safe-outcome Chromium cases, plus the existing real cross-surface checks. New mounted assertions verify that each distinct same-code attempt activates the live region and a repeated attempt stays quiet. Parent renders verify that paired workspace explanations announce once. Isolated server regressions guard explicit generic-denial actions and malformed primary lifecycle metadata.
- The browser class also passed all 13 cases with `CHROME_EXECUTABLE_PATH` unset and `PLAYWRIGHT_BROWSERS_PATH` set to an empty task-owned cache; it stayed empty. Historical tracked screenshots were restored to their pre-run bytes; generated copies are retained under `screenshots-final-generated` in the results directory.
- The repository gate self-tests passed 219 tests with no failures or skips. The completion preflight command below exits 1 with `passed: false`, reason `scope_digest_mismatch`, subject `1-4-return-versioned-safe-outcomes.json`, because the story still has no evidence contract. The workflow's completion transition remains subject to this repository gate; no attested completion, live backend topology or manual screen-reader traversal is claimed.

```bash
dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll validate --story _bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md --contract _bmad-output/implementation-artifacts/evidence/1-4-return-versioned-safe-outcomes.json --target-status done --base 8437900622d5bf7a9d34ca5cc543cd802f653cb1 --head 927a6b022b2f8f262b25cd07350721b2a7a5e29e --results /tmp/chatbot-story-1-4-contracts-server-20261013 --report /tmp/chatbot-story-1-4-contracts-server-20261013/story-evidence-preflight.json
```

- Ordinary Aspire startup remains blocked by the missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret` as recorded above. Prior carried findings and the two newly identified pre-existing issues retain their recorded deferred disposition. The story is `in-review`, with its sprint entry at `review`, pending completion evidence.

### Verification of the 2026-10-14 follow-up patches

- Implemented and checked all 17 patches in the latest source review, preserving the approved frozen intent. The catalog now contains 94 entries. Seven new conflict codes were appended to OpenAPI and the generated enum without changing existing values; both UI locales carry specific copy.
- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` and `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q` passed. The final UI E2E project build also passed; both final builds report zero warnings and errors. The source-mode restore resolved the first stale/mixed-assets CS1704 failure.
- All nine affected native assemblies passed independently with machine TRX/CTRF results in `/tmp/chatbot-story-1-4-20261014`: Contracts 517, Client 92, CLI 53, MCP 63, Server 2,615, UI 469, Conformance 150, Architecture 123, and UI E2E 252 — **4,334 tests, zero failures or skips**.
- The parent workflow inspected canonical TRX result identities and passing outcomes for every frozen matrix row. Evidence includes catalog metadata and en/fr parity, unknown/untranslated terminal fallbacks, metadata-only denial and input-free metric tests, 144 transport/status effects cases, six CLI/MCP null-body cases, 12 Chromium visual-mode rejection cases, actual workspace retry activation, dashboard/composer remount announcements, and the concise live-region after-render lifecycle.
- Actual HttpClient-style timeouts with a canceled linked token and an inner `TimeoutException` retain dependency retry guidance. Explicit cancellation still rethrows without a failure action. Body-less 409 uses the same terminal generic bucket as CLI/MCP; the established legacy conflict alias retains its specific lifecycle guidance.
- `git -c core.whitespace=cr-at-eol diff --check` passed. Generated historical screenshots were retained separately in the results directory and restored to their pre-run bytes.
- Repository gate self-tests passed independently: 219 tests, zero failures or skips. The completion preflight below exits 1 with `passed: false`, reason `scope_digest_mismatch`, subject `1-4-return-versioned-safe-outcomes.json`, because the completion contract is absent. The story remains in review pending a passing exact-scope, mapping, provenance and triggered-primary-lane gate; the implementation suite pass does not replace that obligation.

```bash
dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll validate --story _bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md --contract _bmad-output/implementation-artifacts/evidence/1-4-return-versioned-safe-outcomes.json --target-status done --base 8437900622d5bf7a9d34ca5cc543cd802f653cb1 --head fb0b039a335538b30de359cb1e8f5751d84f5249 --results /tmp/chatbot-story-1-4-20261014 --report /tmp/chatbot-story-1-4-20261014/story-evidence-preflight.json
```

- The exact AppHost startup baseline remains blocked: `aspire start --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --no-build --non-interactive --format Json` exits 2 (child 134), missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret`. Local browser tests use the existing loopback typed-client seam. No successful live backend topology or manual screen-reader traversal is claimed. The Story 1.4 completion evidence contract remains absent; no attested completion is claimed.

### Final verification after the 2026-10-14 workflow review

- All ten grouped review patches were applied. The parent inspected their source and executable assertions. Existing UI instance-permission and category omissions are recorded as two new pre-existing deferrals; prior carried findings retain their dispositions.
- The final Debug source build passed with zero warnings and errors: `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q`. Its log is `/tmp/chatbot-story-1-4-20261014/final-build.log`.
- All nine affected native assemblies then passed independently with `-failSkips -noColor -result-trx <lane>.trx -result-ctrf <lane>.ctrf.json`. Current full TRX/CTRF results, logs and the summary are in `/tmp/chatbot-story-1-4-20261014/final-after-review`.

| Suite | Passed | Failed / skipped |
| --- | ---: | ---: |
| Contracts | 517 | 0 / 0 |
| Client | 92 | 0 / 0 |
| CLI | 53 | 0 / 0 |
| MCP | 63 | 0 / 0 |
| Server | 2,615 | 0 / 0 |
| UI | 496 | 0 / 0 |
| Conformance | 150 | 0 / 0 |
| Architecture | 123 | 0 / 0 |
| UI E2E | 258 | 0 / 0 |
| Total | 4,367 | 0 / 0 |

- The final matrix audit uses passing canonical TRX method identities. It covers all four frozen rows, including 156 transport/status/alias cases, explicit canceled-task behavior, both Stop timeout shapes with real reducer state, accepted-mutation and accepted-Stop polling preservation, why-panel and association submission timeouts, English/French after-render summaries, keyed pagination, 12 Chromium visual-mode rejections and observed browser live-region mutations for fresh same-code attempts. The deliberately missing-browser regression also passed in its focused run. Passive observer updates retain the existing no-live-announcement contract.
- Generic status remains failed, dismissed and nonretryable, with zero retry counters and the schema-valid `maxAttempts: 1`. Missing-locale Association Review retry permission comes from the localized fallback.
- `git -c core.whitespace=cr-at-eol diff --check` passed. The 42 test-generated historical screenshot changes were retained under `generated-screenshots` in the final results directory and restored to their pre-run bytes.
- A concurrent `/pushall` commit advanced seven root gitlinks at `5f1786557448bf182abf6d26c1a36808ceb5dbab`; it is preserved. The final source build and full suites ran after that commit. The final completion preflight below uses the current canonical HEAD and exits 1 with `scope_digest_mismatch` for the absent Story 1.4 contract:

```bash
dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll validate --story _bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md --contract _bmad-output/implementation-artifacts/evidence/1-4-return-versioned-safe-outcomes.json --target-status done --base 8437900622d5bf7a9d34ca5cc543cd802f653cb1 --head 5f1786557448bf182abf6d26c1a36808ceb5dbab --results /tmp/chatbot-story-1-4-20261014/final-after-review --report /tmp/chatbot-story-1-4-20261014/final-after-review/story-evidence-preflight.json
```

- The spec remains `in-review` and sprint status remains `review`. Repository completion evidence and ordinary AppHost startup remain blocked as recorded above; no attested completion, successful live backend topology or manual screen-reader traversal is claimed. The workflow's requested done transition is subordinate to the required repository preflight.

### Completion verification of the final refinement (2026-10-14)

- The final Debug source build passed with zero warnings and errors: `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false -v:q`. The final log is `/tmp/chatbot-story-1-4-review-11laus_d/final-build.log`.
- All nine affected native test assemblies passed independently with `-failSkips -noColor -result-trx <lane>.trx -result-ctrf <lane>.ctrf.json`: **4,386 passed, zero failures or skips**. Gate self-tests passed **226 tests, zero failures or skips**. Complete current-run TRX/CTRF results and logs are retained in `/tmp/chatbot-story-1-4-review-11laus_d/results/completion`, with `suite-summary.json` in its parent run directory.
- Mounted regressions prove terminal and untranslated dashboard failures cannot activate Refresh, dependency failures retain a real reload action in English/French, and fresh Governed Operations submissions announce across remount while passive rerenders remain quiet. Server, CLI and MCP refusal theories reject queued/accepted retry outcomes; catalog tests pin their waiting state. Transport exceptions retain safe catalog dependency copy and retry guidance without raw prose.
- Product completion regressions accept both `review` and `in-review` story bases with an exact `review` sprint base. Missing, unreviewed, terminal or inconsistent base records still fail; the source/provenance/mapping gates and policy exceptions are unchanged.
- The repository completion preflight passed for the proposed done state: 15 exact refinement paths, 11 checked/mapped items, ten current-run lanes and the recognized browser primary. Its final report path is `_bmad-output/implementation-artifacts/evidence/reports/1-4-return-versioned-safe-outcomes.json`.
- The completion contract maps all seven execution tasks and four acceptance criteria to passing canonical test identities and declares the recognized `browser-primary` lane. Its exact refinement scope begins at the already-reviewed implementation `c8da8a5b3aeaf057586aacd30f9c3e5e7bf37947`; historical source inventory is retained separately below.
- `git -c core.whitespace=cr-at-eol diff --check` passed. Browser-generated historical images are retained separately in the run directory and their original tracked bytes are preserved.
- Baseline command `aspire start --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --no-build --non-interactive --format Json` exited 2 (child 134) because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` is absent. The current refinement changes no AppHost path and makes no live Aspire/Dapr claim; browser evidence uses the actual loopback UI host with the existing typed-client seam. This environment limitation and the previously recorded deferred issues remain.

| Suite | Passed | Failed / skipped |
| --- | ---: | ---: |
| Contracts | 519 | 0 / 0 |
| Client | 92 | 0 / 0 |
| Cli | 57 | 0 / 0 |
| Mcp | 67 | 0 / 0 |
| Server | 2617 | 0 / 0 |
| UI | 496 | 0 / 0 |
| Conformance | 150 | 0 / 0 |
| Architecture | 123 | 0 / 0 |
| UI.E2E | 265 | 0 / 0 |
| StoryEvidenceGate | 226 | 0 / 0 |

## File List

- `_bmad-output/implementation-artifacts/evidence/1-4-return-versioned-safe-outcomes.json`
- `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/story-evidence-integrity.md`
- `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`
- `src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/SafeOutcomeProblemCompatibilityTests.cs`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeInteractionTests.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs`

## Historical Implementation Inventory

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
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemTypes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`
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
- `src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs`
- `src/Hexalith.ChatBot.UI/State/OperationalDashboards/OperationalDashboardsEffects.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantLeakageScannerTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/SafeOutcomeFallbackMetricTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/SafeOutcomeProblemCompatibilityTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/ServerSafeOutcomeHostMetricTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/ServerSafeOutcomeMetricExporter.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationProgressReadTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/OperationStatusWorkflowStatusSinkTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/BannerTransitionRenderHost.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/FakeChatBotClient.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/LiveChatBotUiHost.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/RealRenderFixture.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/RealRenderCrossSurfaceE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeRenderDispatcher.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeRenderState.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeTestAuthenticationStartupFilter.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/StreamTransitionRenderHost.cs`
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

- `src/Hexalith.ChatBot.UI/Services/ChatBotUiFailureCatalog.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/InteractionRenderJsRuntime.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeInteractionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/SafeOutcomeInteractiveRenderer.cs`
- `tests/Hexalith.ChatBot.UI.Tests/DashboardFailureDispatcher.cs`
- `tests/Hexalith.ChatBot.UI.Tests/GovernedOperationsEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/OperationalDashboardsEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/UiTransportFailureCases.cs`

- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`

### Review Findings (2026-10-13 contracts and server)

- [x] [Review][Patch] Policy block is returned as an authorization denial [src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs:211]
- [x] [Review][Patch] Generic denial keeps an idempotency-conflict type [src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs:35]
- [x] [Review][Patch] Routing and escalation denials have no problem assertion [tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs:5124]
- [x] [Review][Patch] Catalog codes are not locked to the generated enum [src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml:3250]
- [x] [Review][Defer] Failure card can say retry is allowed when the action was withheld [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor:148] — deferred: outside this contracts and server slice; `RetryableText` shows Yes for a non-terminal catalog entry when the stored retry flag is true, even if the stored action does not match the catalog action

#### Rejected

- `false` — Unknown resolution depends on `Entries[0]` and a public counter. `generic_denial` is that first entry, and `RecordFallback` is the shared input-free counter the UI localizer already calls.
- `false` — `failed_attachment`, command-execution idempotency conflict, and the escalate-only correction codes use the wrong state family. Public copy comes from the catalog headline and action; `State` only excludes waiting and completed codes from problem redaction, and these codes are not excluded.
- `false` — Authenticate, correct-request, request-access, and escalate are reported as terminal. `IsTerminal` means the operation will not continue, which matches those actions. Association Review's policy-failure mapping of that flag is a UI classification, not a catalog error.
- `false` — Redaction rewrites `retry_queued`, `duplicate_suppressed`, and the other waiting or completed codes to generic denial. Those codes are status and audit tokens. Problem Details that reach `Apply` are admission denials, not those codes.
- `false` — A known problem drops its client action unless it already matches the catalog, an unset action becomes authenticate, and `review-source-evidence` is missing from OpenAPI. The factory sets the catalog action before redaction, no catalog entry uses `review-source-evidence`, and the OpenAPI action enum was not part of this change.
- `false` — Unknown completion or audit text throws with the raw token, and the unknown status branch sets `maxAttempts` to 0. Status writers store only `accepted-projection-pending`, `completed`, `failed`, `committed`, and `reconciling`.
- `false` — A known status emits an empty `safeNextActions` array. Accept writes `none` for `command_accepted`, whose catalog action is `none`, and the workflow sink writes the catalog action for the reason it stores.
- `false` — A stored `generic_denial` erases a known primary reason and zeros retry counts. No status writer stores `generic_denial` in the reason fields `KnownCode` rejects.
- `false` — `command_accepted` carries `projection-pending`, so every accepted command is blocked. That reason is read as a failure-item blocked reason, and accepted commands are not failure items.
- `false` — An unknown status still returns an arbitrary operation class. Writers assign only closed server operation-class codes.
- `false` — Redaction leaves untrusted correlation and task text in the problem. `Detail` and `Message` are replaced with catalog copy; the copied identifiers are the gateway's own correlation and task values.

### Review Findings (2026-10-14 src chunk)

Scope: `8437900..08be477`, limited to `src`, `Hexalith.ChatBot.slnx`, gitlinks and `README.md`; the generated client, tests and BMAD artifacts are excluded (38 files, 2,734 diff lines). Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor. All four completed. Tests are a follow-up chunk.

- [x] [Review][Patch] Seven server idempotency-conflict codes have no catalog entry [src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs:18] — resolved by decision (2026-10-14): add catalog entries with specific en/fr copy for `idempotency_conflict_participant_resolution`, `_association_scoring`, `_association_threshold_policy`, `_low_risk_ai_assistance`, `_approval_decision`, `_approved_ai_action_execution` and `_ai_action_proposal`; add them to the OpenAPI code enum without renumbering, regenerate the client and refresh the hash fixture; add a parity test of `CoarseIdempotencyOperationClass.All` conflict codes against the catalog. Today a legitimate non-equivalent duplicate returns 409 `about:blank` "Request unavailable." with Dismiss and counts as a fallback.
- [x] [Review][Patch] UI transport failures became terminal generic denials with no retry [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs:14] — resolved by decision (2026-10-14): mirror the CLI/MCP status buckets. Map `HttpRequestException`, timeouts and body-less 408/429/5xx (`HexalithChatBotApiException` without a problem code, or a typed problem with a null `Result`) to `dependency_degraded`/`retry-later`; keep `generic_denial` for programming errors. Covers `ProjectConversationEffects`, `AssociationReviewEffects`, `GovernedOperationsEffects` and `OperationalDashboardsEffects`, and adds per-status effects tests. Refines C-B3.
- [x] [Review][Patch] Composer announces a terminal generic denial for a transient blocked/stale context [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:161] — resolved by decision (2026-10-14): for `unsafe-context`, render the composer's localized disabled-reason guidance with no catalog code, so there are no terminal semantics and no retry copy; the sibling `ChatBotBlockedState` keeps the explanation.
- [x] [Review][Patch] Blocked/stale conversation shows server next-action tokens as "Dismiss" [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor:118] — resolved by decision (2026-10-14): in `UseLocalStateGuidance` mode, render tokens outside the 7-action vocabulary (`review-status`, `wait-for-*`, `review-*`) as the `SafeNextActionDefault` text the governance panel uses, and render no action button for them.
- [x] [Review][Decision] French copy exists only at the UI boundary — resolved by decision (2026-10-14): accepted. API Problem Details, CLI and MCP return the stable catalog code, version and English default copy, and clients localize by code; English/French parity applies to the UI catalog resources. Recorded in Implementation Notes; no code change.

- [x] [Review][Patch] Streaming status says "Retry response refresh." for a terminal generic denial [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:619] — `GenericFailureCode` is now `generic_denial`, and every failure keeps the conversation and sets `StreamingErrorCode`. Derive the text from the catalog action of the resolved entry, which is correct whichever way the transport decision goes.
- [x] [Review][Patch] Association Review and Operational Dashboards banners show a disabled catalog action beside a live Retry/Refresh [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:53] — the workspace fix (`ShowCatalogAction="false"`) was not applied to these banners. A retryable Load failure shows a disabled "Retry later" plus the enabled `association-review-retry`, and the dashboards banner sits beside the enabled header Refresh. Fix both and extend the `TIMEOUT` render test to assert there is exactly one enabled retry.
- [x] [Review][Patch] One workspace load failure is announced twice, once by the composer [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:495] — `ComposerDisabledReasonCode` now forwards the raw `ErrorCode`, so the composer renders a coded Warning banner (assertive `BlockedAction`) under the key `project-conversation-composer-attempt-N-{code}`. The workspace banner and blocked state announce under `project-workspace-retryable-FailedDependency-{code}`. Only one region should announce a page-load failure.
- [x] [Review][Patch] Status banner turns idempotency conflicts into generic denial; the compatibility check is copied with different rules [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:156] — the banner rejects every `waiting`/`completed` code. The server, CLI, MCP, blocked state and failure item all keep `idempotency_conflict_*`, so a duplicate association decision reaching `AssociationReview` shows "Request unavailable." instead of "Decision already submitted." Workspace `CanRetryLoad` also skips the blocked-state downgrade. Use one shared compatibility helper.
- [x] [Review][Patch] Blocked-state accessible label contradicts the visible catalog headline [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor:131] — the label starts with `BlockedReasonLabel(Reason)`. An unknown workspace code therefore reads "Dependency failed: The requested action is unavailable…" while the heading says "Request unavailable." Build the label from `ResolvedHeadline`.
- [x] [Review][Patch] Failure card makes the whole article an atomic live region [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor:10] — `role="status"`/`"alert"` with `aria-atomic="true"` on the `<article>` reads every metadata row (operation, correlation and workflow IDs, timestamps) and drops article semantics. A polite region inserted with its content already present is often not announced at all. Announce `AccessibleLabel` through a stable live region instead.
- [x] [Review][Patch] Attempt counters restart on remount, so a new same-code attempt after navigation is silent [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:91] — `_attemptSequence` (composer) and `_refreshAttempt` (`OperationalDashboards.razor:299`) are instance fields, but the deduplication set is circuit-scoped. After navigating back, a user's new submission or Refresh reuses an already-announced key, undoing N-B4. Add a per-instance identity to the key; suppressing the initial load on re-entry stays as designed.
- [x] [Review][Patch] MCP null-body problem fallback has no test [src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs:250] — CLI pins this with `NullProblemBodyShouldUseStatusCatalogFallback`. Restoring the baseline unconditional `Result` read would bring back an NRE inside the catch-all with no MCP test failing. Mirror the CLI theory for 401/403/503.
- [x] [Review][Patch] Workspace retry action is checked for its enabled state but never activated [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:96] — `WorkspaceFailureShouldOnlyEnableCatalogRetry` reads only `data-chatbot-action-state`. Dropping `OnActivate` or passing the wrong callback still passes. Activate the action and assert a reload is dispatched.
- [x] [Review][Patch] Dashboards refresh-attempt announcement key is untested [src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor:60] — removing `_refreshAttempt++` or the key segment silences a repeated same-code failure after Refresh, and no test fails. Add a mounted transition test.
- [x] [Review][Patch] Composer `unauthorized` branches and their resource are dead [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:143] — the only caller now passes the raw `ErrorCode`, so the `"unauthorized"` branches in `StatusMessage`/`StatusCatalogCode` and `ProjectConversationComposerUnauthorizedReason` are unreachable. Delete them.
- [x] [Review][Patch] `BlockedReasonText_Default` is no longer read [src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs:666] — the `ChatBotBlockedState` rewrite removed its only reader. Delete the key and both resources.
- [x] [Review][Patch] Private field `redactionStage` breaks the `_camelCase` convention [src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs:10] — the primary-constructor conversion added `redactionStage` next to `_telemetry`. Rename it, or restore the primary constructor.

- [x] [Review][Defer] AI execution recovery returns a bare, non-catalog 404 [src/Hexalith.ChatBot.Server/Lifecycle/AiExecution/AiExecutionRecoveryEndpoints.cs:34] — deferred: pre-existing endpoint outside the code map. `SafeNotFound()` returns `{code:"safe-not-found"}`, which is not problem+json, has no catalog code and no version (from the auditor's AC2/AC3 note and the Blind Hunter's existence-neutral finding).
- [x] [Review][Defer] Suppressed banners keep `role="alert"`/`"status"` with `aria-live="off"` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:106] — deferred: maybe-false, medium if true. The role was kept deliberately for semantic status. Settle by remounting a suppressed `alert` banner under NVDA and JAWS with Chrome and checking whether the alert event still announces it.
- [x] [Review][Defer] A failure item whose code or state changes under the same `ItemId` is never announced [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor:90] — deferred: maybe-false, medium if true. Settle by checking whether failure-state projections update one conversation item in place (retryable → terminal) or append a new item per transition.

#### Rejected (2026-10-14 src chunk)

- `false` — Catalog entries with action `none` never show a disabled reason. No applicable action exists to disable; the renderers show "No user action is required."
- `carried` — Generic-denial failure card shows dependency, escalation, category and scope rows. This duplicates deferred C-E7/B5.
- `carried` — Out-of-scope gitlink, solution and ACL changes. The gitlinks and Release graph are already deferred (abcb637, N-B10/D-B9). The ACL row adds the new SDK `/project/rebuild/part/v1` route to the existing EventStore-only rebuild grant.
- `low` — Failure card shows "Next action: Retry later" beside a `none` metadata row when retry is withheld. This is rare, and R-B3 deliberately kept the disabled catalog action with its reason; it relates to deferred D-E2.
- `carried` — The generic Dismiss can never be activated (B1/R-B4).
- `false` — The CLI JSON separates 404 from 403. It echoes the status the server already sent; the leak source is the endpoint deferred above.
- `carried` — `AccessibleDescription` is English-only and unused (N-B9).
- `carried` — Status `Parse<>` throws and adapter status omits catalog copy (B8/D-B6, C-B6/D-B7).
- `rejected` — The review ledger contradicts the code. The fix would edit this spec.
- `carried` — `Resolve` falls back to `Entries[0]`. The strict-resolve suggestion is covered by the conflict-code decision.
- `carried` — Escalate and delayed codes are labelled `denied`/terminal. `State` is never shown to users, and escalate-is-terminal was decided earlier.
- `low` — SHA-256 plus a lock on each UI catalog lookup. A measurable but small cost, and the fix is a refactor.
- `false` — A culture other than en/fr turns every failure into a generic denial. Request localization accepts only en/fr, and the I/O matrix specifies the missing-locale fallback.
- `carried` — `CommandGatewayHttpResults` constructs its own redaction stage.
- `false` — The catalog version stayed v1 despite semantic changes. The spec requires preserving the v1 wire version.
- `carried` — The fallback counter has no tags and lives in Contracts (earlier metrics decisions).
- `carried` — MCP replaces server correction hints with the action token (metadata-only decision).
- `low` — PascalCase `Failure_Catalog_*` aliases are no longer read. They are kept deliberately as aliases and checked by a parity test.
- `low` — The workspace banner/blocked-state shared announcement key depends on matching spelling. D-B4 chose this, and load errors cannot reach a diverging code.
- `low` — An `about:blank` problem carries a custom title. The fix conflicts with the catalog-headline requirement.
- `false` — `ChatBotBlockedState` has two overlapping local-guidance flags. No named harm.
- `false` — An invalid prior outcome denies a healthy status. That is the fail-closed behaviour decided in N-B2/N-E5.
- `carried` — The localizer cache undercounts repeated fallbacks (C-B10).
- `carried` — Workspace and Association Review retry paths use static keys (R-E5/D-E3/D-B5), and the related commit claim.
- `low` — The CLI emits a null JSON `category` for status-only exceptions. This predates the story.
- `low` — CLI/MCP print `unavailable` for an undefined audit enum. This is CLI output, not the wire contract.
- `low` — The composer data attribute reports `generic_denial` for a whitespace code. The attribute is test-only.
- `carried` — The Release solution graph expands (D-B9).
- `carried` — Association Review `conflict` is no longer retryable. It maps to `invalid_lifecycle_transition`, as recorded earlier.
- `false` — A `reprocess-created` item loses terminal styling. `reprocess_created` is terminal in the catalog.
