# Code Review Findings — canonical Story 13.4

Story: `13-4-review-risky-ai-actions-without-losing-evidence-or-authority`
Spec: `_bmad-output/planning-artifacts/epics.md:3318-3339` (+ Epic 13 binding clause `:3238`, `implementation-conformance-addendum-2026-07-17.md`, `EXPERIENCE.md`)
Date: 2026-08-25
Review target: full current source of the AI-action-review surface (15 files, 1875 lines) — no story diff exists; the story was seeded at `review` by `sprint-change-proposal-2026-07-17.md` §333 for evidence reconciliation.
Layers: Blind Hunter, Edge Case Hunter, Verification Gap Reviewer, Acceptance Auditor (all 4 completed; none failed).

## Verdict

Canonical 13.4 cannot be accepted. Independent of any individual defect, the Epic 13 binding clause requires the story's primary live route to execute successfully, and no such route exists: `ChatBotApprovalConversationItem` is rendered only at `ChatBotConversationStream.razor:38` under `else if (item.IsApprovalEvent)`, and the only real-render suite drives that route through `FakeChatBotClient` whose `GetProjectConversationAsync` returns `Items = []` (`FakeChatBotClient.cs:160`). The approval surface renders **zero times** in any executing test.

### Review Findings

#### Decisions (resolved 2026-08-26)

- [x] [Review][Decision] **No live AI Action Review route** — AC1/AC3 say "the live AI Action Review surface". The six `@page` routes are `/`, `/projects/{ProjectId}/conversation`, `/governed-operations`, `/operational-dashboards`, `/compliance-audit-investigation`, `/association-review/{AssociationId}`. The surface is a conditional list item inside 13.2's conversation route, so 13.4 owns no route and defers acceptance to another surface — which the addendum's "Surface-local acceptance" section forbids. Decide: give 13.4 its own route, or amend the AC to name the conversation route as its host.
- [x] [Review][Decision] **Three of AC1's nine required fields do not render** — **policy reason**: `ChatBotRiskChip PolicyReason="@RiskSummary"` and `RiskSummary` resolves to `DisplayRiskClass`, i.e. the risk-class token, not a policy reason; the model has no `ApprovalPolicyReasonCode` (the sibling `ChatBotAiOutcomeConversationItem.razor:47` renders a real one from `AiPolicyReasonCode`). **expected result**: `ChatBotApprovalConversationItem.razor:70` renders `ApprovalExpectedPostStateLabel` whose value is `RedactionStateLabel(Item.ApprovalExpectedPostStateRedactionState)` — the redaction state *of* the expected post-state, not the expected result; no content field exists on the model. **project scope**: `ProjectId`/`ProjectDisplayName` exist on the model but no row renders them. Requires contract/model fields — decide whether 13.4 owns that work.
- [x] [Review][Decision] **Reject / request-revision / cancel have no state gating at all** — `ChatBotApprovalConversationItem.razor:116-130`: three `FluentButton`s with no `aria-disabled`, no `aria-describedby`, no `CanX` predicate; they submit unconditionally. AC2 requires the allowed/blocked/denied/pending/retryable/terminal matrix for all four verbs. The per-verb rules are unspecified — decide them.
- [x] [Review][Decision] **Routed governance surfaces render hard-coded fixture data as live product data** — `GovernedOperations.razor:254-262` embeds six fake queue rows (`item:ambiguous-001`, `operations-admin`, …); `ChatBotApprovalQueuePriorityView.razor:127` binds the whole priority table to `ChatBotApprovalQueuePriorityContract.CreateDefault()`, a design-time fixture (`sha256:11aa`, `requester:requester-1`) whose batch action is hard-coded `DisabledWithReason` (`:88`) so no approval is ever possible there. `GovernedOperations.razor:86-90` additionally states `"age>0 risk:any confidence:any …"`, `"priority desc, item-ref asc"` and `"page-size:100"` as if they described the live query — the only real filter is `SelectedQueueFamily` and there is no pagination. Needs real data wiring; decide the owner.
- [x] [Review][Decision] **Evidence is not openable** — AC1 requires evidence "linked to the source request". `ChatBotApprovalConversationItem.razor:16-18` passes only `State`/`Text`/`StableId` to `ChatBotEvidenceChip`, whose `CanOpenEvidence` defaults to `false` (`ChatBotEvidenceChip.razor:59`), so the non-interactive branch always renders and `OnActivate` is never wired; `UnavailableReason` is also unset so the expired-evidence reason span never renders. `:23-31` renders one `FluentButton` per evidence reference with **no `OnClick`** — focusable tab stops that do nothing. `ApprovalSourceMessageId`/`ApprovalSourceConversationItemId` (`:53-54`) are inert `<code>` text with no anchor. Decide whether an evidence drawer is in scope.
- [x] [Review][Decision] **`ChatBotTaskIntentReviewPanel` is unreachable dead code that tests now pin** — no `.razor` under `src/` references it. It hard-codes English (`:4`, `:30`, `:51`), leaks raw snake_case reason codes to users (`<p role="alert">predecessor_task_intent_required</p>` at `:58`; `LiveRegionText = transition.DisabledReasonCode ?? "transition_unavailable"` at `:102`), dumps raw `Review.SourceMessageContent` into a `<pre>` (`:24`) with no redaction state in an otherwise metadata-only surface, and its `role="toolbar"` has no roving-tabindex. `Story13DefinitionListMigrationTests.Task_intent_review_panel_keeps_its_preexisting_hard_coded_labels` asserts the English literals, converting the defect into a build-enforced requirement. Decide: delete, or wire and fix.

