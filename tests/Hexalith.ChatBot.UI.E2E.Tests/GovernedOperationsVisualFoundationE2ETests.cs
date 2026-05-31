using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class GovernedOperationsVisualFoundationE2ETests
{
    [Fact]
    public async Task RuntimeTokenFoundationShouldLoadCssAndExposeSemanticAliases()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRuntimeTokenFoundationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Governed operations", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Main));

            ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor").ShouldContain("css/chatbot.tokens.css");

            string infoBackground = await CssVariableAsync(harness.Page, "--chatbot-color-info-background");
            infoBackground.ShouldContain("var(--colorStatusInformationBackground1)");
            infoBackground.ShouldNotContain("#");
            infoBackground.ShouldNotContain("rgb(", Case.Insensitive);
            infoBackground.ShouldNotContain("hsl(", Case.Insensitive);

            string warningForeground = await CssVariableAsync(harness.Page, "--chatbot-color-warning-foreground");
            warningForeground.ShouldContain("var(--colorStatusWarningForeground1)");

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }));
        }
    }

    [Fact]
    public async Task CommandWorkflowShouldDeclareUiOriginAndRenderSemanticStatusSummary()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertCommandWorkflowWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Projection status: pending" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit status: committed" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit history: metadata only" }));

            string commandType = await harness.Page.EvaluateAsync<string>("() => window.__lastCommand.commandType");
            string origin = await harness.Page.EvaluateAsync<string>("() => window.__lastCommand.origin");
            commandType.ShouldBe("RecordGovernedNote");
            origin.ShouldBe("ui");

            await WaitForVisibleAsync(harness.Page.GetByText("Warning", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Success", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Info", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("post-commit", new() { Exact = false }));
            await WaitForVisibleAsync(harness.Page.GetByText("metadata-only", new() { Exact = false }));
        }
    }

    [Fact]
    public async Task BackendFailureShouldRenderDangerAlertAndLeaveRetryAvailable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertBackendFailureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.SubmitFails));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

            ILocator alert = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Submission status: failed" });
            await WaitForVisibleAsync(alert);
            await WaitForVisibleAsync(harness.Page.GetByText("Danger", new() { Exact = true }));
            (await alert.TextContentAsync() ?? string.Empty).ShouldContain("Submission did not complete");
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).IsEnabledAsync()).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ForcedColorsShouldPreserveVisibleStatusLabelsAndNonColorCues()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertForcedColorsWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Projection status: pending" }));
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();

            ILocator status = harness.Page.Locator(".chatbot-status[data-chatbot-status='warning']").First;
            ILocator label = status.Locator(".chatbot-status__label");
            await WaitForVisibleAsync(label);
            (await label.TextContentAsync()).ShouldBe("Warning");

            string borderStyle = await label.EvaluateAsync<string>("element => getComputedStyle(element).borderTopStyle");
            borderStyle.ShouldBe("solid");
        }
    }

    private static async Task<string> CssVariableAsync(IPage page, string name)
        => await page.EvaluateAsync<string>(
                "token => getComputedStyle(document.documentElement).getPropertyValue(token).trim()",
                name)
            .ConfigureAwait(false);

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildGovernedOperationsFixture(FixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string scenarioName = scenario.ToString();

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Governed operations token fixture</title>
                <link rel="stylesheet" href="css/chatbot.tokens.css" />
                <style>{{css}}</style>
              </head>
              <body>
                <div class="chatbot-layout">
                  <a class="chatbot-skip-link" href="#chatbot-main-content">Skip to content</a>
                  <header class="chatbot-shell-header">
                    <span class="chatbot-shell-brand">Hexalith ChatBot</span>
                    <span class="chatbot-metadata">core operations</span>
                  </header>
                  <main id="chatbot-main-content" class="chatbot-shell-main" tabindex="-1">
                    <section class="chatbot-page" aria-labelledby="governed-operations-title">
                      <header class="chatbot-page-header">
                        <span class="chatbot-metadata">Governed command</span>
                        <h1 id="governed-operations-title" class="chatbot-page-title">Governed operations</h1>
                        <p class="chatbot-body">
                          Submit the trivial governed command end-to-end through the command spine. The surface origin
                          <code class="chatbot-code">ui</code> is declared at the boundary and travels into the audit trail.
                        </p>
                      </header>
                      <div class="chatbot-command-bar">
                        <button type="button">Record governed note</button>
                      </div>
                      <div id="fixture-status-root"></div>
                    </section>
                  </main>
                </div>
                <script>
                  const scenario = "{{scenarioName}}";
                  const root = document.querySelector("#fixture-status-root");
                  document.querySelector("button").addEventListener("click", () => {
                    window.__lastCommand = { commandType: "RecordGovernedNote", origin: "ui" };
                    if (scenario === "SubmitFails") {
                      root.innerHTML = `
                        <div class="chatbot-status"
                             data-chatbot-status="danger"
                             role="alert"
                             aria-label="Submission status: failed">
                          <span class="chatbot-status__label">Danger</span>
                          <span>Submission did not complete (code: <code class="chatbot-code">dependency_degraded</code>). You can try again.</span>
                        </div>`;
                      return;
                    }

                    root.innerHTML = `
                      <section class="chatbot-section" aria-labelledby="operation-outcome-title">
                        <h2 id="operation-outcome-title" class="chatbot-section-title">Outcome</h2>
                        <div class="chatbot-status-group" aria-label="Operation status summary">
                          <div class="chatbot-status"
                               data-chatbot-status="warning"
                               role="status"
                               aria-label="Projection status: pending">
                            <span class="chatbot-status__label">Warning</span>
                            <span>Projection is not complete (<code class="chatbot-code">AcceptedProjectionPending</code>).</span>
                          </div>
                          <div class="chatbot-status"
                               data-chatbot-status="success"
                               role="status"
                               aria-label="Audit status: committed">
                            <span class="chatbot-status__label">Success</span>
                            <span>Audit metadata is committed (<code class="chatbot-code">Committed</code>).</span>
                          </div>
                          <div class="chatbot-status"
                               data-chatbot-status="info"
                               role="status"
                               aria-label="Audit history: metadata only">
                            <span class="chatbot-status__label">Info</span>
                            <span>Audit history below is metadata-only.</span>
                          </div>
                        </div>
                        <dl class="chatbot-definition-list">
                          <dt>Operation</dt>
                          <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAX</code></dd>
                          <dt>Command</dt>
                          <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAV</code></dd>
                        </dl>
                        <h2 class="chatbot-section-title">Audit history (metadata-only)</h2>
                        <ul class="chatbot-audit-list">
                          <li><code class="chatbot-code">post-commit - allow/proposed - audit:Committed - origin:Ui - correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW</code></li>
                        </ul>
                      </section>`;
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static void AssertRuntimeTokenFoundationWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);

        app.ShouldContain("css/chatbot.tokens.css");
        css.ShouldContain("--chatbot-color-info-background: var(--colorStatusInformationBackground1);");
        css.ShouldContain("--chatbot-color-warning-foreground: var(--colorStatusWarningForeground1);");
        css.ShouldNotContain("--chatbot-color-info-background: #", Case.Insensitive);
        css.ShouldNotContain("--chatbot-color-info-background: rgb(", Case.Insensitive);
        css.ShouldNotContain("--chatbot-color-info-background: hsl(", Case.Insensitive);
        fixture.ShouldContain("<main id=\"chatbot-main-content\"");
        fixture.ShouldContain("Governed operations");
        fixture.ShouldContain("Record governed note");
    }

    private static void AssertCommandWorkflowWithoutBrowser()
    {
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);

        fixture.ShouldContain("window.__lastCommand = { commandType: \"RecordGovernedNote\", origin: \"ui\" };");
        fixture.ShouldContain("role=\"status\"");
        fixture.ShouldContain("aria-label=\"Projection status: pending\"");
        fixture.ShouldContain("aria-label=\"Audit status: committed\"");
        fixture.ShouldContain("aria-label=\"Audit history: metadata only\"");
        fixture.ShouldContain("data-chatbot-status=\"warning\"");
        fixture.ShouldContain("data-chatbot-status=\"success\"");
        fixture.ShouldContain("data-chatbot-status=\"info\"");
        fixture.ShouldContain("post-commit");
        fixture.ShouldContain("metadata-only");
    }

    private static void AssertBackendFailureWithoutBrowser()
    {
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.SubmitFails);

        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("aria-label=\"Submission status: failed\"");
        fixture.ShouldContain("data-chatbot-status=\"danger\"");
        fixture.ShouldContain("Submission did not complete");
        fixture.ShouldContain("You can try again.");
    }

    private static void AssertForcedColorsWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("CanvasText");
        css.ShouldContain("Highlight");
        css.ShouldContain(".chatbot-status__label");
        css.ShouldContain("border: 1px solid CanvasText");
        fixture.ShouldContain("<span class=\"chatbot-status__label\">Warning</span>");
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

        public static async Task<BrowserHarness> StartAsync(string chromeExecutable, bool forcedColors = false)
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

    private enum FixtureScenario
    {
        ProjectionPending,
        SubmitFails,
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
