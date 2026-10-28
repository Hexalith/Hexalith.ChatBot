---
title: 'Resolve five deferred UI fallback defects'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'b28991f278fa5640f49dd42d6efef9f7249b5fb0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-73, DW-110, DW-111, DW-118 and DW-119 so governed UI surfaces handle incomplete or unsupported metadata predictably. Render a missing health value with the existing Unknown warning. Use server visibility, redaction and freshness labels for association evidence rather than guessing from reference text; preserve the existing omitted-label compatibility for candidate references and refuse undefined nonnull labels. Dispatch a safe load failure for a blank association ID without calling the client or retaining the prior association's review data. Fall back to `und` after locale sanitization removes every character, in both composer submission paths. Reject undefined generated enum values through the existing safe failure path instead of exposing numeric ordinals, preserving declared wire values and nullable values. Add behavioral regressions, independently review, close exactly these five ledger items with verification evidence, then commit and push the task changes to main as requested. Preserve Fluent/FrontComposer composition, authorization, dependency pins and submodules.

</frozen-after-approval>

## Implementation Notes

Investigation found no human-only intent gaps or irreversible application changes. Four existing UI files plus the association load effect need localized changes; existing UI unit and render fixtures provide coverage without dependencies or public APIs. This uses the small-change oneshot route under the user's batch and push authorization.

Code map: `ChatBotConversationItemStatusSummary.HealthBadgeColor` dereferences Health; the localizer already accepts null. `AssociationReviewService.ResolveEvidenceState` honors structural states before a keyword safety net; server candidate evidence may omit labels, so retain the null default. `AssociationReview.OnParametersSet` skips blank IDs; dispatch the existing load action so its reducer clears old route data, and validate the ID before the load effect calls the service. `ProjectConversationService.SafeLocale` needs its fallback after filtering; `WireToken` must reject unknown enum values consistently with AssociationReviewService.WireValue. Extend the corresponding UI.Tests service/effect tests and the existing real HtmlRenderer fixture in UI.E2E.Tests.

Baseline UI.Tests Debug build passed with zero warnings/errors. Required runtime baseline command `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2: Conversations Contracts/Client report `Conversations requires the Platform tool-selected source mapping; validation does not resolve dependency mappings.` No topology started. Exact output: `/tmp/chatbot-five-ui-aspire-baseline.log`. Runtime validation is blocked independently of UI tests; no live qualification is claimed.


Implemented all five fixes and independently checked the final diff. The review also exposed a delayed association-load race; private request generations now suppress obsolete success and failure dispatches. Existing enum-name mappings use validation while preserving their prior declared-name representation, so unsupported values cannot bypass the repaired wire-token helper. No public interface, dependency or submodule changed.

Verification: both original UI test projects build in Debug with zero warnings/errors. The native catalog-pinned xUnit runner passes 176/176 service/effect cases and 8/8 real HtmlRenderer cases with zero failures/skips. These include null health; explicit/omitted evidence labels and benign keyword names; successful selected-fingerprint submission; stale, redacted, unavailable and undefined-label refusal; restricted reference/kind/fingerprint sentinel withholding; blank-ID clearing without client reads; delayed success/failure fencing; both locale submission paths; and unsupported nullable/nonnullable enums through real safe failure reducers.

Broad UI run: 533 total, 529 passed, four failed, zero skipped. The four failure identities exactly match the pre-change baseline (499 total, 495 passed): unchanged Playwright expectation tests require 1.64.0 while the shared catalog resolves 1.64.1. Full VersionedSafeOutcomeRenderTests: 95 total, 76 passed, 19 failed, zero skipped; the 19 existing workspace/streaming cases lack the title now required by FcPageHeader, with identical failure identities before and after review patches. A separate archived baseline renderer build was blocked by 207 FrontComposer analyzer errors, and a fixture-only archived build could not resolve FrontComposer.Contracts; no full baseline renderer pass is claimed. Runtime startup remains blocked by the recorded Conversations source-mapping error.

The tracked receipt `validation/five-deferred-ui-fallbacks.json` binds all changed source/test files and CTRF/TRX checksums, names every covering regression and records exact commands, outcomes and limitations. Raw local results and logs are in `TestResults/five-deferred-ui/`. Exactly DW-73, DW-110, DW-111, DW-118 and DW-119 are closed in the ledger.

The build record stays in review under `docs/story-evidence-integrity.md`: broad checks and UI primary/runtime qualification are incomplete. There is no product-story, sprint, technical-enabler or release-gate completion claim. All five scoped defect implementations and their focused verification are complete; the user authorized committing and pushing them to main.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Earlier association load can repopulate the cleared blank-ID route | medium / patched | Private request generations suppress old success and failure dispatches; deterministic delayed-completion cases pass. |
| Direct enum-name mappings can bypass WireToken validation | medium / patched | Shared EnumName validates every former enum ToString call while retaining known name representations and nullable behavior; lifecycle, provenance, participant, attachment and response-status regressions pass. |
| Unsupported-enum test stops at service exception | medium / patched | Actual ProjectConversationEffects and reducers produce generic_denial, clear loading and retain no unsupported conversation. |
| Blank-ID render fixture does not cover prior review transitions | medium / patched | Component dispatch/render coverage composes with real reducer/effect tests clearing candidates, notes, rationale and accepted commands; delayed success/failure cases prove the new state survives old work. Reviewer accepted this composition without a mounted-store test. |
| Restricted evidence not exercised in rendering | medium / patched | Real service mapping feeds the actual comparison renderer for redacted, unavailable and undefined labels; reference, kind and fingerprint sentinels are absent throughout HTML. |
| Benign evidence-name test omits successful submission | medium / patched | All seven former keywords, with both explicit and omitted labels, submit the selected evidence fingerprint successfully. |

Independent recheck: all six findings addressed; no concrete regression found. No new work deferred. Commitlint, staged whitespace verification, remote divergence inspection and the authorized commit/push complete finalization.
