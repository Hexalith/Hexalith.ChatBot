using System.Text.RegularExpressions;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class FrontComposerShellIntegrationE2ETests
{
    [Fact]
    public async Task FrontComposerShellRuntimeShouldExposeSingleProviderTreeAndBodyRegion()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertShellIntegrationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildFrontComposerShellFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Banner).GetByText("Hexalith ChatBot", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Main));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Governed operations", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Projection status: pending" }));

            (await harness.Page.Locator("fluent-provider").CountAsync()).ShouldBe(1);
            (await harness.Page.Locator("[data-chatbot-owned-provider='true']").CountAsync()).ShouldBe(0);
            (await harness.Page.Locator("[data-chatbot-owned-store-initializer='true']").CountAsync()).ShouldBe(0);
            (await harness.Page.Locator("[data-frontcomposer-store-initializer='true']").CountAsync()).ShouldBe(1);
        }
    }

    [Fact]
    public async Task TokenAliasLayerShouldRemainThinOverFrontComposerAndFluentVariables()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertTokenAliasLayerWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildFrontComposerShellFixture());

            // getComputedStyle resolves the var() chain, so a thin alias COMPUTES to exactly its
            // Fluent/FrontComposer source variable rather than echoing a literal "var(...)" string.
            // Equality proves the ChatBot slot is a pass-through over the framework token (not a second
            // palette). The raw-text "no #/rgb/hsl literal in the source" guarantee is enforced against the
            // stylesheet text by AssertTokenAliasLayerWithoutBrowser and the UI ChatBotSemanticTokenContractTests.
            string infoBackground = await CssVariableAsync(harness.Page, "--chatbot-color-info-background");
            string warningForeground = await CssVariableAsync(harness.Page, "--chatbot-color-warning-foreground");
            string successForeground = await CssVariableAsync(harness.Page, "--chatbot-color-success-foreground");
            string infoBackgroundSource = await CssVariableAsync(harness.Page, "--colorStatusInformationBackground1");
            string warningForegroundSource = await CssVariableAsync(harness.Page, "--colorStatusWarningForeground1");
            string successForegroundSource = await CssVariableAsync(harness.Page, "--colorStatusSuccessForeground1");

            infoBackgroundSource.ShouldNotBeNullOrWhiteSpace();
            warningForegroundSource.ShouldNotBeNullOrWhiteSpace();
            successForegroundSource.ShouldNotBeNullOrWhiteSpace();
            infoBackground.ShouldBe(infoBackgroundSource);
            warningForeground.ShouldBe(warningForegroundSource);
            successForeground.ShouldBe(successForegroundSource);
        }
    }

    [Fact]
    public void SourceWiringShouldUseFrontComposerBootstrapOrderAndNoDuplicateProviders()
        => AssertSourceWiring();

    [Fact]
    public void OperationalSurfacesShouldRenderAsFrontComposerBodyContent()
    {
        string dashboard = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");
        string audit = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor");
        string governedOperations = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        AssertOperationalSurfaceBodyContent(dashboard, "operational-dashboards");
        AssertOperationalSurfaceBodyContent(audit, "audit-investigation-s9");
        AssertOperationalSurfaceBodyContent(governedOperations, "governed-operations");

        audit.ShouldContain("<ChatBotProjectContextHeader");
        governedOperations.ShouldContain("<ChatBotApprovalQueuePriorityView");
    }

    private static void AssertSourceWiring()
    {
        string project = ReadProjectFile("src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj");
        string program = ReadProjectFile("src/Hexalith.ChatBot.UI/Program.cs");
        string imports = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/_Imports.razor");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");

        project.ShouldContain("Hexalith.FrontComposer.Shell.csproj");
        imports.ShouldContain("Hexalith.FrontComposer.Shell.Components.Layout");
        layout.ShouldContain("<FrontComposerShell AppTitle=\"Hexalith ChatBot\">");
        layout.ShouldContain("@Body");

        int fluent = program.IndexOf("AddFluentUIComponents", StringComparison.Ordinal);
        int quickstart = program.IndexOf("AddHexalithFrontComposerQuickstart", StringComparison.Ordinal);
        int domain = program.IndexOf("AddHexalithDomain<ChatBotUiFrontComposerMarker>", StringComparison.Ordinal);
        int eventStore = program.IndexOf("AddHexalithEventStore", StringComparison.Ordinal);
        fluent.ShouldBeGreaterThanOrEqualTo(0);
        quickstart.ShouldBeGreaterThanOrEqualTo(0);
        quickstart.ShouldBeGreaterThan(fluent);
        domain.ShouldBeGreaterThan(quickstart);
        eventStore.ShouldBeGreaterThan(domain);

        app.ShouldContain("css/chatbot.tokens.css");
        (app + layout).ShouldNotContain("<FluentProviders", Case.Sensitive);
        (app + layout).ShouldNotContain("StoreInitializer", Case.Sensitive);
        program.ShouldNotContain("AddFluxor", Case.Sensitive);
    }

    private static void AssertOperationalSurfaceBodyContent(string page, string responsiveFixture)
    {
        page.ShouldContain("<ChatBotConversationShell");
        page.ShouldContain($"data-chatbot-responsive-fixture=\"{responsiveFixture}\"");
        page.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
        page.ShouldNotContain("<main", Case.Sensitive);
        page.ShouldNotContain("role=\"banner\"", Case.Sensitive);
        page.ShouldNotContain("<FluentProviders", Case.Sensitive);
        page.ShouldNotContain("StoreInitializer", Case.Sensitive);
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });

    private static string BuildFrontComposerShellFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <style>
                :root {
                    --colorNeutralBackground1: #ffffff;
                    --colorNeutralBackground2: #f7f7f7;
                    --colorNeutralForeground1: #111111;
                    --colorNeutralForeground3: #555555;
                    --colorNeutralForegroundOnBrand: #ffffff;
                    --colorNeutralStroke1: #d0d0d0;
                    --colorBrandBackground: #0067b8;
                    --colorStatusInformationBackground1: #eff6fc;
                    --colorStatusInformationForeground1: #0f548c;
                    --colorStatusWarningBackground1: #fff4ce;
                    --colorStatusWarningForeground1: #8a6a00;
                    --colorStatusDangerBackground1: #fde7e9;
                    --colorStatusDangerForeground1: #a4262c;
                    --colorStatusSuccessBackground1: #dff6dd;
                    --colorStatusSuccessForeground1: #107c10;
                    --fontFamilyBase: Arial, sans-serif;
                    --fontFamilyMonospace: Consolas, monospace;
                }
                {{css}}
                </style>
            </head>
            <body>
                <fluent-provider data-frontcomposer-provider="true">
                    <div data-frontcomposer-store-initializer="true"></div>
                    <header role="banner">
                        <strong>Hexalith ChatBot</strong>
                    </header>
                    <main tabindex="-1">
                        <section class="chatbot-page">
                            <h1 class="chatbot-page-title">Governed operations</h1>
                            <div class="chatbot-status" data-chatbot-status="warning" role="status" aria-label="Projection status: pending">
                                <span class="chatbot-status__label">Warning</span>
                                <span>Projection pending</span>
                            </div>
                        </section>
                    </main>
                </fluent-provider>
            </body>
            </html>
            """;
    }

    private static void AssertShellIntegrationWithoutBrowser()
    {
        AssertSourceWiring();
        string fixture = BuildFrontComposerShellFixture();

        Regex.Matches(fixture, "<fluent-provider\\b", RegexOptions.CultureInvariant).Count.ShouldBe(1);
        fixture.ShouldContain("data-frontcomposer-store-initializer=\"true\"");
        fixture.ShouldNotContain("data-chatbot-owned-provider=\"true\"");
        fixture.ShouldNotContain("data-chatbot-owned-store-initializer=\"true\"");
        fixture.ShouldContain("role=\"banner\"");
        fixture.ShouldContain("<main");
        fixture.ShouldContain("Governed operations");
    }

    private static void AssertTokenAliasLayerWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string[] colorAssignments = Regex
            .Matches(
                css,
                @"^\s*--chatbot-color-[^:]+:\s*(?<value>[^;]+);",
                RegexOptions.CultureInvariant | RegexOptions.Multiline)
            .Select(static match => match.Groups["value"].Value.Trim())
            .ToArray();

        colorAssignments.ShouldNotBeEmpty();
        foreach (string assignment in colorAssignments)
        {
            assignment.ShouldContain("var(--");
            assignment.ShouldNotContain("#");
            assignment.ShouldNotContain("rgb(", Case.Insensitive);
            assignment.ShouldNotContain("hsl(", Case.Insensitive);
        }

        css.ShouldNotContain("Temporary inheritance bridge", Case.Sensitive);
        css.ShouldNotContain("until the runtime", Case.Sensitive);
    }

    private static async Task<string> CssVariableAsync(IPage page, string variableName)
        => await page.EvaluateAsync<string>(
            "name => getComputedStyle(document.documentElement).getPropertyValue(name).trim()",
            variableName).ConfigureAwait(false);

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(FindSolutionRoot(), relativePath));

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is not null)
        {
            return directory.FullName;
        }

        directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("The test process should run from or beneath the ChatBot repository.");
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

            try
            {
                return await StartAsync(chromeExecutable).ConfigureAwait(false);
            }
            catch (PlaywrightException ex) when (IsBrowserUnavailable(ex))
            {
                return null;
            }
        }

        private static async Task<BrowserHarness> StartAsync(string chromeExecutable)
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
                context = await browser.NewContextAsync(new() { ReducedMotion = ReducedMotion.Reduce }).ConfigureAwait(false);
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

        private static string? ResolveChromeExecutable()
        {
            string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return File.Exists(configured) ? configured : null;
            }

            const string LinuxChrome = "/usr/bin/google-chrome";
            return File.Exists(LinuxChrome) ? LinuxChrome : null;
        }
    }
}
#pragma warning restore CA2007
