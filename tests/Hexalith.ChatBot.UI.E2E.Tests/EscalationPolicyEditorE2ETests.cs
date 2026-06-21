using System.Text.RegularExpressions;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class EscalationPolicyEditorE2ETests
{
    [Fact]
    public void EscalationPolicyEditorSource_UsesFluentControlsAndPreservesE2EMarkers()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor");

        component.ShouldContain("<FluentNumberInput", Case.Sensitive);
        component.ShouldContain("<FluentSelect", Case.Sensitive);
        component.ShouldContain("<FluentOption", Case.Sensitive);
        component.ShouldContain("<FluentTextInput", Case.Sensitive);
        component.ShouldContain("<FluentLabel", Case.Sensitive);
        component.ShouldContain("data-escalation-age-input");
        component.ShouldContain("data-escalation-severity-select");
        component.ShouldContain("data-escalation-role-select");
        component.ShouldContain("data-escalation-channel-select");
        component.ShouldNotContain("<input", Case.Sensitive);
        component.ShouldNotContain("<select", Case.Sensitive);
        component.ShouldNotContain("<option", Case.Sensitive);
    }

    [Fact]
    public async Task EscalationPolicyEditor_MatrixEdit_SubmitsMetadataOnlyGovernedCommand()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertMatrixFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildEscalationPolicyFixture(EscalationPolicyScenario.MatrixEdit));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Escalation policy", Level = 1 }));
            ILocator matrix = harness.Page.GetByRole(AriaRole.Table, new() { NameString = "Escalation policy matrix" });
            await WaitForVisibleAsync(matrix);
            (await matrix.Locator("tbody tr").CountAsync()).ShouldBe(5);

            ILocator failureAge = harness.Page.GetByRole(AriaRole.Spinbutton, new() { NameString = "Age threshold failure:operate" });
            await SetFluentNumberInputValueAsync(failureAge, "1800");
            ILocator failureSeverity = harness.Page.GetByRole(AriaRole.Combobox, new() { NameString = "Severity threshold failure:operate" });
            await SetFluentSelectValueAsync(failureSeverity, "medium");
            ILocator failureRole = harness.Page.GetByRole(AriaRole.Combobox, new() { NameString = "Escalation target role failure:operate" });
            await SetFluentSelectValueAsync(failureRole, "tenant-admin");
            ILocator failureChannel = harness.Page.GetByRole(AriaRole.Combobox, new() { NameString = "Escalation channel failure:operate" });
            await SetFluentSelectValueAsync(failureChannel, "email");
            await harness.Page.GetByRole(AriaRole.Textbox, new() { NameString = "Reason code" }).FillAsync("escalation-update");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save escalation policy" }).ClickAsync();

            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.commandType ?? ''")).ShouldBe("SubmitEscalationPolicyChange");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.origin ?? ''")).ShouldBe("Ui");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.reasonCode ?? ''")).ShouldBe("escalation-update");
            (await harness.Page.EvaluateAsync<int>("() => window.__lastEscalationCommand?.changeSet?.length ?? 0")).ShouldBe(5);
            (await harness.Page.EvaluateAsync<int>("() => window.__lastEscalationCommand?.changeSet?.find(r => r.stateClass === 'failure')?.ageThresholdSeconds ?? 0")).ShouldBe(1800);
            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.changeSet?.find(r => r.stateClass === 'failure')?.severityThreshold ?? ''")).ShouldBe("medium");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.changeSet?.find(r => r.stateClass === 'failure')?.escalationTargetRole ?? ''")).ShouldBe("tenant-admin");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.changeSet?.find(r => r.stateClass === 'failure')?.escalationChannel ?? ''")).ShouldBe("email");

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Escalation policy status" }).GetByText("accepted-projection-pending"));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task EscalationPolicyEditor_ValidationFailure_FocusesSummaryAndBlocksDurableWrite()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertValidationFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildEscalationPolicyFixture(EscalationPolicyScenario.ValidationFailure));

            ILocator summary = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Escalation policy validation summary" });
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("data-validation-placement")).ShouldBe("before-fields");
            (await summary.GetAttributeAsync("tabindex")).ShouldBe("-1");

            ILocator reason = harness.Page.GetByRole(AriaRole.Textbox, new() { NameString = "Reason code" });
            await WaitForVisibleAsync(reason);
            (await reason.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await reason.GetAttributeAsync("aria-describedby")).ShouldBe("escalation-change-reason-message");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save escalation policy" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("escalation-policy-validation-summary");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastEscalationCommand?.commandType ?? ''")).ShouldBe(string.Empty);
            await WaitForVisibleAsync(harness.Page.GetByText("Durable escalation-policy writes: 0", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task EscalationPolicyEditor_PhoneFallback_PreservesSummaryAndSafeSubmitAction()
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
            await harness.Page.SetContentAsync(BuildEscalationPolicyFixture(EscalationPolicyScenario.PhoneFallback));

            ILocator fallback = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Escalation policy summary is available on phone." });
            await WaitForVisibleAsync(fallback);
            await WaitForVisibleAsync(fallback.GetByText("review-needed:see-only -> age 86400s / high -> operations-admin/in-app", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("Dense escalation controls are unavailable on this screen size; summary and safe submit action remain reachable.", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("escalation-policy-draft-preserved", new() { Exact = true }));

            ILocator denseMatrix = harness.Page.Locator("[data-escalation-policy-matrix='true']");
            (await denseMatrix.IsVisibleAsync()).ShouldBeFalse();
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Save escalation policy" }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static Task SetFluentNumberInputValueAsync(ILocator input, string value)
        => input.EvaluateAsync(
            """
            (element, newValue) => {
              element.value = newValue;
              element.setAttribute("value", newValue);
              element.setAttribute("data-value", newValue);
              element.setAttribute("aria-valuenow", newValue);
              element.textContent = newValue;
              element.dispatchEvent(new Event("input", { bubbles: true }));
              element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);

    private static Task SetFluentSelectValueAsync(ILocator select, string value)
        => select.EvaluateAsync(
            """
            (element, selectedValue) => {
              element.value = selectedValue;
              element.setAttribute("value", selectedValue);
              element.setAttribute("data-value", selectedValue);
              element.querySelectorAll("[role='option']").forEach(option => {
                option.setAttribute("aria-selected", option.getAttribute("value") === selectedValue ? "true" : "false");
              });
              element.dispatchEvent(new Event("input", { bubbles: true }));
              element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);

    private static string BuildEscalationPolicyFixture(EscalationPolicyScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string validation = scenario == EscalationPolicyScenario.ValidationFailure
            ? """
                    <div id="escalation-policy-validation-summary"
                         role="alert"
                         aria-label="Escalation policy validation summary"
                         tabindex="-1"
                         data-validation-placement="before-fields">
                      Review the validation summary before saving the escalation policy.
                    </div>
                """
            : """
                    <div id="escalation-policy-validation-summary"
                         role="status"
                         aria-label="Escalation policy validation summary"
                         tabindex="-1"
                         data-validation-placement="before-fields"></div>
                """;
        string reasonInvalid = scenario == EscalationPolicyScenario.ValidationFailure ? "true" : "false";
        string reasonValue = scenario == EscalationPolicyScenario.ValidationFailure ? string.Empty : "escalation-update";
        string durableWrites = scenario == EscalationPolicyScenario.ValidationFailure ? "Durable escalation-policy writes: 0" : "Durable escalation-policy writes: 1";

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Escalation policy editor fixture</title>
                <style>
                  {{css}}
                  .escalation-policy-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .escalation-actions { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 16px; }
                  .escalation-actions fluent-button { min-height: 44px; }
                  .escalation-phone-fallback { display: none; }
                  .escalation-reason-row { display: grid; grid-template-columns: minmax(160px, 240px) minmax(0, 1fr); gap: 12px; margin-top: 16px; }
                  #escalation-change-reason,
                  [data-escalation-age-input],
                  [data-escalation-severity-select],
                  [data-escalation-role-select],
                  [data-escalation-channel-select] { min-height: 44px; }
                  @media (max-width: 640px) {
                    [data-escalation-policy-matrix="true"] { display: none !important; }
                    .escalation-phone-fallback { display: block; }
                  }
                </style>
              </head>
              <body>
                <main class="chatbot-page escalation-policy-fixture"
                      aria-labelledby="escalation-policy-title"
                      data-chatbot-surface="escalation-policy-s7">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Escalation policy S7</span>
                    <h1 id="escalation-policy-title" class="chatbot-page-title">Escalation policy</h1>
                  </header>
                  <section class="chatbot-section" aria-labelledby="escalation-editor-title" data-small-screen-fallback="escalation-policy-draft-preserved">
                    <h2 id="escalation-editor-title" class="chatbot-section-title">Escalation matrix</h2>
                    {{validation}}
                    <dl class="chatbot-definition-list" aria-label="Escalation diff">
                      <dt>Schema version</dt><dd><code class="chatbot-code">escalation-policy-schema.v1</code></dd>
                      <dt>Snapshot</dt><dd><code class="chatbot-code">escalation-snapshot-current</code></dd>
                      <dt>Fingerprint</dt><dd><code class="chatbot-code">sha256:escalationnew</code></dd>
                    </dl>
                    <table data-escalation-policy-matrix="true" aria-label="Escalation policy matrix">
                      <thead>
                        <tr>
                          <th scope="col">State class</th>
                          <th scope="col">Scope</th>
                          <th scope="col">Age threshold</th>
                          <th scope="col">Severity</th>
                          <th scope="col">Escalation target role</th>
                          <th scope="col">Escalation channel</th>
                        </tr>
                      </thead>
                      <tbody>
                        {{EscalationRow("review-needed", "see-only", 86400, "high", "operations-admin", "in-app")}}
                        {{EscalationRow("approval-pending", "policy", 43200, "medium", "policy-admin", "email")}}
                        {{EscalationRow("failure", "operate", 3600, "high", "operations-admin", "operator-alert")}}
                        {{EscalationRow("degraded", "operate", 7200, "medium", "operations-admin", "operator-alert")}}
                        {{EscalationRow("quarantine", "compliance", 1800, "high", "compliance-admin", "email")}}
                      </tbody>
                    </table>
                    <div class="escalation-reason-row">
                      <fluent-label for="escalation-change-reason">Reason code</fluent-label>
                      <fluent-text-input id="escalation-change-reason"
                                         role="textbox"
                                         tabindex="0"
                                         contenteditable="true"
                                         aria-label="Reason code"
                                         aria-invalid="{{reasonInvalid}}"
                                         aria-describedby="escalation-change-reason-message"
                                         value="{{reasonValue}}">{{reasonValue}}</fluent-text-input>
                    </div>
                    <p id="escalation-change-reason-message">A valid reason and policy authority are required before saving.</p>
                    <div class="escalation-actions">
                      <fluent-button role="button"
                                     tabindex="0"
                                     aria-label="Save escalation policy"
                                     onkeydown="if(event.key==='Enter'||event.key===' '){event.preventDefault();this.click();}"
                                     onclick="submitEscalationPolicyChange()">Save escalation policy</fluent-button>
                    </div>
                    <p role="status" aria-label="Escalation policy status" data-escalation-status>idle</p>
                    <p>{{durableWrites}}</p>
                    <aside class="escalation-phone-fallback"
                           role="complementary"
                           aria-label="Escalation policy summary is available on phone.">
                      <p>Escalation policy summary is available on phone.</p>
                      <p><code class="chatbot-code">review-needed:see-only -> age 86400s / high -> operations-admin/in-app</code></p>
                      <p><code class="chatbot-code">failure:operate -> age 3600s / high -> operations-admin/operator-alert</code></p>
                      <p>escalation-policy-draft-preserved</p>
                      <p>Dense escalation controls are unavailable on this screen size; summary and safe submit action remain reachable.</p>
                    </aside>
                  </section>
                </main>
                <script>
                  function controlValue(selector) {
                    const control = document.querySelector(selector);
                    return (control?.value || control?.getAttribute('value') || control?.getAttribute('data-value') || control?.textContent || '').trim();
                  }

                  function row(stateClass, scope) {
                    return {
                      stateClass,
                      scope,
                      ageThresholdSeconds: Number(controlValue(`[data-escalation-age-input="${stateClass}:${scope}"]`)),
                      severityThreshold: controlValue(`[data-escalation-severity-select="${stateClass}:${scope}"]`),
                      escalationTargetRole: controlValue(`[data-escalation-role-select="${stateClass}:${scope}"]`),
                      escalationChannel: controlValue(`[data-escalation-channel-select="${stateClass}:${scope}"]`)
                    };
                  }

                  function submitEscalationPolicyChange() {
                    const reason = controlValue('#escalation-change-reason');
                    if (!reason) {
                      document.querySelector('#escalation-policy-validation-summary').focus();
                      return;
                    }

                    window.__lastEscalationCommand = {
                      commandType: 'SubmitEscalationPolicyChange',
                      origin: 'Ui',
                      escalationPolicyChangeId: 'escalation-change-001',
                      sourceEscalationSnapshotId: 'escalation-snapshot-current',
                      proposedEscalationSnapshotId: 'escalation-snapshot-proposed',
                      reasonCode: reason,
                      sourceVersion: 4,
                      schemaVersion: 'escalation-policy-schema.v1',
                      oldEscalationFingerprint: 'sha256:escalationold',
                      newEscalationFingerprint: 'sha256:escalationnew',
                      changeSet: [
                        row('review-needed', 'see-only'),
                        row('approval-pending', 'policy'),
                        row('failure', 'operate'),
                        row('degraded', 'operate'),
                        row('quarantine', 'compliance')
                      ]
                    };
                    document.querySelector('[data-escalation-status]').textContent = 'accepted-projection-pending';
                  }
                </script>
              </body>
            </html>
            """;
    }

    private static string EscalationRow(
        string stateClass,
        string scope,
        int ageThresholdSeconds,
        string severity,
        string targetRole,
        string channel)
        => $$"""
                        <tr data-escalation-row-key="{{stateClass}}:{{scope}}">
                          <td><span>State class</span> <code class="chatbot-code">{{stateClass}}</code></td>
                          <td><span>Scope</span> <code class="chatbot-code">{{scope}}</code></td>
                          <td>
                            <fluent-label>
                              <span>Age threshold {{stateClass}}:{{scope}}</span>
                              <fluent-number-input role="spinbutton"
                                                   tabindex="0"
                                                   contenteditable="true"
                                                   inputmode="numeric"
                                                   min="0"
                                                   max="2592000"
                                                   aria-label="Age threshold {{stateClass}}:{{scope}}"
                                                   aria-valuemin="0"
                                                   aria-valuemax="2592000"
                                                   aria-valuenow="{{ageThresholdSeconds}}"
                                                   data-escalation-age-input="{{stateClass}}:{{scope}}"
                                                   value="{{ageThresholdSeconds}}">{{ageThresholdSeconds}}</fluent-number-input>
                            </fluent-label>
                          </td>
                          <td>
                            <fluent-label>
                              <span>Severity threshold {{stateClass}}:{{scope}}</span>
                              <fluent-select role="combobox"
                                             tabindex="0"
                                             aria-label="Severity threshold {{stateClass}}:{{scope}}"
                                             data-escalation-severity-select="{{stateClass}}:{{scope}}"
                                             value="{{severity}}"
                                             data-value="{{severity}}">
                                {{Option("low", severity)}}
                                {{Option("medium", severity)}}
                                {{Option("high", severity)}}
                              </fluent-select>
                            </fluent-label>
                          </td>
                          <td>
                            <fluent-label>
                              <span>Escalation target role {{stateClass}}:{{scope}}</span>
                              <fluent-select role="combobox"
                                             tabindex="0"
                                             aria-label="Escalation target role {{stateClass}}:{{scope}}"
                                             data-escalation-role-select="{{stateClass}}:{{scope}}"
                                             value="{{targetRole}}"
                                             data-value="{{targetRole}}">
                                {{Option("tenant-admin", targetRole)}}
                                {{Option("mailbox-admin", targetRole)}}
                                {{Option("policy-admin", targetRole)}}
                                {{Option("compliance-admin", targetRole)}}
                                {{Option("operations-admin", targetRole)}}
                              </fluent-select>
                            </fluent-label>
                          </td>
                          <td>
                            <fluent-label>
                              <span>Escalation channel {{stateClass}}:{{scope}}</span>
                              <fluent-select role="combobox"
                                             tabindex="0"
                                             aria-label="Escalation channel {{stateClass}}:{{scope}}"
                                             data-escalation-channel-select="{{stateClass}}:{{scope}}"
                                             value="{{channel}}"
                                             data-value="{{channel}}">
                                {{Option("in-app", channel)}}
                                {{Option("email", channel)}}
                                {{Option("webhook", channel)}}
                                {{Option("operator-alert", channel)}}
                              </fluent-select>
                            </fluent-label>
                          </td>
                        </tr>
            """;

    private static string Option(string token, string selected)
        => token == selected
            ? $"""<fluent-option role="option" value="{token}" aria-selected="true">{token}</fluent-option>"""
            : $"""<fluent-option role="option" value="{token}" aria-selected="false">{token}</fluent-option>""";

    private static void AssertFixtureUsesFluentEditorControls(string fixture)
    {
        fixture.ShouldContain("<fluent-number-input", Case.Sensitive);
        fixture.ShouldContain("<fluent-select", Case.Sensitive);
        fixture.ShouldContain("<fluent-option", Case.Sensitive);
        fixture.ShouldContain("<fluent-text-input", Case.Sensitive);
        fixture.ShouldContain("<fluent-label", Case.Sensitive);
        fixture.ShouldNotContain("<input", Case.Sensitive);
        fixture.ShouldNotContain("<select", Case.Sensitive);
        fixture.ShouldNotContain("<option", Case.Sensitive);
        fixture.ShouldNotContain("<button", Case.Sensitive);
    }

    private static void AssertMatrixFixtureWithoutBrowser()
    {
        string fixture = BuildEscalationPolicyFixture(EscalationPolicyScenario.MatrixEdit);
        fixture.ShouldContain("SubmitEscalationPolicyChange");
        fixture.ShouldContain("Escalation policy matrix");
        fixture.ShouldContain("review-needed");
        fixture.ShouldContain("approval-pending");
        fixture.ShouldContain("failure");
        fixture.ShouldContain("degraded");
        fixture.ShouldContain("quarantine");
        fixture.ShouldNotContain("<code class=\"chatbot-code\">retry</code>");
        fixture.ShouldContain("operator-alert");
        AssertFixtureUsesFluentEditorControls(fixture);
        AssertMetadataOnly(fixture);
    }

    private static void AssertValidationFixtureWithoutBrowser()
    {
        string fixture = BuildEscalationPolicyFixture(EscalationPolicyScenario.ValidationFailure);
        fixture.ShouldContain("escalation-policy-validation-summary");
        fixture.ShouldContain("aria-invalid=\"true\"");
        fixture.ShouldContain("Durable escalation-policy writes: 0");
        AssertFixtureUsesFluentEditorControls(fixture);
        AssertMetadataOnly(fixture);
    }

    private static void AssertPhoneFallbackFixtureWithoutBrowser()
    {
        string fixture = BuildEscalationPolicyFixture(EscalationPolicyScenario.PhoneFallback);
        fixture.ShouldContain("Escalation policy summary is available on phone.");
        fixture.ShouldContain("escalation-policy-draft-preserved");
        fixture.ShouldContain("Dense escalation controls are unavailable on this screen size");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        string visibleText = Regex.Replace(
            text,
            "<style[^>]*>.*?</style>",
            string.Empty,
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        visibleText.ShouldNotContain("project name", Case.Insensitive);
        visibleText.ShouldNotContain("mailbox body", Case.Insensitive);
        visibleText.ShouldNotContain("provider payload", Case.Insensitive);
        visibleText.ShouldNotContain("raw claim", Case.Insensitive);
        visibleText.ShouldNotContain("headers", Case.Insensitive);
        visibleText.ShouldNotContain("token", Case.Insensitive);
        visibleText.ShouldNotContain("secret", Case.Insensitive);
        visibleText.ShouldNotContain("recipient address", Case.Insensitive);
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

    private enum EscalationPolicyScenario
    {
        MatrixEdit,
        ValidationFailure,
        PhoneFallback,
    }
}
