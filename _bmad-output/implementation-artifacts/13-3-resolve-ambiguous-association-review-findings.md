---
story: 13-3-resolve-ambiguous-association-from-a-safe-live-review-surface
spec: _bmad-output/planning-artifacts/epics.md#Story 13.3
review_date: 2026-08-25
review_mode: full
layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor
scope: full current contents of the 11 Association Review source files + 4 test files (no diff; acceptance review)
---

# Story 13.3 — Code Review Findings (2026-08-25)

Canonical Story 13.3 has no story file; acceptance criteria live in `epics.md:3296-3316`, with binding
context from `implementation-conformance-addendum-2026-07-17.md` and `EXPERIENCE.md`.

Totals: **7 decision-needed (all resolved 2026-08-25) · 38 patch · 8 deferred · 4 dismissed**

## Review Findings

### Decisions resolved (2026-08-25, Jerome)

| # | Decision | Outcome |
|---|---|---|
| 1 | Acceptance of AC1-AC3 | **AC1-AC3 accepted as unmet.** Story 13.3 returns to `in-progress`; live-route E2E for `/association-review/{id}` becomes required story work, not a review patch. |
| 2 | `CanCorrect` vs effect gate | **Narrow `CanCorrect`** to `Associated`/`Corrected`; the panel does not render during propagation. |
| 3 | `Deferred` lifecycle | **Re-decidable, stays non-terminal.** Current behaviour is correct but accidental - pin with a test and make `CanCorrect`'s omission deliberate. |
| 4 | Decision safety | **Confirm step + restricted bar.** Explicit confirm on all four decisions, rename `Preview` -> `Submit`, and with no authorized candidates leave only Defer and Mark-needs-review enabled. |
| 5 | Error-state taxonomy | **Map by code family.** Split load-vs-submit wording; authorization -> `BlockedAction`/`Denial` (no retry copy), quarantine/terminal -> `TerminalPolicyFailure`, dependency -> `DependencyDegraded`, remainder -> `RetryableFailure` with a real retry control. |
| 6 | Evidence chips | **Unnest + drive from state.** Move chips out of the candidate `FluentButton`, set `CanOpenEvidence` from `evidence.State`; the evidence drawer becomes its own scoped story (deferred). |
| 7 | Accordion carve-out | **Accept the carve-out.** Record the single-primary-workflow exception in `implementation-conformance-addendum-2026-07-17.md` so page and binding doc agree. |

### Decision-needed (resolved above; retained for traceability)

