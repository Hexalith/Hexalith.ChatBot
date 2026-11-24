using Hexalith.ChatBot.Client.Generated;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // Browser tests keep awaits on the xUnit synchronization context.

/// <summary>Verifies the live governed queue's disclosure and project selection.</summary>
public sealed class RealRenderReviewQueueE2ETests
{
    /// <summary>Shows the tenant-safe summary, then only authorized project results.</summary>
    [Fact]
    public async Task QueueShouldUseTheSelectedScopeAndRemovePreviousProjectRowsOnRefusal()
    {
        await using RealRenderFixture fixture = new(() => new FakeChatBotClient
        {
            TenantQueueRead = family => new()
            {
                DisclosureState = "buckets-only",
                Coverage = new() { Reason = "projection-current" },
                Buckets = [new() { Status = family == ReviewQueueFamily.AmbiguousAssociation ? "waiting" : "retryable", OwnerClass = "operations", Count = 4 }],
            },
            ProjectQueueRead = (project, family) => project == "visible-project"
                ? Task.FromResult(new ProjectReviewQueue
                {
                    Coverage = new() { Reason = "project-source-inventory-unverified" },
                    Rows = family == ReviewQueueFamily.PendingApproval ? [] :
                    [
                        new() { ItemRef = "review:visible-result", QueueRef = "queue:ambiguous-association",
                            State = "needs-review", Risk = "medium", OwnerRole = "reviewer", NextAction = "review", SourceVersion = 17 },
                    ],
                })
                : Task.FromException<ProjectReviewQueue>(new HttpRequestException("private refusal prose")),
        });
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        await context.AddCookiesAsync([new Cookie { Name = ".AspNetCore.Culture", Value = "c=en|uic=en", Url = fixture.BaseUri }]);
        IPage page = await context.NewPageAsync();
        (await page.GotoAsync(fixture.BaseUri + "/governed-operations"))!.Ok.ShouldBeTrue();
        ILocator queue = page.Locator("[data-chatbot-operational-queue]");
        await Assertions.Expect(queue.GetByText("buckets-only")).ToBeVisibleAsync();
        await Assertions.Expect(queue.GetByText("waiting", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(queue.Locator("[data-chatbot-item-ref]")).ToHaveCountAsync(0);

        ILocator project = queue.GetByRole(AriaRole.Textbox, new() { NameString = "Project identifier" });
        ILocator open = queue.Locator("fluent-button").Filter(new() { HasText = "View project queues" });
        await project.FillAsync("visible-project");
        await open.ClickAsync();
        ILocator row = queue.Locator("[data-chatbot-item-ref='review:visible-result']");
        await Assertions.Expect(row).ToHaveAttributeAsync("data-chatbot-source-version", "17");

        await project.FillAsync("forbidden-project");
        await open.ClickAsync();
        await Assertions.Expect(queue.GetByText("The review queue is unavailable for this selection.")).ToBeVisibleAsync();
        await Assertions.Expect(row).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("private refusal prose")).ToHaveCountAsync(0);
    }
}
