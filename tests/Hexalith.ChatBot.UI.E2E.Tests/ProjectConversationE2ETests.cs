using System.Globalization;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class ProjectConversationE2ETests
{
    [Fact]
    public async Task ProjectConversationLoadingShouldExposePersistentProjectContext()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertLoadingWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Loading));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Project conversation", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Project conversation loading" }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Project context"));
            await WaitForVisibleAsync(harness.Page.GetByText("Authorized Project", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("tenant-alpha", new() { Exact = true }));
        }
    }

    [Fact]
    public async Task ProjectConversationPopulatedStreamShouldRenderOrderedMetadataOnlyItemsAndSystemDecisions()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPopulatedWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Project conversation status: current" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.List, new() { NameString = "Project conversation stream" }));
            ILocator mailboxItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox item: Mailbox intake, Associated" });
            await WaitForVisibleAsync(mailboxItem);
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Confirmed association, Associated, 2026-06-01 08:02:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Rejected association, Rejected, 2026-06-01 08:03:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Deferred association, Deferred, 2026-06-01 08:04:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Needs review, NeedsReview, 2026-06-01 08:05:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Project reassignment, Correction-delayed, 2026-06-01 08:06:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Project reassignment, Correcting, 2026-06-01 08:07:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Project reassignment, Corrected, 2026-06-01 08:08:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval requested, Pending, 2026-06-01 08:09:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval decision, Approved, 2026-06-01 08:10:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval outcome, Approved, 2026-06-01 08:11:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval outcome, Executed, 2026-06-01 08:12:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval outcome, Failed, 2026-06-01 08:13:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval decision, Rejected, 2026-06-01 08:14:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval decision, Revision requested, 2026-06-01 08:15:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval decision, Cancelled, 2026-06-01 08:16:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Retry queued, Retryable, 2026-06-01 08:17:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Retry accepted, Retryable, 2026-06-01 08:17:10Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Duplicate suppressed, Resolved, 2026-06-01 08:17:20Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Dependency degraded, Degraded, 2026-06-01 08:17:30Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Projection retryable, Retryable, 2026-06-01 08:17:40Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Refused action, Blocked, 2026-06-01 08:17:50Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Audit unavailable, Blocked, 2026-06-01 08:17:55Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Retry exhausted, Terminal, 2026-06-01 08:17:58Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Terminal failure, Terminal, 2026-06-01 08:18:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Reprocess created, Resolved, 2026-06-01 08:19:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI proposal, Proposed, 2026-06-01 08:20:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI denial, Denied, 2026-06-01 08:20:10Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI refusal, Blocked, 2026-06-01 08:20:20Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution started, Executing, 2026-06-01 08:20:30Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:20:40Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:21:00Z" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, Corrected context invalidated, Invalidated, 2026-06-01 08:21:10Z" }));
            await WaitForVisibleAsync(harness.Page.GetByText("AI-generated content is labelled and kept distinct from source evidence.").First);
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, invoice.pdf, Pending, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, release-notes.pdf, Captured, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Attachment unavailable, Unavailable, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Redacted attachment, Pending, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, duplicate-invoice.pdf, Retryable, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Attachment unavailable, Unsafe, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Internal participant: Internal contributor, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "External participant: External contributor, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Unresolved participant: Unresolved participant, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Restricted participant: Restricted participant, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByText("Decision kind", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Confirmed association", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Correction kind", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Project reassignment", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Supersedes association", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Propagation progress", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Provider attachment ID", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("graph-attachment-001", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("invoice.pdf", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Capture status", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Storage status", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Scan status", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Allowed review actions", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Why unavailable?", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Approval status", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Command outcome status", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("accepted-projection-pending", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Policy snapshot detail is redacted or unavailable on this surface.").First);
            await WaitForVisibleAsync(harness.Page.GetByText("Audit detail is unavailable on this surface.").First);
            await WaitForVisibleAsync(harness.Page.GetByText("Source", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Microsoft 365 mailbox", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Mailbox", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("controlled-mailbox-001", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Provider message ID", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("graph-message-001", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Internet message ID", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("<internet-message-001@example.test>", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Thread", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("graph-thread-001", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Sent", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Created", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Source timezone", new() { Exact = true }).First);
            await WaitForVisibleAsync(harness.Page.GetByText("Correlation ID", new() { Exact = true }).First);
            await AssertAssociatedEmailMetadataAsync(mailboxItem);
            ILocator confirmedDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:3']");
            ILocator rejectedDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:4']");
            ILocator deferredDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:5']");
            ILocator needsReviewDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:6']");
            ILocator correctionDelayedDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:7']");
            ILocator correctingDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:8']");
            ILocator correctedDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='decision:01HZXASSOC000000000000001:9']");
            ILocator approvalRequest = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-001:request:10']");
            ILocator approvedDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-001:decision:11']");
            ILocator projectionPendingOutcome = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-001:outcome:12']");
            ILocator executedOutcome = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-001:outcome:13']");
            ILocator failedOutcome = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-002:outcome:14']");
            ILocator rejectedApprovalDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-002:decision:15']");
            ILocator revisionDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-003:decision:16']");
            ILocator cancelledDecision = harness.Page.Locator("[data-chatbot-conversation-item-id='approval:approval-004:decision:17']");
            ILocator retryQueuedFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-001:retry-queued:18']");
            ILocator retryAcceptedFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-001:retry-accepted:19']");
            ILocator duplicateSuppressedFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-001:duplicate-suppressed:20']");
            ILocator dependencyDegradedFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-002:dependency-degraded:21']");
            ILocator projectionRetryableFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-003:projection-retryable:22']");
            ILocator policyBlockedFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-004:blocked:23']");
            ILocator auditUnavailableFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-005:blocked:24']");
            ILocator retryExhaustedFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-001:retry-exhausted:25']");
            ILocator terminalFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-001:terminal-failure:26']");
            ILocator reprocessFailure = harness.Page.Locator("[data-chatbot-conversation-item-id='failure:operation-001:reprocess-created:27']");

            await AssertDecisionMetadataAsync(
                confirmedDecision,
                [
                    "Decision kind",
                    "Lifecycle state",
                    "Decision actor",
                    "Decision actor type",
                    "Decided at",
                    "Confidence",
                    "Threshold band",
                    "Surface origin",
                    "Policy snapshot",
                    "Evidence references",
                    "Safe next actions",
                    "Redaction state",
                    "Decision note state",
                    "Retention class",
                    "Schema version",
                    "Source version",
                    "Correlation ID",
                ],
                [
                    "mailbox:intake:subject",
                    "91%",
                    "Associated",
                    "System decision",
                    "2026-06-01 08:02:00Z",
                    "Decision kind",
                    "Confirmed association",
                    "associate",
                    "Decision actor",
                    "user-001",
                    "Decision actor type",
                    "human",
                    "Evidence references",
                    "mailbox:intake:subject",
                    "Decision note state",
                    "Redacted",
                    "redacted",
                    "Retention class",
                    "collaboration_input",
                    "Schema version",
                    "chatbot.project-conversation-item.v1",
                    "Source version",
                    "3",
                    "Correlation ID",
                    "01HZXCORRELATION00000000002",
                ]);
            await AssertDecisionMetadataAsync(
                retryQueuedFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Retry queued",
                    "Failure status",
                    "Retryable",
                    "Catalog code",
                    "retry_queued",
                    "Retry count",
                    "1 of 3",
                    "Operation ID",
                    "operation-001",
                    "Duplicate safety",
                    "duplicate-safe",
                    "Next action",
                    "Retry later when the governed dependency recovers.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                retryAcceptedFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Retry accepted",
                    "Failure status",
                    "Retryable",
                    "Catalog code",
                    "retry_accepted",
                    "Retry count",
                    "2 of 3",
                    "Last retry",
                    "2026-06-01 08:17:10Z",
                    "Retry operation",
                    "retry-operation-002",
                    "Safe next actions",
                    "retry-later",
                    "Next action",
                    "Retry later when the governed dependency recovers.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                duplicateSuppressedFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Duplicate suppressed",
                    "Failure status",
                    "Resolved",
                    "Catalog code",
                    "duplicate_suppressed",
                    "Duplicate safety",
                    "duplicate-suppressed",
                    "Duplicate suppression",
                    "duplicate-suppression-001",
                    "Safe next actions",
                    "none",
                    "Duplicate safety",
                    "Retries and duplicate suppression use governed metadata and do not replace prior history.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                dependencyDegradedFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Dependency degraded",
                    "Failure status",
                    "Degraded",
                    "Catalog code",
                    "dependency_degraded",
                    "Blocked reason",
                    "Dependency degraded",
                    "Dependency",
                    "mailbox-projection",
                    "Degraded until",
                    "2026-06-01 08:47:30Z",
                    "Next action",
                    "Wait for dependency recovery.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                projectionRetryableFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Projection retryable",
                    "Failure status",
                    "Retryable",
                    "Catalog code",
                    "projection_retryable",
                    "Blocked reason",
                    "Projection unavailable",
                    "Failure scope",
                    "project-conversation",
                    "Safe next actions",
                    "retry-later",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                policyBlockedFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Blocked",
                    "Failure status",
                    "Blocked",
                    "Catalog code",
                    "refusal_blocked_action",
                    "Blocked reason",
                    "Policy blocked",
                    "Failure reason",
                    "policy-blocked",
                    "Safe next actions",
                    "review-policy",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                auditUnavailableFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Blocked",
                    "Failure status",
                    "Blocked",
                    "Catalog code",
                    "audit_unavailable",
                    "Blocked reason",
                    "Audit unavailable",
                    "Audit status",
                    "unavailable",
                    "Why unavailable?",
                    "Audit operation detail is redacted or unavailable on this surface.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                retryExhaustedFailure,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Retry exhausted",
                    "Failure status",
                    "Terminal",
                    "Catalog code",
                    "retry_exhausted",
                    "Blocked reason",
                    "Retry exhausted",
                    "Retryable",
                    "No",
                    "Retry count",
                    "3 of 3",
                    "Terminal rule",
                    "Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                terminalFailure,
                expectedOrderedMarkers:
                [
                    "Terminal failure",
                    "Failure status",
                    "Terminal",
                    "Blocked reason",
                    "terminal-state",
                    "Retryable",
                    "No",
                    "Audit status",
                    "unavailable",
                    "Terminal rule",
                    "Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                reprocessFailure,
                expectedOrderedMarkers:
                [
                    "Reprocess created",
                    "Failure status",
                    "Resolved",
                    "Catalog code",
                    "reprocess_created",
                    "Reprocess workflow",
                    "workflow-002",
                    "Supersedes workflow",
                    "workflow-001",
                    "Terminal rule",
                    "Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.",
                ],
                expectedAccessibleNamePrefix: "System status,");
            await AssertDecisionMetadataAsync(
                rejectedDecision,
                expectedOrderedMarkers:
                [
                    "Rejected association",
                    "reject",
                    "Decision actor",
                    "system-policy",
                    "Threshold band",
                    "Manual",
                    "Decision note state",
                    "Redacted",
                    "Correlation ID",
                    "01HZXCORRELATION00000000003",
                ]);
            await AssertDecisionMetadataAsync(
                deferredDecision,
                expectedOrderedMarkers:
                [
                    "Deferred association",
                    "defer",
                    "Policy snapshot",
                    "association-thresholds.m0.default.v1",
                    "Safe next actions",
                    "review-later",
                    "Source version",
                    "5",
                ]);
            await AssertDecisionMetadataAsync(
                needsReviewDecision,
                expectedOrderedMarkers:
                [
                    "Needs review",
                    "needs-review",
                    "Safe next actions",
                    "open-review",
                    "Decision note state",
                    "Unavailable",
                    "Source version",
                    "6",
                    "Why unavailable?",
                    "Decision detail is unavailable on this surface.",
                ]);
            ILocator decisionReason = needsReviewDecision.Locator(".chatbot-decision-conversation-item__reason");
            (await decisionReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await decisionReason.FocusAsync();
            (await decisionReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();
            await AssertDecisionMetadataAsync(
                correctionDelayedDecision,
                [
                    "Correction kind",
                    "Lifecycle state",
                    "Correction actor",
                    "Correction actor type",
                    "Corrected at",
                    "Confidence",
                    "Threshold band",
                    "Prior project",
                    "Corrected project",
                    "Predecessor association",
                    "Supersedes association",
                    "Superseded by association",
                    "Downstream impact status",
                    "Correction ID",
                    "Workflow instance",
                    "Required stores",
                    "Failed stores",
                    "Propagation progress",
                    "Propagation started",
                    "Propagation estimated completion",
                    "Propagation status",
                    "Corrected context stale",
                    "Responsible owner role",
                    "Safe next actions",
                    "Redaction state",
                    "Correction rationale state",
                    "Retention class",
                    "Schema version",
                    "Source version",
                    "Correlation ID",
                ],
                [
                    "Project reassignment",
                    "project-reassignment",
                    "Correction actor",
                    "user-002",
                    "Prior project",
                    "project-redacted",
                    "Corrected project",
                    "project-alpha",
                    "Predecessor association",
                    "01HZXASSOC000000000000000",
                    "Supersedes association",
                    "01HZXASSOC000000000000000",
                    "Superseded by association",
                    "01HZXASSOC000000000000002",
                    "Propagation progress",
                    "1 of 2",
                    "Correction rationale state",
                    "Redacted",
                    "redacted",
                    "Source version",
                    "7",
                ]);
            await AssertDecisionMetadataAsync(
                correctingDecision,
                expectedOrderedMarkers:
                [
                    "Project reassignment",
                    "Correcting",
                    "Downstream impact status",
                    "correcting",
                    "Corrected context stale",
                    "True",
                    "Responsible owner role",
                    "operations",
                    "Source version",
                    "8",
                ]);
            await AssertDecisionMetadataAsync(
                correctedDecision,
                expectedOrderedMarkers:
                [
                    "Project reassignment",
                    "Corrected",
                    "Completed stores",
                    "project-conversation, participants",
                    "Propagation progress",
                    "2 of 2",
                    "Propagation completed",
                    "2026-06-01 08:08:30Z",
                    "Source version",
                    "9",
                ]);
            await AssertApprovalMetadataAsync(
                approvalRequest,
                expectedOrderedMarkers:
                [
                    "Pending",
                    "Approval event",
                    "2026-06-01 08:09:00Z",
                    "Approval event kind",
                    "Approval requested",
                    "request",
                    "Approval status",
                    "pending",
                    "Approval ID",
                    "approval-001",
                    "Evidence freshness",
                    "Expired",
                    "expired",
                    "Disabled reason",
                    "Evidence expired",
                    "evidence-expired",
                    "Safe next actions",
                    "await-approval",
                    "Why unavailable?",
                    "Policy snapshot detail is redacted or unavailable on this surface.",
                ]);
            await AssertApprovalMetadataAsync(
                approvedDecision,
                expectedOrderedMarkers:
                [
                    "Approved",
                    "Approval event",
                    "2026-06-01 08:10:00Z",
                    "Approval event kind",
                    "Approval decision",
                    "decision",
                    "Approval status",
                    "approved",
                    "Approval decision",
                    "approve",
                    "Decision actor",
                    "approver-001",
                    "Authority result",
                    "authorized",
                    "Decision rationale state",
                    "Redacted",
                    "redacted",
                    "Audit operation",
                    "audit-approval-001",
                    "Audit status",
                    "committed",
                    "Supersedes approval",
                    "approval-000",
                ]);
            await AssertApprovalMetadataAsync(
                projectionPendingOutcome,
                expectedOrderedMarkers:
                [
                    "Approved",
                    "Approval event",
                    "2026-06-01 08:11:00Z",
                    "Approval event kind",
                    "Approval outcome",
                    "outcome",
                    "Approval status",
                    "approved",
                    "Command name",
                    "SendExternalReply",
                    "Command outcome status",
                    "accepted-projection-pending",
                    "Audit status",
                    "reconciling",
                    "Outcome at",
                    "2026-06-01 08:11:00Z",
                ]);
            await AssertApprovalMetadataAsync(
                executedOutcome,
                expectedOrderedMarkers:
                [
                    "Executed",
                    "Approval event",
                    "2026-06-01 08:12:00Z",
                    "Approval status",
                    "Executed",
                    "executed",
                    "Command outcome status",
                    "completed",
                    "Projected outcome item",
                    "outcome:item:001",
                ]);
            await AssertApprovalMetadataAsync(
                failedOutcome,
                expectedOrderedMarkers:
                [
                    "Failed",
                    "Approval event",
                    "2026-06-01 08:13:00Z",
                    "Approval status",
                    "Failed",
                    "failed",
                    "Command outcome status",
                    "failed",
                    "Failure code",
                    "command-refused",
                    "Retryability",
                    "retryable",
                    "Audit status",
                    "unavailable",
                    "Why unavailable?",
                    "Audit detail is unavailable on this surface.",
                ]);
            await AssertApprovalMetadataAsync(
                rejectedApprovalDecision,
                expectedOrderedMarkers:
                [
                    "Rejected",
                    "Approval event",
                    "2026-06-01 08:14:00Z",
                    "Approval event kind",
                    "Approval decision",
                    "decision",
                    "Approval status",
                    "rejected",
                    "Approval decision",
                    "reject",
                    "Decision actor",
                    "system-policy",
                    "Authority result",
                    "denied",
                    "Disabled reason",
                    "Insufficient authority",
                    "insufficient-authority",
                    "Decision rationale state",
                    "Unavailable",
                    "unavailable",
                    "Audit status",
                    "unavailable",
                    "Superseded by approval",
                    "approval-003",
                    "Why unavailable?",
                    "Audit detail is unavailable on this surface.",
                ]);
            await AssertApprovalMetadataAsync(
                revisionDecision,
                expectedOrderedMarkers:
                [
                    "Revision requested",
                    "Approval event",
                    "2026-06-01 08:15:00Z",
                    "Approval event kind",
                    "Approval decision",
                    "decision",
                    "Approval status",
                    "revision-requested",
                    "Approval decision",
                    "request-revision",
                    "Decision actor",
                    "approver-002",
                    "Authority result",
                    "authorized",
                    "Decision rationale state",
                    "Redacted",
                    "redacted",
                    "Supersedes approval",
                    "approval-002",
                    "Safe next actions",
                    "revise-proposal",
                ]);
            await AssertApprovalMetadataAsync(
                cancelledDecision,
                expectedOrderedMarkers:
                [
                    "Cancelled",
                    "Approval event",
                    "2026-06-01 08:16:00Z",
                    "Approval event kind",
                    "Approval decision",
                    "decision",
                    "Approval status",
                    "cancelled",
                    "Approval decision",
                    "cancel",
                    "Decision actor",
                    "requester-001",
                    "Authority result",
                    "authorized",
                    "Safe next actions",
                    "none",
                ]);

            ILocator approvalPolicyReason = approvalRequest.Locator(".chatbot-approval-conversation-item__reason").First;
            (await approvalPolicyReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await approvalPolicyReason.FocusAsync();
            (await approvalPolicyReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();
            ILocator approvalAuditReason = failedOutcome.Locator(".chatbot-approval-conversation-item__reason").First;
            (await approvalAuditReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await approvalAuditReason.FocusAsync();
            (await approvalAuditReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();
            ILocator failureReason = retryQueuedFailure.Locator(".chatbot-failure-conversation-item__reason").First;
            (await failureReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await failureReason.FocusAsync();
            (await failureReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            IReadOnlyList<string> itemIds = await harness.Page
                .Locator("[data-chatbot-conversation-item-id]")
                .EvaluateAllAsync<string[]>("items => items.map(item => item.getAttribute('data-chatbot-conversation-item-id'))");
            itemIds.ShouldBe(
                [
                    "01HZXMAILBOX000000000000001",
                    "decision:01HZXASSOC000000000000001:3",
                    "decision:01HZXASSOC000000000000001:4",
                    "decision:01HZXASSOC000000000000001:5",
                    "decision:01HZXASSOC000000000000001:6",
                    "decision:01HZXASSOC000000000000001:7",
                    "decision:01HZXASSOC000000000000001:8",
                    "decision:01HZXASSOC000000000000001:9",
                    "approval:approval-001:request:10",
                    "approval:approval-001:decision:11",
                    "approval:approval-001:outcome:12",
                    "approval:approval-001:outcome:13",
                    "approval:approval-002:outcome:14",
                    "approval:approval-002:decision:15",
                    "approval:approval-003:decision:16",
                    "approval:approval-004:decision:17",
                    "failure:operation-001:retry-queued:18",
                    "failure:operation-001:retry-accepted:19",
                    "failure:operation-001:duplicate-suppressed:20",
                    "failure:operation-002:dependency-degraded:21",
                    "failure:operation-003:projection-retryable:22",
                    "failure:operation-004:blocked:23",
                    "failure:operation-005:blocked:24",
                    "failure:operation-001:retry-exhausted:25",
                    "failure:operation-001:terminal-failure:26",
                    "failure:operation-001:reprocess-created:27",
                    "ai:proposal-001:proposal:30",
                    "ai:proposal-001:denial:31",
                    "ai:proposal-001:refusal:32",
                    "ai:proposal-001:execution-started:33",
                    "ai:proposal-001:execution-succeeded:34",
                    "ai:proposal-001:execution-failed:35",
                    "ai:proposal-001:outcome-recorded:36",
                    "ai:proposal-001:corrected-context-invalidated:37",
                    "attachment:01HZXASSOC000000000000001:0:826F",
                    "attachment:01HZXASSOC000000000000001:1:4A1B",
                    "attachment:01HZXASSOC000000000000001:2:9F20",
                    "attachment:01HZXASSOC000000000000001:3:D4C2",
                    "attachment:01HZXASSOC000000000000001:4:70EA",
                    "attachment:01HZXASSOC000000000000001:5:13B7",
                    "participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000001",
                    "participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000002",
                    "participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000003",
                    "participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000004",
                ]);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("Done", Case.Insensitive);
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationStatusSummaryShouldExposeOrderedFacetsAndProjectionPendingPartialSuccess()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertStatusSummaryCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator summary = harness.Page.GetByLabel("Status summary for item approval:approval-001:outcome:12");
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("aria-live")).ShouldBe("off");

            IReadOnlyList<string> domains = await summary
                .Locator("[data-chatbot-status-domain]")
                .EvaluateAllAsync<string[]>("facets => facets.map(facet => facet.getAttribute('data-chatbot-status-domain') || '')");
            domains.ShouldBe(
                [
                    "association",
                    "attachment",
                    "task",
                    "approval",
                    "command",
                    "failure",
                    "retry",
                    "next-action",
                ],
                ignoreOrder: false);

            IReadOnlyList<string> health = await summary
                .Locator("[data-chatbot-health]")
                .EvaluateAllAsync<string[]>("facets => facets.map(facet => facet.getAttribute('data-chatbot-health') || '')");
            health.ShouldBe(["healthy", "unknown", "unknown", "healthy", "degraded", "unknown", "unknown", "degraded"], ignoreOrder: false);

            string summaryText = await summary.InnerTextAsync();
            AssertTextOrder(
                summaryText,
                "Status and next action",
                "Association",
                "Healthy",
                "Source state",
                "associated",
                "Attachment",
                "Unknown",
                "Task",
                "Unknown",
                "Approval",
                "Healthy",
                "approved",
                "Command",
                "Degraded",
                "accepted-projection-pending",
                "Wait for projection",
                "operation-approval-001",
                "Completion status",
                "accepted-projection-pending",
                "Projection status",
                "accepted-projection-pending",
                "Audit status",
                "reconciling",
                "Correlation id",
                "01HZXCORRELATION00000000012",
                "duplicate-safe",
                "Accepted; projection is pending.",
                "Failure",
                "Unknown",
                "Retry",
                "Unknown",
                "Next action",
                "Degraded");

            ILocator partialSuccess = summary.Locator(".chatbot-conversation-status-summary__reason");
            (await partialSuccess.GetAttributeAsync("role")).ShouldBe("status");
            (await partialSuccess.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await partialSuccess.GetAttributeAsync("data-chatbot-live-announced")).ShouldBe("true");
            (await partialSuccess.GetAttributeAsync("tabindex")).ShouldBe("0");
            await partialSuccess.FocusAsync();
            (await partialSuccess.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            summaryText.ShouldNotContain("Done", Case.Insensitive);
            summaryText.ShouldNotContain("executed", Case.Insensitive);
            AssertMetadataOnlyBody(summaryText);
        }
    }

    [Fact]
    public async Task ProjectConversationStatusSummaryShouldRemainReachableOnMobileForcedColorsAndRetryableFailure()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertStatusSummaryCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator summary = harness.Page.GetByLabel("Status summary for item failure:operation-001:retry-queued:18");
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("aria-live")).ShouldBe("off");
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

            IReadOnlyList<string> domains = await summary
                .Locator("[data-chatbot-status-domain]")
                .EvaluateAllAsync<string[]>("facets => facets.map(facet => facet.getAttribute('data-chatbot-status-domain') || '')");
            domains.ShouldBe(["failure", "retry", "next-action"], ignoreOrder: false);

            string summaryText = await summary.InnerTextAsync();
            AssertTextOrder(
                summaryText,
                "Failure",
                "Degraded",
                "retry-queued",
                "operation-001",
                "01HZXCORRELATION00000000018",
                "Retry",
                "Degraded",
                "Retry count",
                "1",
                "Duplicate safety",
                "duplicate-safe",
                "Next action",
                "Degraded",
                "Retry later",
                "retry-later");

            string animationName = await summary.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string transitionDuration = await summary.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            animationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(transitionDuration);

            LocatorBoundingBoxResult? box = await summary.BoundingBoxAsync();
            box.ShouldNotBeNull();
            box.Width.ShouldBeLessThanOrEqualTo(390);
            summaryText.ShouldNotContain("raw command payload", Case.Insensitive);
            AssertMetadataOnlyBody(summaryText);
        }
    }

    [Fact]
    public async Task ProjectConversationShouldRenderClassificationDetectedIntentAiSummaryDefaultAndReviewHistory()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertClassificationCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Classification));

            // AC1/AC6: classification carries a visible badge sourced from an explicit data attribute, surviving forced colours.
            IReadOnlyList<string> badges = await harness.Page
                .Locator("[data-chatbot-classification]")
                .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('data-chatbot-classification') || '')");
            badges.ShouldBe(["informational", "actionable", "informational"], ignoreOrder: false);

            ILocator informational = harness.Page.GetByLabel("Classification for item 01HZXMAILBOX000000000000011");
            await WaitForVisibleAsync(informational);
            AssertTextOrder(
                await informational.InnerTextAsync(),
                "Informational",
                "informational",
                "Classification kernel",
                "classification-deterministic.kernel.m0.v1",
                "Confidence",
                "88%",
                "Explanation code",
                "classification_informational_notice",
                "Source evidence",
                "mailbox:subject-offset, mailbox:body-offset",
                "Redaction state",
                "metadata_only");

            // AC2: actionable items surface detected intent, action kind, and one safe next action (display only).
            ILocator actionable = harness.Page.GetByLabel("Classification for item ai:proposal-002:outcome-recorded:38");
            await WaitForVisibleAsync(actionable);
            AssertTextOrder(
                await actionable.InnerTextAsync(),
                "Actionable",
                "actionable",
                "Detected intent",
                "Approve the renewal request",
                "Detected action kind",
                "Request decision",
                "request-decision",
                "Source evidence",
                "message:offset:001, message:offset:002",
                "Explanation code",
                "task_intent_captured",
                "Safe next actions",
                "review-task-intent-action",
                "Redaction state",
                "metadata_only");

            // AC3/AC6: source evidence is the default view; the AI summary is an opt-in, labelled, collapsible disclosure.
            ILocator aiItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:30:00Z" });
            ILocator sourceEvidence = aiItem.Locator("[data-chatbot-ai-content='source-evidence']");
            await WaitForVisibleAsync(sourceEvidence);
            ILocator aiSummary = aiItem.Locator("details[data-chatbot-ai-content='ai-summary']");
            (await aiSummary.GetAttributeAsync("open")).ShouldBeNull();

            ILocator provenance = harness.Page.GetByText("Generated by chatbot-orchestrator+v1 at 2026-06-01 08:30:00Z from ai:evidence-1, ai:evidence-2");
            (await provenance.IsVisibleAsync()).ShouldBeFalse();
            AssertTextOrder(
                await aiItem.InnerTextAsync(),
                "Source evidence references are governed metadata, separate from AI-generated content.",
                "AI summary");

            await aiItem.Locator("details[data-chatbot-ai-content='ai-summary'] > summary").ClickAsync();
            await WaitForVisibleAsync(provenance);
            (await aiSummary.GetAttributeAsync("open")).ShouldNotBeNull();

            // AC4/AC6: append-only review history rendered chronologically with unique accessible names per item.
            ILocator emailHistory = harness.Page.GetByLabel("Review history for item 01HZXMAILBOX000000000000011");
            ILocator aiHistory = harness.Page.GetByLabel("Review history for item ai:proposal-002:outcome-recorded:38");
            await WaitForVisibleAsync(emailHistory);
            await WaitForVisibleAsync(aiHistory);
            (await emailHistory.GetAttributeAsync("aria-live")).ShouldBe("off");
            (await aiHistory.Locator(".chatbot-conversation-review-history__entry").CountAsync()).ShouldBe(2);
            AssertTextOrder(
                await aiHistory.InnerTextAsync(),
                "Review history",
                "approval-requested",
                "2026-06-01 08:28:00Z",
                "approval-decided",
                "2026-06-01 08:29:00Z",
                "Redacted",
                "redacted");

            // AC5: redacted classification keeps a safe, explicit badge with a reachable inline explanation.
            ILocator redacted = harness.Page.GetByLabel("Classification for item 01HZXMAILBOX000000000000012");
            await WaitForVisibleAsync(redacted);
            AssertTextOrder(
                await redacted.InnerTextAsync(),
                "Informational",
                "classification_source_redacted",
                "Redaction state",
                "Redacted",
                "redacted");

            // AC6: accessibility floor — forced colours, reduced motion, and a phone-width layout.
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();
            LocatorBoundingBoxResult? box = await actionable.BoundingBoxAsync();
            box.ShouldNotBeNull();
            box.Width.ShouldBeLessThanOrEqualTo(390);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("ignore previous instructions", Case.Insensitive);
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task TaskIntentReviewPanelShouldExposeReviewConversionAndDispositionWorkflow()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertTaskIntentReviewPanelCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(412, 915);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.TaskIntentReview));

            ILocator review = harness.Page.GetByRole(AriaRole.Region, new() { NameString = "Task intent review" });
            await WaitForVisibleAsync(review);
            await WaitForVisibleAsync(review.GetByRole(AriaRole.Region, new() { NameString = "Source message" }));
            AssertTextOrder(
                await review.InnerTextAsync(),
                "task-intent:review-001",
                "Project",
                "project-alpha",
                "Detected intent",
                "Create a follow-up task for the renewal",
                "Detected action kind",
                "request-action",
                "Source evidence",
                "message:offset:001, message:offset:002",
                "Correction readiness",
                "ready",
                "Current state",
                "captured",
                "Available transitions",
                "Convert to AI action",
                "Not actionable",
                "Duplicate",
                "Already handled",
                "Out of scope",
                "Policy blocked",
                "task_intent_policy_blocked",
                "Audit history",
                "audit-transition-001");

            ILocator sourceMessage = review.GetByRole(AriaRole.Region, new() { NameString = "Source message" });
            await sourceMessage.Locator("pre").FocusAsync();
            (await sourceMessage.Locator("pre").EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator blockedReason = review.Locator("#task-intent-review-policy-blocked-reason");
            await WaitForVisibleAsync(blockedReason);
            await blockedReason.FocusAsync();
            (await blockedReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator duplicateButton = review.GetByRole(AriaRole.Button, new() { NameString = "Duplicate" });
            await duplicateButton.ClickAsync();
            await WaitForVisibleAsync(review.GetByRole(AriaRole.Alert));
            await WaitForVisibleAsync(review.GetByRole(AriaRole.Status, new() { NameString = "Task intent transition status" }).GetByText("predecessor_task_intent_required"));

            ILocator predecessor = review.GetByLabel("Predecessor task intent");
            await predecessor.FillAsync("task-intent:prior-001");
            await duplicateButton.ClickAsync();
            await WaitForVisibleAsync(review.GetByRole(AriaRole.Status, new() { NameString = "Task intent transition status" }).GetByText("duplicate"));

            await review.GetByRole(AriaRole.Button, new() { NameString = "Convert to AI action" }).ClickAsync();
            await WaitForVisibleAsync(review.GetByRole(AriaRole.Status, new() { NameString = "Task intent transition status" }).GetByText("convert"));

            ILocator unavailable = harness.Page.GetByRole(AriaRole.Region, new() { NameString = "Task intent review unavailable" });
            await WaitForVisibleAsync(unavailable);
            AssertTextOrder(
                await unavailable.InnerTextAsync(),
                "Review unavailable",
                "task_intent_source_unavailable",
                "safe-not-found",
                "verify-access");
            (await unavailable.GetByRole(AriaRole.Region, new() { NameString = "Source message" }).CountAsync()).ShouldBe(0);

            string unavailableText = await unavailable.InnerTextAsync();
            unavailableText.ShouldNotContain("graph-message-001", Case.Insensitive);
            unavailableText.ShouldNotContain("tenant-beta", Case.Insensitive);
            unavailableText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            unavailableText.ShouldNotContain("raw provider payload", Case.Insensitive);
        }
    }

    [Fact]
    public async Task ApprovalDecisionSurfaceShouldExposeFocusableControlsFreshnessAndLiveOutcomes()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertApprovalDecisionSurfaceCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(412, 915);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.ApprovalDecisionSurface));

            ILocator approval = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval requested, Pending, 2026-06-01 08:09:00Z" });
            await WaitForVisibleAsync(approval);
            AssertTextOrder(
                await approval.InnerTextAsync(),
                "Command name",
                "Project.AppendConversationMessage",
                "Command allowlist version",
                "allowlist.v0",
                "Risk class",
                "approval-required",
                "Risk action classes",
                "modifies-state, exposes-files, invokes-tools",
                "Risk input tuple",
                "command=Project.AppendConversationMessage;effect=project-state;authority=project-contributor;policy=approval-required",
                "Evidence freshness",
                "Fresh, Stale, Expired",
                "Recipients",
                "project:conversation",
                "Sender authority",
                "project-contributor",
                "Expected post-state",
                "Metadata only",
                "Approved",
                "Rejected",
                "Requested revision",
                "Cancelled");

            ILocator freshnessChips = approval.Locator("[data-chatbot-approval-evidence-freshness]");
            (await freshnessChips.CountAsync()).ShouldBe(3);
            (await freshnessChips.Nth(0).GetAttributeAsync("data-chatbot-approval-evidence-freshness")).ShouldBe("fresh");
            (await freshnessChips.Nth(1).GetAttributeAsync("data-chatbot-approval-evidence-freshness")).ShouldBe("stale");
            (await freshnessChips.Nth(2).GetAttributeAsync("data-chatbot-approval-evidence-freshness")).ShouldBe("expired");
            (await freshnessChips.Nth(2).GetAttributeAsync("aria-disabled")).ShouldBe("true");
            await freshnessChips.Nth(2).FocusAsync();
            (await freshnessChips.Nth(2).EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator approve = approval.GetByRole(AriaRole.Button, new() { NameString = "Approved" });
            (await approve.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await approve.GetAttributeAsync("aria-describedby")).ShouldBe("approval-approve-reason");
            await approve.FocusAsync();
            (await approve.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();
            await approve.ClickAsync();
            await WaitForVisibleAsync(approval.GetByRole(AriaRole.Alert).GetByText("Evidence expired"));

            await approval.GetByRole(AriaRole.Button, new() { NameString = "Rejected" }).ClickAsync();
            await WaitForVisibleAsync(approval.GetByRole(AriaRole.Status, new() { NameString = "Approval decision status" }).GetByText("Rejected"));
            await approval.GetByRole(AriaRole.Button, new() { NameString = "Requested revision" }).ClickAsync();
            await WaitForVisibleAsync(approval.GetByRole(AriaRole.Status, new() { NameString = "Approval decision status" }).GetByText("Requested revision"));
            await approval.GetByRole(AriaRole.Button, new() { NameString = "Cancelled" }).ClickAsync();
            await WaitForVisibleAsync(approval.GetByRole(AriaRole.Status, new() { NameString = "Approval decision status" }).GetByText("Cancelled"));

            string text = await approval.InnerTextAsync();
            text.ShouldNotContain("raw prompt", Case.Insensitive);
            text.ShouldNotContain("raw provider payload", Case.Insensitive);
            text.ShouldNotContain("tenant-beta", Case.Insensitive);
        }
    }

    [Fact]
    public async Task CorrectedContextInvalidatedApprovalShouldFailClosedAndKeepReasonReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertCorrectedContextInvalidatedApprovalCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((int width, int height) in new[] { (390, 844), (768, 1024) })
            {
                await harness.Page.SetViewportSizeAsync(width, height);
                await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.CorrectedContextInvalidatedApproval));

                ILocator reviewPanel = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval requested, Invalidated, 2026-06-01 09:20:00Z" });
                await WaitForVisibleAsync(reviewPanel);
                (await reviewPanel.GetAttributeAsync("tabindex")).ShouldBe("0");

                ILocator currentUserInvalidation = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Corrected context invalidated: approval approval-corrected-001 is no longer available. Next action: review-source-evidence." });
                await WaitForVisibleAsync(currentUserInvalidation);
                (await currentUserInvalidation.GetAttributeAsync("aria-live")).ShouldBe("assertive");
                (await currentUserInvalidation.GetAttributeAsync("data-chatbot-feedback-state")).ShouldBe("CurrentUserTerminalInvalidation");
                (await currentUserInvalidation.GetAttributeAsync("data-chatbot-refusal-reason")).ShouldBe("corrected-context-invalidated");

                ILocator historicalInvalidations = harness.Page.GetByLabel("Historical invalidations");
                await WaitForVisibleAsync(historicalInvalidations);
                (await historicalInvalidations.GetAttributeAsync("aria-live")).ShouldBe("off");
                (await historicalInvalidations.GetAttributeAsync("role")).ShouldBeNull();
                (await historicalInvalidations.GetByRole(AriaRole.Alert).CountAsync()).ShouldBe(0);
                (await historicalInvalidations.GetByRole(AriaRole.Status).CountAsync()).ShouldBe(0);

                ILocator approve = reviewPanel.GetByRole(AriaRole.Button, new() { NameString = "Approve action" });
                (await approve.GetAttributeAsync("aria-disabled")).ShouldBe("true");
                (await approve.GetAttributeAsync("aria-describedby")).ShouldBe("corrected-approval-disabled-reason");
                await approve.FocusAsync();
                (await approve.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();
                await harness.Page.Keyboard.PressAsync("Enter");
                (await harness.Page.EvaluateAsync<int>("() => window.__approvalSubmitCount")).ShouldBe(0);
                (await reviewPanel.EvaluateAsync<bool>("panel => panel.contains(document.activeElement)")).ShouldBeTrue();

                ILocator reason = harness.Page.GetByLabel("Why unavailable? Corrected context invalidated. Review source evidence before requesting a new AI proposal.");
                await WaitForVisibleAsync(reason);
                await reason.FocusAsync();
                (await reason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

                await AssertApprovalMetadataAsync(
                    reviewPanel,
                    expectedOrderedMarkers:
                    [
                        "Invalidated",
                        "Approval event",
                        "2026-06-01 09:20:00Z",
                        "Approval event kind",
                        "Approval requested",
                        "Approval status",
                        "Invalidated",
                        "corrected-context-invalidated",
                        "Correction ID",
                        "correction-4-9-001",
                        "Association ID",
                        "association-4-9-001",
                        "Source version",
                        "12",
                        "Corrected evidence state",
                        "metadata_only",
                        "Safe next actions",
                        "review-source-evidence",
                        "retry-later",
                    ]);

                (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
                (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

                LocatorBoundingBoxResult? panelBox = await reviewPanel.BoundingBoxAsync();
                panelBox.ShouldNotBeNull();
                panelBox.Width.ShouldBeLessThanOrEqualTo(width);

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                        || document.body.scrollWidth > document.body.clientWidth + 1
                    """);
                hasHorizontalOverflow.ShouldBeFalse("Corrected-context invalidation review must not overflow at phone or tablet width.");

                string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
                bodyText.ShouldContain("Corrected context invalidated");
                bodyText.ShouldContain("Contexte corrigé invalidé");
                bodyText.ShouldNotContain("raw prompt", Case.Insensitive);
                bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
                bodyText.ShouldNotContain("tenant-beta", Case.Insensitive);
                AssertMetadataOnlyBody(bodyText);
            }
        }
    }

    [Fact]
    public async Task AiActionPreviewAndInspectionShouldRemainReachableMetadataOnlyAndOrdered()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertAiActionPreviewInspectionCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.AiActionPreviewInspection));

            ILocator approvalPreview = harness.Page.GetByRole(AriaRole.Region, new() { NameString = "AI action preview for item approval:approval-preview:request:50" });
            ILocator outcomePreview = harness.Page.GetByRole(AriaRole.Region, new() { NameString = "AI action preview for item ai:proposal-preview:outcome-recorded:51" });
            await AssertAiActionPreviewSectionsAsync(approvalPreview);
            await AssertAiActionPreviewSectionsAsync(outcomePreview);

            ILocator blockedFileSection = approvalPreview.Locator("[data-chatbot-ai-action-preview-section='file-access']");
            (await blockedFileSection.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            await blockedFileSection.FocusAsync();
            (await blockedFileSection.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator blockedGeneratedSection = approvalPreview.Locator("[data-chatbot-ai-action-preview-section='generated-changes']");
            (await blockedGeneratedSection.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            await blockedGeneratedSection.FocusAsync();
            (await blockedGeneratedSection.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            string previewText = await approvalPreview.InnerTextAsync();
            AssertTextOrder(
                previewText,
                "AI action preview",
                "Preview renders metadata only; sensitive generation inputs, provider internals, file content, and hidden evidence never render on this surface.",
                "Outbound communication",
                "Preview state",
                "allowed",
                "Reason code",
                "available",
                "Redaction state",
                "metadata_only",
                "Evidence freshness",
                "fresh, stale, expired",
                "Recipients or destination",
                "project:conversation, reviewer:approver-001",
                "Expected post-state",
                "metadata_only",
                "Safe next action",
                "review-ai-action",
                "File access and context",
                "blocked",
                "not-authorized",
                "Affected resources",
                "evidence:file:requirements, evidence:file:design, redacted",
                "Command execution",
                "Project.AppendConversationMessage",
                "Command allowlist version",
                "ai-action-allowlist.m0",
                "Policy snapshot",
                "metadata_only",
                "Audit status",
                "reconciling",
                "AI-generated changes",
                "not-yet-produced",
                "Generated-content visibility",
                "not-yet-produced");

            ILocator history = harness.Page.GetByLabel("Review history for item ai:proposal-preview:outcome-recorded:51");
            await WaitForVisibleAsync(history);
            (await history.GetAttributeAsync("aria-live")).ShouldBe("off");
            (await history.Locator(".chatbot-conversation-review-history__entry").CountAsync()).ShouldBe(5);
            AssertTextOrder(
                await history.InnerTextAsync(),
                "Review history",
                "proposal",
                "2026-06-01 08:20:00Z",
                "approval-requested",
                "approval-preview",
                "approval-decided",
                "approved",
                "execution-started",
                "operation-preview-001",
                "outcome-recorded",
                "01HZXCORRELATIONPREVIEW00000051",
                "policy-snapshot-preview",
                "superseded-by-none");

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();
            LocatorBoundingBoxResult? previewBox = await approvalPreview.BoundingBoxAsync();
            previewBox.ShouldNotBeNull();
            previewBox.Width.ShouldBeLessThanOrEqualTo(390);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
            bodyText.ShouldNotContain("restricted-quarterly-plan.xlsx", Case.Insensitive);
            bodyText.ShouldNotContain("/tenants/tenant-beta/files", Case.Insensitive);
            bodyText.ShouldNotContain("tenant-beta", Case.Insensitive);
            bodyText.ShouldNotContain("secret", Case.Insensitive);
        }
    }

    [Fact]
    public async Task ProjectConversationWhyProjectPanelShouldOpenFromEmailAndDecisionRowsAndRemainMetadataOnly()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertWhyProjectPanelCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator openButtons = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Available evidence: Why this project" });
            await openButtons.First.ClickAsync();

            ILocator panel = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Why this project evidence for association 01HZXASSOC000000000000001" });
            await AssertWhyPanelMetadataAsync(panel);
            await WaitForVisibleAsync(panel.GetByText("Some evidence detail is redacted or unavailable for this user.", new() { Exact = false }));

            ILocator redactedEvidence = panel.Locator("[data-chatbot-evidence-visibility='redacted']").First;
            await redactedEvidence.FocusAsync();
            (await redactedEvidence.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator correctionLink = panel.GetByRole(AriaRole.Button, new() { NameString = "Open superseding correction correction-002" });
            await correctionLink.ClickAsync();
            ILocator correctionPanel = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Why this project evidence for association 01HZXASSOC000000000000002" });
            await WaitForVisibleAsync(correctionPanel);
            string correctionText = await correctionPanel.InnerTextAsync();
            AssertTextOrder(
                correctionText,
                "Why this project",
                "Operation",
                "01HZXASSOC000000000000002",
                "Signal class",
                "correction",
                "Matched value",
                "association:correction-metadata",
                "Threshold band",
                "Auto",
                "Decision actor",
                "user-002",
                "Corrected context",
                "Propagation completed; impact corrected-context-ready; next action none.");

            ILocator close = correctionPanel.GetByRole(AriaRole.Button, new() { NameString = "Close why this project panel" });
            await close.FocusAsync();
            (await close.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();
            await close.ClickAsync();
            await correctionPanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });

            await openButtons.Nth(1).ClickAsync();
            await AssertWhyPanelMetadataAsync(panel);

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();
            string panelTransitionDuration = await panel.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            AssertReducedMotionTransitionDuration(panelTransitionDuration);
            LocatorBoundingBoxResult? panelBox = await panel.BoundingBoxAsync();
            panelBox.ShouldNotBeNull();
            panelBox.Width.ShouldBeLessThanOrEqualTo(390);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
            bodyText.ShouldNotContain("hidden-project-name", Case.Insensitive);
            bodyText.ShouldNotContain("hidden-participant-name", Case.Insensitive);
            bodyText.ShouldNotContain("hidden-file-name", Case.Insensitive);
        }
    }

    [Fact]
    public async Task ProjectConversationAttachmentItemsShouldExposeStateMetadataAndReachableUnavailableReasons()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAttachmentCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator pendingItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, invoice.pdf, Pending, Associated" });
            ILocator authorizedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, release-notes.pdf, Captured, Associated" });
            ILocator unavailableItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Attachment unavailable, Unavailable, Associated" });
            ILocator redactedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Redacted attachment, Pending, Associated" });
            ILocator retryItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, duplicate-invoice.pdf, Retryable, Associated" });
            ILocator unsafeItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Attachment unavailable, Unsafe, Associated" });

            await AssertAttachmentMetadataAsync(
                pendingItem,
                [
                    "Attachment name",
                    "Provider attachment ID",
                    "Content type",
                    "Size",
                    "Capture status",
                    "Storage status",
                    "Scan status",
                    "Duplicate state",
                    "Retry state",
                    "AI context eligibility",
                    "Mailbox",
                    "Conversation context",
                    "Thread",
                    "Operation",
                    "Lifecycle state",
                    "Redaction state",
                    "Safe next actions",
                    "Correlation ID",
                ],
                [
                    "invoice.pdf",
                    "Pending",
                    "Mailbox attachment",
                    "2026-06-01 08:02:30Z",
                    "Attachment name",
                    "invoice.pdf",
                    "Provider attachment ID",
                    "graph-attachment-001",
                    "Content type",
                    "application/pdf",
                    "Size",
                    "4,096.00",
                    "Capture status",
                    "Captured",
                    "Storage status",
                    "Pending",
                    "Scan status",
                    "Pending",
                    "Duplicate state",
                    "not-evaluated",
                    "Retry state",
                    "not-retryable",
                    "AI context eligibility",
                    "pending",
                    "Mailbox",
                    "controlled-mailbox-001",
                    "Conversation context",
                    "graph-conversation-001",
                    "Thread",
                    "graph-thread-001",
                    "Operation",
                    "01HZXASSOC000000000000001",
                    "Lifecycle state",
                    "Associated",
                    "Redaction state",
                    "Metadata only",
                    "Safe next actions",
                    "none",
                    "Correlation ID",
                    "01HZXCORRELATION00000000007",
                    "Why unavailable?",
                    "Attachment actions are unavailable until storage and scan state are governed.",
                ]);

            await AssertAttachmentMetadataAsync(
                authorizedItem,
                [
                    "Attachment name",
                    "Provider attachment ID",
                    "Content type",
                    "Size",
                    "Capture status",
                    "Storage status",
                    "Scan status",
                    "Duplicate state",
                    "Retry state",
                    "AI context eligibility",
                    "File reference",
                    "Folder reference",
                    "Mailbox",
                    "Conversation context",
                    "Thread",
                    "Operation",
                    "Lifecycle state",
                    "Redaction state",
                    "Safe next actions",
                    "Correlation ID",
                ],
                [
                    "release-notes.pdf",
                    "Captured",
                    "Mailbox attachment",
                    "2026-06-01 08:02:31Z",
                    "File reference",
                    "file-reference-001",
                    "Folder reference",
                    "folder-reference-001",
                    "Why unavailable?",
                    "Open governed file, Add to AI context",
                ]);

            await AssertAttachmentMetadataAsync(
                unavailableItem,
                expectedOrderedMarkers:
                [
                    "Attachment unavailable",
                    "Unavailable",
                    "Mailbox attachment",
                    "Why unavailable?",
                    "Attachment metadata is unavailable on this surface.",
                    "Attachment name",
                    "Attachment unavailable",
                    "Provider attachment ID",
                    "graph-attachment-003",
                    "Content type",
                    "unavailable",
                    "Size",
                    "unavailable",
                    "Scan status",
                    "Unavailable",
                    "Redaction state",
                    "Metadata only",
                ]);

            await AssertAttachmentMetadataAsync(
                redactedItem,
                expectedOrderedMarkers:
                [
                    "Redacted attachment",
                    "Pending",
                    "Mailbox attachment",
                    "Why unavailable?",
                    "Attachment metadata is redacted by policy.",
                    "Attachment name",
                    "Redacted attachment",
                    "Provider attachment ID",
                    "graph-attachment-004",
                    "Redaction state",
                    "Redacted",
                ]);

            await AssertAttachmentMetadataAsync(
                retryItem,
                expectedOrderedMarkers:
                [
                    "duplicate-invoice.pdf",
                    "Retryable",
                    "Mailbox attachment",
                    "Scan status",
                    "Retryable",
                    "Duplicate state",
                    "duplicate-provider-attachment-suppressed",
                    "Retry state",
                    "retryable-after-policy-window",
                    "Why unavailable?",
                    "Retry capture",
                ]);

            await AssertAttachmentMetadataAsync(
                unsafeItem,
                expectedOrderedMarkers:
                [
                    "Attachment unavailable",
                    "Unsafe",
                    "Mailbox attachment",
                    "Why unavailable?",
                    "Attachment metadata is unavailable on this surface.",
                    "Provider attachment ID",
                    "graph-attachment-006",
                    "Scan status",
                    "Unsafe",
                    "AI context eligibility",
                    "blocked-unsafe",
                ]);

            ILocator redactedReason = redactedItem.Locator(".chatbot-attachment-conversation-item__reason").First;
            (await redactedReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await redactedReason.FocusAsync();
            (await redactedReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            IReadOnlyList<string> unavailableEvidenceStates = await harness.Page
                .Locator(".chatbot-attachment-conversation-item .chatbot-chip--evidence[data-chatbot-evidence-state='Unavailable']")
                .AllTextContentsAsync();
            unavailableEvidenceStates.Count.ShouldBeGreaterThanOrEqualTo(2);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("restricted-quarterly-plan.xlsx", Case.Insensitive);
            bodyText.ShouldNotContain("unsafe-malware-sample.exe", Case.Insensitive);
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationStoredAttachmentReferencesShouldRemainMetadataOnlyAndInert()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertStoredAttachmentCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator storedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, release-notes.pdf, Captured, Associated" });
            await AssertAttachmentMetadataAsync(
                storedItem,
                expectedOrderedMarkers:
                [
                    "release-notes.pdf",
                    "Captured",
                    "Mailbox attachment",
                    "Storage status",
                    "Captured",
                    "Scan status",
                    "Captured",
                    "Duplicate state",
                    "unique",
                    "Retry state",
                    "not-retryable",
                    "AI context eligibility",
                    "eligible",
                    "File reference",
                    "file-reference-001",
                    "Folder reference",
                    "folder-reference-001",
                    "Safe next actions",
                    "none",
                ]);

            IReadOnlyList<string> referenceCodes = await storedItem.Locator("code").AllTextContentsAsync();
            referenceCodes.ShouldContain("file-reference-001");
            referenceCodes.ShouldContain("folder-reference-001");
            (await storedItem.Locator("a, button, input, select, textarea, [role='button'], [download], [href]").CountAsync()).ShouldBe(0);

            string markup = await storedItem.InnerHTMLAsync();
            markup.ShouldNotContain("href=", Case.Insensitive);
            markup.ShouldNotContain("download", Case.Insensitive);
            markup.ShouldNotContain("/api/v1/folders", Case.Insensitive);
            markup.ShouldNotContain("/api/v1/files", Case.Insensitive);
            markup.ShouldNotContain("folderId=", Case.Insensitive);
            markup.ShouldNotContain("fileId=", Case.Insensitive);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationDegradedAttachmentStorageShouldNotExposeFolderOrFileReferences()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertStoredAttachmentCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator unavailableItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Attachment unavailable, Unavailable, Associated" });
            ILocator retryableItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, duplicate-invoice.pdf, Retryable, Associated" });
            ILocator unsafeItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, Attachment unavailable, Unsafe, Associated" });

            await WaitForVisibleAsync(unavailableItem);
            await WaitForVisibleAsync(retryableItem);
            await WaitForVisibleAsync(unsafeItem);

            foreach (ILocator item in new[] { unavailableItem, retryableItem, unsafeItem })
            {
                string text = await item.InnerTextAsync();
                text.ShouldNotContain("File reference", Case.Insensitive);
                text.ShouldNotContain("Folder reference", Case.Insensitive);
                text.ShouldNotContain("folder-reference-", Case.Insensitive);
                text.ShouldNotContain("file-reference-", Case.Insensitive);
                (await item.Locator("a, button, input, select, textarea, [role='button'], [download], [href]").CountAsync()).ShouldBe(0);
                AssertMetadataOnlyBody(text);
            }
        }
    }

    [Fact]
    public async Task ProjectConversationParticipantItemsShouldExposeOrderedMetadataAndReachableUnavailableReasons()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertParticipantCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator internalItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Internal participant: Internal contributor, Associated" });
            ILocator unresolvedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Unresolved participant: Unresolved participant, Associated" });
            ILocator restrictedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Restricted participant: Restricted participant, Associated" });

            await AssertParticipantMetadataAsync(
                internalItem,
                [
                    "Participant type",
                    "Participant status",
                    "Participant resolution",
                    "Source participant",
                    "Party ID",
                    "Evidence reference",
                    "Evidence fingerprint",
                    "Mailbox",
                    "Lifecycle state",
                    "Safe next actions",
                    "Correlation ID",
                ],
                [
                    "mailbox:intake:sender",
                    "Resolved",
                    "Internal contributor",
                    "2026-06-01 08:03:00Z",
                    "Participant type",
                    "Internal participant",
                    "Participant status",
                    "Resolved",
                    "Participant resolution",
                    "01HZXRESOLUTION00000000001",
                    "Source participant",
                    "01HZXPARTICIPANT000000001",
                    "Party ID",
                    "tenant-alpha:parties:party-001",
                    "Evidence reference",
                    "mailbox:intake:sender",
                    "Evidence fingerprint",
                    "evidence-sha256-internal",
                    "Mailbox",
                    "controlled-mailbox-001",
                    "Lifecycle state",
                    "Associated",
                    "Safe next actions",
                    "none",
                    "Correlation ID",
                    "01HZXCORRELATION00000000003",
                ]);

            await AssertParticipantMetadataAsync(
                unresolvedItem,
                [
                    "Participant type",
                    "Participant status",
                    "Participant resolution",
                    "Source participant",
                    "Blocked reason",
                    "Evidence reference",
                    "Evidence fingerprint",
                    "Allowed review actions",
                    "Mailbox",
                    "Lifecycle state",
                    "Safe next actions",
                    "Correlation ID",
                ],
                [
                    "mailbox:intake:recipient:1",
                    "Unresolved",
                    "Unresolved participant",
                    "2026-06-01 08:05:00Z",
                    "Why unavailable?",
                    "Participant detail is unavailable: Participant not found",
                    "Participant type",
                    "Unresolved participant",
                    "Participant status",
                    "Unresolved",
                    "Participant resolution",
                    "01HZXRESOLUTION00000000001",
                    "Source participant",
                    "01HZXPARTICIPANT000000003",
                    "Blocked reason",
                    "Participant not found",
                    "Evidence reference",
                    "mailbox:intake:recipient:1",
                    "Evidence fingerprint",
                    "evidence-sha256-unresolved",
                    "Allowed review actions",
                    "Link participant, Create pending participant",
                    "Mailbox",
                    "controlled-mailbox-001",
                    "Lifecycle state",
                    "Associated",
                    "Safe next actions",
                    "none",
                    "Correlation ID",
                    "01HZXCORRELATION00000000005",
                ]);

            ILocator unavailableButton = unresolvedItem.GetByRole(AriaRole.Button, new() { NameString = "Why unavailable?" });
            (await unavailableButton.CountAsync()).ShouldBe(1);
            await unavailableButton.FocusAsync();
            (await unavailableButton.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator unavailableReason = unresolvedItem.Locator(".chatbot-participant-conversation-item__reason");
            (await unavailableReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await unavailableReason.FocusAsync();
            (await unavailableReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            IReadOnlyList<string> restrictedLabels = await restrictedItem.Locator("dt").AllTextContentsAsync();
            restrictedLabels.Select(static label => label.Trim()).ShouldNotContain("Party ID");
            await WaitForVisibleAsync(restrictedItem.GetByText("Participant detail is unavailable: Restricted party"));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationAiOutcomeItemsShouldExposeGovernedMetadataAndKeepGeneratedContentSeparate()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAiOutcomeCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator proposalItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI proposal, Proposed, 2026-06-01 08:20:00Z" });
            ILocator denialItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI denial, Denied, 2026-06-01 08:20:10Z" });
            ILocator refusalItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI refusal, Blocked, 2026-06-01 08:20:20Z" });
            ILocator failedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z" });
            ILocator invalidatedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, Corrected context invalidated, Invalidated, 2026-06-01 08:21:10Z" });

            await AssertAiOutcomeMetadataAsync(
                proposalItem,
                expectedLabels:
                [
                    "AI outcome",
                    "Status",
                    "Actor type",
                    "Proposal id",
                    "Risk class",
                    "Risk action classes",
                    "Policy reason",
                    "Classifier version",
                    "Risk input tuple",
                    "Requester authority",
                    "Policy snapshot id",
                    "Policy visibility",
                    "Command name",
                    "Command allowlist version",
                    "Safe next action",
                    "Correlation ID",
                ],
                expectedOrderedMarkers:
                [
                    "AI-generated",
                    "Tool-invoking",
                    "approval-required",
                    "Proposed",
                    "AI actor",
                    "2026-06-01 08:20:00Z",
                    "AI outcome",
                    "AI proposal",
                    "proposal",
                    "Status",
                    "Proposed",
                    "proposed",
                    "Actor type",
                    "AI actor",
                    "ai",
                    "Proposal id",
                    "proposal-001",
                    "Risk class",
                    "approval-required",
                    "Risk action classes",
                    "invokes-tools",
                    "Policy reason",
                    "policy_requires_approval",
                    "Classifier version",
                    "ai-action-risk-classifier.m0.v1",
                    "Risk input tuple",
                    "command=Project.AppendConversationMessage;effect=project-state;authority=project-contributor;policy=approval-required",
                    "Requester authority",
                    "project-contributor",
                    "Policy snapshot id",
                    "policy-snapshot-4-3",
                    "Policy visibility",
                    "metadata_only",
                    "Command name",
                    "Project.AppendConversationMessage",
                    "Command allowlist version",
                    "ai-action-allowlist.m0",
                    "Safe next action",
                    "review-ai-action",
                    "Correlation ID",
                    "01HZXCORRELATION00000000030",
                    "AI-generated",
                    "AI-generated content is labelled and kept distinct from source evidence.",
                    "Source evidence",
                    "Source evidence references are governed metadata, separate from AI-generated content.",
                    "AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.",
                ]);
            await WaitForVisibleAsync(proposalItem.GetByRole(AriaRole.Status, new() { NameString = "Risk: Tool-invoking. Policy reason: approval-required." }));
            await AssertAiOutcomeMetadataAsync(
                denialItem,
                expectedOrderedMarkers:
                [
                    "AI-generated",
                    "Tool-invoking",
                    "Denied",
                    "AI actor",
                    "2026-06-01 08:20:10Z",
                    "AI outcome",
                    "AI denial",
                    "denial",
                    "Status",
                    "Denied",
                    "denied",
                    "Safe next action",
                    "review-ai-action",
                    "Correlation ID",
                    "01HZXCORRELATION00000000031",
                ]);
            await AssertAiOutcomeMetadataAsync(
                refusalItem,
                expectedOrderedMarkers:
                [
                    "AI-generated",
                    "Tool-invoking",
                    "Blocked",
                    "AI actor",
                    "2026-06-01 08:20:20Z",
                    "AI outcome",
                    "AI refusal",
                    "refusal",
                    "Status",
                    "Blocked",
                    "blocked",
                    "Safe next action",
                    "review-ai-action",
                    "Correlation ID",
                    "01HZXCORRELATION00000000032",
                ]);
            await AssertAiOutcomeMetadataAsync(
                failedItem,
                expectedOrderedMarkers:
                [
                    "AI-generated",
                    "Tool-invoking",
                    "Failed",
                    "AI actor",
                    "2026-06-01 08:20:50Z",
                    "AI outcome",
                    "AI execution failed",
                    "execution-failed",
                    "Status",
                    "Failed",
                    "failed",
                    "Safe next action",
                    "review-ai-action",
                    "Correlation ID",
                    "01HZXCORRELATION00000000035",
                ]);
            await AssertAiOutcomeMetadataAsync(
                invalidatedItem,
                expectedOrderedMarkers:
                [
                    "AI-generated",
                    "Tool-invoking",
                    "Invalidated",
                    "AI actor",
                    "2026-06-01 08:21:10Z",
                    "AI outcome",
                    "Corrected context invalidated",
                    "corrected-context-invalidated",
                    "Status",
                    "Invalidated",
                    "invalidated",
                    "Safe next action",
                    "review-ai-action",
                    "Correlation ID",
                    "01HZXCORRELATION00000000037",
                ]);

            IReadOnlyList<string> aiContentKinds = await harness.Page
                .Locator(".chatbot-ai-outcome-conversation-item [data-chatbot-ai-content]")
                .EvaluateAllAsync<string[]>("sections => sections.map(section => section.getAttribute('data-chatbot-ai-content'))");
            aiContentKinds.Count(static kind => string.Equals(kind, "ai-generated", StringComparison.Ordinal)).ShouldBe(8);
            aiContentKinds.Count(static kind => string.Equals(kind, "source-evidence", StringComparison.Ordinal)).ShouldBe(8);

            ILocator generatedReason = proposalItem.Locator(".chatbot-ai-outcome-conversation-item__generated .chatbot-ai-outcome-conversation-item__reason");
            (await generatedReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await generatedReason.FocusAsync();
            (await generatedReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator sourceEvidenceReason = proposalItem.Locator(".chatbot-ai-outcome-conversation-item__source-evidence .chatbot-ai-outcome-conversation-item__reason");
            (await sourceEvidenceReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await sourceEvidenceReason.FocusAsync();
            (await sourceEvidenceReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("anonymous chat", Case.Insensitive);
            bodyText.ShouldNotContain("model-authored source evidence", Case.Insensitive);
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationLowRiskAiExecutionRowsShouldRenderPolicyContextAndProviderFailureMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertLowRiskAiExecutionCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.LowRiskAiExecution));

            ILocator startedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution started, Executing, 2026-06-01 08:20:30Z" });
            ILocator succeededItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:20:40Z" });
            ILocator routedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, Approval-linked AI action, Pending approval, 2026-06-01 08:20:45Z" });
            ILocator failedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z" });

            await AssertAiOutcomeMetadataAsync(
                startedItem,
                expectedOrderedMarkers:
                [
                    "Low-risk",
                    "Executing",
                    "Context package id",
                    "context-package-001",
                    "Policy reason",
                    "low-risk-execute-allowed",
                    "Execution status",
                    "executing",
                    "Safe next action",
                    "none",
                ]);
            await AssertAiOutcomeMetadataAsync(
                succeededItem,
                expectedOrderedMarkers:
                [
                    "Low-risk",
                    "Succeeded",
                    "Context package id",
                    "context-package-001",
                    "Context package version",
                    "v1",
                    "Authorized context references",
                    "evidence-message-001, evidence-attachment-001",
                    "Excluded context reasons",
                    "redacted, policy-denied",
                    "Execution outcome code",
                    "low-risk-assistance-generated",
                    "Safe next action",
                    "none",
                    "AI summary provenance",
                    "Generated by deterministic-test+test-model-v1 at 2026-06-01 08:20:40Z from evidence-message-001, evidence-attachment-001",
                ]);
            await AssertAiOutcomeMetadataAsync(
                routedItem,
                expectedOrderedMarkers:
                [
                    "Pending approval",
                    "Policy reason",
                    "low_risk_policy_false",
                    "Context package id",
                    "context-package-001",
                    "Safe next action",
                    "review-ai-action",
                ]);
            await AssertAiOutcomeMetadataAsync(
                failedItem,
                expectedOrderedMarkers:
                [
                    "Failed",
                    "Policy reason",
                    "low-risk-execute-allowed",
                    "Execution outcome code",
                    "ai_provider_disabled",
                    "Failure code",
                    "ai_provider_disabled",
                    "Retryability",
                    "retryable",
                    "Safe next action",
                    "review-ai-action",
                ]);

            ILocator sourceEvidence = succeededItem.Locator("[data-chatbot-ai-content='source-evidence']");
            ILocator aiSummary = succeededItem.Locator("details[data-chatbot-ai-content='ai-summary']");
            await WaitForVisibleAsync(sourceEvidence);
            await WaitForVisibleAsync(aiSummary);
            await succeededItem.Locator("details[data-chatbot-ai-content='ai-summary'] > summary").ClickAsync();
            await WaitForVisibleAsync(succeededItem.GetByText("AI-generated summary state", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationApprovedAiActionExecutionRowsShouldRenderAllowlistedLifecycleAndFailureMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertApprovedAiActionExecutionCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.ApprovedAiActionExecution));

            ILocator startedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution started, Executing, 2026-06-01 08:22:00Z" });
            ILocator succeededItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:22:10Z" });
            ILocator failedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution failed, Failed, 2026-06-01 08:22:20Z" });
            ILocator recordedItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:22:30Z" });

            await AssertAiOutcomeMetadataAsync(
                startedItem,
                expectedOrderedMarkers:
                [
                    "AI execution started",
                    "execution-started",
                    "Proposal id",
                    "proposal-approved-001",
                    "Operation id",
                    "operation-approved-001",
                    "Command name",
                    "Project.AppendConversationMessage",
                    "Command allowlist version",
                    "ai-action-command-allowlist.m0",
                    "Approval id",
                    "approval-approved-001",
                    "Execution status",
                    "execution-started",
                    "Audit status",
                    "committed",
                    "Safe next action",
                    "wait-for-command-outcome",
                    "Correlation ID",
                    "01HZXCORRELATIONAPPROVED000001",
                ]);
            await AssertAiOutcomeMetadataAsync(
                succeededItem,
                expectedOrderedMarkers:
                [
                    "AI execution succeeded",
                    "execution-succeeded",
                    "Proposal id",
                    "proposal-approved-001",
                    "Operation id",
                    "operation-approved-001",
                    "Command name",
                    "Project.AppendConversationMessage",
                    "Command allowlist version",
                    "ai-action-command-allowlist.m0",
                    "Approval id",
                    "approval-approved-001",
                    "Execution status",
                    "success",
                    "Execution outcome code",
                    "approved-ai-action-executed",
                    "Audit operation",
                    "audit:approved-execution-001",
                    "Audit status",
                    "committed",
                    "Safe next action",
                    "none",
                    "AI-generated content visibility",
                    "metadata_only",
                ]);
            await AssertAiOutcomeMetadataAsync(
                failedItem,
                expectedOrderedMarkers:
                [
                    "AI execution failed",
                    "execution-failed",
                    "Proposal id",
                    "proposal-approved-002",
                    "Operation id",
                    "operation-approved-002",
                    "Command name",
                    "Project.AppendConversationMessage",
                    "Command allowlist version",
                    "ai-action-command-allowlist.m0",
                    "Approval id",
                    "approval-approved-002",
                    "Execution status",
                    "failed",
                    "Failure code",
                    "dependency_unavailable",
                    "Retryability",
                    "retryable",
                    "Safe next action",
                    "retry-later",
                ]);
            await AssertAiOutcomeMetadataAsync(
                recordedItem,
                expectedOrderedMarkers:
                [
                    "AI outcome recorded",
                    "outcome-recorded",
                    "Proposal id",
                    "proposal-approved-001",
                    "Operation id",
                    "operation-approved-001",
                    "Command name",
                    "Project.AppendConversationMessage",
                    "Command allowlist version",
                    "ai-action-command-allowlist.m0",
                    "Approval id",
                    "approval-approved-001",
                    "Execution outcome code",
                    "outcome-recorded",
                    "Safe next action",
                    "none",
                ]);

            ILocator failureSummary = failedItem.GetByLabel("Status summary for item ai:approved-execution-002:execution-failed:83");
            await WaitForVisibleAsync(failureSummary);
            AssertTextOrder(
                await failureSummary.InnerTextAsync(),
                "Failure",
                "Degraded",
                "dependency_unavailable",
                "Retry",
                "Degraded",
                "Retry count",
                "1",
                "Duplicate safety",
                "duplicate-safe",
                "Next action",
                "Degraded",
                "Retry later",
                "retry-later");

            await succeededItem.Locator("details[data-chatbot-ai-content='ai-summary'] > summary").ClickAsync();
            await WaitForVisibleAsync(succeededItem.GetByText("AI-generated content visibility", new() { Exact = true }));
            await WaitForVisibleAsync(succeededItem.GetByText("metadata_only", new() { Exact = true }).First);
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

            string headerDirection = await failedItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string transitionDuration = await failedItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            headerDirection.ShouldBe("column");
            AssertReducedMotionTransitionDuration(transitionDuration);
            LocatorBoundingBoxResult? failureBox = await failedItem.BoundingBoxAsync();
            failureBox.ShouldNotBeNull();
            failureBox.Width.ShouldBeLessThanOrEqualTo(390);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("Project.SendEmail", Case.Insensitive);
            bodyText.ShouldNotContain("raw command payload", Case.Insensitive);
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationRefusalSafeBlocksShouldRenderCatalogBackedMetadataOnlyReasonsAcrossSurfaces()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertRefusalSafeBlockCoverageWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.RefusalSafeBlock));

            ILocator gatewayBlock = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: Request refused. Next action: Request access." });
            await WaitForVisibleAsync(gatewayBlock);
            (await gatewayBlock.GetAttributeAsync("aria-live")).ShouldBe("assertive");
            (await gatewayBlock.GetAttributeAsync("data-chatbot-feedback-state")).ShouldBe("BlockedAction");
            (await gatewayBlock.GetAttributeAsync("data-chatbot-catalog-code")).ShouldBe("refusal_blocked_action");
            (await gatewayBlock.GetAttributeAsync("data-chatbot-refusal-reason")).ShouldBe("tenant-policy-exceeded");
            await gatewayBlock.FocusAsync();
            (await gatewayBlock.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            ILocator approvalBlock = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval outcome, Blocked, 2026-06-01 09:00:10Z" });
            await AssertApprovalMetadataAsync(
                approvalBlock,
                expectedOrderedMarkers:
                [
                    "Blocked",
                    "Approval event",
                    "2026-06-01 09:00:10Z",
                    "Approval event kind",
                    "Approval outcome",
                    "outcome",
                    "Approval status",
                    "blocked",
                    "Catalog code",
                    "refusal_blocked_action",
                    "Disabled reason",
                    "Evidence expired",
                    "evidence-expired",
                    "Failure reason",
                    "evidence-expired",
                    "Safe next actions",
                    "request-files",
                    "Audit status",
                    "committed",
                ]);

            ILocator approveAction = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Approve action" });
            (await approveAction.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await approveAction.GetAttributeAsync("aria-describedby")).ShouldBe("approval-blocked-reason");
            await approveAction.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<int>("() => window.__approvalSubmitCount")).ShouldBe(0);
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Required evidence is expired; request refreshed files before approval."));

            ILocator operationBlock = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Unsupported command, Blocked, 2026-06-01 09:00:20Z" });
            await AssertDecisionMetadataAsync(
                operationBlock,
                expectedOrderedMarkers:
                [
                    "Failure state kind",
                    "Blocked",
                    "Failure status",
                    "Blocked",
                    "Catalog code",
                    "refusal_blocked_action",
                    "Blocked reason",
                    "Unsupported action",
                    "unsupported-action",
                    "Failure reason",
                    "command-not-allowlisted",
                    "Idempotency admission",
                    "not-admitted",
                    "Dispatcher call",
                    "not-called",
                    "Provider call",
                    "not-called",
                    "Safe next actions",
                    "correct-request",
                    "Surface origin",
                    "mcp",
                ],
                expectedAccessibleNamePrefix: "System status,");

            ILocator aiRefusal = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI refusal, Blocked, 2026-06-01 09:00:30Z" });
            await AssertAiOutcomeMetadataAsync(
                aiRefusal,
                expectedOrderedMarkers:
                [
                    "AI-generated",
                    "Tool-invoking",
                    "Blocked",
                    "AI outcome",
                    "AI refusal",
                    "refusal",
                    "Status",
                    "Blocked",
                    "blocked",
                    "Proposal id",
                    "proposal-refusal-001",
                    "Policy reason",
                    "missing-required-context",
                    "Context package",
                    "unavailable",
                    "Policy snapshot",
                    "unavailable",
                    "Audit denial fact",
                    "recorded",
                    "Safe next action",
                    "request-files",
                    "Correlation ID",
                    "01HZXCORRELATIONREFUSAL000003",
                ]);
            ILocator aiReason = aiRefusal.Locator(".chatbot-ai-outcome-conversation-item__reason").First;
            (await aiReason.GetAttributeAsync("tabindex")).ShouldBe("0");
            await aiReason.FocusAsync();
            (await aiReason.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            IReadOnlyList<string> reasons = await harness.Page
                .Locator("[data-chatbot-refusal-reason]")
                .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('data-chatbot-refusal-reason') || '')");
            reasons.ShouldBe(
                [
                    "tenant-policy-exceeded",
                    "project-authorization-denied",
                    "sender-authority-denied",
                    "approved-command-scope-exceeded",
                    "command-not-allowlisted",
                    "unsupported-action",
                    "unresolved-association",
                    "unresolved-participant",
                    "missing-required-context",
                    "context-package-unavailable",
                    "evidence-expired",
                    "policy-snapshot-unavailable",
                    "approval-state-invalid",
                    "corrected-context-invalidated",
                    "dependency-degraded",
                ],
                ignoreOrder: false);

            int longestHeadline = await harness.Page
                .Locator("[data-chatbot-catalog-headline]")
                .EvaluateAllAsync<int>("headlines => Math.max(...headlines.map(node => (node.textContent || '').trim().length))");
            longestHeadline.ShouldBeLessThanOrEqualTo(80);

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

            LocatorBoundingBoxResult? operationBox = await operationBlock.BoundingBoxAsync();
            operationBox.ShouldNotBeNull();
            operationBox.Width.ShouldBeLessThanOrEqualTo(390);

            bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                """
                () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                    || document.body.scrollWidth > document.body.clientWidth + 1
                """);
            hasHorizontalOverflow.ShouldBeFalse("Refusal safe-block surfaces should not overflow at phone width.");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("Project.SendExternalEmail", Case.Insensitive);
            bodyText.ShouldNotContain("tenant-beta", Case.Insensitive);
            bodyText.ShouldNotContain("restricted-policy-text", Case.Insensitive);
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationPopulatedStreamShouldRespectMotionForcedColorsAndPhoneLayout()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertPopulatedAccessibilityModesWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator mailboxItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox item: Mailbox intake, Associated" });
            await WaitForVisibleAsync(mailboxItem);
            ILocator decisionItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision, Needs review, NeedsReview, 2026-06-01 08:05:00Z" });
            await WaitForVisibleAsync(decisionItem);
            ILocator attachmentItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, invoice.pdf, Pending, Associated" });
            await WaitForVisibleAsync(attachmentItem);
            ILocator participantItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Restricted participant: Restricted participant, Associated" });
            await WaitForVisibleAsync(participantItem);
            ILocator approvalItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Approval event, Approval requested, Pending, 2026-06-01 08:09:00Z" });
            await WaitForVisibleAsync(approvalItem);
            ILocator failureItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System status, Dependency degraded, Degraded, 2026-06-01 08:17:30Z" });
            await WaitForVisibleAsync(failureItem);
            ILocator aiOutcomeItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z" });
            await WaitForVisibleAsync(aiOutcomeItem);

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

            string animationName = await mailboxItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string transitionDuration = await mailboxItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string headerDirection = await mailboxItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string decisionAnimationName = await decisionItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string decisionTransitionDuration = await decisionItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string decisionHeaderDirection = await decisionItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string decisionReasonTransitionDuration = await decisionItem
                .Locator(".chatbot-decision-conversation-item__reason")
                .EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string participantAnimationName = await participantItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string participantTransitionDuration = await participantItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string participantHeaderDirection = await participantItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string attachmentAnimationName = await attachmentItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string attachmentTransitionDuration = await attachmentItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string attachmentHeaderDirection = await attachmentItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string approvalAnimationName = await approvalItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string approvalTransitionDuration = await approvalItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string approvalHeaderDirection = await approvalItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string approvalReasonTransitionDuration = await approvalItem
                .Locator(".chatbot-approval-conversation-item__reason")
                .EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string participantReasonTransitionDuration = await participantItem
                .Locator(".chatbot-participant-conversation-item__reason")
                .EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string failureAnimationName = await failureItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string failureTransitionDuration = await failureItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string failureHeaderDirection = await failureItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string failureReasonTransitionDuration = await failureItem
                .Locator(".chatbot-failure-conversation-item__reason")
                .First
                .EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string aiOutcomeAnimationName = await aiOutcomeItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string aiOutcomeTransitionDuration = await aiOutcomeItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string aiOutcomeHeaderDirection = await aiOutcomeItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            string aiOutcomeReasonTransitionDuration = await aiOutcomeItem
                .Locator(".chatbot-ai-outcome-conversation-item__reason")
                .First
                .EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            animationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(transitionDuration);
            headerDirection.ShouldBe("column");
            decisionAnimationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(decisionTransitionDuration);
            AssertReducedMotionTransitionDuration(decisionReasonTransitionDuration);
            decisionHeaderDirection.ShouldBe("column");
            participantAnimationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(participantTransitionDuration);
            AssertReducedMotionTransitionDuration(participantReasonTransitionDuration);
            participantHeaderDirection.ShouldBe("column");
            attachmentAnimationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(attachmentTransitionDuration);
            attachmentHeaderDirection.ShouldBe("column");
            approvalAnimationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(approvalTransitionDuration);
            AssertReducedMotionTransitionDuration(approvalReasonTransitionDuration);
            approvalHeaderDirection.ShouldBe("column");
            failureAnimationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(failureTransitionDuration);
            AssertReducedMotionTransitionDuration(failureReasonTransitionDuration);
            failureHeaderDirection.ShouldBe("column");
            aiOutcomeAnimationName.ShouldBe("none");
            AssertReducedMotionTransitionDuration(aiOutcomeTransitionDuration);
            AssertReducedMotionTransitionDuration(aiOutcomeReasonTransitionDuration);
            aiOutcomeHeaderDirection.ShouldBe("column");

            LocatorBoundingBoxResult? box = await mailboxItem.BoundingBoxAsync();
            box.ShouldNotBeNull();
            box.Width.ShouldBeLessThanOrEqualTo(390);
            LocatorBoundingBoxResult? decisionBox = await decisionItem.BoundingBoxAsync();
            decisionBox.ShouldNotBeNull();
            decisionBox.Width.ShouldBeLessThanOrEqualTo(390);
            LocatorBoundingBoxResult? participantBox = await participantItem.BoundingBoxAsync();
            participantBox.ShouldNotBeNull();
            participantBox.Width.ShouldBeLessThanOrEqualTo(390);
            LocatorBoundingBoxResult? attachmentBox = await attachmentItem.BoundingBoxAsync();
            attachmentBox.ShouldNotBeNull();
            attachmentBox.Width.ShouldBeLessThanOrEqualTo(390);
            LocatorBoundingBoxResult? approvalBox = await approvalItem.BoundingBoxAsync();
            approvalBox.ShouldNotBeNull();
            approvalBox.Width.ShouldBeLessThanOrEqualTo(390);
            LocatorBoundingBoxResult? failureBox = await failureItem.BoundingBoxAsync();
            failureBox.ShouldNotBeNull();
            failureBox.Width.ShouldBeLessThanOrEqualTo(390);
            LocatorBoundingBoxResult? aiOutcomeBox = await aiOutcomeItem.BoundingBoxAsync();
            aiOutcomeBox.ShouldNotBeNull();
            aiOutcomeBox.Width.ShouldBeLessThanOrEqualTo(390);
        }
    }

    [Fact]
    public async Task ProjectConversationEmptyStateShouldKeepSafeNextActionReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertEmptyWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Empty));

            ILocator emptyState = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: No email-derived context is available. Next action: Wait for associated email." });
            await WaitForVisibleAsync(emptyState);
            await WaitForVisibleAsync(harness.Page.GetByText("Wait for associated email.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Project conversation metadata" }));
        }
    }

    [Fact]
    public async Task ProjectConversationUnauthorizedStateShouldStayRedactedAndIndistinguishable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertUnauthorizedWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Unauthorized));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: Project conversation is unavailable. Next action: Verify access or choose an authorized project." }));
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence restricted", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("project-redacted", new() { Exact = true }));
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static async Task AssertAssociatedEmailMetadataAsync(ILocator mailboxItem)
    {
        (await mailboxItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await mailboxItem.FocusAsync();
        (await mailboxItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        IReadOnlyList<string> labels = await mailboxItem.Locator("dt").AllTextContentsAsync();
        labels.Select(static label => label.Trim()).ShouldBe(
            [
                "Source",
                "Mailbox",
                "Provider message ID",
                "Internet message ID",
                "Operation",
                "Conversation context",
                "Thread",
                "Project",
                "Lifecycle state",
                "Confidence",
                "Threshold band",
                "Safe next actions",
                "Received",
                "Sent",
                "Created",
                "Source timezone",
                "Correlation ID",
            ],
            ignoreOrder: false);

        string text = await mailboxItem.InnerTextAsync();
        AssertTextOrder(
            text,
            "Mailbox intake",
            "2026-06-01 08:00:00Z",
            "Source",
            "Microsoft 365 mailbox",
            "Mailbox",
            "controlled-mailbox-001",
            "Provider message ID",
            "graph-message-001",
            "Internet message ID",
            "<internet-message-001@example.test>",
            "Operation",
            "01HZXASSOC000000000000001",
            "Conversation context",
            "graph-conversation-001",
            "Thread",
            "graph-thread-001",
            "Project",
            "project-alpha",
            "Lifecycle state",
            "Associated",
            "Confidence",
            "91%",
            "Threshold band",
            "Auto",
            "Safe next actions",
            "none",
            "Received",
            "2026-06-01 08:00:00Z",
            "Sent",
            "2026-06-01 07:58:00Z",
            "Created",
            "2026-06-01 07:57:00Z",
            "Source timezone",
            "UTC",
            "Correlation ID",
            "01HZXCORRELATION00000000001",
            "m365-mailbox-intake",
            "metadata_only",
            "91%");
    }

    private static async Task AssertDecisionMetadataAsync(
        ILocator decisionItem,
        IReadOnlyList<string>? expectedLabels = null,
        IReadOnlyList<string>? expectedOrderedMarkers = null,
        string expectedAccessibleNamePrefix = "System decision,")
    {
        await WaitForVisibleAsync(decisionItem);
        (await decisionItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await decisionItem.FocusAsync();
        (await decisionItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        string? accessibleName = await decisionItem.GetAttributeAsync("aria-label");
        accessibleName.ShouldNotBeNullOrWhiteSpace();
        accessibleName.StartsWith(expectedAccessibleNamePrefix, StringComparison.Ordinal).ShouldBeTrue();

        if (expectedLabels is not null)
        {
            IReadOnlyList<string> labels = await decisionItem.Locator("dt").AllTextContentsAsync();
            labels.Select(static label => label.Trim()).ShouldBe(expectedLabels, ignoreOrder: false);
        }

        if (expectedOrderedMarkers is not null)
        {
            string text = await decisionItem.InnerTextAsync();
            AssertTextOrder(text, [.. expectedOrderedMarkers]);
        }
    }

    private static async Task AssertParticipantMetadataAsync(
        ILocator participantItem,
        IReadOnlyList<string> expectedLabels,
        IReadOnlyList<string> expectedOrderedMarkers)
    {
        await WaitForVisibleAsync(participantItem);
        (await participantItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await participantItem.FocusAsync();
        (await participantItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        IReadOnlyList<string> labels = await participantItem.Locator("dt").AllTextContentsAsync();
        labels.Select(static label => label.Trim()).ShouldBe(expectedLabels, ignoreOrder: false);

        string text = await participantItem.InnerTextAsync();
        AssertTextOrder(text, [.. expectedOrderedMarkers]);
    }

    private static async Task AssertAttachmentMetadataAsync(
        ILocator attachmentItem,
        IReadOnlyList<string>? expectedLabels = null,
        IReadOnlyList<string>? expectedOrderedMarkers = null)
    {
        await WaitForVisibleAsync(attachmentItem);
        (await attachmentItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await attachmentItem.FocusAsync();
        (await attachmentItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        if (expectedLabels is not null)
        {
            IReadOnlyList<string> labels = await attachmentItem.Locator("dt").AllTextContentsAsync();
            labels.Select(static label => label.Trim()).ShouldBe(expectedLabels, ignoreOrder: false);
        }

        if (expectedOrderedMarkers is not null)
        {
            string text = await attachmentItem.InnerTextAsync();
            AssertTextOrder(text, [.. expectedOrderedMarkers]);
        }
    }

    private static async Task AssertApprovalMetadataAsync(
        ILocator approvalItem,
        IReadOnlyList<string> expectedOrderedMarkers)
    {
        await WaitForVisibleAsync(approvalItem);
        (await approvalItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await approvalItem.FocusAsync();
        (await approvalItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        string? accessibleName = await approvalItem.GetAttributeAsync("aria-label");
        accessibleName.ShouldNotBeNullOrWhiteSpace();
        accessibleName.StartsWith("Approval event,", StringComparison.Ordinal).ShouldBeTrue();

        string text = await approvalItem.InnerTextAsync();
        AssertTextOrder(text, [.. expectedOrderedMarkers]);
    }

    private static async Task AssertAiActionPreviewSectionsAsync(ILocator preview)
    {
        await WaitForVisibleAsync(preview);
        (await preview.GetAttributeAsync("data-chatbot-ai-action-preview")).ShouldBe("metadata-only");

        IReadOnlyList<string> sectionKinds = await preview
            .Locator("[data-chatbot-ai-action-preview-section]")
            .EvaluateAllAsync<string[]>("sections => sections.map(section => section.getAttribute('data-chatbot-ai-action-preview-section') || '')");
        sectionKinds.ShouldBe(["outbound", "file-access", "command", "generated-changes"], ignoreOrder: false);

        IReadOnlyList<string> sectionLabels = await preview
            .Locator("[data-chatbot-ai-action-preview-section]")
            .EvaluateAllAsync<string[]>("sections => sections.map(section => section.getAttribute('aria-label') || '')");
        sectionLabels.ShouldBe(["Outbound communication", "File access and context", "Command execution", "AI-generated changes"], ignoreOrder: false);

        IReadOnlyList<string> tabIndexes = await preview
            .Locator("[data-chatbot-ai-action-preview-section]")
            .EvaluateAllAsync<string[]>("sections => sections.map(section => section.getAttribute('tabindex') || '')");
        tabIndexes.ShouldBe(["0", "0", "0", "0"], ignoreOrder: false);
    }

    private static async Task AssertAiOutcomeMetadataAsync(
        ILocator aiOutcomeItem,
        IReadOnlyList<string>? expectedLabels = null,
        IReadOnlyList<string>? expectedOrderedMarkers = null)
    {
        await WaitForVisibleAsync(aiOutcomeItem);
        (await aiOutcomeItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await aiOutcomeItem.FocusAsync();
        (await aiOutcomeItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        string? accessibleName = await aiOutcomeItem.GetAttributeAsync("aria-label");
        accessibleName.ShouldNotBeNullOrWhiteSpace();
        accessibleName.StartsWith("AI actor,", StringComparison.Ordinal).ShouldBeTrue();

        if (expectedLabels is not null)
        {
            IReadOnlyList<string> labels = await aiOutcomeItem.Locator(".chatbot-ai-outcome-conversation-item__metadata > dt").AllTextContentsAsync();
            labels.Select(static label => label.Trim()).ShouldBe(expectedLabels, ignoreOrder: false);
        }

        if (expectedOrderedMarkers is not null)
        {
            string text = await aiOutcomeItem.InnerTextAsync();
            AssertTextOrder(text, [.. expectedOrderedMarkers]);
        }
    }

    private static async Task AssertWhyPanelMetadataAsync(ILocator panel)
    {
        await WaitForVisibleAsync(panel);
        IReadOnlyList<string> labels = await panel.Locator(":scope > dl > dt").AllTextContentsAsync();
        labels.Select(static label => label.Trim()).ShouldBe(
            [
                "Operation",
                "Signal class",
                "Matched value",
                "Confidence",
                "Threshold band",
                "Policy snapshot",
                "Scorer/kernel version",
                "Decision actor",
                "Decision actor type",
                "Decided at",
                "Source provenance",
                "Source version",
                "Correlation ID",
                "Redaction state",
                "Schema version",
                "Safe next actions",
            ],
            ignoreOrder: false);

        string text = await panel.InnerTextAsync();
        AssertTextOrder(
            text,
            "Why this project",
            "Operation",
            "01HZXASSOC000000000000001",
            "Signal class",
            "explicit-project-identifier",
            "Matched value",
            "mailbox:metadata",
            "Confidence",
            "91%",
            "Threshold band",
            "Auto",
            "Policy snapshot",
            "association-thresholds.m0.default.v1",
            "Scorer/kernel version",
            "association-deterministic.kernel.m0.v1",
            "Decision actor",
            "actor-safe",
            "Decision actor type",
            "human",
            "Decided at",
            "2026-06-01 08:02:00Z",
            "Source provenance",
            "m365-mailbox-intake",
            "Source version",
            "3",
            "Correlation ID",
            "01HZXCORRELATION00000000002",
            "Authorized evidence",
            "explicit-project-identifier: mailbox:metadata",
            "Signal class",
            "explicit-project-identifier",
            "Matched value",
            "mailbox:metadata",
            "Evidence reference",
            "mailbox:project-id",
            "Evidence fingerprint",
            "evidence-sha256-project",
            "Confidence contribution",
            "0.42");
    }

    private static void AssertTextOrder(string text, params string[] expected)
    {
        int previous = -1;
        foreach (string marker in expected)
        {
            int current = text.IndexOf(marker, previous + 1, StringComparison.Ordinal);
            current.ShouldBeGreaterThan(previous, $"Expected '{marker}' to appear after the previous metadata marker.");
            previous = current;
        }
    }

    private static void AssertReducedMotionTransitionDuration(string transitionDuration)
    {
        foreach (string duration in transitionDuration.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            double seconds = ParseCssDurationSeconds(duration);
            seconds.ShouldBeLessThanOrEqualTo(0.000011d, $"Expected reduced-motion transition duration, got '{transitionDuration}'.");
        }
    }

    private static double ParseCssDurationSeconds(string duration)
    {
        if (duration.EndsWith("ms", StringComparison.Ordinal))
        {
            return double.Parse(duration[..^2], CultureInfo.InvariantCulture) / 1000d;
        }

        if (duration.EndsWith("s", StringComparison.Ordinal))
        {
            return double.Parse(duration[..^1], CultureInfo.InvariantCulture);
        }

        return double.PositiveInfinity;
    }

    private static string BuildProjectConversationFixture(ProjectConversationFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string body = scenario switch
        {
            ProjectConversationFixtureScenario.Loading => BuildLoadingBody(),
            ProjectConversationFixtureScenario.Populated => BuildPopulatedBody(),
            ProjectConversationFixtureScenario.Empty => BuildEmptyBody(),
            ProjectConversationFixtureScenario.Unauthorized => BuildUnauthorizedBody(),
            ProjectConversationFixtureScenario.Classification => BuildClassificationBody(),
            ProjectConversationFixtureScenario.TaskIntentReview => BuildTaskIntentReviewBody(),
            ProjectConversationFixtureScenario.ApprovalDecisionSurface => BuildApprovalDecisionSurfaceBody(),
            ProjectConversationFixtureScenario.CorrectedContextInvalidatedApproval => BuildCorrectedContextInvalidatedApprovalBody(),
            ProjectConversationFixtureScenario.AiActionPreviewInspection => BuildAiActionPreviewInspectionBody(),
            ProjectConversationFixtureScenario.LowRiskAiExecution => BuildLowRiskAiExecutionBody(),
            ProjectConversationFixtureScenario.ApprovedAiActionExecution => BuildApprovedAiActionExecutionBody(),
            ProjectConversationFixtureScenario.RefusalSafeBlock => BuildRefusalSafeBlockBody(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Project conversation</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main id="chatbot-main-content" class="chatbot-shell-main" tabindex="-1">
                  <section class="chatbot-conversation-shell"
                           aria-label="Project conversation"
                           data-chatbot-responsive-fixture="project-conversation">
                    <div class="chatbot-conversation-shell__context">
                      <header class="chatbot-project-context-header" aria-label="Project context">
                        <div class="chatbot-project-context-header__identity">
                          <span class="chatbot-metadata">S1</span>
                          <h2 class="chatbot-project-context-header__title">Authorized Project</h2>
                          <span class="chatbot-metadata"><code class="chatbot-code">project-alpha</code></span>
                        </div>
                        <div class="chatbot-project-context-header__meta" aria-label="Conversation context">
                          <span class="chatbot-metadata">Tenant</span>
                          <span>tenant-alpha</span>
                        </div>
                        <div class="chatbot-status"
                             data-chatbot-status="info"
                             role="status"
                             aria-live="off"
                             aria-label="Project conversation status: current">
                          <span class="chatbot-status__label">Info</span>
                          <span>Current</span>
                        </div>
                      </header>
                    </div>
                    <div class="chatbot-conversation-shell__body">
                      <section class="chatbot-conversation-shell__main" role="region" aria-label="Project conversation stream">
                        <section class="chatbot-page chatbot-project-conversation"
                                 aria-labelledby="project-conversation-title"
                                 data-chatbot-responsive-fixture="project-conversation">
                          <header class="chatbot-page-header">
                            <span class="chatbot-metadata">S1</span>
                            <h1 id="project-conversation-title" class="chatbot-page-title">Project conversation</h1>
                          </header>
                          {{body}}
                        </section>
                      </section>
                      <aside class="chatbot-conversation-shell__panel"
                             role="complementary"
                             aria-label="Project conversation metadata">
                        <section class="chatbot-section" aria-labelledby="project-conversation-metadata-title">
                          <h2 id="project-conversation-metadata-title" class="chatbot-section-title">Project conversation metadata</h2>
                          <dl class="chatbot-definition-list chatbot-labelled-row-list">
                            <dt class="chatbot-labelled-row">Project</dt>
                            <dd><code class="chatbot-code">project-alpha</code></dd>
                            <dt class="chatbot-labelled-row">Lifecycle state</dt>
                            <dd><code class="chatbot-code">Associated</code></dd>
                            <dt class="chatbot-labelled-row">Safe next actions</dt>
                            <dd><code class="chatbot-code">none</code></dd>
                            <dt class="chatbot-labelled-row">Source metadata</dt>
                            <dd><code class="chatbot-code">m365-mailbox-intake - chatbot.project-conversation-response.v1</code></dd>
                          </dl>
                        </section>
                      </aside>
                    </div>
                  </section>
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildLowRiskAiExecutionBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation status: current">
              <span class="chatbot-status__label">Info</span>
              <span>Current</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <ol class="chatbot-conversation-stream__list" aria-label="Project conversation stream">
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:ai-execution-001:execution-started:70" tabindex="0" aria-label="AI actor, AI execution started, Executing, 2026-06-01 08:20:30Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="info" data-chatbot-risk-class="LowRisk" role="status" aria-label="Risk: Low-risk. Policy reason: low-risk-execute-allowed."><span class="chatbot-chip__label">Low-risk</span><span class="chatbot-chip__status">low-risk-execute-allowed</span></span><span class="chatbot-ai-outcome-conversation-item__status">Executing</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:30.0000000Z">2026-06-01 08:20:30Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution started</span> <code class="chatbot-code">execution-started</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Executing</span> <code class="chatbot-code">executing</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">ai-execution-001</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">low-risk</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">low-risk-execute-allowed</code></dd><dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snap-001</code></dd><dt class="chatbot-labelled-row">Context package id</dt><dd><code class="chatbot-code">context-package-001</code></dd><dt class="chatbot-labelled-row">Context package version</dt><dd><code class="chatbot-code">v1</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">executing</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit:ai-execution-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000044</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:ai-execution-001:execution-started:70" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:ai-execution-001:execution-succeeded:71" tabindex="0" aria-label="AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:20:40Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="info" data-chatbot-risk-class="LowRisk" role="status" aria-label="Risk: Low-risk. Policy reason: low-risk-execute-allowed."><span class="chatbot-chip__label">Low-risk</span><span class="chatbot-chip__status">low-risk-execute-allowed</span></span><span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:40.0000000Z">2026-06-01 08:20:40Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution succeeded</span> <code class="chatbot-code">execution-succeeded</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Succeeded</span> <code class="chatbot-code">succeeded</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">ai-execution-001</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">low-risk</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">low-risk-execute-allowed</code></dd><dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snap-001</code></dd><dt class="chatbot-labelled-row">Context package id</dt><dd><code class="chatbot-code">context-package-001</code></dd><dt class="chatbot-labelled-row">Context package version</dt><dd><code class="chatbot-code">v1</code></dd><dt class="chatbot-labelled-row">Context redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Authorized context references</dt><dd><code class="chatbot-code">evidence-message-001, evidence-attachment-001</code></dd><dt class="chatbot-labelled-row">Excluded context reasons</dt><dd><code class="chatbot-code">redacted, policy-denied</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">success</code></dd><dt class="chatbot-labelled-row">Execution outcome code</dt><dd><code class="chatbot-code">low-risk-assistance-generated</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit:ai-execution-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000045</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p><ul class="chatbot-ai-outcome-conversation-item__evidence-list"><li><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence-message-001</span></li><li><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence-attachment-001</span></li></ul><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">Authorized context references</dt><dd><code class="chatbot-code">evidence-message-001, evidence-attachment-001</code></dd><dt class="chatbot-labelled-row">Excluded context reasons</dt><dd><code class="chatbot-code">redacted, policy-denied</code></dd></dl></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:ai-execution-001:execution-succeeded:71" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">AI-generated content visibility</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">AI-generated summary state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">AI summary provenance</dt><dd><code class="chatbot-code">Generated by deterministic-test+test-model-v1 at 2026-06-01 08:20:40Z from evidence-message-001, evidence-attachment-001</code></dd></dl></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:approval-linked:72" tabindex="0" aria-label="AI actor, Approval-linked AI action, Pending approval, 2026-06-01 08:20:45Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="warning" data-chatbot-risk-class="LowRisk" role="status" aria-label="Risk: Low-risk. Policy reason: low_risk_policy_false."><span class="chatbot-chip__label">Low-risk</span><span class="chatbot-chip__status">low_risk_policy_false</span></span><span class="chatbot-ai-outcome-conversation-item__status">Pending approval</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:45.0000000Z">2026-06-01 08:20:45Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>Approval-linked AI action</span> <code class="chatbot-code">approval-linked</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Pending approval</span> <code class="chatbot-code">pending-approval</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">low-risk</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">low_risk_policy_false</code></dd><dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snap-denied</code></dd><dt class="chatbot-labelled-row">Context package id</dt><dd><code class="chatbot-code">context-package-001</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><code class="chatbot-code">pending</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000046</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:proposal-001:approval-linked:72" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:ai-execution-002:execution-failed:73" tabindex="0" aria-label="AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="danger" data-chatbot-risk-class="LowRisk" role="status" aria-label="Risk: Low-risk. Policy reason: low-risk-execute-allowed."><span class="chatbot-chip__label">Low-risk</span><span class="chatbot-chip__status">low-risk-execute-allowed</span></span><span class="chatbot-ai-outcome-conversation-item__status">Failed</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:50.0000000Z">2026-06-01 08:20:50Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution failed</span> <code class="chatbot-code">execution-failed</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Failed</span> <code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">ai-execution-002</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">low-risk</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">low-risk-execute-allowed</code></dd><dt class="chatbot-labelled-row">Context package id</dt><dd><code class="chatbot-code">context-package-001</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Execution outcome code</dt><dd><code class="chatbot-code">ai_provider_disabled</code></dd><dt class="chatbot-labelled-row">Failure code</dt><dd><code class="chatbot-code">ai_provider_disabled</code></dd><dt class="chatbot-labelled-row">Retryability</dt><dd><code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000047</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:ai-execution-002:execution-failed:73" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
              </ol>
            </section>
            """;

    private static string BuildApprovedAiActionExecutionBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation status: current">
              <span class="chatbot-status__label">Info</span>
              <span>Current</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <ol class="chatbot-conversation-stream__list" aria-label="Project conversation stream">
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:approved-execution-001:execution-started:80" tabindex="0" aria-label="AI actor, AI execution started, Executing, 2026-06-01 08:22:00Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="warning" data-chatbot-risk-class="ToolInvoking" role="status" aria-label="Risk: Tool-invoking. Policy reason: approved-ai-action."><span class="chatbot-chip__label">Tool-invoking</span><span class="chatbot-chip__status">approved-ai-action</span></span><span class="chatbot-ai-outcome-conversation-item__status">Executing</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:22:00.0000000Z">2026-06-01 08:22:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution started</span> <code class="chatbot-code">execution-started</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Executing</span> <code class="chatbot-code">executing</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-approved-001</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-approved-001</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">approval-required</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">approved-ai-action</code></dd><dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snapshot-4-7</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd><dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-command-allowlist.m0</code></dd><dt class="chatbot-labelled-row">Approval id</dt><dd><code class="chatbot-code">approval-approved-001</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><code class="chatbot-code">approved</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">execution-started</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit:approved-execution-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">wait-for-command-outcome</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONAPPROVED000001</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:approved-execution-001:execution-started:80" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:approved-execution-001:execution-succeeded:81" tabindex="0" aria-label="AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:22:10Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="success" data-chatbot-risk-class="ToolInvoking" role="status" aria-label="Risk: Tool-invoking. Policy reason: approved-ai-action."><span class="chatbot-chip__label">Tool-invoking</span><span class="chatbot-chip__status">approved-ai-action</span></span><span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:22:10.0000000Z">2026-06-01 08:22:10Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution succeeded</span> <code class="chatbot-code">execution-succeeded</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Succeeded</span> <code class="chatbot-code">succeeded</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-approved-001</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-approved-001</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">approval-required</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">approved-ai-action</code></dd><dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snapshot-4-7</code></dd><dt class="chatbot-labelled-row">Context redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd><dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-command-allowlist.m0</code></dd><dt class="chatbot-labelled-row">Approval id</dt><dd><code class="chatbot-code">approval-approved-001</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">success</code></dd><dt class="chatbot-labelled-row">Execution outcome code</dt><dd><code class="chatbot-code">approved-ai-action-executed</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit:approved-execution-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONAPPROVED000002</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p><ul class="chatbot-ai-outcome-conversation-item__evidence-list"><li><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">approval:approval-approved-001</span></li><li><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">proposal:proposal-approved-001</span></li></ul><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">Authorized context references</dt><dd><code class="chatbot-code">approval:approval-approved-001, proposal:proposal-approved-001</code></dd></dl></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:approved-execution-001:execution-succeeded:81" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">AI-generated content visibility</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">AI-generated summary state</dt><dd><code class="chatbot-code">metadata_only</code></dd></dl></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:approved-execution-002:execution-failed:83" tabindex="0" aria-label="AI actor, AI execution failed, Failed, 2026-06-01 08:22:20Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="danger" data-chatbot-risk-class="ToolInvoking" role="status" aria-label="Risk: Tool-invoking. Policy reason: approved-ai-action."><span class="chatbot-chip__label">Tool-invoking</span><span class="chatbot-chip__status">approved-ai-action</span></span><span class="chatbot-ai-outcome-conversation-item__status">Failed</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:22:20.0000000Z">2026-06-01 08:22:20Z</time></header>
                    <section class="chatbot-conversation-status-summary" aria-label="Status summary for item ai:approved-execution-002:execution-failed:83" aria-live="off"><h3 class="chatbot-conversation-status-summary__title">Status and next action</h3><ul class="chatbot-conversation-status-summary__list"><li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="failure" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Failure</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">dependency_unavailable</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Retry later</span> <code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-approved-002</code></dd></dl></li><li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="retry" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Retry</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Retry count</dt><dd><code class="chatbot-code">1</code></dd><dt class="chatbot-labelled-row">Duplicate safety</dt><dd><code class="chatbot-code">duplicate-safe</code></dd></dl></li><li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="next-action" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Next action</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Retry later</span> <code class="chatbot-code">retry-later</code></dd></dl></li></ul></section>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution failed</span> <code class="chatbot-code">execution-failed</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Failed</span> <code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-approved-002</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-approved-002</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">approval-required</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">approved-ai-action</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd><dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-command-allowlist.m0</code></dd><dt class="chatbot-labelled-row">Approval id</dt><dd><code class="chatbot-code">approval-approved-002</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Execution outcome code</dt><dd><code class="chatbot-code">dependency_unavailable</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Failure code</dt><dd><code class="chatbot-code">dependency_unavailable</code></dd><dt class="chatbot-labelled-row">Retryability</dt><dd><code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONAPPROVED000003</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:approved-execution-002:execution-failed:83" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:approved-execution-001:outcome-recorded:84" tabindex="0" aria-label="AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:22:30Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="success" data-chatbot-risk-class="ToolInvoking" role="status" aria-label="Risk: Tool-invoking. Policy reason: approved-ai-action."><span class="chatbot-chip__label">Tool-invoking</span><span class="chatbot-chip__status">approved-ai-action</span></span><span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:22:30.0000000Z">2026-06-01 08:22:30Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI outcome recorded</span> <code class="chatbot-code">outcome-recorded</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Succeeded</span> <code class="chatbot-code">succeeded</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-approved-001</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-approved-001</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd><dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-command-allowlist.m0</code></dd><dt class="chatbot-labelled-row">Approval id</dt><dd><code class="chatbot-code">approval-approved-001</code></dd><dt class="chatbot-labelled-row">Execution status</dt><dd><code class="chatbot-code">success</code></dd><dt class="chatbot-labelled-row">Execution outcome code</dt><dd><code class="chatbot-code">outcome-recorded</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONAPPROVED000004</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:approved-execution-001:outcome-recorded:84" data-chatbot-ai-content="ai-summary"><summary>AI summary</summary><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">AI-generated content visibility</dt><dd><code class="chatbot-code">metadata_only</code></dd></dl></details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
              </ol>
            </section>
            """;

    private static string BuildRefusalSafeBlockBody()
        => """
            <script>window.__approvalSubmitCount = 0;</script>
            <div class="chatbot-status"
                 data-chatbot-status="danger"
                 data-chatbot-feedback-state="BlockedAction"
                 role="alert"
                 aria-live="assertive"
                 aria-label="Blocked: Request refused. Next action: Request access."
                 tabindex="0"
                 data-chatbot-catalog-code="refusal_blocked_action"
                 data-chatbot-refusal-reason="tenant-policy-exceeded">
              <span class="chatbot-status__label">Blocked</span>
              <span data-chatbot-catalog-headline="true">Request refused</span>
              <span>The request exceeds tenant policy.</span>
              <span>Request access.</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <section class="chatbot-section" aria-label="Refusal taxonomy coverage">
                <h3 class="chatbot-section-title">Refusal taxonomy coverage</h3>
                <ol class="chatbot-conversation-stream__list">
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="project-authorization-denied">project-authorization-denied</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="sender-authority-denied">sender-authority-denied</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="approved-command-scope-exceeded">approved-command-scope-exceeded</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="command-not-allowlisted">command-not-allowlisted</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="unsupported-action">unsupported-action</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="unresolved-association">unresolved-association</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="unresolved-participant">unresolved-participant</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="missing-required-context">missing-required-context</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="context-package-unavailable">context-package-unavailable</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="evidence-expired">evidence-expired</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="policy-snapshot-unavailable">policy-snapshot-unavailable</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="approval-state-invalid">approval-state-invalid</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="corrected-context-invalidated">corrected-context-invalidated</code></li>
                  <li><code class="chatbot-code" data-chatbot-refusal-reason="dependency-degraded">dependency-degraded</code></li>
                </ol>
              </section>
              <ol class="chatbot-conversation-stream__list" aria-label="Project conversation stream">
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item"
                           data-chatbot-conversation-item-kind="Approval"
                           data-chatbot-conversation-item-id="approval:refusal-001:outcome:90"
                           tabindex="0"
                           aria-label="Approval event, Approval outcome, Blocked, 2026-06-01 09:00:10Z">
                    <header class="chatbot-approval-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Approval outcome</span>
                      <span class="chatbot-chip chatbot-chip--risk">Evidence expired</span>
                      <span class="chatbot-approval-conversation-item__status">Blocked</span>
                      <span class="chatbot-actor-badge" aria-label="Approval actor: Approval event">Approval event</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T09:00:10.0000000Z">2026-06-01 09:00:10Z</time>
                    </header>
                    <button type="button"
                            aria-disabled="true"
                            aria-describedby="approval-blocked-reason"
                            tabindex="0"
                            aria-label="Approve action">
                      Approve action
                    </button>
                    <p id="approval-blocked-reason"
                       class="chatbot-approval-conversation-item__reason"
                       tabindex="0"
                       aria-label="Why unavailable? Required evidence is expired; request refreshed files before approval.">
                      <strong>Why unavailable?</strong> Required evidence is expired; request refreshed files before approval.
                    </p>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval outcome</span> <code class="chatbot-code">outcome</code></dd>
                      <dt class="chatbot-labelled-row">Approval status</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd>
                      <dt class="chatbot-labelled-row">Catalog code</dt><dd><span data-chatbot-catalog-headline="true">Refused action</span> <code class="chatbot-code">refusal_blocked_action</code></dd>
                      <dt class="chatbot-labelled-row">Disabled reason</dt><dd><span>Evidence expired</span> <code class="chatbot-code">evidence-expired</code></dd>
                      <dt class="chatbot-labelled-row">Failure reason</dt><dd><code class="chatbot-code">evidence-expired</code></dd>
                      <dt class="chatbot-labelled-row">Evidence freshness</dt><dd><code class="chatbot-code">expired</code></dd>
                      <dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">request-files</code></dd>
                      <dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">ui</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONREFUSAL000001</code></dd>
                    </dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item"
                           data-chatbot-conversation-item-kind="FailureState"
                           data-chatbot-conversation-item-id="failure:refusal-unsupported-command:91"
                           tabindex="0"
                           aria-label="System status, Unsupported command, Blocked, 2026-06-01 09:00:20Z">
                    <header class="chatbot-failure-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Unsupported command</span>
                      <span class="chatbot-chip chatbot-chip--risk">Unsupported action</span>
                      <span class="chatbot-failure-conversation-item__status">Blocked</span>
                      <span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T09:00:20.0000000Z">2026-06-01 09:00:20Z</time>
                    </header>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0">
                      <strong>Request blocked</strong> This command is outside the approved M0 allowlist.
                    </p>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd>
                      <dt class="chatbot-labelled-row">Failure status</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd>
                      <dt class="chatbot-labelled-row">Catalog code</dt><dd><span data-chatbot-catalog-headline="true">Refused action</span> <code class="chatbot-code">refusal_blocked_action</code></dd>
                      <dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Unsupported action</span> <code class="chatbot-code">unsupported-action</code></dd>
                      <dt class="chatbot-labelled-row">Failure reason</dt><dd><code class="chatbot-code">command-not-allowlisted</code></dd>
                      <dt class="chatbot-labelled-row">Idempotency admission</dt><dd><code class="chatbot-code">not-admitted</code></dd>
                      <dt class="chatbot-labelled-row">Dispatcher call</dt><dd><code class="chatbot-code">not-called</code></dd>
                      <dt class="chatbot-labelled-row">Provider call</dt><dd><code class="chatbot-code">not-called</code></dd>
                      <dt class="chatbot-labelled-row">Audit denial fact</dt><dd><code class="chatbot-code">recorded</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">correct-request</code></dd>
                      <dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">mcp</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONREFUSAL000002</code></dd>
                    </dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item"
                           data-chatbot-conversation-item-kind="AiOutcome"
                           data-chatbot-conversation-item-id="ai:proposal-refusal-001:refusal:92"
                           tabindex="0"
                           aria-label="AI actor, AI refusal, Blocked, 2026-06-01 09:00:30Z">
                    <header class="chatbot-ai-outcome-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span>
                      <span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span>
                      <span class="chatbot-ai-outcome-conversation-item__status">Blocked</span>
                      <span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T09:00:30.0000000Z">2026-06-01 09:00:30Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI refusal</span> <code class="chatbot-code">refusal</code></dd>
                      <dt class="chatbot-labelled-row">Status</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd>
                      <dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd>
                      <dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-refusal-001</code></dd>
                      <dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">missing-required-context</code></dd>
                      <dt class="chatbot-labelled-row">Context package</dt><dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Audit denial fact</dt><dd><code class="chatbot-code">recorded</code></dd>
                      <dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">request-files</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONREFUSAL000003</code></dd>
                    </dl>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence">
                      <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0">
                        <strong>Source evidence</strong> Required source evidence is missing or expired; no AI provider call was made.
                      </p>
                    </section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:proposal-refusal-001:refusal:92" data-chatbot-ai-content="ai-summary">
                      <summary>AI summary</summary>
                      <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0">
                        <strong>AI summary</strong> No generated content is available for a refused action.
                      </p>
                    </details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0">
                      <strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong>
                    </p>
                  </article>
                </li>
              </ol>
            </section>
            """;

    private static string BuildLoadingBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 data-chatbot-feedback-state="LoadingColdLoad"
                 role="status"
                 aria-live="polite"
                 aria-label="Project conversation loading">
              <span class="chatbot-status__label">Info</span>
              <span>Loading project conversation.</span>
            </div>
            """;

    private static string BuildPopulatedBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 data-chatbot-feedback-state="CurrentUserAiProposalReady"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation stream status: current">
              <span class="chatbot-status__label">Info</span>
              <span>Current</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <ol class="chatbot-conversation-stream__list" aria-label="Project conversation stream">
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-email-conversation-item"
                           data-chatbot-conversation-item-kind="EmailDerived"
                           data-chatbot-conversation-item-id="01HZXMAILBOX000000000000001"
                           tabindex="0"
                           aria-label="Mailbox item: Mailbox intake, Associated">
                    <header class="chatbot-email-conversation-item__header">
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox intake">Mailbox intake</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:00:00.0000000Z">2026-06-01 08:00:00Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-email-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Source</dt>
                      <dd><code class="chatbot-code">Microsoft 365 mailbox</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Provider message ID</dt>
                      <dd><code class="chatbot-code">graph-message-001</code></dd>
                      <dt class="chatbot-labelled-row">Internet message ID</dt>
                      <dd><code class="chatbot-code">&lt;internet-message-001@example.test&gt;</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Project</dt>
                      <dd><code class="chatbot-code">project-alpha</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Confidence</dt>
                      <dd><code class="chatbot-code">91%</code></dd>
                      <dt class="chatbot-labelled-row">Threshold band</dt>
                      <dd><code class="chatbot-code">Auto</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Received</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T08:00:00.0000000Z">2026-06-01 08:00:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Sent</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T07:58:00.0000000Z">2026-06-01 07:58:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Created</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T07:57:00.0000000Z">2026-06-01 07:57:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Source timezone</dt>
                      <dd><code class="chatbot-code">UTC</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000001</code></dd>
                    </dl>
                    <div class="chatbot-email-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">m365-mailbox-intake</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">metadata_only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span>
                      <button class="chatbot-chip chatbot-chip--evidence"
                              type="button"
                              data-chatbot-evidence-state="Available"
                              aria-label="Available evidence: Why this project"
                              onclick="document.getElementById('why-project-panel').hidden=false;document.getElementById('why-project-panel').focus();">
                        <span class="chatbot-chip__label">Why this project</span>
                        <span class="chatbot-chip__status">Available evidence</span>
                      </button>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item"
                           data-chatbot-conversation-item-kind="SystemDecision"
                           data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:3"
                           tabindex="0"
                           aria-label="System decision, Confirmed association, Associated, 2026-06-01 08:02:00Z">
                    <header class="chatbot-decision-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:subject</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span>
                      <button class="chatbot-chip chatbot-chip--evidence"
                              type="button"
                              data-chatbot-evidence-state="Available"
                              aria-label="Available evidence: Why this project"
                              onclick="document.getElementById('why-project-panel').hidden=false;document.getElementById('why-project-panel').focus();">
                        <span class="chatbot-chip__label">Why this project</span>
                        <span class="chatbot-chip__status">Available evidence</span>
                      </button>
                      <span class="chatbot-decision-conversation-item__status">Associated</span>
                      <span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:00.0000000Z">2026-06-01 08:02:00Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Decision kind</dt>
                      <dd><span>Confirmed association</span> <code class="chatbot-code">associate</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Decision actor</dt>
                      <dd><code class="chatbot-code">user-001</code></dd>
                      <dt class="chatbot-labelled-row">Decision actor type</dt>
                      <dd><code class="chatbot-code">human</code></dd>
                      <dt class="chatbot-labelled-row">Decided at</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T08:02:00.0000000Z">2026-06-01 08:02:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Confidence</dt>
                      <dd><code class="chatbot-code">91%</code></dd>
                      <dt class="chatbot-labelled-row">Threshold band</dt>
                      <dd><code class="chatbot-code">Auto</code></dd>
                      <dt class="chatbot-labelled-row">Surface origin</dt>
                      <dd><code class="chatbot-code">ui</code></dd>
                      <dt class="chatbot-labelled-row">Policy snapshot</dt>
                      <dd><code class="chatbot-code">association-thresholds.m0.default.v1</code></dd>
                      <dt class="chatbot-labelled-row">Evidence references</dt>
                      <dd><code class="chatbot-code">mailbox:intake:subject</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd><code class="chatbot-code">metadata_only</code></dd>
                      <dt class="chatbot-labelled-row">Decision note state</dt>
                      <dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd>
                      <dt class="chatbot-labelled-row">Retention class</dt>
                      <dd><code class="chatbot-code">collaboration_input</code></dd>
                      <dt class="chatbot-labelled-row">Schema version</dt>
                      <dd><code class="chatbot-code">chatbot.project-conversation-item.v1</code></dd>
                      <dt class="chatbot-labelled-row">Source version</dt>
                      <dd><code class="chatbot-code">3</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000002</code></dd>
                    </dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item" data-chatbot-conversation-item-kind="SystemDecision" data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:4" tabindex="0" aria-label="System decision, Rejected association, Rejected, 2026-06-01 08:03:00Z">
                    <header class="chatbot-decision-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:sender</span><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">21%</span><span class="chatbot-decision-conversation-item__status">Rejected</span><span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span><time class="chatbot-metadata" datetime="2026-06-01T08:03:00.0000000Z">2026-06-01 08:03:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata"><dt class="chatbot-labelled-row">Decision kind</dt><dd><span>Rejected association</span> <code class="chatbot-code">reject</code></dd><dt class="chatbot-labelled-row">Lifecycle state</dt><dd><code class="chatbot-code">Rejected</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">system-policy</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">system</code></dd><dt class="chatbot-labelled-row">Decided at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:03:00.0000000Z">2026-06-01 08:03:00Z</time></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">21%</code></dd><dt class="chatbot-labelled-row">Threshold band</dt><dd><code class="chatbot-code">Manual</code></dd><dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">policy</code></dd><dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">association-thresholds.m0.default.v1</code></dd><dt class="chatbot-labelled-row">Evidence references</dt><dd><code class="chatbot-code">mailbox:intake:sender</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Decision note state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Retention class</dt><dd><code class="chatbot-code">collaboration_input</code></dd><dt class="chatbot-labelled-row">Schema version</dt><dd><code class="chatbot-code">chatbot.project-conversation-item.v1</code></dd><dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">4</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000003</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item" data-chatbot-conversation-item-kind="SystemDecision" data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:5" tabindex="0" aria-label="System decision, Deferred association, Deferred, 2026-06-01 08:04:00Z">
                    <header class="chatbot-decision-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:thread</span><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">55%</span><span class="chatbot-decision-conversation-item__status">Deferred</span><span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span><time class="chatbot-metadata" datetime="2026-06-01T08:04:00.0000000Z">2026-06-01 08:04:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata"><dt class="chatbot-labelled-row">Decision kind</dt><dd><span>Deferred association</span> <code class="chatbot-code">defer</code></dd><dt class="chatbot-labelled-row">Lifecycle state</dt><dd><code class="chatbot-code">Deferred</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">user-002</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">55%</code></dd><dt class="chatbot-labelled-row">Threshold band</dt><dd><code class="chatbot-code">Review</code></dd><dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">ui</code></dd><dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">association-thresholds.m0.default.v1</code></dd><dt class="chatbot-labelled-row">Evidence references</dt><dd><code class="chatbot-code">mailbox:intake:thread</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">review-later</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Decision note state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">5</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000004</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item" data-chatbot-conversation-item-kind="SystemDecision" data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:6" tabindex="0" aria-label="System decision, Needs review, NeedsReview, 2026-06-01 08:05:00Z">
                    <header class="chatbot-decision-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:ambiguity</span><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">48%</span><span class="chatbot-decision-conversation-item__status">NeedsReview</span><span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span><time class="chatbot-metadata" datetime="2026-06-01T08:05:00.0000000Z">2026-06-01 08:05:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata"><dt class="chatbot-labelled-row">Decision kind</dt><dd><span>Needs review</span> <code class="chatbot-code">needs-review</code></dd><dt class="chatbot-labelled-row">Lifecycle state</dt><dd><code class="chatbot-code">NeedsReview</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">system-policy</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">system</code></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">48%</code></dd><dt class="chatbot-labelled-row">Threshold band</dt><dd><code class="chatbot-code">Review</code></dd><dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">api</code></dd><dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">association-thresholds.m0.default.v1</code></dd><dt class="chatbot-labelled-row">Evidence references</dt><dd><code class="chatbot-code">mailbox:intake:ambiguity</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">open-review</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Decision note state</dt><dd><span>Unavailable</span> <code class="chatbot-code">unavailable</code></dd><dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">6</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000005</code></dd></dl>
                    <p class="chatbot-decision-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Decision detail is unavailable on this surface.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item" data-chatbot-conversation-item-kind="SystemDecision" data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:7" tabindex="0" aria-label="System decision, Project reassignment, Correction-delayed, 2026-06-01 08:06:00Z">
                    <header class="chatbot-decision-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:subject</span><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span><span class="chatbot-decision-conversation-item__status">Correction-delayed</span><span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span><time class="chatbot-metadata" datetime="2026-06-01T08:06:00.0000000Z">2026-06-01 08:06:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata"><dt class="chatbot-labelled-row">Correction kind</dt><dd><span>Project reassignment</span> <code class="chatbot-code">project-reassignment</code></dd><dt class="chatbot-labelled-row">Lifecycle state</dt><dd><code class="chatbot-code">Correction-delayed</code></dd><dt class="chatbot-labelled-row">Correction actor</dt><dd><code class="chatbot-code">user-002</code></dd><dt class="chatbot-labelled-row">Correction actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Corrected at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:06:00.0000000Z">2026-06-01 08:06:00Z</time></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">91%</code></dd><dt class="chatbot-labelled-row">Threshold band</dt><dd><code class="chatbot-code">Auto</code></dd><dt class="chatbot-labelled-row">Prior project</dt><dd><code class="chatbot-code">project-redacted</code></dd><dt class="chatbot-labelled-row">Corrected project</dt><dd><code class="chatbot-code">project-alpha</code></dd><dt class="chatbot-labelled-row">Predecessor association</dt><dd><code class="chatbot-code">01HZXASSOC000000000000000</code></dd><dt class="chatbot-labelled-row">Supersedes association</dt><dd><code class="chatbot-code">01HZXASSOC000000000000000</code></dd><dt class="chatbot-labelled-row">Superseded by association</dt><dd><code class="chatbot-code">01HZXASSOC000000000000002</code></dd><dt class="chatbot-labelled-row">Downstream impact status</dt><dd><code class="chatbot-code">delayed</code></dd><dt class="chatbot-labelled-row">Correction ID</dt><dd><code class="chatbot-code">correction-001</code></dd><dt class="chatbot-labelled-row">Workflow instance</dt><dd><code class="chatbot-code">workflow-001</code></dd><dt class="chatbot-labelled-row">Required stores</dt><dd><code class="chatbot-code">project-conversation, participants</code></dd><dt class="chatbot-labelled-row">Failed stores</dt><dd><code class="chatbot-code">participants</code></dd><dt class="chatbot-labelled-row">Propagation progress</dt><dd><code class="chatbot-code">1 of 2</code></dd><dt class="chatbot-labelled-row">Propagation started</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:06:00.0000000Z">2026-06-01 08:06:00Z</time></dd><dt class="chatbot-labelled-row">Propagation estimated completion</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:10:00.0000000Z">2026-06-01 08:10:00Z</time></dd><dt class="chatbot-labelled-row">Propagation status</dt><dd><code class="chatbot-code">delayed</code></dd><dt class="chatbot-labelled-row">Corrected context stale</dt><dd><code class="chatbot-code">True</code></dd><dt class="chatbot-labelled-row">Responsible owner role</dt><dd><code class="chatbot-code">operations</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">wait-for-propagation</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Correction rationale state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Retention class</dt><dd><code class="chatbot-code">collaboration_input</code></dd><dt class="chatbot-labelled-row">Schema version</dt><dd><code class="chatbot-code">chatbot.project-conversation-item.v1</code></dd><dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">7</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000006</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item" data-chatbot-conversation-item-kind="SystemDecision" data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:8" tabindex="0" aria-label="System decision, Project reassignment, Correcting, 2026-06-01 08:07:00Z">
                    <header class="chatbot-decision-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:subject</span><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span><span class="chatbot-decision-conversation-item__status">Correcting</span><span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span><time class="chatbot-metadata" datetime="2026-06-01T08:07:00.0000000Z">2026-06-01 08:07:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata"><dt class="chatbot-labelled-row">Correction kind</dt><dd><span>Project reassignment</span> <code class="chatbot-code">project-reassignment</code></dd><dt class="chatbot-labelled-row">Lifecycle state</dt><dd><code class="chatbot-code">Correcting</code></dd><dt class="chatbot-labelled-row">Correction actor</dt><dd><code class="chatbot-code">user-002</code></dd><dt class="chatbot-labelled-row">Correction actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Downstream impact status</dt><dd><code class="chatbot-code">correcting</code></dd><dt class="chatbot-labelled-row">Required stores</dt><dd><code class="chatbot-code">project-conversation, participants</code></dd><dt class="chatbot-labelled-row">Completed stores</dt><dd><code class="chatbot-code">project-conversation</code></dd><dt class="chatbot-labelled-row">Propagation progress</dt><dd><code class="chatbot-code">1 of 2</code></dd><dt class="chatbot-labelled-row">Propagation status</dt><dd><code class="chatbot-code">correcting</code></dd><dt class="chatbot-labelled-row">Corrected context stale</dt><dd><code class="chatbot-code">True</code></dd><dt class="chatbot-labelled-row">Responsible owner role</dt><dd><code class="chatbot-code">operations</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">wait-for-propagation</code></dd><dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">8</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000007</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-decision-conversation-item" data-chatbot-conversation-item-kind="SystemDecision" data-chatbot-conversation-item-id="decision:01HZXASSOC000000000000001:9" tabindex="0" aria-label="System decision, Project reassignment, Corrected, 2026-06-01 08:08:00Z">
                    <header class="chatbot-decision-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:subject</span><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span><span class="chatbot-decision-conversation-item__status">Corrected</span><span class="chatbot-actor-badge" aria-label="System decision actor: System decision">System decision</span><time class="chatbot-metadata" datetime="2026-06-01T08:08:00.0000000Z">2026-06-01 08:08:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-decision-conversation-item__metadata"><dt class="chatbot-labelled-row">Correction kind</dt><dd><span>Project reassignment</span> <code class="chatbot-code">project-reassignment</code></dd><dt class="chatbot-labelled-row">Lifecycle state</dt><dd><code class="chatbot-code">Corrected</code></dd><dt class="chatbot-labelled-row">Correction actor</dt><dd><code class="chatbot-code">user-002</code></dd><dt class="chatbot-labelled-row">Correction actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Downstream impact status</dt><dd><code class="chatbot-code">complete</code></dd><dt class="chatbot-labelled-row">Completed stores</dt><dd><code class="chatbot-code">project-conversation, participants</code></dd><dt class="chatbot-labelled-row">Propagation progress</dt><dd><code class="chatbot-code">2 of 2</code></dd><dt class="chatbot-labelled-row">Propagation completed</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:08:30.0000000Z">2026-06-01 08:08:30Z</time></dd><dt class="chatbot-labelled-row">Propagation status</dt><dd><code class="chatbot-code">completed</code></dd><dt class="chatbot-labelled-row">Corrected context stale</dt><dd><code class="chatbot-code">False</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Correction rationale state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">9</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000008</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-001:request:10" tabindex="0" aria-label="Approval event, Approval requested, Pending, 2026-06-01 08:09:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">evidence:summary:001</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Pending</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:09:00.0000000Z">2026-06-01 08:09:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval requested</span> <code class="chatbot-code">request</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Pending</span> <code class="chatbot-code">pending</code></dd><dt class="chatbot-labelled-row">Approval ID</dt><dd><code class="chatbot-code">approval-001</code></dd><dt class="chatbot-labelled-row">Proposal ID</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Source conversation item</dt><dd><code class="chatbot-code">decision:01HZXASSOC000000000000001:9</code></dd><dt class="chatbot-labelled-row">Requester</dt><dd><code class="chatbot-code">requester-001</code></dd><dt class="chatbot-labelled-row">Requested at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:09:00.0000000Z">2026-06-01 08:09:00Z</time></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">SendExternalReply</code></dd><dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">allowlist.v1</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">high</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">externally-visible</code></dd><dt class="chatbot-labelled-row">Policy visibility</dt><dd><code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Evidence references</dt><dd><code class="chatbot-code">evidence:summary:001</code></dd><dt class="chatbot-labelled-row">Evidence freshness</dt><dd><span>Expired</span> <code class="chatbot-code">expired</code></dd><dt class="chatbot-labelled-row">Affected resources</dt><dd><code class="chatbot-code">project:project-alpha</code></dd><dt class="chatbot-labelled-row">Recipients</dt><dd><code class="chatbot-code">recipient:external:001</code></dd><dt class="chatbot-labelled-row">Sender authority</dt><dd><code class="chatbot-code">on-behalf-of</code></dd><dt class="chatbot-labelled-row">Expected post-state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Action summary state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Disabled reason</dt><dd><span>Evidence expired</span> <code class="chatbot-code">evidence-expired</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">await-approval</code></dd></dl>
                    <p class="chatbot-approval-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Policy snapshot detail is redacted or unavailable on this surface.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-001:decision:11" tabindex="0" aria-label="Approval event, Approval decision, Approved, 2026-06-01 08:10:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:001</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Approved</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:10:00.0000000Z">2026-06-01 08:10:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval decision</span> <code class="chatbot-code">decision</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Approved</span> <code class="chatbot-code">approved</code></dd><dt class="chatbot-labelled-row">Approval decision</dt><dd><span>Approved</span> <code class="chatbot-code">approve</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">approver-001</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Decided at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:10:00.0000000Z">2026-06-01 08:10:00Z</time></dd><dt class="chatbot-labelled-row">Authority result</dt><dd><code class="chatbot-code">authorized</code></dd><dt class="chatbot-labelled-row">Decision rationale state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit-approval-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Supersedes approval</dt><dd><code class="chatbot-code">approval-000</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-001:outcome:12" tabindex="0" aria-label="Approval event, Approval outcome, Approved, 2026-06-01 08:11:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:001</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Approved</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:11:00.0000000Z">2026-06-01 08:11:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval outcome</span> <code class="chatbot-code">outcome</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Approved</span> <code class="chatbot-code">approved</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">SendExternalReply</code></dd><dt class="chatbot-labelled-row">Command outcome status</dt><dd><code class="chatbot-code">accepted-projection-pending</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">reconciling</code></dd><dt class="chatbot-labelled-row">Outcome at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:11:00.0000000Z">2026-06-01 08:11:00Z</time></dd></dl>
                    <section class="chatbot-conversation-status-summary" aria-label="Status summary for item approval:approval-001:outcome:12" aria-live="off">
                      <h3 class="chatbot-conversation-status-summary__title">Status and next action</h3>
                      <ul class="chatbot-conversation-status-summary__list">
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="association" data-chatbot-health="healthy"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Association</span><span class="chatbot-conversation-status-summary__health">Healthy</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">associated</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>No user action</span> <code class="chatbot-code">none</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="attachment" data-chatbot-health="unknown"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Attachment</span><span class="chatbot-conversation-status-summary__health">Unknown</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">not-applicable</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>No user action</span> <code class="chatbot-code">none</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="task" data-chatbot-health="unknown"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Task</span><span class="chatbot-conversation-status-summary__health">Unknown</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">unknown</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>No user action</span> <code class="chatbot-code">none</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="approval" data-chatbot-health="healthy"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Approval</span><span class="chatbot-conversation-status-summary__health">Healthy</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">approved</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>No user action</span> <code class="chatbot-code">none</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="command" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Command</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">accepted-projection-pending</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Wait for projection</span> <code class="chatbot-code">wait-for-projection</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-approval-001</code></dd><dt class="chatbot-labelled-row">Completion status</dt><dd><code class="chatbot-code">accepted-projection-pending</code></dd><dt class="chatbot-labelled-row">Projection status</dt><dd><code class="chatbot-code">accepted-projection-pending</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">reconciling</code></dd><dt class="chatbot-labelled-row">Correlation id</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000012</code></dd><dt class="chatbot-labelled-row">Duplicate safety</dt><dd><code class="chatbot-code">duplicate-safe</code></dd></dl><p class="chatbot-conversation-status-summary__reason" tabindex="0" role="status" aria-live="polite" aria-atomic="true" data-chatbot-announcement-key="operation-approval-001" data-chatbot-live-announced="true">Accepted; projection is pending.</p></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="failure" data-chatbot-health="unknown"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Failure</span><span class="chatbot-conversation-status-summary__health">Unknown</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>No user action</span> <code class="chatbot-code">none</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="retry" data-chatbot-health="unknown"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Retry</span><span class="chatbot-conversation-status-summary__health">Unknown</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">not-retryable</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>No user action</span> <code class="chatbot-code">none</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="next-action" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Next action</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">wait-for-projection</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Wait for projection</span> <code class="chatbot-code">wait-for-projection</code></dd></dl></li>
                      </ul>
                    </section>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-001:outcome:13" tabindex="0" aria-label="Approval event, Approval outcome, Executed, 2026-06-01 08:12:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:001</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Executed</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:12:00.0000000Z">2026-06-01 08:12:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Executed</span> <code class="chatbot-code">executed</code></dd><dt class="chatbot-labelled-row">Command outcome status</dt><dd><code class="chatbot-code">completed</code></dd><dt class="chatbot-labelled-row">Projected outcome item</dt><dd><code class="chatbot-code">outcome:item:001</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-002:outcome:14" tabindex="0" aria-label="Approval event, Approval outcome, Failed, 2026-06-01 08:13:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:002</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Failed</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:13:00.0000000Z">2026-06-01 08:13:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Failed</span> <code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Command outcome status</dt><dd><code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Failure code</dt><dd><code class="chatbot-code">command-refused</code></dd><dt class="chatbot-labelled-row">Retryability</dt><dd><code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">unavailable</code></dd></dl>
                    <p class="chatbot-approval-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Audit detail is unavailable on this surface.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-002:decision:15" tabindex="0" aria-label="Approval event, Approval decision, Rejected, 2026-06-01 08:14:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:002</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Rejected</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:14:00.0000000Z">2026-06-01 08:14:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval decision</span> <code class="chatbot-code">decision</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Rejected</span> <code class="chatbot-code">rejected</code></dd><dt class="chatbot-labelled-row">Approval decision</dt><dd><span>Rejected</span> <code class="chatbot-code">reject</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">system-policy</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">system</code></dd><dt class="chatbot-labelled-row">Decided at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:14:00.0000000Z">2026-06-01 08:14:00Z</time></dd><dt class="chatbot-labelled-row">Authority result</dt><dd><code class="chatbot-code">denied</code></dd><dt class="chatbot-labelled-row">Disabled reason</dt><dd><span>Insufficient authority</span> <code class="chatbot-code">insufficient-authority</code></dd><dt class="chatbot-labelled-row">Decision rationale state</dt><dd><span>Unavailable</span> <code class="chatbot-code">unavailable</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">unavailable</code></dd><dt class="chatbot-labelled-row">Superseded by approval</dt><dd><code class="chatbot-code">approval-003</code></dd></dl>
                    <p class="chatbot-approval-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Audit detail is unavailable on this surface.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-003:decision:16" tabindex="0" aria-label="Approval event, Approval decision, Revision requested, 2026-06-01 08:15:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:003</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Revision requested</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:15:00.0000000Z">2026-06-01 08:15:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval decision</span> <code class="chatbot-code">decision</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Revision requested</span> <code class="chatbot-code">revision-requested</code></dd><dt class="chatbot-labelled-row">Approval decision</dt><dd><span>Requested revision</span> <code class="chatbot-code">request-revision</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">approver-002</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Decided at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:15:00.0000000Z">2026-06-01 08:15:00Z</time></dd><dt class="chatbot-labelled-row">Authority result</dt><dd><code class="chatbot-code">authorized</code></dd><dt class="chatbot-labelled-row">Decision rationale state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit-approval-003</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Supersedes approval</dt><dd><code class="chatbot-code">approval-002</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">revise-proposal</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-approval-conversation-item" data-chatbot-conversation-item-kind="ApprovalEvent" data-chatbot-conversation-item-id="approval:approval-004:decision:17" tabindex="0" aria-label="Approval event, Approval decision, Cancelled, 2026-06-01 08:16:00Z">
                    <header class="chatbot-approval-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">evidence:summary:004</span><span class="chatbot-chip chatbot-chip--risk">High</span><span class="chatbot-approval-conversation-item__status">Cancelled</span><span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span><time class="chatbot-metadata" datetime="2026-06-01T08:16:00.0000000Z">2026-06-01 08:16:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata"><dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval decision</span> <code class="chatbot-code">decision</code></dd><dt class="chatbot-labelled-row">Approval status</dt><dd><span>Cancelled</span> <code class="chatbot-code">cancelled</code></dd><dt class="chatbot-labelled-row">Approval decision</dt><dd><span>Cancelled</span> <code class="chatbot-code">cancel</code></dd><dt class="chatbot-labelled-row">Decision actor</dt><dd><code class="chatbot-code">requester-001</code></dd><dt class="chatbot-labelled-row">Decision actor type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Decided at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:16:00.0000000Z">2026-06-01 08:16:00Z</time></dd><dt class="chatbot-labelled-row">Authority result</dt><dd><code class="chatbot-code">authorized</code></dd><dt class="chatbot-labelled-row">Decision rationale state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">none</code></dd></dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-001:retry-queued:18" tabindex="0" aria-label="System status, Retry queued, Retryable, 2026-06-01 08:17:00Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Retry queued</span><span class="chatbot-chip chatbot-chip--risk">Projection pending</span><span class="chatbot-failure-conversation-item__status">Retryable</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:00.0000000Z">2026-06-01 08:17:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Retry queued</span> <code class="chatbot-code">retry-queued</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Retryable</span> <code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Retry queued</span> <code class="chatbot-code">retry_queued</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Projection pending</span> <code class="chatbot-code">projection-pending</code></dd><dt class="chatbot-labelled-row">Retryable</dt><dd><code class="chatbot-code">Yes</code></dd><dt class="chatbot-labelled-row">Retry count</dt><dd><code class="chatbot-code">1 of 3</code></dd><dt class="chatbot-labelled-row">Next retry</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:22:00.0000000Z">2026-06-01 08:22:00Z</time></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Task ID</dt><dd><code class="chatbot-code">task-001</code></dd><dt class="chatbot-labelled-row">Workflow instance</dt><dd><code class="chatbot-code">workflow-001</code></dd><dt class="chatbot-labelled-row">Retry operation</dt><dd><code class="chatbot-code">retry-operation-001</code></dd><dt class="chatbot-labelled-row">Duplicate safety</dt><dd><code class="chatbot-code">duplicate-safe</code></dd><dt class="chatbot-labelled-row">Audit operation</dt><dd><code class="chatbot-code">audit-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000018</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> A governed retry is queued and duplicate-safe.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Retry later when the governed dependency recovers.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                    <section class="chatbot-conversation-status-summary" aria-label="Status summary for item failure:operation-001:retry-queued:18" aria-live="off">
                      <h3 class="chatbot-conversation-status-summary__title">Status and next action</h3>
                      <ul class="chatbot-conversation-status-summary__list">
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="failure" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Failure</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">retry-queued</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Retry later when the governed dependency recovers.</span> <code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Operation id</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Correlation id</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000018</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="retry" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Retry</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">queued</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Retry later when the governed dependency recovers.</span> <code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Retry count</dt><dd><code class="chatbot-code">1</code></dd><dt class="chatbot-labelled-row">Duplicate safety</dt><dd><code class="chatbot-code">duplicate-safe</code></dd></dl></li>
                        <li class="chatbot-conversation-status-summary__facet" data-chatbot-status-domain="next-action" data-chatbot-health="degraded"><div class="chatbot-conversation-status-summary__facet-header"><span class="chatbot-conversation-status-summary__domain">Next action</span><span class="chatbot-conversation-status-summary__health">Degraded</span></div><dl class="chatbot-definition-list chatbot-conversation-status-summary__metadata"><dt class="chatbot-labelled-row">Source state</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><span>Retry later when the governed dependency recovers.</span> <code class="chatbot-code">retry-later</code></dd></dl></li>
                      </ul>
                    </section>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-001:retry-accepted:19" tabindex="0" aria-label="System status, Retry accepted, Retryable, 2026-06-01 08:17:10Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Retry accepted</span><span class="chatbot-chip chatbot-chip--risk">Projection pending</span><span class="chatbot-failure-conversation-item__status">Retryable</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:10.0000000Z">2026-06-01 08:17:10Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Retry accepted</span> <code class="chatbot-code">retry-accepted</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Retryable</span> <code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Retry accepted</span> <code class="chatbot-code">retry_accepted</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Projection pending</span> <code class="chatbot-code">projection-pending</code></dd><dt class="chatbot-labelled-row">Retryable</dt><dd><code class="chatbot-code">Yes</code></dd><dt class="chatbot-labelled-row">Retry count</dt><dd><code class="chatbot-code">2 of 3</code></dd><dt class="chatbot-labelled-row">Last retry</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:17:10.0000000Z">2026-06-01 08:17:10Z</time></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Task ID</dt><dd><code class="chatbot-code">task-001</code></dd><dt class="chatbot-labelled-row">Workflow instance</dt><dd><code class="chatbot-code">workflow-001</code></dd><dt class="chatbot-labelled-row">Retry operation</dt><dd><code class="chatbot-code">retry-operation-002</code></dd><dt class="chatbot-labelled-row">Duplicate safety</dt><dd><code class="chatbot-code">duplicate-safe</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000019</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> The retry was accepted without creating duplicate work.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Retry later when the governed dependency recovers.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-001:duplicate-suppressed:20" tabindex="0" aria-label="System status, Duplicate suppressed, Resolved, 2026-06-01 08:17:20Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Duplicate suppressed</span><span class="chatbot-chip chatbot-chip--risk">State not permitted</span><span class="chatbot-failure-conversation-item__status">Resolved</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:20.0000000Z">2026-06-01 08:17:20Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Duplicate suppressed</span> <code class="chatbot-code">duplicate-suppressed</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Resolved</span> <code class="chatbot-code">resolved</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Duplicate suppressed</span> <code class="chatbot-code">duplicate_suppressed</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>State not permitted</span> <code class="chatbot-code">state-not-permitted</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Workflow instance</dt><dd><code class="chatbot-code">workflow-001</code></dd><dt class="chatbot-labelled-row">Duplicate safety</dt><dd><code class="chatbot-code">duplicate-suppressed</code></dd><dt class="chatbot-labelled-row">Duplicate suppression</dt><dd><code class="chatbot-code">duplicate-suppression-001</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">none</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000020</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> A duplicate delivery was suppressed without changing the original item.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> No user action is required.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-002:dependency-degraded:21" tabindex="0" aria-label="System status, Dependency degraded, Degraded, 2026-06-01 08:17:30Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Dependency degraded</span><span class="chatbot-chip chatbot-chip--risk">Dependency degraded</span><span class="chatbot-failure-conversation-item__status">Degraded</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:30.0000000Z">2026-06-01 08:17:30Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Dependency degraded</span> <code class="chatbot-code">dependency-degraded</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Degraded</span> <code class="chatbot-code">degraded</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Dependency degraded</span> <code class="chatbot-code">dependency_degraded</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Dependency degraded</span> <code class="chatbot-code">dependency-degraded</code></dd><dt class="chatbot-labelled-row">Failure scope</dt><dd><code class="chatbot-code">mailbox-intake</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-002</code></dd><dt class="chatbot-labelled-row">Dependency</dt><dd><code class="chatbot-code">mailbox-projection</code></dd><dt class="chatbot-labelled-row">Degraded until</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:47:30.0000000Z">2026-06-01 08:47:30Z</time></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">wait-for-dependency</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">wait-for-dependency</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000021</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> A required dependency is temporarily degraded.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Wait for dependency recovery.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-003:projection-retryable:22" tabindex="0" aria-label="System status, Projection retryable, Retryable, 2026-06-01 08:17:40Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Projection retryable</span><span class="chatbot-chip chatbot-chip--risk">Projection unavailable</span><span class="chatbot-failure-conversation-item__status">Retryable</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:40.0000000Z">2026-06-01 08:17:40Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Projection retryable</span> <code class="chatbot-code">projection-retryable</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Retryable</span> <code class="chatbot-code">retryable</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Projection retryable</span> <code class="chatbot-code">projection_retryable</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Projection unavailable</span> <code class="chatbot-code">projection-pending</code></dd><dt class="chatbot-labelled-row">Failure scope</dt><dd><code class="chatbot-code">project-conversation</code></dd><dt class="chatbot-labelled-row">Retryable</dt><dd><code class="chatbot-code">Yes</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-003</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">retry-later</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000022</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> Projection status is retryable and remains metadata-only.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Retry later when the governed dependency recovers.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-004:blocked:23" tabindex="0" aria-label="System status, Refused action, Blocked, 2026-06-01 08:17:50Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Refused action</span><span class="chatbot-chip chatbot-chip--risk">Policy blocked</span><span class="chatbot-failure-conversation-item__status">Blocked</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:50.0000000Z">2026-06-01 08:17:50Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Refused action</span> <code class="chatbot-code">refusal_blocked_action</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Policy blocked</span> <code class="chatbot-code">policy-blocked</code></dd><dt class="chatbot-labelled-row">Failure reason</dt><dd><code class="chatbot-code">policy-blocked</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-004</code></dd><dt class="chatbot-labelled-row">Escalation target</dt><dd><code class="chatbot-code">project-owner</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">review-policy</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">request-access</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000023</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> This operation is blocked by policy.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Request access without probing restricted resources.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-005:blocked:24" tabindex="0" aria-label="System status, Audit unavailable, Blocked, 2026-06-01 08:17:55Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Audit unavailable</span><span class="chatbot-chip chatbot-chip--risk">Audit unavailable</span><span class="chatbot-failure-conversation-item__status">Blocked</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:55.0000000Z">2026-06-01 08:17:55Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Audit unavailable</span> <code class="chatbot-code">audit_unavailable</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Audit unavailable</span> <code class="chatbot-code">audit-unavailable</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-005</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">unavailable</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">escalate</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">escalate</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000024</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> Audit detail is unavailable on this surface.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Escalate to the configured owner or operations role.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Audit operation detail is redacted or unavailable on this surface.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-001:retry-exhausted:25" tabindex="0" aria-label="System status, Retry exhausted, Terminal, 2026-06-01 08:17:58Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Retry exhausted</span><span class="chatbot-chip chatbot-chip--risk">Retry exhausted</span><span class="chatbot-failure-conversation-item__status">Terminal</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:17:58.0000000Z">2026-06-01 08:17:58Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Retry exhausted</span> <code class="chatbot-code">retry-exhausted</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Terminal</span> <code class="chatbot-code">terminal</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Retry exhausted</span> <code class="chatbot-code">retry_exhausted</code></dd><dt class="chatbot-labelled-row">Catalog version</dt><dd><code class="chatbot-code">chatbot.message-catalog.v1</code></dd><dt class="chatbot-labelled-row">Detail visibility</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Retry exhausted</span> <code class="chatbot-code">retry-exhausted</code></dd><dt class="chatbot-labelled-row">Retryable</dt><dd><code class="chatbot-code">No</code></dd><dt class="chatbot-labelled-row">Retry count</dt><dd><code class="chatbot-code">3 of 3</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Workflow instance</dt><dd><code class="chatbot-code">workflow-001</code></dd><dt class="chatbot-labelled-row">Escalation target</dt><dd><code class="chatbot-code">operations</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">escalate</code></dd><dt class="chatbot-labelled-row">Client action</dt><dd><code class="chatbot-code">escalate</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000025</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> Retry attempts are exhausted and operator recovery is required.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Next action</strong> Escalate to the configured owner or operations role.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Duplicate safety</strong> Retries and duplicate suppression use governed metadata and do not replace prior history.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Terminal rule</strong> Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-001:terminal-failure:26" tabindex="0" aria-label="System status, Terminal failure, Terminal, 2026-06-01 08:18:00Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Terminal failure</span><span class="chatbot-chip chatbot-chip--risk">Terminal state</span><span class="chatbot-failure-conversation-item__status">Terminal</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:18:00.0000000Z">2026-06-01 08:18:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Terminal failure</span> <code class="chatbot-code">terminal-failure</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Terminal</span> <code class="chatbot-code">terminal</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Terminal failure</span> <code class="chatbot-code">terminal_failure</code></dd><dt class="chatbot-labelled-row">Blocked reason</dt><dd><span>Terminal state</span> <code class="chatbot-code">terminal-state</code></dd><dt class="chatbot-labelled-row">Retryable</dt><dd><code class="chatbot-code">No</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">unavailable</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">escalate</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Audit operation detail is redacted or unavailable on this surface.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Terminal rule</strong> Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-failure-conversation-item" data-chatbot-conversation-item-kind="FailureState" data-chatbot-conversation-item-id="failure:operation-001:reprocess-created:27" tabindex="0" aria-label="System status, Reprocess created, Resolved, 2026-06-01 08:19:00Z">
                    <header class="chatbot-failure-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Reprocess created</span><span class="chatbot-chip chatbot-chip--risk">Terminal state</span><span class="chatbot-failure-conversation-item__status">Resolved</span><span class="chatbot-actor-badge" aria-label="System actor: System status">System status</span><time class="chatbot-metadata" datetime="2026-06-01T08:19:00.0000000Z">2026-06-01 08:19:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-failure-conversation-item__metadata"><dt class="chatbot-labelled-row">Failure state kind</dt><dd><span>Reprocess created</span> <code class="chatbot-code">reprocess-created</code></dd><dt class="chatbot-labelled-row">Failure status</dt><dd><span>Resolved</span> <code class="chatbot-code">resolved</code></dd><dt class="chatbot-labelled-row">Catalog code</dt><dd><span>Reprocess created</span> <code class="chatbot-code">reprocess_created</code></dd><dt class="chatbot-labelled-row">Reprocess workflow</dt><dd><code class="chatbot-code">workflow-002</code></dd><dt class="chatbot-labelled-row">Supersedes workflow</dt><dd><code class="chatbot-code">workflow-001</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-001</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">none</code></dd></dl>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Reason</strong> A new workflow instance was created for reprocessing.</p>
                    <p class="chatbot-failure-conversation-item__reason" tabindex="0"><strong>Terminal rule</strong> Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.</p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:proposal:30" tabindex="0" aria-label="AI actor, AI proposal, Proposed, 2026-06-01 08:20:00Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk" data-chatbot-status="warning" data-chatbot-risk-class="ToolInvoking" role="status" aria-label="Risk: Tool-invoking. Policy reason: approval-required."><span class="chatbot-chip__label">Tool-invoking</span><span class="chatbot-chip__status">approval-required</span></span><span class="chatbot-ai-outcome-conversation-item__status">Proposed</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:00.0000000Z">2026-06-01 08:20:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI proposal</span> <code class="chatbot-code">proposal</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Proposed</span> <code class="chatbot-code">proposed</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">approval-required</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">invokes-tools</code></dd><dt class="chatbot-labelled-row">Policy reason</dt><dd><code class="chatbot-code">policy_requires_approval</code></dd><dt class="chatbot-labelled-row">Classifier version</dt><dd><code class="chatbot-code">ai-action-risk-classifier.m0.v1</code></dd><dt class="chatbot-labelled-row">Risk input tuple</dt><dd><code class="chatbot-code">command=Project.AppendConversationMessage;effect=project-state;authority=project-contributor;policy=approval-required</code></dd><dt class="chatbot-labelled-row">Requester authority</dt><dd><code class="chatbot-code">project-contributor</code></dd><dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snapshot-4-3</code></dd><dt class="chatbot-labelled-row">Policy visibility</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd><dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-allowlist.m0</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000030</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:denial:31" tabindex="0" aria-label="AI actor, AI denial, Denied, 2026-06-01 08:20:10Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Denied</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:10.0000000Z">2026-06-01 08:20:10Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI denial</span> <code class="chatbot-code">denial</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Denied</span> <code class="chatbot-code">denied</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000031</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:refusal:32" tabindex="0" aria-label="AI actor, AI refusal, Blocked, 2026-06-01 08:20:20Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Blocked</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:20.0000000Z">2026-06-01 08:20:20Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI refusal</span> <code class="chatbot-code">refusal</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Blocked</span> <code class="chatbot-code">blocked</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000032</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:execution-started:33" tabindex="0" aria-label="AI actor, AI execution started, Executing, 2026-06-01 08:20:30Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Executing</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:30.0000000Z">2026-06-01 08:20:30Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution started</span> <code class="chatbot-code">execution-started</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Executing</span> <code class="chatbot-code">executing</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000033</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:execution-succeeded:34" tabindex="0" aria-label="AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:20:40Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:40.0000000Z">2026-06-01 08:20:40Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution succeeded</span> <code class="chatbot-code">execution-succeeded</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Succeeded</span> <code class="chatbot-code">succeeded</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000034</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:execution-failed:35" tabindex="0" aria-label="AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Failed</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:20:50.0000000Z">2026-06-01 08:20:50Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI execution failed</span> <code class="chatbot-code">execution-failed</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Failed</span> <code class="chatbot-code">failed</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000035</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:outcome-recorded:36" tabindex="0" aria-label="AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:21:00Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:21:00.0000000Z">2026-06-01 08:21:00Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI outcome recorded</span> <code class="chatbot-code">outcome-recorded</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Succeeded</span> <code class="chatbot-code">succeeded</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000036</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-001:corrected-context-invalidated:37" tabindex="0" aria-label="AI actor, Corrected context invalidated, Invalidated, 2026-06-01 08:21:10Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span><span class="chatbot-ai-outcome-conversation-item__status">Invalidated</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:21:10.0000000Z">2026-06-01 08:21:10Z</time></header>
                    <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata"><dt class="chatbot-labelled-row">AI outcome</dt><dd><span>Corrected context invalidated</span> <code class="chatbot-code">corrected-context-invalidated</code></dd><dt class="chatbot-labelled-row">Status</dt><dd><span>Invalidated</span> <code class="chatbot-code">invalidated</code></dd><dt class="chatbot-labelled-row">Actor type</dt><dd><span>AI actor</span> <code class="chatbot-code">ai</code></dd><dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-001</code></dd><dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">tool-invoking</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000037</code></dd></dl>
                    <section class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI-generated" data-chatbot-ai-content="ai-generated"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI-generated</strong> AI-generated content is labelled and kept distinct from source evidence.</p></section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-attachment-conversation-item"
                           data-chatbot-conversation-item-kind="Attachment"
                           data-chatbot-conversation-item-id="attachment:01HZXASSOC000000000000001:0:826F"
                           tabindex="0"
                           aria-label="Mailbox attachment, invoice.pdf, Pending, Associated">
                    <header class="chatbot-attachment-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">invoice.pdf</span>
                      <span class="chatbot-attachment-conversation-item__status">Pending</span>
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox attachment">Mailbox attachment</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:30.0000000Z">2026-06-01 08:02:30Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-attachment-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Attachment name</dt>
                      <dd>invoice.pdf</dd>
                      <dt class="chatbot-labelled-row">Provider attachment ID</dt>
                      <dd><code class="chatbot-code">graph-attachment-001</code></dd>
                      <dt class="chatbot-labelled-row">Content type</dt>
                      <dd><code class="chatbot-code">application/pdf</code></dd>
                      <dt class="chatbot-labelled-row">Size</dt>
                      <dd><code class="chatbot-code">4,096.00</code></dd>
                      <dt class="chatbot-labelled-row">Capture status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Storage status</dt>
                      <dd>Pending</dd>
                      <dt class="chatbot-labelled-row">Scan status</dt>
                      <dd>Pending</dd>
                      <dt class="chatbot-labelled-row">Duplicate state</dt>
                      <dd><code class="chatbot-code">not-evaluated</code></dd>
                      <dt class="chatbot-labelled-row">Retry state</dt>
                      <dd><code class="chatbot-code">not-retryable</code></dd>
                      <dt class="chatbot-labelled-row">AI context eligibility</dt>
                      <dd><code class="chatbot-code">pending</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd>Metadata only</dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000007</code></dd>
                    </dl>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment actions are unavailable until storage and scan state are governed.</p>
                    <div class="chatbot-attachment-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">invoice.pdf</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Metadata only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Pending</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-attachment-conversation-item"
                           data-chatbot-conversation-item-kind="Attachment"
                           data-chatbot-conversation-item-id="attachment:01HZXASSOC000000000000001:1:4A1B"
                           tabindex="0"
                           aria-label="Mailbox attachment, release-notes.pdf, Captured, Associated">
                    <header class="chatbot-attachment-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">release-notes.pdf</span>
                      <span class="chatbot-attachment-conversation-item__status">Captured</span>
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox attachment">Mailbox attachment</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:31.0000000Z">2026-06-01 08:02:31Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-attachment-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Attachment name</dt>
                      <dd>release-notes.pdf</dd>
                      <dt class="chatbot-labelled-row">Provider attachment ID</dt>
                      <dd><code class="chatbot-code">graph-attachment-002</code></dd>
                      <dt class="chatbot-labelled-row">Content type</dt>
                      <dd><code class="chatbot-code">application/pdf</code></dd>
                      <dt class="chatbot-labelled-row">Size</dt>
                      <dd><code class="chatbot-code">8,192.00</code></dd>
                      <dt class="chatbot-labelled-row">Capture status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Storage status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Scan status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Duplicate state</dt>
                      <dd><code class="chatbot-code">unique</code></dd>
                      <dt class="chatbot-labelled-row">Retry state</dt>
                      <dd><code class="chatbot-code">not-retryable</code></dd>
                      <dt class="chatbot-labelled-row">AI context eligibility</dt>
                      <dd><code class="chatbot-code">eligible</code></dd>
                      <dt class="chatbot-labelled-row">File reference</dt>
                      <dd><code class="chatbot-code">file-reference-001</code></dd>
                      <dt class="chatbot-labelled-row">Folder reference</dt>
                      <dd><code class="chatbot-code">folder-reference-001</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd>Metadata only</dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000008</code></dd>
                    </dl>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Open governed file, Add to AI context</p>
                    <div class="chatbot-attachment-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">release-notes.pdf</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Metadata only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Captured</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-attachment-conversation-item"
                           data-chatbot-conversation-item-kind="Attachment"
                           data-chatbot-conversation-item-id="attachment:01HZXASSOC000000000000001:2:9F20"
                           tabindex="0"
                           aria-label="Mailbox attachment, Attachment unavailable, Unavailable, Associated">
                    <header class="chatbot-attachment-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Attachment unavailable</span>
                      <span class="chatbot-attachment-conversation-item__status">Unavailable</span>
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox attachment">Mailbox attachment</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:32.0000000Z">2026-06-01 08:02:32Z</time>
                    </header>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment metadata is unavailable on this surface.</p>
                    <dl class="chatbot-definition-list chatbot-attachment-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Attachment name</dt>
                      <dd>Attachment unavailable</dd>
                      <dt class="chatbot-labelled-row">Provider attachment ID</dt>
                      <dd><code class="chatbot-code">graph-attachment-003</code></dd>
                      <dt class="chatbot-labelled-row">Content type</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Size</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Capture status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Storage status</dt>
                      <dd>Unavailable</dd>
                      <dt class="chatbot-labelled-row">Scan status</dt>
                      <dd>Unavailable</dd>
                      <dt class="chatbot-labelled-row">Duplicate state</dt>
                      <dd><code class="chatbot-code">unknown</code></dd>
                      <dt class="chatbot-labelled-row">Retry state</dt>
                      <dd><code class="chatbot-code">not-retryable</code></dd>
                      <dt class="chatbot-labelled-row">AI context eligibility</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd>Metadata only</dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000009</code></dd>
                    </dl>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment actions are unavailable until storage and scan state are governed.</p>
                    <div class="chatbot-attachment-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Attachment unavailable</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Metadata only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Unavailable</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-attachment-conversation-item"
                           data-chatbot-conversation-item-kind="Attachment"
                           data-chatbot-conversation-item-id="attachment:01HZXASSOC000000000000001:3:D4C2"
                           tabindex="0"
                           aria-label="Mailbox attachment, Redacted attachment, Pending, Associated">
                    <header class="chatbot-attachment-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Redacted attachment</span>
                      <span class="chatbot-attachment-conversation-item__status">Pending</span>
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox attachment">Mailbox attachment</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:33.0000000Z">2026-06-01 08:02:33Z</time>
                    </header>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment metadata is redacted by policy.</p>
                    <dl class="chatbot-definition-list chatbot-attachment-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Attachment name</dt>
                      <dd>Redacted attachment</dd>
                      <dt class="chatbot-labelled-row">Provider attachment ID</dt>
                      <dd><code class="chatbot-code">graph-attachment-004</code></dd>
                      <dt class="chatbot-labelled-row">Content type</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Size</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Capture status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Storage status</dt>
                      <dd>Pending</dd>
                      <dt class="chatbot-labelled-row">Scan status</dt>
                      <dd>Pending</dd>
                      <dt class="chatbot-labelled-row">Duplicate state</dt>
                      <dd><code class="chatbot-code">redacted</code></dd>
                      <dt class="chatbot-labelled-row">Retry state</dt>
                      <dd><code class="chatbot-code">not-retryable</code></dd>
                      <dt class="chatbot-labelled-row">AI context eligibility</dt>
                      <dd><code class="chatbot-code">redacted</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd>Redacted</dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000010</code></dd>
                    </dl>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment actions are unavailable until storage and scan state are governed.</p>
                    <div class="chatbot-attachment-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Redacted attachment</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Redacted</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Pending</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-attachment-conversation-item"
                           data-chatbot-conversation-item-kind="Attachment"
                           data-chatbot-conversation-item-id="attachment:01HZXASSOC000000000000001:4:70EA"
                           tabindex="0"
                           aria-label="Mailbox attachment, duplicate-invoice.pdf, Retryable, Associated">
                    <header class="chatbot-attachment-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">duplicate-invoice.pdf</span>
                      <span class="chatbot-attachment-conversation-item__status">Retryable</span>
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox attachment">Mailbox attachment</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:34.0000000Z">2026-06-01 08:02:34Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-attachment-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Attachment name</dt>
                      <dd>duplicate-invoice.pdf</dd>
                      <dt class="chatbot-labelled-row">Provider attachment ID</dt>
                      <dd><code class="chatbot-code">graph-attachment-005</code></dd>
                      <dt class="chatbot-labelled-row">Content type</dt>
                      <dd><code class="chatbot-code">application/pdf</code></dd>
                      <dt class="chatbot-labelled-row">Size</dt>
                      <dd><code class="chatbot-code">4,096.00</code></dd>
                      <dt class="chatbot-labelled-row">Capture status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Storage status</dt>
                      <dd>Retryable</dd>
                      <dt class="chatbot-labelled-row">Scan status</dt>
                      <dd>Retryable</dd>
                      <dt class="chatbot-labelled-row">Duplicate state</dt>
                      <dd><code class="chatbot-code">duplicate-provider-attachment-suppressed</code></dd>
                      <dt class="chatbot-labelled-row">Retry state</dt>
                      <dd><code class="chatbot-code">retryable-after-policy-window</code></dd>
                      <dt class="chatbot-labelled-row">AI context eligibility</dt>
                      <dd><code class="chatbot-code">pending</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd>Metadata only</dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">retry-attachment</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000011</code></dd>
                    </dl>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Retry capture</p>
                    <div class="chatbot-attachment-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">duplicate-invoice.pdf</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Metadata only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Retryable</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-attachment-conversation-item"
                           data-chatbot-conversation-item-kind="Attachment"
                           data-chatbot-conversation-item-id="attachment:01HZXASSOC000000000000001:5:13B7"
                           tabindex="0"
                           aria-label="Mailbox attachment, Attachment unavailable, Unsafe, Associated">
                    <header class="chatbot-attachment-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Attachment unavailable</span>
                      <span class="chatbot-attachment-conversation-item__status">Unsafe</span>
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox attachment">Mailbox attachment</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:35.0000000Z">2026-06-01 08:02:35Z</time>
                    </header>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment metadata is unavailable on this surface.</p>
                    <dl class="chatbot-definition-list chatbot-attachment-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Attachment name</dt>
                      <dd>Attachment unavailable</dd>
                      <dt class="chatbot-labelled-row">Provider attachment ID</dt>
                      <dd><code class="chatbot-code">graph-attachment-006</code></dd>
                      <dt class="chatbot-labelled-row">Content type</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Size</dt>
                      <dd><code class="chatbot-code">unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Capture status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Storage status</dt>
                      <dd>Captured</dd>
                      <dt class="chatbot-labelled-row">Scan status</dt>
                      <dd>Unsafe</dd>
                      <dt class="chatbot-labelled-row">Duplicate state</dt>
                      <dd><code class="chatbot-code">not-evaluated</code></dd>
                      <dt class="chatbot-labelled-row">Retry state</dt>
                      <dd><code class="chatbot-code">not-retryable</code></dd>
                      <dt class="chatbot-labelled-row">AI context eligibility</dt>
                      <dd><code class="chatbot-code">blocked-unsafe</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Redaction state</dt>
                      <dd>Metadata only</dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">quarantine-review</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000012</code></dd>
                    </dl>
                    <p class="chatbot-attachment-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Attachment actions are unavailable until storage and scan state are governed.</p>
                    <div class="chatbot-attachment-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Attachment unavailable</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">Metadata only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Unsafe</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-participant-conversation-item"
                           data-chatbot-conversation-item-kind="Participant"
                           data-chatbot-conversation-item-id="participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000001"
                           tabindex="0"
                           aria-label="Internal participant: Internal contributor, Associated">
                    <header class="chatbot-participant-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:sender</span>
                      <span class="chatbot-participant-conversation-item__status">Resolved</span>
                      <span class="chatbot-actor-badge" aria-label="Human user actor: Internal contributor">Internal contributor</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:03:00.0000000Z">2026-06-01 08:03:00Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-participant-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Participant type</dt>
                      <dd><code class="chatbot-code">Internal participant</code></dd>
                      <dt class="chatbot-labelled-row">Participant status</dt>
                      <dd><code class="chatbot-code">Resolved</code></dd>
                      <dt class="chatbot-labelled-row">Participant resolution</dt>
                      <dd><code class="chatbot-code">01HZXRESOLUTION00000000001</code></dd>
                      <dt class="chatbot-labelled-row">Source participant</dt>
                      <dd><code class="chatbot-code">01HZXPARTICIPANT000000001</code></dd>
                      <dt class="chatbot-labelled-row">Party ID</dt>
                      <dd><code class="chatbot-code">tenant-alpha:parties:party-001</code></dd>
                      <dt class="chatbot-labelled-row">Evidence reference</dt>
                      <dd><code class="chatbot-code">mailbox:intake:sender</code></dd>
                      <dt class="chatbot-labelled-row">Evidence fingerprint</dt>
                      <dd><code class="chatbot-code">evidence-sha256-internal</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000003</code></dd>
                    </dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-participant-conversation-item"
                           data-chatbot-conversation-item-kind="Participant"
                           data-chatbot-conversation-item-id="participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000002"
                           tabindex="0"
                           aria-label="External participant: External contributor, Associated">
                    <header class="chatbot-participant-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">mailbox:intake:recipient:0</span>
                      <span class="chatbot-participant-conversation-item__status">Resolved</span>
                      <span class="chatbot-actor-badge" aria-label="External party actor: External contributor">External contributor</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:04:00.0000000Z">2026-06-01 08:04:00Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-participant-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Participant type</dt>
                      <dd><code class="chatbot-code">External participant</code></dd>
                      <dt class="chatbot-labelled-row">Participant status</dt>
                      <dd><code class="chatbot-code">Resolved</code></dd>
                      <dt class="chatbot-labelled-row">Participant resolution</dt>
                      <dd><code class="chatbot-code">01HZXRESOLUTION00000000001</code></dd>
                      <dt class="chatbot-labelled-row">Source participant</dt>
                      <dd><code class="chatbot-code">01HZXPARTICIPANT000000002</code></dd>
                      <dt class="chatbot-labelled-row">Party ID</dt>
                      <dd><code class="chatbot-code">tenant-alpha:parties:party-002</code></dd>
                      <dt class="chatbot-labelled-row">Evidence reference</dt>
                      <dd><code class="chatbot-code">mailbox:intake:recipient:0</code></dd>
                      <dt class="chatbot-labelled-row">Evidence fingerprint</dt>
                      <dd><code class="chatbot-code">evidence-sha256-external</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000004</code></dd>
                    </dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-participant-conversation-item"
                           data-chatbot-conversation-item-kind="Participant"
                           data-chatbot-conversation-item-id="participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000003"
                           tabindex="0"
                           aria-label="Unresolved participant: Unresolved participant, Associated">
                    <header class="chatbot-participant-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">mailbox:intake:recipient:1</span>
                      <span class="chatbot-participant-conversation-item__status">Unresolved</span>
                      <span class="chatbot-actor-badge" aria-label="External party actor: Unresolved participant">Unresolved participant <button class="chatbot-actor-badge__action" type="button">Why unavailable?</button></span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:05:00.0000000Z">2026-06-01 08:05:00Z</time>
                    </header>
                    <p class="chatbot-participant-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Participant detail is unavailable: Participant not found</p>
                    <dl class="chatbot-definition-list chatbot-participant-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Participant type</dt>
                      <dd><code class="chatbot-code">Unresolved participant</code></dd>
                      <dt class="chatbot-labelled-row">Participant status</dt>
                      <dd><code class="chatbot-code">Unresolved</code></dd>
                      <dt class="chatbot-labelled-row">Participant resolution</dt>
                      <dd><code class="chatbot-code">01HZXRESOLUTION00000000001</code></dd>
                      <dt class="chatbot-labelled-row">Source participant</dt>
                      <dd><code class="chatbot-code">01HZXPARTICIPANT000000003</code></dd>
                      <dt class="chatbot-labelled-row">Blocked reason</dt>
                      <dd>Participant not found</dd>
                      <dt class="chatbot-labelled-row">Evidence reference</dt>
                      <dd><code class="chatbot-code">mailbox:intake:recipient:1</code></dd>
                      <dt class="chatbot-labelled-row">Evidence fingerprint</dt>
                      <dd><code class="chatbot-code">evidence-sha256-unresolved</code></dd>
                      <dt class="chatbot-labelled-row">Allowed review actions</dt>
                      <dd>Link participant, Create pending participant</dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000005</code></dd>
                    </dl>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-participant-conversation-item"
                           data-chatbot-conversation-item-kind="Participant"
                           data-chatbot-conversation-item-id="participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000004"
                           tabindex="0"
                           aria-label="Restricted participant: Restricted participant, Associated">
                    <header class="chatbot-participant-conversation-item__header">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unauthorized">mailbox:intake:recipient:2</span>
                      <span class="chatbot-participant-conversation-item__status">Resolved</span>
                      <span class="chatbot-actor-badge" aria-label="External party actor: Restricted participant">Restricted participant <button class="chatbot-actor-badge__action" type="button">Why unavailable?</button></span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:06:00.0000000Z">2026-06-01 08:06:00Z</time>
                    </header>
                    <p class="chatbot-participant-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Participant detail is unavailable: Restricted party</p>
                    <dl class="chatbot-definition-list chatbot-participant-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Participant type</dt>
                      <dd><code class="chatbot-code">Restricted participant</code></dd>
                      <dt class="chatbot-labelled-row">Participant status</dt>
                      <dd><code class="chatbot-code">Resolved</code></dd>
                      <dt class="chatbot-labelled-row">Participant resolution</dt>
                      <dd><code class="chatbot-code">01HZXRESOLUTION00000000001</code></dd>
                      <dt class="chatbot-labelled-row">Source participant</dt>
                      <dd><code class="chatbot-code">01HZXPARTICIPANT000000004</code></dd>
                      <dt class="chatbot-labelled-row">Blocked reason</dt>
                      <dd>Restricted party</dd>
                      <dt class="chatbot-labelled-row">Evidence reference</dt>
                      <dd><code class="chatbot-code">mailbox:intake:recipient:2</code></dd>
                      <dt class="chatbot-labelled-row">Evidence fingerprint</dt>
                      <dd><code class="chatbot-code">evidence-sha256-restricted</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000006</code></dd>
                    </dl>
                  </article>
                </li>
              </ol>
              <aside id="why-project-panel"
                     class="chatbot-why-project-panel"
                     role="complementary"
                     aria-label="Why this project evidence for association 01HZXASSOC000000000000001"
                     data-chatbot-why-project-panel="metadata-only"
                     tabindex="0"
                     hidden>
                <header class="chatbot-why-project-panel__header">
                  <h2 class="chatbot-section-title">Why this project</h2>
                  <button type="button"
                          class="chatbot-why-project-panel__close"
                          aria-label="Close why this project panel"
                          onclick="document.getElementById('why-project-panel').hidden=true;">
                    x
                  </button>
                </header>
                <dl class="chatbot-definition-list chatbot-why-project-panel__metadata">
                  <dt class="chatbot-labelled-row">Operation</dt>
                  <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                  <dt class="chatbot-labelled-row">Signal class</dt>
                  <dd><code class="chatbot-code">explicit-project-identifier</code></dd>
                  <dt class="chatbot-labelled-row">Matched value</dt>
                  <dd><code class="chatbot-code">mailbox:metadata</code></dd>
                  <dt class="chatbot-labelled-row">Confidence</dt>
                  <dd><code class="chatbot-code">91%</code></dd>
                  <dt class="chatbot-labelled-row">Threshold band</dt>
                  <dd><code class="chatbot-code">Auto</code></dd>
                  <dt class="chatbot-labelled-row">Policy snapshot</dt>
                  <dd><code class="chatbot-code">association-thresholds.m0.default.v1</code></dd>
                  <dt class="chatbot-labelled-row">Scorer/kernel version</dt>
                  <dd><code class="chatbot-code">association-deterministic.kernel.m0.v1</code></dd>
                  <dt class="chatbot-labelled-row">Decision actor</dt>
                  <dd><code class="chatbot-code">actor-safe</code></dd>
                  <dt class="chatbot-labelled-row">Decision actor type</dt>
                  <dd><code class="chatbot-code">human</code></dd>
                  <dt class="chatbot-labelled-row">Decided at</dt>
                  <dd><time class="chatbot-code" datetime="2026-06-01T08:02:00.0000000Z">2026-06-01 08:02:00Z</time></dd>
                  <dt class="chatbot-labelled-row">Source provenance</dt>
                  <dd><code class="chatbot-code">m365-mailbox-intake</code></dd>
                  <dt class="chatbot-labelled-row">Source version</dt>
                  <dd><code class="chatbot-code">3</code></dd>
                  <dt class="chatbot-labelled-row">Correlation ID</dt>
                  <dd><code class="chatbot-code">01HZXCORRELATION00000000002</code></dd>
                  <dt class="chatbot-labelled-row">Redaction state</dt>
                  <dd><code class="chatbot-code">metadata_only</code></dd>
                  <dt class="chatbot-labelled-row">Schema version</dt>
                  <dd><code class="chatbot-code">chatbot.association-routing-status.v1</code></dd>
                  <dt class="chatbot-labelled-row">Safe next actions</dt>
                  <dd><code class="chatbot-code">none</code></dd>
                </dl>
                <button type="button"
                        class="chatbot-why-project-panel__correction"
                        data-chatbot-correction-link="association:01HZXASSOC000000000000002"
                        onclick="document.getElementById('why-project-panel').hidden=true;document.getElementById('why-project-correction-panel').hidden=false;document.getElementById('why-project-correction-panel').focus();">
                  Open superseding correction correction-002
                </button>
                <section class="chatbot-why-project-panel__evidence" aria-labelledby="why-project-evidence-title">
                  <h3 id="why-project-evidence-title" class="chatbot-section-title">Authorized evidence</h3>
                  <ol class="chatbot-why-project-panel__evidence-list">
                    <li class="chatbot-why-project-panel__evidence-row" data-chatbot-evidence-visibility="available" tabindex="0">
                      <div class="chatbot-why-project-panel__evidence-header">
                        <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">explicit-project-identifier: mailbox:metadata</span>
                        <span class="chatbot-metadata">fresh</span>
                      </div>
                      <dl class="chatbot-definition-list chatbot-why-project-panel__evidence-metadata">
                        <dt class="chatbot-labelled-row">Signal class</dt>
                        <dd><code class="chatbot-code">explicit-project-identifier</code></dd>
                        <dt class="chatbot-labelled-row">Matched value</dt>
                        <dd><code class="chatbot-code">mailbox:metadata</code></dd>
                        <dt class="chatbot-labelled-row">Evidence reference</dt>
                        <dd><code class="chatbot-code">mailbox:project-id</code></dd>
                        <dt class="chatbot-labelled-row">Evidence fingerprint</dt>
                        <dd><code class="chatbot-code">evidence-sha256-project</code></dd>
                        <dt class="chatbot-labelled-row">Evidence freshness</dt>
                        <dd><code class="chatbot-code">fresh</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt>
                        <dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Confidence contribution</dt>
                        <dd><code class="chatbot-code">0.42</code></dd>
                      </dl>
                    </li>
                    <li class="chatbot-why-project-panel__evidence-row" data-chatbot-evidence-visibility="redacted" tabindex="0">
                      <div class="chatbot-why-project-panel__evidence-header">
                        <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">human-selection: redacted-selection-token</span>
                        <span class="chatbot-metadata">unavailable</span>
                      </div>
                      <dl class="chatbot-definition-list chatbot-why-project-panel__evidence-metadata">
                        <dt class="chatbot-labelled-row">Signal class</dt>
                        <dd><code class="chatbot-code">human-selection</code></dd>
                        <dt class="chatbot-labelled-row">Matched value</dt>
                        <dd><code class="chatbot-code">redacted-selection-token</code></dd>
                        <dt class="chatbot-labelled-row">Evidence reference</dt>
                        <dd><code class="chatbot-code">association:human-selection</code></dd>
                        <dt class="chatbot-labelled-row">Evidence fingerprint</dt>
                        <dd><code class="chatbot-code">evidence-sha256-redacted</code></dd>
                        <dt class="chatbot-labelled-row">Evidence freshness</dt>
                        <dd><code class="chatbot-code">unavailable</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt>
                        <dd><code class="chatbot-code">redacted</code></dd>
                      </dl>
                      <p class="chatbot-why-project-panel__state" tabindex="0">Some evidence detail is redacted or unavailable for this user. The panel keeps the decision understandable without confirming hidden resources.</p>
                    </li>
                  </ol>
                </section>
              </aside>
              <aside id="why-project-correction-panel"
                     class="chatbot-why-project-panel"
                     role="complementary"
                     aria-label="Why this project evidence for association 01HZXASSOC000000000000002"
                     data-chatbot-why-project-panel="metadata-only"
                     tabindex="0"
                     hidden>
                <header class="chatbot-why-project-panel__header">
                  <h2 class="chatbot-section-title">Why this project</h2>
                  <button type="button"
                          class="chatbot-why-project-panel__close"
                          aria-label="Close why this project panel"
                          onclick="document.getElementById('why-project-correction-panel').hidden=true;">
                    x
                  </button>
                </header>
                <dl class="chatbot-definition-list chatbot-why-project-panel__metadata">
                  <dt class="chatbot-labelled-row">Operation</dt>
                  <dd><code class="chatbot-code">01HZXASSOC000000000000002</code></dd>
                  <dt class="chatbot-labelled-row">Signal class</dt>
                  <dd><code class="chatbot-code">correction</code></dd>
                  <dt class="chatbot-labelled-row">Matched value</dt>
                  <dd><code class="chatbot-code">association:correction-metadata</code></dd>
                  <dt class="chatbot-labelled-row">Confidence</dt>
                  <dd><code class="chatbot-code">93%</code></dd>
                  <dt class="chatbot-labelled-row">Threshold band</dt>
                  <dd><code class="chatbot-code">Auto</code></dd>
                  <dt class="chatbot-labelled-row">Decision actor</dt>
                  <dd><code class="chatbot-code">user-002</code></dd>
                </dl>
                <p class="chatbot-why-project-panel__state" tabindex="0">
                  <strong>Corrected context</strong>
                  Propagation completed; impact corrected-context-ready; next action none.
                </p>
              </aside>
            </section>
            """;

    private static string BuildEmptyBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="warning"
                 data-chatbot-feedback-state="BlockedAction"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation status: empty">
              <span class="chatbot-status__label">Warning</span>
              <span>Empty</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <div class="chatbot-blocked-state"
                   data-chatbot-feedback-state="BlockedAction"
                   role="alert"
                   aria-live="polite"
                   aria-label="Blocked: No email-derived context is available. Next action: Wait for associated email.">
                <span class="chatbot-status__label">Blocked</span>
                <p>No email-derived context is available.</p>
                <p>Wait for associated email.</p>
              </div>
            </section>
            """;

    private static string BuildUnauthorizedBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="warning"
                 data-chatbot-feedback-state="BlockedAction"
                 role="alert"
                 aria-live="polite"
                 aria-label="Blocked: Project conversation is unavailable. Next action: Verify access or choose an authorized project.">
              <span class="chatbot-status__label">Warning</span>
              <span>Project conversation is unavailable.</span>
              <span>Verify access or choose an authorized project.</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <article class="chatbot-email-conversation-item"
                       data-chatbot-conversation-item-kind="Redacted"
                       data-chatbot-conversation-item-id="project-redacted"
                       aria-label="Redacted project conversation item">
                <header class="chatbot-email-conversation-item__header">
                  <span class="chatbot-actor-badge" aria-label="Mailbox actor: redacted">Mailbox event</span>
                  <span class="chatbot-email-conversation-item__decision">Evidence restricted</span>
                </header>
                <dl class="chatbot-definition-list chatbot-email-conversation-item__metadata">
                  <dt class="chatbot-labelled-row">Project</dt>
                  <dd><code class="chatbot-code">project-redacted</code></dd>
                  <dt class="chatbot-labelled-row">Lifecycle state</dt>
                  <dd><code class="chatbot-code">Blocked</code></dd>
                  <dt class="chatbot-labelled-row">Safe next actions</dt>
                  <dd><code class="chatbot-code">verify-access</code></dd>
                </dl>
              </article>
            </section>
            """;

    private static string BuildClassificationBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation stream status: current">
              <span class="chatbot-status__label">Info</span>
              <span>Current</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <ol class="chatbot-conversation-stream__list" aria-label="Project conversation stream">
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-email-conversation-item" data-chatbot-conversation-item-kind="EmailDerived" data-chatbot-conversation-item-id="01HZXMAILBOX000000000000011" tabindex="0" aria-label="Mailbox item: Classification review, Associated">
                    <header class="chatbot-email-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Mailbox intake</span><span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox intake">Mailbox intake</span><time class="chatbot-metadata" datetime="2026-06-01T08:24:00.0000000Z">2026-06-01 08:24:00Z</time></header>
                    <section class="chatbot-conversation-classification" aria-label="Classification for item 01HZXMAILBOX000000000000011" data-chatbot-classification="informational">
                      <div class="chatbot-conversation-classification__badge"><span aria-hidden="true">i</span><span>Informational</span><code class="chatbot-code">informational</code></div>
                      <dl class="chatbot-definition-list chatbot-conversation-classification__metadata"><dt class="chatbot-labelled-row">Classification kernel</dt><dd><code class="chatbot-code">classification-deterministic.kernel.m0.v1</code></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">88%</code></dd><dt class="chatbot-labelled-row">Explanation code</dt><dd><code class="chatbot-code">classification_informational_notice</code></dd><dt class="chatbot-labelled-row">Source evidence</dt><dd><code class="chatbot-code">mailbox:subject-offset, mailbox:body-offset</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd></dl>
                    </section>
                    <section class="chatbot-conversation-review-history" aria-label="Review history for item 01HZXMAILBOX000000000000011" aria-live="off">
                      <h3 class="chatbot-conversation-review-history__title">Review history</h3>
                      <ol class="chatbot-conversation-review-history__list">
                        <li class="chatbot-conversation-review-history__entry">
                          <dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Reviewed resource</dt><dd><code class="chatbot-code">email</code>: <code class="chatbot-code">01HZXMAILBOX000000000000011</code></dd><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">association-confirmed</code></dd><dt class="chatbot-labelled-row">Review decision</dt><dd><code class="chatbot-code">confirm</code></dd><dt class="chatbot-labelled-row">Reviewer type</dt><dd><code class="chatbot-code">internal-participant</code></dd><dt class="chatbot-labelled-row">Reviewer</dt><dd><code class="chatbot-code">Internal contributor</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:25:00.0000000Z">2026-06-01 08:25:00Z</time></dd><dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">ui</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000040</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Safe reason code</dt><dd><code class="chatbot-code">association_confirmed</code></dd></dl>
                        </li>
                      </ol>
                    </section>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-ai-outcome-conversation-item" data-chatbot-conversation-item-kind="AiOutcome" data-chatbot-conversation-item-id="ai:proposal-002:outcome-recorded:38" tabindex="0" aria-label="AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:30:00Z">
                    <header class="chatbot-ai-outcome-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span><span class="chatbot-chip chatbot-chip--risk">Task-creating</span><span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span><span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span><time class="chatbot-metadata" datetime="2026-06-01T08:30:00.0000000Z">2026-06-01 08:30:00Z</time></header>
                    <section class="chatbot-conversation-classification" aria-label="Classification for item ai:proposal-002:outcome-recorded:38" data-chatbot-classification="actionable">
                      <div class="chatbot-conversation-classification__badge"><span aria-hidden="true">!</span><span>Actionable</span><code class="chatbot-code">actionable</code></div>
                      <dl class="chatbot-definition-list chatbot-conversation-classification__metadata"><dt class="chatbot-labelled-row">Classification kernel</dt><dd><code class="chatbot-code">classification-deterministic.kernel.m0.v1</code></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">93%</code></dd><dt class="chatbot-labelled-row">Explanation code</dt><dd><code class="chatbot-code">classification_actionable_request</code></dd><dt class="chatbot-labelled-row">Source evidence</dt><dd><code class="chatbot-code">mailbox:body-offset</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd></dl>
                      <dl class="chatbot-definition-list chatbot-conversation-classification__intent"><dt class="chatbot-labelled-row">Detected intent</dt><dd>Approve the renewal request</dd><dt class="chatbot-labelled-row">Detected action kind</dt><dd><span>Request decision</span> <code class="chatbot-code">request-decision</code></dd><dt class="chatbot-labelled-row">Source evidence</dt><dd><code class="chatbot-code">message:offset:001, message:offset:002</code></dd><dt class="chatbot-labelled-row">Explanation code</dt><dd><code class="chatbot-code">task_intent_captured</code></dd><dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">review-task-intent-action</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd></dl>
                    </section>
                    <section class="chatbot-ai-outcome-conversation-item__source-evidence" aria-label="Source evidence" data-chatbot-ai-content="source-evidence"><p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>Source evidence</strong> Source evidence references are governed metadata, separate from AI-generated content.</p></section>
                    <details class="chatbot-ai-outcome-conversation-item__generated" aria-label="AI summary for item ai:proposal-002:outcome-recorded:38" data-chatbot-ai-content="ai-summary">
                      <summary>AI summary</summary>
                      <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI summary</strong> AI-generated content is labelled and kept distinct from source evidence.</p>
                      <dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">AI-generated content visibility</dt><dd><code class="chatbot-code">opt-in</code></dd><dt class="chatbot-labelled-row">AI-generated summary state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">AI summary provenance</dt><dd><code class="chatbot-code">Generated by chatbot-orchestrator+v1 at 2026-06-01 08:30:00Z from ai:evidence-1, ai:evidence-2</code></dd></dl>
                    </details>
                    <p class="chatbot-ai-outcome-conversation-item__reason" tabindex="0"><strong>AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.</strong></p>
                    <section class="chatbot-conversation-review-history" aria-label="Review history for item ai:proposal-002:outcome-recorded:38" aria-live="off">
                      <h3 class="chatbot-conversation-review-history__title">Review history</h3>
                      <ol class="chatbot-conversation-review-history__list">
                        <li class="chatbot-conversation-review-history__entry">
                          <dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Reviewed resource</dt><dd><code class="chatbot-code">ai-action</code>: <code class="chatbot-code">proposal-002</code></dd><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">approval-requested</code></dd><dt class="chatbot-labelled-row">Review decision</dt><dd><code class="chatbot-code">request</code></dd><dt class="chatbot-labelled-row">Reviewer type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:28:00.0000000Z">2026-06-01 08:28:00Z</time></dd><dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">ui</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Safe reason code</dt><dd><code class="chatbot-code">approval_requested</code></dd></dl>
                        </li>
                        <li class="chatbot-conversation-review-history__entry">
                          <dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Reviewed resource</dt><dd><code class="chatbot-code">ai-action</code>: <code class="chatbot-code">proposal-002</code></dd><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">approval-decided</code></dd><dt class="chatbot-labelled-row">Review decision</dt><dd><code class="chatbot-code">approve</code></dd><dt class="chatbot-labelled-row">Reviewer type</dt><dd><code class="chatbot-code">human</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:29:00.0000000Z">2026-06-01 08:29:00Z</time></dd><dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">ui</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Safe reason code</dt><dd><code class="chatbot-code">approval_decided</code></dd></dl>
                        </li>
                      </ol>
                    </section>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-email-conversation-item" data-chatbot-conversation-item-kind="EmailDerived" data-chatbot-conversation-item-id="01HZXMAILBOX000000000000012" tabindex="0" aria-label="Mailbox item: Classification redacted, Associated">
                    <header class="chatbot-email-conversation-item__header"><span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">Mailbox intake</span><span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox intake">Mailbox intake</span><time class="chatbot-metadata" datetime="2026-06-01T08:31:00.0000000Z">2026-06-01 08:31:00Z</time></header>
                    <section class="chatbot-conversation-classification" aria-label="Classification for item 01HZXMAILBOX000000000000012" data-chatbot-classification="informational">
                      <div class="chatbot-conversation-classification__badge"><span aria-hidden="true">i</span><span>Informational</span><code class="chatbot-code">informational</code></div>
                      <dl class="chatbot-definition-list chatbot-conversation-classification__metadata"><dt class="chatbot-labelled-row">Classification kernel</dt><dd><code class="chatbot-code">classification-deterministic.kernel.m0.v1</code></dd><dt class="chatbot-labelled-row">Confidence</dt><dd><code class="chatbot-code">0%</code></dd><dt class="chatbot-labelled-row">Explanation code</dt><dd><code class="chatbot-code">classification_source_redacted</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd></dl>
                    </section>
                  </article>
                </li>
              </ol>
            </section>
            """;

    private static string BuildApprovalDecisionSurfaceBody()
        => """
            <article class="chatbot-approval-conversation-item"
                     data-chatbot-conversation-item-kind="ApprovalEvent"
                     data-chatbot-conversation-item-id="approval:approval-s3:request:42"
                     tabindex="0"
                     aria-label="Approval event, Approval requested, Pending, 2026-06-01 08:09:00Z">
              <header class="chatbot-approval-conversation-item__header">
                <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">evidence:file:requirements</span>
                <span class="chatbot-chip chatbot-chip--risk">approval-required</span>
                <button class="chatbot-chip chatbot-chip--evidence" type="button" data-chatbot-approval-evidence-freshness="fresh" aria-disabled="false" aria-label="evidence:file:requirements, Fresh">
                  <span class="chatbot-chip__label">evidence:file:requirements</span>
                  <span class="chatbot-chip__status">Fresh</span>
                </button>
                <button class="chatbot-chip chatbot-chip--evidence" type="button" data-chatbot-approval-evidence-freshness="stale" aria-disabled="false" aria-label="evidence:file:design, Stale">
                  <span class="chatbot-chip__label">evidence:file:design</span>
                  <span class="chatbot-chip__status">Stale</span>
                </button>
                <button class="chatbot-chip chatbot-chip--evidence" type="button" data-chatbot-approval-evidence-freshness="expired" aria-disabled="true" aria-label="evidence:file:policy, Expired">
                  <span class="chatbot-chip__label">evidence:file:policy</span>
                  <span class="chatbot-chip__status">Expired</span>
                </button>
                <span class="chatbot-approval-conversation-item__status">Pending</span>
                <span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span>
                <time class="chatbot-metadata" datetime="2026-06-01T08:09:00.0000000Z">2026-06-01 08:09:00Z</time>
              </header>
              <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata">
                <dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval requested</span> <code class="chatbot-code">request</code></dd>
                <dt class="chatbot-labelled-row">Approval status</dt><dd><span>Pending</span> <code class="chatbot-code">pending</code></dd>
                <dt class="chatbot-labelled-row">Approval ID</dt><dd><code class="chatbot-code">approval-s3</code></dd>
                <dt class="chatbot-labelled-row">Proposal ID</dt><dd><code class="chatbot-code">proposal-s3</code></dd>
                <dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd>
                <dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">allowlist.v0</code></dd>
                <dt class="chatbot-labelled-row">Risk class</dt><dd><code class="chatbot-code">approval-required</code></dd>
                <dt class="chatbot-labelled-row">Risk action classes</dt><dd><code class="chatbot-code">modifies-state, exposes-files, invokes-tools</code></dd>
                <dt class="chatbot-labelled-row">Risk input tuple</dt><dd><code class="chatbot-code">command=Project.AppendConversationMessage;effect=project-state;authority=project-contributor;policy=approval-required</code></dd>
                <dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">policy-snapshot-s3</code></dd>
                <dt class="chatbot-labelled-row">Evidence references</dt><dd><code class="chatbot-code">evidence:file:requirements, evidence:file:design, evidence:file:policy</code></dd>
                <dt class="chatbot-labelled-row">Evidence freshness</dt><dd><span>Fresh, Stale, Expired</span> <code class="chatbot-code">fresh, stale, expired</code></dd>
                <dt class="chatbot-labelled-row">Recipients</dt><dd><code class="chatbot-code">project:conversation</code></dd>
                <dt class="chatbot-labelled-row">Sender authority</dt><dd><code class="chatbot-code">project-contributor</code></dd>
                <dt class="chatbot-labelled-row">Expected post-state</dt><dd><span>Metadata only</span> <code class="chatbot-code">metadata_only</code></dd>
                <dt class="chatbot-labelled-row">Action summary state</dt><dd><span>Redacted</span> <code class="chatbot-code">redacted</code></dd>
                <dt class="chatbot-labelled-row">Disabled reason</dt><dd><span>Evidence expired</span> <code class="chatbot-code">evidence-expired</code></dd>
              </dl>
              <div class="chatbot-approval-conversation-item__actions" aria-label="Approval decision">
                <button type="button" class="chatbot-action-button chatbot-action-button--primary" aria-disabled="true" aria-describedby="approval-approve-reason" onclick="const status=document.getElementById('approval-decision-status'); status.setAttribute('role','alert'); status.setAttribute('aria-live','assertive'); status.textContent='Evidence expired';">
                  Approved
                </button>
                <button type="button" class="chatbot-action-button" onclick="const status=document.getElementById('approval-decision-status'); status.setAttribute('role','status'); status.setAttribute('aria-live','polite'); status.textContent='Rejected';">
                  Rejected
                </button>
                <button type="button" class="chatbot-action-button" onclick="const status=document.getElementById('approval-decision-status'); status.setAttribute('role','status'); status.setAttribute('aria-live','polite'); status.textContent='Requested revision';">
                  Requested revision
                </button>
                <button type="button" class="chatbot-action-button" onclick="const status=document.getElementById('approval-decision-status'); status.setAttribute('role','status'); status.setAttribute('aria-live','polite'); status.textContent='Cancelled';">
                  Cancelled
                </button>
              </div>
              <p id="approval-approve-reason" class="chatbot-approval-conversation-item__reason" tabindex="0"><strong>Why unavailable?</strong> Evidence expired</p>
              <p id="approval-decision-status"
                 class="chatbot-approval-conversation-item__reason"
                 tabindex="-1"
                 role="status"
                 aria-live="polite"
                 aria-label="Approval decision status"></p>
            </article>
            """;

    private static string BuildCorrectedContextInvalidatedApprovalBody()
        => """
            <script>window.__approvalSubmitCount = 0;</script>
            <section class="chatbot-blocked-state"
                     role="alert"
                     aria-live="assertive"
                     aria-label="Corrected context invalidated: approval approval-corrected-001 is no longer available. Next action: review-source-evidence."
                     data-chatbot-feedback-state="CurrentUserTerminalInvalidation"
                     data-chatbot-refusal-reason="corrected-context-invalidated"
                     data-chatbot-catalog-code="corrected_context_invalidated">
              <strong>Corrected context invalidated</strong>
              <span>approval approval-corrected-001 is no longer available.</span>
              <span>Next action: review-source-evidence.</span>
            </section>
            <article id="corrected-approval-panel"
                     class="chatbot-approval-conversation-item"
                     data-chatbot-conversation-item-kind="ApprovalEvent"
                     data-chatbot-conversation-item-id="approval:approval-corrected-001:invalidated:49"
                     tabindex="0"
                     aria-label="Approval event, Approval requested, Invalidated, 2026-06-01 09:20:00Z">
              <header class="chatbot-approval-conversation-item__header">
                <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">evidence:summary:corrected</span>
                <span class="chatbot-chip chatbot-chip--risk">approval-required</span>
                <span class="chatbot-approval-conversation-item__status">Invalidated</span>
                <span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span>
                <time class="chatbot-metadata" datetime="2026-06-01T09:20:00.0000000Z">2026-06-01 09:20:00Z</time>
              </header>
              <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata">
                <dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval requested</span> <code class="chatbot-code">request</code></dd>
                <dt class="chatbot-labelled-row">Approval status</dt><dd><span>Invalidated</span> <code class="chatbot-code">invalidated</code></dd>
                <dt class="chatbot-labelled-row">Approval ID</dt><dd><code class="chatbot-code">approval-corrected-001</code></dd>
                <dt class="chatbot-labelled-row">Proposal ID</dt><dd><code class="chatbot-code">proposal-corrected-001</code></dd>
                <dt class="chatbot-labelled-row">Failure reason</dt><dd><span>Corrected context invalidated</span> <code class="chatbot-code">corrected-context-invalidated</code></dd>
                <dt class="chatbot-labelled-row">Disabled reason</dt><dd><span>Corrected context invalidated</span> <span lang="fr">Contexte corrigé invalidé</span></dd>
                <dt class="chatbot-labelled-row">Correction ID</dt><dd><code class="chatbot-code">correction-4-9-001</code></dd>
                <dt class="chatbot-labelled-row">Association ID</dt><dd><code class="chatbot-code">association-4-9-001</code></dd>
                <dt class="chatbot-labelled-row">Source version</dt><dd><code class="chatbot-code">12</code></dd>
                <dt class="chatbot-labelled-row">Corrected evidence state</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONCORRECTED0001</code></dd>
                <dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd>
                <dt class="chatbot-labelled-row">Safe next actions</dt><dd><code class="chatbot-code">review-source-evidence</code>, <code class="chatbot-code">retry-later</code></dd>
              </dl>
              <div class="chatbot-approval-conversation-item__actions" aria-label="Approval decision">
                <button type="button"
                        class="chatbot-action-button chatbot-action-button--primary"
                        aria-disabled="true"
                        aria-describedby="corrected-approval-disabled-reason"
                        onclick="event.preventDefault(); const panel=document.getElementById('corrected-approval-panel'); const status=document.getElementById('corrected-approval-status'); status.setAttribute('role','alert'); status.setAttribute('aria-live','assertive'); status.textContent='Corrected context invalidated'; panel.focus();">
                  Approve action
                </button>
              </div>
              <p id="corrected-approval-disabled-reason"
                 class="chatbot-approval-conversation-item__reason"
                 tabindex="0"
                 aria-label="Why unavailable? Corrected context invalidated. Review source evidence before requesting a new AI proposal.">
                <strong>Why unavailable?</strong> Corrected context invalidated. Review source evidence before requesting a new AI proposal.
              </p>
              <p id="corrected-approval-status"
                 class="chatbot-approval-conversation-item__reason"
                 tabindex="-1"
                 role="status"
                 aria-live="polite"
                 aria-label="Approval decision status"></p>
            </article>
            <section class="chatbot-conversation-review-history"
                     aria-label="Historical invalidations"
                     aria-live="off"
                     data-chatbot-feedback-state="ObservedHistoricalInvalidation">
              <h3 class="chatbot-conversation-review-history__title">Historical invalidations</h3>
              <ol class="chatbot-conversation-review-history__list">
                <li class="chatbot-conversation-review-history__entry">
                  <dl class="chatbot-definition-list chatbot-conversation-review-history__metadata">
                    <dt class="chatbot-labelled-row">Reviewed resource</dt><dd><code class="chatbot-code">proposal-corrected-previous</code></dd>
                    <dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">corrected-context-invalidated</code></dd>
                    <dt class="chatbot-labelled-row">Correction ID</dt><dd><code class="chatbot-code">correction-4-9-previous</code></dd>
                    <dt class="chatbot-labelled-row">Safe reason code</dt><dd><code class="chatbot-code">corrected-context-invalidated</code></dd>
                  </dl>
                </li>
              </ol>
            </section>
            """;

    private static string BuildAiActionPreviewInspectionBody()
        => """
            <ol class="chatbot-conversation-stream" aria-label="Project conversation stream" data-chatbot-conversation-stream="metadata-only">
              <li class="chatbot-conversation-stream__entry">
                <article class="chatbot-approval-conversation-item"
                         data-chatbot-conversation-item-kind="ApprovalEvent"
                         data-chatbot-conversation-item-id="approval:approval-preview:request:50"
                         tabindex="0"
                         aria-label="Approval event, Approval requested, Pending, 2026-06-01 08:20:00Z">
                  <header class="chatbot-approval-conversation-item__header">
                    <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">evidence:file:requirements</span>
                    <span class="chatbot-chip chatbot-chip--risk">approval-required</span>
                    <span class="chatbot-approval-conversation-item__status">Pending</span>
                    <span class="chatbot-actor-badge" aria-label="System actor: Approval event">Approval event</span>
                    <time class="chatbot-metadata" datetime="2026-06-01T08:20:00.0000000Z">2026-06-01 08:20:00Z</time>
                  </header>
                  <dl class="chatbot-definition-list chatbot-approval-conversation-item__metadata">
                    <dt class="chatbot-labelled-row">Approval event kind</dt><dd><span>Approval requested</span> <code class="chatbot-code">request</code></dd>
                    <dt class="chatbot-labelled-row">Approval status</dt><dd><span>Pending</span> <code class="chatbot-code">pending</code></dd>
                    <dt class="chatbot-labelled-row">Approval ID</dt><dd><code class="chatbot-code">approval-preview</code></dd>
                    <dt class="chatbot-labelled-row">Proposal ID</dt><dd><code class="chatbot-code">proposal-preview</code></dd>
                    <dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd>
                    <dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-allowlist.m0</code></dd>
                    <dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">policy-snapshot-preview</code></dd>
                    <dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">reconciling</code></dd>
                    <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONPREVIEW00000050</code></dd>
                  </dl>
                  <section class="chatbot-ai-action-preview"
                           data-chatbot-ai-action-preview="metadata-only"
                           aria-label="AI action preview for item approval:approval-preview:request:50">
                    <h3 class="chatbot-ai-action-preview__title">AI action preview</h3>
                    <p class="chatbot-ai-action-preview__reason" tabindex="0">Preview renders metadata only; sensitive generation inputs, provider internals, file content, and hidden evidence never render on this surface.</p>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="outbound" aria-label="Outbound communication" aria-disabled="false" tabindex="0">
                      <h4 class="chatbot-ai-action-preview__section-title">Outbound communication</h4>
                      <dl class="chatbot-definition-list">
                        <dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">allowed</code></dd>
                        <dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">available</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Evidence freshness</dt><dd><code class="chatbot-code">fresh, stale, expired</code></dd>
                        <dt class="chatbot-labelled-row">Recipients or destination</dt><dd><code class="chatbot-code">project:conversation, reviewer:approver-001</code></dd>
                        <dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">reconciling</code></dd>
                        <dt class="chatbot-labelled-row">Expected post-state</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd>
                      </dl>
                    </section>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="file-access" aria-label="File access and context" aria-disabled="true" tabindex="0">
                      <h4 class="chatbot-ai-action-preview__section-title">File access and context</h4>
                      <dl class="chatbot-definition-list">
                        <dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">blocked</code></dd>
                        <dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">not-authorized</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">redacted</code></dd>
                        <dt class="chatbot-labelled-row">Evidence freshness</dt><dd><code class="chatbot-code">fresh, stale, expired</code></dd>
                        <dt class="chatbot-labelled-row">Affected resources</dt><dd><code class="chatbot-code">evidence:file:requirements, evidence:file:design, redacted</code></dd>
                        <dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd>
                      </dl>
                    </section>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="command" aria-label="Command execution" aria-disabled="false" tabindex="0">
                      <h4 class="chatbot-ai-action-preview__section-title">Command execution</h4>
                      <dl class="chatbot-definition-list">
                        <dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">allowed</code></dd>
                        <dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">available</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd>
                        <dt class="chatbot-labelled-row">Command allowlist version</dt><dd><code class="chatbot-code">ai-action-allowlist.m0</code></dd>
                        <dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">reconciling</code></dd>
                        <dt class="chatbot-labelled-row">Expected post-state</dt><dd><code class="chatbot-code">accepted-projection-pending</code></dd>
                        <dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd>
                      </dl>
                    </section>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="generated-changes" aria-label="AI-generated changes" aria-disabled="true" tabindex="0">
                      <h4 class="chatbot-ai-action-preview__section-title">AI-generated changes</h4>
                      <dl class="chatbot-definition-list">
                        <dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">blocked</code></dd>
                        <dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">not-yet-produced</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Generated-content visibility</dt><dd><code class="chatbot-code">not-yet-produced</code></dd>
                        <dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">review-ai-action</code></dd>
                      </dl>
                    </section>
                  </section>
                </article>
              </li>
              <li class="chatbot-conversation-stream__entry">
                <article class="chatbot-ai-outcome-conversation-item"
                         data-chatbot-conversation-item-kind="AiOutcome"
                         data-chatbot-conversation-item-id="ai:proposal-preview:outcome-recorded:51"
                         tabindex="0"
                         aria-label="AI actor, AI outcome recorded, Succeeded, 2026-06-01 08:21:00Z">
                  <header class="chatbot-ai-outcome-conversation-item__header">
                    <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">AI-generated</span>
                    <span class="chatbot-chip chatbot-chip--risk">Tool-invoking</span>
                    <span class="chatbot-ai-outcome-conversation-item__status">Succeeded</span>
                    <span class="chatbot-actor-badge" aria-label="AI actor actor: AI actor">AI actor</span>
                    <time class="chatbot-metadata" datetime="2026-06-01T08:21:00.0000000Z">2026-06-01 08:21:00Z</time>
                  </header>
                  <dl class="chatbot-definition-list chatbot-ai-outcome-conversation-item__metadata">
                    <dt class="chatbot-labelled-row">AI outcome</dt><dd><span>AI outcome recorded</span> <code class="chatbot-code">outcome-recorded</code></dd>
                    <dt class="chatbot-labelled-row">Status</dt><dd><span>Succeeded</span> <code class="chatbot-code">succeeded</code></dd>
                    <dt class="chatbot-labelled-row">Proposal id</dt><dd><code class="chatbot-code">proposal-preview</code></dd>
                    <dt class="chatbot-labelled-row">Approval id</dt><dd><code class="chatbot-code">approval-preview</code></dd>
                    <dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-preview-001</code></dd>
                    <dt class="chatbot-labelled-row">Policy snapshot id</dt><dd><code class="chatbot-code">policy-snapshot-preview</code></dd>
                    <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONPREVIEW00000051</code></dd>
                  </dl>
                  <section class="chatbot-ai-action-preview"
                           data-chatbot-ai-action-preview="metadata-only"
                           aria-label="AI action preview for item ai:proposal-preview:outcome-recorded:51">
                    <h3 class="chatbot-ai-action-preview__title">AI action preview</h3>
                    <p class="chatbot-ai-action-preview__reason" tabindex="0">Preview renders metadata only; sensitive generation inputs, provider internals, file content, and hidden evidence never render on this surface.</p>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="outbound" aria-label="Outbound communication" aria-disabled="false" tabindex="0"><h4 class="chatbot-ai-action-preview__section-title">Outbound communication</h4><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">allowed</code></dd><dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Recipients or destination</dt><dd><code class="chatbot-code">project:conversation</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd></dl></section>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="file-access" aria-label="File access and context" aria-disabled="true" tabindex="0"><h4 class="chatbot-ai-action-preview__section-title">File access and context</h4><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">blocked</code></dd><dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">evidence-expired</code></dd><dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">redacted</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd></dl></section>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="command" aria-label="Command execution" aria-disabled="false" tabindex="0"><h4 class="chatbot-ai-action-preview__section-title">Command execution</h4><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">allowed</code></dd><dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Command name</dt><dd><code class="chatbot-code">Project.AppendConversationMessage</code></dd><dt class="chatbot-labelled-row">Audit status</dt><dd><code class="chatbot-code">committed</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd></dl></section>
                    <section class="chatbot-ai-action-preview__section" data-chatbot-ai-action-preview-section="generated-changes" aria-label="AI-generated changes" aria-disabled="false" tabindex="0"><h4 class="chatbot-ai-action-preview__section-title">AI-generated changes</h4><dl class="chatbot-definition-list"><dt class="chatbot-labelled-row">Preview state</dt><dd><code class="chatbot-code">allowed</code></dd><dt class="chatbot-labelled-row">Reason code</dt><dd><code class="chatbot-code">available</code></dd><dt class="chatbot-labelled-row">Generated-content visibility</dt><dd><code class="chatbot-code">metadata_only</code></dd><dt class="chatbot-labelled-row">Safe next action</dt><dd><code class="chatbot-code">none</code></dd></dl></section>
                  </section>
                  <section class="chatbot-conversation-review-history" aria-label="Review history for item ai:proposal-preview:outcome-recorded:51" aria-live="off">
                    <h3 class="chatbot-conversation-review-history__title">Review history</h3>
                    <ol class="chatbot-conversation-review-history__list">
                      <li class="chatbot-conversation-review-history__entry"><dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Reviewed resource</dt><dd><code class="chatbot-code">ai-action</code>: <code class="chatbot-code">proposal-preview</code></dd><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">proposal</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:20:00.0000000Z">2026-06-01 08:20:00Z</time></dd></dl></li>
                      <li class="chatbot-conversation-review-history__entry"><dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">approval-requested</code></dd><dt class="chatbot-labelled-row">Approval id</dt><dd><code class="chatbot-code">approval-preview</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:20:10.0000000Z">2026-06-01 08:20:10Z</time></dd></dl></li>
                      <li class="chatbot-conversation-review-history__entry"><dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">approval-decided</code></dd><dt class="chatbot-labelled-row">Review decision</dt><dd><code class="chatbot-code">approved</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:20:20.0000000Z">2026-06-01 08:20:20Z</time></dd></dl></li>
                      <li class="chatbot-conversation-review-history__entry"><dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">execution-started</code></dd><dt class="chatbot-labelled-row">Operation ID</dt><dd><code class="chatbot-code">operation-preview-001</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:20:30.0000000Z">2026-06-01 08:20:30Z</time></dd></dl></li>
                      <li class="chatbot-conversation-review-history__entry"><dl class="chatbot-definition-list chatbot-conversation-review-history__metadata"><dt class="chatbot-labelled-row">Review action</dt><dd><code class="chatbot-code">outcome-recorded</code></dd><dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATIONPREVIEW00000051</code></dd><dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">policy-snapshot-preview</code></dd><dt class="chatbot-labelled-row">Supersession link</dt><dd><code class="chatbot-code">superseded-by-none</code></dd><dt class="chatbot-labelled-row">Reviewed at</dt><dd><time class="chatbot-code" datetime="2026-06-01T08:21:00.0000000Z">2026-06-01 08:21:00Z</time></dd></dl></li>
                    </ol>
                  </section>
                </article>
              </li>
            </ol>
            """;

    private static string BuildTaskIntentReviewBody()
        => """
            <section class="chatbot-task-intent-review-panel"
                     aria-label="Task intent review"
                     data-chatbot-task-intent-id="task-intent:review-001">
              <header class="chatbot-task-intent-review-panel__header">
                <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">task-intent:review-001</span>
                <span class="chatbot-task-intent-review-panel__state">captured</span>
                <span class="chatbot-task-intent-review-panel__reason">task_intent_captured</span>
              </header>
              <dl class="chatbot-definition-list">
                <dt class="chatbot-labelled-row">Project</dt>
                <dd><code class="chatbot-code">project-alpha</code></dd>
                <dt class="chatbot-labelled-row">Detected intent</dt>
                <dd>Create a follow-up task for the renewal</dd>
                <dt class="chatbot-labelled-row">Detected action kind</dt>
                <dd><code class="chatbot-code">request-action</code></dd>
                <dt class="chatbot-labelled-row">Source evidence</dt>
                <dd><code class="chatbot-code">message:offset:001, message:offset:002</code></dd>
                <dt class="chatbot-labelled-row">Correction readiness</dt>
                <dd><code class="chatbot-code">ready</code></dd>
                <dt class="chatbot-labelled-row">Current state</dt>
                <dd><code class="chatbot-code">captured</code></dd>
                <dt class="chatbot-labelled-row">Source version</dt>
                <dd><code class="chatbot-code">8</code></dd>
                <dt class="chatbot-labelled-row">Correlation</dt>
                <dd><code class="chatbot-code">01HZXTASKREVIEW000000000001</code></dd>
              </dl>
              <section class="chatbot-task-intent-review-panel__source"
                       aria-label="Source message">
                <pre tabindex="0">Please create a governed follow-up task for the renewal and keep the evidence attached.</pre>
              </section>
              <section aria-label="Available transitions">
                <div class="chatbot-task-intent-review-panel__actions"
                     role="toolbar"
                     aria-label="Task intent actions">
                  <button type="button"
                          class="chatbot-governed-action"
                          onclick="document.getElementById('task-intent-review-status').textContent='convert';">
                    Convert to AI action
                  </button>
                  <button type="button"
                          class="chatbot-governed-action"
                          onclick="document.getElementById('task-intent-review-status').textContent='not-actionable';">
                    Not actionable
                  </button>
                  <button type="button"
                          class="chatbot-governed-action"
                          onclick="const input=document.getElementById('task-intent-predecessor'); const alert=document.getElementById('task-intent-duplicate-alert'); const status=document.getElementById('task-intent-review-status'); if(!input.value.trim()){ alert.hidden=false; input.setAttribute('aria-invalid','true'); status.textContent='predecessor_task_intent_required'; } else { alert.hidden=true; input.setAttribute('aria-invalid','false'); status.textContent='duplicate'; }">
                    Duplicate
                  </button>
                  <button type="button"
                          class="chatbot-governed-action"
                          onclick="document.getElementById('task-intent-review-status').textContent='already-handled';">
                    Already handled
                  </button>
                  <button type="button"
                          class="chatbot-governed-action"
                          onclick="document.getElementById('task-intent-review-status').textContent='out-of-scope';">
                    Out of scope
                  </button>
                  <button type="button"
                          class="chatbot-governed-action"
                          aria-disabled="true"
                          aria-describedby="task-intent-review-policy-blocked-reason"
                          disabled>
                    Policy blocked
                  </button>
                  <span id="task-intent-review-policy-blocked-reason"
                        class="chatbot-task-intent-review-panel__disabled-reason"
                        tabindex="0">
                    task_intent_policy_blocked
                  </span>
                </div>
              </section>
              <div class="chatbot-task-intent-review-panel__duplicate">
                <label for="task-intent-predecessor">Predecessor task intent</label>
                <input id="task-intent-predecessor"
                       aria-invalid="false"
                       oninput="document.getElementById('task-intent-duplicate-alert').hidden=true; this.setAttribute('aria-invalid','false');" />
                <p id="task-intent-duplicate-alert" role="alert" hidden>predecessor_task_intent_required</p>
              </div>
              <section aria-label="Audit history">
                <dl class="chatbot-definition-list">
                  <dt class="chatbot-labelled-row">Audit operation</dt>
                  <dd><code class="chatbot-code">audit-transition-001</code></dd>
                  <dt class="chatbot-labelled-row">Reviewer actor</dt>
                  <dd><code class="chatbot-code">actor-alpha</code></dd>
                  <dt class="chatbot-labelled-row">Decided timestamp</dt>
                  <dd><time class="chatbot-code" datetime="2026-06-01T08:40:00.0000000Z">2026-06-01 08:40:00Z</time></dd>
                </dl>
              </section>
              <div id="task-intent-review-status"
                   class="chatbot-task-intent-review-panel__status"
                   role="status"
                   aria-live="polite"
                   aria-label="Task intent transition status"></div>
            </section>
            <section class="chatbot-task-intent-review-panel"
                     aria-label="Task intent review unavailable">
              <header class="chatbot-task-intent-review-panel__header">
                <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Unavailable">Review unavailable</span>
                <span class="chatbot-task-intent-review-panel__reason">task_intent_source_unavailable</span>
              </header>
              <dl class="chatbot-definition-list">
                <dt class="chatbot-labelled-row">Catalog code</dt>
                <dd><code class="chatbot-code">safe-not-found</code></dd>
                <dt class="chatbot-labelled-row">Safe next action</dt>
                <dd><code class="chatbot-code">verify-access</code></dd>
                <dt class="chatbot-labelled-row">Redaction state</dt>
                <dd><code class="chatbot-code">unavailable</code></dd>
              </dl>
            </section>
            """;

    private static void AssertLoadingWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Loading);
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");

        page.ShouldContain("<ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotUiTextKey.ProjectConversationLoading");
        fixture.ShouldContain("aria-label=\"Project conversation loading\"");
        fixture.ShouldContain("aria-label=\"Project context\"");
        fixture.ShouldContain("tenant-alpha");
    }

    private static void AssertPopulatedWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");
        string item = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor");
        string decision = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor");
        string participant = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor");
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string failure = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor");

        stream.ShouldContain("data-chatbot-conversation-stream=\"metadata-only\"");
        stream.ShouldContain("ChatBotParticipantConversationItem");
        stream.ShouldContain("ChatBotAttachmentConversationItem");
        stream.ShouldContain("ChatBotDecisionConversationItem");
        stream.ShouldContain("ChatBotApprovalConversationItem");
        stream.ShouldContain("ChatBotFailureStateConversationItem");
        stream.ShouldContain("ChatBotAiOutcomeConversationItem");
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");
        aiOutcome.ShouldContain("AiOutcomeAccessible");
        aiOutcome.ShouldContain("AiOutcomeKindLabel");
        aiOutcome.ShouldContain("AiOutcomeStatusLabel");
        aiOutcome.ShouldContain("AiOutcomeGeneratedLabel");
        aiOutcome.ShouldContain("AiOutcomeSourceEvidenceLabel");
        aiOutcome.ShouldContain("AiOutcomeMetadataOnlyReason");
        item.ShouldContain("ProjectConversationSystemDecision");
        decision.ShouldContain("ProjectConversationDecisionItemAccessible");
        decision.ShouldContain("DecisionKindLabel");
        decision.ShouldContain("CorrectionKindLabel");
        participant.ShouldContain("ProjectConversationParticipantItemAccessible");
        attachment.ShouldContain("ProjectConversationAttachmentItemAccessible");
        approval.ShouldContain("ApprovalEventAccessible");
        failure.ShouldContain("FailureStateAccessible");
        failure.ShouldContain("FailureCatalogHeadline");
        failure.ShouldContain("FailureCatalogReason");
        failure.ShouldContain("FailureDuplicateSafetyReason");
        failure.ShouldContain("FailureTerminalRuleReason");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"01HZXMAILBOX000000000000001\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"decision:01HZXASSOC000000000000001:3\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"decision:01HZXASSOC000000000000001:9\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"attachment:01HZXASSOC000000000000001:0:826F\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-001:request:10\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-001:decision:11\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-001:outcome:12\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-001:outcome:13\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-002:outcome:14\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-002:decision:15\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-003:decision:16\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"approval:approval-004:decision:17\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-001:retry-queued:18\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-001:retry-accepted:19\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-001:duplicate-suppressed:20\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-002:dependency-degraded:21\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-003:projection-retryable:22\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-004:blocked:23\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-005:blocked:24\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-001:retry-exhausted:25\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-001:terminal-failure:26\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"failure:operation-001:reprocess-created:27\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:proposal:30\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:denial:31\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:refusal:32\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:execution-started:33\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:execution-succeeded:34\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:execution-failed:35\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:outcome-recorded:36\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:proposal-001:corrected-context-invalidated:37\"");
        fixture.ShouldContain("AI-generated content is labelled and kept distinct from source evidence.");
        fixture.ShouldContain("Source evidence references are governed metadata, separate from AI-generated content.");
        fixture.ShouldContain("AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.");
        fixture.ShouldNotContain("ignore previous instructions");
        fixture.ShouldContain("request-revision");
        fixture.ShouldContain("insufficient-authority");
        fixture.ShouldContain("Revision requested");
        fixture.ShouldContain("Cancelled");
        fixture.ShouldContain("Retry queued");
        fixture.ShouldContain("Retry accepted");
        fixture.ShouldContain("Retry exhausted");
        fixture.ShouldContain("Duplicate suppressed");
        fixture.ShouldContain("Dependency degraded");
        fixture.ShouldContain("Projection retryable");
        fixture.ShouldContain("Refused action");
        fixture.ShouldContain("Audit unavailable");
        fixture.ShouldContain("Terminal failure");
        fixture.ShouldContain("Reprocess created");
        fixture.ShouldContain("retry_queued");
        fixture.ShouldContain("retry_accepted");
        fixture.ShouldContain("retry_exhausted");
        fixture.ShouldContain("duplicate_suppressed");
        fixture.ShouldContain("dependency_degraded");
        fixture.ShouldContain("projection_retryable");
        fixture.ShouldContain("refusal_blocked_action");
        fixture.ShouldContain("audit_unavailable");
        fixture.ShouldContain("terminal_failure");
        fixture.ShouldContain("reprocess_created");
        fixture.ShouldContain("policy-blocked");
        fixture.ShouldContain("wait-for-dependency");
        fixture.ShouldContain("Duplicate safety");
        fixture.ShouldContain("Retries and duplicate suppression use governed metadata and do not replace prior history.");
        fixture.ShouldContain("Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"attachment:01HZXASSOC000000000000001:1:4A1B\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"attachment:01HZXASSOC000000000000001:2:9F20\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"attachment:01HZXASSOC000000000000001:3:D4C2\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"attachment:01HZXASSOC000000000000001:4:70EA\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"attachment:01HZXASSOC000000000000001:5:13B7\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"participant:01HZXRESOLUTION00000000001:01HZXPARTICIPANT000000001\"");
        fixture.ShouldContain("Mailbox attachment");
        fixture.ShouldContain("Provider attachment ID");
        fixture.ShouldContain("graph-attachment-001");
        fixture.ShouldContain("release-notes.pdf");
        fixture.ShouldContain("Attachment unavailable");
        fixture.ShouldContain("Redacted attachment");
        fixture.ShouldContain("duplicate-provider-attachment-suppressed");
        fixture.ShouldContain("retryable-after-policy-window");
        fixture.ShouldContain("blocked-unsafe");
        fixture.ShouldContain("Capture status");
        fixture.ShouldContain("Storage status");
        fixture.ShouldContain("Scan status");
        fixture.ShouldContain("Internal participant");
        fixture.ShouldContain("External participant");
        fixture.ShouldContain("Unresolved participant");
        fixture.ShouldContain("Restricted participant");
        fixture.ShouldContain("Allowed review actions");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("Source");
        fixture.ShouldContain("Microsoft 365 mailbox");
        fixture.ShouldContain("Mailbox");
        fixture.ShouldContain("controlled-mailbox-001");
        fixture.ShouldContain("Provider message ID");
        fixture.ShouldContain("graph-message-001");
        fixture.ShouldContain("Internet message ID");
        fixture.ShouldContain("&lt;internet-message-001@example.test&gt;");
        fixture.ShouldContain("Thread");
        fixture.ShouldContain("graph-thread-001");
        fixture.ShouldContain("Sent");
        fixture.ShouldContain("Created");
        fixture.ShouldContain("Source timezone");
        fixture.ShouldContain("Correlation ID");
        fixture.ShouldContain("Threshold band");
        fixture.ShouldContain("Confirmed association");
        fixture.ShouldContain("Rejected association");
        fixture.ShouldContain("Deferred association");
        fixture.ShouldContain("Needs review");
        fixture.ShouldContain("Project reassignment");
        fixture.ShouldContain("Correction-delayed");
        fixture.ShouldContain("Correcting");
        fixture.ShouldContain("Corrected");
        fixture.ShouldContain("Decision actor");
        fixture.ShouldContain("Decision actor type");
        fixture.ShouldContain("Decision note state");
        fixture.ShouldContain("Correction actor");
        fixture.ShouldContain("Correction actor type");
        fixture.ShouldContain("Correction rationale state");
        fixture.ShouldContain("Prior project");
        fixture.ShouldContain("Corrected project");
        fixture.ShouldContain("Predecessor association");
        fixture.ShouldContain("Supersedes association");
        fixture.ShouldContain("Superseded by association");
        fixture.ShouldContain("Downstream impact status");
        fixture.ShouldContain("Workflow instance");
        fixture.ShouldContain("Propagation progress");
        fixture.ShouldContain("Propagation started");
        fixture.ShouldContain("Propagation estimated completion");
        fixture.ShouldContain("Propagation completed");
        fixture.ShouldContain("Corrected context stale");
        fixture.ShouldContain("Responsible owner role");
        fixture.ShouldContain("Retention class");
        fixture.ShouldContain("Schema version");
        fixture.ShouldContain("Source version");
        fixture.ShouldContain("<span>Unavailable</span> <code class=\"chatbot-code\">unavailable</code>");
        fixture.ShouldContain("Decision detail is unavailable on this surface.");
        fixture.ShouldContain("Approval requested");
        fixture.ShouldContain("Approval decision");
        fixture.ShouldContain("Approval outcome");
        fixture.ShouldContain("Evidence freshness");
        fixture.ShouldContain("expired");
        fixture.ShouldContain("accepted-projection-pending");
        fixture.ShouldContain("Policy snapshot detail is redacted or unavailable on this surface.");
        fixture.ShouldContain("Audit detail is unavailable on this surface.");
        fixture.ShouldNotContain("Done");
        AssertTextOrder(
            fixture,
            "Source",
            "Microsoft 365 mailbox",
            "Mailbox",
            "controlled-mailbox-001",
            "Provider message ID",
            "graph-message-001",
            "Internet message ID",
            "&lt;internet-message-001@example.test&gt;",
            "Operation",
            "01HZXASSOC000000000000001",
            "Conversation context",
            "graph-conversation-001",
            "Thread",
            "graph-thread-001",
            "Project",
            "project-alpha",
            "Lifecycle state",
            "Associated",
            "Confidence",
            "91%",
            "Threshold band",
            "Auto",
            "Safe next actions",
            "none",
            "Received",
            "2026-06-01 08:00:00Z",
            "Sent",
            "2026-06-01 07:58:00Z",
            "Created",
            "2026-06-01 07:57:00Z",
            "Source timezone",
            "UTC",
            "Correlation ID",
            "01HZXCORRELATION00000000001",
            "m365-mailbox-intake",
            "metadata_only",
            "91%");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertAttachmentCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");

        AssertTextOrder(
            attachment,
            "<ChatBotEvidenceChip State=\"@EvidenceState\"",
            "<span class=\"chatbot-attachment-conversation-item__status\">@LocalizedStatus</span>",
            "<ChatBotActorBadge",
            "<time");
        attachment.ShouldContain("AttachmentDisplayNameLabel");
        attachment.ShouldContain("AttachmentDuplicateStateLabel");
        attachment.ShouldContain("AttachmentRetryStateLabel");
        attachment.ShouldContain("AttachmentAiEligibilityLabel");
        attachment.ShouldContain("AttachmentRedactedReason");
        attachment.ShouldContain("AttachmentUnavailableReason");
        attachment.ShouldContain("AttachmentActionsUnavailableReason");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, invoice.pdf, Pending, Associated\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, release-notes.pdf, Captured, Associated\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, Attachment unavailable, Unavailable, Associated\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, Redacted attachment, Pending, Associated\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, duplicate-invoice.pdf, Retryable, Associated\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, Attachment unavailable, Unsafe, Associated\"");
        fixture.ShouldContain("File reference");
        fixture.ShouldContain("Folder reference");
        fixture.ShouldContain("Open governed file, Add to AI context");
        fixture.ShouldContain("Attachment metadata is unavailable on this surface.");
        fixture.ShouldContain("Attachment metadata is redacted by policy.");
        fixture.ShouldContain("Retry capture");
        fixture.ShouldNotContain("restricted-quarterly-plan.xlsx", Case.Insensitive);
        fixture.ShouldNotContain("unsafe-malware-sample.exe", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertStoredAttachmentCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");

        attachment.ShouldContain("AttachmentFileReferenceLabel");
        attachment.ShouldContain("AttachmentFolderReferenceLabel");
        attachment.ShouldContain("!string.IsNullOrWhiteSpace(Item.AttachmentFileId)");
        attachment.ShouldContain("!string.IsNullOrWhiteSpace(Item.AttachmentFolderId)");
        fixture.ShouldContain("<dt class=\"chatbot-labelled-row\">File reference</dt>");
        fixture.ShouldContain("<dd><code class=\"chatbot-code\">file-reference-001</code></dd>");
        fixture.ShouldContain("<dt class=\"chatbot-labelled-row\">Folder reference</dt>");
        fixture.ShouldContain("<dd><code class=\"chatbot-code\">folder-reference-001</code></dd>");
        attachment.ShouldNotContain("<button", Case.Insensitive);
        attachment.ShouldNotContain("<a ", Case.Insensitive);
        attachment.ShouldNotContain("href=", Case.Insensitive);
        attachment.ShouldNotContain("download", Case.Insensitive);
        attachment.ShouldNotContain("/api/v1/folders", Case.Insensitive);
        attachment.ShouldNotContain("/api/v1/files", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertParticipantCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string participant = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor");

        AssertTextOrder(
            participant,
            "<ChatBotEvidenceChip State=\"@EvidenceState\"",
            "<span class=\"chatbot-participant-conversation-item__status\">@LocalizedParticipantStatus</span>",
            "<ChatBotActorBadge",
            "<time");
        participant.ShouldContain("ParticipantResolutionLabel");
        participant.ShouldContain("SourceParticipantLabel");
        participant.ShouldContain("ParticipantAllowedReviewActionsLabel");
        participant.ShouldContain("WhyUnavailable");
        fixture.ShouldContain("Participant resolution");
        fixture.ShouldContain("Source participant");
        fixture.ShouldContain("Allowed review actions");
        fixture.ShouldContain("Participant detail is unavailable: Participant not found");
        fixture.ShouldContain("Participant detail is unavailable: Restricted party");
        fixture.ShouldNotContain("provider display name", Case.Insensitive);
        fixture.ShouldNotContain("raw email address evidence", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertAiOutcomeCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");

        AssertTextOrder(
            aiOutcome,
            "<ChatBotEvidenceChip State=\"ChatBotEvidenceState.Available\"",
            "<ChatBotRiskChip",
            "<span class=\"chatbot-ai-outcome-conversation-item__status\">",
            "<ChatBotActorBadge",
            "<time");
        // Story 3.11 reworked the posture: source evidence is the default section and the AI-authored
        // summary is the opt-in, labelled disclosure (no "ai-generated" default section remains).
        AssertTextOrder(
            aiOutcome,
            "data-chatbot-ai-content=\"source-evidence\"",
            "<details",
            "data-chatbot-ai-content=\"ai-summary\"");
        aiOutcome.ShouldNotContain("data-chatbot-ai-content=\"ai-generated\"");
        aiOutcome.ShouldContain("AiOutcomeMetadataOnlyReason");
        aiOutcome.ShouldContain("AiOutcomeAuditUnavailableReason");
        aiOutcome.ShouldContain("private ChatBotRiskActionClass RiskActionClass");
        aiOutcome.ShouldNotContain("RiskClass=\"ChatBotRiskActionClass.ToolInvoking\"");
        fixture.ShouldContain("aria-label=\"AI actor, AI proposal, Proposed, 2026-06-01 08:20:00Z\"");
        fixture.ShouldContain("Risk class</dt><dd><code class=\"chatbot-code\">approval-required</code>");
        fixture.ShouldContain("Risk action classes</dt><dd><code class=\"chatbot-code\">invokes-tools</code>");
        fixture.ShouldContain("Policy reason</dt><dd><code class=\"chatbot-code\">policy_requires_approval</code>");
        fixture.ShouldContain("Classifier version</dt><dd><code class=\"chatbot-code\">ai-action-risk-classifier.m0.v1</code>");
        fixture.ShouldContain("Risk input tuple</dt><dd><code class=\"chatbot-code\">command=Project.AppendConversationMessage;effect=project-state;authority=project-contributor;policy=approval-required</code>");
        fixture.ShouldContain("Requester authority</dt><dd><code class=\"chatbot-code\">project-contributor</code>");
        fixture.ShouldContain("Policy snapshot id</dt><dd><code class=\"chatbot-code\">policy-snapshot-4-3</code>");
        fixture.ShouldContain("Command name</dt><dd><code class=\"chatbot-code\">Project.AppendConversationMessage</code>");
        fixture.ShouldContain("Command allowlist version</dt><dd><code class=\"chatbot-code\">ai-action-allowlist.m0</code>");
        fixture.ShouldContain("aria-label=\"AI actor, AI denial, Denied, 2026-06-01 08:20:10Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, AI refusal, Blocked, 2026-06-01 08:20:20Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, Corrected context invalidated, Invalidated, 2026-06-01 08:21:10Z\"");
        fixture.ShouldContain("AI-generated content is labelled and kept distinct from source evidence.");
        fixture.ShouldContain("Source evidence references are governed metadata, separate from AI-generated content.");
        fixture.ShouldContain("AI outcomes render as governed metadata only; generated content and provider internals are never shown on this surface.");
        fixture.ShouldNotContain("anonymous chat", Case.Insensitive);
        fixture.ShouldNotContain("model-authored source evidence", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertLowRiskAiExecutionCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.LowRiskAiExecution);
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");

        fixture.ShouldContain("aria-label=\"AI actor, AI execution started, Executing, 2026-06-01 08:20:30Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, AI execution succeeded, Succeeded, 2026-06-01 08:20:40Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, Approval-linked AI action, Pending approval, 2026-06-01 08:20:45Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z\"");
        fixture.ShouldContain("Risk class</dt><dd><code class=\"chatbot-code\">low-risk</code>");
        fixture.ShouldContain("Policy reason</dt><dd><code class=\"chatbot-code\">low-risk-execute-allowed</code>");
        fixture.ShouldContain("Policy reason</dt><dd><code class=\"chatbot-code\">low_risk_policy_false</code>");
        fixture.ShouldContain("Context package id</dt><dd><code class=\"chatbot-code\">context-package-001</code>");
        fixture.ShouldContain("Authorized context references</dt><dd><code class=\"chatbot-code\">evidence-message-001, evidence-attachment-001</code>");
        fixture.ShouldContain("Excluded context reasons</dt><dd><code class=\"chatbot-code\">redacted, policy-denied</code>");
        fixture.ShouldContain("Execution outcome code</dt><dd><code class=\"chatbot-code\">low-risk-assistance-generated</code>");
        fixture.ShouldContain("Execution outcome code</dt><dd><code class=\"chatbot-code\">ai_provider_disabled</code>");
        fixture.ShouldContain("Failure code</dt><dd><code class=\"chatbot-code\">ai_provider_disabled</code>");
        fixture.ShouldContain("Safe next action</dt><dd><code class=\"chatbot-code\">none</code>");
        fixture.ShouldContain("Safe next action</dt><dd><code class=\"chatbot-code\">review-ai-action</code>");
        fixture.ShouldContain("Generated by deterministic-test+test-model-v1 at 2026-06-01 08:20:40Z from evidence-message-001, evidence-attachment-001");
        AssertTextOrder(
            fixture,
            "data-chatbot-conversation-item-id=\"ai:ai-execution-001:execution-succeeded:71\"",
            "data-chatbot-ai-content=\"source-evidence\"",
            "<details class=\"chatbot-ai-outcome-conversation-item__generated\"",
            "data-chatbot-ai-content=\"ai-summary\"");
        aiOutcome.ShouldContain("AiOutcomeContextPackageLabel");
        aiOutcome.ShouldContain("AiOutcomeAuthorizedContextLabel");
        aiOutcome.ShouldContain("AiSummaryProvenanceLabel");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertApprovedAiActionExecutionCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.ApprovedAiActionExecution);
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");
        string statusSummary = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        aiOutcome.ShouldContain("AiOutcomeCommandNameLabel");
        aiOutcome.ShouldContain("AiOutcomeCommandAllowlistVersionLabel");
        aiOutcome.ShouldContain("AiOutcomeApprovalIdLabel");
        aiOutcome.ShouldContain("AiOutcomeGeneratedContentVisibilityLabel");
        statusSummary.ShouldContain("StatusSummaryDuplicateSafetyLabel");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:approved-execution-001:execution-started:80\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:approved-execution-001:execution-succeeded:81\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:approved-execution-002:execution-failed:83\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"ai:approved-execution-001:outcome-recorded:84\"");
        fixture.ShouldContain("Project.AppendConversationMessage");
        fixture.ShouldContain("ai-action-command-allowlist.m0");
        fixture.ShouldContain("approval-approved-001");
        fixture.ShouldContain("approval-approved-002");
        fixture.ShouldContain("dependency_unavailable");
        fixture.ShouldContain("duplicate-safe");
        fixture.ShouldContain("metadata_only");
        fixture.ShouldNotContain("Project.SendEmail", Case.Insensitive);
        AssertTextOrder(
            fixture,
            "AI execution started",
            "execution-started",
            "Project.AppendConversationMessage",
            "ai-action-command-allowlist.m0",
            "approval-approved-001",
            "wait-for-command-outcome",
            "AI execution succeeded",
            "approved-ai-action-executed",
            "metadata_only",
            "AI execution failed",
            "dependency_unavailable",
            "retryable",
            "duplicate-safe",
            "AI outcome recorded",
            "outcome-recorded");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertWhyProjectPanelCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string panel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor");
        string email = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor");
        string decision = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        panel.ShouldContain("data-chatbot-why-project-panel=\"metadata-only\"");
        panel.ShouldContain("WhyProjectPanelAccessible");
        panel.ShouldContain("WhyProjectEvidenceRedactedExplanation");
        panel.ShouldContain("SupersedingCorrection");
        email.ShouldContain("WhyProjectOpenAction");
        decision.ShouldContain("WhyProjectOpenAction");
        css.ShouldContain(".chatbot-why-project-panel");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        fixture.ShouldContain("aria-label=\"Available evidence: Why this project\"");
        fixture.ShouldContain("aria-label=\"Why this project evidence for association 01HZXASSOC000000000000001\"");
        fixture.ShouldContain("data-chatbot-evidence-visibility=\"redacted\"");
        fixture.ShouldContain("Open superseding correction correction-002");
        fixture.ShouldContain("aria-label=\"Why this project evidence for association 01HZXASSOC000000000000002\"");
        fixture.ShouldContain("Some evidence detail is redacted or unavailable for this user.");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertPopulatedAccessibilityModesWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain(".chatbot-email-conversation-item");
        css.ShouldContain(".chatbot-participant-conversation-item");
        css.ShouldContain(".chatbot-attachment-conversation-item");
        css.ShouldContain(".chatbot-decision-conversation-item");
        css.ShouldContain(".chatbot-decision-conversation-item__reason");
        css.ShouldContain(".chatbot-failure-conversation-item");
        css.ShouldContain(".chatbot-failure-conversation-item__header");
        css.ShouldContain(".chatbot-failure-conversation-item__reason");
        css.ShouldContain(".chatbot-ai-outcome-conversation-item");
        css.ShouldContain(".chatbot-ai-outcome-conversation-item__header");
        css.ShouldContain(".chatbot-ai-outcome-conversation-item__reason");
        css.ShouldContain(".chatbot-why-project-panel");
        css.ShouldContain("animation: none !important;");
        css.ShouldContain("transition-duration: 0.01ms !important;");
        css.ShouldContain(".chatbot-email-conversation-item__header");
        css.ShouldContain(".chatbot-decision-conversation-item__header");
        css.ShouldContain("flex-direction: column;");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("aria-label=\"Mailbox item: Mailbox intake, Associated\"");
        fixture.ShouldContain("aria-label=\"System decision, Needs review, NeedsReview, 2026-06-01 08:05:00Z\"");
        fixture.ShouldContain("aria-label=\"System status, Retry queued, Retryable, 2026-06-01 08:17:00Z\"");
        fixture.ShouldContain("aria-label=\"System status, Dependency degraded, Degraded, 2026-06-01 08:17:30Z\"");
        fixture.ShouldContain("aria-label=\"System status, Audit unavailable, Blocked, 2026-06-01 08:17:55Z\"");
        fixture.ShouldContain("aria-label=\"AI actor, AI execution failed, Failed, 2026-06-01 08:20:50Z\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, invoice.pdf, Pending, Associated\"");
        fixture.ShouldContain("aria-label=\"Project conversation metadata\"");
    }

    private static void AssertStatusSummaryCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string statusSummary = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor");
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string failure = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        approval.ShouldContain("ChatBotConversationItemStatusSummary");
        failure.ShouldContain("ChatBotConversationItemStatusSummary");
        statusSummary.ShouldContain("data-chatbot-status-domain");
        statusSummary.ShouldContain("data-chatbot-health");
        statusSummary.ShouldContain("StatusSummaryPartialSuccess");
        statusSummary.ShouldContain("aria-live=\"@LiveRegionMode(facet)\"");
        statusSummary.ShouldContain("ChatBotAnnouncementDeduplicationState");
        statusSummary.ShouldContain("OncePerStableOperationKey");
        statusSummary.ShouldContain("data-chatbot-live-announced");
        css.ShouldContain(".chatbot-conversation-status-summary");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        fixture.ShouldContain("aria-label=\"Status summary for item approval:approval-001:outcome:12\"");
        fixture.ShouldContain("aria-label=\"Status summary for item failure:operation-001:retry-queued:18\"");
        AssertTextOrder(
            fixture,
            "Status summary for item approval:approval-001:outcome:12",
            "association",
            "attachment",
            "task",
            "approval",
            "command",
            "failure",
            "retry",
            "next-action");
        fixture.ShouldContain("Accepted; projection is pending.");
        fixture.ShouldContain("wait-for-projection");
        fixture.ShouldContain("operation-approval-001");
        fixture.ShouldContain("duplicate-safe");
        fixture.ShouldContain("Retry later when the governed dependency recovers.");
        fixture.ShouldNotContain("raw command payload", Case.Insensitive);
        fixture.ShouldNotContain("Done", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertClassificationCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Classification);
        string badge = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor");
        string history = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor");
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        // AC1/AC2: badge sources classification + detected intent from explicit projected fields, not UI text parsing.
        badge.ShouldContain("data-chatbot-classification=\"@Classification.Kind\"");
        badge.ShouldContain("ClassificationAccessible");
        badge.ShouldContain("ClassificationKindLabel");
        badge.ShouldContain("DetectedIntentSummaryLabel");
        badge.ShouldContain("DetectedActionKindLabel");

        // AC4: review history is append-only, chronologically ordered, with a per-item accessible name and no live announce.
        history.ShouldContain("chatbot-conversation-review-history");
        history.ShouldContain("ReviewHistoryAccessible");
        history.ShouldContain("OrderBy(static value => value.ReviewedAtUtc)");
        history.ShouldContain("aria-live=\"off\"");

        // AC3: source evidence precedes the opt-in, labelled AI summary disclosure carrying a provenance string.
        AssertTextOrder(
            aiOutcome,
            "data-chatbot-ai-content=\"source-evidence\"",
            "<details",
            "data-chatbot-ai-content=\"ai-summary\"",
            "AiSummaryLabel",
            "AiSummaryProvenanceLabel");
        aiOutcome.ShouldContain("ChatBotConversationItemClassificationBadge");
        aiOutcome.ShouldContain("ChatBotConversationItemReviewHistory");
        aiOutcome.ShouldNotContain("data-chatbot-ai-content=\"ai-generated\"");

        css.ShouldContain(".chatbot-conversation-classification");
        css.ShouldContain(".chatbot-conversation-review-history");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");

        fixture.ShouldContain("data-chatbot-classification=\"informational\"");
        fixture.ShouldContain("data-chatbot-classification=\"actionable\"");
        fixture.ShouldContain("aria-label=\"Classification for item 01HZXMAILBOX000000000000011\"");
        fixture.ShouldContain("aria-label=\"Classification for item ai:proposal-002:outcome-recorded:38\"");
        fixture.ShouldContain("aria-label=\"Review history for item 01HZXMAILBOX000000000000011\"");
        fixture.ShouldContain("aria-label=\"Review history for item ai:proposal-002:outcome-recorded:38\"");
        fixture.ShouldContain("Approve the renewal request");
        fixture.ShouldContain("request-decision");
        fixture.ShouldContain("message:offset:001, message:offset:002");
        fixture.ShouldContain("task_intent_captured");
        fixture.ShouldContain("review-task-intent-action");
        fixture.ShouldContain("classification_source_redacted");
        fixture.ShouldContain("Generated by chatbot-orchestrator+v1 at 2026-06-01 08:30:00Z from ai:evidence-1, ai:evidence-2");
        AssertTextOrder(
            fixture,
            "data-chatbot-ai-content=\"source-evidence\"",
            "<details class=\"chatbot-ai-outcome-conversation-item__generated\"",
            "data-chatbot-ai-content=\"ai-summary\"");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertTaskIntentReviewPanelCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.TaskIntentReview);
        string panel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor");

        panel.ShouldContain("aria-label=\"Task intent review\"");
        panel.ShouldContain("role=\"toolbar\"");
        panel.ShouldContain("aria-label=\"Task intent actions\"");
        panel.ShouldContain("aria-disabled");
        panel.ShouldContain("aria-describedby");
        panel.ShouldContain("Predecessor task intent");
        panel.ShouldContain("predecessor_task_intent_required");
        panel.ShouldContain("TaskIntentTransitionSelectionModel");
        panel.ShouldContain("role=\"status\"");
        panel.ShouldContain("aria-live=\"polite\"");
        fixture.ShouldContain("Convert to AI action");
        fixture.ShouldContain("Not actionable");
        fixture.ShouldContain("Duplicate");
        fixture.ShouldContain("Already handled");
        fixture.ShouldContain("Out of scope");
        fixture.ShouldContain("task_intent_policy_blocked");
        fixture.ShouldContain("task_intent_source_unavailable");
        fixture.ShouldNotContain("graph-message-001", Case.Insensitive);
        fixture.ShouldNotContain("tenant-beta", Case.Insensitive);
    }

    private static void AssertApprovalDecisionSurfaceCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.ApprovalDecisionSurface);
        string item = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs");

        item.ShouldContain("data-chatbot-approval-evidence-freshness");
        item.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"");
        item.ShouldContain("SubmitApprovalDecisionAsync");
        item.ShouldContain("ApprovalDecisionKind.RequestRevision");
        item.ShouldContain("aria-live=\"@DecisionLiveRegion\"");
        service.ShouldContain("item.ApprovalId");
        service.ShouldContain("item.ApprovalProposalId");
        service.ShouldContain("ContractSurfaceOrigin.Ui");
        fixture.ShouldContain("Project.AppendConversationMessage");
        fixture.ShouldContain("approval-required");
        fixture.ShouldContain("data-chatbot-approval-evidence-freshness=\"fresh\"");
        fixture.ShouldContain("data-chatbot-approval-evidence-freshness=\"stale\"");
        fixture.ShouldContain("data-chatbot-approval-evidence-freshness=\"expired\"");
        fixture.ShouldContain("aria-describedby=\"approval-approve-reason\"");
        fixture.ShouldContain("Evidence expired");
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("tenant-beta", Case.Insensitive);
    }

    private static void AssertCorrectedContextInvalidatedApprovalCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.CorrectedContextInvalidatedApproval);
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string localizer = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        approval.ShouldContain("ApprovalDisabledReasonLabel");
        approval.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"");
        approval.ShouldContain("aria-describedby=\"@ApproveReasonId\"");
        localizer.ShouldContain("corrected-context-invalidated");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        fixture.ShouldContain("data-chatbot-feedback-state=\"CurrentUserTerminalInvalidation\"");
        fixture.ShouldContain("aria-live=\"assertive\"");
        fixture.ShouldContain("aria-live=\"off\"");
        fixture.ShouldContain("aria-describedby=\"corrected-approval-disabled-reason\"");
        fixture.ShouldContain("corrected-context-invalidated");
        fixture.ShouldContain("correction-4-9-001");
        fixture.ShouldContain("association-4-9-001");
        fixture.ShouldContain("review-source-evidence");
        fixture.ShouldContain("Contexte corrigé invalidé");
        fixture.ShouldNotContain("raw prompt", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("tenant-beta", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertAiActionPreviewInspectionCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.AiActionPreviewInspection);
        string preview = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor");
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");
        string history = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        preview.ShouldContain("data-chatbot-ai-action-preview=\"metadata-only\"");
        preview.ShouldContain("data-chatbot-ai-action-preview-section");
        preview.ShouldContain("aria-disabled=\"@section.AriaDisabled\"");
        preview.ShouldContain("tabindex=\"0\"");
        approval.ShouldContain("ChatBotAiActionPreviewSections");
        aiOutcome.ShouldContain("ChatBotAiActionPreviewSections");
        history.ShouldContain("aria-live=\"off\"");
        css.ShouldContain(".chatbot-ai-action-preview");
        css.ShouldContain(".chatbot-ai-action-preview__section");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");

        fixture.ShouldContain("aria-label=\"AI action preview for item approval:approval-preview:request:50\"");
        fixture.ShouldContain("aria-label=\"AI action preview for item ai:proposal-preview:outcome-recorded:51\"");
        fixture.ShouldContain("data-chatbot-ai-action-preview-section=\"outbound\"");
        fixture.ShouldContain("data-chatbot-ai-action-preview-section=\"file-access\"");
        fixture.ShouldContain("data-chatbot-ai-action-preview-section=\"command\"");
        fixture.ShouldContain("data-chatbot-ai-action-preview-section=\"generated-changes\"");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("not-authorized");
        fixture.ShouldContain("not-yet-produced");
        fixture.ShouldContain("aria-label=\"Review history for item ai:proposal-preview:outcome-recorded:51\"");
        fixture.ShouldContain("operation-preview-001");
        fixture.ShouldContain("01HZXCORRELATIONPREVIEW00000051");
        fixture.ShouldContain("policy-snapshot-preview");
        fixture.ShouldNotContain("restricted-quarterly-plan.xlsx", Case.Insensitive);
        fixture.ShouldNotContain("/tenants/tenant-beta/files", Case.Insensitive);
        fixture.ShouldNotContain("tenant-beta", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertRefusalSafeBlockCoverageWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.RefusalSafeBlock);
        string blockedState = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor");
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string failure = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor");
        string aiOutcome = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor");
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        blockedState.ShouldContain("data-chatbot-feedback-state");
        blockedState.ShouldContain("aria-live");
        approval.ShouldContain("aria-disabled");
        approval.ShouldContain("WhyUnavailable");
        failure.ShouldContain("FailureCatalogHeadline");
        failure.ShouldContain("FailureCatalogReason");
        aiOutcome.ShouldContain("AiOutcomeKindLabel");
        aiOutcome.ShouldContain("AiOutcomeMetadataOnlyReason");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");

        fixture.ShouldContain("aria-label=\"Blocked: Request refused. Next action: Request access.\"");
        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("aria-live=\"assertive\"");
        fixture.ShouldContain("data-chatbot-catalog-code=\"refusal_blocked_action\"");
        fixture.ShouldContain("data-chatbot-refusal-reason=\"tenant-policy-exceeded\"");
        fixture.ShouldContain("aria-label=\"Approval event, Approval outcome, Blocked, 2026-06-01 09:00:10Z\"");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("aria-describedby=\"approval-blocked-reason\"");
        fixture.ShouldContain("Evidence expired");
        fixture.ShouldContain("evidence-expired");
        fixture.ShouldContain("aria-label=\"System status, Unsupported command, Blocked, 2026-06-01 09:00:20Z\"");
        fixture.ShouldContain("unsupported-action");
        fixture.ShouldContain("command-not-allowlisted");
        fixture.ShouldContain("not-admitted");
        fixture.ShouldContain("not-called");
        fixture.ShouldContain("aria-label=\"AI actor, AI refusal, Blocked, 2026-06-01 09:00:30Z\"");
        fixture.ShouldContain("missing-required-context");
        fixture.ShouldContain("context-package-unavailable");
        fixture.ShouldContain("policy-snapshot-unavailable");
        fixture.ShouldContain("approval-state-invalid");
        fixture.ShouldContain("corrected-context-invalidated");
        fixture.ShouldContain("dependency-degraded");
        fixture.ShouldContain("Audit denial fact");
        fixture.ShouldContain("request-files");
        fixture.ShouldContain("correct-request");
        fixture.ShouldContain("mcp");
        fixture.ShouldNotContain("Project.SendExternalEmail", Case.Insensitive);
        fixture.ShouldNotContain("tenant-beta", Case.Insensitive);
        fixture.ShouldNotContain("restricted-policy-text", Case.Insensitive);
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertEmptyWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Empty);
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");

        stream.ShouldContain("<ChatBotBlockedState");
        fixture.ShouldContain("No email-derived context is available.");
        fixture.ShouldContain("Wait for associated email.");
        fixture.ShouldContain("role=\"alert\"");
    }

    private static void AssertUnauthorizedWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Unauthorized);
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain("@media (forced-colors: active)");
        fixture.ShouldContain("Project conversation is unavailable.");
        fixture.ShouldContain("Evidence restricted");
        fixture.ShouldContain("project-redacted");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertMetadataOnlyBody(string text)
    {
        text.ShouldNotContain("restricted@example.com", Case.Insensitive);
        text.ShouldNotContain("sender@example.test", Case.Insensitive);
        text.ShouldNotContain("raw provider payload", Case.Insensitive);
        text.ShouldNotContain("Secret Project", Case.Insensitive);
        text.ShouldNotContain("raw exception", Case.Insensitive);
        text.ShouldNotContain("stack trace", Case.Insensitive);
        text.ShouldNotContain("provider diagnostic", Case.Insensitive);
        text.ShouldNotContain("raw prompt", Case.Insensitive);
        text.ShouldNotContain("raw model output", Case.Insensitive);
        text.ShouldNotContain("raw command payload", Case.Insensitive);
        text.ShouldNotContain("raw policy body", Case.Insensitive);
        text.ShouldNotContain("raw audit envelope", Case.Insensitive);
        text.ShouldNotContain("full email body", Case.Insensitive);
        text.ShouldNotContain("raw email address evidence", Case.Insensitive);
        text.ShouldNotContain("provider display name", Case.Insensitive);
        text.ShouldNotContain("unauthorized party name", Case.Insensitive);
        text.ShouldNotContain("restricted party detail", Case.Insensitive);
        text.ShouldNotContain("hidden diagnostic", Case.Insensitive);
        text.ShouldNotContain("raw attachment content", Case.Insensitive);
        text.ShouldNotContain("base64 attachment", Case.Insensitive);
        text.ShouldNotContain("attachment bytes", Case.Insensitive);
        text.ShouldNotContain("graph delta token", Case.Insensitive);
        text.ShouldNotContain("local attachment path", Case.Insensitive);
        text.ShouldNotContain("source provider payload", Case.Insensitive);
        text.ShouldNotContain("raw decision note", Case.Insensitive);
        text.ShouldNotContain("raw correction rationale", Case.Insensitive);
        text.ShouldNotContain("hidden evidence value", Case.Insensitive);
        text.ShouldNotContain("malware scan detail", Case.Insensitive);
        text.ShouldNotContain("unauthorized folder name", Case.Insensitive);
        text.ShouldNotContain("unauthorized file name", Case.Insensitive);
    }

    private sealed class BrowserHarness : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;

        private BrowserHarness(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
            Page = page;
        }

        public IPage Page { get; }

        public static async Task<BrowserHarness?> TryStartAsync(bool forcedColors = false)
        {
            string? chromeExecutable = ResolveChromeExecutable();
            if (chromeExecutable is null)
            {
                return null;
            }

            try
            {
                return await StartAsync(chromeExecutable, forcedColors).ConfigureAwait(false);
            }
            catch (PlaywrightException ex) when (IsBrowserUnavailable(ex))
            {
                return null;
            }
        }

        private static async Task<BrowserHarness> StartAsync(string chromeExecutable, bool forcedColors)
        {
            IPlaywright? playwright = null;
            IBrowser? browser = null;
            IBrowserContext? context = null;

            try
            {
                playwright = await Playwright.CreateAsync().ConfigureAwait(false);
                browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    ExecutablePath = chromeExecutable,
                    Args = ["--no-sandbox", "--disable-dev-shm-usage"],
                }).ConfigureAwait(false);
                context = await browser.NewContextAsync(new()
                {
                    ForcedColors = forcedColors ? ForcedColors.Active : ForcedColors.None,
                    ReducedMotion = ReducedMotion.Reduce,
                }).ConfigureAwait(false);
                IPage page = await context.NewPageAsync().ConfigureAwait(false);

                return new BrowserHarness(playwright, browser, context, page);
            }
            catch
            {
                if (context is not null)
                {
                    await context.DisposeAsync().ConfigureAwait(false);
                }

                if (browser is not null)
                {
                    await browser.DisposeAsync().ConfigureAwait(false);
                }

                playwright?.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync().ConfigureAwait(false);
            await _browser.DisposeAsync().ConfigureAwait(false);
            _playwright.Dispose();
        }

        private static bool IsBrowserUnavailable(PlaywrightException ex)
            => ex.Message.Contains("crashpad", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Operation not permitted", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase);
    }

    private enum ProjectConversationFixtureScenario
    {
        Loading,
        Populated,
        Empty,
        Unauthorized,
        Classification,
        TaskIntentReview,
        ApprovalDecisionSurface,
        CorrectedContextInvalidatedApproval,
        AiActionPreviewInspection,
        LowRiskAiExecution,
        ApprovedAiActionExecution,
        RefusalSafeBlock,
    }

    private static string? ResolveChromeExecutable()
    {
        string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return File.Exists(configured) ? configured : null;
        }

        string linuxChrome = "/usr/bin/google-chrome";
        return File.Exists(linuxChrome) ? linuxChrome : null;
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(FindSolutionRoot(), relativePath));

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("The test process should run beneath the ChatBot repository.");
        return directory.FullName;
    }
}
#pragma warning restore CA2007
