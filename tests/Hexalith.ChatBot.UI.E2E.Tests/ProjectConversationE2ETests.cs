using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class ProjectConversationE2ETests
{
    [Fact]
    public async Task ProjectConversationLoadingShouldExposePersistentProjectContext()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertLoadingWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Loading));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Project conversation", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Project conversation loading" }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Project context"));
            await WaitForVisibleAsync(harness.Page.GetByText("Authorized Project", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("tenant-alpha", new() { Exact = true }));
        }
    }

    [Fact]
    public async Task ProjectConversationPopulatedStreamShouldRenderOrderedMetadataOnlyItemsAndSystemDecisions()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPopulatedWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Project conversation status: current" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.List, new() { NameString = "Project conversation stream" }));
            ILocator mailboxItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox item: Mailbox intake, Associated" });
            await WaitForVisibleAsync(mailboxItem);
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "System decision: Association decision, Associated" }));
            await WaitForVisibleAsync(harness.Page.GetByText("System decision: Associate", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Source", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Microsoft 365 mailbox", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Mailbox", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("controlled-mailbox-001", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Provider message ID", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("graph-message-001", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Internet message ID", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("<internet-message-001@example.test>", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Thread", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("graph-thread-001", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Sent", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Created", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Source timezone", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Correlation ID", new() { Exact = true }));
            await AssertAssociatedEmailMetadataAsync(mailboxItem);

            IReadOnlyList<string> itemIds = await harness.Page
                .Locator("[data-chatbot-conversation-item-id]")
                .EvaluateAllAsync<string[]>("items => items.map(item => item.getAttribute('data-chatbot-conversation-item-id'))");
            itemIds.ShouldBe(["01HZXMAILBOX000000000000001", "01HZXDECISION0000000000001"]);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
        }
    }

    [Fact]
    public async Task ProjectConversationPopulatedStreamShouldRespectMotionForcedColorsAndPhoneLayout()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertPopulatedAccessibilityModesWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated));

            ILocator mailboxItem = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Mailbox item: Mailbox intake, Associated" });
            await WaitForVisibleAsync(mailboxItem);

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

            string animationName = await mailboxItem.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string transitionDuration = await mailboxItem.EvaluateAsync<string>("element => getComputedStyle(element).transitionDuration");
            string headerDirection = await mailboxItem.Locator("header").EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
            animationName.ShouldBe("none");
            transitionDuration.ShouldContain("0.01ms");
            headerDirection.ShouldBe("column");

            LocatorBoundingBoxResult? box = await mailboxItem.BoundingBoxAsync();
            box.ShouldNotBeNull();
            box.Width.ShouldBeLessThanOrEqualTo(390);
        }
    }

    [Fact]
    public async Task ProjectConversationEmptyStateShouldKeepSafeNextActionReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertEmptyWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Empty));

            ILocator emptyState = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: No email-derived context is available. Next action: Wait for associated email." });
            await WaitForVisibleAsync(emptyState);
            await WaitForVisibleAsync(harness.Page.GetByText("Wait for associated email.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Project conversation metadata" }));
        }
    }

    [Fact]
    public async Task ProjectConversationUnauthorizedStateShouldStayRedactedAndIndistinguishable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertUnauthorizedWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildProjectConversationFixture(ProjectConversationFixtureScenario.Unauthorized));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: Project conversation is unavailable. Next action: Verify access or choose an authorized project." }));
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence restricted", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("project-redacted", new() { Exact = true }));
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnlyBody(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static async Task AssertAssociatedEmailMetadataAsync(ILocator mailboxItem)
    {
        (await mailboxItem.GetAttributeAsync("tabindex")).ShouldBe("0");
        await mailboxItem.FocusAsync();
        (await mailboxItem.EvaluateAsync<bool>("element => document.activeElement === element")).ShouldBeTrue();

        IReadOnlyList<string> labels = await mailboxItem.Locator("dt").AllTextContentsAsync();
        labels.Select(static label => label.Trim()).ShouldBe(
            [
                "Source",
                "Mailbox",
                "Provider message ID",
                "Internet message ID",
                "Operation",
                "Conversation context",
                "Thread",
                "Project",
                "Lifecycle state",
                "Confidence",
                "Threshold band",
                "Safe next actions",
                "Received",
                "Sent",
                "Created",
                "Source timezone",
                "Correlation ID",
            ],
            ignoreOrder: false);

        string text = await mailboxItem.InnerTextAsync();
        AssertTextOrder(
            text,
            "Mailbox intake",
            "2026-06-01 08:00:00Z",
            "Source",
            "Microsoft 365 mailbox",
            "Mailbox",
            "controlled-mailbox-001",
            "Provider message ID",
            "graph-message-001",
            "Internet message ID",
            "<internet-message-001@example.test>",
            "Operation",
            "01HZXASSOC000000000000001",
            "Conversation context",
            "graph-conversation-001",
            "Thread",
            "graph-thread-001",
            "Project",
            "project-alpha",
            "Lifecycle state",
            "Associated",
            "Confidence",
            "91%",
            "Threshold band",
            "Auto",
            "Safe next actions",
            "none",
            "Received",
            "2026-06-01 08:00:00Z",
            "Sent",
            "2026-06-01 07:58:00Z",
            "Created",
            "2026-06-01 07:57:00Z",
            "Source timezone",
            "UTC",
            "Correlation ID",
            "01HZXCORRELATION00000000001",
            "m365-mailbox-intake",
            "metadata_only",
            "91%");
    }

    private static void AssertTextOrder(string text, params string[] expected)
    {
        int previous = -1;
        foreach (string marker in expected)
        {
            int current = text.IndexOf(marker, previous + 1, StringComparison.Ordinal);
            current.ShouldBeGreaterThan(previous, $"Expected '{marker}' to appear after the previous metadata marker.");
            previous = current;
        }
    }

    private static string BuildProjectConversationFixture(ProjectConversationFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string body = scenario switch
        {
            ProjectConversationFixtureScenario.Loading => BuildLoadingBody(),
            ProjectConversationFixtureScenario.Populated => BuildPopulatedBody(),
            ProjectConversationFixtureScenario.Empty => BuildEmptyBody(),
            ProjectConversationFixtureScenario.Unauthorized => BuildUnauthorizedBody(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Project conversation</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main id="chatbot-main-content" class="chatbot-shell-main" tabindex="-1">
                  <section class="chatbot-conversation-shell"
                           aria-label="Project conversation"
                           data-chatbot-responsive-fixture="project-conversation">
                    <div class="chatbot-conversation-shell__context">
                      <header class="chatbot-project-context-header" aria-label="Project context">
                        <div class="chatbot-project-context-header__identity">
                          <span class="chatbot-metadata">S1</span>
                          <h2 class="chatbot-project-context-header__title">Authorized Project</h2>
                          <span class="chatbot-metadata"><code class="chatbot-code">project-alpha</code></span>
                        </div>
                        <div class="chatbot-project-context-header__meta" aria-label="Conversation context">
                          <span class="chatbot-metadata">Tenant</span>
                          <span>tenant-alpha</span>
                        </div>
                        <div class="chatbot-status"
                             data-chatbot-status="info"
                             role="status"
                             aria-live="off"
                             aria-label="Project conversation status: current">
                          <span class="chatbot-status__label">Info</span>
                          <span>Current</span>
                        </div>
                      </header>
                    </div>
                    <div class="chatbot-conversation-shell__body">
                      <section class="chatbot-conversation-shell__main" role="region" aria-label="Project conversation stream">
                        <section class="chatbot-page chatbot-project-conversation"
                                 aria-labelledby="project-conversation-title"
                                 data-chatbot-responsive-fixture="project-conversation">
                          <header class="chatbot-page-header">
                            <span class="chatbot-metadata">S1</span>
                            <h1 id="project-conversation-title" class="chatbot-page-title">Project conversation</h1>
                          </header>
                          {{body}}
                        </section>
                      </section>
                      <aside class="chatbot-conversation-shell__panel"
                             role="complementary"
                             aria-label="Project conversation metadata">
                        <section class="chatbot-section" aria-labelledby="project-conversation-metadata-title">
                          <h2 id="project-conversation-metadata-title" class="chatbot-section-title">Project conversation metadata</h2>
                          <dl class="chatbot-definition-list chatbot-labelled-row-list">
                            <dt class="chatbot-labelled-row">Project</dt>
                            <dd><code class="chatbot-code">project-alpha</code></dd>
                            <dt class="chatbot-labelled-row">Lifecycle state</dt>
                            <dd><code class="chatbot-code">Associated</code></dd>
                            <dt class="chatbot-labelled-row">Safe next actions</dt>
                            <dd><code class="chatbot-code">none</code></dd>
                            <dt class="chatbot-labelled-row">Source metadata</dt>
                            <dd><code class="chatbot-code">m365-mailbox-intake - chatbot.project-conversation-response.v1</code></dd>
                          </dl>
                        </section>
                      </aside>
                    </div>
                  </section>
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildLoadingBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 data-chatbot-feedback-state="LoadingColdLoad"
                 role="status"
                 aria-live="polite"
                 aria-label="Project conversation loading">
              <span class="chatbot-status__label">Info</span>
              <span>Loading project conversation.</span>
            </div>
            """;

    private static string BuildPopulatedBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="info"
                 data-chatbot-feedback-state="CurrentUserAiProposalReady"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation status: current">
              <span class="chatbot-status__label">Info</span>
              <span>Current</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <ol class="chatbot-conversation-stream__list" aria-label="Project conversation stream">
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-email-conversation-item"
                           data-chatbot-conversation-item-kind="EmailDerived"
                           data-chatbot-conversation-item-id="01HZXMAILBOX000000000000001"
                           tabindex="0"
                           aria-label="Mailbox item: Mailbox intake, Associated">
                    <header class="chatbot-email-conversation-item__header">
                      <span class="chatbot-actor-badge" aria-label="Mailbox actor: Mailbox intake">Mailbox intake</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:00:00.0000000Z">2026-06-01 08:00:00Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-email-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Source</dt>
                      <dd><code class="chatbot-code">Microsoft 365 mailbox</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Provider message ID</dt>
                      <dd><code class="chatbot-code">graph-message-001</code></dd>
                      <dt class="chatbot-labelled-row">Internet message ID</dt>
                      <dd><code class="chatbot-code">&lt;internet-message-001@example.test&gt;</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Thread</dt>
                      <dd><code class="chatbot-code">graph-thread-001</code></dd>
                      <dt class="chatbot-labelled-row">Project</dt>
                      <dd><code class="chatbot-code">project-alpha</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Confidence</dt>
                      <dd><code class="chatbot-code">91%</code></dd>
                      <dt class="chatbot-labelled-row">Threshold band</dt>
                      <dd><code class="chatbot-code">Auto</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Received</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T08:00:00.0000000Z">2026-06-01 08:00:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Sent</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T07:58:00.0000000Z">2026-06-01 07:58:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Created</dt>
                      <dd><time class="chatbot-code" datetime="2026-06-01T07:57:00.0000000Z">2026-06-01 07:57:00Z</time></dd>
                      <dt class="chatbot-labelled-row">Source timezone</dt>
                      <dd><code class="chatbot-code">UTC</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000001</code></dd>
                    </dl>
                    <div class="chatbot-email-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">m365-mailbox-intake</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">metadata_only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span>
                    </div>
                  </article>
                </li>
                <li class="chatbot-conversation-stream__entry">
                  <article class="chatbot-email-conversation-item"
                           data-chatbot-conversation-item-kind="SystemDecision"
                           data-chatbot-conversation-item-id="01HZXDECISION0000000000001"
                           tabindex="0"
                           aria-label="System decision: Association decision, Associated">
                    <header class="chatbot-email-conversation-item__header">
                      <span class="chatbot-actor-badge" aria-label="System decision actor: Association decision">Association decision</span>
                      <span class="chatbot-email-conversation-item__decision">System decision: Associate</span>
                      <time class="chatbot-metadata" datetime="2026-06-01T08:02:00.0000000Z">2026-06-01 08:02:00Z</time>
                    </header>
                    <dl class="chatbot-definition-list chatbot-email-conversation-item__metadata">
                      <dt class="chatbot-labelled-row">Source</dt>
                      <dd><code class="chatbot-code">Microsoft 365 mailbox</code></dd>
                      <dt class="chatbot-labelled-row">Mailbox</dt>
                      <dd><code class="chatbot-code">controlled-mailbox-001</code></dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01HZXASSOC000000000000001</code></dd>
                      <dt class="chatbot-labelled-row">Conversation context</dt>
                      <dd><code class="chatbot-code">graph-conversation-001</code></dd>
                      <dt class="chatbot-labelled-row">Lifecycle state</dt>
                      <dd><code class="chatbot-code">Associated</code></dd>
                      <dt class="chatbot-labelled-row">Confidence</dt>
                      <dd><code class="chatbot-code">91%</code></dd>
                      <dt class="chatbot-labelled-row">Threshold band</dt>
                      <dd><code class="chatbot-code">Auto</code></dd>
                      <dt class="chatbot-labelled-row">Safe next actions</dt>
                      <dd><code class="chatbot-code">none</code></dd>
                      <dt class="chatbot-labelled-row">Correlation ID</dt>
                      <dd><code class="chatbot-code">01HZXCORRELATION00000000002</code></dd>
                    </dl>
                    <div class="chatbot-email-conversation-item__chips" aria-label="Project conversation metadata">
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">m365-mailbox-intake</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Redacted">metadata_only</span>
                      <span class="chatbot-chip chatbot-chip--evidence" data-chatbot-evidence-state="Available">91%</span>
                    </div>
                  </article>
                </li>
              </ol>
            </section>
            """;

    private static string BuildEmptyBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="warning"
                 data-chatbot-feedback-state="BlockedAction"
                 role="status"
                 aria-live="off"
                 aria-label="Project conversation status: empty">
              <span class="chatbot-status__label">Warning</span>
              <span>Empty</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <div class="chatbot-blocked-state"
                   data-chatbot-feedback-state="BlockedAction"
                   role="alert"
                   aria-live="polite"
                   aria-label="Blocked: No email-derived context is available. Next action: Wait for associated email.">
                <span class="chatbot-status__label">Blocked</span>
                <p>No email-derived context is available.</p>
                <p>Wait for associated email.</p>
              </div>
            </section>
            """;

    private static string BuildUnauthorizedBody()
        => """
            <div class="chatbot-status"
                 data-chatbot-status="warning"
                 data-chatbot-feedback-state="BlockedAction"
                 role="alert"
                 aria-live="polite"
                 aria-label="Blocked: Project conversation is unavailable. Next action: Verify access or choose an authorized project.">
              <span class="chatbot-status__label">Warning</span>
              <span>Project conversation is unavailable.</span>
              <span>Verify access or choose an authorized project.</span>
            </div>
            <section class="chatbot-conversation-stream"
                     aria-labelledby="project-conversation-stream-title"
                     data-chatbot-conversation-stream="metadata-only">
              <h2 id="project-conversation-stream-title" class="chatbot-section-title">Project conversation stream</h2>
              <article class="chatbot-email-conversation-item"
                       data-chatbot-conversation-item-kind="Redacted"
                       data-chatbot-conversation-item-id="project-redacted"
                       aria-label="Redacted project conversation item">
                <header class="chatbot-email-conversation-item__header">
                  <span class="chatbot-actor-badge" aria-label="Mailbox actor: redacted">Mailbox event</span>
                  <span class="chatbot-email-conversation-item__decision">Evidence restricted</span>
                </header>
                <dl class="chatbot-definition-list chatbot-email-conversation-item__metadata">
                  <dt class="chatbot-labelled-row">Project</dt>
                  <dd><code class="chatbot-code">project-redacted</code></dd>
                  <dt class="chatbot-labelled-row">Lifecycle state</dt>
                  <dd><code class="chatbot-code">Blocked</code></dd>
                  <dt class="chatbot-labelled-row">Safe next actions</dt>
                  <dd><code class="chatbot-code">verify-access</code></dd>
                </dl>
              </article>
            </section>
            """;

    private static void AssertLoadingWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Loading);
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");

        page.ShouldContain("<ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotUiTextKey.ProjectConversationLoading");
        fixture.ShouldContain("aria-label=\"Project conversation loading\"");
        fixture.ShouldContain("aria-label=\"Project context\"");
        fixture.ShouldContain("tenant-alpha");
    }

    private static void AssertPopulatedWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");
        string item = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor");

        stream.ShouldContain("data-chatbot-conversation-stream=\"metadata-only\"");
        item.ShouldContain("ProjectConversationSystemDecision");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"01HZXMAILBOX000000000000001\"");
        fixture.ShouldContain("data-chatbot-conversation-item-id=\"01HZXDECISION0000000000001\"");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("Source");
        fixture.ShouldContain("Microsoft 365 mailbox");
        fixture.ShouldContain("Mailbox");
        fixture.ShouldContain("controlled-mailbox-001");
        fixture.ShouldContain("Provider message ID");
        fixture.ShouldContain("graph-message-001");
        fixture.ShouldContain("Internet message ID");
        fixture.ShouldContain("&lt;internet-message-001@example.test&gt;");
        fixture.ShouldContain("Thread");
        fixture.ShouldContain("graph-thread-001");
        fixture.ShouldContain("Sent");
        fixture.ShouldContain("Created");
        fixture.ShouldContain("Source timezone");
        fixture.ShouldContain("Correlation ID");
        fixture.ShouldContain("Threshold band");
        fixture.ShouldContain("System decision: Associate");
        AssertTextOrder(
            fixture,
            "Source",
            "Microsoft 365 mailbox",
            "Mailbox",
            "controlled-mailbox-001",
            "Provider message ID",
            "graph-message-001",
            "Internet message ID",
            "&lt;internet-message-001@example.test&gt;",
            "Operation",
            "01HZXASSOC000000000000001",
            "Conversation context",
            "graph-conversation-001",
            "Thread",
            "graph-thread-001",
            "Project",
            "project-alpha",
            "Lifecycle state",
            "Associated",
            "Confidence",
            "91%",
            "Threshold band",
            "Auto",
            "Safe next actions",
            "none",
            "Received",
            "2026-06-01 08:00:00Z",
            "Sent",
            "2026-06-01 07:58:00Z",
            "Created",
            "2026-06-01 07:57:00Z",
            "Source timezone",
            "UTC",
            "Correlation ID",
            "01HZXCORRELATION00000000001",
            "m365-mailbox-intake",
            "metadata_only",
            "91%");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertPopulatedAccessibilityModesWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Populated);
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain(".chatbot-email-conversation-item");
        css.ShouldContain("animation: none !important;");
        css.ShouldContain("transition-duration: 0.01ms !important;");
        css.ShouldContain(".chatbot-email-conversation-item__header");
        css.ShouldContain("flex-direction: column;");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("aria-label=\"Mailbox item: Mailbox intake, Associated\"");
        fixture.ShouldContain("aria-label=\"Project conversation metadata\"");
    }

    private static void AssertEmptyWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Empty);
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");

        stream.ShouldContain("<ChatBotBlockedState");
        fixture.ShouldContain("No email-derived context is available.");
        fixture.ShouldContain("Wait for associated email.");
        fixture.ShouldContain("role=\"alert\"");
    }

    private static void AssertUnauthorizedWithoutBrowser()
    {
        string fixture = BuildProjectConversationFixture(ProjectConversationFixtureScenario.Unauthorized);
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain("@media (forced-colors: active)");
        fixture.ShouldContain("Project conversation is unavailable.");
        fixture.ShouldContain("Evidence restricted");
        fixture.ShouldContain("project-redacted");
        AssertMetadataOnlyBody(fixture);
    }

    private static void AssertMetadataOnlyBody(string text)
    {
        text.ShouldNotContain("restricted@example.com", Case.Insensitive);
        text.ShouldNotContain("sender@example.test", Case.Insensitive);
        text.ShouldNotContain("raw provider payload", Case.Insensitive);
        text.ShouldNotContain("Secret Project", Case.Insensitive);
        text.ShouldNotContain("raw exception", Case.Insensitive);
        text.ShouldNotContain("full email body", Case.Insensitive);
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

        private static async Task<BrowserHarness> StartAsync(string chromeExecutable, bool forcedColors)
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

    private enum ProjectConversationFixtureScenario
    {
        Loading,
        Populated,
        Empty,
        Unauthorized,
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
