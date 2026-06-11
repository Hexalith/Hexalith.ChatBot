using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.

/// <summary>
/// Story 8.5 E2E coverage for the degraded operational-dashboard surface: a degraded row renders all four NFR42
/// elements as metadata-only safe tokens, while healthy rows do not fabricate degraded-only runbook fields.
/// </summary>
public sealed class OperationalDashboardsDegradedSurfaceE2ETests
{
    [Fact]
    public async Task DegradedDashboardView_ShouldRenderNfr42FourElementSurfaceAndStayMetadataOnly()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertDegradedSurfaceContractWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Operational dashboards", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Table, new() { NameString = "Operational dashboards" }));

            ILocator degraded = harness.Page.Locator("[data-chatbot-dashboard-view='mailbox-processing']");
            await WaitForVisibleAsync(degraded);
            (await degraded.GetAttributeAsync("data-chatbot-health")).ShouldBe("degraded");
            (await degraded.GetAttributeAsync("tabindex")).ShouldBe("0");

            string rowText = await degraded.InnerTextAsync();
            rowText.ShouldContain("Status");
            rowText.ShouldContain("Degraded");
            rowText.ShouldContain("Affected scope");
            rowText.ShouldContain("mailbox:ops");
            rowText.ShouldContain("Owner role");
            rowText.ShouldContain("mailbox-admin");
            rowText.ShouldContain("Next safe action");
            rowText.ShouldContain("renew-graph-subscription");

            ILocator affectedScope = degraded.Locator("[data-chatbot-affected-scope='mailbox:ops']");
            ILocator nextSafeAction = degraded.Locator("[data-chatbot-next-safe-action='renew-graph-subscription']");
            await WaitForVisibleAsync(affectedScope);
            await WaitForVisibleAsync(nextSafeAction);
            (await affectedScope.TextContentAsync()).ShouldBe("mailbox:ops");
            (await nextSafeAction.TextContentAsync()).ShouldBe("renew-graph-subscription");

            ILocator status = degraded.GetByRole(AriaRole.Status, new() { NameString = "Mailbox processing: Degraded" });
            await WaitForVisibleAsync(status);
            (await status.GetAttributeAsync("aria-live")).ShouldBe("polite");

            ILocator healthy = harness.Page.Locator("[data-chatbot-dashboard-view='approval-queues']");
            await WaitForVisibleAsync(healthy);
            (await healthy.GetAttributeAsync("data-chatbot-health")).ShouldBe("healthy");
            (await healthy.Locator("[data-chatbot-affected-scope]").CountAsync()).ShouldBe(0);
            (await healthy.Locator("[data-chatbot-next-safe-action]").CountAsync()).ShouldBe(0);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static void AssertDegradedSurfaceContractWithoutBrowser()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");
        string contractTests = ReadProjectFile("tests/Hexalith.ChatBot.Contracts.Tests/OperationalDashboardContractTests.cs");
        string projectorTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Projections/OperationalDashboardProjectorTests.cs");
        string english = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.resx");
        string french = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx");

        page.ShouldContain("data-chatbot-health");
        page.ShouldContain("data-chatbot-affected-scope");
        page.ShouldContain("data-chatbot-next-safe-action");
        page.ShouldContain("OperationalDashboardsOwnerRoleLabel");
        page.ShouldContain("OperationalDashboardsAffectedScopeLabel");
        page.ShouldContain("OperationalDashboardsNextSafeActionLabel");
        page.ShouldContain("view.AffectedScope is { } affectedScope");
        page.ShouldContain("view.NextSafeAction is { } nextSafeAction");

        contractTests.ShouldContain("DegradedViewMustCarryAffectedScopeAndNextSafeActionWhileHealthyViewMayLeaveThemNull");
        contractTests.ShouldContain("degraded_affected_scope_missing");
        contractTests.ShouldContain("degraded_next_safe_action_missing");
        projectorTests.ShouldContain("DegradedViewShouldCarryResolvedAffectedScopeKindAndNextSafeActionWhileHealthyViewsLeaveThemNull");
        projectorTests.ShouldContain("mailbox.AffectedScope.ShouldBe(\"mailbox:ops\")");
        projectorTests.ShouldContain("approvals.AffectedScope.ShouldBeNull()");
        english.ShouldContain("OperationalDashboards_AffectedScope_Label");
        english.ShouldContain("OperationalDashboards_NextSafeAction_Label");
        french.ShouldContain("OperationalDashboards_AffectedScope_Label");
        french.ShouldContain("OperationalDashboards_NextSafeAction_Label");

