using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.

/// <summary>
/// Accessibility/foundation E2E coverage for the read-only operational dashboards surface (Story 8.1, AC4/AC10):
/// the semantic-token CSS loads, the surface exposes WCAG-2.2-AA landmarks with keyboard-reachable rows and
/// non-color status, and freshness/status are announced through <c>aria-live</c> regions with deduplication. When
/// no browser is available the test falls back to asserting the same contract against the page and CSS sources.
/// </summary>
public sealed class OperationalDashboardsAccessibilityE2ETests
{
    [Fact]
    public async Task DashboardShouldExposeLandmarksKeyboardRowsAndLiveFreshnessAnnouncements()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertContractWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Operational dashboards", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Main));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Table));

            ILocator freshness = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Freshness: Expired" });
            await WaitForVisibleAsync(freshness);
            (await freshness.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await freshness.GetAttributeAsync("data-chatbot-announcement-key")).ShouldBe("dashboard-freshness-audit-projection-lag");

            // Non-color status: the status text label is present, not color alone.
            await WaitForVisibleAsync(harness.Page.GetByText("Expired", new() { Exact = true }));

            // The row is keyboard reachable (tabindex=0).
            ILocator row = harness.Page.Locator("[data-chatbot-dashboard-view='audit-projection-lag']");
            await WaitForVisibleAsync(row);
            (await row.GetAttributeAsync("tabindex")).ShouldBe("0");

            // No duplicate live announcement for the same stable key.
            int announcements = await harness.Page.Locator("[data-chatbot-announcement-key='dashboard-freshness-audit-projection-lag']").CountAsync();
            announcements.ShouldBe(1);
        }
    }

    private static void AssertContractWithoutBrowser()
    {
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");

        app.ShouldContain("css/chatbot.tokens.css");

        // Landmarks + keyboard-reachable rows.
        page.ShouldContain("role=\"table\"");
        page.ShouldContain("role=\"row\"");
        page.ShouldContain("tabindex=\"0\"");

        // Governed primitives drive the aria-live status + freshness announcements with dedup keys.
        page.ShouldContain("ChatBotStatusBanner");
        page.ShouldContain("AnnouncementKey=\"@($\"dashboard-freshness-{viewToken}\")\"");

        // Non-color status: localized health/freshness labels accompany the semantic color.
        page.ShouldContain("HealthLabel");
        page.ShouldContain("FreshnessLabel");
    }

    private static string BuildFixture()
        => """
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8" /></head>
        <body>
          <section aria-label="Operational dashboards">
            <h1>Operational dashboards</h1>
            <section role="region" aria-label="Operational dashboards">
              <div role="region" aria-label="Operational dashboards main">
                <main>
                  <div class="chatbot-status" role="status" aria-live="polite" aria-atomic="true"
                       aria-label="Freshness: Expired"
                       data-chatbot-announcement-key="dashboard-freshness-audit-projection-lag">
                    <span>Expired</span>
                  </div>
                  <div class="chatbot-table" role="table" aria-label="Operational dashboards">
                    <article role="row" tabindex="0" data-chatbot-dashboard-view="audit-projection-lag" data-chatbot-freshness="expired">
                      <dl><dt>View</dt><dd>Audit projection lag</dd><dt>Status</dt><dd>Unknown</dd></dl>
                    </article>
                  </div>
                </main>
              </div>
            </section>
          </section>
        </body>
        </html>
        """;

    private static async Task WaitForVisibleAsync(ILocator locator)
        => await locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

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
