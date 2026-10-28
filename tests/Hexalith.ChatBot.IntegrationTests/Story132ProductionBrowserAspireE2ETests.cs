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
    public async Task AuthenticatedProductionClientShouldFailClosedWithoutPersistingAcrossRequiredChromeMatrix()
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

            // Story 1.7 keeps production admission. The M0 message/approval/execution/stop loop needs
            // Story 1.8 commit participation; this live contract proves refusal and persisted-state invariance.
            using HttpClient chatBotState = new() { BaseAddress = app.GetEndpoint("chatbot-dapr-cli", "http") };
            using HttpClient ownerState = new() { BaseAddress = app.GetEndpoint("eventstore-dapr-cli", "http") };
            const string tenant = "tenant-alpha";
            string actorId = $"{tenant}:chatbot:{ProjectId}";
            string ownerPath = $"v1.0/actors/AggregateActor/{Uri.EscapeDataString(actorId)}/state/{Uri.EscapeDataString(actorId + ":metadata")}";
            string projectionPath = $"v1.0/state/chatbot-statestore/{Uri.EscapeDataString(tenant + ":project-conversation:" + ProjectId + ":index")}?consistency=strong";
            string ownerBefore = await ReadPersistedSnapshotAsync(ownerState, ownerPath, cancellationToken).ConfigureAwait(true);
            string projectionBefore = await ReadPersistedSnapshotAsync(chatBotState, projectionPath, cancellationToken).ConfigureAwait(true);

            await AssertLowRiskHttpRefusalAsync(chatBot, chatBotState, accessToken, cancellationToken).ConfigureAwait(true);
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
                await page.GotoAsync(ConversationUri(uiEndpoint, row.Culture),
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }).ConfigureAwait(true);
                (await page.Locator("#project-conversation-title").InnerTextAsync().ConfigureAwait(true))
                    .ShouldContain(row.Culture == "fr" ? "Conversation projet" : "Project conversation");
                foreach (string mode in new[] { "Message", "Ask AI" })
                {
                    await SubmitComposerAndExpectSafeRefusalAsync(page, mode, "Story 1.7 governed refusal", row.Culture).ConfigureAwait(true);
                }
                ILocator input = page.Locator("[data-chatbot-composer-input='true']");
                await AssertCriticalControlNotClippedAsync(input, row).ConfigureAwait(true);
                (await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches").ConfigureAwait(true)).ShouldBe(row.ReducedMotion);
                (await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches").ConfigureAwait(true)).ShouldBe(row.ForcedColors);
                AssertNoBrowserErrors(browserErrors);
                output.WriteLine("STORY132_FAIL_CLOSED_BROWSER_MATRIX {0}", row);
            }
            (await ReadPersistedSnapshotAsync(ownerState, ownerPath, cancellationToken).ConfigureAwait(true)).ShouldBe(ownerBefore);
            (await ReadPersistedSnapshotAsync(chatBotState, projectionPath, cancellationToken).ConfigureAwait(true)).ShouldBe(projectionBefore);
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
            // Development only because this local topology serves HTTP identity metadata, which the EventStore SDK
            // rejects outside Development. Development also relaxes that SDK's internal-credential, reminder and
            // app-channel token checks, so this lane is client fail-closed evidence, not host-hardening evidence.
            context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Development";
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
            context.EnvironmentVariables["ChatBot__Story132Acceptance__Enabled"] = "true";
            context.EnvironmentVariables["Logging__LogLevel__Hexalith.ChatBot.Server.Lifecycle.AiExecution.AiExecutionCoordinator"] = "Information";
        }));
    }

    private async Task WaitForStory132TopologyAsync(
        DistributedApplication app,
        CancellationToken cancellationToken)
    {
        // ONE shared wall-clock budget across the five resources, not five independent 5-minute waits. Per-resource
        // timeouts multiply: five slow resources cost 25 minutes, and this suite runs as a step of the same
        // `topology-acceptance` job (timeout-minutes: 30) that first runs the governed-command lane. A partial
        // startup hang was then killed by the runner, skipping the `if: always()` artifact upload and losing the
        // evidence this lane exists to produce. Mirrors TopologyReadinessBudget in TrivialGovernedCommandAspireE2eTests.
        TimeSpan readinessBudget = TimeSpan.FromMinutes(8);
        Stopwatch readinessClock = Stopwatch.StartNew();
        foreach (string resourceName in new[] { "security", "eventstore", "tenants", ChatBotResourceName, ChatBotUiResourceName })
        {
            TimeSpan remaining = readinessBudget - readinessClock.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException(
                    $"The shared {readinessBudget} Story 13.2 topology readiness budget was exhausted before "
                    + $"'{resourceName}' was waited on. Earlier resources consumed the whole budget.");
            }

            ResourceEvent resource;
            try
            {
                resource = await app.ResourceNotifications
                    .WaitForResourceHealthyAsync(resourceName, cancellationToken)
                    .WaitAsync(remaining, cancellationToken)
                    .ConfigureAwait(true);
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException(
                    $"'{resourceName}' did not report healthy within the remaining {remaining} of the shared "
                    + $"{readinessBudget} Story 13.2 topology readiness budget.",
                    exception);
            }

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

    private static async Task SubmitComposerAndExpectSafeRefusalAsync(IPage page, string mode, string text, string culture)
    {
        ILocator composer = page.Locator(".chatbot-governed-composer");
        ILocator submitControl = composer.Locator("fluent-button").Filter(new() { HasText = culture == "fr" ? "Envoyer" : "Submit" }).First;
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
            ILocator modeControl = composer.Locator("fluent-button[aria-pressed]").Filter(new() { HasText = mode == "Ask AI" && culture == "fr" ? "Demander à l'IA" : mode }).First;
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

        ILocator inputHost = page.Locator("[data-chatbot-composer-input='true']");
        ILocator input = inputHost.Locator("textarea");
        await input.FillAsync(text).ConfigureAwait(true);
        ILocator status = page.Locator("[data-chatbot-stable-id$='project-conversation-composer-status']");
        ILocator validation = page.Locator("[data-chatbot-composer-validation='true']");
        await submitControl.ClickAsync().ConfigureAwait(true);
        await WaitForAsync(
            async () => await IsAcceptedReceiptAsync(input, status).ConfigureAwait(true) ||
                await validation.CountAsync().ConfigureAwait(true) > 0 ||
                await IsRenderedSubmissionFailureAsync(status).ConfigureAwait(true),
            $"the {mode} submission result",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        (await IsRenderedSubmissionFailureAsync(status).ConfigureAwait(true)).ShouldBeTrue();
        (await composer.GetAttributeAsync("data-chatbot-submission-error-code").ConfigureAwait(true))
            .ShouldBe(Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCodes.CanonicalExecutionUnavailable);
        (await input.InputValueAsync().ConfigureAwait(true)).ShouldBe(text);
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

    /// <summary>Exercises production HTTP admission and directly verifies absent durable operation and identity state.</summary>
    private static async Task AssertLowRiskHttpRefusalAsync(HttpClient client, HttpClient state, string accessToken, CancellationToken cancellationToken)
    {
        string operation = ChatBotCommandId.New().ToString();
        string correlation = ChatBotCommandId.New().ToString();
        ExecuteLowRiskAIAssistance command = new(ProjectId, $"refused-proposal:{operation}", $"refused-intent:{operation}",
            $"refused-source:{operation}", "actor-alpha", LowRiskAiAssistanceKind.SummarizeVisibleContext,
            $"refused-context:{operation}", "1", "metadata_only", "collaboration_input", "disabled", [], [], [], 0,
            "story-13-2-acceptance-policy", correlation, operation, $"refused-transition:{operation}");
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = JsonContent.Create(new { commandId = operation, commandType = nameof(ExecuteLowRiskAIAssistance), command, origin = "ui", requestSchemaVersion = "v1" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", correlation);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        problem.RootElement.GetProperty("code").GetString().ShouldBe(Hexalith.ChatBot.Contracts.Messages.ChatBotMessageCodes.CanonicalExecutionUnavailable);
        problem.RootElement.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        string identity = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\u001f', "tenant-alpha", "command-identity", operation))));
        foreach (string key in new[] { $"tenant-alpha:operation-status:{operation}", identity })
        {
            using HttpResponseMessage persisted = await state.GetAsync($"v1.0/state/chatbot-statestore/{Uri.EscapeDataString(key)}?consistency=strong", cancellationToken).ConfigureAwait(false);
            persisted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }

    /// <summary>Reads authoritative persisted contents; transport faults cannot stand in for absence.</summary>
    private static async Task<string> ReadPersistedSnapshotAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(path, cancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
        return $"{(int)response.StatusCode}:" + await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