**Resolutions — Jerome, 2026-08-26**

| # | Decision | Resolution |
|---|---|---|
| D1 | No live AI Action Review route | **Add a dedicated route.** 13.4 gets its own primary live route hosting the approval surface directly, satisfying the addendum's surface-local acceptance rule. |
| D2 | policy reason / expected result / project scope absent | **In scope for 13.4.** Add the approval policy-reason code and an expected-result content field to the contract/model, wire project scope into the panel, render all three. |
| D3 | reject/revise/cancel ungated | **Approve strict, other three permissive-with-reason.** `CanApprove` mirrors `GovernedOperationAggregate.ApprovalDisabledReason` (incl. the evidence count-mismatch rule) + `AiActionApprovalGate`; reject/revise/cancel stay enabled but gain in-flight guards and real rejection handling. Cross-boundary predicate test (option C) deferred until D1/D2 land. |
| D4 | routed surfaces render fixture data | **Split out.** 13.4 makes only its own new route real. `/governed-operations` hardcoded rows, the `CreateDefault()` priority binding and the fabricated query/sort/page metadata become a separate story; labelled in-product as sample data in the interim. |
| D5 | evidence chips inert | **Downgraded to patch.** Copy the established sibling pattern (`ChatBotDecisionConversationItem.razor:23-24`): `CanOpenEvidence` + `OnActivate` to the existing `ChatBotWhyProjectPanel`, set `UnavailableReason`, fix the dead per-reference buttons. |
| D6 | `ChatBotTaskIntentReviewPanel` hostless | **Wire into the new D1 route, then fix.** The service is live (`ProjectConversationService.GetTaskIntentReviewAsync:104`). Host it, localize the hard-coded English, stop surfacing raw snake_case reason codes, associate the validation error with its input, delete the test pinning the English literals. |

**Server-enforcement note (corrects the initial severity read).** The domain fails closed on `DecideAiActionApproval`: authority class (`AiActionApprovalGate.cs:44`, differentiated Approve vs review verbs), evidence-expired incl. count mismatch (`GovernedOperationAggregate.cs:3763`, Approve only), corrected-context-invalidated (`:2125`, Approve only), already-decided/conflicting (`:2117`), source-version mismatch (`:2112`). An ungated Approve therefore yields an **unhandled server rejection**, not an improper approval. `policy-blocked`, `dependency-degraded`, `awaiting-other-actor`, `state-not-permitted` are enforced nowhere server-side; `duplicate-decision` and `conflicting-decision` exist only in the UI localizer.

**New work items arising from the decisions** (not review patches — implementation scope for 13.4):

- [ ] [Decision D1] Create the dedicated AI Action Review route and host the approval surface on it.
- [ ] [Decision D2] Add `ApprovalPolicyReasonCode` and an expected-result content field to the approval contract/model; render policy reason, expected result and project scope.
- [ ] [Decision D6] Host `ChatBotTaskIntentReviewPanel` on the new route.
- [ ] [Decision D4] Label the `/governed-operations` queue rows and the priority table as sample data until the split-out story wires them.

