using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Commands;

using Microsoft.Playwright;

using Shouldly;

using ApprovalDecisionKind = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;
using ApprovalEvidenceFreshness = Hexalith.ChatBot.Client.Generated.ApprovalEvidenceFreshness;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods keep awaits on the xUnit synchronization context.

/// <summary>Exercises governed approval decisions and queue priority on the live Blazor routes.</summary>
public sealed class RealRenderApprovalE2ETests
{
    private const string ProjectId = "project-alpha";
    private const string ConversationRoute = "/projects/" + ProjectId + "/conversation";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string PendingApprovalSelector = "article[data-chatbot-conversation-item-id='item:approval-001']";

    /// <summary>Approves a pending item and checks both its announcement and the governed client command.</summary>
    [Fact]
    public async Task FreshApprovalShouldSubmitOnlyTheDecisionAndAnnounceTheOutcome()
    {
        ConcurrentQueue<(IChatBotCommand Command, string? CorrelationId, ChatBotSurfaceOrigin Origin)> submissions = new();
        await using RealRenderFixture fixture = new(() => new FakeChatBotClient
        {
            ConfigurePendingApproval = item => item.ProjectId = ProjectId,
            SubmissionObserved = (command, correlationId, origin) => submissions.Enqueue((command, correlationId, origin)),
        });
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAsync(context, fixture.BaseUri, ConversationRoute);
        ILocator approval = await GetInteractiveApprovalAsync(page);
        ILocator approve = approval.Locator(".chatbot-approval-conversation-item__actions fluent-button[aria-disabled]");
        await Assertions.Expect(approval.Locator("[data-chatbot-approval-evidence-freshness='fresh']")).ToBeVisibleAsync();
        await Assertions.Expect(approve).ToHaveAttributeAsync("aria-disabled", "false");

        await approve.ClickAsync();

        ILocator status = approval.Locator(".chatbot-approval-conversation-item__decision-status");
        await Assertions.Expect(status).ToHaveTextAsync("Approved");
        await Assertions.Expect(status).ToHaveAttributeAsync("aria-live", "polite");
        await Assertions.Expect(status).ToBeFocusedAsync();
        AssertApprovalSubmission(submissions);
    }

    /// <summary>Checks the disabled approval reason and keyboard feedback for expired evidence.</summary>
    [Fact]
    public async Task ExpiredEvidenceShouldExplainTheBlockAndPreventSubmission()
    {
        ConcurrentQueue<IChatBotCommand> submissions = new();
        await using RealRenderFixture fixture = new(() => new FakeChatBotClient
        {
            ConfigurePendingApproval = item =>
            {
                item.ProjectId = ProjectId;
                item.ApprovalEvidenceReferences = ["evidence:fresh", "evidence:stale", "evidence:expired"];
                item.ApprovalEvidenceFreshnessStates = [ApprovalEvidenceFreshness.Fresh, ApprovalEvidenceFreshness.Stale, ApprovalEvidenceFreshness.Expired];
            },
            SubmissionObserved = (command, _, _) => submissions.Enqueue(command),
        });
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAsync(context, fixture.BaseUri, ConversationRoute, width: 390);
        ILocator approval = await GetInteractiveApprovalAsync(page);
        ILocator approve = approval.Locator(".chatbot-approval-conversation-item__actions fluent-button[aria-disabled]");
        await Assertions.Expect(approval.Locator("[data-chatbot-approval-evidence-freshness] .chatbot-chip__status"))
            .ToHaveTextAsync(["Fresh", "Stale", "Expired"]);
        await Assertions.Expect(approval.Locator("fluent-button[data-chatbot-approval-evidence-freshness='expired']"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(approve).ToHaveAttributeAsync("aria-disabled", "true");
        string? reasonId = await approve.GetAttributeAsync("aria-describedby");
        reasonId.ShouldNotBeNullOrWhiteSpace();
        await Assertions.Expect(approval.Locator($"[id='{reasonId}']")).ToHaveTextAsync("Why unavailable? Evidence expired");

        await approve.FocusAsync();
        await Assertions.Expect(approve).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        ILocator status = approval.Locator(".chatbot-approval-conversation-item__decision-status");
        await Assertions.Expect(status).ToHaveTextAsync("Evidence expired");
        await Assertions.Expect(status).ToHaveAttributeAsync("aria-live", "assertive");
        await Assertions.Expect(status).ToBeFocusedAsync();
        submissions.ShouldBeEmpty();
    }

    /// <summary>Checks safe feedback when the server refuses a decision after the pending item renders.</summary>
    /// <param name="code">The typed refusal code returned by the client seam.</param>
    /// <param name="reason">The localized reason announced by the component.</param>
    [Theory]
    [InlineData("insufficient-authority", "Insufficient authority")]
    [InlineData("corrected-context-invalidated", "Corrected context invalidated")]
    public async Task RefusedApprovalShouldAnnounceTheSafeReasonWithoutExecutingTheAction(string code, string reason)
    {
        ConcurrentQueue<(IChatBotCommand Command, string? CorrelationId, ChatBotSurfaceOrigin Origin)> submissions = new();
        await using RealRenderFixture fixture = new(() => new FakeChatBotClient(code)
        {
            ConfigurePendingApproval = item => item.ProjectId = ProjectId,
            SubmissionObserved = (command, correlationId, origin) => submissions.Enqueue((command, correlationId, origin)),
        });
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAsync(context, fixture.BaseUri, ConversationRoute);
        ILocator approval = await GetInteractiveApprovalAsync(page);
        ILocator approve = approval.Locator(".chatbot-approval-conversation-item__actions fluent-button[aria-disabled]");
        await Assertions.Expect(approve).ToHaveAttributeAsync("aria-disabled", "false");

        await approve.ClickAsync();

        ILocator status = approval.Locator(".chatbot-approval-conversation-item__decision-status");
        await Assertions.Expect(status).ToHaveTextAsync(reason);
        await Assertions.Expect(status).ToHaveAttributeAsync("aria-live", "assertive");
        await Assertions.Expect(status).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByText("restricted prose", new() { Exact = false })).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("restricted payload", new() { Exact = false })).ToHaveCountAsync(0);
        AssertApprovalSubmission(submissions);
    }

