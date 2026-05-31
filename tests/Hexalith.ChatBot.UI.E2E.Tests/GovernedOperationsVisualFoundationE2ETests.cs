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
            await WaitForVisibleAsync(harness.Page.GetByText("Info", new() { Exact = true }));
            await WaitForVisibleAsync(harness.Page.GetByText("post-commit", new() { Exact = false }));
            await WaitForVisibleAsync(harness.Page.GetByText("metadata-only", new() { Exact = false }));
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
            int matchingAnnouncements = await harness.Page.Locator("[data-chatbot-announcement-key='01ARZ3NDEKTSV4RRFFQ69G5FAX']").CountAsync();
            matchingAnnouncements.ShouldBe(1);
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
            string transform = await motionFixture.EvaluateAsync<string>("element => getComputedStyle(element).transform");
            animationName.ShouldBe("none");
            transform.ShouldBe("none");
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
                await harness.Page.SetContentAsync(BuildGovernedOperationsFixture(FixtureScenario.ProjectionPending));
                await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Record governed note" }).ClickAsync();

                await WaitForVisibleAsync(harness.Page.GetByText("Governed operations", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Operation", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Command", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Lifecycle state", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Completion status", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Audit status", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByText("Safe next actions", new() { Exact = true }));
                await WaitForVisibleAsync(harness.Page.GetByLabel("Audit history: metadata only"));

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
                await WaitForVisibleAsync(harness.Page.GetByText("AcceptedProjectionPending", new() { Exact = true }));
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
                    harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Resolve actor" }),
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

            await WaitForVisibleAsync(harness.Page.GetByLabel("Human user actor: Jerome"));
            await WaitForVisibleAsync(harness.Page.GetByLabel("MCP actor: Unresolved actor"));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Resolve actor" }));

            ILocator evidenceButton = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Available evidence: Audit correlation record" });
            await WaitForVisibleAsync(evidenceButton);
            await evidenceButton.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            await harness.Page.Keyboard.PressAsync("Space");
            int activationCount = await harness.Page.EvaluateAsync<int>("() => window.__evidenceOpenCount");
            activationCount.ShouldBe(2);

            ILocator redactedEvidence = harness.Page.GetByLabel("Evidence redacted: Supporting file. Evidence is redacted by policy.");
            await WaitForVisibleAsync(redactedEvidence);
            await WaitForVisibleAsync(harness.Page.GetByText("Evidence is redacted by policy.", new() { Exact = true }));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool." }));
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
            await WaitForVisibleAsync(harness.Page.GetByText("Choose a safe decision before submitting.", new() { Exact = true }));
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

            ILocator announcement = harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Response stopped" });
            await WaitForVisibleAsync(announcement);
            (await announcement.GetAttributeAsync("aria-live")).ShouldBe("polite");
            (await announcement.TextContentAsync()).ShouldBe("Response stopped");
            (await harness.Page.GetByRole(AriaRole.Status, new() { NameString = "Response stopped" }).CountAsync()).ShouldBe(1);
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("composer-target");

            ILocator idleStopRegion = harness.Page.Locator("[data-chatbot-stable-id='streaming-stop-idle']");
            (await idleStopRegion.GetAttributeAsync("data-chatbot-streaming")).ShouldBe("false");
            (await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Stop idle response generation" }).CountAsync()).ShouldBe(0);
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("composer-target");
        }
    }

    private static async Task<string> CssVariableAsync(IPage page, string name)
        => await page.EvaluateAsync<string>(
                "token => getComputedStyle(document.documentElement).getPropertyValue(token).trim()",
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
                            <div id="fixture-status-root"></div>
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

                    root.innerHTML = `
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
                          data-chatbot-actor-category="Mcp"
                          aria-label="MCP actor: Unresolved actor">
                      <span class="chatbot-actor-badge__icon" aria-hidden="true">MP</span>
                      <span class="chatbot-actor-badge__category">MCP</span>
                      <span class="chatbot-actor-badge__label">Unresolved actor</span>
                      <button class="chatbot-actor-badge__action" type="button">Resolve actor</button>
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
                          aria-label="Evidence redacted: Supporting file. Evidence is redacted by policy.">
                      <span class="chatbot-chip__cue" aria-hidden="true">EV</span>
                      <span class="chatbot-chip__label">Supporting file</span>
                      <span class="chatbot-chip__status">Evidence redacted</span>
                    </span>
                    <span class="chatbot-chip__reason">Evidence is redacted by policy.</span>
                    <span class="chatbot-chip chatbot-chip--risk"
                          data-chatbot-status="warning"
                          data-chatbot-risk-class="ToolInvoking"
                          role="status"
                          aria-label="Risk: Tool-invoking. Policy reason: Requires approval before invoking an external tool.">
                      <span class="chatbot-chip__cue" aria-hidden="true">RK</span>
                      <span class="chatbot-chip__label">Tool-invoking</span>
                      <span class="chatbot-chip__status">Requires approval before invoking an external tool.</span>
                    </span>
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
        fixture.ShouldContain("data-chatbot-announcement-key=\"01ARZ3NDEKTSV4RRFFQ69G5FAX\"");
        fixture.ShouldContain("data-chatbot-repeat-rule=\"OncePerStableOperationKey\"");
        fixture.ShouldContain("aria-live=\"polite\"");
        fixture.ShouldContain("data-chatbot-feedback-state=\"ObservedForOthersRejectionOrQueueUpdate\"");
        fixture.ShouldContain("data-chatbot-live=\"off\"");
        fixture.ShouldContain("data-chatbot-repeat-rule=\"NoLiveAnnouncement\"");
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

        fixture.ShouldContain("aria-label=\"Human user actor: Jerome\"");
        fixture.ShouldContain("aria-label=\"MCP actor: Unresolved actor\"");
        fixture.ShouldContain("Resolve actor");
        fixture.ShouldContain("type=\"button\"");
        fixture.ShouldContain("aria-disabled=\"false\"");
        fixture.ShouldContain("Evidence is redacted by policy.");
        fixture.ShouldContain("role=\"status\"");
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
        component.ShouldContain("Response stopped");
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
        SubmitFails,
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