#### Patch — applied 2026-08-26

- [x] [Review][Patch] [D5] Evidence chips are inert — wire `CanOpenEvidence` + `OnActivate` to `ChatBotWhyProjectPanel` per the sibling pattern, set `UnavailableReason`, and fix the per-reference buttons that are focusable with no `OnClick` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:16]
- [x] [Review][Patch] [D3] Reject/request-revision/cancel submit unconditionally — add in-flight guards and reachable reasons; they stay enabled per the D3 resolution [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:116]
- [x] [Review][Patch] `policy-blocked`, `duplicate-decision`, `conflicting-decision`, `dependency-degraded`, `awaiting-other-actor` do not block Approve — `CanApprove` is a three-item deny-list while the localizer defines eleven disabled reasons [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:167]
- [x] [Review][Patch] `SubmitDecisionAsync` catches only `InvalidOperationException`; a 403/409/422 `HexalithChatBotApiException<ProblemDetails>` escapes into the renderer, and the one exception caught (the missing-context guard from `ProjectConversationService.cs:152`) is mislabeled to the user as `projection-pending` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:276]
- [x] [Review][Patch] A failed operation renders the green "Projection complete" success banner — `IsProjectionPending` is `Contains("Pending")` and everything else falls to the success `else` [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:174]
- [x] [Review][Patch] Audit-not-committed renders nothing at all — no banner, no explanation, no safe next action [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:186]
- [x] [Review][Patch] The decision live region enters the DOM together with its first text, so neither the assertive block message nor the success message announces; render it persistent and empty [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:139]
- [x] [Review][Patch] No focus is moved after any decision, block, or failure — no `@ref`/`ElementReference`/`FocusAsync`/`IJSRuntime` in the component; the `tabindex="-1"` status paragraph is a target nothing focuses. EXPERIENCE.md:168,170 [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:142]
- [x] [Review][Patch] `ReduceFailed` leaves `Outcome` untouched, so the failure banner renders alongside the previous success's outcome, audit history and "projection complete" banner [src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsReducers.cs:36]
- [x] [Review][Patch] An HTTP timeout raises `TaskCanceledException`, matches the `OperationCanceledException` rethrow, and leaves `IsSubmitting` true forever — the record-note action becomes permanently disabled [src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs:31]
- [x] [Review][Patch] No in-flight guard on either decision path — a second click before the first completes submits a duplicate governed decision / duplicate governed note [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:276, src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs:22]
- [x] [Review][Patch] Operation state is not stable across a decision — `SubmitDecisionAsync` sets a local string only; no dispatch, no re-read, `Item.ApprovalStatus` stays `pending`, all four buttons stay live [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:276]
- [x] [Review][Patch] Redacted evidence can be disclosed — the visibility switch is ordinal-exact so `"Redacted"` renders as Available, and `PrimaryDisplayToken` takes `Evidence.First()` without checking visibility, surfacing a redacted matched value in the summary row [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor:124]
- [x] [Review][Patch] `IsUnavailableReason` is a seven-item denylist, so any unlisted reason code (rate-limited, state-not-permitted) makes a blocked action preview report state "allowed" with `aria-disabled` false; invert to an available-reason allowlist [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor:149]
- [x] [Review][Patch] The story's guard is non-load-bearing — `Story13DefinitionListMigrationTests` asserts the file *contains* `"FluentStack"` and `"chatbot-code"`, substrings the `CodeRow`/`TextRow` helper bodies satisfy on their own; deleting every call site keeps it green while the page renders no data. Add a live-DOM assertion on a named row's label and value [tests/Hexalith.ChatBot.UI.Tests/Story13DefinitionListMigrationTests.cs:104]
- [x] [Review][Patch] `FakeChatBotClient.GetProjectConversationAsync` returns `Items = []`, so the only real-render suite never mounts the approval item, the AI-action preview, or the why-project panel; seed one pending-approval item [tests/Hexalith.ChatBot.UI.E2E.Tests/FakeChatBotClient.cs:160]
- [x] [Review][Patch] The phone fallback exists only in the test fixture's injected CSS — `chatbot.tokens.css` has no rule for `chatbot-approval-priority-table` or `chatbot-small-screen-fallback`, so on the real route the dense table never hides at phone width and the fallback `<aside>` never hides at desktop, duplicating every row for all users and screen readers [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor:104]
- [x] [Review][Patch] `aria-describedby="@ApproveReasonId"` is unconditional but the referenced `<p id>` renders only under `@if (!CanApprove)` — a dangling reference whenever approval is permitted [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:112]
- [x] [Review][Patch] `aria-pressed="@(family == SelectedQueueFamily)"` binds a `bool`, so Blazor omits the attribute when false and the toggle group never reports a pressed state; same at `ChatBotGovernedComposer.razor:36,43` [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:78]
- [x] [Review][Patch] `role="table"` with `role="row"` children and no `role="cell"` anywhere in the repo — an ARIA table exposing rows with no cells; the rows also carry `tabindex="0"` on non-interactive containers [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:99]
- [x] [Review][Patch] Render-visible fields assigned after `ConfigureAwait(false)`, off the renderer's synchronization context, while siblings (`ChatBotGovernedAction.razor:87`, `ChatBotWhyProjectPanel.razor:175`) use `ConfigureAwait(true)` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:280]
- [x] [Review][Patch] Status classification by substring (`Contains("Pending")`, `Contains("Committed")`) on strings produced from an enum that is available on both sides of the seam [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:280]
- [x] [Review][Patch] The projection pending and projection complete banners share `AnnouncementKey="@outcome.OperationId"` and one `StateFamily`, so `OncePerStableOperationKey` dedup silences the pending→complete transition — the one change the user is waiting on [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:170]
- [x] [Review][Patch] Repeat failures and repeat blocked clicks produce no live-region change — the announcement key and the `DecisionStatus` text are identical each time [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:57, src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:269]
- [x] [Review][Patch] Un-localized user-visible text: raw wire tokens as queue-family button labels (`@familyToken`), the `$"{intro} ui {suffix}"` splice, and the priority contract's English literals ("Critical"/"High"/"Medium", "Per-project approval authority is required…", "Dense batch controls are unavailable…") which bypass `ChatBotUiTextLocalizer` entirely [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:81,275, src/Hexalith.ChatBot.UI/Design/ChatBotApprovalQueuePriorityContract.cs:128]
- [x] [Review][Patch] Evidence chip and approval gate read misaligned lists — the chip falls back to `"expired"` when `ApprovalEvidenceFreshnessStates` is shorter than `ApprovalEvidenceReferences`, but `ResolvedApproveDisabledReason` scans the original list, so an expired `aria-disabled` chip can render while Approve stays enabled [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:212]
- [x] [Review][Patch] A null/blank/unknown `ApprovalPolicySnapshotVisibility` makes the policy-snapshot row vanish with no "why unavailable" explanation — only `"authorized"` shows the value and only redacted/unavailable trigger the explanation [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:183]
- [x] [Review][Patch] `RiskActionClass` defaults every unrecognised or empty risk-class list to `ProjectMutating`, so the chip asserts project-mutating for an item with no known risk class; default to `Unknown` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:228]
- [x] [Review][Patch] The audit-unavailable case reports the wrong reason — when `ApprovalAuditStatus` is redacted/unavailable and `ApprovalDisabledReason` is blank, the reachable reason reads `state-not-permitted`; `audit-unavailable` is a defined key that is never selected [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:178]
- [x] [Review][Patch] `PriorityRank` switches on the English display label, so any localized or unlisted label ranks 0 and `PrioritizedHighestFirst` is trivially satisfied [src/Hexalith.ChatBot.UI/Design/ChatBotApprovalQueuePriorityContract.cs:104]
- [x] [Review][Patch] Three per-row `ChatBotGovernedAction`s are `Enabled` and `CriticalAction="true"` with no `OnActivate` — claim and secondary actions on critical governed items swallow the click silently [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:120]
- [x] [Review][Patch] Blank-guarding is inconsistent within one block — `CodeRow` is called directly for several values, bypassing the `MetadataRow` null-guard used by the rows above, so empty values render as a label plus an empty `<code>` box; same for `SafeNextActions` when the list is empty [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:87, src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:213]
- [x] [Review][Patch] `MetadataListLabelValueRow` joins `labels` and `values` independently, so mismatched lengths render two comma-joined strings of different arity side by side as if they corresponded [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:317]
- [x] [Review][Patch] `response.TaskId ?? response.CommandId` does not handle an empty string, so an empty `operationId` is used for two client reads and stamped into the UI as the operation identity [src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs:33]
- [x] [Review][Patch] Constant DOM ids on repeatable components — `TitleId`/`EvidenceTitleId` are `const`, and `ChatBotGovernedAction`'s default `StableId = "governed-action"` yields duplicate `-disabled-reason` ids; `TitleId` is also referenced by nothing while the `<aside>` names itself via `aria-label`, so the visible heading is not the accessible name (WCAG 2.5.3) [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor:99]
- [x] [Review][Patch] An unguarded `TimeRow` on a non-nullable `DateTimeOffset` renders `0001-01-01 00:00:00Z` as a governed decision timestamp [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor:30]
- [x] [Review][Patch] Timestamps and counts bypass the injected `ChatBotCultureFormatter` in favour of inline `InvariantCulture.ToString("u")` — including in the component that injects the formatter for `FormatConfidence` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor:171, src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:342]
- [x] [Review][Patch] `aria-label` on `FluentStack`, which renders a plain `<div>` with no role, is ignored by assistive technology while the guards count it as "accessibility preserved" [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor:34, src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:85]
- [x] [Review][Patch] `aria-disabled` on a `<section>` (no defined meaning on a non-widget role) and `tabindex="0"` on `<p>`, `<li>`, `<span>`, `<article>`, `<pre>` inflate the tab order with non-interactive content [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor:18]
- [x] [Review][Patch] The four decision buttons are raw `FluentButton`s that bypass `ChatBotGovernedAction`, so they never receive `chatbot-touch-target-primary` — the only rule applying the 44px target. EXPERIENCE.md:244 forbids compact-only sizing for approval controls [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:109]
- [x] [Review][Patch] An `<h2>` is emitted inside a `FluentAccordionItem` that already produced one, re-introducing the duplicate-heading defect the comment at `:62` says was removed [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:219]
- [x] [Review][Patch] Per-cell `chatbot-row-label` spans repeat the `<th scope="col">` header inside every `<td>`, so screen readers announce header, redundant label, then value for all seven columns of every row [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor:59]
- [x] [Review][Patch] An empty queue renders `role="table"` with zero rows and no empty-state explanation [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:100]
- [x] [Review][Patch] `ApprovalAuditStatus` uses `??` where every sibling uses the whitespace-aware `FirstNonBlank`, so an empty-string approval status beats a valid AI audit status [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor:69]
- [x] [Review][Patch] `@if (Sections.Count > 0)` is always true (`BuildSections` returns four sections unconditionally), and `Sections` re-runs `BuildSections()` on every access — twice per render [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor:4]
- [x] [Review][Patch] Token vocabulary is inconsistent — `RedactionFor` returns `metadata_only` while the enclosing element uses `metadata-only` and `IsUnavailableReason` matches only hyphenated forms [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor:147]
- [x] [Review][Patch] The `ApprovalDecisionKind` switch ends in `_ => "cancel"`, so an unrecognised decision is announced to the user as a cancellation [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor:284]
- [x] [Review][Patch] `#operation-outcome-title` is declared `LoadedContentLandingTargetId` by the focus contract test but sits on a bare `<section>` with no `tabindex="-1"`, so `.focus()` is a no-op in every major browser [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:161]
- [x] [Review][Patch] The restricted-marker leak check scans only `Groups`, missing `DisabledBatchAction`, `SmallScreenFallback` and `Validation` copy that the view also renders; its bare markers `"token"`/`"prompt"` matched case-insensitively with `Contains` will also false-positive on legitimate values [src/Hexalith.ChatBot.UI/Design/ChatBotApprovalQueuePriorityContract.cs:82]
- [x] [Review][Patch] Five `chatbot-*` classes used by these components have no rule in the only stylesheet — `chatbot-table`, `chatbot-row-label`, `chatbot-priority-label`, `chatbot-approval-priority-table`, `chatbot-small-screen-fallback`; the guards check for banned classes, not for classes with no backing rule [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:99]
- [x] [Review][Patch] The panel close button renders a literal lowercase `x` instead of an icon or localized glyph [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor:16]

