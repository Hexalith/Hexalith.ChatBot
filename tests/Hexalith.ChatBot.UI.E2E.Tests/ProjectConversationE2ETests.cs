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
        IReadOnlyList<string>? expectedOrderedMarkers = null)
    {
        await WaitForVisibleAsync(decisionItem);
        (await decisionItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await decisionItem.FocusAsync();
        (await decisionItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        string? accessibleName = await decisionItem.GetAttributeAsync("aria-label");
        accessibleName.ShouldNotBeNullOrWhiteSpace();
        accessibleName.StartsWith("System decision,", StringComparison.Ordinal).ShouldBeTrue();

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

        stream.ShouldContain("data-chatbot-conversation-stream=\"metadata-only\"");
        stream.ShouldContain("ChatBotParticipantConversationItem");
        stream.ShouldContain("ChatBotAttachmentConversationItem");
        stream.ShouldContain("ChatBotDecisionConversationItem");
        stream.ShouldContain("ChatBotApprovalConversationItem");
        item.ShouldContain("ProjectConversationSystemDecision");
        decision.ShouldContain("ProjectConversationDecisionItemAccessible");
        decision.ShouldContain("DecisionKindLabel");
        decision.ShouldContain("CorrectionKindLabel");
        participant.ShouldContain("ProjectConversationParticipantItemAccessible");
        attachment.ShouldContain("ProjectConversationAttachmentItemAccessible");
        approval.ShouldContain("ApprovalEventAccessible");
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
        fixture.ShouldContain("request-revision");
        fixture.ShouldContain("insufficient-authority");
        fixture.ShouldContain("Revision requested");
        fixture.ShouldContain("Cancelled");
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
        css.ShouldContain("animation: none !important;");
        css.ShouldContain("transition-duration: 0.01ms !important;");
        css.ShouldContain(".chatbot-email-conversation-item__header");
        css.ShouldContain(".chatbot-decision-conversation-item__header");
        css.ShouldContain("flex-direction: column;");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("aria-label=\"Mailbox item: Mailbox intake, Associated\"");
        fixture.ShouldContain("aria-label=\"System decision, Needs review, NeedsReview, 2026-06-01 08:05:00Z\"");
        fixture.ShouldContain("aria-label=\"Mailbox attachment, invoice.pdf, Pending, Associated\"");
        fixture.ShouldContain("aria-label=\"Project conversation metadata\"");
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
        text.ShouldNotContain("full email body", Case.Insensitive);
        text.ShouldNotContain("raw email address evidence", Case.Insensitive);
        text.ShouldNotContain("provider display name", Case.Insensitive);
        text.ShouldNotContain("unauthorized party name", Case.Insensitive);
        text.ShouldNotContain("restricted party detail", Case.Insensitive);
        text.ShouldNotContain("hidden diagnostic", Case.Insensitive);
        text.ShouldNotContain("raw attachment content", Case.Insensitive);
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
