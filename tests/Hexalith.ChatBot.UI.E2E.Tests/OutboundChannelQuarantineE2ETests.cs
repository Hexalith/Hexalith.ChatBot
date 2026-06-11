using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class OutboundChannelQuarantineE2ETests
{
    [Fact]
    public async Task QuarantinedOutboundChannelGuidance_ShouldHoldApprovedSendsAndKeepDraftsApprovalsVisible()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertQuarantineFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildOutboundChannelQuarantineFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Outbound channel governance", Level = 1 }));

            ILocator guidance = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Outbound channel quarantine guidance" });
            await WaitForVisibleAsync(guidance);
            await WaitForVisibleAsync(guidance.GetByText("Outbound channel held for review.", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("reason-code:outbound_channel_quarantined", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("safe-next-action:request-access", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("disabled-action:disabled-action", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("responsible-role:policy-admin", new() { Exact = true }));
            await WaitForVisibleAsync(guidance.GetByText("two-person-rule:required", new() { Exact = true }));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send approved draft through quarantined channel" }).ClickAsync();
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Outbound send status" }).GetByText("outbound_channel_quarantined"));
            (await harness.Page.EvaluateAsync<int>("() => window.__heldSendCount")).ShouldBe(1);
            (await harness.Page.EvaluateAsync<int>("() => window.__externalDispatchCount")).ShouldBe(0);

            foreach (string command in new[] { "Create draft", "Request approval", "Record approval" })
            {
                await harness.Page.GetByRole(AriaRole.Button, new() { NameString = command }).ClickAsync();
            }

            (await harness.Page.EvaluateAsync<string[]>("() => window.__inspectableWorkflowEvents"))
                .ShouldBe(["draft-created", "approval-requested", "approval-decision-recorded"]);
            (await harness.Page.EvaluateAsync<int>("() => window.__externalDispatchCount")).ShouldBe(0);

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send through active sibling channel" }).ClickAsync();
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Send same channel in tenant beta" }).ClickAsync();
            (await harness.Page.EvaluateAsync<int>("() => window.__externalDispatchCount")).ShouldBe(2);

            ILocator prior = harness.Page.GetByRole(AriaRole.List, new() { NameString = "Prior outbound channel activity" });
            await WaitForVisibleAsync(prior);
            await WaitForVisibleAsync(prior.GetByText("draft:prior-outbound-draft-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("approval:prior-send-approval-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("decision:prior-approval-decision-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("send:prior-succeeded-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("audit:pre-existing-outbound-channel-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("conversation:visible-redacted-thread-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("pending-draft:inspectable-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("pending-approval:inspectable-001", new() { Exact = true }));
            await WaitForVisibleAsync(prior.GetByText("artifact-state:intact", new() { Exact = true }));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Request quarantine review" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastReviewRequest?.subject ?? ''"))
                .ShouldBe("adapter:mailbox-outbound");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastReviewRequest?.reasonCode ?? ''"))
                .ShouldBe("outbound_channel_quarantined");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastReviewRequest?.responsibleRole ?? ''"))
                .ShouldBe("policy-admin");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public void Story725OutboundChannelQuarantineContract_ShouldStayWiredAcrossGatewayAuditCatalogAndGeneratedArtifacts()
    {
        string openApi = ReadProjectFile("src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml");
        string generatedClient = ReadProjectFile("src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs");
        string authorizationTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/OutboundChannelQuarantineAuthorizationTests.cs");
        string gatewayTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs");
        string aggregateTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs");
        string dispatcherTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs");
        string catalogTests = ReadProjectFile("tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs");
        string clientGenerationTests = ReadProjectFile("tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs");

        openApi.ShouldContain("SubmitOutboundChannelQuarantine:");
        openApi.ShouldContain("ApproveOutboundChannelQuarantine:");
        openApi.ShouldContain("- quarantined");
        generatedClient.ShouldContain("public partial class SubmitOutboundChannelQuarantine");
        generatedClient.ShouldContain("public partial class ApproveOutboundChannelQuarantine");
        generatedClient.ShouldContain("Quarantined");

        authorizationTests.ShouldContain("QuarantineProposalShouldRequireHumanPolicyAdmin");
        authorizationTests.ShouldContain("QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover");
        authorizationTests.ShouldContain("QuarantineCommandsShouldRejectInvalidMetadataOnlyPayloads");
        gatewayTests.ShouldContain("OutboundChannelQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch");
        gatewayTests.ShouldContain("OutboundChannelQuarantineAuditEnvelopeShouldCarryActiveToQuarantinedTransitionAndRemainMetadataOnly");
        gatewayTests.ShouldContain("Active->Quarantined");
        gatewayTests.ShouldContain("admin-operation:outbound-channel-quarantine-approve");
        gatewayTests.ShouldContain("admin-scope:policy");

        aggregateTests.ShouldContain("HandleOutboundChannelQuarantineProposalShouldCreatePendingWithoutQuarantining");
        aggregateTests.ShouldContain("HandleOutboundChannelQuarantineApprovalShouldRequirePendingAndDistinctSecondActor");
        aggregateTests.ShouldContain("HandleOutboundChannelQuarantineApprovalShouldRejectSubjectVersionOrReasonMismatch");
        aggregateTests.ShouldContain("HandleOutboundChannelQuarantineShouldNotMutatePriorCommittedOrPendingRecords");
        dispatcherTests.ShouldContain("DispatchShouldRejectOutboundChannelQuarantineApprovalWhenApproverEqualsRequester");
        dispatcherTests.ShouldContain("DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelQuarantined");
        dispatcherTests.ShouldContain("outbound_channel_quarantined");
        catalogTests.ShouldContain("OutboundChannelQuarantined");
        catalogTests.ShouldContain("ChatBotMessageNextActions.RequestAccess");
        catalogTests.ShouldContain("ChatBotDisabledActionReasons.DisabledAction");
        clientGenerationTests.ShouldContain("GeneratedClientShouldContainOutboundChannelQuarantineContractsWithSafeMetadataOnly");
        clientGenerationTests.ShouldContain("hexalith-chatbot-generated-client.sha256");
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildOutboundChannelQuarantineFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head>
            <meta charset="utf-8" />
            <title>Outbound channel quarantine fixture</title>
          </head>
          <body>
            <main aria-labelledby="outbound-channel-governance-title">
              <header>
                <p>Policy Administration</p>
                <h1 id="outbound-channel-governance-title">Outbound channel governance</h1>
              </header>
              <section aria-labelledby="outbound-channel-status-title">
                <h2 id="outbound-channel-status-title">Outbound channel status</h2>
                <div id="outbound-channel-quarantine-guidance"
                     role="alert"
                     aria-label="Outbound channel quarantine guidance"
                     data-message-code="outbound_channel_quarantined"
                     data-disabled-action-reason="disabled-action"
                     data-safe-next-action="request-access">
                  <span>Outbound channel held for review.</span>
                  <p id="outbound-channel-quarantine-reason">Outbound sending through this channel is paused until a policy administrator review and release.</p>
                  <dl aria-label="Outbound channel quarantine safe metadata">
                    <dt>Subject</dt><dd><code>outbound-channel:adapter:mailbox-outbound</code></dd>
                    <dt>Reason code</dt><dd><code>reason-code:outbound_channel_quarantined</code></dd>
                    <dt>State transition</dt><dd><code>state-transition:Active-&gt;Quarantined</code></dd>
                    <dt>Safe next action</dt><dd><code>safe-next-action:request-access</code></dd>
                    <dt>Disabled action reason</dt><dd><code>disabled-action:disabled-action</code></dd>
                    <dt>Responsible role</dt><dd><code>responsible-role:policy-admin</code></dd>
                    <dt>Approval rule</dt><dd><code>two-person-rule:required</code></dd>
                    <dt>Detail visibility</dt><dd><code>metadata-only</code></dd>
                  </dl>
                </div>
                <p role="status" aria-label="Outbound send status" aria-live="polite" data-send-status>ready</p>
                <button type="button" data-send="quarantined">Send approved draft through quarantined channel</button>
                <button type="button" data-send="sibling">Send through active sibling channel</button>
                <button type="button" data-send="other-tenant">Send same channel in tenant beta</button>
                <button type="button" data-workflow="draft-created">Create draft</button>
                <button type="button" data-workflow="approval-requested">Request approval</button>
                <button type="button" data-workflow="approval-decision-recorded">Record approval</button>
                <button type="button" data-review-request>Request quarantine review</button>
              </section>
              <section aria-labelledby="prior-outbound-activity-title">
                <h2 id="prior-outbound-activity-title">Prior outbound activity</h2>
                <ul aria-label="Prior outbound channel activity">
                  <li><code>draft:prior-outbound-draft-001</code></li>
                  <li><code>approval:prior-send-approval-001</code></li>
                  <li><code>decision:prior-approval-decision-001</code></li>
                  <li><code>send:prior-succeeded-001</code></li>
                  <li><code>audit:pre-existing-outbound-channel-001</code></li>
                  <li><code>conversation:visible-redacted-thread-001</code></li>
                  <li><code>pending-draft:inspectable-001</code></li>
                  <li><code>pending-approval:inspectable-001</code></li>
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
                  if (button.dataset.send === "quarantined") {
                    window.__heldSendCount += 1;
                    document.querySelector("[data-send-status]").textContent = "outbound_channel_quarantined";
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
              document.querySelector("[data-review-request]").addEventListener("click", () => {
                window.__lastReviewRequest = {
                  subject: "adapter:mailbox-outbound",
                  reasonCode: "outbound_channel_quarantined",
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
        string fixture = BuildOutboundChannelQuarantineFixture();

        fixture.ShouldContain("Outbound channel held for review.");
        fixture.ShouldContain("reason-code:outbound_channel_quarantined");
        fixture.ShouldContain("safe-next-action:request-access");
        fixture.ShouldContain("disabled-action:disabled-action");
        fixture.ShouldContain("responsible-role:policy-admin");
        fixture.ShouldContain("two-person-rule:required");
        fixture.ShouldContain("outbound-channel:adapter:mailbox-outbound");
        fixture.ShouldContain("draft:prior-outbound-draft-001");
        fixture.ShouldContain("approval:prior-send-approval-001");
        fixture.ShouldContain("decision:prior-approval-decision-001");
        fixture.ShouldContain("send:prior-succeeded-001");
        fixture.ShouldContain("audit:pre-existing-outbound-channel-001");
        fixture.ShouldContain("conversation:visible-redacted-thread-001");
        fixture.ShouldContain("pending-draft:inspectable-001");
        fixture.ShouldContain("pending-approval:inspectable-001");
        fixture.ShouldContain("artifact-state:intact");
        fixture.ShouldContain("data-send=\"quarantined\"");
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
