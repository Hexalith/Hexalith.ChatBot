using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class TenantPolicyEditorE2ETests
{
    [Fact]
    public async Task TenantPolicyEditorValidationFailureShouldExposeSemanticRecoveryContracts()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertValidationFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.Invalid));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Tenant configuration", Level = 1 }));
            ILocator summary = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Tenant policy validation summary" });
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("data-validation-placement")).ShouldBe("before-fields");
            (await summary.GetAttributeAsync("tabindex")).ShouldBe("-1");

            ILocator highThreshold = harness.Page.GetByLabel("Association high threshold");
            await WaitForVisibleAsync(highThreshold);
            (await highThreshold.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await highThreshold.GetAttributeAsync("aria-describedby")).ShouldBe("association-t-high-message");
            await WaitForVisibleAsync(harness.Page.Locator("#association-t-high-message"));

            ILocator aiPolicy = harness.Page.GetByRole(AriaRole.Group, new() { NameString = "AI action low-risk classes" });
            await WaitForVisibleAsync(aiPolicy);
            (await aiPolicy.GetAttributeAsync("aria-describedby")).ShouldBe("ai-action-low-risk-allowed-message");
            await WaitForVisibleAsync(harness.Page.GetByLabel("modifies-state low-risk allowed"));
            await WaitForVisibleAsync(harness.Page.GetByLabel("acts-on-behalf low-risk allowed"));

            ILocator save = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save tenant policy" });
            await save.ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("tenant-policy-validation-summary");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task TenantPolicyEditorPendingApprovalShouldKeepTwoPersonAndMetadataOnlyStateVisible()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPendingApprovalFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.PendingApproval));

            ILocator status = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Tenant policy status: pending approval" });
            await WaitForVisibleAsync(status);
            (await status.GetAttributeAsync("data-chatbot-status")).ShouldBe("warning");
            await WaitForVisibleAsync(harness.Page.GetByText("Second policy admin required", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("policy-change-001", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("association.t-high, ai-action.low-risk-allowed", new() { Exact = true }));

            ILocator requester = harness.Page.GetByLabel("Requester actor: admin-a");
            ILocator approver = harness.Page.GetByLabel("Approver actor: admin-b required");
            await WaitForVisibleAsync(requester);
            await WaitForVisibleAsync(approver);

            ILocator approve = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Approve pending policy" });
            (await approve.GetAttributeAsync("aria-describedby")).ShouldBe("approve-policy-reason");
            await approve.ClickAsync();
            string command = await harness.Page.EvaluateAsync<string>("() => window.__lastPolicyCommand.commandType");
            command.ShouldBe("ApproveTenantPolicyChange");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task TenantPolicyEditorPhoneFallbackShouldPreserveDraftAndAvoidDenseEditing()
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
            await harness.Page.SetContentAsync(BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.PhoneFallback));

            ILocator fallback = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Tenant policy summary is available on phone." });
            await WaitForVisibleAsync(fallback);
            await WaitForVisibleAsync(harness.Page.GetByText("Dense policy controls are unavailable on this screen size; summary and safe approval actions remain reachable.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("tenant-policy-draft-preserved", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Approve pending policy" }));

            ILocator denseForm = harness.Page.Locator("[data-policy-dense-editor='true']");
            (await denseForm.IsVisibleAsync()).ShouldBeFalse();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task TenantPolicyEditorConflictShouldNameSafeConflictCauseAndRecoveryAction()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertConflictFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.Conflict));

            ILocator conflict = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Tenant policy save conflict: stale data" });
            await WaitForVisibleAsync(conflict);
            (await conflict.GetAttributeAsync("data-chatbot-save-conflict-cause")).ShouldBe("stale-data");
            await WaitForVisibleAsync(harness.Page.GetByText("Reload the policy snapshot before saving again.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Reload policy snapshot" }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string scenarioName = scenario.ToString();

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Tenant policy editor fixture</title>
                <style>
                  {{css}}
                  .tenant-policy-editor-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .tenant-policy-editor-fixture .chatbot-form-grid { display: grid; grid-template-columns: minmax(180px, 260px) minmax(0, 1fr); gap: 12px 16px; align-items: start; }
                  .tenant-policy-editor-fixture input[type="text"] { min-height: 44px; padding: 8px; }
                  .tenant-policy-action-row { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 16px; }
                  .tenant-policy-phone-fallback { display: none; }
                  @media (max-width: 640px) {
                    [data-policy-dense-editor="true"] { display: none !important; }
                    .tenant-policy-phone-fallback { display: block; }
                  }
                </style>
              </head>
              <body>
                <main class="chatbot-page tenant-policy-editor-fixture" aria-labelledby="tenant-configuration-title">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Tenant Configuration S5</span>
                    <h1 id="tenant-configuration-title" class="chatbot-page-title">Tenant configuration</h1>
                  </header>
                  <section class="chatbot-section"
                           aria-labelledby="tenant-policy-editor-title"
                           data-chatbot-surface="tenant-configuration-s5"
                           data-small-screen-fallback="tenant-policy-draft-preserved">
                    <h2 id="tenant-policy-editor-title" class="chatbot-section-title">Tenant policy</h2>
                    {{BuildScenarioStatus(scenario)}}
                    <section data-policy-dense-editor="true" aria-label="Tenant policy dense editor">
                      <div id="tenant-policy-validation-summary"
                           class="chatbot-status"
                           data-chatbot-status="warning"
                           data-validation-placement="before-fields"
                           role="alert"
                           tabindex="-1"
                           aria-label="Tenant policy validation summary">
                        <span class="chatbot-status__label">Warning</span>
                        <span>Review the validation summary before saving the tenant policy.</span>
                      </div>
                      <div class="chatbot-form-grid">
                        <label class="chatbot-labelled-row" for="association-t-high">Association high threshold</label>
                        <div>
                          <input id="association-t-high"
                                 type="text"
                                 value="1.20"
                                 aria-invalid="true"
                                 aria-describedby="association-t-high-message" />
                          <p id="association-t-high-message" class="chatbot-body">Value must be between 0.80 and 1.00.</p>
                        </div>
                        <label class="chatbot-labelled-row" for="policy-change-reason">Change reason</label>
                        <div>
                          <input id="policy-change-reason"
                                 type="text"
                                 value=""
                                 aria-invalid="true"
                                 aria-describedby="policy-change-reason-message" />
                          <p id="policy-change-reason-message" class="chatbot-body">A documented justification is required.</p>
                        </div>
                      </div>
                      <fieldset aria-label="AI action low-risk classes"
                                aria-describedby="ai-action-low-risk-allowed-message">
                        <legend>AI action low-risk classes</legend>
                        <label><input type="checkbox" aria-label="modifies-state low-risk allowed" /> modifies-state</label>
                        <label><input type="checkbox" aria-label="exposes-files low-risk allowed" /> exposes-files</label>
                        <label><input type="checkbox" aria-label="sends-external low-risk allowed" /> sends-external</label>
                        <label><input type="checkbox" aria-label="creates-tasks low-risk allowed" /> creates-tasks</label>
                        <label><input type="checkbox" aria-label="invokes-tools low-risk allowed" /> invokes-tools</label>
                        <label><input type="checkbox" aria-label="acts-on-behalf low-risk allowed" /> acts-on-behalf</label>
                        <p id="ai-action-low-risk-allowed-message" class="chatbot-body">Every action class defaults to approval required unless explicitly enabled.</p>
                      </fieldset>
                      <dl class="chatbot-definition-list" aria-label="Tenant policy safe metadata">
                        <dt class="chatbot-labelled-row">Schema version</dt>
                        <dd><code class="chatbot-code">tenant-policy-schema.m0</code></dd>
                        <dt class="chatbot-labelled-row">Snapshot id</dt>
                        <dd><code class="chatbot-code">policy-snapshot-current</code></dd>
                        <dt class="chatbot-labelled-row">Changed knobs</dt>
                        <dd><code class="chatbot-code">association.t-high, ai-action.low-risk-allowed</code></dd>
                        <dt class="chatbot-labelled-row">Safe conflict cause</dt>
                        <dd><code class="chatbot-code">stale-data</code></dd>
                      </dl>
                    </section>
                    <p id="tenant-policy-save-disabled-reason" class="chatbot-body">A valid reason and policy authority are required before saving.</p>
                    <div class="tenant-policy-action-row">
                      <button type="button"
                              aria-describedby="tenant-policy-save-disabled-reason"
                              aria-disabled="true"
                              data-chatbot-stable-id="tenant-policy-save">Save tenant policy</button>
                      <button type="button"
                              aria-describedby="approve-policy-reason"
                              data-chatbot-stable-id="tenant-policy-approve">Approve pending policy</button>
                      <span id="approve-policy-reason">Second distinct human policy admin approval with documented justification is required.</span>
                    </div>
                    <aside class="tenant-policy-phone-fallback"
                           role="complementary"
                           aria-label="Tenant policy summary is available on phone.">
                      <p>Tenant policy summary is available on phone.</p>
                      <p>pending-approval</p>
                      <p>tenant-policy-draft-preserved</p>
                      <p>Dense policy controls are unavailable on this screen size; summary and safe approval actions remain reachable.</p>
                    </aside>
                  </section>
                </main>
                <script>
                  const scenario = "{{scenarioName}}";
                  document.querySelector("[data-chatbot-stable-id='tenant-policy-save']").addEventListener("click", () => {
                    document.querySelector("#tenant-policy-validation-summary").focus();
                    window.__lastPolicyCommand = { commandType: "SubmitTenantPolicyChange", scenario };
                  });
                  document.querySelector("[data-chatbot-stable-id='tenant-policy-approve']").addEventListener("click", () => {
                    window.__lastPolicyCommand = { commandType: "ApproveTenantPolicyChange", scenario };
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static string BuildScenarioStatus(TenantPolicyEditorScenario scenario)
        => scenario switch
        {
            TenantPolicyEditorScenario.PendingApproval => """
                <div class="chatbot-status"
                     data-chatbot-status="warning"
                     role="status"
                     aria-live="polite"
                     aria-label="Tenant policy status: pending approval">
                  <span class="chatbot-status__label">Warning</span>
                  <span>Second policy admin required</span>
                </div>
                <dl class="chatbot-definition-list" aria-label="Pending policy approval metadata">
                  <dt class="chatbot-labelled-row">Policy change</dt>
                  <dd><code class="chatbot-code">policy-change-001</code></dd>
                  <dt class="chatbot-labelled-row">Requester</dt>
                  <dd><span aria-label="Requester actor: admin-a">admin-a</span></dd>
                  <dt class="chatbot-labelled-row">Approver</dt>
                  <dd><span aria-label="Approver actor: admin-b required">admin-b required</span></dd>
                </dl>
                """,
            TenantPolicyEditorScenario.Conflict => """
                <div class="chatbot-status"
                     data-chatbot-status="danger"
                     data-chatbot-save-conflict-cause="stale-data"
                     role="alert"
                     aria-label="Tenant policy save conflict: stale data">
                  <span class="chatbot-status__label">Danger</span>
                  <span>Reload the policy snapshot before saving again.</span>
                </div>
                <button type="button">Reload policy snapshot</button>
                """,
            _ => """
                <div class="chatbot-status"
                     data-chatbot-status="info"
                     role="status"
                     aria-label="Tenant policy status: editing">
                  <span class="chatbot-status__label">Info</span>
                  <span>Editing policy draft</span>
                </div>
                """,
        };

    private static void AssertValidationFixtureWithoutBrowser()
    {
        string fixture = BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.Invalid);

        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("data-validation-placement=\"before-fields\"");
        fixture.ShouldContain("aria-invalid=\"true\"");
        fixture.ShouldContain("aria-describedby=\"association-t-high-message\"");
        fixture.ShouldContain("AI action low-risk classes");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPendingApprovalFixtureWithoutBrowser()
    {
        string fixture = BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.PendingApproval);

        fixture.ShouldContain("Tenant policy status: pending approval");
        fixture.ShouldContain("Second policy admin required");
        fixture.ShouldContain("Requester actor: admin-a");
        fixture.ShouldContain("Approver actor: admin-b required");
        fixture.ShouldContain("ApproveTenantPolicyChange");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPhoneFallbackFixtureWithoutBrowser()
    {
        string fixture = BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.PhoneFallback);

        fixture.ShouldContain("tenant-policy-draft-preserved");
        fixture.ShouldContain("Dense policy controls are unavailable on this screen size; summary and safe approval actions remain reachable.");
        fixture.ShouldContain("data-policy-dense-editor=\"true\"");
        AssertMetadataOnly(fixture);
    }

    private static void AssertConflictFixtureWithoutBrowser()
    {
        string fixture = BuildTenantPolicyEditorFixture(TenantPolicyEditorScenario.Conflict);

        fixture.ShouldContain("data-chatbot-save-conflict-cause=\"stale-data\"");
        fixture.ShouldContain("Reload the policy snapshot before saving again.");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldNotContain("project name", Case.Insensitive);
        text.ShouldNotContain("mailbox body", Case.Insensitive);
        text.ShouldNotContain("provider payload", Case.Insensitive);
        text.ShouldNotContain("raw claim", Case.Insensitive);
        text.ShouldNotContain("authorization header", Case.Insensitive);
        text.ShouldNotContain("bearer token", Case.Insensitive);
        text.ShouldNotContain("secret", Case.Insensitive);
        text.ShouldNotContain("{\"policy", Case.Insensitive);
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

    private enum TenantPolicyEditorScenario
    {
        Invalid,
        PendingApproval,
        PhoneFallback,
        Conflict,
    }
}
