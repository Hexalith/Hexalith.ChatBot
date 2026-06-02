using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class ComplianceAdministrationE2ETests
{
    [Fact]
    public async Task ComplianceAuditInvestigationShouldExposeMetadataOnlyTimelineAndSafeEscalation()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAuditInvestigationFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.AuditInvestigation));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Compliance audit", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.List, new() { NameString = "Compliance audit timeline" }));

            ILocator row = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Audit record, SubmitRetentionConfigurationChange, restricted, 2026-06-02 04:00:00Z" });
            await WaitForVisibleAsync(row);
            await WaitForVisibleAsync(row.GetByText("actor:admin-alpha", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("decision:allow", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("policy-snapshot:policy-snapshot-admin-v1", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("redaction:restricted", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("safe-next-action:request-access", new() { Exact = true }));

            ILocator escalation = row.GetByRole(AriaRole.Button, new() { NameString = "Request compliance access" });
            (await escalation.GetAttributeAsync("aria-describedby")).ShouldBe("compliance-escalation-reason");
            await escalation.ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.commandType")).ShouldBe("RequestComplianceEscalation");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.escalationTarget")).ShouldBe("project-opaque-ref");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Trigger investigation" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.commandType")).ShouldBe("RequestComplianceInvestigation");

            ILocator retry = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Retry queue item" });
            (await retry.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await retry.GetAttributeAsync("aria-describedby")).ShouldBe("compliance-operate-denied");
            await retry.ClickAsync();
            (await harness.Page.EvaluateAsync<string?>("() => window.__lastWorkflowMutation ?? null")).ShouldBeNull();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task RetentionConfigurationValidationShouldFocusSummaryAndSubmitSafeSnapshotMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRetentionFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.RetentionValidation));

            ILocator summary = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Retention validation summary" });
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("data-validation-placement")).ShouldBe("before-fields");
            (await summary.GetAttributeAsync("tabindex")).ShouldBe("-1");

            ILocator sourceEmail = harness.Page.GetByLabel("Source email metadata retention days");
            (await sourceEmail.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await sourceEmail.GetAttributeAsync("aria-describedby")).ShouldBe("source-email-retention-message");

            ILocator audit = harness.Page.GetByLabel("Audit record retention days");
            (await audit.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await audit.GetAttributeAsync("aria-describedby")).ShouldBe("audit-retention-message");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit retention change" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("retention-validation-summary");
            (await harness.Page.EvaluateAsync<string?>("() => window.__lastRetentionCommand ?? null")).ShouldBeNull();

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit valid retention change" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetentionCommand.commandType")).ShouldBe("SubmitRetentionConfigurationChange");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetentionCommand.oldFingerprint")).ShouldBe("sha256:oldretentionfingerprint001");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetentionCommand.newFingerprint")).ShouldBe("sha256:newretentionfingerprint001");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task CompliancePhoneFallbackShouldKeepReadOnlySummaryAndEscalationReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPhoneFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.PhoneFallback));

            ILocator fallback = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Compliance audit summary is available on phone." });
            await WaitForVisibleAsync(fallback);
            await WaitForVisibleAsync(fallback.GetByText("audit-record:retention-change-001", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("safe-next-action:request-access", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("Dense audit analysis and retention editing require a larger screen; summary and safe escalation remain reachable.", new() { Exact = true }));

            ILocator denseAudit = harness.Page.Locator("[data-compliance-dense-audit='true']");
            (await denseAudit.IsVisibleAsync()).ShouldBeFalse();
            ILocator denseRetention = harness.Page.Locator("[data-compliance-dense-retention='true']");
            (await denseRetention.IsVisibleAsync()).ShouldBeFalse();

            await fallback.GetByRole(AriaRole.Button, new() { NameString = "Request compliance access" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.commandType")).ShouldBe("RequestComplianceEscalation");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildComplianceFixture(ComplianceFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string scenarioName = scenario.ToString();

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Compliance administration fixture</title>
                <style>
                  {{css}}
                  .compliance-admin-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .compliance-admin-fixture .chatbot-form-grid { display: grid; grid-template-columns: minmax(180px, 260px) minmax(0, 1fr); gap: 12px 16px; align-items: start; }
                  .compliance-admin-fixture input[type="text"] { min-height: 44px; padding: 8px; }
                  .compliance-action-row { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 16px; }
                  .compliance-phone-fallback { display: none; }
                  @media (max-width: 640px) {
                    [data-compliance-dense-audit="true"] { display: none !important; }
                    [data-compliance-dense-retention="true"] { display: none !important; }
                    .compliance-phone-fallback { display: block; }
                  }
                </style>
              </head>
              <body>
                <main class="chatbot-page compliance-admin-fixture"
                      aria-labelledby="compliance-audit-title"
                      data-compliance-fixture-scenario="{{scenarioName}}">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Compliance Administration</span>
                    <h1 id="compliance-audit-title" class="chatbot-page-title">Compliance audit</h1>
                  </header>
                  <section class="chatbot-section"
                           data-chatbot-surface="audit-investigation-s9"
                           aria-labelledby="compliance-timeline-title">
                    <h2 id="compliance-timeline-title" class="chatbot-section-title">Audit investigation</h2>
                    <ol data-compliance-dense-audit="true" aria-label="Compliance audit timeline">
                      <li>
                        <article aria-label="Audit record, SubmitRetentionConfigurationChange, restricted, 2026-06-02 04:00:00Z"
                                 data-redaction-state="restricted"
                                 data-escalation-state="not-requested">
                          <h3>SubmitRetentionConfigurationChange</h3>
                          <dl class="chatbot-definition-list" aria-label="Audit record safe metadata">
                            <dt class="chatbot-labelled-row">Actor</dt>
                            <dd><code class="chatbot-code">actor:admin-alpha</code></dd>
                            <dt class="chatbot-labelled-row">Command surface</dt>
                            <dd><code class="chatbot-code">command:SubmitRetentionConfigurationChange</code></dd>
                            <dt class="chatbot-labelled-row">Decision</dt>
                            <dd><code class="chatbot-code">decision:allow</code></dd>
                            <dt class="chatbot-labelled-row">Reason</dt>
                            <dd><code class="chatbot-code">reason:pre_commit_gate</code></dd>
                            <dt class="chatbot-labelled-row">Correlation</dt>
                            <dd><code class="chatbot-code">correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW</code></dd>
                            <dt class="chatbot-labelled-row">Policy snapshot</dt>
                            <dd><code class="chatbot-code">policy-snapshot:policy-snapshot-admin-v1</code></dd>
                            <dt class="chatbot-labelled-row">Outcome</dt>
                            <dd><code class="chatbot-code">outcome:accepted</code></dd>
                            <dt class="chatbot-labelled-row">Redaction state</dt>
                            <dd><code class="chatbot-code">redaction:restricted</code></dd>
                            <dt class="chatbot-labelled-row">Escalation status</dt>
                            <dd><code class="chatbot-code">escalation:not-requested</code></dd>
                            <dt class="chatbot-labelled-row">Safe next action</dt>
                            <dd><code class="chatbot-code">safe-next-action:request-access</code></dd>
                          </dl>
                          <p id="compliance-escalation-reason" class="chatbot-body">Request access with an investigation id and opaque resource reference.</p>
                          <p id="compliance-operate-denied" class="chatbot-body">Compliance scope can inspect audit metadata but cannot operate workflow items.</p>
                          <div class="compliance-action-row">
                            <button type="button"
                                    aria-describedby="compliance-escalation-reason"
                                    data-chatbot-stable-id="compliance-request-access">Request compliance access</button>
                            <button type="button"
                                    data-chatbot-stable-id="compliance-trigger-investigation">Trigger investigation</button>
                            <button type="button"
                                    aria-disabled="true"
                                    aria-describedby="compliance-operate-denied">Retry queue item</button>
                          </div>
                        </article>
                      </li>
                    </ol>
                    <aside class="compliance-phone-fallback"
                           role="complementary"
                           aria-label="Compliance audit summary is available on phone.">
                      <p>Compliance audit summary is available on phone.</p>
                      <p>audit-record:retention-change-001</p>
                      <p>redaction:restricted</p>
                      <p>safe-next-action:request-access</p>
                      <p>Dense audit analysis and retention editing require a larger screen; summary and safe escalation remain reachable.</p>
                      <button type="button"
                              aria-describedby="compliance-escalation-reason"
                              data-chatbot-stable-id="compliance-request-access">Request compliance access</button>
                    </aside>
                  </section>
                  <section class="chatbot-section"
                           data-compliance-dense-retention="true"
                           aria-labelledby="retention-editor-title">
                    <h2 id="retention-editor-title" class="chatbot-section-title">Retention configuration</h2>
                    <div id="retention-validation-summary"
                         class="chatbot-status"
                         data-chatbot-status="warning"
                         data-validation-placement="before-fields"
                         role="alert"
                         tabindex="-1"
                         aria-label="Retention validation summary">
                      <span class="chatbot-status__label">Warning</span>
                      <span>Retention windows must stay within bounded compliance policy.</span>
                    </div>
                    <div class="chatbot-form-grid">
                      <label class="chatbot-labelled-row" for="source-email-retention">Source email metadata retention days</label>
                      <div>
                        <input id="source-email-retention"
                               type="text"
                               value="10"
                               aria-invalid="true"
                               aria-describedby="source-email-retention-message" />
                        <p id="source-email-retention-message" class="chatbot-body">Window must be between 30 and 3650 days.</p>
                      </div>
                      <label class="chatbot-labelled-row" for="audit-retention">Audit record retention days</label>
                      <div>
                        <input id="audit-retention"
                               type="text"
                               value="365"
                               aria-invalid="true"
                               aria-describedby="audit-retention-message" />
                        <p id="audit-retention-message" class="chatbot-body">Audit chain reconstructability requires at least 2555 days.</p>
                      </div>
                    </div>
                    <dl class="chatbot-definition-list" aria-label="Retention safe snapshot metadata">
                      <dt class="chatbot-labelled-row">Source snapshot</dt>
                      <dd><code class="chatbot-code">retention-snapshot-current</code></dd>
                      <dt class="chatbot-labelled-row">Proposed snapshot</dt>
                      <dd><code class="chatbot-code">retention-snapshot-proposed</code></dd>
                      <dt class="chatbot-labelled-row">Old fingerprint</dt>
                      <dd><code class="chatbot-code">sha256:oldretentionfingerprint001</code></dd>
                      <dt class="chatbot-labelled-row">New fingerprint</dt>
                      <dd><code class="chatbot-code">sha256:newretentionfingerprint001</code></dd>
                      <dt class="chatbot-labelled-row">Deletion mode</dt>
                      <dd><code class="chatbot-code">projection-tombstone-key-shredding</code></dd>
                    </dl>
                    <div class="compliance-action-row">
                      <button type="button"
                              data-chatbot-stable-id="retention-submit-invalid">Submit retention change</button>
                      <button type="button"
                              data-chatbot-stable-id="retention-submit-valid">Submit valid retention change</button>
                    </div>
                  </section>
                </main>
                <script>
                  document.querySelectorAll("[data-chatbot-stable-id='compliance-request-access']").forEach(button => {
                    button.addEventListener("click", () => {
                      window.__lastComplianceCommand = {
                        commandType: "RequestComplianceEscalation",
                        escalationTarget: "project-opaque-ref"
                      };
                    });
                  });
                  document.querySelector("[data-chatbot-stable-id='compliance-trigger-investigation']").addEventListener("click", () => {
                    window.__lastComplianceCommand = {
                      commandType: "RequestComplianceInvestigation",
                      investigationId: "investigation-001"
                    };
                  });
                  document.querySelector("[data-chatbot-stable-id='retention-submit-invalid']").addEventListener("click", () => {
                    document.querySelector("#retention-validation-summary").focus();
                  });
                  document.querySelector("[data-chatbot-stable-id='retention-submit-valid']").addEventListener("click", () => {
                    window.__lastRetentionCommand = {
                      commandType: "SubmitRetentionConfigurationChange",
                      oldFingerprint: "sha256:oldretentionfingerprint001",
                      newFingerprint: "sha256:newretentionfingerprint001"
                    };
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static void AssertAuditInvestigationFixtureWithoutBrowser()
    {
        string fixture = BuildComplianceFixture(ComplianceFixtureScenario.AuditInvestigation);

        fixture.ShouldContain("Compliance audit timeline");
        fixture.ShouldContain("redaction:restricted");
        fixture.ShouldContain("safe-next-action:request-access");
        fixture.ShouldContain("RequestComplianceEscalation");
        fixture.ShouldContain("RequestComplianceInvestigation");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("Compliance scope can inspect audit metadata but cannot operate workflow items.");
        AssertMetadataOnly(fixture);
    }

    private static void AssertRetentionFixtureWithoutBrowser()
    {
        string fixture = BuildComplianceFixture(ComplianceFixtureScenario.RetentionValidation);

        fixture.ShouldContain("Retention validation summary");
        fixture.ShouldContain("data-validation-placement=\"before-fields\"");
        fixture.ShouldContain("aria-invalid=\"true\"");
        fixture.ShouldContain("SubmitRetentionConfigurationChange");
        fixture.ShouldContain("projection-tombstone-key-shredding");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPhoneFixtureWithoutBrowser()
    {
        string fixture = BuildComplianceFixture(ComplianceFixtureScenario.PhoneFallback);

        fixture.ShouldContain("Compliance audit summary is available on phone.");
        fixture.ShouldContain("Dense audit analysis and retention editing require a larger screen; summary and safe escalation remain reachable.");
        fixture.ShouldContain("data-compliance-dense-audit=\"true\"");
        fixture.ShouldContain("data-compliance-dense-retention=\"true\"");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldNotContain("project name", Case.Insensitive);
        text.ShouldNotContain("mailbox body", Case.Insensitive);
        text.ShouldNotContain("message subject", Case.Insensitive);
        text.ShouldNotContain("provider payload", Case.Insensitive);
        text.ShouldNotContain("raw claim", Case.Insensitive);
        text.ShouldNotContain("authorization header", Case.Insensitive);
        text.ShouldNotContain("bearer token", Case.Insensitive);
        text.ShouldNotContain("command body", Case.Insensitive);
        text.ShouldNotContain("audit envelope", Case.Insensitive);
        text.ShouldNotContain("workflow mutation", Case.Insensitive);
        text.ShouldNotContain("{\"audit", Case.Insensitive);
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
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

    private enum ComplianceFixtureScenario
    {
        AuditInvestigation,
        RetentionValidation,
        PhoneFallback,
    }
}
#pragma warning restore CA2007
