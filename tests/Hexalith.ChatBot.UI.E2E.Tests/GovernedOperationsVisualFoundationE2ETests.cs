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
            await WaitForVisibleAsync(harness.Page.GetByLabel("Audit history: metadata only"));

            string commandType = await harness.Page.EvaluateAsync<string>("() => window.__lastCommand.commandType");
            string origin = await harness.Page.EvaluateAsync<string>("() => window.__lastCommand.origin");
            commandType.ShouldBe("RecordGovernedNote");
            origin.ShouldBe("ui");

            await WaitForVisibleAsync(harness.Page.GetByText("Warning", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Success", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Project status: UI origin remains visible").GetByText("Info", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("post-commit", new() { Exact = false }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Audit history (metadata-only)" }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldContain("AcceptedProjectionPending");
            bodyText.ShouldNotContain("Done", Case.Insensitive);
            bodyText.ShouldNotContain("Completed", Case.Insensitive);
            bodyText.ShouldNotContain("tenant-alpha", Case.Insensitive);
            bodyText.ShouldNotContain("restricted-file.txt", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("raw exception", Case.Insensitive);
            bodyText.ShouldNotContain("/home/", Case.Insensitive);
        }
    }

    [Fact]
    public async Task GovernedOperationsShouldExposeMatrixLiveBehaviorWithoutDuplicateAnnouncements()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertMatrixLiveBehaviorWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

            ILocator projection = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Projection status: pending" });
            await WaitForVisibleAsync(projection);
            (await projection.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await projection.GetAttributeAsync("data-chatbot-announcement-key")).ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
            (await projection.GetAttributeAsync("data-chatbot-repeat-rule")).ShouldBe("OncePerStableOperationKey");
            (await projection.GetAttributeAsync("data-chatbot-live-announced")).ShouldBe("true");

            ILocator audit = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit status: committed" });
            await WaitForVisibleAsync(audit);
            (await audit.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await audit.GetAttributeAsync("data-chatbot-announcement-key")).ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX-audit");

            ILocator history = harness.Page.GetByLabel("Audit history: metadata only");
            await WaitForVisibleAsync(history);
            (await history.GetAttributeAsync("role")).ShouldBeNull();
            (await history.GetAttributeAsync("aria-live")).ShouldBe("off");
            (await history.GetAttributeAsync("data-chatbot-feedback-state")).ShouldBe("ObservedForOthersRejectionOrQueueUpdate");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();
            ILocator repeatedProjection = harness.Page.Locator("[data-chatbot-announcement-key='01ARZ3NDEKTSV4RRFFQ69G5FAX']").First;
            await WaitForVisibleAsync(repeatedProjection);
            (await repeatedProjection.GetAttributeAsync("data-chatbot-live-announced")).ShouldBe("false");
            (await repeatedProjection.GetAttributeAsync("data-chatbot-live")).ShouldBe("off");
            (await repeatedProjection.GetAttributeAsync("aria-live")).ShouldBe("off");
            (await repeatedProjection.GetAttributeAsync("role")).ShouldBeNull();
        }
    }

    [Fact]
    public async Task InitialHistoricalContentShouldNotExposeWorkflowLiveAnnouncements()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertInitialHistoricalContentWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Governed operations", Level = 1 }));

            (await harness.Page.Locator("[data-chatbot-feedback-state='CurrentUserCommandAcceptedProjectionPending']").CountAsync()).ShouldBe(0);
            (await harness.Page.Locator("[data-chatbot-feedback-state='ObservedForOthersRejectionOrQueueUpdate']").CountAsync()).ShouldBe(0);
            (await harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Projection status: pending" }).CountAsync()).ShouldBe(0);
            (await harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit status: committed" }).CountAsync()).ShouldBe(0);
            (await harness.Page.GetByLabel("Audit history: metadata only").CountAsync()).ShouldBe(0);
        }
    }

    [Fact]
    public async Task ReducedMotionShouldSuppressNonEssentialMotionAndKeepTextStatusCues()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertReducedMotionWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();
            await WaitForVisibleAsync(harness.Page.GetByText("Projection pending", new() { Exact = true }));

            ILocator motionFixture = harness.Page.Locator("[data-chatbot-motion-fixture='governed-motion']");
            string animationName = await motionFixture.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string backgroundImage = await motionFixture.EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage");
            string transform = await motionFixture.EvaluateAsync<string>("element => getComputedStyle(element).transform");
            bool transitionSuppressed = await motionFixture.EvaluateAsync<bool>(
                """
                element => getComputedStyle(element).transitionDuration
                    .split(",")
                    .every(value => {
                        const trimmed = value.trim();
                        const numeric = Number.parseFloat(trimmed);
                        if (Number.isNaN(numeric)) {
                            return false;
                        }

                        return trimmed.endsWith("ms")
                            ? numeric <= 0.01
                            : numeric <= 0.00001;
                    })
                """);
            animationName.ShouldBe("none");
            backgroundImage.ShouldBe("none");
            transform.ShouldBe("none");
            transitionSuppressed.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task BackendFailureShouldRenderRetryableDangerStatusAndLeaveRetryAvailable()
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

            ILocator status = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Submission status: failed" });
            await WaitForVisibleAsync(status);
            await WaitForVisibleAsync(harness.Page.GetByText("Danger", new() { Exact = true }));
            (await status.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await status.GetAttributeAsync("data-chatbot-feedback-state")).ShouldBe("RetryableFailure");
            (await status.TextContentAsync() ?? string.Empty).ShouldContain("Submission did not complete");
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).IsEnabledAsync()).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task GovernedOperationsShouldRenderRetryFailureDuplicateSafetyMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRetryFailureDuplicateSafetyWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.RetryFailureMetadata));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

            ILocator status = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Operation recovery status: retryable failure" });
            await WaitForVisibleAsync(status);
            (await status.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await status.GetAttributeAsync("data-chatbot-feedback-state")).ShouldBe("RetryableFailure");
            await WaitForVisibleAsync(harness.Page.GetByText("Retry count", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("2 of 5", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Operation class", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("message-intake", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Owner role", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("mailbox-operator", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Duplicate safety note", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("duplicate-provider-message-suppressed", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("retry-later", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Retry waits for the next policy window."));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("sender@example.test", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("raw exception", Case.Insensitive);
        }
    }

    [Fact]
    public async Task OperationalQueueManagementShouldExposeFamiliesFiltersPaginationAndSafeActions()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertOperationalQueueManagementWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildOperationalQueueManagementFixture());

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Operational queue management", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Group, new() { NameString = "Queue family" }));
            await WaitForVisibleAsync(harness.Page.GetByText("age>0 risk:any confidence:any project:any mailbox:any failure-state:any assigned:any next-action:any", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("priority desc, item-ref asc, source-version asc", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("page-size:100", new() { Exact = true }));

            ILocator queueSurface = harness.Page.Locator("[data-chatbot-operational-queue='true']");
            (await queueSurface.GetAttributeAsync("data-chatbot-loading-mode")).ShouldBe("Pagination");
            (await harness.Page.Locator("[data-chatbot-loading-mode='InfiniteScroll']").CountAsync()).ShouldBe(0);

            foreach (string family in OperationalQueueFamilyTokens)
            {
                ILocator tab = harness.Page.Locator($"[data-queue-tab='{family}']");
                await tab.ClickAsync();
                await WaitForVisibleAsync(harness.Page.Locator($"[data-chatbot-queue-family='{family}']"));
                (await tab.GetAttributeAsync("aria-pressed")).ShouldBe("true");
                ILocator queueRow = harness.Page.Locator("#queue-row-root");
                await WaitForVisibleAsync(queueRow.GetByRole(AriaRole.Button, new() { NameString = $"Claim item:{family}-001 {family}" }));
                await WaitForVisibleAsync(queueRow.GetByRole(AriaRole.Button, new() { NameString = $"More actions item:{family}-001 {family}" }));
                await WaitForVisibleAsync(harness.Page.GetByLabel($"Why unavailable? Detail for {family} requires project authority or escalation."));
                await WaitForVisibleAsync(harness.Page.GetByText("Retry count", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Source version", new() { Exact = true }));
            }

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldContain("metadata_only");
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("bearer", Case.Insensitive);
        }
    }

    [Fact]
    public async Task OperationalQueueManagementShouldReflowAndKeepDisabledReasonsReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertOperationalQueueManagementResponsiveWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((int width, int height) in new[] { (1280, 900), (800, 900), (390, 844) })
            {
                await harness.Page.SetViewportSizeAsync(width, height);
                await harness.Page.SetContentAsync(BuildOperationalQueueManagementFixture());
                await harness.Page.EvaluateAsync("() => renderQueueRow('failed-ingestion')");

                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Operational queue management", Level = 1 }));
                ILocator queueRow = harness.Page.Locator("[data-chatbot-queue-family='failed-ingestion']");
                await WaitForVisibleAsync(queueRow);
                string queueText = await queueRow.InnerTextAsync();
                foreach (string expected in new[]
                {
                    "failed-ingestion",
                    "item:failed-ingestion-001",
                    "correlation:queue-failed-ingestion",
                    "tenant:tenant-alpha",
                    "mailbox:operations",
                    "workflow:failed-ingestion-001",
                    "metadata_only",
                })
                {
                    queueText.ShouldContain(expected);
                }

                ILocator disabledDetail = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Open detail item:failed-ingestion-001 failed-ingestion" });
                (await disabledDetail.GetAttributeAsync("aria-disabled")).ShouldBe("true");
                await disabledDetail.FocusAsync();
                await harness.Page.Keyboard.PressAsync("Enter");
                (await harness.Page.EvaluateAsync<int>("() => window.__detailOpenCount")).ShouldBe(0);

                ILocator reason = harness.Page.GetByLabel("Why unavailable? Detail for failed-ingestion requires project authority or escalation.");
                await WaitForVisibleAsync(reason);
                await reason.FocusAsync();
                (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("detail-reason-failed-ingestion");

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => {
                        const fixture = document.querySelector("[data-chatbot-responsive-fixture='operational-queue-management']");
                        return document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                            || document.body.scrollWidth > document.body.clientWidth + 1
                            || (fixture && fixture.scrollWidth > fixture.clientWidth + 1);
                    }
                    """);
                hasHorizontalOverflow.ShouldBeFalse($"Operational queue management should not overflow at {width}px.");
            }
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
                await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPendingRendered));

                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Governed operations", Level = 1 }));
                await harness.Page.WaitForFunctionAsync(
                    "() => document.querySelector('#fixture-status-root')?.textContent?.includes('Projection is not complete')");
                string statusText = await harness.Page.Locator("#fixture-status-root").InnerTextAsync();
                foreach (string expected in new[] { "Operation", "Command", "Lifecycle state", "Completion status", "Audit status", "Safe next actions" })
                {
                    statusText.ShouldContain(expected);
                }

                statusText.ShouldContain("metadata-only");

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
    public async Task GovernedOperationsShouldRenderEnglishAndFrenchTextWithoutChangingMachineMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertLocalizationFoundationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((string culture, string heading, string action, string operationLabel) in new[]
            {
                ("en", "Governed operations", "Record governed note", "Operation"),
                ("fr", "Opérations gouvernées", "Enregistrer la note gouvernée", "Opération"),
            })
            {
                await harness.Page.SetViewportSizeAsync(390, 844);
                await harness.Page.SetContentAsync(BuildLocalizedGovernedOperationsFixture(culture));

                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = heading, Level = 1 }));
                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = action }));
                await WaitForVisibleAsync(harness.Page.GetByText(operationLabel, new() { Exact = true }));
                string projectionStatusLabel = culture == "fr" ? "Statut de projection : en attente" : "Projection status: pending";
                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = projectionStatusLabel }).GetByText("AcceptedProjectionPending", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("01ARZ3NDEKTSV4RRFFQ69G5FAX", new() { Exact = true }));

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                        || document.body.scrollWidth > document.body.clientWidth + 1
                    """);
                hasHorizontalOverflow.ShouldBeFalse($"Localized governed operations should not overflow for {culture}.");
            }
        }
    }

    [Fact]
    public async Task GovernedOperationsShouldExposeRedactionRecoveryAndCognitiveLoadFixture()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRedactionRecoveryCognitiveLoadWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((string culture, string notice, string recovery) in new[]
            {
                ("en", "This export is redacted; full detail requires escalation.", "Retry only while duplicate-safety copy remains visible."),
                ("fr", "Cette exportation est masquée ; le détail complet nécessite une escalade.", "Réessayez uniquement quand la copie de sûreté anti-doublon reste visible."),
            })
            {
                await harness.Page.SetViewportSizeAsync(390, 844);
                await harness.Page.SetContentAsync(BuildRedactionRecoveryCognitiveLoadFixture(culture));

                await WaitForVisibleAsync(harness.Page.GetByLabel(notice));
                await WaitForVisibleAsync(harness.Page.GetByText(recovery, new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("01ARZ3NDEKTSV4RRFFQ69G5FAX", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("audit:Committed", new() { Exact = false }));

                int primaryActionCount = await harness.Page.Locator("[data-chatbot-action-kind='primary']").CountAsync();
                primaryActionCount.ShouldBe(1);

                await WaitForVisibleAsync(harness.Page.GetByText(culture == "fr" ? "Filtre : Revue en attente. 2 résultats." : "Filter: Pending review. 2 results.", new() { Exact = true }));

                bool unsafeTextVisible = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => document.body.innerText.includes("restricted-file.txt")
                        || document.body.innerText.includes("Secret Project")
                        || document.body.innerText.includes("raw exception")
                    """);
                unsafeTextVisible.ShouldBeFalse();

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                        || document.body.scrollWidth > document.body.clientWidth + 1
                    """);
                hasHorizontalOverflow.ShouldBeFalse($"Redaction/recovery fixture should not overflow for {culture}.");
            }
        }
    }

    [Fact]
    public async Task AssociationReviewShouldSelectCandidateCompareEvidenceAndKeepDisabledReasonsReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationReviewSelectionWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationReviewFixture(AssociationReviewFixtureScenario.Candidates));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Association review", Level = 1 }));
            ILocator candidate = harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" });
            await WaitForVisibleAsync(candidate);
            (await candidate.GetAttributeAsync("aria-checked")).ShouldBe("false");

            await candidate.ClickAsync();

            (await candidate.GetAttributeAsync("aria-checked")).ShouldBe("true");
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Authorized candidate A" }));
            ILocator comparison = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Authorized candidate A" });
            await WaitForVisibleAsync(comparison.GetByText("thread-reference AUTH-100", new() { Exact = true }));
            await WaitForVisibleAsync(comparison.GetByText("Evidence redacted", new() { Exact = true }));

            ILocator choose = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Choose candidate" });
            (await choose.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await choose.GetAttributeAsync("aria-describedby")).ShouldBe("association-action-choose-candidate-disabled-reason");
            await choose.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<int>("() => window.__decisionPreviewCount")).ShouldBe(0);

            ILocator reason = harness.Page.Locator("#association-action-choose-candidate-disabled-reason");
            await WaitForVisibleAsync(reason);
            await reason.FocusAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("association-action-choose-candidate-disabled-reason");
        }
    }

    [Fact]
    public async Task AssociationReviewShouldSubmitDecisionThroughUiCommandSpineAndRefreshStatus()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationDecisionSubmitWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationDecisionSubmitFixture(conflict: false));

            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" }).ClickAsync();
            await harness.Page.GetByLabel("Decision note").FillAsync("  Reviewed safe metadata.  ");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Choose candidate" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Association decision accepted: projection pending" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Audit status: reconciling" }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Why unavailable? Decision already recorded."));

            string commandType = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.commandType");
            string origin = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.origin");
            string decisionKind = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.decisionKind");
            string note = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.decisionNote");
            string evidence = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.candidateEvidenceFingerprint");
            int refreshCount = await harness.Page.EvaluateAsync<int>("() => window.__routingRefreshCount");

            commandType.ShouldBe("AssociateEmailToProject");
            origin.ShouldBe("ui");
            decisionKind.ShouldBe("associate");
            note.ShouldBe("Reviewed safe metadata.");
            evidence.ShouldBe("hash-project");
            refreshCount.ShouldBe(1);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldShowSafeIdempotencyConflictWithoutLeakingDecisionPayload()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationDecisionConflictWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationDecisionSubmitFixture(conflict: true));

            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" }).ClickAsync();
            await harness.Page.GetByLabel("Decision note").FillAsync("raw provider payload should not appear");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Choose candidate" }).ClickAsync();

            ILocator alert = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Submission failed: idempotency_conflict_association_decision" });
            await WaitForVisibleAsync(alert);
            (await alert.TextContentAsync() ?? string.Empty).ShouldContain("already decided");
            (await harness.Page.EvaluateAsync<int>("() => window.__routingRefreshCount")).ShouldBe(0);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldSubmitCorrectionThroughUiCommandSpineAndShowPartialStatus()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationCorrectionSubmitWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationCorrectionSubmitFixture(conflict: false, blocked: false));

            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" }).ClickAsync();
            await harness.Page.GetByLabel("Correction rationale").FillAsync("  Wrong project selected from safe metadata.  ");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit correction" }).ClickAsync();

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Association correction accepted: downstream preview only" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction status: partial" }));
            await WaitForVisibleAsync(harness.Page.GetByText("Corrected target project-beta", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("preview-only", new() { Exact = true }));

            string commandType = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.commandType");
            string origin = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.origin");
            string correctionKind = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.correctionKind");
            string targetProjectId = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.targetProjectId");
            string rationale = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.correctionRationale");
            string predecessor = await harness.Page.EvaluateAsync<string>("() => window.__submittedCommand.command.predecessorAssociationId");
            int refreshCount = await harness.Page.EvaluateAsync<int>("() => window.__routingRefreshCount");

            commandType.ShouldBe("CorrectEmailProjectAssociation");
            origin.ShouldBe("ui");
            correctionKind.ShouldBe("project-reassignment");
            targetProjectId.ShouldBe("project-beta");
            rationale.ShouldBe("Wrong project selected from safe metadata.");
            predecessor.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
            refreshCount.ShouldBe(1);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldKeepBlockedCorrectionReasonFocusableWithoutSubmitting()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationCorrectionBlockedWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationCorrectionSubmitFixture(conflict: false, blocked: true));

            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" }).ClickAsync();
            ILocator submit = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit correction" });
            await WaitForVisibleAsync(submit);
            (await submit.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await submit.GetAttributeAsync("aria-describedby")).ShouldBe("association-correction-submit-disabled-reason");

            await submit.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<int>("() => window.__correctionSubmitCount")).ShouldBe(0);
            (await harness.Page.EvaluateAsync<bool>("() => window.__submittedCommand === null")).ShouldBeTrue();

            ILocator reason = harness.Page.GetByLabel("Why unavailable? Projection invalidation is unavailable, so correction is blocked.");
            await WaitForVisibleAsync(reason);
            await reason.FocusAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("association-correction-submit-disabled-reason");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw exception", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldShowSafeCorrectionConflictWithoutLeakingPayload()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationCorrectionConflictWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationCorrectionSubmitFixture(conflict: true, blocked: false));

            await harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" }).ClickAsync();
            await harness.Page.GetByLabel("Correction rationale").FillAsync("raw provider payload should not appear");
            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit correction" }).ClickAsync();

            ILocator alert = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Correction failed: idempotency_conflict_correction" });
            await WaitForVisibleAsync(alert);
            (await alert.TextContentAsync() ?? string.Empty).ShouldContain("already been corrected");
            (await harness.Page.EvaluateAsync<int>("() => window.__routingRefreshCount")).ShouldBe(0);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldSurfaceCorrectionPropagationProgressAndBlockCorrectedContextUse()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationCorrectionPropagationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario.Pending));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Association review", Level = 1 }));
            ILocator status = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status: Correcting" });
            await WaitForVisibleAsync(status);
            (await status.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await status.GetAttributeAsync("data-chatbot-feedback-state")).ShouldBe("DependencyDegraded");

            await WaitForVisibleAsync(harness.Page.GetByText("2 of 4 stores acknowledged", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("2026-05-31T09:40:00Z", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Project owner", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Wait for propagation before using corrected project context.", new() { Exact = true }));

            ILocator submit = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit correction" });
            (await submit.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            await submit.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<int>("() => window.__correctionSubmitCount")).ShouldBe(0);

            ILocator aiAction = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Prepare AI action" });
            (await aiAction.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            ILocator reason = harness.Page.Locator("#association-ai-action-disabled-reason");
            await WaitForVisibleAsync(reason);
            await reason.FocusAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("association-ai-action-disabled-reason");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("raw exception", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldSurfaceCorrectionDelayedEscalationWithoutStartingNewWorkflow()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationCorrectionPropagationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario.Delayed));

            ILocator status = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status: Correction-delayed" });
            await WaitForVisibleAsync(status);
            (await status.GetAttributeAsync("data-chatbot-status")).ShouldBe("warning");
            await WaitForVisibleAsync(harness.Page.GetByText("Correction propagation is delayed and operations has been alerted.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Operations", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("Escalate to operations while corrected context remains blocked.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("workflow-correction-001", new() { Exact = true }));

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Refresh status" }).ClickAsync();
            (await harness.Page.EvaluateAsync<int>("() => window.__routingRefreshCount")).ShouldBe(1);
            (await harness.Page.EvaluateAsync<int>("() => window.__workflowStartCount")).ShouldBe(0);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw provider payload", Case.Insensitive);
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("raw exception", Case.Insensitive);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldShowCompletePropagationAndAllowPreparedContextActions()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationCorrectionPropagationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario.Complete));

            ILocator status = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status: complete" });
            await WaitForVisibleAsync(status);
            (await status.GetAttributeAsync("data-chatbot-status")).ShouldBe("success");
            await WaitForVisibleAsync(harness.Page.GetByText("4 of 4 stores acknowledged", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Correction propagation status: complete" }));

            ILocator aiAction = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Prepare AI action" });
            (await aiAction.GetAttributeAsync("aria-disabled")).ShouldBe("false");
            await aiAction.ClickAsync();
            (await harness.Page.EvaluateAsync<int>("() => window.__aiActionPrepareCount")).ShouldBe(1);
            (await harness.Page.GetByLabel("Why unavailable? Corrected context is not ready for AI actions or command preparation.").CountAsync()).ShouldBe(0);
        }
    }

    [Fact]
    public async Task AssociationReviewShouldReflowAcrossDesktopTabletAndPhoneWithoutUnsafeOverflow()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertAssociationReviewResponsiveWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((int width, int height) in new[] { (1280, 900), (800, 900), (390, 844) })
            {
                await harness.Page.SetViewportSizeAsync(width, height);
                await harness.Page.SetContentAsync(BuildAssociationReviewFixture(AssociationReviewFixtureScenario.Candidates));

                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Association review", Level = 1 }));
                await WaitForVisibleAsync(harness.Page.GetByText("Candidate projects", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Evidence comparison", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Source metadata", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("safe-next-action, projection-pending", new() { Exact = true }));

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => {
                        const fixture = document.querySelector("[data-chatbot-responsive-fixture='association-review']");
                        const shellMain = document.querySelector(".chatbot-shell-main");
                        return document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                            || document.body.scrollWidth > document.body.clientWidth + 1
                            || (shellMain && shellMain.scrollWidth > shellMain.clientWidth + 1)
                            || (fixture && fixture.scrollWidth > fixture.clientWidth + 1);
                    }
                    """);
                hasHorizontalOverflow.ShouldBeFalse($"The association review fixture should not overflow at {width}px.");
            }
        }
    }

    [Fact]
    public async Task AssociationReviewShouldPreserveForcedColorsReducedMotionAndBlockedRedactionStates()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync(forcedColors: true);
        if (harness is null)
        {
            AssertAssociationReviewForcedColorsAndBlockedWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildAssociationReviewFixture(AssociationReviewFixtureScenario.Candidates));

            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches")).ShouldBeTrue();
            (await harness.Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches")).ShouldBeTrue();

            ILocator candidate = harness.Page.GetByRole(AriaRole.Radio, new() { NameString = "Candidate 1. Confidence 72%. Authorized candidate A" });
            await WaitForVisibleAsync(candidate);
            string borderStyle = await candidate.EvaluateAsync<string>("element => getComputedStyle(element).borderTopStyle");
            string animationName = await candidate.EvaluateAsync<string>("element => getComputedStyle(element).animationName");
            string transform = await candidate.EvaluateAsync<string>("element => getComputedStyle(element).transform");
            borderStyle.ShouldBe("solid");
            animationName.ShouldBe("none");
            transform.ShouldBe("none");

            await harness.Page.SetContentAsync(BuildAssociationReviewFixture(AssociationReviewFixtureScenario.BlockedRedacted));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Blocked: No authorized candidates are available. Next action: Review authorized metadata, then defer or escalate." }));
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence restricted", new() { Exact = true }));

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            bodyText.ShouldNotContain("Secret Project", Case.Insensitive);
            bodyText.ShouldNotContain("restricted@example.com", Case.Insensitive);
            bodyText.ShouldNotContain("raw exception", Case.Insensitive);
            bodyText.ShouldNotContain("full email thread", Case.Insensitive);
        }
    }

    [Fact]
    public async Task FrenchCriticalLabelsShouldWrapWithoutHidingSafetyStateOrActions()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertFrenchCriticalLabelsWithoutBrowser();
            return;
        }

        await using (harness)
        {
            foreach ((int width, int height) in new[] { (1280, 900), (800, 900), (390, 844) })
            {
                await harness.Page.SetViewportSizeAsync(width, height);
                await harness.Page.SetContentAsync(BuildFrenchCriticalLabelFixture());

                await WaitForVisibleAsync(harness.Page.GetByLabel("Acteur utilisateur humain : Jerome"));
                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Risque : Invoque un outil. Raison de stratégie : Approbation requise avant d'appeler un outil externe." }));
                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Statut de projection : en attente" }));
                await WaitForVisibleAsync(harness.Page.GetByText("Confiance : 88 %", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Prochaine action :", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Raison de récupération sûre :", new() { Exact = true }));

                bool hidesCriticalText = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => [...document.querySelectorAll("[data-chatbot-critical-localized='true']")]
                        .some(element => {
                            const style = getComputedStyle(element);
                            const rect = element.getBoundingClientRect();
                            return style.display === "none"
                                || style.visibility === "hidden"
                                || rect.width <= 0
                                || rect.height <= 0
                                || element.scrollWidth > element.clientWidth + 1;
                        })
                    """);
                hidesCriticalText.ShouldBeFalse($"French critical labels should wrap without hidden overflow at {width}px.");

                bool hasHorizontalOverflow = await harness.Page.EvaluateAsync<bool>(
                    """
                    () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                        || document.body.scrollWidth > document.body.clientWidth + 1
                    """);
                hasHorizontalOverflow.ShouldBeFalse($"French safety fixture should not overflow at {width}px.");
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
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Resolve MCP actor: Unresolved actor" }),
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

            foreach (string actorLabel in new[]
            {
                "Human user actor: Jerome",
                "External party actor: External participant",
                "Service client actor: Graph connector",
                "AI actor: Copilot planner",
                "Background worker actor: Intake worker",
                "CLI actor: chatbot-cli",
                "MCP actor: Unresolved actor",
                "Mailbox event actor: Shared mailbox event",
            })
            {
                await WaitForVisibleAsync(harness.Page.GetByLabel(actorLabel, new() { Exact = true }));
            }

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Resolve MCP actor: Unresolved actor" }));

            ILocator evidenceButton = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Available evidence: Audit correlation record" });
            await WaitForVisibleAsync(evidenceButton);
            await evidenceButton.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            await harness.Page.Keyboard.PressAsync("Space");
            int activationCount = await harness.Page.EvaluateAsync<int>("() => window.__evidenceOpenCount");
            activationCount.ShouldBe(2);

            ILocator redactedEvidence = harness.Page.GetByLabel("Evidence redacted: Supporting file. Evidence is redacted by policy.");
            await WaitForVisibleAsync(redactedEvidence);
            (await redactedEvidence.GetAttributeAsync("aria-describedby")).ShouldBe("evidence-redacted-reason");
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence is redacted by policy.", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Evidence unavailable: Evidence cache. Evidence store is unavailable."));
            await WaitForVisibleAsync(harness.Page.GetByLabel("Evidence restricted: Restricted metadata. Authorization required."));

            foreach (string riskLabel in new[]
            {
                "Risk: Externally visible. Policy reason: Customer-visible output requires review.",
                "Risk: File-exposing. Policy reason: File metadata could be exposed.",
                "Risk: Project-mutating. Policy reason: Project state would change.",
                "Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool.",
                "Risk: Task-creating. Policy reason: Creates follow-up work for another actor.",
                "Risk: Participant-representing. Policy reason: Acts on behalf of a participant.",
            })
            {
                await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = riskLabel }));
            }

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Info status: Command accepted; projection is pending." }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Warning status: Dependency degraded; retry remains available." }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Danger status: Validation failed for the current user." }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Success status: Audit metadata committed." }));
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
            (await disabledAction.GetAttributeAsync("disabled")).ShouldBeNull();
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
    public async Task GovernedOperationsShouldExposeKeyboardLandmarkAndFocusPath()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertKeyboardLandmarkFocusPathWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));

            ILocator skipLink = harness.Page.GetByRole(AriaRole.Link, new() { NameString = "Skip to content" });
            await WaitForVisibleAsync(skipLink);
            await skipLink.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            await harness.Page.WaitForFunctionAsync("() => document.activeElement?.id === 'chatbot-main-content'");

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Main));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Governed operations", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Region, new() { NameString = "Governed command path" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Governed operation review context" }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Project status: UI origin remains visible" }));

            bool visibleOrderPreserved = await harness.Page.EvaluateAsync<bool>(
                """
                () => {
                    const before = (first, second) => Boolean(first && second && (first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING));
                    const skip = document.querySelector(".chatbot-skip-link");
                    const main = document.querySelector("#chatbot-main-content");
                    const heading = document.querySelector("h1");
                    const shell = document.querySelector(".chatbot-conversation-shell[aria-label='Governed operations']");
                    const project = document.querySelector("[aria-label='Project context']");
                    const primary = document.querySelector("[role='region'][aria-label='Governed command path']");
                    const complementary = document.querySelector("[role='complementary'][aria-label='Governed operation review context']");
                    const status = document.querySelector("[role='status'][aria-label='Project status: UI origin remains visible']");

                    return before(skip, main)
                        && before(main, shell)
                        && before(shell, project)
                        && before(project, primary)
                        && before(primary, complementary)
                        && Boolean(primary?.contains(heading))
                        && Boolean(project?.contains(status));
                }
                """);
            visibleOrderPreserved.ShouldBeTrue();

            await harness.Page.Keyboard.PressAsync("Tab");
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement?.textContent?.trim() ?? ''"))
                .ShouldBe("Record governed note");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).FocusAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement?.textContent?.trim() ?? ''"))
                .ShouldBe("Record governed note");

            string[] duplicateLandmarks = await harness.Page.EvaluateAsync<string[]>(
                """
                () => {
                    const nodes = [...document.querySelectorAll("main, [role='region'], aside[aria-label], header[aria-label], [role='status'], [role='alert']")];
                    const keys = nodes.map(node => {
                        const role = node.getAttribute("role") || node.tagName.toLowerCase();
                        const name = node.getAttribute("aria-label") || node.getAttribute("aria-labelledby") || "";
                        return `${role}:${name}`;
                    }).filter(key => key.endsWith(":") === false);
                    return keys.filter((key, index) => keys.indexOf(key) !== index);
                }
                """);
            duplicateLandmarks.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task BusyRegionShouldSettleOnSameRegionAndPreserveKeyboardFocus()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertBusyRegionFocusPreservationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildBusyRegionFixture());

            ILocator refresh = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Refresh operation status" });
            ILocator busyRegion = harness.Page.GetByRole(AriaRole.Region, new() { NameString = "Operation status summary" });

            await refresh.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            await harness.Page.WaitForFunctionAsync(
                "() => document.querySelector('#operation-status-region')?.getAttribute('aria-busy') === 'false' && window.__refreshCount === 1");

            (await busyRegion.GetAttributeAsync("aria-busy")).ShouldBe("false");
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''")).ShouldBe("refresh-operation-status");
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Status refresh: complete" }));
        }
    }

    [Fact]
    public async Task ValidationFailureShouldFocusSummaryAndAssociateInvalidFields()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertValidationAssociationWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildValidationAssociationFixture());

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit approval review" }).ClickAsync();

            ILocator summary = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Approval validation summary" });
            await WaitForVisibleAsync(summary);
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''")).ShouldBe("approval-errors");

            ILocator rationale = harness.Page.GetByLabel("Approval rationale");
            (await rationale.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await rationale.GetAttributeAsync("aria-describedby")).ShouldBe("approval-rationale-message");

            ILocator decision = harness.Page.GetByLabel("Approval decision");
            (await decision.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await decision.GetAttributeAsync("aria-errormessage")).ShouldBe("approval-decision-message");
            await WaitForVisibleAsync(harness.Page.Locator("#approval-decision-message"));
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

            ILocator announcement = harness.Page.Locator("#streaming-stop-active-announcement");
            await harness.Page.WaitForFunctionAsync(
                "() => document.querySelector('#streaming-stop-active-announcement')?.textContent === 'Response stopped'");
            (await announcement.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await announcement.TextContentAsync()).ShouldBe("Response stopped");
            (await harness.Page.Locator("[role='status']").Filter(new() { HasTextString = "Response stopped" }).CountAsync()).ShouldBe(1);
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("composer-target");

            ILocator idleStopRegion = harness.Page.Locator("[data-chatbot-stable-id='streaming-stop-idle']");
            (await idleStopRegion.GetAttributeAsync("data-chatbot-streaming")).ShouldBe("false");
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Stop idle response generation" }).CountAsync()).ShouldBe(0);
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("composer-target");
        }
    }

    private static readonly string[] OperationalQueueFamilyTokens =
    [
        "ambiguous-association",
        "unresolved-participant",
        "pending-approval",
        "failed-ingestion",
        "failed-attachment",
        "retryable-operation",
    ];

    private static async Task<string> CssVariableAsync(IPage page, string name)
        => await page.EvaluateAsync<string>(
                """
                token => {
                    for (const sheet of document.styleSheets) {
                        try {
                            for (const rule of sheet.cssRules) {
                                if (rule.selectorText === ":root") {
                                    const value = rule.style.getPropertyValue(token).trim();
                                    if (value) {
                                        return value;
                                    }
                                }
                            }
                        } catch {
                            // Ignore unresolved external stylesheets in SetContent fixtures.
                        }
                    }

                    return getComputedStyle(document.documentElement).getPropertyValue(token).trim();
                }
                """,
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
        string initialStatusRoot = scenario is FixtureScenario.ProjectionPendingRendered
            ? BuildGovernedOperationOutcomeMarkup()
            : string.Empty;

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
                    <div class="chatbot-shimmer chatbot-skeleton chatbot-row-motion chatbot-streaming-text chatbot-panel-transition"
                         data-chatbot-motion-fixture="governed-motion">Projection pending</div>
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
                               data-chatbot-live="polite"
                               data-chatbot-announcement-key="governed-operations-context"
                               data-chatbot-repeat-rule="OncePerStableOperationKey"
                               role="status"
                               aria-live="polite"
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
                            <div id="fixture-status-root">{{initialStatusRoot}}</div>
                          </section>
                        </section>
                        <aside class="chatbot-conversation-shell__panel"
                               role="complementary"
                               aria-label="Governed operation review context">
                          <section class="chatbot-section" aria-labelledby="governed-review-context-title">
                            <h2 id="governed-review-context-title" class="chatbot-section-title">Review context</h2>
                            <p class="chatbot-body">Current operation context remains available beside the command path.</p>
                          </section>
                        </aside>
                      </div>
                    </section>
                  </main>
                </div>
                <script>
                  const scenario = "{{scenarioName}}";
                  const root = document.querySelector("#fixture-status-root");
                  window.__announcedKeys = window.__announcedKeys || new Set();
                  document.querySelector("button").addEventListener("click", () => {
                    window.__lastCommand = { commandType: "RecordGovernedNote", origin: "ui" };
                    if (scenario === "SubmitFails") {
                      root.innerHTML = `
                        <div class="chatbot-status"
                             data-chatbot-status="danger"
                             data-chatbot-feedback-state="RetryableFailure"
                             data-chatbot-live="polite"
                             data-chatbot-announcement-key="governed-note-failed-dependency_degraded"
                             data-chatbot-repeat-rule="OncePerFailureKey"
                             role="status"
                             aria-live="polite"
                             aria-label="Submission status: failed">
                          <span class="chatbot-status__label">Danger</span>
                          <span>Submission did not complete (code: <code class="chatbot-code">dependency_degraded</code>). You can try again.</span>
                        </div>`;
                      return;
                    }
                    if (scenario === "RetryFailureMetadata") {
                      root.innerHTML = `
                        <section class="chatbot-section" aria-labelledby="operation-recovery-title">
                          <h2 id="operation-recovery-title" class="chatbot-section-title">Recovery</h2>
                          <div class="chatbot-status"
                               data-chatbot-status="warning"
                               data-chatbot-feedback-state="RetryableFailure"
                               data-chatbot-live="polite"
                               data-chatbot-announcement-key="01ARZ3NDEKTSV4RRFFQ69G5FAX-retry"
                               data-chatbot-repeat-rule="OncePerStableOperationKey"
                               role="status"
                               aria-live="polite"
                               aria-label="Operation recovery status: retryable failure">
                            <span class="chatbot-status__label">Warning</span>
                            <span>Recoverable mailbox failure. Next retry is policy-controlled.</span>
                          </div>
                          <dl class="chatbot-definition-list">
                            <dt class="chatbot-labelled-row">Operation</dt>
                            <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAX</code></dd>
                            <dt class="chatbot-labelled-row">Operation class</dt>
                            <dd><code class="chatbot-code">message-intake</code></dd>
                            <dt class="chatbot-labelled-row">Lifecycle state</dt>
                            <dd><code class="chatbot-code">Failed</code></dd>
                            <dt class="chatbot-labelled-row">Failure reason</dt>
                            <dd><code class="chatbot-code">graph_throttled</code></dd>
                            <dt class="chatbot-labelled-row">Retry count</dt>
                            <dd><code class="chatbot-code">2 of 5</code></dd>
                            <dt class="chatbot-labelled-row">Next retry</dt>
                            <dd><code class="chatbot-code">2026-05-31T09:05:00Z</code></dd>
                            <dt class="chatbot-labelled-row">Safe next actions</dt>
                            <dd><code class="chatbot-code">retry-later</code></dd>
                            <dt class="chatbot-labelled-row">Owner role</dt>
                            <dd><code class="chatbot-code">mailbox-operator</code></dd>
                            <dt class="chatbot-labelled-row">Duplicate safety note</dt>
                            <dd><code class="chatbot-code">duplicate-provider-message-suppressed</code></dd>
                          </dl>
                          <button type="button"
                                  class="chatbot-touch-target-secondary"
                                  aria-disabled="true"
                                  aria-describedby="retry-policy-window-reason">Retry now</button>
                          <span id="retry-policy-window-reason"
                                tabindex="0"
                                aria-label="Why unavailable? Retry waits for the next policy window.">
                            Retry waits for the next policy window.
                          </span>
                        </section>`;
                      return;
                    }

                    const projectionKey = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
                    const auditKey = "01ARZ3NDEKTSV4RRFFQ69G5FAX-audit";
                    const projectionAnnounced = !window.__announcedKeys.has(projectionKey);
                    const auditAnnounced = !window.__announcedKeys.has(auditKey);
                    window.__announcedKeys.add(projectionKey);
                    window.__announcedKeys.add(auditKey);
                    const projectionLive = projectionAnnounced ? "polite" : "off";
                    const auditLive = auditAnnounced ? "polite" : "off";
                    const projectionRole = projectionAnnounced ? 'role="status"' : "";
                    const auditRole = auditAnnounced ? 'role="status"' : "";

                    root.innerHTML = `
                      <section class="chatbot-section" aria-labelledby="operation-outcome-title">
                        <h2 id="operation-outcome-title" class="chatbot-section-title">Outcome</h2>
                        <div class="chatbot-status-group" aria-label="Operation status summary">
                          <div class="chatbot-status"
                               data-chatbot-status="warning"
                               data-chatbot-feedback-state="CurrentUserCommandAcceptedProjectionPending"
                               data-chatbot-live="${projectionLive}"
                               data-chatbot-announcement-key="${projectionKey}"
                               data-chatbot-repeat-rule="OncePerStableOperationKey"
                               data-chatbot-live-announced="${projectionAnnounced ? "true" : "false"}"
                               ${projectionRole}
                               aria-live="${projectionLive}"
                               aria-label="Projection status: pending">
                            <span class="chatbot-status__label">Warning</span>
                            <span>Projection is not complete (<code class="chatbot-code">AcceptedProjectionPending</code>).</span>
                          </div>
                          <div class="chatbot-status"
                               data-chatbot-status="success"
                               data-chatbot-feedback-state="CurrentUserCommandAcceptedProjectionPending"
                               data-chatbot-live="${auditLive}"
                               data-chatbot-announcement-key="${auditKey}"
                               data-chatbot-repeat-rule="OncePerStableOperationKey"
                               data-chatbot-live-announced="${auditAnnounced ? "true" : "false"}"
                               ${auditRole}
                               aria-live="${auditLive}"
                               aria-label="Audit status: committed">
                            <span class="chatbot-status__label">Success</span>
                            <span>Audit metadata is committed (<code class="chatbot-code">Committed</code>).</span>
                          </div>
                          <div class="chatbot-status"
                               data-chatbot-status="info"
                               data-chatbot-feedback-state="ObservedForOthersRejectionOrQueueUpdate"
                               data-chatbot-live="off"
                               data-chatbot-announcement-key="audit-history-metadata-only"
                               data-chatbot-repeat-rule="NoLiveAnnouncement"
                               data-chatbot-live-announced="false"
                               aria-live="off"
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

    private static string BuildGovernedOperationOutcomeMarkup()
        => """
            <section class="chatbot-section" aria-labelledby="operation-outcome-title">
              <h2 id="operation-outcome-title" class="chatbot-section-title">Outcome</h2>
              <div class="chatbot-status-group" aria-label="Operation status summary">
                <div class="chatbot-status"
                     data-chatbot-status="warning"
                     data-chatbot-feedback-state="CurrentUserCommandAcceptedProjectionPending"
                     data-chatbot-live="polite"
                     data-chatbot-announcement-key="01ARZ3NDEKTSV4RRFFQ69G5FAX"
                     data-chatbot-repeat-rule="OncePerStableOperationKey"
                     role="status"
                     aria-live="polite"
                     aria-label="Projection status: pending">
                  <span class="chatbot-status__label">Warning</span>
                  <span>Projection is not complete (<code class="chatbot-code">AcceptedProjectionPending</code>).</span>
                </div>
                <div class="chatbot-status"
                     data-chatbot-status="success"
                     data-chatbot-feedback-state="CurrentUserCommandAcceptedProjectionPending"
                     data-chatbot-live="polite"
                     data-chatbot-announcement-key="01ARZ3NDEKTSV4RRFFQ69G5FAX-audit"
                     data-chatbot-repeat-rule="OncePerStableOperationKey"
                     role="status"
                     aria-live="polite"
                     aria-label="Audit status: committed">
                  <span class="chatbot-status__label">Success</span>
                  <span>Audit metadata is committed (<code class="chatbot-code">Committed</code>).</span>
                </div>
                <div class="chatbot-status"
                     data-chatbot-status="info"
                     data-chatbot-feedback-state="ObservedForOthersRejectionOrQueueUpdate"
                     data-chatbot-live="off"
                     data-chatbot-announcement-key="audit-history-metadata-only"
                     data-chatbot-repeat-rule="NoLiveAnnouncement"
                     aria-live="off"
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
              <code class="chatbot-code">post-commit - allow/proposed - audit:Committed - origin:Ui - correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW</code>
            </section>
            """;

    private static string BuildOperationalQueueManagementFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Operational queue management fixture</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-page"
                      aria-labelledby="operational-queue-management-title"
                      data-chatbot-responsive-fixture="operational-queue-management">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Governed operations</span>
                    <h1 id="operational-queue-management-title" class="chatbot-page-title">Operational queue management</h1>
                  </header>
                  <section class="chatbot-section"
                           aria-labelledby="operational-queue-title"
                           data-chatbot-operational-queue="true"
                           data-chatbot-loading-mode="Pagination">
                    <h2 id="operational-queue-title" class="chatbot-section-title">Tenant queue rows</h2>
                    <div class="chatbot-command-bar" role="group" aria-label="Queue family">
                      <button type="button" class="chatbot-touch-target-dense-secondary" data-queue-tab="ambiguous-association" aria-pressed="true">ambiguous-association</button>
                      <button type="button" class="chatbot-touch-target-dense-secondary" data-queue-tab="unresolved-participant" aria-pressed="false">unresolved-participant</button>
                      <button type="button" class="chatbot-touch-target-dense-secondary" data-queue-tab="pending-approval" aria-pressed="false">pending-approval</button>
                      <button type="button" class="chatbot-touch-target-dense-secondary" data-queue-tab="failed-ingestion" aria-pressed="false">failed-ingestion</button>
                      <button type="button" class="chatbot-touch-target-dense-secondary" data-queue-tab="failed-attachment" aria-pressed="false">failed-attachment</button>
                      <button type="button" class="chatbot-touch-target-dense-secondary" data-queue-tab="retryable-operation" aria-pressed="false">retryable-operation</button>
                    </div>
                    <dl class="chatbot-definition-list chatbot-labelled-row-list" aria-label="Queue filters">
                      <dt class="chatbot-labelled-row">Filters</dt>
                      <dd><code class="chatbot-code">age&gt;0 risk:any confidence:any project:any mailbox:any failure-state:any assigned:any next-action:any</code></dd>
                      <dt class="chatbot-labelled-row">Sort</dt>
                      <dd><code class="chatbot-code">priority desc, item-ref asc, source-version asc</code></dd>
                      <dt class="chatbot-labelled-row">Result count</dt>
                      <dd><code class="chatbot-code" id="queue-result-count">1</code></dd>
                      <dt class="chatbot-labelled-row">Pagination</dt>
                      <dd><code class="chatbot-code">page-size:100</code></dd>
                    </dl>
                    <div id="queue-row-root" class="chatbot-table" role="table" aria-label="Tenant queue rows"></div>
                  </section>
                </main>
                <script>
                  window.__detailOpenCount = 0;
                  const rows = [
                    { family: "ambiguous-association", item: "item:ambiguous-association-001", state: "waiting", risk: "high", confidence: "0.62", retry: "1", action: "claim", source: "12", correlation: "correlation:queue-ambiguous-association", workflow: "workflow:ambiguous-association-001", reasonId: "detail-reason-ambiguous-association" },
                    { family: "unresolved-participant", item: "item:unresolved-participant-001", state: "blocked", risk: "medium", confidence: "0.44", retry: "0", action: "assign", source: "9", correlation: "correlation:queue-unresolved-participant", workflow: "workflow:unresolved-participant-001", reasonId: "detail-reason-unresolved-participant" },
                    { family: "pending-approval", item: "item:pending-approval-001", state: "escalation-needed", risk: "critical", confidence: "0.91", retry: "0", action: "prioritize", source: "21", correlation: "correlation:queue-pending-approval", workflow: "workflow:pending-approval-001", reasonId: "detail-reason-pending-approval" },
                    { family: "failed-ingestion", item: "item:failed-ingestion-001", state: "failed", risk: "high", confidence: "0.70", retry: "3", action: "retry", source: "33", correlation: "correlation:queue-failed-ingestion", workflow: "workflow:failed-ingestion-001", reasonId: "detail-reason-failed-ingestion" },
                    { family: "failed-attachment", item: "item:failed-attachment-001", state: "retryable", risk: "medium", confidence: "0.68", retry: "2", action: "retry", source: "18", correlation: "correlation:queue-failed-attachment", workflow: "workflow:failed-attachment-001", reasonId: "detail-reason-failed-attachment" },
                    { family: "retryable-operation", item: "item:retryable-operation-001", state: "retryable", risk: "low", confidence: "0.80", retry: "1", action: "requeue", source: "7", correlation: "correlation:queue-retryable-operation", workflow: "workflow:retryable-operation-001", reasonId: "detail-reason-retryable-operation" },
                  ];

                  function renderQueueRow(family) {
                    const row = rows.find(item => item.family === family);
                    document.querySelectorAll("[data-queue-tab]").forEach(tab => {
                      tab.setAttribute("aria-pressed", String(tab.dataset.queueTab === family));
                    });
                    document.querySelector("#queue-result-count").textContent = "1";
                    document.querySelector("#queue-row-root").innerHTML = `
                      <article class="chatbot-labelled-row-list"
                               role="row"
                               tabindex="0"
                               data-chatbot-queue-family="${row.family}"
                               data-chatbot-queue-ref="queue:${row.family}"
                               data-chatbot-item-ref="${row.item}"
                               data-chatbot-source-version="${row.source}">
                        <dl class="chatbot-definition-list">
                          <dt class="chatbot-labelled-row">Queue family</dt>
                          <dd><code class="chatbot-code">${row.family}</code></dd>
                          <dt class="chatbot-labelled-row">Item ref</dt>
                          <dd><code class="chatbot-code">${row.item}</code></dd>
                          <dt class="chatbot-labelled-row">Lifecycle state</dt>
                          <dd><code class="chatbot-code">${row.state}</code></dd>
                          <dt class="chatbot-labelled-row">Risk</dt>
                          <dd><code class="chatbot-code">${row.risk}</code></dd>
                          <dt class="chatbot-labelled-row">Confidence</dt>
                          <dd><code class="chatbot-code">${row.confidence}</code></dd>
                          <dt class="chatbot-labelled-row">Assignee</dt>
                          <dd><code class="chatbot-code">admin:reviewer-a</code></dd>
                          <dt class="chatbot-labelled-row">Owner role</dt>
                          <dd><code class="chatbot-code">operations-admin</code></dd>
                          <dt class="chatbot-labelled-row">Next action</dt>
                          <dd><code class="chatbot-code">${row.action}</code></dd>
                          <dt class="chatbot-labelled-row">Retry count</dt>
                          <dd><code class="chatbot-code">${row.retry}</code></dd>
                          <dt class="chatbot-labelled-row">Terminal status</dt>
                          <dd><code class="chatbot-code">non-terminal</code></dd>
                          <dt class="chatbot-labelled-row">Health</dt>
                          <dd><code class="chatbot-code">degraded</code></dd>
                          <dt class="chatbot-labelled-row">Freshness</dt>
                          <dd><code class="chatbot-code">2026-06-02T04:00:00Z</code></dd>
                          <dt class="chatbot-labelled-row">Source version</dt>
                          <dd><code class="chatbot-code">${row.source}</code></dd>
                          <dt class="chatbot-labelled-row">Diagnostics</dt>
                          <dd><code class="chatbot-code">${row.correlation}</code> <code class="chatbot-code">tenant:tenant-alpha</code> <code class="chatbot-code">mailbox:operations</code> <code class="chatbot-code">${row.workflow}</code></dd>
                          <dt class="chatbot-labelled-row">Redaction state</dt>
                          <dd><code class="chatbot-code">metadata_only</code></dd>
                        </dl>
                        <div class="chatbot-command-bar">
                          <button type="button" class="chatbot-touch-target-primary" data-chatbot-critical-action="true">Claim ${row.item} ${row.family}</button>
                          <button type="button" class="chatbot-touch-target-dense-secondary" data-chatbot-critical-action="true">More actions ${row.item} ${row.family}</button>
                          <button type="button"
                                  class="chatbot-touch-target-dense-secondary"
                                  aria-disabled="true"
                                  aria-describedby="${row.reasonId}"
                                  onclick="if (this.getAttribute('aria-disabled') !== 'true') { window.__detailOpenCount += 1; }">Open detail ${row.item} ${row.family}</button>
                          <span id="${row.reasonId}"
                                tabindex="0"
                                aria-label="Why unavailable? Detail for ${row.family} requires project authority or escalation.">
                            Detail for ${row.family} requires project authority or escalation.
                          </span>
                        </div>
                      </article>`;
                  }

                  document.querySelectorAll("[data-queue-tab]").forEach(tab => {
                    tab.addEventListener("click", () => renderQueueRow(tab.dataset.queueTab));
                  });
                  renderQueueRow("ambiguous-association");
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
                          data-chatbot-actor-category="ExternalParty"
                          aria-label="External party actor: External participant">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">EP</span>
                      <span class="chatbot-actor-badge__category">External party</span>
                      <span class="chatbot-actor-badge__label">External participant</span>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="ServiceClient"
                          aria-label="Service client actor: Graph connector">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">SC</span>
                      <span class="chatbot-actor-badge__category">Service client</span>
                      <span class="chatbot-actor-badge__label">Graph connector</span>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="AiActor"
                          aria-label="AI actor: Copilot planner">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">AI</span>
                      <span class="chatbot-actor-badge__category">AI actor</span>
                      <span class="chatbot-actor-badge__label">Copilot planner</span>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="BackgroundWorker"
                          aria-label="Background worker actor: Intake worker">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">BW</span>
                      <span class="chatbot-actor-badge__category">Background worker</span>
                      <span class="chatbot-actor-badge__label">Intake worker</span>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="Cli"
                          aria-label="CLI actor: chatbot-cli">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">CL</span>
                      <span class="chatbot-actor-badge__category">CLI</span>
                      <span class="chatbot-actor-badge__label">chatbot-cli</span>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="Mcp"
                          aria-label="MCP actor: Unresolved actor">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">MP</span>
                      <span class="chatbot-actor-badge__category">MCP</span>
                      <span class="chatbot-actor-badge__label">Unresolved actor</span>
                      <button class="chatbot-actor-badge__action" type="button" aria-label="Resolve MCP actor: Unresolved actor">Resolve actor</button>
                    </span>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="MailboxEvent"
                          aria-label="Mailbox event actor: Shared mailbox event">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">ME</span>
                      <span class="chatbot-actor-badge__category">Mailbox event</span>
                      <span class="chatbot-actor-badge__label">Shared mailbox event</span>
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
                          aria-describedby="evidence-redacted-reason"
                          aria-label="Evidence redacted: Supporting file. Evidence is redacted by policy.">
                      <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                      <span class="chatbot-chip__label">Supporting file</span>
                      <span class="chatbot-chip__status">Evidence redacted</span>
                    </span>
                    <span id="evidence-redacted-reason" class="chatbot-chip__reason">Evidence is redacted by policy.</span>
                    <span class="chatbot-chip chatbot-chip--evidence"
                          data-chatbot-evidence-state="Unavailable"
                          aria-describedby="evidence-unavailable-reason"
                          aria-label="Evidence unavailable: Evidence cache. Evidence store is unavailable.">
                      <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                      <span class="chatbot-chip__label">Evidence cache</span>
                      <span class="chatbot-chip__status">Evidence unavailable</span>
                    </span>
                    <span id="evidence-unavailable-reason" class="chatbot-chip__reason">Evidence store is unavailable.</span>
                    <span class="chatbot-chip chatbot-chip--evidence"
                          data-chatbot-evidence-state="Unauthorized"
                          aria-describedby="evidence-unauthorized-reason"
                          aria-label="Evidence restricted: Restricted metadata. Authorization required.">
                      <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                      <span class="chatbot-chip__label">Restricted metadata</span>
                      <span class="chatbot-chip__status">Evidence restricted</span>
                    </span>
                    <span id="evidence-unauthorized-reason" class="chatbot-chip__reason">Authorization required.</span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="ExternallyVisible"
                          role="status"
                          aria-label="Risk: Externally visible. Policy reason: Customer-visible output requires review.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Externally visible</span>
                      <span class="chatbot-chip__status">Customer-visible output requires review.</span>
                    </span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="FileExposing"
                          role="status"
                          aria-label="Risk: File-exposing. Policy reason: File metadata could be exposed.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">File-exposing</span>
                      <span class="chatbot-chip__status">File metadata could be exposed.</span>
                    </span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="ProjectMutating"
                          role="status"
                          aria-label="Risk: Project-mutating. Policy reason: Project state would change.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Project-mutating</span>
                      <span class="chatbot-chip__status">Project state would change.</span>
                    </span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="ToolInvoking"
                          role="status"
                          aria-label="Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Tool-invoking</span>
                      <span class="chatbot-chip__status">Requires approval before invoking an external tool.</span>
                    </span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="TaskCreating"
                          role="status"
                          aria-label="Risk: Task-creating. Policy reason: Creates follow-up work for another actor.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Task-creating</span>
                      <span class="chatbot-chip__status">Creates follow-up work for another actor.</span>
                    </span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="ParticipantRepresenting"
                          role="status"
                          aria-label="Risk: Participant-representing. Policy reason: Acts on behalf of a participant.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Participant-representing</span>
                      <span class="chatbot-chip__status">Acts on behalf of a participant.</span>
                    </span>
                  </section>
                  <section class="chatbot-section" aria-label="Status banners">
                    <div class="chatbot-status"
                         data-chatbot-status="info"
                         role="status"
                         aria-live="polite"
                         aria-label="Info status: Command accepted; projection is pending.">
                      <span class="chatbot-status__label">Info</span>
                      <span>Command accepted; projection is pending.</span>
                    </div>
                    <div class="chatbot-status"
                         data-chatbot-status="warning"
                         role="status"
                         aria-live="polite"
                         aria-label="Warning status: Dependency degraded; retry remains available.">
                      <span class="chatbot-status__label">Warning</span>
                      <span>Dependency degraded; retry remains available.</span>
                    </div>
                    <div class="chatbot-status"
                         data-chatbot-status="danger"
                         role="alert"
                         aria-live="assertive"
                         aria-label="Danger status: Validation failed for the current user.">
                      <span class="chatbot-status__label">Danger</span>
                      <span>Validation failed for the current user.</span>
                    </div>
                    <div class="chatbot-status"
                         data-chatbot-status="success"
                         role="status"
                         aria-live="polite"
                         aria-label="Success status: Audit metadata committed.">
                      <span class="chatbot-status__label">Success</span>
                      <span>Audit metadata committed.</span>
                    </div>
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

    private static string BuildLocalizedGovernedOperationsFixture(string culture)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        bool french = string.Equals(culture, "fr", StringComparison.Ordinal);
        string title = french ? "Opérations gouvernées" : "Governed operations";
        string action = french ? "Enregistrer la note gouvernée" : "Record governed note";
        string operation = french ? "Opération" : "Operation";
        string command = french ? "Commande" : "Command";
        string lifecycle = french ? "État du cycle de vie" : "Lifecycle state";
        string completion = french ? "Statut d'achèvement" : "Completion status";
        string audit = french ? "Statut d'audit" : "Audit status";
        string safeNext = french ? "Actions sûres suivantes" : "Safe next actions";
        string projectionLabel = french ? "Statut de projection : en attente" : "Projection status: pending";
        string auditLabel = french ? "Statut d'audit : validé" : "Audit status: committed";
        string metadata = french ? "Historique d'audit (métadonnées uniquement)" : "Audit history (metadata-only)";

        return $$"""
            <!doctype html>
            <html lang="{{culture}}">
              <head>
                <meta charset="utf-8" />
                <title>{{title}}</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main">
                  <section class="chatbot-page" data-chatbot-responsive-fixture="governed-operations">
                    <header class="chatbot-page-header">
                      <h1 class="chatbot-page-title">{{title}}</h1>
                      <button type="button" class="chatbot-touch-target-primary">{{action}}</button>
                    </header>
                    <div class="chatbot-status-group" aria-label="{{(french ? "Résumé du statut de l'opération" : "Operation status summary")}}">
                      <div class="chatbot-status"
                           data-chatbot-status="warning"
                           data-chatbot-feedback-state="CurrentUserCommandAcceptedProjectionPending"
                           data-chatbot-announcement-key="01ARZ3NDEKTSV4RRFFQ69G5FAX"
                           role="status"
                           aria-live="polite"
                           aria-label="{{projectionLabel}}">
                        <span class="chatbot-status__label">{{(french ? "Avertissement" : "Warning")}}</span>
                        <span>{{(french ? "La projection n'est pas complète." : "Projection is not complete.")}} <code class="chatbot-code">AcceptedProjectionPending</code></span>
                      </div>
                      <div class="chatbot-status"
                           data-chatbot-status="success"
                           data-chatbot-feedback-state="CurrentUserCommandAcceptedProjectionPending"
                           data-chatbot-announcement-key="01ARZ3NDEKTSV4RRFFQ69G5FAX-audit"
                           role="status"
                           aria-live="polite"
                           aria-label="{{auditLabel}}">
                        <span class="chatbot-status__label">{{(french ? "Succès" : "Success")}}</span>
                        <span>{{(french ? "Les métadonnées d'audit sont validées." : "Audit metadata is committed.")}} <code class="chatbot-code">Committed</code></span>
                      </div>
                    </div>
                    <dl class="chatbot-definition-list chatbot-labelled-row-list">
                      <dt class="chatbot-labelled-row">{{operation}}</dt>
                      <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAX</code></dd>
                      <dt class="chatbot-labelled-row">{{command}}</dt>
                      <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAV</code></dd>
                      <dt class="chatbot-labelled-row">{{lifecycle}}</dt>
                      <dd><code class="chatbot-code">Accepted</code></dd>
                      <dt class="chatbot-labelled-row">{{completion}}</dt>
                      <dd><code class="chatbot-code">AcceptedProjectionPending</code></dd>
                      <dt class="chatbot-labelled-row">{{audit}}</dt>
                      <dd><code class="chatbot-code">Committed</code></dd>
                      <dt class="chatbot-labelled-row">{{safeNext}}</dt>
                      <dd><code class="chatbot-code">Retry, inspect audit metadata, defer</code></dd>
                    </dl>
                    <h2 class="chatbot-section-title">{{metadata}}</h2>
                    <code class="chatbot-code">post-commit - allow/proposed - audit:Committed - origin:Ui - correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW</code>
                  </section>
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildFrenchCriticalLabelFixture()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        return $$"""
            <!doctype html>
            <html lang="fr">
              <head>
                <meta charset="utf-8" />
                <title>Libellés critiques gouvernés</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main">
                  <section class="chatbot-page" data-chatbot-responsive-fixture="governed-operations">
                    <h1 class="chatbot-page-title">Libellés critiques gouvernés</h1>
                    <div class="chatbot-status-group" aria-label="Résumé du statut de l'opération">
                      <div class="chatbot-status"
                           data-chatbot-status="warning"
                           data-chatbot-critical-localized="true"
                           role="status"
                           aria-live="polite"
                           aria-label="Statut de projection : en attente">
                        <span class="chatbot-status__label">Avertissement</span>
                        <span>La projection n'est pas complète. La récupération sûre reste disponible.</span>
                      </div>
                      <span class="chatbot-chip chatbot-chip--risk"
                            data-chatbot-status="warning"
                            data-chatbot-critical-localized="true"
                            role="status"
                            aria-label="Risque : Invoque un outil. Raison de stratégie : Approbation requise avant d'appeler un outil externe.">
                        <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                        <span class="chatbot-chip__label">Invoque un outil</span>
                        <span class="chatbot-chip__status">Approbation requise avant d'appeler un outil externe.</span>
                      </span>
                    </div>
                    <span class="chatbot-actor-badge"
                          data-chatbot-actor-category="HumanUser"
                          data-chatbot-critical-localized="true"
                          aria-label="Acteur utilisateur humain : Jerome">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">HU</span>
                      <span class="chatbot-actor-badge__category">utilisateur humain</span>
                      <span class="chatbot-actor-badge__label">Jerome</span>
                    </span>
                    <dl class="chatbot-definition-list chatbot-labelled-row-list">
                      <dt class="chatbot-labelled-row" data-chatbot-critical-localized="true">Confiance</dt>
                      <dd data-chatbot-critical-localized="true">Confiance : 88 %</dd>
                      <dt class="chatbot-labelled-row" data-chatbot-critical-localized="true">État</dt>
                      <dd data-chatbot-critical-localized="true">En attente de projection sûre</dd>
                    </dl>
                    <section class="chatbot-blocked-state"
                             data-chatbot-critical-localized="true"
                             role="alert"
                             aria-live="assertive"
                             aria-label="Refusé : L'action demandée est bloquée par la stratégie. Prochaine action : Choisir une action à risque plus faible.">
                      <div class="chatbot-blocked-state__heading">
                        <span class="chatbot-chip__cue" aria-hidden="true">BL</span>
                        <h2 class="chatbot-section-title">Refusé</h2>
                      </div>
                      <p class="chatbot-body">L'action demandée est bloquée par la stratégie.</p>
                      <p class="chatbot-body"><strong>Prochaine action :</strong> Choisir une action à risque plus faible.</p>
                      <p class="chatbot-body"><strong>Raison de récupération sûre :</strong> La revue de quarantaine doit être terminée avant une nouvelle tentative.</p>
                    </section>
                  </section>
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildRedactionRecoveryCognitiveLoadFixture(string culture)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        bool french = string.Equals(culture, "fr", StringComparison.Ordinal);
        string title = french ? "Récupération gouvernée" : "Governed recovery";
        string notice = french
            ? "Cette exportation est masquée ; le détail complet nécessite une escalade."
            : "This export is redacted; full detail requires escalation.";
        string escalation = french
            ? "Demandez une escalade pour voir le détail complet."
            : "Request escalation to view full detail.";
        string filter = french ? "Filtre : Revue en attente. 2 résultats." : "Filter: Pending review. 2 results.";
        string retry = french
            ? "Réessayez uniquement quand la copie de sûreté anti-doublon reste visible."
            : "Retry only while duplicate-safety copy remains visible.";
        string evidence = french ? "Preuve" : "Evidence";
        string risk = french ? "Risque" : "Risk";
        string status = french ? "Statut" : "Status";
        string actor = french ? "Acteur" : "Actor";
        string timestamp = french ? "Horodatage" : "Timestamp";

        return $$"""
            <!doctype html>
            <html lang="{{culture}}">
              <head>
                <meta charset="utf-8" />
                <title>{{title}}</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main">
                  <section class="chatbot-page" data-chatbot-responsive-fixture="redaction-recovery">
                    <h1 class="chatbot-page-title">{{title}}</h1>
                    <p class="chatbot-body">Pending governed note review</p>
                    <p class="chatbot-body" data-chatbot-active-filter-summary="true">{{filter}}</p>
                    <div class="chatbot-status"
                         data-chatbot-status="info"
                         data-chatbot-feedback-state="ObservedForOthersRejectionOrQueueUpdate"
                         data-chatbot-off-surface-kind="AuditCopy"
                         aria-label="{{notice}}">
                      <span class="chatbot-status__label">{{(french ? "Info" : "Info")}}</span>
                      <span>{{notice}}</span>
                      <span>{{escalation}}</span>
                    </div>
                    <dl class="chatbot-definition-list chatbot-labelled-row-list"
                        data-chatbot-canonical-field-order="evidence,risk,status,actor,timestamp">
                      <dt class="chatbot-labelled-row">{{evidence}}</dt>
                      <dd>Audit metadata only</dd>
                      <dt class="chatbot-labelled-row">{{risk}}</dt>
                      <dd>Low risk handoff</dd>
                      <dt class="chatbot-labelled-row">{{status}}</dt>
                      <dd><code class="chatbot-code">audit:Committed</code></dd>
                      <dt class="chatbot-labelled-row">{{actor}}</dt>
                      <dd><code class="chatbot-code">Ui</code></dd>
                      <dt class="chatbot-labelled-row">{{timestamp}}</dt>
                      <dd>2026-05-31T09:00:00Z</dd>
                      <dt class="chatbot-labelled-row">Operation</dt>
                      <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAX</code></dd>
                    </dl>
                    <div class="chatbot-command-bar" data-chatbot-workflow-item="audit-entry">
                      <button type="button" class="chatbot-touch-target-primary" data-chatbot-action-kind="primary">Approve governed operation</button>
                      <button type="button" class="chatbot-touch-target-primary" data-chatbot-action-kind="secondary">Defer governed operation</button>
                      <button type="button" class="chatbot-touch-target-primary" data-chatbot-action-kind="destructive">Reject governed operation</button>
                    </div>
                    <section class="chatbot-blocked-state" role="status" aria-label="{{retry}}">
                      <h2 class="chatbot-section-title">{{(french ? "Récupération sûre" : "Safe recovery")}}</h2>
                      <p class="chatbot-body">Retry is duplicate-safe and will not create a second command.</p>
                      <p class="chatbot-body">{{retry}}</p>
                    </section>
                  </section>
                </main>
              </body>
            </html>
            """;
    }

    private static string BuildAssociationReviewFixture(AssociationReviewFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string body = scenario is AssociationReviewFixtureScenario.BlockedRedacted
            ? BuildBlockedAssociationReviewBody()
            : BuildCandidateAssociationReviewBody();

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Association review</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main" id="chatbot-main-content" tabindex="-1">
                  <section class="chatbot-conversation-shell"
                           aria-label="Association review"
                           data-chatbot-responsive-fixture="association-review">
                    <div class="chatbot-conversation-shell__context">
                      <header class="chatbot-project-context-header" aria-label="Project context">
                        <div class="chatbot-project-context-header__identity">
                          <span class="chatbot-metadata">S2</span>
                          <h2 class="chatbot-project-context-header__title">Association review</h2>
                          <span class="chatbot-metadata"><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAZ</code></span>
                        </div>
                        <div class="chatbot-status"
                             data-chatbot-status="info"
                             role="status"
                             aria-live="off"
                             aria-label="Association status: NeedsReview - Ambiguous - 72%">
                          <span class="chatbot-status__label">Info</span>
                          <span>NeedsReview - Ambiguous - 72%</span>
                        </div>
                      </header>
                    </div>
                    <div class="chatbot-conversation-shell__body">
                      <section class="chatbot-conversation-shell__main" role="region" aria-label="Candidate projects">
                        <section class="chatbot-page chatbot-association-review"
                                 aria-labelledby="association-review-title"
                                 data-chatbot-responsive-fixture="association-review">
                          <header class="chatbot-page-header">
                            <span class="chatbot-metadata">S2</span>
                            <h1 id="association-review-title" class="chatbot-page-title">Association review</h1>
                            <p class="chatbot-body">Review authorized metadata, then defer or escalate until decision recording is available.</p>
                          </header>
                          {{body}}
                        </section>
                      </section>
                      <aside class="chatbot-conversation-shell__panel chatbot-panel-transition"
                             role="complementary"
                             aria-label="Evidence comparison">
                        <section class="chatbot-association-comparison"
                                 aria-labelledby="association-comparison-title"
                                 data-chatbot-association-comparison="true">
                          <h2 id="association-comparison-title" class="chatbot-section-title">Evidence comparison</h2>
                          <article id="association-comparison-panel"
                                   class="chatbot-association-comparison__panel"
                                   aria-label="No candidate selected">
                            <div class="chatbot-status"
                                 data-chatbot-status="info"
                                 aria-label="Review authorized metadata, then defer or escalate until decision recording is available.">
                              <span class="chatbot-status__label">Info</span>
                              <span>Review authorized metadata, then defer or escalate until decision recording is available.</span>
                            </div>
                          </article>
                        </section>
                        <section class="chatbot-section" aria-labelledby="association-source-title">
                          <h2 id="association-source-title" class="chatbot-section-title">Source metadata</h2>
                          <dl class="chatbot-definition-list chatbot-labelled-row-list">
                            <dt class="chatbot-labelled-row">Operation</dt>
                            <dd><code class="chatbot-code">01ARZ3NDEKTSV4RRFFQ69G5FAZ</code></dd>
                            <dt class="chatbot-labelled-row">Conversation context</dt>
                            <dd><code class="chatbot-code">mailbox-message-7</code></dd>
                            <dt class="chatbot-labelled-row">Lifecycle state</dt>
                            <dd><code class="chatbot-code">NeedsReview</code></dd>
                            <dt class="chatbot-labelled-row">Safe next actions</dt>
                            <dd><code class="chatbot-code">safe-next-action, projection-pending</code></dd>
                          </dl>
                        </section>
                      </aside>
                    </div>
                  </section>
                </main>
                <script>
                  window.__decisionPreviewCount = 0;
                  const comparison = document.querySelector("#association-comparison-panel");
                  document.querySelectorAll("[role='radio']").forEach(candidate => {
                    candidate.addEventListener("click", event => {
                      document.querySelectorAll("[role='radio']").forEach(item => item.setAttribute("aria-checked", "false"));
                      event.currentTarget.setAttribute("aria-checked", "true");
                      comparison.setAttribute("aria-label", "Authorized candidate A");
                      comparison.innerHTML = `
                        <h3 class="chatbot-card-title">Authorized candidate A</h3>
                        <dl class="chatbot-definition-list chatbot-labelled-row-list">
                          <dt class="chatbot-labelled-row">Project</dt>
                          <dd><code class="chatbot-code">project-alpha</code></dd>
                          <dt class="chatbot-labelled-row">Confidence</dt>
                          <dd>72%</dd>
                          <dt class="chatbot-labelled-row">Reason codes</dt>
                          <dd><code class="chatbot-code">thread-reference, participant-match</code></dd>
                        </dl>
                        <div class="chatbot-association-evidence-grid">
                          <button class="chatbot-chip chatbot-chip--evidence"
                                  type="button"
                                  data-chatbot-evidence-state="Available"
                                  aria-label="Available evidence: thread-reference AUTH-100"
                                  aria-disabled="false">
                            <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                            <span class="chatbot-chip__label">thread-reference AUTH-100</span>
                            <span class="chatbot-chip__status">Available evidence</span>
                          </button>
                          <span class="chatbot-chip chatbot-chip--evidence"
                                data-chatbot-evidence-state="Redacted"
                                aria-label="Evidence redacted: restricted metadata. Evidence restricted.">
                            <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                            <span class="chatbot-chip__label">restricted metadata</span>
                            <span class="chatbot-chip__status">Evidence redacted</span>
                          </span>
                        </div>`;
                    });
                  });

                  document.querySelectorAll("[data-chatbot-action-state='DisabledWithReason'] button").forEach(button => {
                    button.addEventListener("click", event => {
                      event.preventDefault();
                    });
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static string BuildAssociationDecisionSubmitFixture(bool conflict)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string submitScript = conflict ? AssociationDecisionConflictScript() : AssociationDecisionAcceptedScript();

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Association decision submit</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main" id="chatbot-main-content" tabindex="-1">
                  <section class="chatbot-page chatbot-association-review"
                           aria-labelledby="association-review-title"
                           data-chatbot-responsive-fixture="association-review">
                    <header class="chatbot-page-header">
                      <span class="chatbot-metadata">S2</span>
                      <h1 id="association-review-title" class="chatbot-page-title">Association review</h1>
                      <p class="chatbot-body">Review authorized metadata, then submit through the command spine.</p>
                    </header>
                    <section class="chatbot-section" aria-labelledby="association-candidates-title">
                      <h2 id="association-candidates-title" class="chatbot-section-title">Candidate projects</h2>
                      <div class="chatbot-association-candidate-list" role="radiogroup" aria-label="Candidate projects">
                        <button class="chatbot-association-candidate chatbot-row-motion chatbot-panel-transition"
                                type="button"
                                role="radio"
                                aria-checked="false"
                                aria-label="Candidate 1. Confidence 72%. Authorized candidate A"
                                data-project-id="project-alpha"
                                data-evidence-fingerprint="hash-project">
                          <span class="chatbot-association-candidate__rank">1</span>
                          <span class="chatbot-association-candidate__body">
                            <span class="chatbot-association-candidate__title">Authorized candidate A</span>
                            <span class="chatbot-association-candidate__meta">Within threshold - 72%</span>
                            <span class="chatbot-association-candidate__reasons">thread-reference, participant-match</span>
                          </span>
                        </button>
                      </div>
                    </section>
                    <section class="chatbot-association-actions" aria-labelledby="association-actions-title">
                      <h2 id="association-actions-title" class="chatbot-section-title">Safe next actions</h2>
                      <label class="chatbot-field">
                        <span class="chatbot-labelled-row">Decision note</span>
                        <textarea class="chatbot-textarea" rows="3" aria-label="Decision note"></textarea>
                      </label>
                      <div id="association-submit-feedback"></div>
                      <div class="chatbot-command-bar chatbot-association-actions__bar">
                        <span class="chatbot-association-action-wrap">
                          <span id="association-action-choose-candidate"
                                class="chatbot-governed-action"
                                data-chatbot-critical-action="true"
                                data-chatbot-action-state="Enabled"
                                data-chatbot-touch-target="primary"
                                data-chatbot-stable-id="association-action-choose-candidate">
                            <button type="button"
                                    aria-label="Choose candidate"
                                    aria-disabled="false">
                              Choose candidate
                            </button>
                          </span>
                          <span class="chatbot-action-consequence">Association will attach to one selected project after the projection refreshes.</span>
                        </span>
                      </div>
                    </section>
                  </section>
                </main>
                <script>
                  window.__submittedCommand = null;
                  window.__routingRefreshCount = 0;
                  let selected = null;
                  document.querySelectorAll("[role='radio']").forEach(candidate => {
                    candidate.addEventListener("click", event => {
                      document.querySelectorAll("[role='radio']").forEach(item => item.setAttribute("aria-checked", "false"));
                      event.currentTarget.setAttribute("aria-checked", "true");
                      selected = event.currentTarget;
                    });
                  });
                  {{submitScript}}
                </script>
              </body>
            </html>
            """;
    }

    private static string AssociationDecisionAcceptedScript()
        => """
                  document.querySelector("[aria-label='Choose candidate']").addEventListener("click", event => {
                    const note = document.querySelector("[aria-label='Decision note']").value.trim().replace(/\s+/g, " ");
                    window.__submittedCommand = {
                      commandType: "AssociateEmailToProject",
                      origin: "ui",
                      command: {
                        associationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                        intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                        projectId: selected?.dataset.projectId,
                        decisionKind: "associate",
                        decisionNote: note,
                        candidateEvidenceFingerprint: selected?.dataset.evidenceFingerprint,
                        sourceVersion: 1,
                        schemaVersion: "chatbot.association-decision-command.v1"
                      }
                    };
                    window.__routingRefreshCount += 1;
                    document.querySelector("#association-submit-feedback").innerHTML = `
                      <div class="chatbot-status"
                           data-chatbot-status="warning"
                           role="status"
                           aria-live="polite"
                           aria-label="Association decision accepted: projection pending">
                        <span class="chatbot-status__label">Warning</span>
                        <span>Association decision accepted: projection pending</span>
                      </div>
                      <div class="chatbot-status"
                           data-chatbot-status="info"
                           role="status"
                           aria-live="polite"
                           aria-label="Audit status: reconciling">
                        <span class="chatbot-status__label">Info</span>
                        <span>Audit status: reconciling</span>
                      </div>`;
                    const action = document.querySelector("#association-action-choose-candidate");
                    action.dataset.chatbotActionState = "DisabledWithReason";
                    event.currentTarget.setAttribute("aria-disabled", "true");
                    event.currentTarget.setAttribute("aria-describedby", "association-action-choose-candidate-disabled-reason");
                    action.insertAdjacentHTML("beforeend", `
                      <span id="association-action-choose-candidate-disabled-reason"
                            class="chatbot-governed-action__reason"
                            tabindex="0"
                            aria-label="Why unavailable? Decision already recorded.">
                        <strong>Why unavailable?</strong> Decision already recorded.
                      </span>`);
                  });
            """;

    private static string AssociationDecisionConflictScript()
        => """
                  document.querySelector("[aria-label='Choose candidate']").addEventListener("click", event => {
                    event.preventDefault();
                    window.__submittedCommand = {
                      commandType: "AssociateEmailToProject",
                      origin: "ui",
                      command: {
                        associationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                        intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                        projectId: selected?.dataset.projectId,
                        decisionKind: "associate",
                        candidateEvidenceFingerprint: selected?.dataset.evidenceFingerprint,
                        sourceVersion: 1,
                        schemaVersion: "chatbot.association-decision-command.v1"
                      }
                    };
                    document.querySelector("#association-submit-feedback").innerHTML = `
                      <div class="chatbot-status"
                           data-chatbot-status="danger"
                           role="alert"
                           aria-live="assertive"
                           aria-label="Submission failed: idempotency_conflict_association_decision">
                        <span class="chatbot-status__label">Danger</span>
                        <span>Submission failed. The association decision was already decided.</span>
                      </div>`;
                  });
            """;

    private static string BuildAssociationCorrectionSubmitFixture(bool conflict, bool blocked)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string submitScript = conflict ? AssociationCorrectionConflictScript() : AssociationCorrectionAcceptedScript();
        string disabledReason = blocked
            ? """
                          <span id="association-correction-submit-disabled-reason"
                                class="chatbot-governed-action__reason"
                                tabindex="0"
                                aria-label="Why unavailable? Projection invalidation is unavailable, so correction is blocked.">
                            <strong>Why unavailable?</strong> Projection invalidation is unavailable, so correction is blocked.
                          </span>
              """
            : string.Empty;
        string disabledAttributes = blocked
            ? """
                                    aria-disabled="true"
                                    aria-describedby="association-correction-submit-disabled-reason"
              """
            : """
                                    aria-disabled="false"
              """;
        string actionState = blocked ? "DisabledWithReason" : "Enabled";

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Association correction submit</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main" id="chatbot-main-content" tabindex="-1">
                  <section class="chatbot-page chatbot-association-review"
                           aria-labelledby="association-review-title"
                           data-chatbot-responsive-fixture="association-review">
                    <header class="chatbot-page-header">
                      <span class="chatbot-metadata">S2</span>
                      <h1 id="association-review-title" class="chatbot-page-title">Association review</h1>
                      <p class="chatbot-body">Correct an existing metadata-only association through the command spine.</p>
                    </header>
                    <section class="chatbot-section" aria-labelledby="association-candidates-title">
                      <h2 id="association-candidates-title" class="chatbot-section-title">Candidate projects</h2>
                      <div class="chatbot-association-candidate-list" role="radiogroup" aria-label="Candidate projects">
                        <button class="chatbot-association-candidate chatbot-row-motion chatbot-panel-transition"
                                type="button"
                                role="radio"
                                aria-checked="false"
                                aria-label="Candidate 1. Confidence 72%. Authorized candidate A"
                                data-project-id="project-beta"
                                data-evidence-fingerprint="hash-project-beta">
                          <span class="chatbot-association-candidate__rank">1</span>
                          <span class="chatbot-association-candidate__body">
                            <span class="chatbot-association-candidate__title">Authorized candidate A</span>
                            <span class="chatbot-association-candidate__meta">Within threshold - 72%</span>
                            <span class="chatbot-association-candidate__reasons">thread-reference, participant-match</span>
                          </span>
                        </button>
                      </div>
                    </section>
                    <section class="chatbot-section chatbot-association-correction" aria-labelledby="association-correction-title">
                      <h2 id="association-correction-title" class="chatbot-section-title">Correction</h2>
                      <div class="chatbot-status"
                           data-chatbot-status="info"
                           role="status"
                           aria-live="polite"
                           aria-label="Correction status: projection pending">
                        <span class="chatbot-status__label">Info</span>
                        <span>Correction can be submitted after selecting an authorized target.</span>
                      </div>
                      <dl class="chatbot-definition-list chatbot-labelled-row-list">
                        <dt class="chatbot-labelled-row">Affected context</dt>
                        <dd><code class="chatbot-code" id="correction-affected-context">Select an authorized target project to preview the correction.</code></dd>
                        <dt class="chatbot-labelled-row">Downstream impact</dt>
                        <dd><code class="chatbot-code" id="correction-downstream-impact">Projection update is pending.</code></dd>
                        <dt class="chatbot-labelled-row">Next action</dt>
                        <dd>Review the affected context preview before saving the correction.</dd>
                      </dl>
                      <label class="chatbot-field">
                        <span class="chatbot-labelled-row">Correction rationale</span>
                        <textarea class="chatbot-textarea" rows="3" aria-label="Correction rationale"></textarea>
                      </label>
                      <div id="association-correction-feedback"></div>
                      <span id="association-correction-submit"
                            class="chatbot-governed-action"
                            data-chatbot-critical-action="true"
                            data-chatbot-action-state="{{actionState}}"
                            data-chatbot-touch-target="primary"
                            data-chatbot-stable-id="association-correction-submit">
                        <button type="button"
                                aria-label="Submit correction"
                                {{disabledAttributes}}>
                          Submit correction
                        </button>
                        {{disabledReason}}
                      </span>
                    </section>
                  </section>
                </main>
                <script>
                  window.__submittedCommand = null;
                  window.__routingRefreshCount = 0;
                  window.__correctionSubmitCount = 0;
                  let selected = null;
                  document.querySelectorAll("[role='radio']").forEach(candidate => {
                    candidate.addEventListener("click", event => {
                      document.querySelectorAll("[role='radio']").forEach(item => item.setAttribute("aria-checked", "false"));
                      event.currentTarget.setAttribute("aria-checked", "true");
                      selected = event.currentTarget;
                      document.querySelector("#correction-affected-context").textContent = `Corrected target ${selected.dataset.projectId}`;
                    });
                  });
                  {{submitScript}}
                </script>
              </body>
            </html>
            """;
    }

    private static string AssociationCorrectionAcceptedScript()
        => """
                  document.querySelector("[aria-label='Submit correction']").addEventListener("click", event => {
                    if (event.currentTarget.getAttribute("aria-disabled") === "true") {
                      event.preventDefault();
                      return;
                    }

                    window.__correctionSubmitCount += 1;
                    const rationale = document.querySelector("[aria-label='Correction rationale']").value.trim().replace(/\s+/g, " ");
                    window.__submittedCommand = {
                      commandType: "CorrectEmailProjectAssociation",
                      origin: "ui",
                      command: {
                        associationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                        intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                        priorProjectId: "project-alpha",
                        targetProjectId: selected?.dataset.projectId,
                        correctionKind: "project-reassignment",
                        correctionRationale: rationale,
                        predecessorAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                        candidateEvidenceFingerprint: selected?.dataset.evidenceFingerprint,
                        sourceVersion: 2,
                        schemaVersion: "chatbot.association-correction-command.v1"
                      }
                    };
                    window.__routingRefreshCount += 1;
                    document.querySelector("#correction-downstream-impact").textContent = "preview-only";
                    document.querySelector("#association-correction-feedback").innerHTML = `
                      <div class="chatbot-status"
                           data-chatbot-status="warning"
                           role="status"
                           aria-live="polite"
                           aria-label="Association correction accepted: downstream preview only">
                        <span class="chatbot-status__label">Warning</span>
                        <span>Association correction accepted: downstream preview only</span>
                      </div>
                      <div class="chatbot-status"
                           data-chatbot-status="warning"
                           role="status"
                           aria-live="polite"
                           aria-label="Correction status: partial">
                        <span class="chatbot-status__label">Warning</span>
                        <span>Correction is accepted; downstream propagation remains a preview.</span>
                      </div>`;
                  });
            """;

    private static string AssociationCorrectionConflictScript()
        => """
                  document.querySelector("[aria-label='Submit correction']").addEventListener("click", event => {
                    if (event.currentTarget.getAttribute("aria-disabled") === "true") {
                      event.preventDefault();
                      return;
                    }

                    event.preventDefault();
                    window.__correctionSubmitCount += 1;
                    window.__submittedCommand = {
                      commandType: "CorrectEmailProjectAssociation",
                      origin: "ui",
                      command: {
                        associationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                        intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                        priorProjectId: "project-alpha",
                        targetProjectId: selected?.dataset.projectId,
                        correctionKind: "project-reassignment",
                        predecessorAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                        candidateEvidenceFingerprint: selected?.dataset.evidenceFingerprint,
                        sourceVersion: 2,
                        schemaVersion: "chatbot.association-correction-command.v1"
                      }
                    };
                    document.querySelector("#association-correction-feedback").innerHTML = `
                      <div class="chatbot-status"
                           data-chatbot-status="danger"
                           role="alert"
                           aria-live="assertive"
                           aria-label="Correction failed: idempotency_conflict_correction">
                        <span class="chatbot-status__label">Danger</span>
                        <span>Correction failed. This association has already been corrected.</span>
                      </div>`;
                  });
            """;

    private static string BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        bool blocking = scenario is not CorrectionPropagationFixtureScenario.Complete;
        string lifecycle = scenario switch
        {
            CorrectionPropagationFixtureScenario.Delayed => "Correction-delayed",
            CorrectionPropagationFixtureScenario.Complete => "Corrected",
            _ => "Correcting",
        };
        string propagationStatus = scenario switch
        {
            CorrectionPropagationFixtureScenario.Delayed => "delayed",
            CorrectionPropagationFixtureScenario.Complete => "complete",
            _ => "correcting",
        };
        string downstreamStatus = scenario switch
        {
            CorrectionPropagationFixtureScenario.Complete => "complete",
            _ => "correcting",
        };
        string statusLabel = scenario is CorrectionPropagationFixtureScenario.Complete
            ? "Correction propagation status: complete"
            : $"Correction propagation status: {lifecycle}";
        string statusKind = scenario is CorrectionPropagationFixtureScenario.Complete ? "success" : "warning";
        string statusMessage = scenario switch
        {
            CorrectionPropagationFixtureScenario.Delayed => "Correction propagation is delayed and operations has been alerted.",
            CorrectionPropagationFixtureScenario.Complete => "Correction propagation is complete. Corrected context is ready.",
            _ => "Correction propagation is rebuilding required stores.",
        };
        string progress = scenario switch
        {
            CorrectionPropagationFixtureScenario.Delayed => "1 of 4 stores acknowledged",
            CorrectionPropagationFixtureScenario.Complete => "4 of 4 stores acknowledged",
            _ => "2 of 4 stores acknowledged",
        };
        string owner = scenario is CorrectionPropagationFixtureScenario.Delayed ? "Operations" : "Project owner";
        string nextAction = scenario switch
        {
            CorrectionPropagationFixtureScenario.Delayed => "Escalate to operations while corrected context remains blocked.",
            CorrectionPropagationFixtureScenario.Complete => "Corrected project context is ready for command preparation.",
            _ => "Wait for propagation before using corrected project context.",
        };
        string disabledAttributes = blocking
            ? """
                                      aria-disabled="true"
                                      aria-describedby="association-correction-submit-disabled-reason"
              """
            : """
                                      aria-disabled="false"
              """;
        string aiDisabledAttributes = blocking
            ? """
                                      aria-disabled="true"
                                      aria-describedby="association-ai-action-disabled-reason"
              """
            : """
                                      aria-disabled="false"
              """;
        string disabledReason = blocking
            ? """
                            <span id="association-correction-submit-disabled-reason"
                                  class="chatbot-governed-action__reason"
                                  tabindex="0"
                                  aria-label="Why unavailable? Corrected context is not ready for AI actions or command preparation.">
                              <strong>Why unavailable?</strong> Corrected context is not ready for AI actions or command preparation.
                            </span>
              """
            : string.Empty;
        string aiDisabledReason = blocking
            ? """
                            <span id="association-ai-action-disabled-reason"
                                  class="chatbot-governed-action__reason"
                                  tabindex="0"
                                  aria-label="Why unavailable? Corrected context is not ready for AI actions or command preparation.">
                              <strong>Why unavailable?</strong> Corrected context is not ready for AI actions or command preparation.
                            </span>
              """
            : string.Empty;
        string actionState = blocking ? "DisabledWithReason" : "Enabled";

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Association correction propagation</title>
                <style>{{css}}</style>
              </head>
              <body>
                <main class="chatbot-shell-main" id="chatbot-main-content" tabindex="-1">
                  <section class="chatbot-page chatbot-association-review"
                           aria-labelledby="association-review-title"
                           data-chatbot-responsive-fixture="association-review">
                    <header class="chatbot-page-header">
                      <span class="chatbot-metadata">S2</span>
                      <h1 id="association-review-title" class="chatbot-page-title">Association review</h1>
                      <p class="chatbot-body">Correction propagation uses metadata-only status until every required store acknowledges.</p>
                    </header>
                    <section class="chatbot-section chatbot-association-correction" aria-labelledby="association-correction-title">
                      <h2 id="association-correction-title" class="chatbot-section-title">Correction</h2>
                      <div class="chatbot-status"
                           data-chatbot-status="{{statusKind}}"
                           data-chatbot-feedback-state="DependencyDegraded"
                           data-chatbot-announcement-key="correction-propagation-01ARZ3NDEKTSV4RRFFQ69G5FAV"
                           role="status"
                           aria-live="polite"
                           aria-label="{{statusLabel}}">
                        <span class="chatbot-status__label">{{(statusKind is "success" ? "Success" : "Warning")}}</span>
                        <span>{{statusMessage}}</span>
                      </div>
                      <dl class="chatbot-definition-list chatbot-labelled-row-list">
                        <dt class="chatbot-labelled-row">Lifecycle state</dt>
                        <dd><code class="chatbot-code">{{lifecycle}}</code></dd>
                        <dt class="chatbot-labelled-row">Downstream impact</dt>
                        <dd><code class="chatbot-code">{{downstreamStatus}}</code></dd>
                        <dt class="chatbot-labelled-row">Propagation status</dt>
                        <dd><code class="chatbot-code">{{propagationStatus}}</code></dd>
                        <dt class="chatbot-labelled-row">Propagation progress</dt>
                        <dd>{{progress}}</dd>
                        <dt class="chatbot-labelled-row">Estimated completion</dt>
                        <dd>2026-05-31T09:40:00Z</dd>
                        <dt class="chatbot-labelled-row">Responsible owner</dt>
                        <dd>{{owner}}</dd>
                        <dt class="chatbot-labelled-row">Workflow instance</dt>
                        <dd><code class="chatbot-code">workflow-correction-001</code></dd>
                        <dt class="chatbot-labelled-row">Required stores</dt>
                        <dd><code class="chatbot-code">association-routing, evidence-snapshot, operational-status, ai-context-readiness</code></dd>
                        <dt class="chatbot-labelled-row">Completed stores</dt>
                        <dd><code class="chatbot-code">association-routing, evidence-snapshot</code></dd>
                        <dt class="chatbot-labelled-row">Next action</dt>
                        <dd>{{nextAction}}</dd>
                      </dl>
                      <div class="chatbot-command-bar chatbot-association-actions__bar">
                        <span class="chatbot-association-action-wrap">
                          <span class="chatbot-governed-action"
                                data-chatbot-critical-action="true"
                                data-chatbot-action-state="{{actionState}}"
                                data-chatbot-touch-target="primary"
                                data-chatbot-stable-id="association-correction-submit">
                            <button type="button"
                                    aria-label="Submit correction"
                                    {{disabledAttributes}}>
                              Submit correction
                            </button>
                            {{disabledReason}}
                          </span>
                        </span>
                        <span class="chatbot-association-action-wrap">
                          <span class="chatbot-governed-action"
                                data-chatbot-critical-action="true"
                                data-chatbot-action-state="{{actionState}}"
                                data-chatbot-touch-target="primary"
                                data-chatbot-stable-id="association-ai-action">
                            <button type="button"
                                    aria-label="Prepare AI action"
                                    {{aiDisabledAttributes}}>
                              Prepare AI action
                            </button>
                            {{aiDisabledReason}}
                          </span>
                        </span>
                        <button type="button" class="chatbot-touch-target-primary" aria-label="Refresh status">Refresh status</button>
                      </div>
                    </section>
                  </section>
                </main>
                <script>
                  window.__correctionSubmitCount = 0;
                  window.__aiActionPrepareCount = 0;
                  window.__routingRefreshCount = 0;
                  window.__workflowStartCount = 0;

                  document.querySelector("[aria-label='Submit correction']").addEventListener("click", event => {
                    if (event.currentTarget.getAttribute("aria-disabled") === "true") {
                      event.preventDefault();
                      return;
                    }

                    window.__correctionSubmitCount += 1;
                  });
                  document.querySelector("[aria-label='Prepare AI action']").addEventListener("click", event => {
                    if (event.currentTarget.getAttribute("aria-disabled") === "true") {
                      event.preventDefault();
                      return;
                    }

                    window.__aiActionPrepareCount += 1;
                  });
                  document.querySelector("[aria-label='Refresh status']").addEventListener("click", () => {
                    window.__routingRefreshCount += 1;
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static string BuildCandidateAssociationReviewBody()
        => """
                          <section class="chatbot-section" aria-labelledby="association-candidates-title">
                            <h2 id="association-candidates-title" class="chatbot-section-title">Candidate projects</h2>
                            <div class="chatbot-association-candidate-list" role="radiogroup" aria-label="Candidate projects">
                              <button class="chatbot-association-candidate chatbot-row-motion chatbot-panel-transition"
                                      type="button"
                                      role="radio"
                                      aria-checked="false"
                                      aria-label="Candidate 1. Confidence 72%. Authorized candidate A"
                                      data-chatbot-association-candidate="project-alpha">
                                <span class="chatbot-association-candidate__rank">1</span>
                                <span class="chatbot-association-candidate__body">
                                  <span class="chatbot-association-candidate__title">Authorized candidate A</span>
                                  <span class="chatbot-association-candidate__meta">Within threshold - 72%</span>
                                  <span class="chatbot-association-candidate__reasons">thread-reference, participant-match</span>
                                </span>
                                <span class="chatbot-association-candidate__evidence">
                                  <span class="chatbot-chip chatbot-chip--evidence"
                                        data-chatbot-evidence-state="Available"
                                        aria-label="Available evidence: thread-reference AUTH-100">
                                    <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                                    <span class="chatbot-chip__label">thread-reference AUTH-100</span>
                                    <span class="chatbot-chip__status">Available evidence</span>
                                  </span>
                                  <span class="chatbot-chip chatbot-chip--evidence"
                                        data-chatbot-evidence-state="Redacted"
                                        aria-label="Evidence redacted: restricted metadata. Evidence restricted.">
                                    <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                                    <span class="chatbot-chip__label">restricted metadata</span>
                                    <span class="chatbot-chip__status">Evidence redacted</span>
                                  </span>
                                </span>
                              </button>
                              <button class="chatbot-association-candidate chatbot-row-motion chatbot-panel-transition"
                                      type="button"
                                      role="radio"
                                      aria-checked="false"
                                      aria-label="Candidate 2. Confidence 68%. Authorized candidate B"
                                      data-chatbot-association-candidate="project-beta">
                                <span class="chatbot-association-candidate__rank">2</span>
                                <span class="chatbot-association-candidate__body">
                                  <span class="chatbot-association-candidate__title">Authorized candidate B</span>
                                  <span class="chatbot-association-candidate__meta">Within threshold - 68%</span>
                                  <span class="chatbot-association-candidate__reasons">subject-alias, attachment-metadata</span>
                                </span>
                              </button>
                            </div>
                          </section>
                          <section class="chatbot-association-actions" aria-labelledby="association-actions-title">
                            <h2 id="association-actions-title" class="chatbot-section-title">Safe next actions</h2>
                            <label class="chatbot-field">
                              <span class="chatbot-labelled-row">Decision note</span>
                              <textarea class="chatbot-textarea" rows="3" aria-label="Decision note"></textarea>
                            </label>
                            <div class="chatbot-command-bar chatbot-association-actions__bar">
                              <span class="chatbot-association-action-wrap">
                                <span class="chatbot-governed-action"
                                      data-chatbot-critical-action="true"
                                      data-chatbot-action-state="DisabledWithReason"
                                      data-chatbot-touch-target="primary"
                                      data-chatbot-stable-id="association-action-choose-candidate">
                                  <button type="button"
                                          aria-label="Choose candidate"
                                          aria-disabled="true"
                                          aria-describedby="association-action-choose-candidate-disabled-reason">
                                    Choose candidate
                                  </button>
                                  <span id="association-action-choose-candidate-disabled-reason"
                                        class="chatbot-governed-action__reason"
                                        tabindex="0"
                                        aria-label="Why unavailable? Projection is still updating.">
                                    <strong>Why unavailable?</strong> Projection is still updating.
                                  </span>
                                </span>
                                <span class="chatbot-action-consequence">Association will attach to one selected project when decision recording is available.</span>
                              </span>
                              <span class="chatbot-association-action-wrap">
                                <span class="chatbot-governed-action"
                                      data-chatbot-critical-action="true"
                                      data-chatbot-action-state="DisabledWithReason"
                                      data-chatbot-touch-target="primary"
                                      data-chatbot-stable-id="association-action-defer">
                                  <button type="button"
                                          aria-label="Defer"
                                          aria-disabled="true"
                                          aria-describedby="association-action-defer-disabled-reason">
                                    Defer
                                  </button>
                                  <span id="association-action-defer-disabled-reason"
                                        class="chatbot-governed-action__reason"
                                        tabindex="0"
                                        aria-label="Why unavailable? Projection is still updating.">
                                    <strong>Why unavailable?</strong> Projection is still updating.
                                  </span>
                                </span>
                                <span class="chatbot-action-consequence">The item remains visible for later review.</span>
                              </span>
                            </div>
                          </section>
            """;

    private static string BuildBlockedAssociationReviewBody()
        => """
                          <section class="chatbot-blocked-state"
                                   data-chatbot-blocked-reason="UnresolvedAssociation"
                                   role="alert"
                                   aria-live="assertive"
                                   aria-label="Blocked: No authorized candidates are available. Next action: Review authorized metadata, then defer or escalate.">
                            <div class="chatbot-blocked-state__heading">
                              <span class="chatbot-chip__cue" aria-hidden="true">BL</span>
                              <h2 class="chatbot-section-title">Blocked</h2>
                            </div>
                            <p class="chatbot-body">No authorized candidates are available.</p>
                            <p class="chatbot-body"><strong>Next action:</strong> Review authorized metadata, then defer or escalate.</p>
                          </section>
                          <section class="chatbot-section" aria-labelledby="association-redacted-evidence-title">
                            <h2 id="association-redacted-evidence-title" class="chatbot-section-title">Candidate projects</h2>
                            <span class="chatbot-chip chatbot-chip--evidence"
                                  data-chatbot-evidence-state="Unauthorized"
                                  aria-label="Evidence restricted: candidate metadata. Evidence restricted.">
                              <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                              <span class="chatbot-chip__label">candidate metadata</span>
                              <span class="chatbot-chip__status">Evidence restricted</span>
                            </span>
                          </section>
                          <section class="chatbot-association-actions" aria-labelledby="association-actions-title">
                            <h2 id="association-actions-title" class="chatbot-section-title">Safe next actions</h2>
                            <div class="chatbot-command-bar chatbot-association-actions__bar">
                              <span class="chatbot-association-action-wrap">
                                <span class="chatbot-governed-action"
                                      data-chatbot-critical-action="true"
                                      data-chatbot-action-state="DisabledWithReason"
                                      data-chatbot-touch-target="primary"
                                      data-chatbot-stable-id="association-action-choose-candidate">
                                  <button type="button"
                                          aria-label="Choose candidate"
                                          aria-disabled="true"
                                          aria-describedby="association-action-choose-candidate-disabled-reason">
                                    Choose candidate
                                  </button>
                                  <span id="association-action-choose-candidate-disabled-reason"
                                        class="chatbot-governed-action__reason"
                                        tabindex="0"
                                        aria-label="Why unavailable? Select an authorized candidate before choosing this action.">
                                    <strong>Why unavailable?</strong> Select an authorized candidate before choosing this action.
                                  </span>
                                </span>
                              </span>
                            </div>
                          </section>
            """;

    private static string BuildBusyRegionFixture()
        => """
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Busy region focus fixture</title>
              </head>
              <body>
                <main aria-labelledby="busy-region-title">
                  <h1 id="busy-region-title">Busy region focus contract</h1>
                  <section id="operation-status-region"
                           role="region"
                           aria-label="Operation status summary"
                           aria-busy="false">
                    <button id="refresh-operation-status" type="button">Refresh operation status</button>
                    <div id="operation-status-content">
                      <div role="status" aria-label="Status refresh: idle">Ready to refresh.</div>
                    </div>
                  </section>
                </main>
                <script>
                  window.__refreshCount = 0;
                  document.querySelector("#refresh-operation-status").addEventListener("click", event => {
                    const region = document.querySelector("#operation-status-region");
                    const content = document.querySelector("#operation-status-content");
                    region.setAttribute("aria-busy", "true");
                    content.innerHTML = "<div role='status' aria-label='Status refresh: complete'>Status refresh complete.</div>";
                    region.setAttribute("aria-busy", "false");
                    window.__refreshCount += 1;
                    event.currentTarget.focus();
                  });
                </script>
              </body>
            </html>
            """;

    private static string BuildValidationAssociationFixture()
        => """
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Validation association fixture</title>
              </head>
              <body>
                <main aria-labelledby="validation-title">
                  <h1 id="validation-title">Validation association contract</h1>
                  <form id="approval-review" aria-labelledby="approval-review-title" novalidate>
                    <div id="approval-errors"
                         role="alert"
                         aria-label="Approval validation summary"
                         tabindex="-1"
                         hidden>
                      <p>Choose a safe decision before submitting.</p>
                    </div>
                    <section aria-labelledby="approval-review-title">
                      <h2 id="approval-review-title">Approval review</h2>
                      <label for="approval-rationale">Approval rationale</label>
                      <textarea id="approval-rationale"></textarea>
                      <p id="approval-rationale-message" hidden>Enter a bounded rationale.</p>
                      <label for="approval-decision">Approval decision</label>
                      <select id="approval-decision">
                        <option value="">Choose</option>
                        <option value="defer">Defer</option>
                      </select>
                      <p id="approval-decision-message" hidden>Choose a safe decision before submitting.</p>
                    </section>
                    <button type="submit">Submit approval review</button>
                  </form>
                </main>
                <script>
                  document.querySelector("#approval-review").addEventListener("submit", event => {
                    event.preventDefault();
                    const summary = document.querySelector("#approval-errors");
                    const rationale = document.querySelector("#approval-rationale");
                    const rationaleMessage = document.querySelector("#approval-rationale-message");
                    const decision = document.querySelector("#approval-decision");
                    const decisionMessage = document.querySelector("#approval-decision-message");

                    summary.hidden = false;
                    rationale.setAttribute("aria-invalid", "true");
                    rationale.setAttribute("aria-describedby", "approval-rationale-message");
                    rationaleMessage.hidden = false;
                    decision.setAttribute("aria-invalid", "true");
                    decision.setAttribute("aria-errormessage", "approval-decision-message");
                    decisionMessage.hidden = false;
                    summary.focus();
                  });
                </script>
              </body>
            </html>
            """;

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

    private static void AssertMatrixLiveBehaviorWithoutBrowser()
    {
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        component.ShouldContain("data-chatbot-announcement-key");
        component.ShouldContain("aria-live=\"@AriaLive\"");
        component.ShouldContain("ChatBotStateFeedbackMatrix.For(stateFamily)");

        page.ShouldContain("AnnouncementKey=\"@outcome.OperationId\"");
        page.ShouldContain("ObservedForOthersRejectionOrQueueUpdate");

        fixture.ShouldContain("data-chatbot-feedback-state=\"CurrentUserCommandAcceptedProjectionPending\"");
        fixture.ShouldContain("const projectionKey = \"01ARZ3NDEKTSV4RRFFQ69G5FAX\";");
        fixture.ShouldContain("data-chatbot-announcement-key=\"${projectionKey}\"");
        fixture.ShouldContain("data-chatbot-repeat-rule=\"OncePerStableOperationKey\"");
        fixture.ShouldContain("data-chatbot-live-announced=\"${projectionAnnounced ? \"true\" : \"false\"}\"");
        fixture.ShouldContain("aria-live=\"polite\"");
        fixture.ShouldContain("data-chatbot-feedback-state=\"ObservedForOthersRejectionOrQueueUpdate\"");
        fixture.ShouldContain("data-chatbot-live=\"off\"");
        fixture.ShouldContain("data-chatbot-repeat-rule=\"NoLiveAnnouncement\"");
        fixture.ShouldContain("window.__announcedKeys.add(projectionKey)");
        fixture.ShouldContain("const projectionLive = projectionAnnounced ? \"polite\" : \"off\";");
        fixture.ShouldNotContain("role=\"status\"\n                               aria-live=\"off\"");
    }

    private static void AssertInitialHistoricalContentWithoutBrowser()
    {
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);
        int initialContentEnd = fixture.IndexOf("<script>", StringComparison.Ordinal);
        initialContentEnd.ShouldBeGreaterThan(0);
        string initialContent = fixture[..initialContentEnd];

        initialContent.ShouldContain("Project status: UI origin remains visible");
        initialContent.ShouldNotContain("data-chatbot-feedback-state=\"CurrentUserCommandAcceptedProjectionPending\"");
        initialContent.ShouldNotContain("data-chatbot-feedback-state=\"ObservedForOthersRejectionOrQueueUpdate\"");
        initialContent.ShouldNotContain("Projection status: pending");
        initialContent.ShouldNotContain("Audit status: committed");
        initialContent.ShouldNotContain("Audit history: metadata only");
    }

    private static void AssertReducedMotionWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);

        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain(".chatbot-shimmer");
        css.ShouldContain(".chatbot-skeleton");
        css.ShouldContain(".chatbot-row-motion");
        css.ShouldContain(".chatbot-streaming-text");
        css.ShouldContain(".chatbot-panel-transition");
        css.ShouldContain("animation: none !important;");
        css.ShouldContain("transition-duration: 0.01ms !important;");
        css.ShouldContain("scroll-behavior: auto !important;");
        css.ShouldContain("background-image: none !important;");
        css.ShouldContain("transform: none !important;");
        css.ShouldContain("@media (forced-colors: active)");

        fixture.ShouldContain("data-chatbot-motion-fixture=\"governed-motion\"");
        fixture.ShouldContain("Projection pending");
    }

    private static void AssertBusyRegionFocusPreservationWithoutBrowser()
    {
        string fixture = BuildBusyRegionFixture();

        fixture.ShouldContain("id=\"operation-status-region\"");
        fixture.ShouldContain("role=\"region\"");
        fixture.ShouldContain("aria-label=\"Operation status summary\"");
        fixture.ShouldContain("aria-busy=\"false\"");
        fixture.ShouldContain("region.setAttribute(\"aria-busy\", \"true\")");
        fixture.ShouldContain("region.setAttribute(\"aria-busy\", \"false\")");
        fixture.ShouldContain("event.currentTarget.focus()");
        fixture.ShouldContain("aria-label='Status refresh: complete'");
    }

    private static void AssertValidationAssociationWithoutBrowser()
    {
        string fixture = BuildValidationAssociationFixture();

        fixture.ShouldContain("id=\"approval-errors\"");
        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("aria-label=\"Approval validation summary\"");
        fixture.ShouldContain("tabindex=\"-1\"");
        fixture.ShouldContain("rationale.setAttribute(\"aria-invalid\", \"true\")");
        fixture.ShouldContain("rationale.setAttribute(\"aria-describedby\", \"approval-rationale-message\")");
        fixture.ShouldContain("decision.setAttribute(\"aria-invalid\", \"true\")");
        fixture.ShouldContain("decision.setAttribute(\"aria-errormessage\", \"approval-decision-message\")");
        fixture.ShouldContain("summary.focus()");
    }

    private static void AssertKeyboardLandmarkFocusPathWithoutBrowser()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");
        string routes = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Routes.razor");
        string shell = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending);
        string fixtureBody = fixture[fixture.IndexOf("<body>", StringComparison.Ordinal)..];

        layout.ShouldContain("href=\"#chatbot-main-content\"");
        layout.ShouldContain("id=\"chatbot-main-content\"");
        layout.ShouldContain("tabindex=\"-1\"");
        routes.ShouldContain("Selector=\"h1\"");
        shell.ShouldContain("role=\"complementary\"");
        page.ShouldContain("ComplementaryLabel=\"@UiText[ChatBotUiTextKey.GovernedOperationReviewContext]\"");
        fixture.ShouldContain("aria-label=\"Governed command path\"");
        fixture.ShouldContain("role=\"complementary\"");
        fixture.ShouldContain("aria-label=\"Governed operation review context\"");
        fixture.ShouldContain("aria-label=\"Project status: UI origin remains visible\"");
        fixtureBody.IndexOf("<a class=\"chatbot-skip-link\"", StringComparison.Ordinal).ShouldBeLessThan(fixtureBody.IndexOf("<main id=\"chatbot-main-content\"", StringComparison.Ordinal));
        fixtureBody.IndexOf("<main id=\"chatbot-main-content\"", StringComparison.Ordinal).ShouldBeLessThan(fixtureBody.IndexOf("<section class=\"chatbot-conversation-shell\"", StringComparison.Ordinal));
        fixtureBody.IndexOf("<section class=\"chatbot-conversation-shell\"", StringComparison.Ordinal).ShouldBeLessThan(fixtureBody.IndexOf("aria-label=\"Project context\"", StringComparison.Ordinal));
        fixtureBody.IndexOf("aria-label=\"Project context\"", StringComparison.Ordinal).ShouldBeLessThan(fixtureBody.IndexOf("aria-label=\"Governed command path\"", StringComparison.Ordinal));
        fixtureBody.IndexOf("aria-label=\"Governed command path\"", StringComparison.Ordinal).ShouldBeLessThan(fixtureBody.IndexOf("aria-label=\"Governed operation review context\"", StringComparison.Ordinal));
        fixtureBody.IndexOf("id=\"governed-operations-title\"", StringComparison.Ordinal).ShouldBeGreaterThan(fixtureBody.IndexOf("aria-label=\"Governed command path\"", StringComparison.Ordinal));
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
        fixture.ShouldContain("AcceptedProjectionPending");
        fixture.ShouldNotContain("Done", Case.Insensitive);
        fixture.ShouldNotContain("Completed", Case.Insensitive);
        fixture.ShouldNotContain("tenant-alpha", Case.Insensitive);
        fixture.ShouldNotContain("restricted-file.txt", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("raw exception", Case.Insensitive);
        fixture.ShouldNotContain("/home/", Case.Insensitive);
    }

    private static void AssertBackendFailureWithoutBrowser()
    {
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.SubmitFails);

        fixture.ShouldContain("role=\"status\"");
        fixture.ShouldContain("aria-live=\"polite\"");
        fixture.ShouldContain("data-chatbot-feedback-state=\"RetryableFailure\"");
        fixture.ShouldContain("aria-label=\"Submission status: failed\"");
        fixture.ShouldContain("data-chatbot-status=\"danger\"");
        fixture.ShouldContain("Submission did not complete");
        fixture.ShouldContain("You can try again.");
    }

    private static void AssertRetryFailureDuplicateSafetyWithoutBrowser()
    {
        string fixture = BuildGovernedOperationsFixture(FixtureScenario.RetryFailureMetadata);

        fixture.ShouldContain("aria-label=\"Operation recovery status: retryable failure\"");
        fixture.ShouldContain("data-chatbot-feedback-state=\"RetryableFailure\"");
        fixture.ShouldContain("message-intake");
        fixture.ShouldContain("graph_throttled");
        fixture.ShouldContain("2 of 5");
        fixture.ShouldContain("retry-later");
        fixture.ShouldContain("mailbox-operator");
        fixture.ShouldContain("duplicate-provider-message-suppressed");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("Why unavailable? Retry waits for the next policy window.");
        fixture.ShouldNotContain("sender@example.test", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
        fixture.ShouldNotContain("raw exception", Case.Insensitive);
    }

    private static void AssertOperationalQueueManagementWithoutBrowser()
    {
        string fixture = BuildOperationalQueueManagementFixture();
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        fixture.ShouldContain("data-chatbot-operational-queue=\"true\"");
        fixture.ShouldContain("data-chatbot-loading-mode=\"Pagination\"");
        fixture.ShouldNotContain("InfiniteScroll", Case.Insensitive);
        fixture.ShouldContain("age&gt;0 risk:any confidence:any project:any mailbox:any failure-state:any assigned:any next-action:any");
        fixture.ShouldContain("priority desc, item-ref asc, source-version asc");
        fixture.ShouldContain("page-size:100");
        fixture.ShouldContain("metadata_only");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("requires project authority or escalation");

        foreach (string family in OperationalQueueFamilyTokens)
        {
            fixture.ShouldContain($"data-queue-tab=\"{family}\"");
            fixture.ShouldContain($"item:{family}-001");
            fixture.ShouldContain($"correlation:queue-{family}");
        }

        page.ShouldContain("data-chatbot-operational-queue=\"true\"");
        page.ShouldContain("ChatBotQueueLoadingMode.Pagination");
        page.ShouldNotContain("ChatBotQueueLoadingMode.InfiniteScroll");
        page.ShouldContain("GovernedOperationsQueuePrimaryAction");
        page.ShouldContain("GovernedOperationsQueueSecondaryActions");
        page.ShouldContain("GovernedOperationsQueueDetailUnavailable");
        page.ShouldContain("page-size:100");
        page.ShouldContain("data-chatbot-source-version");

        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("bearer", Case.Insensitive);
    }

    private static void AssertOperationalQueueManagementResponsiveWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string fixture = BuildOperationalQueueManagementFixture();

        css.ShouldContain("@media (max-width: 599px)");
        css.ShouldContain("overflow-wrap: anywhere;");
        css.ShouldContain(".chatbot-definition-list");
        css.ShouldContain(".chatbot-labelled-row");
        fixture.ShouldContain("data-chatbot-responsive-fixture=\"operational-queue-management\"");
        fixture.ShouldContain("item:failed-ingestion-001");
        fixture.ShouldContain("correlation:queue-failed-ingestion");
        fixture.ShouldContain("tenant:tenant-alpha");
        fixture.ShouldContain("mailbox:operations");
        fixture.ShouldContain("workflow:failed-ingestion-001");
        fixture.ShouldContain("reasonId: \"detail-reason-failed-ingestion\"");
        fixture.ShouldContain("window.__detailOpenCount = 0");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
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

        foreach (string actorCategory in new[]
        {
            "HumanUser",
            "ExternalParty",
            "ServiceClient",
            "AiActor",
            "BackgroundWorker",
            "Cli",
            "Mcp",
            "MailboxEvent",
        })
        {
            fixture.ShouldContain($"data-chatbot-actor-category=\"{actorCategory}\"");
        }

        fixture.ShouldContain("aria-label=\"Human user actor: Jerome\"");
        fixture.ShouldContain("aria-label=\"External party actor: External participant\"");
        fixture.ShouldContain("aria-label=\"Service client actor: Graph connector\"");
        fixture.ShouldContain("aria-label=\"AI actor: Copilot planner\"");
        fixture.ShouldContain("aria-label=\"Background worker actor: Intake worker\"");
        fixture.ShouldContain("aria-label=\"CLI actor: chatbot-cli\"");
        fixture.ShouldContain("aria-label=\"MCP actor: Unresolved actor\"");
        fixture.ShouldContain("aria-label=\"Mailbox event actor: Shared mailbox event\"");
        fixture.ShouldContain("aria-label=\"Resolve MCP actor: Unresolved actor\"");
        fixture.ShouldContain("Resolve actor");
        fixture.ShouldContain("type=\"button\"");
        fixture.ShouldContain("aria-disabled=\"false\"");
        foreach (string evidenceState in new[] { "Available", "Unavailable", "Redacted", "Unauthorized" })
        {
            fixture.ShouldContain($"data-chatbot-evidence-state=\"{evidenceState}\"");
        }

        fixture.ShouldContain("Evidence is redacted by policy.");
        fixture.ShouldContain("Evidence store is unavailable.");
        fixture.ShouldContain("Authorization required.");
        foreach (string riskClass in new[]
        {
            "ExternallyVisible",
            "FileExposing",
            "ProjectMutating",
            "ToolInvoking",
            "TaskCreating",
            "ParticipantRepresenting",
        })
        {
            fixture.ShouldContain($"data-chatbot-risk-class=\"{riskClass}\"");
        }

        fixture.ShouldContain("role=\"status\"");
        fixture.ShouldContain("Info status: Command accepted; projection is pending.");
        fixture.ShouldContain("Warning status: Dependency degraded; retry remains available.");
        fixture.ShouldContain("Danger status: Validation failed for the current user.");
        fixture.ShouldContain("Success status: Audit metadata committed.");
        fixture.ShouldContain("Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool.");
        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("Next action: Choose a lower-risk action.");
        fixture.ShouldNotContain("restricted-file.txt", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);

        evidence.ShouldContain("@onclick=\"ActivateAsync\"");
        evidence.ShouldNotContain("@onkeydown");
        blocked.ShouldContain("ChatBotStateFeedbackMatrix.For(FeedbackState)");
        blocked.ShouldContain("role=\"@FeedbackContract.AriaRole\"");
        blocked.ShouldContain("aria-live=\"@FeedbackContract.AriaLive\"");
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
        component.ShouldContain("ChatBotUiTextKey.StopResponseAnnouncement");
        component.ShouldContain("FocusReturnTargetId");
        component.ShouldContain("role=\"status\"");
        component.ShouldContain("aria-live=\"polite\"");
        component.ShouldContain("LiveRegionMessage = string.Empty");
        component.ShouldContain("HexalithChatBot.focusElementById");
        focusScript.ShouldContain("document.getElementById");
    }

    private static void AssertLocalizationFoundationWithoutBrowser()
    {
        string english = BuildLocalizedGovernedOperationsFixture("en");
        string french = BuildLocalizedGovernedOperationsFixture("fr");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        english.ShouldContain("Governed operations");
        english.ShouldContain("Record governed note");
        french.ShouldContain("Opérations gouvernées");
        french.ShouldContain("Enregistrer la note gouvernée");
        french.ShouldContain("AcceptedProjectionPending");
        french.ShouldContain("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        french.ShouldContain("origin:Ui");
        page.ShouldContain("ChatBotUiTextKey.RecordGovernedNote");
        page.ShouldContain("ChatBotUiTextKey.ProjectionPendingAccessible");
        page.ShouldContain("ChatBotUiTextKey.AuditStatusCommittedAccessible");
    }

    private static void AssertFrenchCriticalLabelsWithoutBrowser()
    {
        string fixture = BuildFrenchCriticalLabelFixture();

        fixture.ShouldContain("data-chatbot-critical-localized=\"true\"");
        fixture.ShouldContain("Acteur utilisateur humain : Jerome");
        fixture.ShouldContain("Risque : Invoque un outil");
        fixture.ShouldContain("Statut de projection : en attente");
        fixture.ShouldContain("Confiance : 88 %");
        fixture.ShouldContain("Prochaine action :");
        fixture.ShouldContain("Raison de récupération sûre :");
        fixture.ShouldContain("Choisir une action à risque plus faible.");
        fixture.ShouldContain("La revue de quarantaine doit être terminée avant une nouvelle tentative.");
    }

    private static void AssertRedactionRecoveryCognitiveLoadWithoutBrowser()
    {
        string english = BuildRedactionRecoveryCognitiveLoadFixture("en");
        string french = BuildRedactionRecoveryCognitiveLoadFixture("fr");

        english.ShouldContain("This export is redacted; full detail requires escalation.");
        english.ShouldContain("Filter: Pending review. 2 results.");
        english.ShouldContain("data-chatbot-action-kind=\"primary\"");
        english.ShouldContain("data-chatbot-action-kind=\"secondary\"");
        english.ShouldContain("data-chatbot-action-kind=\"destructive\"");
        english.ShouldContain("data-chatbot-canonical-field-order=\"evidence,risk,status,actor,timestamp\"");
        english.ShouldContain("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        english.ShouldContain("audit:Committed");
        english.ShouldNotContain("restricted-file.txt", Case.Insensitive);
        english.ShouldNotContain("Secret Project", Case.Insensitive);
        english.ShouldNotContain("raw exception", Case.Insensitive);

        french.ShouldContain("Cette exportation est masquée ; le détail complet nécessite une escalade.");
        french.ShouldContain("Filtre : Revue en attente. 2 résultats.");
        french.ShouldContain("Réessayez uniquement quand la copie de sûreté anti-doublon reste visible.");
        french.ShouldContain("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        french.ShouldContain("audit:Committed");
    }

    private static void AssertAssociationReviewSelectionWithoutBrowser()
    {
        string fixture = BuildAssociationReviewFixture(AssociationReviewFixtureScenario.Candidates);
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string row = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor");
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string comparison = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor");

        page.ShouldContain("data-chatbot-responsive-fixture=\"association-review\"");
        row.ShouldContain("role=\"radio\"");
        row.ShouldContain("aria-checked=\"@IsSelectedText\"");
        row.ShouldContain("ChatBotEvidenceChip");
        actions.ShouldContain("aria-label=\"@UiText[ChatBotUiTextKey.AssociationReviewDecisionNote]\"");
        actions.ShouldContain("projection-pending");
        actions.ShouldContain("ChatBotGovernedAction");
        comparison.ShouldContain("data-chatbot-association-comparison=\"true\"");
        comparison.ShouldContain("Candidate.DisplayLabel");

        fixture.ShouldContain("role=\"radiogroup\"");
        fixture.ShouldContain("aria-label=\"Candidate 1. Confidence 72%. Authorized candidate A\"");
        fixture.ShouldContain("aria-checked=\"false\"");
        fixture.ShouldContain("association-comparison-panel");
        fixture.ShouldContain("thread-reference AUTH-100");
        fixture.ShouldContain("Evidence redacted");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("aria-describedby=\"association-action-choose-candidate-disabled-reason\"");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("Projection is still updating.");
    }

    private static void AssertAssociationDecisionSubmitWithoutBrowser()
    {
        string fixture = BuildAssociationDecisionSubmitFixture(conflict: false);
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs");
        string effects = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs");
        string reducers = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs");

        service.ShouldContain(".SubmitAsync(command, review.CorrelationId, origin: ChatBotSurfaceOrigin.Ui");
        service.ShouldContain("GetAssociationReviewAsync(review.AssociationId, cancellationToken)");
        service.ShouldContain("new ContractAssociateEmailToProject");
        service.ShouldContain("DecisionEvidenceFingerprint");
        effects.ShouldContain("AssociationDecisionSubmittedAction(result)");
        reducers.ShouldContain("Review = action.Result.Review");

        fixture.ShouldContain("commandType: \"AssociateEmailToProject\"");
        fixture.ShouldContain("origin: \"ui\"");
        fixture.ShouldContain("decisionKind: \"associate\"");
        fixture.ShouldContain("candidateEvidenceFingerprint: selected?.dataset.evidenceFingerprint");
        fixture.ShouldContain("Association decision accepted: projection pending");
        fixture.ShouldContain("Audit status: reconciling");
        fixture.ShouldContain("Decision already recorded.");
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
    }

    private static void AssertAssociationDecisionConflictWithoutBrowser()
    {
        string fixture = BuildAssociationDecisionSubmitFixture(conflict: true);
        string effects = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs");
        string messageCodes = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs");

        effects.ShouldContain("AssociationDecisionSubmitFailedAction(SafeFailureCode(problem.Result?.Code))");
        messageCodes.ShouldContain("idempotency_conflict_association_decision");
        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("Submission failed: idempotency_conflict_association_decision");
        fixture.ShouldContain("already decided");
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
    }

    private static void AssertAssociationCorrectionSubmitWithoutBrowser()
    {
        string fixture = BuildAssociationCorrectionSubmitFixture(conflict: false, blocked: false);
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs");
        string effects = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs");
        string reducers = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs");

        service.ShouldContain(".SubmitAsync(command, review.CorrelationId, origin: ChatBotSurfaceOrigin.Ui");
        service.ShouldContain("new ContractCorrectEmailProjectAssociation");
        service.ShouldContain("CorrectionEvidenceFingerprint");
        effects.ShouldContain("AssociationCorrectionSubmittedAction(result)");
        reducers.ShouldContain("ReduceCorrectionSubmitted");

        fixture.ShouldContain("commandType: \"CorrectEmailProjectAssociation\"");
        fixture.ShouldContain("origin: \"ui\"");
        fixture.ShouldContain("correctionKind: \"project-reassignment\"");
        fixture.ShouldContain("candidateEvidenceFingerprint: selected?.dataset.evidenceFingerprint");
        fixture.ShouldContain("Association correction accepted: downstream preview only");
        fixture.ShouldContain("Correction status: partial");
        fixture.ShouldContain("preview-only");
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
    }

    private static void AssertAssociationCorrectionBlockedWithoutBrowser()
    {
        string fixture = BuildAssociationCorrectionSubmitFixture(conflict: false, blocked: true);
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");

        actions.ShouldContain("projection-invalidation-unavailable");
        actions.ShouldContain("AssociationReviewCorrectionProjectionUnavailable");
        fixture.ShouldContain("aria-disabled=\"true\"");
        fixture.ShouldContain("aria-describedby=\"association-correction-submit-disabled-reason\"");
        fixture.ShouldContain("tabindex=\"0\"");
        fixture.ShouldContain("Projection invalidation is unavailable, so correction is blocked.");
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("raw exception", Case.Insensitive);
    }

    private static void AssertAssociationCorrectionConflictWithoutBrowser()
    {
        string fixture = BuildAssociationCorrectionSubmitFixture(conflict: true, blocked: false);
        string effects = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs");
        string messageCodes = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs");

        effects.ShouldContain("AssociationCorrectionSubmitFailedAction(SafeFailureCode(problem.Result?.Code))");
        messageCodes.ShouldContain("idempotency_conflict_correction");
        fixture.ShouldContain("role=\"alert\"");
        fixture.ShouldContain("Correction failed: idempotency_conflict_correction");
        fixture.ShouldContain("already been corrected");
        fixture.ShouldNotContain("raw provider payload", Case.Insensitive);
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
    }

    private static void AssertAssociationCorrectionPropagationWithoutBrowser()
    {
        string pending = BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario.Pending);
        string delayed = BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario.Delayed);
        string complete = BuildAssociationCorrectionPropagationFixture(CorrectionPropagationFixtureScenario.Complete);
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string model = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs");
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs");
        string messageCodes = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs");

        actions.ShouldContain("PropagationProgressDenominator is > 0");
        actions.ShouldContain("AssociationReviewCorrectionPropagationProgressTemplate");
        actions.ShouldContain("AssociationReviewCorrectionContextBlocked");
        actions.ShouldContain("AssociationReviewCorrectionSafeNextActionWait");
        actions.ShouldContain("AssociationReviewCorrectionSafeNextActionEscalate");
        page.ShouldContain("PropagationStatus=\"@review.PropagationStatus\"");
        page.ShouldContain("IsCorrectedContextStale=\"@review.IsCorrectedContextStale\"");
        model.ShouldContain("public bool IsPropagationBlocking");
        service.ShouldContain("status.PropagationProgressNumerator");
        service.ShouldContain("status.IsCorrectedContextStale");
        messageCodes.ShouldContain("association_ai_context_blocked");
        messageCodes.ShouldContain("association_correction_propagation_delayed");

        pending.ShouldContain("Correction propagation status: Correcting");
        pending.ShouldContain("2 of 4 stores acknowledged");
        pending.ShouldContain("Corrected context is not ready for AI actions or command preparation.");
        delayed.ShouldContain("Correction propagation status: Correction-delayed");
        delayed.ShouldContain("Correction propagation is delayed and operations has been alerted.");
        delayed.ShouldContain("workflow-correction-001");
        complete.ShouldContain("Correction propagation status: complete");
        complete.ShouldContain("4 of 4 stores acknowledged");
        complete.ShouldContain("Corrected project context is ready for command preparation.");

        pending.ShouldNotContain("restricted@example.com", Case.Insensitive);
        pending.ShouldNotContain("raw provider payload", Case.Insensitive);
        pending.ShouldNotContain("Secret Project", Case.Insensitive);
        pending.ShouldNotContain("raw exception", Case.Insensitive);
        delayed.ShouldNotContain("restricted@example.com", Case.Insensitive);
        delayed.ShouldNotContain("raw provider payload", Case.Insensitive);
        delayed.ShouldNotContain("Secret Project", Case.Insensitive);
        delayed.ShouldNotContain("raw exception", Case.Insensitive);
    }

    private static void AssertAssociationReviewResponsiveWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string fixture = BuildAssociationReviewFixture(AssociationReviewFixtureScenario.Candidates);

        css.ShouldContain(".chatbot-association-review");
        css.ShouldContain(".chatbot-association-candidate-list");
        css.ShouldContain(".chatbot-association-comparison");
        css.ShouldContain(".chatbot-association-actions");
        css.ShouldContain("@media (max-width: 48rem)");
        css.ShouldContain("min-width: 0;");
        css.ShouldContain("grid-column: 1 / -1;");

        fixture.ShouldContain("data-chatbot-responsive-fixture=\"association-review\"");
        fixture.ShouldContain("Candidate projects");
        fixture.ShouldContain("Evidence comparison");
        fixture.ShouldContain("Source metadata");
        fixture.ShouldContain("safe-next-action, projection-pending");
        fixture.ShouldNotContain("Secret Project", Case.Insensitive);
        fixture.ShouldNotContain("restricted@example.com", Case.Insensitive);
        fixture.ShouldNotContain("raw exception", Case.Insensitive);
        fixture.ShouldNotContain("full email thread", Case.Insensitive);
    }

    private static void AssertAssociationReviewForcedColorsAndBlockedWithoutBrowser()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string candidates = BuildAssociationReviewFixture(AssociationReviewFixtureScenario.Candidates);
        string blocked = BuildAssociationReviewFixture(AssociationReviewFixtureScenario.BlockedRedacted);
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");

        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain(".chatbot-association-candidate");
        css.ShouldContain(".chatbot-association-comparison__panel");
        css.ShouldContain("transform: none !important;");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("CanvasText");
        css.ShouldContain("Highlight");
        css.ShouldContain(".chatbot-association-candidate:focus");

        candidates.ShouldContain("chatbot-row-motion chatbot-panel-transition");
        candidates.ShouldContain("Evidence redacted");

        blocked.ShouldContain("role=\"alert\"");
        blocked.ShouldContain("No authorized candidates are available.");
        blocked.ShouldContain("Evidence restricted");
        blocked.ShouldContain("aria-label=\"Evidence restricted: candidate metadata. Evidence restricted.\"");
        blocked.ShouldNotContain("Secret Project", Case.Insensitive);
        blocked.ShouldNotContain("restricted@example.com", Case.Insensitive);
        blocked.ShouldNotContain("raw exception", Case.Insensitive);
        blocked.ShouldNotContain("full email thread", Case.Insensitive);

        page.ShouldContain("ChatBotBlockedReason.UnresolvedAssociation");
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
        ProjectionPendingRendered,
        SubmitFails,
        RetryFailureMetadata,
    }

    private enum AssociationReviewFixtureScenario
    {
        Candidates,
        BlockedRedacted,
    }

    private enum CorrectionPropagationFixtureScenario
    {
        Pending,
        Delayed,
        Complete,
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
