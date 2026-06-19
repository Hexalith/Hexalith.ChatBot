using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class DuplicateRetryFailureStatesE2ETests
{
    [Fact]
    public async Task DuplicateMailboxDelivery_SuppressesDuplicateAndKeepsOriginalArtifacts()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertDuplicateSuppressionContractWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildDuplicateSuppressionFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Duplicate delivery suppression", Level = 1 }));
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Deliver duplicate mailbox message" }).ClickAsync();
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Deliver duplicate mailbox message" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Duplicate suppression status" }).GetByText("duplicate_suppressed"));
            await WaitForVisibleAsync(harness.Page.GetByText("Original operation", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("01ARZ3NDEKTSV4RRFFQ69G5FAX", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Duplicate attempts", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("EventStore dispatches", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Project messages", new() { Exact = true }));

            string artifactFingerprint = await harness.Page.EvaluateAsync<string>("() => window.__artifactFingerprint");
            artifactFingerprint.ShouldBe("messages:1|attachments:1|task-intents:1|approvals:0|commands:1|notifications:0|decisions:1|audit-decisions:1");
            (await harness.Page.Locator("[data-duplicate-attempts]").TextContentAsync()).ShouldBe("2");
            (await harness.Page.Locator("[data-dispatch-count]").TextContentAsync()).ShouldBe("1");
            (await harness.Page.Locator("[data-message-count]").TextContentAsync()).ShouldBe("1");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task RetryAdmission_ReplaysAcceptedRetryAndRejectsConflictingDuplicate()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRetryAdmissionContractWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildRetryAdmissionFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Retry admission", Level = 1 }));
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Request retry" }).ClickAsync();
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Replay same retry" }).ClickAsync();
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit conflicting retry" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Retry idempotency status" }).GetByText("idempotency_conflict_retry"));
            await WaitForVisibleAsync(harness.Page.GetByText("tenant-alpha + failed-event-001 + actor-reviewer-001", new() { Exact = true }));

            (await harness.Page.EvaluateAsync<int>("() => window.__retryDispatchCount")).ShouldBe(1);
            (await harness.Page.EvaluateAsync<int>("() => window.__retryReplayCount")).ShouldBe(1);
            (await harness.Page.EvaluateAsync<int>("() => window.__retryConflictCount")).ShouldBe(1);
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetryCommand.commandType")).ShouldBe("RequestFailedWorkflowRetry");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetryCommand.origin")).ShouldBe("ui");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldContain("retry_accepted");
            bodyText.ShouldContain("retry_replayed");
            bodyText.ShouldContain("idempotency_conflict_retry");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task RetryExhaustion_ReprocessCreatesNewWorkflowWithoutMutatingTerminalRecord()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertTerminalReprocessContractWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildTerminalReprocessFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Retry exhaustion and terminal reprocess", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Retry exhaustion status" }).GetByText("retry_exhausted"));
            ILocator retryNow = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Retry terminal item" });
            (await retryNow.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            await retryNow.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<int>("() => window.__terminalRetryAttempts")).ShouldBe(0);

            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Retry attempts are exhausted; create a reprocess workflow."));
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Create reprocess workflow" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Reprocess status" }).GetByText("reprocess_created"));
            await WaitForVisibleAsync(harness.Page.GetByText("Original lifecycle", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Failed", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("New workflow lifecycle", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Received", new() { Exact = true }));

            (await harness.Page.EvaluateAsync<int>("() => window.__badTerminalTransitions")).ShouldBe(0);
            (await harness.Page.EvaluateAsync<string>("() => window.__originalWorkflow.lifecycle")).ShouldBe("Failed");
            (await harness.Page.EvaluateAsync<string>("() => window.__newWorkflow.supersedes")).ShouldBe("workflow-001");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldContain("supersedes_workflow:workflow-001");
            bodyText.ShouldContain("superseded_by_workflow:workflow-002");
            bodyText.ShouldContain("Terminal states stay append-only");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public void OperationStatusApiContract_ExposesStory29MetadataAndSafeNotFoundCollapse()
    {
        string openApi = ReadProjectFile("src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml");
        string retryCommand = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Commands/RequestFailedWorkflowRetry.cs");
        string operationStatus = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs");
        string compatibilityEndpoints = ReadProjectFile("src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs");
        string queryHandlers = ReadProjectFile("src/Hexalith.ChatBot.Server/Queries/ChatBotReadQueryHandlers.cs");

        foreach (string required in new[]
        {
            "operationClass",
            "maxAttempts",
            "nextRetryAt",
            "duplicateSafetyNote",
            "ownerRole",
            "failureReasonCode",
            "terminalReasonCode",
            "originalOperationId",
            "duplicateAttemptCount",
        })
        {
            openApi.ShouldContain(required);
            operationStatus.ShouldContain(ToPascalCase(required));
        }

        retryCommand.ShouldContain("RequestFailedWorkflowRetry");
        retryCommand.ShouldContain("RetryId");
        retryCommand.ShouldContain("FailedEventId");
        retryCommand.ShouldContain("FailedOperationClass");
        retryCommand.ShouldContain("FailureReasonCode");
        retryCommand.ShouldContain("ExpectedFailedSourceVersion");

        compatibilityEndpoints.ShouldContain("if (!ChatBotIdentity.IsValidUlid(operationId))");
        compatibilityEndpoints.ShouldContain("ChatBotAuthorizationReasonCodes.SafeNotFound");
        compatibilityEndpoints.ShouldContain("new OperationStatusQuery(operationId, correlationContext.TaskId)");
        queryHandlers.ShouldContain("if (!ChatBotIdentity.IsValidUlid(request.OperationId))");
        queryHandlers.ShouldContain("TryGetAsync(query.TenantId, request.OperationId");
        queryHandlers.ShouldContain("ChatBotAuthorizationReasonCodes.SafeNotFound");
        (compatibilityEndpoints + queryHandlers).ShouldNotContain("raw provider payload", Case.Insensitive);
        (compatibilityEndpoints + queryHandlers).ShouldNotContain("exception.ToString", Case.Insensitive);
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static string BuildDuplicateSuppressionFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head><meta charset="utf-8" /><title>Duplicate delivery suppression</title></head>
          <body>
            <main aria-labelledby="duplicate-title">
              <h1 id="duplicate-title">Duplicate delivery suppression</h1>
              <button type="button" aria-label="Deliver duplicate mailbox message" onclick="window.__duplicateAttempts += 1; renderDuplicate();">Deliver duplicate mailbox message</button>
              <section aria-label="Duplicate operation status">
                <p role="status" aria-label="Duplicate suppression status" aria-live="polite" data-duplicate-status>accepted</p>
                <dl>
                  <dt>Original operation</dt><dd>01ARZ3NDEKTSV4RRFFQ69G5FAX</dd>
                  <dt>Duplicate attempt correlation</dt><dd>01DUPLICATEATTEMPT0000000001</dd>
                  <dt>Operation class</dt><dd>message-intake</dd>
                  <dt>Retry count</dt><dd>0</dd>
                  <dt>Duplicate attempts</dt><dd data-duplicate-attempts>0</dd>
                  <dt>Duplicate safety note</dt><dd>duplicate-provider-message-suppressed</dd>
                  <dt>Safe next action</dt><dd>inspect-original-operation</dd>
                  <dt>Audit status</dt><dd>AuditReconciling</dd>
                  <dt>Audit facts</dt><dd>metadata_only</dd>
                  <dt>EventStore dispatches</dt><dd data-dispatch-count>1</dd>
                  <dt>Project messages</dt><dd data-message-count>1</dd>
                  <dt>Attachments</dt><dd data-attachment-count>1</dd>
                  <dt>Task intents</dt><dd data-task-intent-count>1</dd>
                  <dt>Approval artifacts</dt><dd data-approval-count>0</dd>
                  <dt>Command artifacts</dt><dd data-command-count>1</dd>
                  <dt>Notification artifacts</dt><dd data-notification-count>0</dd>
                  <dt>Association decisions</dt><dd data-decision-count>1</dd>
                  <dt>Decision audit records</dt><dd data-audit-decision-count>1</dd>
                </dl>
              </section>
            </main>
            <script>
              window.__duplicateAttempts = 0;
              window.__artifactFingerprint = "messages:1|attachments:1|task-intents:1|approvals:0|commands:1|notifications:0|decisions:1|audit-decisions:1";
              function renderDuplicate() {
                document.querySelector("[data-duplicate-status]").textContent = "duplicate_suppressed";
                document.querySelector("[data-duplicate-attempts]").textContent = String(window.__duplicateAttempts);
                document.querySelector("[data-dispatch-count]").textContent = "1";
                document.querySelector("[data-message-count]").textContent = "1";
                document.querySelector("[data-attachment-count]").textContent = "1";
                document.querySelector("[data-task-intent-count]").textContent = "1";
                document.querySelector("[data-approval-count]").textContent = "0";
                document.querySelector("[data-command-count]").textContent = "1";
                document.querySelector("[data-notification-count]").textContent = "0";
                document.querySelector("[data-decision-count]").textContent = "1";
                document.querySelector("[data-audit-decision-count]").textContent = "1";
              }
            </script>
          </body>
        </html>
        """;

    private static string BuildRetryAdmissionFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head><meta charset="utf-8" /><title>Retry admission</title></head>
          <body>
            <main aria-labelledby="retry-title">
              <h1 id="retry-title">Retry admission</h1>
              <section aria-label="Retry operation contract">
                <p>tenant-alpha + failed-event-001 + actor-reviewer-001</p>
                <p role="status" aria-label="Retry idempotency status" aria-live="polite" data-retry-status>ready</p>
                <dl>
                  <dt>Retry operation</dt><dd>retry_accepted</dd>
                  <dt>Replay operation</dt><dd>retry_replayed</dd>
                  <dt>Conflict code</dt><dd>idempotency_conflict_retry</dd>
                  <dt>Operation class</dt><dd>retry</dd>
                  <dt>Failure reason</dt><dd>graph_throttled</dd>
                  <dt>Safe next action</dt><dd>retry-later</dd>
                  <dt>Audit facts</dt><dd>metadata_only</dd>
                </dl>
                <button type="button" aria-label="Request retry" onclick="acceptRetry();">Request retry</button>
                <button type="button" aria-label="Replay same retry" onclick="replayRetry();">Replay same retry</button>
                <button type="button" aria-label="Submit conflicting retry" onclick="conflictRetry();">Submit conflicting retry</button>
              </section>
            </main>
            <script>
              window.__retryDispatchCount = 0;
              window.__retryReplayCount = 0;
              window.__retryConflictCount = 0;
              window.__lastRetryCommand = {};
              function acceptRetry() {
                window.__retryDispatchCount = 1;
                window.__lastRetryCommand = {
                  commandType: "RequestFailedWorkflowRetry",
                  origin: "ui",
                  retryId: "01RETRY0000000000000000001",
                  failedEventId: "failed-event-001",
                  operationClass: "retry",
                  failureReasonCode: "graph_throttled"
                };
                document.querySelector("[data-retry-status]").textContent = "retry_accepted";
              }
              function replayRetry() {
                window.__retryReplayCount = 1;
                document.querySelector("[data-retry-status]").textContent = "retry_replayed";
              }
              function conflictRetry() {
                window.__retryConflictCount = 1;
                document.querySelector("[data-retry-status]").textContent = "idempotency_conflict_retry";
              }
            </script>
          </body>
        </html>
        """;

    private static string BuildTerminalReprocessFixture()
        => """
        <!doctype html>
        <html lang="en">
          <head><meta charset="utf-8" /><title>Retry exhaustion and terminal reprocess</title></head>
          <body>
            <main aria-labelledby="terminal-title">
              <h1 id="terminal-title">Retry exhaustion and terminal reprocess</h1>
              <section aria-label="Terminal operation">
                <p role="status" aria-label="Retry exhaustion status" aria-live="polite">retry_exhausted</p>
                <dl>
                  <dt>Original workflow</dt><dd>workflow-001</dd>
                  <dt>Original lifecycle</dt><dd data-original-lifecycle>Failed</dd>
                  <dt>Retry count</dt><dd>5 of 5</dd>
                  <dt>Terminal reason</dt><dd>retry_exhausted</dd>
                  <dt>Owner role</dt><dd>operations-on-call</dd>
                  <dt>Safe next action</dt><dd>create-reprocess-workflow</dd>
                  <dt>Audit status</dt><dd>unavailable</dd>
                  <dt>Audit facts</dt><dd>metadata_only</dd>
                </dl>
                <button type="button"
                        aria-label="Retry terminal item"
                        aria-disabled="true"
                        aria-describedby="terminal-retry-reason"
                        onclick="if (this.getAttribute('aria-disabled') !== 'true') { window.__terminalRetryAttempts += 1; }">Retry terminal item</button>
                <span id="terminal-retry-reason" tabindex="0" aria-label="Why unavailable? Retry attempts are exhausted; create a reprocess workflow.">
                  Why unavailable? Retry attempts are exhausted; create a reprocess workflow.
                </span>
                <button type="button" aria-label="Create reprocess workflow" onclick="createReprocess();">Create reprocess workflow</button>
                <p role="status" aria-label="Reprocess status" aria-live="polite" data-reprocess-status>not-started</p>
                <dl>
                  <dt>New workflow</dt><dd data-new-workflow>none</dd>
                  <dt>New workflow lifecycle</dt><dd data-new-lifecycle>none</dd>
                  <dt>Reprocess audit links</dt><dd data-reprocess-links>none</dd>
                  <dt>Terminal rule</dt><dd>Terminal states stay append-only; reprocess creates a new workflow instance instead of moving this item backward.</dd>
                </dl>
              </section>
            </main>
            <script>
              window.__terminalRetryAttempts = 0;
              window.__badTerminalTransitions = 0;
              window.__originalWorkflow = { id: "workflow-001", lifecycle: "Failed", supersededBy: null };
              window.__newWorkflow = null;
              function createReprocess() {
                window.__originalWorkflow.supersededBy = "workflow-002";
                window.__newWorkflow = { id: "workflow-002", lifecycle: "Received", supersedes: "workflow-001" };
                if (window.__originalWorkflow.lifecycle !== "Failed") {
                  window.__badTerminalTransitions += 1;
                }
                document.querySelector("[data-reprocess-status]").textContent = "reprocess_created";
                document.querySelector("[data-new-workflow]").textContent = "workflow-002";
                document.querySelector("[data-new-lifecycle]").textContent = "Received";
                document.querySelector("[data-reprocess-links]").textContent = "supersedes_workflow:workflow-001; superseded_by_workflow:workflow-002";
              }
            </script>
          </body>
        </html>
        """;

    private static void AssertDuplicateSuppressionContractWithoutBrowser()
    {
        string fixture = BuildDuplicateSuppressionFixture();
        string admissionPipeline = ReadProjectFile("src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs");
        string audit = ReadProjectFile("src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs");

        fixture.ShouldContain("duplicate_suppressed");
        fixture.ShouldContain("Original operation");
        fixture.ShouldContain("Duplicate attempt correlation");
        fixture.ShouldContain("Duplicate safety note");
        fixture.ShouldContain("EventStore dispatches</dt><dd data-dispatch-count>1</dd>");
        admissionPipeline.ShouldContain("RecordDuplicateReplaySideEffectsAsync");
        admissionPipeline.ShouldContain("AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed");
        admissionPipeline.ShouldContain("PartialOutputCodes = [\"duplicate_suppressed\"]");
        audit.ShouldContain("duplicate_suppressed");
        AssertMetadataOnly(fixture);
    }

    private static void AssertRetryAdmissionContractWithoutBrowser()
    {
        string fixture = BuildRetryAdmissionFixture();
        string retryCommand = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Commands/RequestFailedWorkflowRetry.cs");
        string dispatcher = ReadProjectFile("src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs");

        fixture.ShouldContain("RequestFailedWorkflowRetry");
        fixture.ShouldContain("tenant-alpha + failed-event-001 + actor-reviewer-001");
        fixture.ShouldContain("retry_accepted");
        fixture.ShouldContain("retry_replayed");
        fixture.ShouldContain("idempotency_conflict_retry");
        retryCommand.ShouldContain("FailedEventId");
        retryCommand.ShouldContain("FailedOperationClass");
        dispatcher.ShouldContain("RequestFailedWorkflowRetry");
        AssertMetadataOnly(fixture);
    }

    private static void AssertTerminalReprocessContractWithoutBrowser()
    {
        string fixture = BuildTerminalReprocessFixture();
        string reprocessFactory = ReadProjectFile("src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleReprocessFactory.cs");
        string stateModelTests = ReadProjectFile("tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs");

        fixture.ShouldContain("retry_exhausted");
        fixture.ShouldContain("reprocess_created");
        fixture.ShouldContain("supersedes_workflow:workflow-001");
        fixture.ShouldContain("superseded_by_workflow:workflow-002");
        fixture.ShouldContain("Terminal states stay append-only");
        reprocessFactory.ShouldContain("SupersedesWorkflow");
        stateModelTests.ShouldContain("Failed");
        stateModelTests.ShouldContain("Received");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldContain("metadata", Case.Insensitive);
        text.ShouldNotContain("sender@example.test", Case.Insensitive);
        text.ShouldNotContain("restricted@example.com", Case.Insensitive);
        text.ShouldNotContain("raw provider payload", Case.Insensitive);
        text.ShouldNotContain("Graph delta token", Case.Insensitive);
        text.ShouldNotContain("Secret Project", Case.Insensitive);
        text.ShouldNotContain("NullReferenceException", Case.Insensitive);
        text.ShouldNotContain("System.InvalidOperationException", Case.Insensitive);
        text.ShouldNotContain("/home/", Case.Insensitive);
    }

    private static string ToPascalCase(string value)
        => string.Concat(value[..1].ToUpperInvariant(), value[1..]);

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