#### Deferred

- [x] [Review][Defer] **E2E fixtures have drifted from the components they claim to prove** [tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs] — deferred, pre-existing
- [x] [Review][Defer] **Display formatting is built in the service layer** [src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs:62] — deferred, pre-existing
- [x] [Review][Defer] **Five forked copies of the `CodeRow`/`TextRow`/`TimeRow` helpers** [src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor:314] — deferred, pre-existing
- [x] [Review][Defer] **Server problem codes are rendered verbatim and mint unbounded announcement keys** [src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs:51] — deferred, pre-existing

## Dismissed as noise

- `ChatBotGovernedAction` swallowing a blocked click — it already sets `_blockedAnnouncement` and calls `StateHasChanged` (`:80-88`); the finding's "suggested guard" is the shipped code.
- `Item` null causing an NRE — the only call site (`ChatBotConversationStream.razor:38`) always passes a non-null item.
- `Groups`/`ShownPriorityMetadata` null-dereference in the contract — both are non-nullable record parameters.
- Duplicate `DisabledReasonId` from repeated transition values, and duplicate-`Transition` ordering in `ChatBotTaskIntentReviewPanel` — folded into the dead-code decision above.

## What passes

- The shared governed path is genuine: `ProjectConversationService.SubmitApprovalDecisionAsync` (`:145-172`) builds a `DecideAiActionApprovalCommand` with a deterministic decision id, expected `SourceVersion` and correlation id, submitted through `IChatBotClient` with `origin: SurfaceOrigin.Ui`. No bypass.
- Expired evidence *does* disable approval with a reachable, non-tooltip reason (`:167-181`, `:109-114`, `:132-137`) — AC2's named case is satisfied.
- Redaction suppression on the approval item is correct and non-leaking: `DisplayPolicySnapshotId` and `DisplayAuditOperationId` return `null` rather than the id, with a focusable "Why unavailable?" explanation instead.
- Forced-colors and reduced-motion rules cover this surface (`chatbot.tokens.css:347-348`, `:401-404`, `:409-413`), and the scoped bundle is linked in `App.razor`, so they load.
- No `<dl>` monospace dump remains in these files; metadata renders through `FluentStack`/`FluentText` rows.
- `SharedResource.resx` and `SharedResource.fr.resx` are at parity (877 entries each).

