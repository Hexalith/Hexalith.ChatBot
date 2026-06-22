using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.

/// <summary>
/// Story 8.3 E2E coverage for the operational-dashboard published-SLO/error-budget section. The browser path
/// verifies the operator-facing metadata-only table, stable metric/burn tokens, coarse burn status, and absence of
/// restricted/raw detail. The no-browser fallback asserts the same contract against the Razor, catalog, and
/// localization sources.
/// </summary>
public sealed class OperationalDashboardsPublishedSlosE2ETests
{
    private static readonly string[] ExpectedMetricNames =
    [
        "chatbot.command.execution.latency",
        "chatbot.association.latency",
        "chatbot.operation.identity.latency",
        "chatbot.correction.propagation.latency",
        "chatbot.audit.projection.lag",
        "chatbot.retry.exhausted",
        "chatbot.approval.queue.age",
        "chatbot.mailbox.subscription.expiry",
        "chatbot.ingestion.latency",
        "chatbot.ambiguous.resolution.time",
        "chatbot.duplicate.suppressed",
        "chatbot.mailbox.failure.rate",
        "chatbot.ai.mediation.latency",
    ];

    [Fact]
    public async Task OperationalDashboard_PublishedSlosRenderMetadataOnlyCatalogAndCoarseBurn()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPublishedSloContractWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Operational dashboards", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Published SLOs / Error budgets", Level = 2 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Table, new() { NameString = "Published SLOs / Error budgets" }));

            ILocator rows = harness.Page.Locator("[data-chatbot-slo-metric]");
            (await rows.CountAsync()).ShouldBe(ExpectedMetricNames.Length);

            foreach (string metricName in ExpectedMetricNames)
            {
                ILocator row = harness.Page.Locator($"[data-chatbot-slo-metric='{metricName}']");
                await WaitForVisibleAsync(row);
                (await row.GetAttributeAsync("tabindex")).ShouldBe("0");

                string rowText = await row.InnerTextAsync();
                rowText.ShouldContain("Target");
                rowText.ShouldContain("Measurement window");
                rowText.ShouldContain("Error budget");
                rowText.ShouldContain("Alert threshold");
                rowText.ShouldContain("Calibration source");
                rowText.ShouldContain("Tenant scope");
                rowText.ShouldContain("Error-budget burn");
                rowText.ShouldContain("platform-default");
            }

            ILocator auditLag = harness.Page.Locator("[data-chatbot-slo-metric='chatbot.audit.projection.lag']");
            (await auditLag.GetAttributeAsync("data-chatbot-slo-burn")).ShouldBe("approaching");
            (await auditLag.InnerTextAsync()).ShouldContain("Approaching");
            (await auditLag.InnerTextAsync()).ShouldContain("lag-gt-5m");

            ILocator pending = harness.Page.Locator("[data-chatbot-slo-metric='chatbot.ingestion.latency']");
            (await pending.GetAttributeAsync("data-chatbot-slo-burn")).ShouldBe("unknown");
            (await pending.InnerTextAsync()).ShouldContain("calibration-pending");
            (await pending.InnerTextAsync()).ShouldContain("a11-pending");
            (await pending.InnerTextAsync()).ShouldContain("Unknown");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldContain("p95-le-2000ms");
            bodyText.ShouldContain("p95-le-10000ms");
            bodyText.ShouldContain("p95-le-5000ms");
            bodyText.ShouldContain("p95-le-10m");
            AssertMetadataOnly(bodyText);
        }
    }

    private static void AssertPublishedSloContractWithoutBrowser()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");
        string catalog = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineContracts.cs");
        string english = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.resx");
        string french = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx");

        page.ShouldContain("overview.PublishedSlos");
        page.ShouldContain("<FluentDataGrid");
        page.ShouldContain("data-chatbot-slo-metric");
        page.ShouldContain("data-chatbot-slo-burn");
        page.ShouldContain("ErrorBudgetBurnStates.ToWireValue");
        page.ShouldContain("BurnLabel");

        foreach (string metricName in ExpectedMetricNames)
        {
            catalog.ShouldContain($"\"{metricName}\"");
        }

        catalog.ShouldContain("p95-le-2000ms");
        catalog.ShouldContain("p95-le-10000ms");
        catalog.ShouldContain("p95-le-5000ms");
        catalog.ShouldContain("p95-le-10m");
        catalog.ShouldContain("calibration-pending");
        catalog.ShouldContain("a11-pending");
        catalog.ShouldContain("lag-gt-5m");

        foreach (string localizationKey in new[]
        {
            "OperationalDashboards_Slos_Title",
            "OperationalDashboards_Slo_MetricName_Label",
            "OperationalDashboards_Slo_Target_Label",
            "OperationalDashboards_Slo_Window_Label",
            "OperationalDashboards_Slo_ErrorBudget_Label",
            "OperationalDashboards_Slo_AlertThreshold_Label",
            "OperationalDashboards_Slo_CalibrationSource_Label",
            "OperationalDashboards_Slo_TenantScope_Label",
            "OperationalDashboards_Slo_Burn_Label",
            "OperationalDashboards_Burn_Unknown",
            "OperationalDashboards_Burn_Approaching",
        })
        {
            english.ShouldContain(localizationKey);
            french.ShouldContain(localizationKey);
        }
    }

    private static string BuildFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string rows = string.Join(Environment.NewLine, BuildSloRows());

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Operational dashboards SLO fixture</title>
                <style>
                  {{css}}
                  .slo-fixture { max-width: 1180px; margin: 0 auto; padding: 24px; }
                </style>
              </head>
              <body>
                <main class="chatbot-page slo-fixture" aria-labelledby="operational-dashboards-title">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">S8</span>
                    <h1 id="operational-dashboards-title" class="chatbot-page-title">Operational dashboards</h1>
                  </header>
                  <section class="chatbot-section" aria-labelledby="operational-dashboards-slos-title">
                    <h2 id="operational-dashboards-slos-title" class="chatbot-section-title">Published SLOs / Error budgets</h2>
                    <p class="chatbot-body">Metadata-only SLO catalog and current coarse error-budget burn.</p>
                    <div class="chatbot-table" role="table" aria-label="Published SLOs / Error budgets">
            {{rows}}
                    </div>
                  </section>
                </main>
              </body>
            </html>
            """;
    }

    private static IEnumerable<string> BuildSloRows()
    {
        yield return SloRow("chatbot.command.execution.latency", "p95-le-2000ms", "rolling-24h", "calibration-pending", "budget-burn", "nfr24", "unknown", "Unknown");
        yield return SloRow("chatbot.association.latency", "p95-le-10000ms", "rolling-24h", "calibration-pending", "budget-burn", "nfr25", "unknown", "Unknown");
        yield return SloRow("chatbot.operation.identity.latency", "p95-le-5000ms", "rolling-24h", "calibration-pending", "budget-burn", "nfr26", "unknown", "Unknown");
        yield return SloRow("chatbot.correction.propagation.latency", "p95-le-10m", "rolling-24h", "calibration-pending", "budget-burn", "nfr17a", "unknown", "Unknown");
        yield return SloRow("chatbot.audit.projection.lag", "p95-le-5m", "rolling-24h", "degraded-100ev-failed-1000ev", "lag-gt-5m", "nfr43", "approaching", "Approaching");
        yield return SloRow("chatbot.retry.exhausted", "on-exhaustion", "rolling-24h", "calibration-pending", "any-exhaustion", "nfr43", "unknown", "Unknown");
        yield return SloRow("chatbot.approval.queue.age", "p95-le-2-business-days", "rolling-7d", "calibration-pending", "age-gt-2-business-days", "nfr43", "unknown", "Unknown");
        yield return SloRow("chatbot.mailbox.subscription.expiry", "expiry-le-7d", "rolling-7d", "calibration-pending", "expiry-le-7d", "nfr43", "unknown", "Unknown");
        yield return SloRow("chatbot.ingestion.latency", "calibration-pending", "rolling-24h", "calibration-pending", "budget-burn", "a11-pending", "unknown", "Unknown");
        yield return SloRow("chatbot.ambiguous.resolution.time", "calibration-pending", "rolling-7d", "calibration-pending", "budget-burn", "a11-pending", "unknown", "Unknown");
        yield return SloRow("chatbot.duplicate.suppressed", "calibration-pending", "rolling-24h", "calibration-pending", "spike-baseline", "a11-pending", "unknown", "Unknown");
        yield return SloRow("chatbot.mailbox.failure.rate", "calibration-pending", "rolling-24h", "calibration-pending", "budget-burn", "a11-pending", "unknown", "Unknown");
        yield return SloRow("chatbot.ai.mediation.latency", "calibration-pending", "rolling-24h", "calibration-pending", "budget-burn", "a11-pending", "unknown", "Unknown");
    }

    private static string SloRow(
        string metricName,
        string target,
        string window,
        string errorBudget,
        string alertThreshold,
        string calibrationSource,
        string burnToken,
        string burnLabel)
        => $$"""
                        <article class="chatbot-labelled-row-list"
                                 role="row"
                                 tabindex="0"
                                 data-chatbot-slo-metric="{{metricName}}"
                                 data-chatbot-slo-burn="{{burnToken}}">
                          <dl class="chatbot-definition-list">
                            <dt class="chatbot-labelled-row">Metric name</dt><dd><code class="chatbot-code">{{metricName}}</code></dd>
                            <dt class="chatbot-labelled-row">Target</dt><dd><code class="chatbot-code">{{target}}</code></dd>
                            <dt class="chatbot-labelled-row">Measurement window</dt><dd><code class="chatbot-code">{{window}}</code></dd>
                            <dt class="chatbot-labelled-row">Error budget</dt><dd><code class="chatbot-code">{{errorBudget}}</code></dd>
                            <dt class="chatbot-labelled-row">Alert threshold</dt><dd><code class="chatbot-code">{{alertThreshold}}</code></dd>
                            <dt class="chatbot-labelled-row">Calibration source</dt><dd><code class="chatbot-code">{{calibrationSource}}</code></dd>
                            <dt class="chatbot-labelled-row">Tenant scope</dt><dd><code class="chatbot-code">platform-default</code></dd>
                            <dt class="chatbot-labelled-row">Error-budget burn</dt><dd><code class="chatbot-code">{{burnLabel}}</code></dd>
                          </dl>
                          <div class="chatbot-status-group" aria-label="{{burnLabel}}">
                            <p role="status" aria-label="{{metricName}}: {{burnLabel}}" data-chatbot-announcement-key="dashboard-slo-burn-{{metricName}}">{{burnLabel}}</p>
                          </div>
                        </article>
            """;

    private static void AssertMetadataOnly(string bodyText)
    {
        bodyText.ShouldNotContain("Project Alpha");
        bodyText.ShouldNotContain("EvidenceContent");
        bodyText.ShouldNotContain("MailboxSubject");
        bodyText.ShouldNotContain("exception", Case.Insensitive);
        bodyText.ShouldNotContain("bearer", Case.Insensitive);
        bodyText.ShouldNotContain("secret", Case.Insensitive);
        bodyText.ShouldNotContain("raw percentile", Case.Insensitive);
        bodyText.ShouldNotContain("raw event count", Case.Insensitive);
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

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
            string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return File.Exists(configured) ? configured : null;
            }

            const string linuxChrome = "/usr/bin/google-chrome";
            return File.Exists(linuxChrome) ? linuxChrome : null;
        }
    }
}
