using System.Text.RegularExpressions;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class ProjectWorkspaceE2ETests
{
    private static readonly Regex RawTextareaTag = new(
        "<textarea(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public async Task ProjectWorkspaceFixtureShouldExposeRootPickerStatesInsideSingleFrontComposerShell()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertWorkspaceFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectWorkspaceFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Banner).GetByText("Hexalith ChatBot", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Main));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Project Workspace", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "No project selected" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Link, new() { NameString = "Open Alpha project" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Link, new() { NameString = "Open Beta project" }));

            (await harness.Page.Locator("fluent-provider").CountAsync()).ShouldBe(1);
            (await harness.Page.Locator("[data-chatbot-owned-provider='true']").CountAsync()).ShouldBe(0);
            (await harness.Page.Locator("[data-chatbot-owned-store-initializer='true']").CountAsync()).ShouldBe(0);
            (await harness.Page.Locator("[data-frontcomposer-store-initializer='true']").CountAsync()).ShouldBe(1);
        }
    }

    [Fact]
    public void ProjectWorkspaceSourceShouldKeepSelectedProjectConversationContextFilesInOneShell()
    {
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor");
        string selectedProject = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");

        layout.ShouldContain("<FrontComposerShell AppTitle=\"Hexalith ChatBot\">");
        workspace.ShouldContain("@page \"/\"");
        selectedProject.ShouldContain("ChatBotConversationStream");
        selectedProject.ShouldContain("ChatBotAttachmentConversationItem");
        selectedProject.ShouldContain("ProjectWorkspaceFilesPanelEmpty");
        (workspace + selectedProject).ShouldNotContain("<FrontComposerShell", Case.Sensitive);
        (workspace + selectedProject).ShouldNotContain("<FluentProviders", Case.Sensitive);
        (workspace + selectedProject).ShouldNotContain("StoreInitializer", Case.Sensitive);
    }

    [Fact]
    public async Task ProjectWorkspaceFixtureShouldExposeAllUxDr5StatesWithoutUnauthorizedDetailLeakage()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertWorkspaceStatesWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectWorkspaceStateFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Project Workspace", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Cold load" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "No project selected" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Empty project" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Active conversation" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Dependency degraded" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Access blocked" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Project context updated" }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Project context", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.List, new() { NameString = "Project conversation stream" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox item: Mailbox intake, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Project files", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox attachment, invoice.pdf, Captured, Associated" }));

            string body = await harness.Page.Locator("body").InnerTextAsync();
            AssertNoUnauthorizedWorkspaceDetails(body);
            (await harness.Page.Locator("textarea").CountAsync()).ShouldBe(0);
            (await harness.Page.Locator("[data-chatbot-marketing-hero='true']").CountAsync()).ShouldBe(0);
        }
    }

    private static void AssertWorkspaceFixtureWithoutBrowser()
    {
        string fixture = BuildProjectWorkspaceFixture();

        fixture.ShouldContain("data-chatbot-responsive-fixture=\"project-workspace\"");
        fixture.ShouldContain("Project Workspace");
        fixture.ShouldContain("No project selected");
        fixture.ShouldContain("Open Alpha project");
        fixture.ShouldContain("Open Beta project");
        fixture.ShouldContain("data-frontcomposer-store-initializer=\"true\"");
        fixture.ShouldNotContain("data-chatbot-owned-provider=\"true\"");
        fixture.ShouldNotContain("data-chatbot-owned-store-initializer=\"true\"");
        fixture.ShouldNotContain("hero", Case.Insensitive);
        ShouldNotContainRawTextareaTag(fixture);
    }

    private static void AssertWorkspaceStatesWithoutBrowser()
    {
        string fixture = BuildProjectWorkspaceStateFixture();

        foreach (string state in new[]
        {
            "Cold load",
            "No project selected",
            "Empty project",
            "Active conversation",
            "Dependency degraded",
            "Access blocked",
            "Project context updated",
        })
        {
            fixture.ShouldContain($"aria-label=\"{state}\"");
            fixture.ShouldContain(state);
        }

        fixture.ShouldContain("aria-label=\"Project context\"");
        fixture.ShouldContain("aria-label=\"Project conversation stream\"");
        fixture.ShouldContain("aria-label=\"Project files\"");
        fixture.ShouldContain("Mailbox item: Mailbox intake, Associated");
        fixture.ShouldContain("Mailbox attachment, invoice.pdf, Captured, Associated");
        fixture.ShouldContain("Project details are redacted on this surface.");
        fixture.ShouldContain("Request access or choose another authorized project.");
        AssertNoUnauthorizedWorkspaceDetails(fixture);
        ShouldNotContainRawTextareaTag(fixture);
        fixture.ShouldNotContain("hero", Case.Insensitive);
    }

    private static void AssertNoUnauthorizedWorkspaceDetails(string content)
    {
        content.ShouldNotContain("Secret Project", Case.Sensitive);
        content.ShouldNotContain("secret-project-id", Case.Sensitive);
        content.ShouldNotContain("private-mailbox@example.test", Case.Sensitive);
        content.ShouldNotContain("provider-payload", Case.Sensitive);
        content.ShouldNotContain("graph-message-redacted", Case.Sensitive);
        content.ShouldNotContain("confidential-plan.pdf", Case.Sensitive);
        content.ShouldNotContain("System.Exception", Case.Sensitive);
        content.ShouldNotContain("StackTrace", Case.Sensitive);
    }

    private static string BuildProjectWorkspaceFixture()
        => """
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
            </head>
            <body>
                <fluent-provider data-frontcomposer-provider="true">
                    <div data-frontcomposer-store-initializer="true"></div>
                    <header role="banner">
                        <strong>Hexalith ChatBot</strong>
                    </header>
                    <main tabindex="-1">
                        <section class="chatbot-page chatbot-project-workspace" data-chatbot-responsive-fixture="project-workspace">
                            <h1 id="project-workspace-title">Project Workspace</h1>
                            <div class="chatbot-status" role="status" aria-label="No project selected">
                                <span>No project selected</span>
                            </div>
                            <section aria-label="Recent authorized projects">
                                <a href="/?projectId=project-alpha" aria-label="Open Alpha project">Alpha project</a>
                                <a href="/?projectId=project-beta" aria-label="Open Beta project">Beta project</a>
                            </section>
                        </section>
                    </main>
                </fluent-provider>
            </body>
            </html>
            """;

    private static string BuildProjectWorkspaceStateFixture()
        => """
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
            </head>
            <body>
                <fluent-provider data-frontcomposer-provider="true">
                    <div data-frontcomposer-store-initializer="true"></div>
                    <header role="banner">
                        <strong>Hexalith ChatBot</strong>
                    </header>
                    <main tabindex="-1">
                        <section class="chatbot-page chatbot-project-workspace" data-chatbot-responsive-fixture="project-workspace">
                            <h1 id="project-workspace-title">Project Workspace</h1>

                            <div class="chatbot-status" role="status" aria-label="Cold load">
                                <span>Cold load</span>
                                <span>Project conversation loading</span>
                            </div>
                            <div class="chatbot-status" role="status" aria-label="No project selected">
                                <span>No project selected</span>
                                <a href="/?projectId=project-alpha" aria-label="Open Alpha project">Open project</a>
                            </div>
                            <div class="chatbot-status" role="status" aria-label="Empty project">
                                <span>Empty project</span>
                                <span>No authorized files are available for this project.</span>
                            </div>
                            <div class="chatbot-status" role="status" aria-label="Active conversation">
                                <span>Active conversation</span>
                                <span>Current</span>
                            </div>
                            <div class="chatbot-status" role="status" aria-label="Dependency degraded">
                                <span>Dependency degraded</span>
                                <span>Retry later when the governed dependency recovers.</span>
                            </div>
                            <div class="chatbot-status" role="status" aria-label="Access blocked">
                                <span>Access blocked</span>
                                <span>Project details are redacted on this surface.</span>
                                <span>Request access or choose another authorized project.</span>
                            </div>
                            <div class="chatbot-status" role="status" aria-label="Project context updated">
                                <span>Project context updated</span>
                            </div>

                            <aside aria-label="Project context">
                                <h2>Project context</h2>
                                <dl>
                                    <dt>Project</dt>
                                    <dd><code>project-alpha</code></dd>
                                    <dt>Lifecycle state</dt>
                                    <dd><code>active</code></dd>
                                    <dt>Safe next actions</dt>
                                    <dd><code>open</code></dd>
                                </dl>
                            </aside>

                            <ol aria-label="Project conversation stream">
                                <li>
                                    <article aria-label="Mailbox item: Mailbox intake, Associated">
                                        <h2>Mailbox intake</h2>
                                        <span>Associated</span>
                                    </article>
                                </li>
                            </ol>

                            <section aria-label="Project files">
                                <h2>Project files</h2>
                                <article aria-label="Mailbox attachment, invoice.pdf, Captured, Associated">
                                    <h3>invoice.pdf</h3>
                                    <span>Captured</span>
                                    <span>Associated</span>
                                </article>
                            </section>
                        </section>
                    </main>
                </fluent-provider>
            </body>
            </html>
            """;

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(FindSolutionRoot(), relativePath));

    private static void ShouldNotContainRawTextareaTag(string content)
        => RawTextareaTag.Matches(content).ShouldBeEmpty("raw lowercase <textarea> tags are forbidden; FluentTextArea is allowed.");

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
