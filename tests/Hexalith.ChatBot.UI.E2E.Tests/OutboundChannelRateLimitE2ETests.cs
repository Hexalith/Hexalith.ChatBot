using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class OutboundChannelRateLimitE2ETests
{
    [Fact]
    public async Task RateLimitedOutboundChannelGuidance_ShouldHoldApprovedSendsAndKeepDraftsApprovalsVisible()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRateLimitFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildOutboundChannelRateLimitFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Outbound channel governance", Level = 1 }));

            ILocator guidance = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Outbound channel rate-limit guidance" });
            await WaitForVisibleAsync(guidance);
            await WaitForVisibleAsync(guidance.GetByText("Outbound channel rate limited.", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("reason-code:outbound_channel_rate_limited", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("safe-next-action:retry-later", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("disabled-action:dependency-degraded", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("budget:200", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("observed-window-count:200", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("throttled:true", new() { Exact = true }));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send approved draft through rate-limited channel" }).ClickAsync();
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Outbound send status" }).GetByText("outbound_channel_rate_limited"));
            (await harness.Page.EvaluateAsync<int>("() => window.__heldSendCount")).ShouldBe(1);
            (await harness.Page.EvaluateAsync<int>("() => window.__externalDispatchCount")).ShouldBe(0);

            foreach (string command in new[] { "Create draft", "Request approval", "Record approval" })
            {
                await harness.Page.GetByRole(AriaRole.Button, new() { NameString = command }).ClickAsync();
            }

            (await harness.Page.EvaluateAsync<string[]>("() => window.__inspectableWorkflowEvents"))
                .ShouldBe(["draft-created", "approval-requested", "approval-decision-recorded"]);
            (await harness.Page.EvaluateAsync<int>("() => window.__externalDispatchCount")).ShouldBe(0);

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send while under budget" }).ClickAsync();
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send through sibling channel" }).ClickAsync();
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send same channel in tenant beta" }).ClickAsync();
            (await harness.Page.EvaluateAsync<int>("() => window.__externalDispatchCount")).ShouldBe(3);

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Retry later" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastNextAction ?? ''"))
                .ShouldBe("retry-later");

            ILocator prior = harness.Page.GetByRole(AriaRole.List, new() { NameString = "Prior outbound channel activity" });
            await WaitForVisibleAsync(prior);
            await WaitForVisibleAsync(prior.GetByText("draft:prior-outbound-draft-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("approval:prior-send-approval-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("decision:prior-approval-decision-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("send:prior-succeeded-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("audit:pre-existing-outbound-channel-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("artifact-state:intact", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public void Story726OutboundChannelRateLimitContract_ShouldStayWiredAcrossGatewayAuditCatalogAndGeneratedArtifacts()
    {
        string openApi = ReadProjectFile("src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml");
        string generatedClient = ReadProjectFile("src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs");
        string authorizationTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/OutboundChannelRateLimitAuthorizationTests.cs");
        string admissionApiTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs");
        string gatewayTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs");
        string aggregateTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs");
        string dispatcherTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs");
        string catalogTests = ReadProjectFile("tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs");
        string clientGenerationTests = ReadProjectFile("tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs");

        openApi.ShouldContain("SubmitOutboundChannelRateLimit:");
        openApi.ShouldContain("OutboundChannelRateLimitWindow:");
        openApi.ShouldContain("rolling-hour");
        openApi.ShouldNotContain("ApproveOutboundChannelRateLimit");
        generatedClient.ShouldContain("public partial class SubmitOutboundChannelRateLimit");
        generatedClient.ShouldContain("public enum OutboundChannelRateLimitWindow");
        generatedClient.ShouldNotContain("ApproveOutboundChannelRateLimit");

        authorizationTests.ShouldContain("RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover");
        authorizationTests.ShouldContain("RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway");
        admissionApiTests.ShouldContain("CommandGatewayApi_ShouldAcceptOutboundChannelRateLimitAsSinglePolicyAdminMutationThroughUiSpine");
        gatewayTests.ShouldContain("OutboundChannelRateLimitAuditEnvelopeShouldCarryBudgetWindowAndRemainMetadataOnly");
        gatewayTests.ShouldContain("OutboundChannelRateLimitPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch");
        gatewayTests.ShouldContain("admin-operation:outbound-channel-rate-limit");
        gatewayTests.ShouldContain("admin-scope:policy");

        aggregateTests.ShouldContain("HandleOutboundChannelRateLimitShouldConfigureDirectlyWithoutPendingEvent");
        aggregateTests.ShouldContain("HandleOutboundChannelRateLimitShouldRejectOutOfBoundsBudget");
        aggregateTests.ShouldContain("HandleOutboundSendShouldFailClosedWithOutboundChannelRateLimitedReasonWhenChannelRateLimited");
        dispatcherTests.ShouldContain("DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelAtRateLimitBudget");
        dispatcherTests.ShouldContain("DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelRateLimited");
        dispatcherTests.ShouldContain("outbound_channel_rate_limited");
        catalogTests.ShouldContain("OutboundChannelRateLimited");
        catalogTests.ShouldContain("ChatBotMessageNextActions.RetryLater");
        catalogTests.ShouldContain("ChatBotDisabledActionReasons.DependencyDegraded");
        clientGenerationTests.ShouldContain("GeneratedClientShouldContainOutboundChannelRateLimitContractWithSafeMetadataOnly");
        clientGenerationTests.ShouldContain("hexalith-chatbot-generated-client.sha256");
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildOutboundChannelRateLimitFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head>
            <meta charset="utf-8" />
            <title>Outbound channel rate-limit fixture</title>
          </head>
          <body>
            <main aria-labelledby="outbound-channel-governance-title">
              <header>
                <p>Policy Administration</p>
                <h1 id="outbound-channel-governance-title">Outbound channel governance</h1>
              </header>
              <section aria-labelledby="outbound-channel-status-title">
                <h2 id="outbound-channel-status-title">Outbound channel status</h2>
                <div id="outbound-channel-rate-limit-guidance"
                     role="alert"
                     aria-label="Outbound channel rate-limit guidance"
                     data-message-code="outbound_channel_rate_limited"
                     data-disabled-action-reason="dependency-degraded"
                     data-safe-next-action="retry-later">
                  <span>Outbound channel rate limited.</span>
                  <p id="outbound-channel-rate-limit-reason">This channel's send capacity is temporarily limited to keep external volume within tenant policy; retry shortly.</p>
                  <dl aria-label="Outbound channel rate-limit safe metadata">
                    <dt>Subject</dt><dd><code>outbound-channel:adapter:mailbox-outbound</code></dd>
                    <dt>Reason code</dt><dd><code>reason-code:outbound_channel_rate_limited</code></dd>
                    <dt>Safe next action</dt><dd><code>safe-next-action:retry-later</code></dd>
                    <dt>Disabled action reason</dt><dd><code>disabled-action:dependency-degraded</code></dd>
                    <dt>Window</dt><dd><code>window:rolling-hour</code></dd>
                    <dt>Effective budget</dt><dd><code>budget:200</code></dd>
                    <dt>Observed count</dt><dd><code>observed-window-count:200</code></dd>
                    <dt>Throttled</dt><dd><code>throttled:true</code></dd>
                    <dt>Approval rule</dt><dd><code>two-person-rule:not-required</code></dd>
                    <dt>Detail visibility</dt><dd><code>metadata-only</code></dd>
                  </dl>
                </div>
                <p role="status" aria-label="Outbound send status" aria-live="polite" data-send-status>ready</p>
                <button type="button" data-send="limited">Send approved draft through rate-limited channel</button>
                <button type="button" data-send="under-budget">Send while under budget</button>
                <button type="button" data-send="sibling">Send through sibling channel</button>
                <button type="button" data-send="other-tenant">Send same channel in tenant beta</button>
                <button type="button" data-workflow="draft-created">Create draft</button>
                <button type="button" data-workflow="approval-requested">Request approval</button>
                <button type="button" data-workflow="approval-decision-recorded">Record approval</button>
                <button type="button" data-next-action>Retry later</button>
              </section>
              <section aria-labelledby="prior-outbound-activity-title">
                <h2 id="prior-outbound-activity-title">Prior outbound activity</h2>
                <ul aria-label="Prior outbound channel activity">
                  <li><code>draft:prior-outbound-draft-001</code></li>
                  <li><code>approval:prior-send-approval-001</code></li>
                  <li><code>decision:prior-approval-decision-001</code></li>
                  <li><code>send:prior-succeeded-001</code></li>
                  <li><code>audit:pre-existing-outbound-channel-001</code></li>
                  <li><code>artifact-state:intact</code></li>
                </ul>
              </section>
            </main>
            <script>
              window.__heldSendCount = 0;
              window.__externalDispatchCount = 0;
              window.__inspectableWorkflowEvents = [];
              document.querySelectorAll("[data-send]").forEach(button => {
                button.addEventListener("click", event => {
                  event.preventDefault();
                  if (button.dataset.send === "limited") {
                    window.__heldSendCount += 1;
                    document.querySelector("[data-send-status]").textContent = "outbound_channel_rate_limited";
                    return;
                  }

                  window.__externalDispatchCount += 1;
                  document.querySelector("[data-send-status]").textContent = "sent";
                });
              });
              document.querySelectorAll("[data-workflow]").forEach(button => {
                button.addEventListener("click", event => {
                  event.preventDefault();
                  window.__inspectableWorkflowEvents.push(button.dataset.workflow);
                });
              });
              document.querySelector("[data-next-action]").addEventListener("click", () => {
                window.__lastNextAction = "retry-later";
              });
            </script>
          </body>
        </html>
        """;

    private static void AssertRateLimitFixtureWithoutBrowser()
    {
        string fixture = BuildOutboundChannelRateLimitFixture();

        fixture.ShouldContain("Outbound channel rate limited.");
        fixture.ShouldContain("reason-code:outbound_channel_rate_limited");
        fixture.ShouldContain("safe-next-action:retry-later");
        fixture.ShouldContain("disabled-action:dependency-degraded");
        fixture.ShouldContain("window:rolling-hour");
        fixture.ShouldContain("budget:200");
        fixture.ShouldContain("observed-window-count:200");
        fixture.ShouldContain("throttled:true");
        fixture.ShouldContain("two-person-rule:not-required");
        fixture.ShouldContain("outbound-channel:adapter:mailbox-outbound");
        fixture.ShouldContain("draft:prior-outbound-draft-001");
        fixture.ShouldContain("approval:prior-send-approval-001");
        fixture.ShouldContain("decision:prior-approval-decision-001");
        fixture.ShouldContain("send:prior-succeeded-001");
        fixture.ShouldContain("audit:pre-existing-outbound-channel-001");
        fixture.ShouldContain("artifact-state:intact");
        fixture.ShouldContain("data-send=\"limited\"");
        fixture.ShouldContain("data-send=\"under-budget\"");
        fixture.ShouldContain("data-send=\"sibling\"");
        fixture.ShouldContain("data-send=\"other-tenant\"");
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
            foreach (string? candidate in new[]
                     {
                         Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH"),
                         "/usr/bin/chromium",
                         "/usr/bin/chromium-browser",
                         "/usr/bin/google-chrome",
                     })
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