- [ ] [Review][Decision] **Story 13.3 cannot close on the evidence that exists.** The Epic 13 preamble is binding: "A story cannot close from source scans, static fixtures, screenshots without direct invariant assertions, or a diagnostic fallback. Its primary live route must execute successfully." The only decision-recording E2E (`AssociationDecisionRecordingE2ETests.cs:22,59`) calls `SetContentAsync(BuildAssociationDecisionFixture(...))` — there is no `GotoAsync` and no host anywhere in the file. The only live navigation to `/association-review/{id}` is `RealRenderCrossSurfaceE2ETests.cs:33`, which the addendum §4 explicitly forbids as first acceptance. Decision: build live-route acceptance evidence for this surface, or accept AC1-AC3 as unmet and move 13.3 back to in-progress.
- [ ] [Review][Decision] **`CanCorrect` and the correction effect disagree on two reachable lifecycle states.** `AssociationReview.razor:204` allows `Associated|Corrected|Correcting|Correction-delayed`; `AssociationReviewEffects.cs:104` rejects anything that is `not ("Associated" or "Corrected")`. In `Correcting`/`Correction-delayed` the panel renders with an **enabled** submit whose only outcome is `correction-invalid-lifecycle`. Decision: widen the effect to permit correction during propagation, or narrow `CanCorrect` so the panel does not render.
- [ ] [Review][Decision] **A single click commits an irreversible governed decision, from an action named "Preview", and the decision bar renders even with no authorized candidates.** `PreviewDecision` (`AssociationReview.razor:197`) dispatches straight to `HandlePreviewAsync` → `SubmitDecisionAsync` (`AssociationReviewEffects.cs:43`) — no preview, no confirmation, no undo. The action bar sits at `AssociationReview.razor:91`, outside the `HasAuthorizedCandidates` branch, so enabled Reject-all/Defer/Mark-needs-review render alongside the blocked state. Decision: add a confirm step and/or scope which actions remain permitted when no candidate is authorized (EXPERIENCE.md requires "the next permitted action" be exposed, which may justify Defer/Escalate but not Reject-all).
- [ ] [Review][Decision] **Every failure collapses into one `RetryableFailure` banner, including authorization denial.** `AssociationReview.razor:43-52` hardcodes `Kind=Danger, StateFamily=RetryableFailure` for any `ErrorCode`; `authorization_denied` (asserted at `AssociationReviewEffectsTests.cs:31`) therefore renders `Submission_Failed_BodyTemplate` — "Submission did not complete (code: {0}). You can try again." `ChatBotBlockedReason.Denial|Quarantine|FailedDependency` all exist and are unused. AC3 requires distinct unauthorized / quarantine-terminal / dependency-degraded states. Decision: define the error-code → state-family mapping for this surface.
- [ ] [Review][Decision] **Is `Deferred` terminal or re-decidable?** `AssociationReviewModels.cs:45` omits `Deferred` from `IsTerminal` (and `CanCorrect` omits it), so after a Defer all four decision actions stay enabled while the correction panel disappears. Decision: state whether a deferred association is re-decidable, then align `IsTerminal`/`CanCorrect`.
- [ ] [Review][Decision] **Evidence chips are focusable no-ops nested inside the candidate control.** `ChatBotAssociationCandidateRow.razor:26-35` renders `ChatBotEvidenceChip` with `CanOpenEvidence="true"` hardcoded inside a `FluentButton role="radio"`; the chip renders its own `FluentButton` and no `OnActivate` is bound at either call site, so `ActivateAsync` invokes an unassigned callback. Button-inside-button is invalid HTML, breaks the radio's accessible-name computation, and chip clicks select the candidate. EXPERIENCE.md specifies an evidence drawer that does not exist anywhere on this surface. Decision: wire the evidence drawer, or set `CanOpenEvidence` from `evidence.State` and stop advertising the affordance.
- [ ] [Review][Decision] **The page declares an accordion carve-out the addendum says does not exist.** `AssociationReview.razor:72-76` (`Candidate projects`) and `ChatBotAssociationReviewActions.razor:4-5` (`Safe next actions`) are sibling titled sections; the addendum requires grouping in one `FluentAccordion` with "Documented carve-outs: none", while `AssociationReview.razor:120-122` documents its own carve-out. The "single primary workflow" exception is plausibly applicable. Decision: accept the exception in the addendum, or group the sections.

### Patch (added by decision resolution)

- [ ] [Review][Patch] Narrow `CanCorrect` to `Associated`/`Corrected` so the correction panel does not render during propagation (decision 2) [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:204]
- [ ] [Review][Patch] Pin `Deferred` as re-decidable with a test, and make its omission from `CanCorrect` deliberate rather than accidental (decision 3) [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs:45]
- [ ] [Review][Patch] Add an explicit confirm step to all four decisions, rename `Preview` -> `Submit`, and gate the action bar so only Defer / Mark-needs-review remain enabled with no authorized candidates (decision 4) [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:91]
- [ ] [Review][Patch] Map error codes to state families by family and split load-vs-submit wording; add a retry control to the retryable branch (decision 5) [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:43]
- [ ] [Review][Patch] Unnest the evidence chips from the candidate `FluentButton` and drive `CanOpenEvidence` from `evidence.State` (decision 6) [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor:26]
- [ ] [Review][Patch] Record the single-primary-workflow accordion carve-out in the binding addendum (decision 7) [_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/implementation-conformance-addendum-2026-07-17.md:1]

### Patch

