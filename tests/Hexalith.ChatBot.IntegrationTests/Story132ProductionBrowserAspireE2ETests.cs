using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Acceptance;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.IntegrationTests.Recovery;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

using Shouldly;

using Xunit;

namespace Hexalith.ChatBot.IntegrationTests;

[Trait("Category", "E2E")]
public sealed class Story132ProductionBrowserAspireE2ETests(ITestOutputHelper output)
{
    private const string ChatBotResourceName = "chatbot";
    private const string ChatBotUiResourceName = "chatbot-ui";
    private const string ProjectId = "project-alpha";
    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(90);

    private static readonly BrowserMatrixRow[] BrowserMatrix =
        (from culture in new[] { "en", "fr" }
         from viewport in new[] { (Width: 1280, Height: 720), (Width: 768, Height: 1024), (Width: 390, Height: 844) }
         from reducedMotion in new[] { false, true }
         from forcedColors in new[] { false, true }
         select new BrowserMatrixRow(culture, viewport.Width, viewport.Height, reducedMotion, forcedColors)).ToArray();

    [Fact]
    public void AcceptanceFixtureShouldFailProductionAndReplaceOnlyGuardedProvidersInDevelopment()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [Story132AcceptanceServiceCollectionExtensions.EnabledConfigurationKey] = "true",
            })
            .Build();

        ServiceCollection production = BaseAcceptanceServices();
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            production.AddStory132AcceptanceFixture(configuration, new StubHostEnvironment(Environments.Production)));
        exception.Message.ShouldContain(Story132AcceptanceServiceCollectionExtensions.EnabledConfigurationKey);
        exception.Message.ShouldContain(Environments.Production);

        ServiceCollection development = BaseAcceptanceServices();
        development.AddStory132AcceptanceFixture(configuration, new StubHostEnvironment(Environments.Development));
        development.Count(static descriptor => descriptor.ServiceType == typeof(ITenantAiPolicySnapshotProvider)).ShouldBe(1);
        development.Single(static descriptor => descriptor.ServiceType == typeof(ITenantAiPolicySnapshotProvider))
            .ImplementationType.ShouldBe(typeof(Story132AcceptanceTenantAiPolicySnapshotProvider));
        development.Count(static descriptor => descriptor.ServiceType == typeof(IAiAssistanceProvider)).ShouldBe(1);
        development.Single(static descriptor => descriptor.ServiceType == typeof(IAiAssistanceProvider))
            .ImplementationType.ShouldBe(typeof(Story132AcceptanceAiAssistanceProvider));
        development.Count(static descriptor => descriptor.ServiceType == typeof(IRiskClassifier)).ShouldBe(1);
        development.Single(static descriptor => descriptor.ServiceType == typeof(IRiskClassifier))
            .ImplementationType.ShouldBe(typeof(Story132AcceptanceRiskClassifier));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("audit_unavailable", true)]
    public void ComposerBlockedPresentationShouldOnlyBeARejectionWithAnExplicitSubmissionError(
        string? submissionError,
        bool expected)
        => IsSubmissionFailureMarker(submissionError).ShouldBe(expected);

    [Fact]
    public async Task AuthenticatedProductionClientShouldMessageAskAndStopAcrossRequiredChromeMatrix()
    {
        TrivialGovernedCommandAspireE2eTests.RequireTier3Runtime(
            "Story 13.2 production-browser acceptance requires HEXALITH_CHATBOT_TIER3=1, Docker, DAPR, and Chrome.");
        string chromeExecutable = RequireChromeExecutable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DistributedApplication app = await TrivialGovernedCommandAspireE2eTests.StartTestingApplicationAsync(
            output,
            cancellationToken,
            ConfigureStory132AcceptanceOnChatBotOnly).ConfigureAwait(true);
        Story132CoordinatorLogProbe coordinatorLogProbe = await Story132CoordinatorLogProbe.StartAsync(
            app.Services.GetRequiredService<ResourceLoggerService>().WatchAsync(ChatBotResourceName),
            cancellationToken).ConfigureAwait(true);
        await using ConfiguredAsyncDisposable coordinatorLogProbeScope = coordinatorLogProbe.ConfigureAwait(true);

        try
        {
            await WaitForStory132TopologyAsync(app, cancellationToken).ConfigureAwait(true);
            using HttpClient chatBot = app.CreateHttpClient(ChatBotResourceName);
            chatBot.Timeout = TimeSpan.FromSeconds(30);
            using HttpClient eventStore = app.CreateHttpClient("eventstore", "http");
            eventStore.Timeout = TimeSpan.FromSeconds(15);
            using HttpClient tenants = app.CreateHttpClient("tenants", "http");
            tenants.Timeout = TimeSpan.FromSeconds(15);
            await TrivialGovernedCommandAspireE2eTests.WaitForListenerAsync(eventStore, cancellationToken).ConfigureAwait(true);
            await RecoveryWriterProtocolProvisioner
                .ActivateAsync(app, RecoveryRepositoryCommitResolver.Resolve(), cancellationToken)
                .ConfigureAwait(true);
            await TrivialGovernedCommandAspireE2eTests.WaitForReadyAsync(eventStore, "EventStore", cancellationToken).ConfigureAwait(true);
            await TrivialGovernedCommandAspireE2eTests.WaitForListenerAsync(tenants, cancellationToken).ConfigureAwait(true);
            await TrivialGovernedCommandAspireE2eTests.WaitForChatBotListeningAsync(chatBot, cancellationToken).ConfigureAwait(true);
            await TrivialGovernedCommandAspireE2eTests.WaitForReadyAsync(chatBot, "ChatBot", cancellationToken).ConfigureAwait(true);
            string accessToken = await TrivialGovernedCommandAspireE2eTests
                .AcquireTenantBoundAccessTokenAsync(app, cancellationToken).ConfigureAwait(true);
            AssertProjectOwnerClaim(accessToken);
            output.WriteLine("STORY132_AUTHORIZATION_CLAIM chatbot:project-owner=project-alpha verified");
            await TrivialGovernedCommandAspireE2eTests.WaitForChatBotDaprSidecarAsync(
                chatBot,
                accessToken,
                ChatBotCommandId.New().ToString(),
                cancellationToken).ConfigureAwait(true);
            Uri uiEndpoint = app.GetEndpoint(ChatBotUiResourceName, "http");
            await WaitForUiListeningAsync(uiEndpoint, cancellationToken).ConfigureAwait(true);

            using IPlaywright playwright = await Playwright.CreateAsync().ConfigureAwait(true);
            IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                ExecutablePath = chromeExecutable,
                Args = ["--no-sandbox", "--disable-dev-shm-usage"],
            }).ConfigureAwait(true);
            await using ConfiguredAsyncDisposable browserScope = browser.ConfigureAwait(true);
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                // Aspire's local Keycloak endpoint uses the development certificate. Trust only inside this
                // ephemeral browser context; production OIDC authority and UI configuration remain unchanged.
                IgnoreHTTPSErrors = true,
            }).ConfigureAwait(true);
            await using ConfiguredAsyncDisposable contextScope = context.ConfigureAwait(true);
            await context.RouteAsync(
                "**/favicon.ico",
                static route => route.FulfillAsync(new RouteFulfillOptions { Status = 204 })).ConfigureAwait(true);
            IPage page = await context.NewPageAsync().ConfigureAwait(true);
            ConcurrentQueue<string> browserErrors = [];
            page.PageError += (_, error) => browserErrors.Enqueue($"pageerror: {error}");
            page.Console += (_, message) =>
            {
                if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
                {
                    browserErrors.Enqueue($"console: {message.Text} @ {message.Location}");
                }
            };

            await AuthenticateActorAlphaAsync(page, uiEndpoint).ConfigureAwait(true);
            AssertNoBrowserErrors(browserErrors);

            try
            {
                await SubmitComposerAsync(page, "Message", "Story 13.2 production route message").ConfigureAwait(true);
            }
            catch
            {
                ProjectConversationResponse diagnostic = await ReadConversationAsync(
                    chatBot,
                    accessToken,
                    cancellationToken).ConfigureAwait(true);
                output.WriteLine(
                    $"STORY132_MESSAGE_PROJECTION_DIAGNOSTIC status={diagnostic.Status} "
                    + $"safeNextAction={diagnostic.SafeNextAction ?? "<null>"} items={diagnostic.Items.Count}");
                foreach (ProjectConversationItem item in diagnostic.Items)
                {
                    output.WriteLine(
                        $"STORY132_MESSAGE_ITEM kind={item.Kind} lifecycle={item.LifecycleState} sourceVersion={item.SourceVersion} "
                        + $"failureReason={item.FailureReasonCode ?? "<null>"} blockedReason={item.BlockedReason ?? "<null>"} "
                        + $"auditStatus={item.AuditStatus ?? "<null>"} safeNextAction={item.SafeNextAction ?? "<null>"}");
                }

                throw;
            }
            try
            {
                await WaitForAsync(
                    async () => await page.Locator(".chatbot-conversation-stream__entry").CountAsync().ConfigureAwait(true) >= 1,
                    "the production Message command projection",
                    cancellationToken).ConfigureAwait(true);
            }
            catch
            {
                ILocator composer = page.Locator(".chatbot-governed-composer");
                ILocator retryableCode = page.Locator(
                    "[data-chatbot-stable-id='project-conversation-retryable'] code");
                ILocator conversationCode = page.Locator(
                    "[data-chatbot-stable-id='project-conversation-status'] code");
                string correlationId = await AttributeOrAbsentAsync(
                    composer,
                    "data-chatbot-pending-correlation-id").ConfigureAwait(true);
                ProjectConversationResponse diagnostic = await ReadConversationAsync(
                    chatBot,
                    accessToken,
                    cancellationToken).ConfigureAwait(true);
                output.WriteLine(
                    $"STORY132_MESSAGE_POLL_DIAGNOSTIC uiError={await TextOrAbsentAsync(retryableCode).ConfigureAwait(true)} "
                    + $"uiStatus={await TextOrAbsentAsync(conversationCode).ConfigureAwait(true)} correlation={correlationId} "
                    + $"expectedItem={UserMessageProjectionIdentity(correlationId)}");
                foreach (ProjectConversationItem item in diagnostic.Items)
                {
                    output.WriteLine(
                        $"STORY132_MESSAGE_POLL_ITEM itemId={item.ItemId} lifecycle={item.LifecycleState} "
                        + $"sourceVersion={item.SourceVersion} auditStatus={item.AuditStatus ?? "<null>"}");
                }

                throw;
            }
            await AssertHealthyCurrentStatusAsync(page).ConfigureAwait(true);

            await SubmitComposerAsync(page, "Ask AI", "Summarize the visible governed context").ConfigureAwait(true);
            ILocator proposal = page.Locator(".chatbot-ai-outcome-conversation-item").Last;
            await proposal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);

            ProjectConversationResponse beforeExecution = await ReadConversationAsync(
                chatBot,
                accessToken,
                cancellationToken).ConfigureAwait(true);
            ProjectConversationItem proposalItem = beforeExecution.Items
                .Where(static item => item.AiOutcomeKind is AiOutcomeKind.Proposal)
                .OrderByDescending(static item => item.SourceVersion)
                .First();
            proposalItem.AiRiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
            proposalItem.AiSafeNextAction.ShouldBe("review-ai-action");
            (await proposal.InnerTextAsync().ConfigureAwait(true)).ShouldContain("approval", Case.Insensitive);
            AssertNoBrowserErrors(browserErrors);

            long expectedProposalSourceVersion = beforeExecution.Items.Max(static item => item.SourceVersion);
            string executionId = ChatBotCommandId.New().ToString();
            string executionCorrelationId = ChatBotCommandId.New().ToString();
            await SubmitLowRiskExecutionAsync(
                chatBot,
                accessToken,
                proposalItem,
                expectedProposalSourceVersion,
                executionId,
                executionCorrelationId,
                cancellationToken).ConfigureAwait(true);

            ILocator stopSlot = page.Locator("[data-chatbot-stable-id='project-conversation-ai-response-stop']");
            ILocator stopControl = stopSlot.Locator("fluent-button");
            await WaitForAsync(
                async () => string.Equals(
                    await stopControl.GetAttributeAsync("data-chatbot-streaming-stop-active").ConfigureAwait(true),
                    "true",
                    StringComparison.Ordinal),
                "the SignalR-nudged active generation",
                cancellationToken).ConfigureAwait(true);

            BrowserMatrix.Length.ShouldBe(24);
            foreach (BrowserMatrixRow row in BrowserMatrix)
            {
                await page.SetViewportSizeAsync(row.Width, row.Height).ConfigureAwait(true);
                await page.EmulateMediaAsync(new PageEmulateMediaOptions
                {
                    ReducedMotion = row.ReducedMotion ? ReducedMotion.Reduce : ReducedMotion.NoPreference,
                    ForcedColors = row.ForcedColors ? ForcedColors.Active : ForcedColors.None,
                }).ConfigureAwait(true);
                // The interactive Blazor circuit negotiates culture on its WebSocket request, where the page query
                // string is absent. Set the standard localization cookie as well so both prerender and the live
                // circuit exercise the requested EN/FR culture.
                await context.AddCookiesAsync(
                [
                    new Microsoft.Playwright.Cookie
                    {
                        Name = ".AspNetCore.Culture",
                        Value = $"c={row.Culture}|uic={row.Culture}",
                        Url = uiEndpoint.ToString(),
                    },
                ]).ConfigureAwait(true);
                await page.GotoAsync(
                    ConversationUri(uiEndpoint, row.Culture),
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }).ConfigureAwait(true);
                await stopControl.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);

                (await page.Locator("#project-conversation-title").InnerTextAsync().ConfigureAwait(true))
                    .ShouldContain(row.Culture == "fr" ? "Conversation projet" : "Project conversation");
                (await stopControl.GetAttributeAsync("aria-disabled").ConfigureAwait(true)).ShouldBe("false");
                (await stopControl.GetAttributeAsync("data-chatbot-streaming-stop-active").ConfigureAwait(true)).ShouldBe("true");
                (await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches").ConfigureAwait(true))
                    .ShouldBe(row.ReducedMotion);
                (await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches").ConfigureAwait(true))
                    .ShouldBe(row.ForcedColors);
                await AssertHealthyCurrentStatusAsync(page).ConfigureAwait(true);
                await AssertEveryCriticalSurfaceInMatrixRowAsync(page, stopSlot, stopControl, row).ConfigureAwait(true);
                await AssertCriticalControlNotClippedAsync(stopControl, row).ConfigureAwait(true);
                AssertNoBrowserErrors(browserErrors);
                output.WriteLine("STORY132_BROWSER_MATRIX {0}", row);
            }

            await page.EvaluateAsync(
                """
                () => {
                    window.__story132Announcements = 0;
                    const live = document.querySelector("[data-chatbot-stable-id='project-conversation-ai-response-stop'] [role='status']");
                    let previous = (live?.textContent || '').trim();
                    new MutationObserver(() => {
                        const current = (live?.textContent || '').trim();
                        if (current && current !== previous) window.__story132Announcements++;
                        previous = current;
                    }).observe(live, { subtree: true, childList: true, characterData: true });
                }
                """).ConfigureAwait(true);
            await stopControl.ClickAsync().ConfigureAwait(true);
            await page.Locator("[data-chatbot-streaming-state='stopped']")
                .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);
            (await page.Locator("[data-chatbot-streaming-state='stopped']").InnerTextAsync().ConfigureAwait(true))
                .ShouldBe(BrowserMatrix[^1].Culture == "fr" ? "Réponse arrêtée." : "Response stopped.");
            await WaitForAsync(
                async () => await page.EvaluateAsync<int>("() => window.__story132Announcements || 0").ConfigureAwait(true) == 1,
                "exactly one verified-stop live announcement",
                cancellationToken).ConfigureAwait(true);
            (await stopSlot.Locator("[role='status']").InnerTextAsync().ConfigureAwait(true))
                .ShouldBe(BrowserMatrix[^1].Culture == "fr" ? "Réponse arrêtée" : "Response stopped");
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(true);
            (await page.EvaluateAsync<int>("() => window.__story132Announcements || 0").ConfigureAwait(true)).ShouldBe(1);
            (await page.Locator("#project-conversation-composer-input")
                .EvaluateAsync<bool>("element => document.activeElement === element").ConfigureAwait(true)).ShouldBeTrue();
            (await stopControl.GetAttributeAsync("aria-disabled").ConfigureAwait(true)).ShouldBe("true");
            AssertNoBrowserErrors(browserErrors);
        }
        catch
        {
            output.WriteLine("STORY132_COORDINATOR_MILESTONES {0}", coordinatorLogProbe.Render());
            output.WriteLine("STORY132_CHATBOT_LOG_TAIL\n{0}", coordinatorLogProbe.RenderLogTail());
            throw;
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static ServiceCollection BaseAcceptanceServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<ITenantAiPolicySnapshotProvider, UnavailableTenantAiPolicySnapshotProvider>();
        services.AddSingleton<IAiAssistanceProvider, DisabledAiAssistanceProvider>();
        services.AddScoped<IRiskClassifier, DeterministicAiActionRiskClassifier>();
        return services;
    }

    private static void ConfigureStory132AcceptanceOnChatBotOnly(IDistributedApplicationTestingBuilder builder)
    {
        IResource chatBot = builder.Resources.Single(resource =>
            string.Equals(resource.Name, ChatBotResourceName, StringComparison.Ordinal));
        chatBot.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Testing";
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
            context.EnvironmentVariables["ChatBot__Story132Acceptance__Enabled"] = "true";
            context.EnvironmentVariables["Logging__LogLevel__Hexalith.ChatBot.Server.Lifecycle.AiExecution.AiExecutionCoordinator"] = "Information";
        }));
    }

    private async Task WaitForStory132TopologyAsync(
        DistributedApplication app,
        CancellationToken cancellationToken)
    {
        foreach (string resourceName in new[] { "security", "eventstore", "tenants", ChatBotResourceName, ChatBotUiResourceName })
        {
            ResourceEvent resource = await app.ResourceNotifications
                .WaitForResourceHealthyAsync(resourceName, cancellationToken)
                .WaitAsync(TimeSpan.FromMinutes(5), cancellationToken)
                .ConfigureAwait(true);
            output.WriteLine(
                $"STORY132_ASPIRE_RESOURCE {resourceName} state={resource.Snapshot.State?.Text ?? "unknown"} "
                + $"health={resource.Snapshot.HealthStatus?.ToString() ?? "unknown"}");
        }
    }

    private static async Task AuthenticateActorAlphaAsync(IPage page, Uri uiEndpoint)
    {
        await page.GotoAsync(
            ConversationUri(uiEndpoint, "en"),
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(true);
        ILocator username = page.Locator("#username");
        await username.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);
        await username.FillAsync("actor-alpha").ConfigureAwait(true);
        await page.Locator("#password").FillAsync("actor-alpha-pass").ConfigureAwait(true);
        await page.Locator("#kc-login").ClickAsync().ConfigureAwait(true);
        await page.WaitForURLAsync(
            url => new Uri(url).AbsolutePath.Equals($"/projects/{ProjectId}/conversation", StringComparison.Ordinal),
            new PageWaitForURLOptions { Timeout = 90_000, WaitUntil = WaitUntilState.NetworkIdle }).ConfigureAwait(true);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Project conversation", Level = 1 })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);
    }

    private static async Task SubmitComposerAsync(IPage page, string mode, string text)
    {
        ILocator composer = page.Locator(".chatbot-governed-composer");
        ILocator submitControl = composer.Locator("fluent-button").Filter(new() { HasText = "Submit" }).First;
        await submitControl.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        }).ConfigureAwait(true);
        await WaitForAsync(
            async () =>
                !string.Equals(await submitControl.GetAttributeAsync("aria-disabled").ConfigureAwait(true), "true", StringComparison.Ordinal) &&
                await submitControl.GetAttributeAsync("disabled").ConfigureAwait(true) is null,
            "the composer readiness gate",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        if (!string.Equals(mode, "Message", StringComparison.Ordinal))
        {
            ILocator modeControl = composer.Locator("fluent-button[aria-pressed]").Filter(new() { HasText = mode }).First;
            await modeControl.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            }).ConfigureAwait(true);
            await modeControl.ClickAsync().ConfigureAwait(true);
            await WaitForAsync(
                async () => string.Equals(
                    await modeControl.GetAttributeAsync("aria-pressed").ConfigureAwait(true),
                    "true",
                    StringComparison.Ordinal),
                $"the {mode} composer mode",
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        ILocator inputHost = page.Locator("#project-conversation-composer-input");
        ILocator input = inputHost.Locator("textarea");
        await input.FillAsync(text).ConfigureAwait(true);
        ILocator status = page.Locator("[data-chatbot-stable-id='project-conversation-composer-status']");
        ILocator validation = page.Locator("#project-conversation-composer-error");
        await submitControl.ClickAsync().ConfigureAwait(true);
        await WaitForAsync(
            async () => await IsAcceptedReceiptAsync(input, status).ConfigureAwait(true) ||
                await validation.CountAsync().ConfigureAwait(true) > 0 ||
                await IsRenderedSubmissionFailureAsync(status).ConfigureAwait(true),
            $"the accepted {mode} receipt or a rendered rejection",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        if (!await IsAcceptedReceiptAsync(input, status).ConfigureAwait(true))
        {
            throw new InvalidOperationException(await ComposerFailureDiagnosticAsync(
                mode,
                inputHost,
                input,
                submitControl,
                status,
                validation).ConfigureAwait(true));
        }
    }

    private static async Task<bool> IsAcceptedReceiptAsync(ILocator input, ILocator status)
        => string.IsNullOrEmpty(await input.InputValueAsync().ConfigureAwait(true)) &&
            await status.CountAsync().ConfigureAwait(true) > 0 &&
            string.Equals(
                await status.GetAttributeAsync("data-chatbot-feedback-state").ConfigureAwait(true),
                "CurrentUserCommandAcceptedProjectionPending",
                StringComparison.Ordinal);

    private static async Task<bool> IsRenderedSubmissionFailureAsync(ILocator status)
    {
        if (await status.CountAsync().ConfigureAwait(true) == 0 ||
            !string.Equals(
                await status.GetAttributeAsync("data-chatbot-feedback-state").ConfigureAwait(true),
                "BlockedAction",
                StringComparison.Ordinal))
        {
            return false;
        }

        // BlockedAction is also the composer's safe presentation while an accepted command's authoritative re-query
        // is transiently loading/degraded. Only the dedicated mutation error marker is a submission rejection; the
        // generic status code must not make the live gate fail before bounded accepted-item polling recovers.
        ILocator composer = status.Locator("xpath=ancestor::section[1]");
        string? submissionError = await composer
            .GetAttributeAsync("data-chatbot-submission-error-code")
            .ConfigureAwait(true);
        return IsSubmissionFailureMarker(submissionError);
    }

    private static bool IsSubmissionFailureMarker(string? submissionError)
        => !string.IsNullOrWhiteSpace(submissionError);

    private static async Task<string> ComposerFailureDiagnosticAsync(
        string mode,
        ILocator inputHost,
        ILocator input,
        ILocator submitControl,
        ILocator status,
        ILocator validation)
    {
        string statusText = await status.CountAsync().ConfigureAwait(true) > 0
            ? await status.InnerTextAsync().ConfigureAwait(true)
            : "<absent>";
        string validationText = await validation.CountAsync().ConfigureAwait(true) > 0
            ? await validation.InnerTextAsync().ConfigureAwait(true)
            : "<absent>";

        return $"Composer {mode} submission retained its draft. " +
            $"validation={validationText}; status={statusText}; " +
            $"submission-error={await AttributeOrAbsentAsync(inputHost.Locator("xpath=ancestor::section[1]"), "data-chatbot-submission-error-code").ConfigureAwait(true)}; " +
            $"status-state={await AttributeOrAbsentAsync(status, "data-chatbot-feedback-state").ConfigureAwait(true)}; " +
            $"status-kind={await AttributeOrAbsentAsync(status, "data-chatbot-status").ConfigureAwait(true)}; " +
            $"host-value={await AttributeOrAbsentAsync(inputHost, "value").ConfigureAwait(true)}; " +
            $"host-aria-invalid={await AttributeOrAbsentAsync(inputHost, "aria-invalid").ConfigureAwait(true)}; " +
            $"host-disabled={await AttributeOrAbsentAsync(inputHost, "disabled").ConfigureAwait(true)}; " +
            $"inner-value={await input.InputValueAsync().ConfigureAwait(true)}; " +
            $"inner-attribute-value={await AttributeOrAbsentAsync(input, "value").ConfigureAwait(true)}; " +
            $"submit-disabled={await AttributeOrAbsentAsync(submitControl, "disabled").ConfigureAwait(true)}; " +
            $"submit-aria-disabled={await AttributeOrAbsentAsync(submitControl, "aria-disabled").ConfigureAwait(true)}.";
    }

    private static async Task<string> AttributeOrAbsentAsync(ILocator locator, string attribute)
        => await locator.CountAsync().ConfigureAwait(true) > 0
            ? await locator.GetAttributeAsync(attribute).ConfigureAwait(true) ?? "<absent>"
            : "<absent>";

    private static async Task<string> TextOrAbsentAsync(ILocator locator)
        => await locator.CountAsync().ConfigureAwait(true) > 0
            ? await locator.InnerTextAsync().ConfigureAwait(true)
            : "<absent>";

    private static string UserMessageProjectionIdentity(string correlationId)
        => "ui-message:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(correlationId)))
            .ToLowerInvariant()[..24];

    private static async Task<ProjectConversationResponse> ReadConversationAsync(
        HttpClient client,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectId}/conversation?pageSize=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", ChatBotCommandId.New().ToString());
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectConversationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The project conversation response was empty.");
    }

    private static async Task SubmitLowRiskExecutionAsync(
        HttpClient client,
        string accessToken,
        ProjectConversationItem proposal,
        long expectedProposalSourceVersion,
        string executionId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ExecuteLowRiskAIAssistance command = new(
            ProjectId,
            proposal.AiProposalId ?? throw new InvalidOperationException("The proposal projection has no proposal id."),
            $"task-intent:{executionId}",
            proposal.AiSourceMessageId ?? $"source-message:{executionId}",
            "actor-alpha",
            LowRiskAiAssistanceKind.SummarizeVisibleContext,
            $"context:{executionId}",
            "1",
            "metadata_only",
            "collaboration_input",
            "disabled",
            [$"evidence:{executionId}"],
            [$"project:{ProjectId}"],
            [],
            expectedProposalSourceVersion,
            "story-13-2-acceptance-policy",
            correlationId,
            executionId,
            $"transition:{executionId}",
            proposal.AiSourceConversationItemId);
        string commandId = ChatBotCommandId.New().ToString();
        string taskId = ChatBotCommandId.New().ToString();
        JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        JsonElement commandPayload = JsonSerializer.SerializeToElement(command, jsonOptions);
        string[] commandProjectIds = commandPayload
            .EnumerateObject()
            .Where(static property => string.Equals(property.Name, "projectId", StringComparison.OrdinalIgnoreCase))
            .Select(static property => property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : string.Empty)
            .ToArray();
        if (commandProjectIds.Length != 1 || !string.Equals(commandProjectIds[0], ProjectId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The low-risk execution wire command does not carry exactly one authorized project id.");
        }

        string wirePayload = JsonSerializer.Serialize(new
        {
            commandId,
            commandType = nameof(ExecuteLowRiskAIAssistance),
            command = commandPayload,
            origin = "ui",
            requestSchemaVersion = "v1",
        }, jsonOptions);
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(wirePayload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", correlationId);
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException(
                $"ExecuteLowRiskAIAssistance required 202 Accepted but received {(int)response.StatusCode} "
                + $"{response.StatusCode}: {body}");
        }
    }

    private static async Task AssertHealthyCurrentStatusAsync(IPage page)
    {
        ILocator status = page.Locator("[data-chatbot-stable-id='project-conversation-status']");
        await status.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);
        (await status.GetAttributeAsync("data-chatbot-feedback-state").ConfigureAwait(true))
            .ShouldBe("CurrentUserAiProposalReady");
        string text = await status.InnerTextAsync().ConfigureAwait(true);
        text.ShouldNotContain("Dependency degraded", Case.Insensitive);
        text.ShouldNotContain("Dépendance dégradée", Case.Insensitive);
    }

    private static async Task AssertCriticalControlNotClippedAsync(ILocator control, BrowserMatrixRow row)
    {
        bool clipped = await control.EvaluateAsync<bool>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                if (rect.left < -1 || rect.right > window.innerWidth + 1 || rect.width < 44 || rect.height < 44) return true;
                for (let current = element.parentElement; current; current = current.parentElement) {
                    const style = getComputedStyle(current);
                    if ((style.overflowX === 'hidden' || style.overflowX === 'clip') && current.scrollWidth > current.clientWidth + 1) return true;
                }
                return false;
            }
            """).ConfigureAwait(true);
        clipped.ShouldBeFalse($"Critical control must not be clipped at {row.Width}x{row.Height}, {row.Culture}, {row.ModeLabel}.");
    }

    private static async Task AssertEveryCriticalSurfaceInMatrixRowAsync(
        IPage page,
        ILocator stopSlot,
        ILocator stopControl,
        BrowserMatrixRow row)
    {
        ILocator stream = page.Locator(".chatbot-conversation-stream");
        ILocator outcome = page.Locator(".chatbot-ai-outcome-conversation-item").Last;
        ILocator attribution = outcome.Locator(".chatbot-ai-outcome-conversation-item__header");
        ILocator evidence = outcome.Locator("[data-chatbot-ai-content='source-evidence']");
        ILocator generated = outcome.Locator("[data-chatbot-ai-content='ai-summary']");
        ILocator status = page.Locator("[data-chatbot-stable-id='project-conversation-status']");
        ILocator composer = page.Locator(".chatbot-governed-composer");
        ILocator input = page.Locator("#project-conversation-composer-input");
        ILocator submit = composer.Locator(".chatbot-governed-composer__actions fluent-button").First;
        ILocator liveRegion = stopSlot.Locator("[role='status']");

        foreach (ILocator surface in new[] { stream, outcome, attribution, evidence, generated, status, composer, input, submit, stopControl })
        {
            await surface.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 90_000 }).ConfigureAwait(true);
        }

        string attributionLabel = await outcome.GetAttributeAsync("aria-label").ConfigureAwait(true) ?? string.Empty;
        attributionLabel.ShouldNotBeNullOrWhiteSpace();
        (await evidence.InnerTextAsync().ConfigureAwait(true)).ShouldNotBeNullOrWhiteSpace();
        (await generated.GetAttributeAsync("aria-label").ConfigureAwait(true)).ShouldNotBeNullOrWhiteSpace();
        (await liveRegion.GetAttributeAsync("aria-live").ConfigureAwait(true)).ShouldBe("polite");
        (await liveRegion.GetAttributeAsync("aria-atomic").ConfigureAwait(true)).ShouldBe("true");
        (await input.GetAttributeAsync("aria-describedby").ConfigureAwait(true) ?? string.Empty)
            .ShouldContain("project-conversation-composer-help");
        await input.FocusAsync().ConfigureAwait(true);
        // Blazor may finish an advisory SignalR re-query in the same turn as Playwright focuses the Fluent host. The
        // control must still become the active element; wait for that exact invariant instead of sampling the one
        // transient render boundary where the host is being reconciled.
        await WaitForAsync(
            async () =>
            {
                // Re-apply focus if an advisory re-query reconciled the Fluent host after the preceding attempt.
                // The asserted state is unchanged; this makes every row prove the currently mounted control.
                await input.FocusAsync().ConfigureAwait(true);
                return await input
                    .EvaluateAsync<bool>("element => document.activeElement === element")
                    .ConfigureAwait(true);
            },
            $"the composer input focus contract at {row.Width}x{row.Height}, {row.Culture}, {row.ModeLabel}",
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await AssertCriticalControlNotClippedAsync(input, row).ConfigureAwait(true);
        await AssertCriticalControlNotClippedAsync(submit, row).ConfigureAwait(true);
    }

    private static void AssertNoBrowserErrors(ConcurrentQueue<string> errors)
        => errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors));

    private static async Task WaitForUiListeningAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        using HttpClient client = new() { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(10) };
        await WaitForAsync(
            async () =>
            {
                try
                {
                    using HttpResponseMessage response = await client.GetAsync("/health", cancellationToken).ConfigureAwait(false);
                    return response.IsSuccessStatusCode;
                }
                catch (HttpRequestException)
                {
                    return false;
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            },
            "the ChatBot UI listener",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition, string description, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProjectionTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await condition().ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Timed out waiting for {description}.");
    }

    private static string RequireChromeExecutable()
    {
        string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
        string path = string.IsNullOrWhiteSpace(configured) ? "/usr/bin/google-chrome" : configured;
        if (File.Exists(path))
        {
            return path;
        }

        bool required = string.Equals(Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3_REQUIRED"), "1", StringComparison.Ordinal);
        if (required)
        {
            throw new InvalidOperationException($"The required Story 13.2 browser lane cannot find Chrome at '{path}'.");
        }

        Assert.Skip($"Chrome is unavailable at '{path}'. Set CHROME_EXECUTABLE_PATH to run Story 13.2 browser acceptance.");
        throw new UnreachableException();
    }

    private static string ConversationUri(Uri baseUri, string culture)
        => new Uri(baseUri, $"/projects/{ProjectId}/conversation?culture={culture}&ui-culture={culture}").ToString();

    private static void AssertProjectOwnerClaim(string accessToken)
    {
        string[] segments = accessToken.Split('.');
        if (segments.Length < 2)
        {
            throw new InvalidOperationException("The actor-alpha access token is not a JWT.");
        }

        string payload = segments[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using JsonDocument token = JsonDocument.Parse(Convert.FromBase64String(payload));
        if (!token.RootElement.TryGetProperty(ParticipantAuthorizationStage.ProjectOwnerClaim, out JsonElement claim))
        {
            throw new InvalidOperationException(
                $"The actor-alpha access token has no '{ParticipantAuthorizationStage.ProjectOwnerClaim}' claim.");
        }

        bool authorized = claim.ValueKind == JsonValueKind.String
            ? string.Equals(claim.GetString(), ProjectId, StringComparison.Ordinal)
            : claim.ValueKind == JsonValueKind.Array && claim.EnumerateArray().Any(value =>
                value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), ProjectId, StringComparison.Ordinal));
        if (!authorized)
        {
            throw new InvalidOperationException(
                $"The actor-alpha access token does not authorize project '{ProjectId}'.");
        }
    }

    private sealed record BrowserMatrixRow(
        string Culture,
        int Width,
        int Height,
        bool ReducedMotion,
        bool ForcedColors)
    {
        public string ModeLabel => $"motion={(ReducedMotion ? "reduced" : "normal")}, forcedColors={(ForcedColors ? "active" : "none")}";
    }

    private sealed class Story132CoordinatorLogProbe : IAsyncDisposable
    {
        private static readonly int[] EventIds = [130202, 130203, 130204, 130205];
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _capture;
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentDictionary<int, int> _counts = new();
        private readonly ConcurrentQueue<string> _logTail = new();

        private Story132CoordinatorLogProbe(IAsyncEnumerable<IReadOnlyList<LogLine>> batches)
            => _capture = CaptureAsync(batches);

        public static async Task<Story132CoordinatorLogProbe> StartAsync(
            IAsyncEnumerable<IReadOnlyList<LogLine>> batches,
            CancellationToken cancellationToken)
        {
            Story132CoordinatorLogProbe probe = new(batches);
            await probe._started.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return probe;
        }

        public string Render()
            => string.Join(',', EventIds.Select(id => $"{id}={_counts.GetValueOrDefault(id)}"));

        public string RenderLogTail()
            => string.Join(Environment.NewLine, _logTail);

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync().ConfigureAwait(false);
            try
            {
                await _capture.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Metadata-only diagnostics cannot replace the acceptance result during teardown.
            }

            _stop.Dispose();
        }

        private async Task CaptureAsync(IAsyncEnumerable<IReadOnlyList<LogLine>> batches)
        {
            try
            {
                IAsyncEnumerator<IReadOnlyList<LogLine>> enumerator = batches.GetAsyncEnumerator(_stop.Token);
                await using (enumerator.ConfigureAwait(false))
                {
                    ValueTask<bool> moveNext = enumerator.MoveNextAsync();
                    _started.TrySetResult();
                    while (await moveNext.ConfigureAwait(false))
                    {
                        foreach (LogLine line in enumerator.Current)
                        {
                            _logTail.Enqueue(line.Content);
                            while (_logTail.Count > 80)
                            {
                                _ = _logTail.TryDequeue(out _);
                            }

                            foreach (int eventId in EventIds)
                            {
                                if (line.Content.Contains($"[{eventId}]", StringComparison.Ordinal))
                                {
                                    _counts.AddOrUpdate(eventId, 1, static (_, current) => checked(current + 1));
                                }
                            }
                        }

                        moveNext = enumerator.MoveNextAsync();
                    }
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
                _started.TrySetResult();
            }
        }
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Hexalith.ChatBot.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

}
