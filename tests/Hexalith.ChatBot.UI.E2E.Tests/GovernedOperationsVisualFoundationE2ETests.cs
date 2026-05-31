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

    [Fact]
    public async Task GovernedOperationsShouldReflowAcrossDesktopTabletAndPhoneWithoutLosingSafeMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertResponsiveFoundationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((int width, int height) in new[] { (1280, 900), (800, 900), (390, 844) })
            {
                await harness.Page.SetViewportSizeAsync(width, height);
                await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));
                await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

                await WaitForVisibleAsync(harness.Page.GetByText("Governed operations", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Operation", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Command", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Lifecycle state", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Completion status", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Audit status", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Safe next actions", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit history: metadata only" }));

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => {
                        const fixture = document.querySelector("[data-chatbot-responsive-fixture='governed-operations']");
                        const shellMain = document.querySelector(".chatbot-shell-main");
                        return document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                            || document.body.scrollWidth > document.body.clientWidth + 1
                            || (shellMain && shellMain.scrollWidth > shellMain.clientWidth + 1)
                            || (fixture && fixture.scrollWidth > fixture.clientWidth + 1);
                    }
                    """);
                hasHorizontalOverflow.ShouldBeFalse($"The governed operations fixture should not overflow at {width}px.");
            }
        }
    }

    [Fact]
    public async Task TouchTargetsShouldMeetPrimaryAndDenseMinimumsAtPhoneAndTabletWidths()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertTouchTargetsWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((int width, int height) in new[] { (390, 844), (800, 900) })
            {
                await harness.Page.SetViewportSizeAsync(width, height);
                await harness.Page.SetContentAsync(BuildInteractionGuardrailFixture());

                await AssertMinimumTargetSizeAsync(
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Retry quarantined operation" }),
                    44);
                await AssertMinimumTargetSizeAsync(
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Escalate governed operation" }),
                    44);
                await AssertMinimumTargetSizeAsync(
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Approve governed operation" }),
                    44);
                await AssertMinimumTargetSizeAsync(
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Delete governed operation" }),
                    44);
                await AssertMinimumTargetSizeAsync(
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Stop response generation" }),
                    44);

                await harness.Page.SetContentAsync(BuildGovernedPrimitiveFixture());
                await AssertMinimumTargetSizeAsync(
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Resolve actor" }),
                    24);
            }
        }
    }

    [Fact]
    public async Task GovernedPrimitivesShouldExposeAccessibleNonColorUserContracts()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertGovernedPrimitivesWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedPrimitiveFixture());

            await WaitForVisibleAsync(harness.Page.GetByLabel("Human user actor: Jerome"));
            await WaitForVisibleAsync(harness.Page.GetByLabel("MCP actor: Unresolved actor"));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Resolve actor" }));

            ILocator evidenceButton = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Available evidence: Audit correlation record" });
            await WaitForVisibleAsync(evidenceButton);
            await evidenceButton.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            await harness.Page.Keyboard.PressAsync("Space");
            int activationCount = await harness.Page.EvaluateAsync<int>("() => window.__evidenceOpenCount");
            activationCount.ShouldBe(2);

            ILocator redactedEvidence = harness.Page.GetByLabel("Evidence redacted: Supporting file. Evidence is redacted by policy.");
            await WaitForVisibleAsync(redactedEvidence);
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence is redacted by policy.", new() { Exact = true }));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool." }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Denied: The requested action is blocked by policy. Next action: Choose a lower-risk action." }));

            string html = await harness.Page.ContentAsync();
            html.ShouldNotContain("restricted-file.txt", Case.Insensitive);
            html.ShouldNotContain("Secret Project", Case.Insensitive);
        }
    }

    [Fact]
    public async Task GovernedActionShouldExposeReachableDisabledReasonWithoutHoverOnlyBehavior()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertGovernedActionGuardrailWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildInteractionGuardrailFixture());

            ILocator disabledAction = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Retry quarantined operation" });
            await WaitForVisibleAsync(disabledAction);
            (await disabledAction.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await disabledAction.GetAttributeAsync("aria-describedby")).ShouldBe("retry-disabled-reason");
            (await disabledAction.GetAttributeAsync("title")).ShouldBeNull();

            await disabledAction.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            int disabledActivationCount = await harness.Page.EvaluateAsync<int>("() => window.__disabledActivationCount");
            disabledActivationCount.ShouldBe(0);

            ILocator disabledReason = harness.Page.GetByLabel("Why unavailable? Quarantine review is required before retry.");
            await WaitForVisibleAsync(disabledReason);
            await disabledReason.FocusAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("retry-disabled-reason");

            ILocator enabledAction = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Escalate governed operation" });
            await enabledAction.ClickAsync();
            int enabledActivationCount = await harness.Page.EvaluateAsync<int>("() => window.__enabledActivationCount");
            enabledActivationCount.ShouldBe(1);

            string html = await harness.Page.ContentAsync();
            html.ShouldContain("data-chatbot-critical-action=\"true\"");
            html.ShouldNotContain("onmouseover", Case.Insensitive);
            html.ShouldNotContain("onmouseenter", Case.Insensitive);
        }
    }

    [Fact]
    public async Task StreamingStopControlShouldCancelAnnouncePolitelyAndReturnFocus()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertStreamingStopControlWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildInteractionGuardrailFixture());

            ILocator composer = harness.Page.GetByLabel("Governed response composer");
            ILocator stop = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Stop response generation" });
            await WaitForVisibleAsync(stop);
            (await stop.GetAttributeAsync("title")).ShouldBeNull();

            await stop.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");

            int stopActivationCount = await harness.Page.EvaluateAsync<int>("() => window.__stopActivationCount");
            stopActivationCount.ShouldBe(1);

            ILocator announcement = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Response stopped" });
            await WaitForVisibleAsync(announcement);
            (await announcement.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await announcement.TextContentAsync()).ShouldBe("Response stopped");
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("composer-target");

            ILocator idleStopRegion = harness.Page.Locator("[data-chatbot-stable-id='streaming-stop-idle']");
            (await idleStopRegion.GetAttributeAsync("data-chatbot-streaming")).ShouldBe("false");
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Stop idle response generation" }).CountAsync()).ShouldBe(0);
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("composer-target");
        }
    }

    private static async Task<string> CssVariableAsync(IPage page, string name)
        => await page.EvaluateAsync<string>(
                "token => getComputedStyle(document.documentElement).getPropertyValue(token).trim()",
                name)
            .ConfigureAwait(false);

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static async Task AssertMinimumTargetSizeAsync(ILocator locator, int minimumCssPixels)
    {
        await WaitForVisibleAsync(locator);
        float width = await locator.EvaluateAsync<float>("element => element.getBoundingClientRect().width");
        float height = await locator.EvaluateAsync<float>("element => element.getBoundingClientRect().height");

        width.ShouldBeGreaterThanOrEqualTo(minimumCssPixels);
        height.ShouldBeGreaterThanOrEqualTo(minimumCssPixels);
    }

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
                    <section class="chatbot-conversation-shell"
                             aria-label="Governed operations"
                             data-chatbot-responsive-fixture="governed-operations">
                      <div class="chatbot-conversation-shell__context">
                        <header class="chatbot-project-context-header" aria-label="Project context">
                          <div class="chatbot-project-context-header__identity">
                            <span class="chatbot-metadata">Project</span>
                            <h2 class="chatbot-project-context-header__title">Governed operations</h2>
                            <span class="chatbot-metadata"><code class="chatbot-code">m0-governed-command</code></span>
                          </div>
                          <div class="chatbot-project-context-header__meta" aria-label="Conversation context">
                            <span class="chatbot-metadata">Current surface</span>
                            <span>Operational command submission</span>
                          </div>
                          <div class="chatbot-status"
                               data-chatbot-status="info"
                               role="status"
                               aria-label="Project status: UI origin remains visible">
                            <span class="chatbot-status__label">Info</span>
                            <span>UI origin remains visible</span>
                          </div>
                        </header>
                      </div>
                      <div class="chatbot-conversation-shell__body">
                        <section class="chatbot-conversation-shell__main" role="region" aria-label="Governed command path">
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
                              <button type="button"
                                      class="chatbot-touch-target-primary"
                                      data-chatbot-touch-target="primary">Record governed note</button>
                            </div>
                            <div id="fixture-status-root"></div>
                          </section>
                        </section>
                      </div>
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
                          <dt class="chatbot-labelled-row">Operation</dt>
                          <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAX</code></dd>
                          <dt class="chatbot-labelled-row">Command</dt>
                          <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAV</code></dd>
                          <dt class="chatbot-labelled-row">Lifecycle state</dt>
                          <dd><code class="chatbot-code">Accepted</code></dd>
                          <dt class="chatbot-labelled-row">Completion status</dt>
                          <dd><code class="chatbot-code">AcceptedProjectionPending</code></dd>
                          <dt class="chatbot-labelled-row">Audit status</dt>
                          <dd><code class="chatbot-code">Committed</code></dd>
                          <dt class="chatbot-labelled-row">Safe next actions</dt>
                          <dd><code class="chatbot-code">Retry, inspect audit metadata, defer</code></dd>
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

    private static string BuildGovernedPrimitiveFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Governed primitive fixture</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-page" aria-labelledby="primitive-title">
                  <h1 id="primitive-title" class="chatbot-page-title">Governed primitive contracts</h1>
                  <section class="chatbot-section" aria-label="Actor badges">
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="HumanUser"
                          aria-label="Human user actor: Jerome">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">HU</span>
                      <span class="chatbot-actor-badge__category">Human user</span>
                      <span class="chatbot-actor-badge__label">Jerome</span>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="Mcp"
                          aria-label="MCP actor: Unresolved actor">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">MP</span>
                      <span class="chatbot-actor-badge__category">MCP</span>
                      <span class="chatbot-actor-badge__label">Unresolved actor</span>
                      <button class="chatbot-actor-badge__action" type="button">Resolve actor</button>
                    </span>
                  </section>
                  <section class="chatbot-section" aria-label="Evidence and risk chips">
                    <button class="chatbot-chip chatbot-chip--evidence"
                            type="button"
                            data-chatbot-evidence-state="Available"
                            aria-label="Available evidence: Audit correlation record"
                            aria-disabled="false">
                      <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                      <span class="chatbot-chip__label">Audit correlation record</span>
                      <span class="chatbot-chip__status">Available evidence</span>
                    </button>
                    <span class="chatbot-chip chatbot-chip--evidence"
                          data-chatbot-evidence-state="Redacted"
                          aria-label="Evidence redacted: Supporting file. Evidence is redacted by policy.">
                      <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                      <span class="chatbot-chip__label">Supporting file</span>
                      <span class="chatbot-chip__status">Evidence redacted</span>
                    </span>
                    <span class="chatbot-chip__reason">Evidence is redacted by policy.</span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="ToolInvoking"
                          role="status"
                          aria-label="Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Tool-invoking</span>
                      <span class="chatbot-chip__status">Requires approval before invoking an external tool.</span>
                    </span>
                  </section>
                  <section class="chatbot-blocked-state"
                           data-chatbot-blocked-reason="Denial"
                           data-chatbot-stable-id="policy-denial"
                           role="alert"
                           aria-label="Denied: The requested action is blocked by policy. Next action: Choose a lower-risk action.">
                    <div class="chatbot-blocked-state__heading">
                      <span class="chatbot-chip__cue" aria-hidden="true">BL</span>
                      <h2 class="chatbot-section-title">Denied</h2>
                    </div>
                    <p class="chatbot-body">The requested action is blocked by policy.</p>
                    <p class="chatbot-body"><strong>Next action:</strong> Choose a lower-risk action.</p>
                  </section>
                </main>
                <script>
                  window.__evidenceOpenCount = 0;
                  document.querySelector("button.chatbot-chip--evidence").addEventListener("click", () => {
                    window.__evidenceOpenCount += 1;
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static string BuildInteractionGuardrailFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string focusScript = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js");

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Interaction guardrail fixture</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-page" aria-labelledby="guardrail-title">
                  <h1 id="guardrail-title" class="chatbot-page-title">Interaction guardrails</h1>
                  <section class="chatbot-section" aria-label="Critical action guardrails">
                    <span class="chatbot-governed-action"
                          data-chatbot-critical-action="true"
                          data-chatbot-action-state="DisabledWithReason"
                          data-chatbot-stable-id="retry-quarantined-operation">
                      <button type="button"
                              aria-label="Retry quarantined operation"
                              aria-disabled="true"
                              aria-describedby="retry-disabled-reason">
                        Retry quarantined operation
                      </button>
                      <span id="retry-disabled-reason"
                            class="chatbot-governed-action__reason"
                            tabindex="0"
                            aria-label="Why unavailable? Quarantine review is required before retry.">
                        <strong>Why unavailable?</strong> Quarantine review is required before retry.
                      </span>
                    </span>
                    <span class="chatbot-governed-action"
                          data-chatbot-critical-action="true"
                          data-chatbot-action-state="Enabled"
                          data-chatbot-stable-id="escalate-governed-operation">
                      <button type="button"
                              aria-label="Escalate governed operation"
                              aria-disabled="false">
                        Escalate governed operation
                      </button>
                    </span>
                    <span class="chatbot-governed-action"
                          data-chatbot-critical-action="true"
                          data-chatbot-action-kind="Approval"
                          data-chatbot-action-state="Enabled"
                          data-chatbot-stable-id="approve-governed-operation">
                      <button type="button"
                              aria-label="Approve governed operation"
                              aria-disabled="false">
                        Approve governed operation
                      </button>
                    </span>
                    <span class="chatbot-governed-action"
                          data-chatbot-critical-action="true"
                          data-chatbot-action-kind="Destructive"
                          data-chatbot-action-state="Enabled"
                          data-chatbot-stable-id="delete-governed-operation">
                      <button type="button"
                              aria-label="Delete governed operation"
                              aria-disabled="false">
                        Delete governed operation
                      </button>
                    </span>
                  </section>
                  <section class="chatbot-section" aria-label="Streaming stop guardrail">
                    <textarea id="composer-target" aria-label="Governed response composer"></textarea>
                    <div class="chatbot-streaming-stop"
                         data-chatbot-streaming="true"
                         data-chatbot-stable-id="streaming-stop-active">
                      <button type="button" aria-label="Stop response generation">Stop response</button>
                      <span id="streaming-stop-active-announcement"
                            class="chatbot-visually-hidden"
                            role="status"
                            aria-live="polite"
                            aria-atomic="true"></span>
                    </div>
                    <div class="chatbot-streaming-stop"
                         data-chatbot-streaming="false"
                         data-chatbot-stable-id="streaming-stop-idle">
                      <span id="streaming-stop-idle-announcement"
                            class="chatbot-visually-hidden"
                            role="status"
                            aria-live="polite"
                            aria-atomic="true"></span>
                    </div>
                  </section>
                </main>
                <script>{{focusScript}}</script>
                <script>
                  window.__disabledActivationCount = 0;
                  window.__enabledActivationCount = 0;
                  window.__stopActivationCount = 0;

                  document.querySelector("[aria-label='Retry quarantined operation']").addEventListener("click", event => {
                    if (event.currentTarget.getAttribute("aria-disabled") === "true") {
                      event.preventDefault();
                      return;
                    }

                    window.__disabledActivationCount += 1;
                  });

                  document.querySelector("[aria-label='Escalate governed operation']").addEventListener("click", () => {
                    window.__enabledActivationCount += 1;
                  });

                  document.querySelector("[aria-label='Stop response generation']").addEventListener("click", () => {
                    window.__stopActivationCount += 1;
                    document.querySelector("#streaming-stop-active-announcement").textContent = "Response stopped";
                    window.HexalithChatBot.focusElementById("composer-target");
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
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        page.ShouldContain("<ChatBotConversationShell");
        page.ShouldContain("<ChatBotProjectContextHeader");
        page.ShouldContain("<ChatBotStatusBanner");
        page.ShouldNotContain("<div class=\"chatbot-status\"");
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

    private static void AssertResponsiveFoundationWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);

        css.ShouldContain("@media (max-width: 599px)");
        css.ShouldContain("@media (min-width: 600px)");
        css.ShouldContain("@media (min-width: 900px)");
        css.ShouldContain("overflow-wrap: anywhere;");
        css.ShouldNotContain("overflow-x: clip;");
        css.ShouldContain(".chatbot-definition-list");
        css.ShouldContain(".chatbot-labelled-row");

        fixture.ShouldContain("data-chatbot-responsive-fixture=\"governed-operations\"");
        fixture.ShouldContain("Project status: UI origin remains visible");
        fixture.ShouldContain("Lifecycle state");
        fixture.ShouldContain("Completion status");
        fixture.ShouldContain("Audit status");
        fixture.ShouldContain("Safe next actions");
        fixture.ShouldContain("metadata-only");
    }

    private static void AssertTouchTargetsWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string fixture = BuildInteractionGuardrailFixture();

        css.ShouldContain("--chatbot-touch-target-primary: 44px;");
        css.ShouldContain("--chatbot-touch-target-dense-secondary: 24px;");
        css.ShouldContain(".chatbot-touch-target-primary");
        css.ShouldContain(".chatbot-touch-target-dense-secondary");
        css.ShouldContain("min-inline-size: var(--chatbot-touch-target-primary);");
        css.ShouldContain("min-block-size: var(--chatbot-touch-target-primary);");

        fixture.ShouldContain("Retry quarantined operation");
        fixture.ShouldContain("Escalate governed operation");
        fixture.ShouldContain("Approve governed operation");
        fixture.ShouldContain("Delete governed operation");
        fixture.ShouldContain("Stop response generation");
        fixture.ShouldContain("data-chatbot-action-kind=\"Approval\"");
        fixture.ShouldContain("data-chatbot-action-kind=\"Destructive\"");
    }

    private static void AssertGovernedPrimitivesWithoutBrowser()
    {
        string fixture = BuildGovernedPrimitiveFixture();
        string evidence = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor");
        string blocked = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor");

        fixture.ShouldContain("aria-label=\"Human user actor: Jerome\"");
        fixture.ShouldContain("aria-label=\"MCP actor: Unresolved actor\"");
        fixture.ShouldContain("Resolve actor");
        fixture.ShouldContain("type=\"button\"");
        fixture.ShouldContain("aria-disabled=\"false\"");
        fixture.ShouldContain("Evidence is redacted by policy.");
        fixture.ShouldContain("role=\"status\"");
        fixture.ShouldContain("Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool.");
        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("Next action: Choose a lower-risk action.");
        fixture.ShouldNotContain("restricted-file.txt", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);

        evidence.ShouldContain("@onclick=\"ActivateAsync\"");
        evidence.ShouldNotContain("@onkeydown");
        blocked.ShouldContain("IsTerminalForCurrentUser ? \"alert\" : \"status\"");
    }

    private static void AssertGovernedActionGuardrailWithoutBrowser()
    {
        string fixture = BuildInteractionGuardrailFixture();
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor");

        fixture.ShouldContain("data-chatbot-critical-action=\"true\"");
        fixture.ShouldContain("aria-label=\"Retry quarantined operation\"");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("aria-describedby=\"retry-disabled-reason\"");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("Why unavailable? Quarantine review is required before retry.");
        fixture.ShouldNotContain("onmouseover", Case.Insensitive);
        fixture.ShouldNotContain("onmouseenter", Case.Insensitive);
        fixture.ShouldNotContain("title=", Case.Insensitive);

        component.ShouldContain("aria-disabled=\"@AriaDisabled\"");
        component.ShouldContain("aria-describedby=\"@ReasonReferenceId\"");
        component.ShouldContain("tabindex=\"0\"");
        component.ShouldContain("State is not ChatBotGovernedActionState.Enabled");
        component.ShouldNotContain("@onmouseover");
        component.ShouldNotContain("@onmouseenter");
    }

    private static void AssertStreamingStopControlWithoutBrowser()
    {
        string fixture = BuildInteractionGuardrailFixture();
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor");
        string focusScript = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js");

        fixture.ShouldContain("data-chatbot-streaming=\"true\"");
        fixture.ShouldContain("aria-label=\"Stop response generation\"");
        fixture.ShouldContain("role=\"status\"");
        fixture.ShouldContain("aria-live=\"polite\"");
        fixture.ShouldContain("Response stopped");
        fixture.ShouldContain("focusElementById(\"composer-target\")");
        fixture.ShouldContain("data-chatbot-streaming=\"false\"");
        fixture.ShouldNotContain("Stop idle response generation");

        component.ShouldContain("StopAnnouncement");
        component.ShouldContain("Response stopped");
        component.ShouldContain("FocusReturnTargetId");
        component.ShouldContain("role=\"status\"");
        component.ShouldContain("aria-live=\"polite\"");
        component.ShouldContain("LiveRegionMessage = string.Empty");
        component.ShouldContain("HexalithChatBot.focusElementById");
        focusScript.ShouldContain("document.getElementById");
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
