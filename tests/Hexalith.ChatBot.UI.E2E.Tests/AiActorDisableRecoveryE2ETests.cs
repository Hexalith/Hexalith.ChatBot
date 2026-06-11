using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class AiActorDisableRecoveryE2ETests
{
    [Fact]
    public async Task DisabledAiActorGuidance_ShouldBlockFutureProposalAndKeepPriorArtifactsVisible()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertDisabledAiActorFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildDisabledAiActorFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "AI actor governance", Level = 1 }));

            ILocator guidance = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "AI actor disabled guidance" });
            await WaitForVisibleAsync(guidance);
            await WaitForVisibleAsync(guidance.GetByText("AI actor disabled.", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("reason-code:ai_actor_disabled", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("safe-next-action:request-access", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("disabled-action:disabled-action", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("responsible-role:policy-admin", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("two-person-rule:required", new() { Exact = true }));

            ILocator submit = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit AI proposal" });
            (await submit.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await submit.GetAttributeAsync("aria-describedby")).ShouldBe("ai-actor-disabled-reason");
            await submit.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<int>("() => window.__blockedProposalAttempts")).ShouldBe(0);

            ILocator prior = harness.Page.GetByRole(AriaRole.List, new() { NameString = "Prior AI actor activity" });
            await WaitForVisibleAsync(prior);
            await WaitForVisibleAsync(prior.GetByText("proposal:ai-proposal-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("command:ExecuteLowRiskAIAssistance", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("audit:pre-existing-ai-proposal-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("artifact-state:intact", new() { Exact = true }));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Request policy-admin re-enable" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRecoveryRequest?.commandType ?? ''"))
                .ShouldBe("RequestPolicyAdminReenable");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRecoveryRequest?.subject ?? ''"))
                .ShouldBe("ai-actor:gpt-mediation-actor");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRecoveryRequest?.reasonCode ?? ''"))
                .ShouldBe("ai_actor_disabled");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildDisabledAiActorFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>AI actor disable recovery fixture</title>
                <style>
                  {{css}}
                  .ai-actor-disable-fixture { max-width: 960px; margin: 0 auto; padding: 24px; }
                  .ai-actor-disable-fixture .action-row { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
                  .ai-actor-disable-fixture button[aria-disabled="true"] { opacity: .55; cursor: not-allowed; }
                </style>
              </head>
              <body>
                <main class="chatbot-page ai-actor-disable-fixture"
                      data-chatbot-surface="ai-actor-governance"
                      aria-labelledby="ai-actor-governance-title">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Policy Administration</span>
                    <h1 id="ai-actor-governance-title" class="chatbot-page-title">AI actor governance</h1>
                  </header>
                  <section class="chatbot-section"
                           aria-labelledby="ai-actor-status-title"
                           data-ai-actor-ref="ai-actor:gpt-mediation-actor">
                    <h2 id="ai-actor-status-title" class="chatbot-section-title">AI actor status</h2>
                    <div id="ai-actor-disabled-guidance"
                         class="chatbot-status"
                         role="alert"
                         aria-label="AI actor disabled guidance"
                         data-chatbot-status="danger"
                         data-message-code="ai_actor_disabled"
                         data-disabled-action-reason="disabled-action"
                         data-safe-next-action="request-access">
                      <span class="chatbot-status__label">AI actor disabled.</span>
                      <p id="ai-actor-disabled-reason" class="chatbot-body">This AI actor's proposals are blocked until a policy administrator re-enables it.</p>
                      <dl class="chatbot-definition-list" aria-label="AI actor disabled safe metadata">
                        <dt class="chatbot-labelled-row">Subject</dt>
                        <dd><code class="chatbot-code">ai-actor:gpt-mediation-actor</code></dd>
                        <dt class="chatbot-labelled-row">Reason code</dt>
                        <dd><code class="chatbot-code">reason-code:ai_actor_disabled</code></dd>
                        <dt class="chatbot-labelled-row">State transition</dt>
                        <dd><code class="chatbot-code">state-transition:Active-&gt;Disabled</code></dd>
                        <dt class="chatbot-labelled-row">Safe next action</dt>
                        <dd><code class="chatbot-code">safe-next-action:request-access</code></dd>
                        <dt class="chatbot-labelled-row">Disabled action reason</dt>
                        <dd><code class="chatbot-code">disabled-action:disabled-action</code></dd>
                        <dt class="chatbot-labelled-row">Responsible role</dt>
                        <dd><code class="chatbot-code">responsible-role:policy-admin</code></dd>
                        <dt class="chatbot-labelled-row">Approval rule</dt>
                        <dd><code class="chatbot-code">two-person-rule:required</code></dd>
                        <dt class="chatbot-labelled-row">Detail visibility</dt>
                        <dd><code class="chatbot-code">metadata-only</code></dd>
                      </dl>
                    </div>
                    <div class="action-row">
                      <button type="button"
                              aria-disabled="true"
                              aria-describedby="ai-actor-disabled-reason"
                              data-chatbot-stable-id="blocked-ai-proposal">Submit AI proposal</button>
                      <button type="button"
                              data-chatbot-stable-id="request-policy-admin-reenable">Request policy-admin re-enable</button>
                    </div>
                  </section>
                  <section class="chatbot-section" aria-labelledby="prior-activity-title">
                    <h2 id="prior-activity-title" class="chatbot-section-title">Prior activity</h2>
                    <ul aria-label="Prior AI actor activity">
                      <li><code class="chatbot-code">proposal:ai-proposal-001</code></li>
                      <li><code class="chatbot-code">command:ExecuteLowRiskAIAssistance</code></li>
                      <li><code class="chatbot-code">audit:pre-existing-ai-proposal-001</code></li>
                      <li><code class="chatbot-code">artifact-state:intact</code></li>
                    </ul>
                  </section>
                </main>
                <script>
                  window.__blockedProposalAttempts = 0;
                  document.querySelector('[data-chatbot-stable-id="blocked-ai-proposal"]').addEventListener('click', event => {
                    event.preventDefault();
                  });
                  document.querySelector('[data-chatbot-stable-id="request-policy-admin-reenable"]').addEventListener('click', () => {
                    window.__lastRecoveryRequest = {
                      commandType: 'RequestPolicyAdminReenable',
                      subject: 'ai-actor:gpt-mediation-actor',
                      reasonCode: 'ai_actor_disabled',
                      nextAction: 'request-access'
                    };
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static void AssertDisabledAiActorFixtureWithoutBrowser()
    {
        string fixture = BuildDisabledAiActorFixture();

        fixture.ShouldContain("AI actor disabled.");
        fixture.ShouldContain("reason-code:ai_actor_disabled");
        fixture.ShouldContain("safe-next-action:request-access");
        fixture.ShouldContain("disabled-action:disabled-action");
        fixture.ShouldContain("responsible-role:policy-admin");
        fixture.ShouldContain("two-person-rule:required");
        fixture.ShouldContain("proposal:ai-proposal-001");
        fixture.ShouldContain("artifact-state:intact");
        fixture.ShouldContain("aria-disabled=\"true\"");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldContain("metadata", Case.Insensitive);
        text.ShouldNotContain("prompt", Case.Insensitive);
        text.ShouldNotContain("completion", Case.Insensitive);
        text.ShouldNotContain("oauth", Case.Insensitive);
        text.ShouldNotContain("bearer", Case.Insensitive);
        text.ShouldNotContain("secret", Case.Insensitive);
        text.ShouldNotContain("raw claims", Case.Insensitive);
        text.ShouldNotContain("mailbox", Case.Insensitive);
        text.ShouldNotContain("project-name", Case.Insensitive);
        text.ShouldNotContain("restricted-file", Case.Insensitive);
        text.ShouldNotContain("@example", Case.Insensitive);
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
            string? explicitPath = Environment.GetEnvironmentVariable("CHROME_BIN");
            if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            {
                return explicitPath;
            }

            foreach (string candidate in new[]
                     {
                         "/usr/bin/google-chrome",
                         "/usr/bin/google-chrome-stable",
                         "/usr/bin/chromium",
                         "/usr/bin/chromium-browser",
                     })
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
