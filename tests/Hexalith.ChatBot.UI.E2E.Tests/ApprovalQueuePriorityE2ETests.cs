using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class ApprovalQueuePriorityE2ETests
{
    [Fact]
    public async Task ApprovalQueuePriority_GroupedPriorityWorkflow_KeepsBatchApproveDisabledWithReachableReason()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPriorityFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildApprovalQueuePriorityFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Prioritized approval queue", Level = 1 }));
            ILocator table = harness.Page.GetByRole(AriaRole.Table, new() { NameString = "Prioritized approval queue" });
            await WaitForVisibleAsync(table);
            (await table.Locator("tbody tr").CountAsync()).ShouldBe(3);

            string[] priorityOrder = await harness.Page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('[data-approval-group-row] [data-priority-label]')).map(e => e.textContent.trim())");
            priorityOrder.ShouldBe(["Critical", "High", "Low"]);

            ILocator approve = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Approve group sha256:priority-critical" });
            await WaitForVisibleAsync(approve);
            (await approve.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await approve.GetAttributeAsync("aria-describedby")).ShouldBe("approval-queue-batch-approve:sha256:priority-critical-disabled-reason");
            await approve.FocusAsync();
            (await approve.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            // The governed batch action is advisory-disabled (aria-disabled, not natively disabled), so it stays
            // focusable and reachable. A real user can still activate it, so force the click past Playwright's
            // actionability gate to prove activation triggers no batch fan-out command.
            await approve.ClickAsync(new() { Force = true });

            (await harness.Page.EvaluateAsync<string>("() => window.__lastApprovalBatch?.commandType ?? ''")).ShouldBeEmpty();
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Approval batch outcome" })
                .GetByText("idle", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task ApprovalQueuePriority_PartialAuthorityOutcome_FocusesStatusAndKeepsSafeReasonReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPartialAuthorityFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildApprovalQueuePriorityFixture());

            ILocator approve = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Approve group sha256:priority-critical" });
            await WaitForVisibleAsync(approve);
            (await approve.GetAttributeAsync("aria-describedby")).ShouldBe("approval-queue-batch-approve:sha256:priority-critical-disabled-reason");

            await approve.FocusAsync();
            (await approve.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

            // Advisory-disabled governed action: force past Playwright's actionability gate to prove a reachable
            // activation still records no batch fan-out command.
            await approve.ClickAsync(new() { Force = true });
            (await harness.Page.EvaluateAsync<string>("() => window.__lastApprovalBatch?.commandType ?? ''")).ShouldBeEmpty();

            ILocator reason = harness.Page.GetByRole(AriaRole.Note, new() { NameString = "Partial authority reason" });
            await WaitForVisibleAsync(reason);
            await WaitForVisibleAsync(reason.GetByText("Only items you are authorized to decide are acted on; the rest are handled individually with a safe reason.", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldContain("partial-authority");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task ApprovalQueuePriority_PhoneFallback_PreservesSafeSummaryAndHidesDenseControls()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPhoneFallbackFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildApprovalQueuePriorityFixture());

            ILocator fallback = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Prioritized approval summary is available on phone." });
            await WaitForVisibleAsync(fallback);
            await WaitForVisibleAsync(fallback.GetByText("Critical / requester:req-alpha / command:Project.AppendConversationMessage / project:redacted / 3", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("approval-queue-priority-draft-preserved", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("Dense batch approval controls are unavailable on this screen size.", new() { Exact = true }));

            ILocator denseTable = harness.Page.Locator("[data-approval-priority-table='true']");
            (await denseTable.IsVisibleAsync()).ShouldBeFalse();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildApprovalQueuePriorityFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Approval queue priority fixture</title>
                <style>
                  {{css}}
                  .approval-queue-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .approval-queue-actions { display: flex; gap: 12px; flex-wrap: wrap; }
                  .approval-queue-actions [role="button"] { min-height: 44px; }
                  .approval-phone-fallback { display: none; }
                  @media (max-width: 640px) {
                    [data-approval-priority-table="true"] { display: none !important; }
                    .approval-phone-fallback { display: block; }
                  }
                </style>
              </head>
              <body>
                <main class="chatbot-page approval-queue-fixture"
                      aria-labelledby="approval-queue-title"
                      data-chatbot-surface="approval-queue-priority-s7">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Approval queue S7.8</span>
                    <h1 id="approval-queue-title" class="chatbot-page-title">Prioritized approval queue</h1>
                  </header>
                  <section class="chatbot-section" aria-labelledby="approval-queue-title" data-small-screen-fallback="approval-queue-priority-draft-preserved">
                    <p id="approval-batch-status"
                       role="status"
                       aria-label="Approval batch outcome"
                       tabindex="-1">idle</p>
                    <ul id="approval-item-outcomes" aria-label="Approval item outcomes"></ul>
                    <table data-approval-priority-table="true" aria-label="Prioritized approval queue">
                      <thead>
                        <tr>
                          <th scope="col">Priority</th>
                          <th scope="col">Approval group</th>
                          <th scope="col">Requester</th>
                          <th scope="col">Command</th>
                          <th scope="col">Project</th>
                          <th scope="col">Items in group</th>
                          <th scope="col">Batch action</th>
                        </tr>
                      </thead>
                      <tbody>
                        {{GroupRow("sha256:priority-critical", "Critical", "risk:blocked|authority:send-on-behalf|age:7200s", "requester:req-alpha", "command:Project.AppendConversationMessage", "project:redacted", 3, true)}}
                        {{GroupRow("sha256:priority-high", "High", "risk:high|authority:shared-mailbox-send|age:1800s", "requester:req-bravo", "command:Project.LinkEvidence", "project:authorized", 2, false)}}
                        {{GroupRow("sha256:priority-low", "Low", "risk:low|authority:authenticated-user-send|age:60s", "requester:req-charlie", "command:Project.Comment", "project:authorized", 1, false)}}
                      </tbody>
                    </table>
                    <p id="approval-partial-authority-reason"
                       role="note"
                       aria-label="Partial authority reason">
                      <span>Only items you are authorized to decide are acted on; the rest are handled individually with a safe reason.</span>
                      <code class="chatbot-code">partial-authority</code>
                    </p>
                    <aside class="approval-phone-fallback"
                           role="complementary"
                           aria-label="Prioritized approval summary is available on phone.">
                      <p>Prioritized approval summary is available on phone.</p>
                      <p><code class="chatbot-code">Critical / requester:req-alpha / command:Project.AppendConversationMessage / project:redacted / 3</code></p>
                      <p><code class="chatbot-code">High / requester:req-bravo / command:Project.LinkEvidence / project:authorized / 2</code></p>
                      <p>approval-queue-priority-draft-preserved</p>
                      <p>Dense batch approval controls are unavailable on this screen size.</p>
                    </aside>
                  </section>
                </main>
                <script>window.__lastApprovalBatch = null;</script>
              </body>
            </html>
            """;
    }

    private static string GroupRow(
        string groupKey,
        string priority,
        string explanation,
        string requester,
        string command,
        string project,
        int count,
        bool partialAuthority)
        => $$"""
                        <tr data-approval-group-row="{{groupKey}}">
                          <td><span data-priority-label>{{priority}}</span><br /><code class="chatbot-code" data-approval-priority-explanation="{{groupKey}}">{{explanation}}</code></td>
                          <td><code class="chatbot-code">{{groupKey}}</code></td>
                          <td><code class="chatbot-code">{{requester}}</code></td>
                          <td><code class="chatbot-code">{{command}}</code></td>
                          <td><code class="chatbot-code">{{project}}</code></td>
                          <td><span data-approval-group-item-count="{{groupKey}}">{{count}}</span></td>
                          <td class="approval-queue-actions">
                            <span class="chatbot-governed-action"
                                  data-chatbot-critical-action="true"
                                  data-chatbot-action-state="DisabledWithReason"
                                  data-chatbot-touch-target="primary"
                                  data-chatbot-stable-id="approval-queue-batch-approve:{{groupKey}}">
                              <fluent-button role="button"
                                             tabindex="0"
                                             aria-label="Approve group {{groupKey}}"
                                             aria-disabled="true"
                                             aria-describedby="approval-queue-batch-approve:{{groupKey}}-disabled-reason">Approve group ({{count}})</fluent-button>
                              <span id="approval-queue-batch-approve:{{groupKey}}-disabled-reason"
                                    class="chatbot-governed-action__reason"
                                    tabindex="0"
                                    aria-label="Why unavailable? Batch approval fan-out is not enabled for this queue.">
                                <strong>Why unavailable?</strong> Batch approval fan-out is not enabled for this queue.
                              </span>
                            </span>
                          </td>
                        </tr>
            """;

    private static void AssertPriorityFixtureWithoutBrowser()
    {
        string fixture = BuildApprovalQueuePriorityFixture();
        fixture.ShouldContain("data-approval-priority-table=\"true\"");
        fixture.ShouldContain("sha256:priority-critical");
        fixture.ShouldContain("risk:blocked|authority:send-on-behalf|age:7200s");
        fixture.ShouldContain("data-chatbot-action-state=\"DisabledWithReason\"");
        fixture.ShouldContain("Batch approval fan-out is not enabled for this queue.");
        fixture.ShouldNotContain("ApprovalBatchDecisionFanOut");
        fixture.ShouldNotContain("DecideAiActionApproval");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPartialAuthorityFixtureWithoutBrowser()
    {
        string fixture = BuildApprovalQueuePriorityFixture();
        fixture.ShouldContain("aria-describedby=\"approval-queue-batch-approve:sha256:priority-critical-disabled-reason\"");
        fixture.ShouldContain("<fluent-button");
        fixture.ShouldContain("partial-authority");
        fixture.ShouldContain("Batch approval fan-out is not enabled for this queue.");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPhoneFallbackFixtureWithoutBrowser()
    {
        string fixture = BuildApprovalQueuePriorityFixture();
        fixture.ShouldContain("Prioritized approval summary is available on phone.");
        fixture.ShouldContain("approval-queue-priority-draft-preserved");
        fixture.ShouldContain("Dense batch approval controls are unavailable on this screen size.");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldNotContain("project-name", Case.Insensitive);
        text.ShouldNotContain("project content", Case.Insensitive);
        text.ShouldNotContain("provider payload", Case.Insensitive);
        text.ShouldNotContain("mailbox subject", Case.Insensitive);
        text.ShouldNotContain("recipient@example", Case.Insensitive);
        text.ShouldNotContain("bearer", Case.Insensitive);
        text.ShouldNotContain("secret", Case.Insensitive);
        text.ShouldNotContain("raw claims", Case.Insensitive);
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
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

        public static async Task<BrowserHarness> StartAsync(string chromeExecutable)
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
    }

}