- [ ] [Review][Patch] `aria-describedby` on both textareas points at ids the app never renders — `ChatBotStatusBanner` emits only `data-chatbot-stable-id`, no `id` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:15]
- [ ] [Review][Patch] Contract assertion `comparison.ShouldContain("<dl")` is satisfied only by a code comment; the element no longer exists and `Story13DefinitionListMigrationTests` forbids it [tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs:162]
- [ ] [Review][Patch] Successful outcomes (`Associated`/`Corrected`) are announced as `TerminalPolicyFailure` with a Warning banner [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:180]
- [ ] [Review][Patch] No in-flight guard: `IsSubmitting` is written by six reducers and read by nothing on this surface, so a double-click issues two durable commands [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs:78]
- [ ] [Review][Patch] A restricted evidence reference is emitted verbatim into a DOM id exactly when it is restricted (`StableId` feeds `UnavailableReasonId`) [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor:33]
- [ ] [Review][Patch] Every candidate is labelled "Within threshold" regardless of band; the real `ThresholdBand` is mapped but never rendered and `ConfidenceBandLabel` is unused here [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor:20]
- [ ] [Review][Patch] The decision evidence fingerprint falls back to another candidate's evidence instead of failing closed [src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs:204]
- [ ] [Review][Patch] `PriorProjectId` is fabricated from an arbitrary non-target candidate and written into the audit trail; a test pins the fabrication as expected [src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs:228]
- [ ] [Review][Patch] `correction-source-required` is missing from `IsSafeValidationCode`, so it degrades to the generic `association-review-unavailable` [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs:147]
- [ ] [Review][Patch] Switching associations renders the previous one's data, and `DecisionNote` is never cleared by any reducer, so a note authored for A is submitted for B [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs:7]
- [ ] [Review][Patch] No focus management exists on the surface — zero `FocusAsync`/`ElementReference`; AC2 requires validation to move focus to a summary [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:1]
- [ ] [Review][Patch] The validation summary renders after the field it describes; EXPERIENCE.md requires the summary before the panel [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor:19]
- [ ] [Review][Patch] Loading state has no skeleton and no `aria-busy` on the busy region [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:33]
- [ ] [Review][Patch] `SafeFailureCode` passes arbitrary server-controlled text into the page body and the `AnnouncementKey` with no allowlist, unlike `IsSafeValidationCode` [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs:144]
- [ ] [Review][Patch] User-visible EN and FR consequence copy references internal delivery state ("after decision recording is available", "the backend decision story" / "l'histoire backend de décision") and is factually stale — the command path is wired [src/Hexalith.ChatBot.UI/Localization/SharedResource.resx:22]
- [ ] [Review][Patch] Two user-visible English literals are hardcoded in the service, bypassing the localizer: "Evidence restricted" and "Project candidate {Rank}" [src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs:273]
- [ ] [Review][Patch] Raw untranslated wire tokens are composed into user prose and reused as the banner message, the accessible label, and the header state [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:164]
- [ ] [Review][Story work] The decision-recording E2E verifies its own hand-authored fixture and cannot fail if the surface stops submitting, submits the wrong command, or lets a blocked action through. **Reclassified by decision 1 as required story work**, not a review patch: rewrite against the live route the way `RealRenderCrossSurfaceE2ETests` does. [tests/Hexalith.ChatBot.UI.E2E.Tests/AssociationDecisionRecordingE2ETests.cs:22]
- [ ] [Review][Patch] Chromium absence silently downgrades to string-only assertions instead of skipping; `ResolveChromeExecutable` probes only `/usr/bin/google-chrome` [tests/Hexalith.ChatBot.UI.E2E.Tests/AssociationDecisionRecordingE2ETests.cs:380]
- [ ] [Review][Patch] `HandleCorrectionAsync` has no test at all, and the preview success path never executes because all three effect tests construct the effect with a null `IState`; make the `IState` parameter required rather than optional-for-tests [tests/Hexalith.ChatBot.UI.Tests/AssociationReviewEffectsTests.cs:59]
- [ ] [Review][Patch] Action gating and disabled-reason resolution have no executing test — the gate can be inverted or re-prioritized with nothing failing; extract to a testable static class [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor:288]
- [ ] [Review][Patch] No test constructs an `AssociationReviewState` and applies a reducer; the rule that determines the correction target is unpinned [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs:14]
- [ ] [Review][Patch] Restricted-evidence suppression is verified only at the service layer, never at the two components that render it [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor:57]
- [ ] [Review][Patch] No test exercises French, tablet/phone viewports, or forced-colors for this surface, all of which AC3 names explicitly [tests/Hexalith.ChatBot.UI.E2E.Tests/AssociationDecisionRecordingE2ETests.cs:97]
- [ ] [Review][Patch] Operation identity (`CommandId`/`CorrelationId`/`TaskId`/`LifecycleState`) is computed, carried, and dropped by both submitted-reducers; the "accepted, projection pending" state is never shown [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs:92]
- [ ] [Review][Patch] A failed load is a dead end: the banner claims `RetryableFailure` but offers no retry control, and `OnParametersSet` will not re-dispatch [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:43]
- [ ] [Review][Patch] The 1024-character note cap is enforced only at submit by exception — no `maxlength`, no counter — and `NormalizeNote` silently collapses newlines into one line [src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs:237]
- [ ] [Review][Patch] Model data is fetched, mapped, passed, and never rendered: `PropagationEstimatedCompletionAtUtc`, `PriorProjectId`, `CorrectionRationale` (always null), `RequiredEvidenceComplete`, `ThresholdBand`, `RetentionClass`, `SourceThreadId`, `SupersedesAssociationId` [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor:138]
- [ ] [Review][Patch] `IsPropagationBlocking` is dead in production; its only reference is a source-string E2E assertion that exists to prevent its deletion [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs:47]
- [ ] [Review][Patch] `role="radiogroup"` is declared with no roving tabindex and no arrow-key handling; every candidate is a separate tab stop [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:79]
- [ ] [Review][Patch] A single returned candidate is auto-selected regardless of `RequiredEvidenceComplete` or disabled reasons, against "no hidden auto-association when confidence is ambiguous" [src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs:20]
- [ ] [Review][Patch] `ResolveCorrectionDisabledReasonCode` concatenates two entries the priority list already contains, making them unreachable, and allocates a fresh array on every call [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor:326]
- [ ] [Review][Patch] `WireValue<T>` silently returns the numeric string for an enum value the generated client does not define, and that number reaches the UI and the `IsTerminal`/`CanCorrect` string comparisons [src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs:321]

