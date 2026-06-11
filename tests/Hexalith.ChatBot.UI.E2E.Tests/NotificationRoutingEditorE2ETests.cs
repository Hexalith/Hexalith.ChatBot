using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class NotificationRoutingEditorE2ETests
{
    [Fact]
    public async Task NotificationRoutingEditor_MatrixEdit_SubmitsMetadataOnlyGovernedCommand()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertMatrixFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildNotificationRoutingFixture(NotificationRoutingScenario.MatrixEdit));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Notification routing", Level = 1 }));
            ILocator matrix = harness.Page.GetByRole(AriaRole.Table, new() { NameString = "Notification routing matrix" });
            await WaitForVisibleAsync(matrix);
            (await matrix.Locator("tbody tr").CountAsync()).ShouldBe(6);

            ILocator failureRole = harness.Page.GetByLabel("Recipient role failure:operate");
            await failureRole.SelectOptionAsync("tenant-admin");
            ILocator failureChannel = harness.Page.GetByLabel("Channel failure:operate");
            await failureChannel.SelectOptionAsync("email");
            await harness.Page.GetByLabel("Reason code").FillAsync("routing-update");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save notification routing" }).ClickAsync();

            (await harness.Page.EvaluateAsync<string>("() => window.__lastRoutingCommand?.commandType ?? ''")).ShouldBe("SubmitNotificationRoutingChange");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRoutingCommand?.origin ?? ''")).ShouldBe("Ui");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRoutingCommand?.reasonCode ?? ''")).ShouldBe("routing-update");
            (await harness.Page.EvaluateAsync<int>("() => window.__lastRoutingCommand?.changeSet?.length ?? 0")).ShouldBe(6);
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRoutingCommand?.changeSet?.find(r => r.stateClass === 'failure')?.recipientRole ?? ''")).ShouldBe("tenant-admin");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRoutingCommand?.changeSet?.find(r => r.stateClass === 'failure')?.channel ?? ''")).ShouldBe("email");

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Notification routing status" }).GetByText("accepted-projection-pending"));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task NotificationRoutingEditor_ValidationFailure_FocusesSummaryAndBlocksDurableWrite()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertValidationFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildNotificationRoutingFixture(NotificationRoutingScenario.ValidationFailure));

            ILocator summary = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Notification routing validation summary" });
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("data-validation-placement")).ShouldBe("before-fields");
            (await summary.GetAttributeAsync("tabindex")).ShouldBe("-1");

            ILocator reason = harness.Page.GetByLabel("Reason code");
            await WaitForVisibleAsync(reason);
            (await reason.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await reason.GetAttributeAsync("aria-describedby")).ShouldBe("routing-change-reason-message");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save notification routing" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("notification-routing-validation-summary");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRoutingCommand?.commandType ?? ''")).ShouldBe(string.Empty);
            await WaitForVisibleAsync(harness.Page.GetByText("Durable routing writes: 0", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task NotificationRoutingEditor_PhoneFallback_PreservesSummaryAndSafeSubmitAction()
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
            await harness.Page.SetContentAsync(BuildNotificationRoutingFixture(NotificationRoutingScenario.PhoneFallback));

            ILocator fallback = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Notification routing summary is available on phone." });
            await WaitForVisibleAsync(fallback);
            await WaitForVisibleAsync(fallback.GetByText("review-needed:see-only -> operations-admin/in-app", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("Dense routing controls are unavailable on this screen size; summary and safe submit action remain reachable.", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("notification-routing-draft-preserved", new() { Exact = true }));

            ILocator denseMatrix = harness.Page.Locator("[data-notification-routing-matrix='true']");
            (await denseMatrix.IsVisibleAsync()).ShouldBeFalse();
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save notification routing" }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildNotificationRoutingFixture(NotificationRoutingScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string validation = scenario == NotificationRoutingScenario.ValidationFailure
            ? """
                    <div id="notification-routing-validation-summary"
                         role="alert"
                         aria-label="Notification routing validation summary"
                         tabindex="-1"
                         data-validation-placement="before-fields">
                      Review the validation summary before saving the routing map.
                    </div>
                """
            : """
                    <div id="notification-routing-validation-summary"
                         role="status"
                         aria-label="Notification routing validation summary"
                         tabindex="-1"
                         data-validation-placement="before-fields"></div>
                """;
        string reasonInvalid = scenario == NotificationRoutingScenario.ValidationFailure ? "true" : "false";
        string reasonValue = scenario == NotificationRoutingScenario.ValidationFailure ? string.Empty : "routing-update";
        string durableWrites = scenario == NotificationRoutingScenario.ValidationFailure ? "Durable routing writes: 0" : "Durable routing writes: 1";

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Notification routing editor fixture</title>
                <style>
                  {{css}}
                  .notification-routing-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .routing-actions { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 16px; }
                  .routing-actions button { min-height: 44px; }
                  .routing-phone-fallback { display: none; }
                  .routing-reason-row { display: grid; grid-template-columns: minmax(160px, 240px) minmax(0, 1fr); gap: 12px; margin-top: 16px; }
                  .routing-reason-row input, .notification-routing-fixture select { min-height: 44px; }
                  @media (max-width: 640px) {
                    [data-notification-routing-matrix="true"] { display: none !important; }
                    .routing-phone-fallback { display: block; }
                  }
                </style>
              </head>
              <body>
                <main class="chatbot-page notification-routing-fixture"
                      aria-labelledby="notification-routing-title"
                      data-chatbot-surface="notification-routing-s5">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Notification routing S5</span>
                    <h1 id="notification-routing-title" class="chatbot-page-title">Notification routing</h1>
                  </header>
                  <section class="chatbot-section" aria-labelledby="routing-editor-title" data-small-screen-fallback="notification-routing-draft-preserved">
                    <h2 id="routing-editor-title" class="chatbot-section-title">Routing matrix</h2>
                    {{validation}}
                    <dl class="chatbot-definition-list" aria-label="Routing diff">
                      <dt>Schema version</dt><dd><code class="chatbot-code">notification-routing-schema.v1</code></dd>
                      <dt>Snapshot</dt><dd><code class="chatbot-code">routing-snapshot-current</code></dd>
                      <dt>Fingerprint</dt><dd><code class="chatbot-code">sha256:routingnew</code></dd>
                    </dl>
                    <table data-notification-routing-matrix="true" aria-label="Notification routing matrix">
                      <thead>
                        <tr>
                          <th scope="col">State class</th>
                          <th scope="col">Scope</th>
                          <th scope="col">Recipient role</th>
                          <th scope="col">Channel</th>
                        </tr>
                      </thead>
                      <tbody>
                        {{RoutingRow("review-needed", "see-only", "operations-admin", "in-app")}}
                        {{RoutingRow("approval-pending", "policy", "policy-admin", "email")}}
                        {{RoutingRow("failure", "operate", "operations-admin", "operator-alert")}}
                        {{RoutingRow("degraded", "operate", "operations-admin", "operator-alert")}}
                        {{RoutingRow("quarantine", "compliance", "compliance-admin", "email")}}
                        {{RoutingRow("retry", "operate", "operations-admin", "in-app")}}
                      </tbody>
                    </table>
                    <div class="routing-reason-row">
                      <label for="routing-change-reason">Reason code</label>
                      <input id="routing-change-reason"
                             aria-label="Reason code"
                             aria-invalid="{{reasonInvalid}}"
                             aria-describedby="routing-change-reason-message"
                             value="{{reasonValue}}" />
                    </div>
                    <p id="routing-change-reason-message">A valid reason and policy authority are required before saving.</p>
                    <div class="routing-actions">
                      <button type="button"
                              aria-label="Save notification routing"
                              onclick="submitRoutingChange()">Save notification routing</button>
                    </div>
                    <p role="status" aria-label="Notification routing status" data-routing-status>idle</p>
                    <p>{{durableWrites}}</p>
                    <aside class="routing-phone-fallback"
                           role="complementary"
                           aria-label="Notification routing summary is available on phone.">
                      <p>Notification routing summary is available on phone.</p>
                      <p><code class="chatbot-code">review-needed:see-only -> operations-admin/in-app</code></p>
                      <p><code class="chatbot-code">failure:operate -> operations-admin/operator-alert</code></p>
                      <p>notification-routing-draft-preserved</p>
                      <p>Dense routing controls are unavailable on this screen size; summary and safe submit action remain reachable.</p>
                    </aside>
                  </section>
                </main>
                <script>
                  function row(stateClass, scope) {
                    return {
                      stateClass,
                      scope,
                      recipientRole: document.querySelector(`[data-routing-role-select="${stateClass}:${scope}"]`).value,
                      channel: document.querySelector(`[data-routing-channel-select="${stateClass}:${scope}"]`).value
                    };
                  }

                  function submitRoutingChange() {
                    const reason = document.querySelector('#routing-change-reason').value;
                    if (!reason) {
                      document.querySelector('#notification-routing-validation-summary').focus();
                      return;
                    }

                    window.__lastRoutingCommand = {
                      commandType: 'SubmitNotificationRoutingChange',
                      origin: 'Ui',
                      changeId: 'routing-change-001',
                      sourceRoutingSnapshotId: 'routing-snapshot-current',
                      proposedRoutingSnapshotId: 'routing-snapshot-proposed',
                      reasonCode: reason,
                      sourceVersion: 4,
                      schemaVersion: 'notification-routing-schema.v1',
                      oldFingerprint: 'sha256:routingold',
                      newFingerprint: 'sha256:routingnew',
                      changeSet: [
                        row('review-needed', 'see-only'),
                        row('approval-pending', 'policy'),
                        row('failure', 'operate'),
                        row('degraded', 'operate'),
                        row('quarantine', 'compliance'),
                        row('retry', 'operate')
                      ]
                    };
                    document.querySelector('[data-routing-status]').textContent = 'accepted-projection-pending';
                  }
                </script>
              </body>
            </html>
            """;
    }

    private static string RoutingRow(string stateClass, string scope, string recipientRole, string channel)
        => $$"""
                        <tr data-routing-row-key="{{stateClass}}:{{scope}}">
                          <td><span>State class</span> <code class="chatbot-code">{{stateClass}}</code></td>
                          <td><span>Scope</span> <code class="chatbot-code">{{scope}}</code></td>
                          <td>
                            <label>
                              <span>Recipient role {{stateClass}}:{{scope}}</span>
                              <select aria-label="Recipient role {{stateClass}}:{{scope}}"
                                      data-routing-role-select="{{stateClass}}:{{scope}}">
                                {{Option("tenant-admin", recipientRole)}}
                                {{Option("mailbox-admin", recipientRole)}}
                                {{Option("policy-admin", recipientRole)}}
                                {{Option("compliance-admin", recipientRole)}}
                                {{Option("operations-admin", recipientRole)}}
                              </select>
                            </label>
                          </td>
                          <td>
                            <label>
                              <span>Channel {{stateClass}}:{{scope}}</span>
                              <select aria-label="Channel {{stateClass}}:{{scope}}"
                                      data-routing-channel-select="{{stateClass}}:{{scope}}">
                                {{Option("in-app", channel)}}
                                {{Option("email", channel)}}
                                {{Option("webhook", channel)}}
                                {{Option("operator-alert", channel)}}
                              </select>
                            </label>
                          </td>
                        </tr>
            """;

    private static string Option(string token, string selected)
        => token == selected
            ? $"""<option value="{token}" selected>{token}</option>"""
            : $"""<option value="{token}">{token}</option>""";

    private static void AssertMatrixFixtureWithoutBrowser()
    {
        string fixture = BuildNotificationRoutingFixture(NotificationRoutingScenario.MatrixEdit);
        fixture.ShouldContain("SubmitNotificationRoutingChange");
        fixture.ShouldContain("Notification routing matrix");
        fixture.ShouldContain("review-needed");
        fixture.ShouldContain("approval-pending");
        fixture.ShouldContain("failure");
        fixture.ShouldContain("degraded");
        fixture.ShouldContain("quarantine");
        fixture.ShouldContain("retry");
        fixture.ShouldContain("operator-alert");
        AssertMetadataOnly(fixture);
    }

    private static void AssertValidationFixtureWithoutBrowser()
    {
        string fixture = BuildNotificationRoutingFixture(NotificationRoutingScenario.ValidationFailure);
        fixture.ShouldContain("notification-routing-validation-summary");
        fixture.ShouldContain("aria-invalid=\"true\"");
        fixture.ShouldContain("Durable routing writes: 0");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPhoneFallbackFixtureWithoutBrowser()
    {
        string fixture = BuildNotificationRoutingFixture(NotificationRoutingScenario.PhoneFallback);
        fixture.ShouldContain("Notification routing summary is available on phone.");
        fixture.ShouldContain("notification-routing-draft-preserved");
        fixture.ShouldContain("Dense routing controls are unavailable on this screen size");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldNotContain("project name", Case.Insensitive);
        text.ShouldNotContain("mailbox body", Case.Insensitive);
        text.ShouldNotContain("provider payload", Case.Insensitive);
        text.ShouldNotContain("raw claim", Case.Insensitive);
        text.ShouldNotContain("headers", Case.Insensitive);
        text.ShouldNotContain("token", Case.Insensitive);
        text.ShouldNotContain("secret", Case.Insensitive);
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

    private enum NotificationRoutingScenario
    {
        MatrixEdit,
        ValidationFailure,
        PhoneFallback,
    }
}
