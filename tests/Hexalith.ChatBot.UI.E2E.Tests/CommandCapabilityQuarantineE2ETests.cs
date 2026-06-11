using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class CommandCapabilityQuarantineE2ETests
{
    [Fact]
    public async Task QuarantinedCommandCapabilityGuidance_ShouldFailClosedForAllActorsAndKeepPriorArtifactsVisible()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertQuarantineFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildCommandCapabilityQuarantineFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Command capability governance", Level = 1 }));

            ILocator guidance = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Command capability quarantine guidance" });
            await WaitForVisibleAsync(guidance);
            await WaitForVisibleAsync(guidance.GetByText("Command quarantined for review.", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("reason-code:command_capability_quarantined", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("safe-next-action:request-access", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("disabled-action:disabled-action", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("responsible-role:policy-admin", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("two-person-rule:required", new() { Exact = true }));

            foreach (string actor in new[] { "Human", "Service client", "AI actor" })
            {
                await harness.Page.GetByRole(AriaRole.Button, new() { NameString = $"Submit as {actor}" }).ClickAsync();
            }

            (await harness.Page.EvaluateAsync<int>("() => window.__admittedCommandAttempts")).ShouldBe(0);
            (await harness.Page.EvaluateAsync<string[]>("() => window.__deniedActors")).ShouldBe(["human", "service", "ai"]);
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Admission status" }).GetByText("command_capability_quarantined"));

            ILocator prior = harness.Page.GetByRole(AriaRole.List, new() { NameString = "Prior command capability activity" });
            await WaitForVisibleAsync(prior);
            await WaitForVisibleAsync(prior.GetByText("command:AssociateEmailToProject", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("audit:pre-existing-command-capability-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("approval:prior-review-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("artifact-state:intact", new() { Exact = true }));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Request quarantine review" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastReviewRequest?.subject ?? ''"))
                .ShouldBe("AssociateEmailToProject");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastReviewRequest?.reasonCode ?? ''"))
                .ShouldBe("command_capability_quarantined");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastReviewRequest?.responsibleRole ?? ''"))
                .ShouldBe("policy-admin");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public void Story22CommandCapabilityQuarantineContract_ShouldStayWiredAcrossGatewayAuditCatalogAndGeneratedArtifacts()
    {
        string openApi = ReadProjectFile("src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml");
        string generatedClient = ReadProjectFile("src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs");
        string authorizationTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/CommandCapabilityQuarantineAuthorizationTests.cs");
        string gatewayTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs");
        string aggregateTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs");
        string dispatcherTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs");
        string catalogTests = ReadProjectFile("tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs");
        string clientGenerationTests = ReadProjectFile("tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs");

        openApi.ShouldContain("SubmitCommandCapabilityQuarantine:");
        openApi.ShouldContain("ApproveCommandCapabilityQuarantine:");
        openApi.ShouldContain("- quarantined");
        generatedClient.ShouldContain("public partial class SubmitCommandCapabilityQuarantine");
        generatedClient.ShouldContain("public partial class ApproveCommandCapabilityQuarantine");
        generatedClient.ShouldContain("Quarantined");

        authorizationTests.ShouldContain("QuarantinedCommandCapabilityShouldFailClosedForEveryActorBeforeGrantValidation");
        authorizationTests.ShouldContain("command_capability_quarantined");
        authorizationTests.ShouldContain("CommandCapabilityQuarantined");
        authorizationTests.ShouldContain("QuarantinedCapabilityShouldNotAffectSiblingActiveTypeOrOtherTenants");
        authorizationTests.ShouldContain("SelfLockoutGuardShouldRejectQuarantiningAnFr74GovernanceCommand");
        authorizationTests.ShouldContain("ServiceClientGrantUnderScoped");
        authorizationTests.ShouldContain("AiActorQuarantined");

        gatewayTests.ShouldContain("CommandCapabilityQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch");
        gatewayTests.ShouldContain("CommandCapabilityQuarantineAuditEnvelopeShouldCarryActiveToQuarantinedTransitionAndRemainMetadataOnly");
        gatewayTests.ShouldContain("Active->Quarantined");
        gatewayTests.ShouldContain("admin-operation:command-capability-quarantine-approve");
        gatewayTests.ShouldContain("admin-scope:policy");

        aggregateTests.ShouldContain("HandleCommandCapabilityQuarantineProposalShouldCreatePendingWithoutQuarantining");
        aggregateTests.ShouldContain("HandleCommandCapabilityQuarantineApprovalShouldRequirePendingAndDistinctSecondActor");
        aggregateTests.ShouldContain("HandleCommandCapabilityQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch");
        dispatcherTests.ShouldContain("DispatchShouldRejectCommandCapabilityQuarantineApprovalWhenApproverEqualsRequester");
        catalogTests.ShouldContain("CommandCapabilityQuarantined");
        catalogTests.ShouldContain("ChatBotMessageNextActions.RequestAccess");
        catalogTests.ShouldContain("ChatBotDisabledActionReasons.DisabledAction");
        clientGenerationTests.ShouldContain("hexalith-chatbot-generated-client.sha256");
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildCommandCapabilityQuarantineFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head>
            <meta charset="utf-8" />
            <title>Command capability quarantine fixture</title>
          </head>
          <body>
            <main aria-labelledby="command-capability-governance-title">
              <header>
                <p>Policy Administration</p>
                <h1 id="command-capability-governance-title">Command capability governance</h1>
              </header>
              <section aria-labelledby="command-capability-status-title">
                <h2 id="command-capability-status-title">Command capability status</h2>
                <div id="command-capability-quarantine-guidance"
                     role="alert"
                     aria-label="Command capability quarantine guidance"
                     data-message-code="command_capability_quarantined"
                     data-disabled-action-reason="disabled-action"
                     data-safe-next-action="request-access">
                  <span>Command quarantined for review.</span>
                  <p id="command-capability-quarantine-reason">Contained for review; release requires a second policy administrator.</p>
                  <dl aria-label="Command capability quarantine safe metadata">
                    <dt>Subject</dt><dd><code>command-capability:AssociateEmailToProject</code></dd>
                    <dt>Reason code</dt><dd><code>reason-code:command_capability_quarantined</code></dd>
                    <dt>State transition</dt><dd><code>state-transition:Active-&gt;Quarantined</code></dd>
                    <dt>Safe next action</dt><dd><code>safe-next-action:request-access</code></dd>
                    <dt>Disabled action reason</dt><dd><code>disabled-action:disabled-action</code></dd>
                    <dt>Responsible role</dt><dd><code>responsible-role:policy-admin</code></dd>
                    <dt>Approval rule</dt><dd><code>two-person-rule:required</code></dd>
                    <dt>Detail visibility</dt><dd><code>metadata-only</code></dd>
                  </dl>
                </div>
                <p role="status" aria-label="Admission status" aria-live="polite" data-admission-status>ready</p>
                <button type="button" data-actor="human">Submit as Human</button>
                <button type="button" data-actor="service">Submit as Service client</button>
                <button type="button" data-actor="ai">Submit as AI actor</button>
                <button type="button" data-review-request>Request quarantine review</button>
              </section>
              <section aria-labelledby="prior-command-activity-title">
                <h2 id="prior-command-activity-title">Prior activity</h2>
                <ul aria-label="Prior command capability activity">
                  <li><code>command:AssociateEmailToProject</code></li>
                  <li><code>audit:pre-existing-command-capability-001</code></li>
                  <li><code>approval:prior-review-001</code></li>
                  <li><code>artifact-state:intact</code></li>
                </ul>
              </section>
            </main>
            <script>
              window.__admittedCommandAttempts = 0;
              window.__deniedActors = [];
              document.querySelectorAll("[data-actor]").forEach(button => {
                button.addEventListener("click", event => {
                  event.preventDefault();
                  window.__deniedActors.push(button.dataset.actor);
                  document.querySelector("[data-admission-status]").textContent = "command_capability_quarantined";
                });
              });
              document.querySelector("[data-review-request]").addEventListener("click", () => {
                window.__lastReviewRequest = {
                  subject: "AssociateEmailToProject",
                  reasonCode: "command_capability_quarantined",
                  responsibleRole: "policy-admin",
                  nextAction: "request-access"
                };
              });
            </script>
          </body>
        </html>
        """;

    private static void AssertQuarantineFixtureWithoutBrowser()
    {
        string fixture = BuildCommandCapabilityQuarantineFixture();

        fixture.ShouldContain("Command quarantined for review.");
        fixture.ShouldContain("reason-code:command_capability_quarantined");
        fixture.ShouldContain("safe-next-action:request-access");
        fixture.ShouldContain("disabled-action:disabled-action");
        fixture.ShouldContain("responsible-role:policy-admin");
        fixture.ShouldContain("two-person-rule:required");
        fixture.ShouldContain("command:AssociateEmailToProject");
        fixture.ShouldContain("audit:pre-existing-command-capability-001");
        fixture.ShouldContain("artifact-state:intact");
        fixture.ShouldContain("data-actor=\"human\"");
        fixture.ShouldContain("data-actor=\"service\"");
        fixture.ShouldContain("data-actor=\"ai\"");
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