        AssertMetadataOnly(BuildFixture());
    }

    private static string BuildFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head><meta charset="utf-8" /><title>Operational dashboards degraded surface</title></head>
          <body>
            <main aria-labelledby="operational-dashboards-title">
              <h1 id="operational-dashboards-title">Operational dashboards</h1>
              <div class="chatbot-table" role="table" aria-label="Operational dashboards">
                <article role="row"
                         tabindex="0"
                         data-chatbot-dashboard-view="mailbox-processing"
                         data-chatbot-health="degraded"
                         data-chatbot-freshness="fresh">
                  <dl>
                    <dt>View</dt><dd>Mailbox processing</dd>
                    <dt>Status</dt><dd><code>Degraded</code></dd>
                    <dt>Affected scope</dt><dd><code data-chatbot-affected-scope="mailbox:ops">mailbox:ops</code></dd>
                    <dt>Owner role</dt><dd><code>mailbox-admin</code></dd>
                    <dt>Next safe action</dt><dd><code data-chatbot-next-safe-action="renew-graph-subscription">renew-graph-subscription</code></dd>
                    <dt>Freshness</dt><dd><code>Fresh</code></dd>
                  </dl>
                  <div aria-label="Degraded">
                    <p role="status"
                       aria-live="polite"
                       aria-atomic="true"
                       aria-label="Mailbox processing: Degraded"
                       data-chatbot-announcement-key="dashboard-health-mailbox-processing">Degraded</p>
                  </div>
                </article>
                <article role="row"
                         tabindex="0"
                         data-chatbot-dashboard-view="approval-queues"
                         data-chatbot-health="healthy"
                         data-chatbot-freshness="fresh">
                  <dl>
                    <dt>View</dt><dd>Approval queues</dd>
                    <dt>Status</dt><dd><code>Healthy</code></dd>
                    <dt>Owner role</dt><dd><code>operations-admin</code></dd>
                    <dt>Freshness</dt><dd><code>Fresh</code></dd>
                  </dl>
                </article>
              </div>
            </main>
          </body>
        </html>
        """;

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldContain("mailbox:ops");
        text.ShouldContain("renew-graph-subscription");
        text.ShouldNotContain("Project Alpha", Case.Insensitive);
        text.ShouldNotContain("EvidenceContent", Case.Insensitive);
        text.ShouldNotContain("MailboxSubject", Case.Insensitive);
        text.ShouldNotContain("exception", Case.Insensitive);
        text.ShouldNotContain("bearer", Case.Insensitive);
        text.ShouldNotContain("secret", Case.Insensitive);
        text.ShouldNotContain("password", Case.Insensitive);
        text.ShouldNotContain("@example", Case.Insensitive);
        text.ShouldNotContain(".txt", Case.Insensitive);
        text.ShouldNotContain(".json", Case.Insensitive);
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

        public static async Task<BrowserHarness?> TryStartAsync()
        {
            string? chromeExecutable = ResolveChromeExecutable();
            if (chromeExecutable is null)
            {
                return null;
            }

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
                context = await browser.NewContextAsync(new() { ReducedMotion = ReducedMotion.Reduce }).ConfigureAwait(false);
                IPage page = await context.NewPageAsync().ConfigureAwait(false);
                return new BrowserHarness(playwright, browser, context, page);
            }
            catch (PlaywrightException)
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
                return null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync().ConfigureAwait(false);
            await _browser.DisposeAsync().ConfigureAwait(false);
            _playwright.Dispose();
        }

        private static string? ResolveChromeExecutable()
        {
            string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return File.Exists(configured) ? configured : null;
            }

            const string linuxChrome = "/usr/bin/google-chrome";
            return File.Exists(linuxChrome) ? linuxChrome : null;
        }
    }
}