## Patch application record (2026-08-26)

All 51 patch findings applied across 16 files. `dotnet build src/Hexalith.ChatBot.UI` and
`dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests`: **0 errors**.

**Concurrent-writer caveat.** A `bmad-loop` session (`projects-bd`, tmux `bmad-loop-20260825-121015-999b`) was
writing to this repository throughout the patch run — 33 files it modified, several overlapping these targets.
Each file was re-read immediately before editing and every edit was a targeted string replacement, so no
concurrent change was clobbered. Two build breaks appeared mid-run in files not touched here
(`AssociationReview.razor`, then `ProjectConversationStateTests.cs`); the first resolved itself, the second was
still red at the end of the run.

**Not verified by test execution.** `tests/Hexalith.ChatBot.UI.Tests` — which holds
`Story13DefinitionListMigrationTests`, `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`
and `ChatBotApprovalQueuePriorityContractTests` — does not compile because of
`ProjectConversationStateTests.cs(206,45): CS0111` in the concurrent session's work. The suite must be run once
that clears. The UI.E2E suite was deliberately not run (a full run regenerates fixture PNGs and drifts
submodules).

**One patch applied only in part.** The queue-family filter buttons rendered raw wire tokens
(`OperationalQueueFamilies.ToWireValue`) as visible prose. They now render the token inside
`<code class="chatbot-code">` with a localized `aria-label`, which stops it reading as an untranslated sentence —
but true localization needs six new keys in `ChatBotUiTextKey`, `SharedResource.resx` and `SharedResource.fr.resx`,
all of which the concurrent session was actively editing. Deferred rather than risk a merge conflict in the
localization tables.

**Design change worth noting.** `ChatBotRiskActionClass` has no `Unknown` member and adding one would ripple into
the localizer and both resx files. Instead of asserting `ProjectMutating` for an unrecognised risk class, the risk
chip is now suppressed when no known class matches; the raw class still renders in the metadata rows.