### Deferred (pre-existing, not caused by this surface)

- [x] [Review][Defer] Reusable components hardcode DOM ids (`association-actions-title`, `association-decision-note`, `association-comparison-title`, …), which collide if instantiated twice [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor:4] — deferred, pre-existing
- [x] [Review][Defer] Three verbatim copies of `CodeRow`/`TextRow` and two of `EvidenceText`, each with its own near-identical Story 13.4 comment [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:209] — deferred, pre-existing
- [x] [Review][Defer] The CSS conformance assertions check that `chatbot.tokens.css` contains an association selector and, separately, that the file contains responsive/forced-colors/reduced-motion blocks, with nothing tying them together [tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs:227] — deferred, pre-existing
- [x] [Review][Defer] `css.ShouldNotContain("#")` bans every `#` in the file while the raw-colour check misses `rgba(`, `hsla(`, `oklch(`, `color-mix(`, and named colours [tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs:232] — deferred, pre-existing
- [x] [Review][Defer] `AssociationReviewComponentContractTests` is a grab bag: four of its eight facts assert on ProjectConversation, task-intent, why-project, `Program.cs`, and the `.csproj` [tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs:1] — deferred, pre-existing
- [x] [Review][Defer] `ResolveEvidenceState` substring-matches seven keywords against `EvidenceKind + EvidenceReference` as a "safety net", producing both false positives and false negatives on data the server already labelled [src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs:296] — deferred, pre-existing
- [x] [Review][Defer] An empty or whitespace `AssociationId` renders the header with no loading, error, or blocked state [src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:152] — deferred, pre-existing

- [x] [Review][Defer] The evidence drawer EXPERIENCE.md specifies does not exist anywhere on this surface [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor:105] - deferred by decision 6 to its own scoped story

### Dismissed (4)

Cancellation-path findings (IsLoading/IsSubmitting latched, unreachable `catch (OperationCanceledException)` arms):
no effect passes a `CancellationToken` to the service and nothing cancels on navigation, so the branch is
unreachable in production. Playwright driver leak on a non-`PlaywrightException` launch failure: test-harness
robustness only, with no product consequence.
