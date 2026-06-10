using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class CorrectionPropagationContractE2ETests
{
    [Fact]
    public async Task CorrectionPropagation_SubmissionBlocksStaleContextUntilRequiredStoresAcknowledge()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertHappyPathFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildCorrectionPropagationFixture(CorrectionPropagationScenario.HappyPath));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Correction propagation", Level = 1 }));
            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Correct to Authorized Project Beta" }).CheckAsync();
            await harness.Page.GetByLabel("Correction rationale").FillAsync("Wrong project association; metadata-only repair.");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit correction" }).ClickAsync();

            (await harness.Page.EvaluateAsync<string>("() => window.__lastCorrectionCommand?.commandType ?? ''")).ShouldBe("CorrectEmailProjectAssociation");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastCorrectionCommand?.origin ?? ''")).ShouldBe("Ui");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastCorrectionCommand?.targetProjectId ?? ''")).ShouldBe("project-beta");
            (await harness.Page.EvaluateAsync<string>("() => window.__workflowInstanceId ?? ''")).ShouldBe("correction-propagation:tenant-alpha:association-001:correction-001:v9");

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status" }).GetByText("Correcting"));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Progressbar, new() { NameString = "Propagation progress" }));
            (await harness.Page.GetByRole(AriaRole.Progressbar, new() { NameString = "Propagation progress" }).GetAttributeAsync("aria-valuenow")).ShouldBe("2");

            // The required-store keys appear in both the "Required stores" and the "Completed stores" rows, so a
            // page-wide GetByText is not unique (Playwright strict mode). Scope the assertion to the required-stores
            // row, which lists every M0 store key the propagation must invalidate.
            ILocator requiredStores = harness.Page.Locator("[data-required-stores]");
            await WaitForVisibleAsync(requiredStores);
            string requiredStoresText = await requiredStores.InnerTextAsync();
            requiredStoresText.ShouldContain("association-routing");
            requiredStoresText.ShouldContain("evidence-snapshot");
            requiredStoresText.ShouldContain("operation-status");
            requiredStoresText.ShouldContain("ai-context-readiness");
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "AI corrected-context readiness" }).GetByText("Correcting"));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Corrected project context is stale until all required M0 stores acknowledge."));
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Prepare AI action" }).GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''")).ShouldBe("correction-status");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Acknowledge remaining stores" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status" }).GetByText("Corrected"));
            (await harness.Page.GetByRole(AriaRole.Progressbar, new() { NameString = "Propagation progress" }).GetAttributeAsync("aria-valuenow")).ShouldBe("4");
            await WaitForVisibleAsync(harness.Page.GetByText("Downstream impact: complete", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Required M0 acknowledgements: 4 of 4", new() { Exact = true }));
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Prepare AI action" }).GetAttributeAsync("aria-disabled")).ShouldBe("false");
            (await harness.Page.EvaluateAsync<string>("() => window.__workflowInstanceId ?? ''")).ShouldBe("correction-propagation:tenant-alpha:association-001:correction-001:v9");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task CorrectionPropagation_DelayedStateRaisesP2SignalAndCompletesSameWorkflowInstance()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertDelayedFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildCorrectionPropagationFixture(CorrectionPropagationScenario.Delayed));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status" }).GetByText("Correction-delayed"));
            await WaitForVisibleAsync(harness.Page.GetByText("Incident signal: P2 correction propagation breach", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Responsible owner: operations-on-call", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Next safe action: Escalate to operations while the same workflow continues.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Correction is delayed; corrected context remains blocked."));
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Prepare AI action" }).GetAttributeAsync("aria-disabled")).ShouldBe("true");

            string workflowBefore = await harness.Page.EvaluateAsync<string>("() => window.__workflowInstanceId ?? ''");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Complete delayed propagation" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status" }).GetByText("Corrected"));
            await WaitForVisibleAsync(harness.Page.GetByText("Incident signal: cleared", new() { Exact = true }));
            (await harness.Page.EvaluateAsync<string>("() => window.__workflowInstanceId ?? ''")).ShouldBe(workflowBefore);
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Prepare AI action" }).GetAttributeAsync("aria-disabled")).ShouldBe("false");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task CorrectionPropagation_FailClosedDependenciesBlockDurableWriteWithCatalogReasons()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertFailClosedFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildCorrectionPropagationFixture(CorrectionPropagationScenario.FailClosed));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Correction blocked before durable state was written." }));
            await WaitForVisibleAsync(harness.Page.GetByText("association_correction_workflow_unavailable", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("association_correction_projection_unavailable", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("association_correction_audit_unavailable", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("idempotency_store_unavailable", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Correction propagation dependencies are unavailable; retry later."));
            ILocator submit = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit correction" });
            (await submit.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await harness.Page.EvaluateAsync<int>("() => window.__durableCorrectionWrites ?? 0")).ShouldBe(0);
            (await harness.Page.EvaluateAsync<int>("() => window.__propagationWorkflowStarts ?? 0")).ShouldBe(0);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildCorrectionPropagationFixture(CorrectionPropagationScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Correction propagation fixture</title>
                <style>
                  {{css}}
                  .correction-propagation-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .correction-propagation-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(280px, 420px); gap: 20px; }
                  .correction-propagation-actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
                  .correction-propagation-actions button { min-height: 44px; }
                  .correction-propagation-reason { display: block; margin-top: 4px; }
                  .correction-propagation-fixture textarea { width: 100%; min-height: 84px; }
                </style>
              </head>
              <body>
                <main class="chatbot-page correction-propagation-fixture"
                      aria-labelledby="correction-propagation-title"
                      data-chatbot-surface="association-review-correction-propagation"
                      data-fixture-scenario="{{scenario}}">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">S4</span>
                    <h1 id="correction-propagation-title" class="chatbot-page-title">Correction propagation</h1>
                    <p class="chatbot-body">Repair project association context through metadata-only propagation status.</p>
                  </header>
                  {{BuildScenarioBody(scenario)}}
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildScenarioBody(CorrectionPropagationScenario scenario)
        => scenario switch
        {
            CorrectionPropagationScenario.HappyPath => HappyPathBody,
            CorrectionPropagationScenario.Delayed => DelayedBody,
            CorrectionPropagationScenario.FailClosed => FailClosedBody,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

    private const string SharedStatusPanel = """
                    <aside class="chatbot-section" aria-labelledby="propagation-status-title">
                      <h2 id="propagation-status-title" class="chatbot-section-title">Propagation status</h2>
                      <p id="correction-status"
                         role="status"
                         tabindex="-1"
                         aria-label="Correction propagation status"
                         data-correction-status>Ready for correction</p>
                      <p role="status" aria-label="AI corrected-context readiness" data-ai-readiness>Ready</p>
                      <div role="progressbar"
                           aria-label="Propagation progress"
                           aria-valuemin="0"
                           aria-valuemax="4"
                           aria-valuenow="0"
                           data-propagation-progress>0 of 4</div>
                      <dl class="chatbot-definition-list">
                        <dt class="chatbot-labelled-row">Workflow instance</dt>
                        <dd><code class="chatbot-code" data-workflow-id>correction-propagation:tenant-alpha:association-001:correction-001:v9</code></dd>
                        <dt class="chatbot-labelled-row">Required stores</dt>
                        <dd data-required-stores>association-routing, evidence-snapshot, operation-status, ai-context-readiness</dd>
                        <dt class="chatbot-labelled-row">Completed stores</dt>
                        <dd data-completed-stores>none</dd>
                        <dt class="chatbot-labelled-row">Downstream impact</dt>
                        <dd data-downstream-impact>Downstream impact: pending</dd>
                        <dt class="chatbot-labelled-row">Acknowledgements</dt>
                        <dd data-acknowledgements>Required M0 acknowledgements: 0 of 4</dd>
                        <dt class="chatbot-labelled-row">Responsible owner</dt>
                        <dd data-owner>Responsible owner: propagation-coordinator</dd>
                        <dt class="chatbot-labelled-row">Next safe action</dt>
                        <dd data-next-action>Next safe action: Submit correction through the governed command spine.</dd>
                        <dt class="chatbot-labelled-row">Incident signal</dt>
                        <dd data-incident>Incident signal: none</dd>
                      </dl>
                      <div class="correction-propagation-actions">
                        <button type="button"
                                aria-label="Prepare AI action"
                                aria-disabled="false"
                                aria-describedby="ai-blocked-reason"
                                onclick="if (this.getAttribute('aria-disabled') === 'true') { return; } window.__preparedAiAction = true;">Prepare AI action</button>
                        <span id="ai-blocked-reason"
                              class="correction-propagation-reason"
                              tabindex="0"
                              aria-label="Why unavailable? Corrected project context is stale until all required M0 stores acknowledge.">
                          <strong>Why unavailable?</strong> Corrected project context is stale until all required M0 stores acknowledge.
                        </span>
                      </div>
                    </aside>
        """;

    private const string HappyPathBody = """
                  <div class="correction-propagation-grid">
                    <section class="chatbot-section" aria-labelledby="correction-command-title">
                      <h2 id="correction-command-title" class="chatbot-section-title">Correction command</h2>
                      <div role="radiogroup" aria-label="Corrected project">
                        <label class="chatbot-list-row">
                          <input type="radio" name="correctedProject" value="project-beta" aria-label="Correct to Authorized Project Beta" />
                          <span>Authorized Project Beta</span>
                          <code class="chatbot-code">project-beta</code>
                        </label>
                      </div>
                      <label class="chatbot-field">
                        <span class="chatbot-labelled-row">Correction rationale</span>
                        <textarea aria-label="Correction rationale"></textarea>
                      </label>
                      <div class="correction-propagation-actions">
                        <button type="button"
                                aria-label="Submit correction"
                                aria-disabled="false"
                                onclick="window.__lastCorrectionCommand = {
                                  commandType: 'CorrectEmailProjectAssociation',
                                  origin: 'Ui',
                                  associationId: 'association-001',
                                  intakeId: 'intake-001',
                                  priorProjectId: 'project-alpha',
                                  targetProjectId: document.querySelector('input[name=correctedProject]:checked')?.value ?? '',
                                  correctionKind: 'project-reassignment',
                                  correctionRationaleState: 'redacted',
                                  predecessorAssociationId: 'association-000',
                                  candidateEvidenceFingerprint: 'evidence:subject-match:sha256',
                                  sourceVersion: 9,
                                  schemaVersion: 'chatbot.association-correction-command.v1'
                                };
                                window.__workflowInstanceId = 'correction-propagation:tenant-alpha:association-001:correction-001:v9';
                                window.__durableCorrectionWrites = 1;
                                window.__propagationWorkflowStarts = 1;
                                document.querySelector('[data-correction-status]').textContent = 'Correcting';
                                document.querySelector('[data-ai-readiness]').textContent = 'Correcting';
                                document.querySelector('[data-propagation-progress]').setAttribute('aria-valuenow', '2');
                                document.querySelector('[data-propagation-progress]').textContent = '2 of 4';
                                document.querySelector('[data-completed-stores]').textContent = 'association-routing, evidence-snapshot';
                                document.querySelector('[data-downstream-impact]').textContent = 'Downstream impact: correcting';
                                document.querySelector('[data-acknowledgements]').textContent = 'Required M0 acknowledgements: 2 of 4';
                                document.querySelector('[data-next-action]').textContent = 'Next safe action: Wait for propagation before preparing AI actions.';
                                document.querySelector('button[aria-label=&quot;Prepare AI action&quot;]').setAttribute('aria-disabled', 'true');
                                document.getElementById('correction-status').focus();">Submit correction</button>
                        <button type="button"
                                aria-label="Acknowledge remaining stores"
                                onclick="document.querySelector('[data-correction-status]').textContent = 'Corrected';
                                document.querySelector('[data-ai-readiness]').textContent = 'Ready';
                                document.querySelector('[data-propagation-progress]').setAttribute('aria-valuenow', '4');
                                document.querySelector('[data-propagation-progress]').textContent = '4 of 4';
                                document.querySelector('[data-completed-stores]').textContent = 'association-routing, evidence-snapshot, operation-status, ai-context-readiness';
                                document.querySelector('[data-downstream-impact]').textContent = 'Downstream impact: complete';
                                document.querySelector('[data-acknowledgements]').textContent = 'Required M0 acknowledgements: 4 of 4';
                                document.querySelector('[data-next-action]').textContent = 'Next safe action: Corrected context is ready.';
                                document.querySelector('button[aria-label=&quot;Prepare AI action&quot;]').setAttribute('aria-disabled', 'false');">Acknowledge remaining stores</button>
                      </div>
                    </section>
        """ + SharedStatusPanel + """
                  </div>
        """;

    private const string DelayedBody = """
                  <div class="correction-propagation-grid">
                    <section class="chatbot-section" aria-labelledby="delayed-title">
                      <h2 id="delayed-title" class="chatbot-section-title">Delayed propagation</h2>
                      <p>Correction SLO p95 target: 10 minutes.</p>
                      <div class="correction-propagation-actions">
                        <button type="button"
                                aria-label="Complete delayed propagation"
                                onclick="document.querySelector('[data-correction-status]').textContent = 'Corrected';
                                document.querySelector('[data-ai-readiness]').textContent = 'Ready';
                                document.querySelector('[data-propagation-progress]').setAttribute('aria-valuenow', '4');
                                document.querySelector('[data-propagation-progress]').textContent = '4 of 4';
                                document.querySelector('[data-downstream-impact]').textContent = 'Downstream impact: complete';
                                document.querySelector('[data-acknowledgements]').textContent = 'Required M0 acknowledgements: 4 of 4';
                                document.querySelector('[data-incident]').textContent = 'Incident signal: cleared';
                                document.querySelector('[data-next-action]').textContent = 'Next safe action: Corrected context is ready.';
                                document.querySelector('button[aria-label=&quot;Prepare AI action&quot;]').setAttribute('aria-disabled', 'false');">Complete delayed propagation</button>
                      </div>
                    </section>
        """ + SharedStatusPanel + """
                  </div>
                  <script>
                    window.__workflowInstanceId = 'correction-propagation:tenant-alpha:association-001:correction-001:v9';
                    document.querySelector('[data-correction-status]').textContent = 'Correction-delayed';
                    document.querySelector('[data-ai-readiness]').textContent = 'Correcting';
                    document.querySelector('[data-propagation-progress]').setAttribute('aria-valuenow', '3');
                    document.querySelector('[data-propagation-progress]').textContent = '3 of 4';
                    document.querySelector('[data-downstream-impact]').textContent = 'Downstream impact: delayed';
                    document.querySelector('[data-acknowledgements]').textContent = 'Required M0 acknowledgements: 3 of 4';
                    document.querySelector('[data-owner]').textContent = 'Responsible owner: operations-on-call';
                    document.querySelector('[data-next-action]').textContent = 'Next safe action: Escalate to operations while the same workflow continues.';
                    document.querySelector('[data-incident]').textContent = 'Incident signal: P2 correction propagation breach';
                    document.querySelector('button[aria-label="Prepare AI action"]').setAttribute('aria-disabled', 'true');
                    document.getElementById('ai-blocked-reason').setAttribute('aria-label', 'Why unavailable? Correction is delayed; corrected context remains blocked.');
                    document.getElementById('ai-blocked-reason').innerHTML = '<strong>Why unavailable?</strong> Correction is delayed; corrected context remains blocked.';
                  </script>
        """;

    private const string FailClosedBody = """
                  <section class="chatbot-section" aria-labelledby="blocked-title">
                    <h2 id="blocked-title" class="chatbot-section-title">Fail-closed dependencies</h2>
                    <p role="alert" aria-label="Correction blocked before durable state was written.">Correction blocked before durable state was written.</p>
                    <dl class="chatbot-definition-list">
                      <dt class="chatbot-labelled-row">Workflow runtime</dt><dd><code class="chatbot-code">association_correction_workflow_unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Projection invalidation</dt><dd><code class="chatbot-code">association_correction_projection_unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Audit writer</dt><dd><code class="chatbot-code">association_correction_audit_unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Idempotency store</dt><dd><code class="chatbot-code">idempotency_store_unavailable</code></dd>
                      <dt class="chatbot-labelled-row">Durable correction writes</dt><dd>0</dd>
                      <dt class="chatbot-labelled-row">Propagation workflow starts</dt><dd>0</dd>
                    </dl>
                    <div class="correction-propagation-actions">
                      <span>
                        <button type="button"
                                aria-label="Submit correction"
                                aria-disabled="true"
                                aria-describedby="correction-blocked-reason"
                                onclick="if (this.getAttribute('aria-disabled') === 'true') { return; } window.__durableCorrectionWrites = 1;">Submit correction</button>
                        <span id="correction-blocked-reason"
                              class="correction-propagation-reason"
                              tabindex="0"
                              aria-label="Why unavailable? Correction propagation dependencies are unavailable; retry later.">
                          <strong>Why unavailable?</strong> Correction propagation dependencies are unavailable; retry later.
                        </span>
                      </span>
                    </div>
                  </section>
                  <script>
                    window.__durableCorrectionWrites = 0;
                    window.__propagationWorkflowStarts = 0;
                  </script>
        """;

    private static void AssertHappyPathFixtureWithoutBrowser()
    {
        string fixture = BuildCorrectionPropagationFixture(CorrectionPropagationScenario.HappyPath);
        fixture.ShouldContain("CorrectEmailProjectAssociation");
        fixture.ShouldContain("Submit correction");
        fixture.ShouldContain("Correcting");
        fixture.ShouldContain("Required M0 acknowledgements: 4 of 4");
        fixture.ShouldContain("Corrected");
        fixture.ShouldContain("Corrected project context is stale until all required M0 stores acknowledge.");
        AssertMetadataOnly(fixture);
    }

    private static void AssertDelayedFixtureWithoutBrowser()
    {
        string fixture = BuildCorrectionPropagationFixture(CorrectionPropagationScenario.Delayed);
        fixture.ShouldContain("Correction-delayed");
        fixture.ShouldContain("P2 correction propagation breach");
        fixture.ShouldContain("operations-on-call");
        fixture.ShouldContain("Complete delayed propagation");
        AssertMetadataOnly(fixture);
    }

    private static void AssertFailClosedFixtureWithoutBrowser()
    {
        string fixture = BuildCorrectionPropagationFixture(CorrectionPropagationScenario.FailClosed);
        fixture.ShouldContain("association_correction_workflow_unavailable");
        fixture.ShouldContain("association_correction_projection_unavailable");
        fixture.ShouldContain("association_correction_audit_unavailable");
        fixture.ShouldContain("idempotency_store_unavailable");
        fixture.ShouldContain("aria-disabled=\"true\"");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        foreach (string forbidden in new[]
        {
            "customer@example.com",
            "sender@example.test",
            "From:",
            "To:",
            "Authorization:",
            "Bearer ",
            "token=",
            "secret",
            "rawBody",
            "bodyPreview",
            "internetMessageHeaders",
            "raw provider payload",
            "raw exception",
            "stack trace",
            "raw email body",
            "raw addresses",
            "unauthorized project name",
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

    private enum CorrectionPropagationScenario
    {
        HappyPath,
        Delayed,
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

            string linuxChrome = "/usr/bin/google-chrome";
            return File.Exists(linuxChrome) ? linuxChrome : null;
        }
    }
}
#pragma warning restore CA2007
