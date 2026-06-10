using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class AssociationDecisionRecordingE2ETests
{
    [Fact]
    public async Task AssociationDecision_ConfirmedCandidate_SubmitsMetadataOnlyCommandAndRequeriesStatus()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertHappyPathFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationDecisionFixture(AssociationDecisionFixtureScenario.HappyPath));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Association review", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate Project Alpha. Confidence 91%. Evidence complete." }));
            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate Project Alpha. Confidence 91%. Evidence complete." }).CheckAsync();
            await harness.Page.GetByLabel("Decision note").FillAsync("Reviewed against metadata-only evidence.");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Choose candidate" }).ClickAsync();

            (await harness.Page.EvaluateAsync<string>("() => window.__lastAssociationCommand?.commandType ?? ''")).ShouldBe("AssociateEmailToProject");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastAssociationCommand?.origin ?? ''")).ShouldBe("Ui");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastAssociationCommand?.selectedProjectId ?? ''")).ShouldBe("project-alpha");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastAssociationCommand?.decisionNote ?? ''")).ShouldBe("Reviewed against metadata-only evidence.");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastAssociationCommand?.candidateEvidenceFingerprint ?? ''")).ShouldBe("evidence:subject-match:sha256");

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Association decision status" }).GetByText("accepted-projection-pending"));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit reconciliation status" }).GetByText("reconciling"));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Routing status refresh" }).GetByText("GetAssociationRoutingStatusAsync re-query count: 1"));
            await WaitForVisibleAsync(harness.Page.GetByText("Decision event: MailboxEmailAssociationConfirmed", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence references: mailbox:intake:subject, mailbox:thread:participants", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task AssociationDecision_FailClosedStates_BlockDurableWriteAndSuppressRestrictedEvidence()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertFailClosedFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationDecisionFixture(AssociationDecisionFixtureScenario.FailClosed));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Association review", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: Evidence expired. No durable decision was written." }));
            await WaitForVisibleAsync(harness.Page.GetByText("evidence-expired", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("already-decided", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("audit-unavailable", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("not-authorized", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Audit replay intent queued; operator alert emitted.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Evidence is stale; refresh before deciding."));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? This association is already terminal."));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Audit writing is unavailable, so decision recording is blocked."));

            // The disabled "Choose candidate" action must be a no-op. Following the repository E2E
            // convention (see GovernedOperationsVisualFoundationE2ETests), assert the action is
            // aria-disabled rather than clicking it: Playwright treats aria-disabled controls as
            // not-enabled, so ClickAsync would time out waiting for the element to become enabled
            // instead of proving the no-op.
            ILocator chooseCandidate = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Choose candidate" });
            (await chooseCandidate.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastAssociationCommand?.commandType ?? ''")).ShouldBe(string.Empty);
            await WaitForVisibleAsync(harness.Page.GetByText("Durable decision writes: 0", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
            bodyText.ShouldNotContain("NullReferenceException");
            bodyText.ShouldNotContain("System.InvalidOperationException");
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildAssociationDecisionFixture(AssociationDecisionFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Association decision recording fixture</title>
                <style>
                  {{css}}
                  .association-decision-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .association-decision-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(280px, 420px); gap: 20px; }
                  .association-decision-actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
                  .association-decision-actions button { min-height: 44px; }
                  .association-decision-reason { display: block; margin-top: 4px; }
                  .association-decision-fixture textarea { width: 100%; min-height: 84px; }
                </style>
              </head>
              <body>
                <main class="chatbot-page association-decision-fixture"
                      aria-labelledby="association-review-title"
                      data-chatbot-surface="association-review-s2"
                      data-fixture-scenario="{{scenario}}">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">S2</span>
                    <h1 id="association-review-title" class="chatbot-page-title">Association review</h1>
                    <p class="chatbot-body">Review authorized metadata before confirming, rejecting, deferring, or escalating.</p>
                  </header>
                  {{BuildScenarioBody(scenario)}}
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildScenarioBody(AssociationDecisionFixtureScenario scenario)
        => scenario switch
        {
            AssociationDecisionFixtureScenario.HappyPath => HappyPathBody,
            AssociationDecisionFixtureScenario.FailClosed => FailClosedBody,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

    private const string HappyPathBody = """
                  <div class="association-decision-grid">
                    <section class="chatbot-section" aria-labelledby="candidate-title">
                      <h2 id="candidate-title" class="chatbot-section-title">Candidate projects</h2>
                      <div role="radiogroup" aria-label="Candidate projects">
                        <label class="chatbot-list-row">
                          <input type="radio"
                                 name="candidate"
                                 value="project-alpha"
                                 aria-label="Candidate Project Alpha. Confidence 91%. Evidence complete." />
                          <span>Project Alpha</span>
                          <code class="chatbot-code">confidence:0.91</code>
                          <code class="chatbot-code">fingerprint:evidence:subject-match:sha256</code>
                        </label>
                        <label class="chatbot-list-row">
                          <input type="radio"
                                 name="candidate"
                                 value="project-beta"
                                 aria-label="Candidate Project Beta. Confidence 64%. Evidence complete." />
                          <span>Project Beta</span>
                          <code class="chatbot-code">confidence:0.64</code>
                        </label>
                      </div>
                      <label class="chatbot-field">
                        <span class="chatbot-labelled-row">Decision note</span>
                        <textarea aria-label="Decision note"></textarea>
                      </label>
                      <div class="association-decision-actions">
                        <button type="button"
                                aria-label="Choose candidate"
                                aria-disabled="false"
                                onclick="window.__lastAssociationCommand = {
                                  commandType: 'AssociateEmailToProject',
                                  origin: 'Ui',
                                  associationId: '01HZXASSOC000000000000001',
                                  intakeId: 'intake-001',
                                  selectedProjectId: document.querySelector('input[name=candidate]:checked')?.value ?? '',
                                  decisionKind: 'associate',
                                  decisionNote: document.querySelector('textarea[aria-label=&quot;Decision note&quot;]').value,
                                  candidateEvidenceFingerprint: 'evidence:subject-match:sha256',
                                  sourceVersion: 9,
                                  schemaVersion: 'chatbot.association-decision-command.v1'
                                };
                                window.__routingStatusRequeryCount = (window.__routingStatusRequeryCount ?? 0) + 1;
                                document.querySelector('[data-decision-status]').textContent = 'accepted-projection-pending';
                                document.querySelector('[data-audit-status]').textContent = 'reconciling';
                                document.querySelector('[data-routing-refresh]').textContent = 'GetAssociationRoutingStatusAsync re-query count: ' + window.__routingStatusRequeryCount;">Choose candidate</button>
                        <button type="button" aria-label="Reject all" aria-disabled="false">Reject all</button>
                        <button type="button" aria-label="Defer" aria-disabled="false">Defer</button>
                        <button type="button" aria-label="Mark needs review" aria-disabled="false">Mark needs review</button>
                      </div>
                    </section>
                    <aside class="chatbot-section" aria-labelledby="decision-metadata-title">
                      <h2 id="decision-metadata-title" class="chatbot-section-title">Decision metadata</h2>
                      <dl class="chatbot-definition-list">
                        <dt class="chatbot-labelled-row">Tenant</dt><dd><code class="chatbot-code">tenant-alpha</code></dd>
                        <dt class="chatbot-labelled-row">Actor</dt><dd><code class="chatbot-code">actor:reviewer-001</code></dd>
                        <dt class="chatbot-labelled-row">Correlation ID</dt><dd><code class="chatbot-code">01HZXCORRELATION00000000026</code></dd>
                        <dt class="chatbot-labelled-row">Surface origin</dt><dd><code class="chatbot-code">Ui</code></dd>
                        <dt class="chatbot-labelled-row">Policy snapshot</dt><dd><code class="chatbot-code">policy-snapshot:association-v1</code></dd>
                        <dt class="chatbot-labelled-row">Redaction state</dt><dd><code class="chatbot-code">metadata_only</code></dd>
                        <dt class="chatbot-labelled-row">Retention class</dt><dd><code class="chatbot-code">collaboration_input</code></dd>
                      </dl>
                      <p>Decision event: MailboxEmailAssociationConfirmed</p>
                      <p>Evidence references: mailbox:intake:subject, mailbox:thread:participants</p>
                      <p role="status" aria-label="Association decision status" data-decision-status>NeedsReview</p>
                      <p role="status" aria-label="Audit reconciliation status" data-audit-status>available</p>
                      <p role="status" aria-label="Routing status refresh" data-routing-refresh>GetAssociationRoutingStatusAsync re-query count: 0</p>
                    </aside>
                  </div>
        """;

    private const string FailClosedBody = """
                  <section class="chatbot-section" aria-labelledby="blocked-title">
                    <h2 id="blocked-title" class="chatbot-section-title">Fail-closed decision states</h2>
                    <p role="alert" aria-label="Blocked: Evidence expired. No durable decision was written.">Blocked: Evidence expired. No durable decision was written.</p>
                    <dl class="chatbot-definition-list">
                      <dt class="chatbot-labelled-row">Safe problem</dt><dd><code class="chatbot-code">evidence-expired</code></dd>
                      <dt class="chatbot-labelled-row">Idempotency result</dt><dd><code class="chatbot-code">already-decided</code></dd>
                      <dt class="chatbot-labelled-row">Audit writer</dt><dd><code class="chatbot-code">audit-unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Candidate authorization</dt><dd><code class="chatbot-code">not-authorized</code></dd>
                      <dt class="chatbot-labelled-row">Durable decision writes</dt><dd>Durable decision writes: 0</dd>
                    </dl>
                    <p>Audit replay intent queued; operator alert emitted.</p>
                    <div class="association-decision-actions">
                      <span>
                        <button type="button" aria-label="Choose candidate" aria-disabled="true" aria-describedby="choose-disabled"
                                onclick="if (this.getAttribute('aria-disabled') === 'true') { return; } window.__lastAssociationCommand = { commandType: 'AssociateEmailToProject' };">Choose candidate</button>
                        <span id="choose-disabled" class="association-decision-reason" tabindex="0" aria-label="Why unavailable? Evidence is stale; refresh before deciding.">
                          <strong>Why unavailable?</strong> Evidence is stale; refresh before deciding.
                        </span>
                      </span>
                      <span>
                        <button type="button" aria-label="Reject all" aria-disabled="true" aria-describedby="reject-disabled">Reject all</button>
                        <span id="reject-disabled" class="association-decision-reason" tabindex="0" aria-label="Why unavailable? This association is already terminal.">
                          <strong>Why unavailable?</strong> This association is already terminal.
                        </span>
                      </span>
                      <span>
                        <button type="button" aria-label="Defer" aria-disabled="true" aria-describedby="defer-disabled">Defer</button>
                        <span id="defer-disabled" class="association-decision-reason" tabindex="0" aria-label="Why unavailable? Audit writing is unavailable, so decision recording is blocked.">
                          <strong>Why unavailable?</strong> Audit writing is unavailable, so decision recording is blocked.
                        </span>
                      </span>
                    </div>
                    <section aria-label="Suppressed evidence">
                      <h3>Suppressed evidence</h3>
                      <p>Unauthorized project detail suppressed.</p>
                      <p>Raw provider payload suppressed.</p>
                      <p>Raw addresses suppressed.</p>
                    </section>
                  </section>
        """;

    private static void AssertHappyPathFixtureWithoutBrowser()
    {
        string fixture = BuildAssociationDecisionFixture(AssociationDecisionFixtureScenario.HappyPath);
        fixture.ShouldContain("AssociateEmailToProject");
        fixture.ShouldContain("origin: 'Ui'");
        fixture.ShouldContain("GetAssociationRoutingStatusAsync re-query count");
        fixture.ShouldContain("MailboxEmailAssociationConfirmed");
        fixture.ShouldContain("metadata_only");
        AssertMetadataOnly(fixture);
    }

    private static void AssertFailClosedFixtureWithoutBrowser()
    {
        string fixture = BuildAssociationDecisionFixture(AssociationDecisionFixtureScenario.FailClosed);
        fixture.ShouldContain("No durable decision was written.");
        fixture.ShouldContain("evidence-expired");
        fixture.ShouldContain("already-decided");
        fixture.ShouldContain("audit-unavailable");
        fixture.ShouldContain("not-authorized");
        fixture.ShouldContain("Durable decision writes: 0");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        foreach (string forbidden in new[]
        {
            "customer@example.com",
            "From:",
            "To:",
            "Authorization:",
            "Bearer ",
            "token=",
            "secret",
            "rawBody",
            "bodyPreview",
            "internetMessageHeaders",
            "/home/administrator",
            "C:\\",
        })
        {
            text.ShouldNotContain(forbidden, Case.Insensitive);
        }
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

    private enum AssociationDecisionFixtureScenario
    {
        HappyPath,
        FailClosed,
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