    /// <summary>Checks the production priority component's order, batch authority reason, and phone summary.</summary>
    [Fact]
    public async Task PriorityQueueShouldRenderOrderedGroupsAndKeepBatchApprovalUnavailable()
    {
        ConcurrentQueue<IChatBotCommand> submissions = new();
        await using RealRenderFixture fixture = new(() => new FakeChatBotClient
        {
            SubmissionObserved = (command, _, _) => submissions.Enqueue(command),
        });
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAsync(context, fixture.BaseUri, "/governed-operations");
        ILocator priority = page.GetByRole(AriaRole.Region, new() { NameString = "Prioritized approval queue", Exact = true });
        ILocator table = priority.GetByRole(AriaRole.Table, new() { NameString = "Prioritized approval queue", Exact = true });
        await Assertions.Expect(table).ToBeVisibleAsync();
        await Assertions.Expect(table.GetByRole(AriaRole.Columnheader)).ToHaveTextAsync(
            ["Priority", "Approval group", "Requester", "Command", "Project", "Items in group", "Approve group"]);
        ILocator rows = table.Locator("tbody tr");
        await Assertions.Expect(rows.Locator(".chatbot-priority-label")).ToHaveTextAsync(["Critical", "High", "Medium"]);
        await Assertions.Expect(rows.Locator("[data-approval-group-item-count]")).ToHaveTextAsync(["3", "2", "1"]);

        ILocator approveGroup = rows.First.GetByRole(AriaRole.Button, new() { NameString = "Approve group", Exact = true });
        await Assertions.Expect(approveGroup).ToHaveAttributeAsync("aria-disabled", "true");
        string? reasonId = await approveGroup.GetAttributeAsync("aria-describedby");
        reasonId.ShouldNotBeNullOrWhiteSpace();
        await Assertions.Expect(rows.First.Locator($"[id='{reasonId}']"))
            .ToContainTextAsync("Per-project approval authority is required for every item in the group");
        await Assertions.Expect(priority.Locator("[data-approval-partial-authority-reason]"))
            .ToContainTextAsync("Only items you are authorized to decide are acted on");
        await approveGroup.FocusAsync();
        await Assertions.Expect(approveGroup).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await page.SetViewportSizeAsync(390, 844);
        await Assertions.Expect(priority.Locator("[data-approval-priority-table]")).ToBeHiddenAsync();
        ILocator summary = priority.GetByRole(AriaRole.Complementary, new() { NameString = "Prioritized approval summary is available on phone.", Exact = true });
        await Assertions.Expect(summary).ToBeVisibleAsync();
        await Assertions.Expect(summary.Locator("code")).ToHaveTextAsync(
        [
            "Critical · requester:requester-1 · command:appendconversationmessage · project:project-1 · 3",
            "High · requester:requester-2 · command:createoutbounddraft · project:project-2 · 2",
            "Medium · requester:requester-3 · command:appendconversationmessage · project:project-3 · 1",
        ]);
        await Assertions.Expect(summary).ToContainTextAsync("Dense batch approval controls are unavailable on this screen size.");
        submissions.ShouldBeEmpty();
    }

    private static async Task<IPage> OpenAsync(IBrowserContext context, string baseUri, string route, int width = 1280)
    {
        await context.AddCookiesAsync([new Cookie { Name = ".AspNetCore.Culture", Value = "c=en|uic=en", Url = baseUri }]);
        IPage page = await context.NewPageAsync();
        await page.SetViewportSizeAsync(width, 900);
        IResponse? response = await page.GotoAsync(baseUri + route);
        response.ShouldNotBeNull();
        response.Ok.ShouldBeTrue();
        return page;
    }

    private static async Task<ILocator> GetInteractiveApprovalAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "selector => !!document.querySelector(selector + ' fluent-button[aria-disabled]')?.shadowRoot",
            PendingApprovalSelector);
        ILocator approval = page.Locator(PendingApprovalSelector);
        await Assertions.Expect(approval).ToBeVisibleAsync();
        return approval;
    }

    private static void AssertApprovalSubmission(
        ConcurrentQueue<(IChatBotCommand Command, string? CorrelationId, ChatBotSurfaceOrigin Origin)> submissions)
    {
        var submission = Assert.Single(submissions);
        submission.Command.ShouldBe(new DecideAiActionApproval(
            ProjectId,
            "approval:approval-001",
            "proposal:approval-001",
            "message:approval-001",
            ApprovalDecisionKind.Approve,
            1,
            CorrelationId,
            "approval-decision:approval:approval-001:Approve:1")
        {
            StateOwnerAggregateId = "conversation:approval-001",
        });
        submission.CorrelationId.ShouldBe(CorrelationId);
        submission.Origin.ShouldBe(ChatBotSurfaceOrigin.Ui);
    }
}
