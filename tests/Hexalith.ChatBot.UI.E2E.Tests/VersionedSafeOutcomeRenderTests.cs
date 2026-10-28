using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using Fluxor;

using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Components.Pages;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.ProjectConversation;
using Hexalith.ChatBot.UI.State.AssociationReview;
using Hexalith.ChatBot.UI.State.GovernedOperations;
using Hexalith.ChatBot.UI.State.OperationalDashboards;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

public sealed class VersionedSafeOutcomeRenderTests
{
    [Fact]
    public async Task KnownCodeAndKnownBlockedReasonShouldRenderSpecificSafeCopyAndAction()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());

            string mappedDenial = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotBlockedState>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.Denial,
                    [nameof(ChatBotBlockedState.StableId)] = "mapped-denial",
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string auditBlock = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotBlockedState>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.FailedDependency,
                    [nameof(ChatBotBlockedState.MessageCatalogCode)] = ChatBotMessageCodes.AuditUnavailable,
                    [nameof(ChatBotBlockedState.StableId)] = "audit-block",
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string auditBanner = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotStatusBanner>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Warning,
                    [nameof(ChatBotStatusBanner.MessageCatalogCode)] = ChatBotMessageCodes.AuditUnavailable,
                    [nameof(ChatBotStatusBanner.StateFamily)] = ChatBotFeedbackStateFamily.RetryableFailure,
                    [nameof(ChatBotStatusBanner.Message)] = "restricted private details",
                    [nameof(ChatBotStatusBanner.AccessibleLabel)] = "restricted private label",
                    [nameof(ChatBotStatusBanner.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "restricted private child")),
                    [nameof(ChatBotStatusBanner.StableId)] = "audit-banner",
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string denialText = WebUtility.HtmlDecode(mappedDenial);
            denialText.ShouldContain("L'opération n'est pas disponible pour cet appelant.");
            denialText.ShouldContain("Demander l'accès");
            denialText.ShouldNotContain("L'action demandée est indisponible.");
            mappedDenial.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");

            string auditBlockText = WebUtility.HtmlDecode(auditBlock);
            auditBlockText.ShouldContain("La commande ne peut pas être acceptée tant que l'enregistrement d'audit est indisponible.");
            auditBlockText.ShouldContain("Réessayer plus tard");
            auditBlockText.ShouldNotContain("L'action demandée est indisponible.");

            string auditBannerText = WebUtility.HtmlDecode(auditBanner);
            auditBannerText.ShouldContain("Audit indisponible");
            auditBannerText.ShouldContain("La commande ne peut pas être acceptée tant que l'enregistrement d'audit est indisponible.");
            auditBannerText.ShouldContain("Réessayer plus tard");
            auditBannerText.ShouldNotContain("restricted private");
            auditBanner.ShouldContain("role=\"status\"");
            auditBanner.ShouldContain("aria-live=\"polite\"");
            auditBanner.ShouldContain("aria-disabled=\"true\"");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task UnknownCodeShouldRenderLocalizedTerminalFallbackAndAnnounceOnlyOnce()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            ParameterView parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.Denial,
                [nameof(ChatBotBlockedState.MessageCatalogCode)] = "private_unknown_failure",
                [nameof(ChatBotBlockedState.ReasonText)] = "restricted private prose",
                [nameof(ChatBotBlockedState.SafeNextAction)] = "unsafe-action",
                [nameof(ChatBotBlockedState.StableId)] = "safe-outcome-test",
                [nameof(ChatBotBlockedState.IsTerminalForCurrentUser)] = true,
            });

            string first = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotBlockedState>(parameters).ConfigureAwait(true)).ToHtmlString())
                .ConfigureAwait(true);
            string repeated = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotBlockedState>(parameters).ConfigureAwait(true)).ToHtmlString())
                .ConfigureAwait(true);
            string unknownBanner = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotStatusBanner>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Danger,
                    [nameof(ChatBotStatusBanner.MessageCatalogCode)] = "private_unknown_failure",
                    [nameof(ChatBotStatusBanner.Message)] = "restricted private details",
                    [nameof(ChatBotStatusBanner.StateFamily)] = ChatBotFeedbackStateFamily.RetryableFailure,
                    [nameof(ChatBotStatusBanner.StableId)] = "unknown-banner",
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string firstText = WebUtility.HtmlDecode(first);
            string repeatedText = WebUtility.HtmlDecode(repeated);
            firstText.ShouldContain("L'action demandée est indisponible.");
            firstText.ShouldContain("Fermer ce message.");
            firstText.ShouldContain("État non autorisé");
            first.ShouldNotContain("private_unknown_failure");
            first.ShouldNotContain("restricted private prose");
            first.ShouldNotContain("unsafe-action");
            first.ShouldContain("role=\"alert\"");
            first.ShouldContain("aria-live=\"assertive\"");
            first.ShouldContain("data-chatbot-live-announced=\"true\"");
            repeated.ShouldContain("aria-live=\"off\"");
            repeated.ShouldContain("data-chatbot-live-announced=\"false\"");
            repeatedText.ShouldContain("L'action demandée est indisponible.");
            WebUtility.HtmlDecode(unknownBanner).ShouldContain("L'action demandée est indisponible.");
            unknownBanner.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");
            unknownBanner.ShouldContain("aria-live=\"assertive\"");
            unknownBanner.ShouldNotContain("private_unknown_failure");
            unknownBanner.ShouldNotContain("restricted private details");

            string action = Regex.Match(first, "<fluent-button[^>]*aria-disabled=\"true\"[^>]*>").Value;
            action.ShouldNotBeEmpty(first);
            string reasonId = Regex.Match(action, "aria-describedby=\"([^\"]+)\"").Groups[1].Value;
            reasonId.ShouldNotBeEmpty();
            first.ShouldContain($"id=\"{reasonId}\"");
            first.ShouldContain("tabindex=\"0\"");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task LocalEmptyBlockedStateShouldKeepTrustedGuidance()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddFluentUIComponents();
        services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        services.AddSingleton<ChatBotCultureFormatter>();
        services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());

        string html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<ChatBotBlockedState>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.UnresolvedAssociation,
                [nameof(ChatBotBlockedState.UseLocalEmptyStateGuidance)] = true,
                [nameof(ChatBotBlockedState.ReasonText)] = "Trusted empty conversation explanation",
                [nameof(ChatBotBlockedState.SafeNextAction)] = "Trusted empty conversation action",
            })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

        html.ShouldContain("Trusted empty conversation explanation");
        html.ShouldContain("Trusted empty conversation action");
        html.ShouldNotContain("The requested action is unavailable.");
    }

    [Fact]
    public async Task FailureConversationItemShouldHideUnknownCodeAndUnsafeAction()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            ProjectConversationItemModel item = JsonSerializer.Deserialize<ProjectConversationItemModel>(
                """{"itemId":"failure-unknown","kind":"FailureState","actorKind":"System","actorLabel":"Worker","occurredAt":"2026-01-01T00:00:00Z","failureStatus":"failed","messageCatalogCode":"private_unknown_failure","safeNextAction":"unsafe-action","clientAction":"unsafe-action","failureReasonCode":"restricted private prose","messageDetailVisibility":"metadata_only","blockedReason":"Denied"}""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            string html = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotFailureStateConversationItem>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotFailureStateConversationItem.Item)] = item,
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string rendered = WebUtility.HtmlDecode(html);
            rendered.ShouldContain("L'action demandée est indisponible.");
            rendered.ShouldContain("generic_denial");
            rendered.ShouldContain("Fermer ce message");
            html.ShouldContain("role=\"status\"");
            html.ShouldContain("aria-live=\"off\"");
            html.ShouldNotContain("private_unknown_failure");
            html.ShouldNotContain("unsafe-action");
            html.ShouldNotContain("restricted private prose");
            html.ShouldNotContain("retry-later");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task UncodedLocalBannerShouldKeepGuidanceAndDifferentCodesShouldAnnounce()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());

            async Task<string> Render(string? code, string message) => await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotStatusBanner>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotStatusBanner.StableId)] = "same-composer",
                    [nameof(ChatBotStatusBanner.AnnouncementKey)] = "fixed-composer",
                    [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Danger,
                    [nameof(ChatBotStatusBanner.StateFamily)] = ChatBotFeedbackStateFamily.TerminalPolicyFailure,
                    [nameof(ChatBotStatusBanner.MessageCatalogCode)] = code,
                    [nameof(ChatBotStatusBanner.Message)] = message,
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string local = await Render(null, "Trusted local projection guidance");
            string authorization = await Render(ChatBotMessageCodes.AuthorizationDenied, "raw server text");
            string audit = await Render(ChatBotMessageCodes.AuditUnavailable, "raw server text");

            local.ShouldContain("Trusted local projection guidance");
            authorization.ShouldContain("data-chatbot-live-announced=\"true\"");
            Regex.Match(audit, "data-chatbot-announcement-key=\"([^\"]+)\"").Groups[1].Value.ShouldBe("fixed-composer-audit_unavailable");
            audit.ShouldContain("data-chatbot-live-announced=\"true\"");
            authorization.ShouldNotContain("raw server text");
            audit.ShouldNotContain("raw server text");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }


    [Theory]
    [InlineData("authorization_denied", "The operation is not available to this caller.", "Request access")]
    [InlineData("restricted_unknown_code", "The requested action is unavailable.", "Dismiss")]
    public async Task ComposerSubmissionFailureShouldRenderSafeCatalogGuidance(string code, string reason, string action)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());

            string html = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotGovernedComposer>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotGovernedComposer.SubmissionErrorCode)] = code,
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string text = WebUtility.HtmlDecode(html);
            text.ShouldContain(reason);
            text.ShouldContain(action);
            html.ShouldContain($"data-chatbot-feedback-state=\"{(code == "restricted_unknown_code" ? "TerminalPolicyFailure" : "BlockedAction")}\"");
            html.ShouldContain("role=\"alert\"");
            html.ShouldContain("aria-disabled=\"true\"");
            html.ShouldNotContain("restricted_unknown_code");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ChatBotFeedbackStateFamily.BlockedAction)]
    [InlineData(ChatBotFeedbackStateFamily.DependencyDegraded)]
    public async Task TerminalBannerShouldDeduplicateWithoutCallerTerminalState(ChatBotFeedbackStateFamily? stateFamily)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            ParameterView parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Danger,
                [nameof(ChatBotStatusBanner.MessageCatalogCode)] = "restricted_unknown_code",
                [nameof(ChatBotStatusBanner.StateFamily)] = stateFamily,
                [nameof(ChatBotStatusBanner.Message)] = "restricted raw prose",
            });
            async Task<string> Render() => await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotStatusBanner>(parameters).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            string first = await Render();
            string repeat = await Render();

            first.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");
            first.ShouldContain("role=\"alert\"");
            first.ShouldContain("aria-live=\"assertive\"");
            repeat.ShouldContain("data-chatbot-live-announced=\"false\"");
            repeat.ShouldContain("aria-live=\"off\"");
            repeat.ShouldContain("The requested action is unavailable.");
            first.ShouldNotContain("restricted");
            repeat.ShouldNotContain("restricted");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }


    [Fact]
    public async Task ObserverGenericDenialShouldKeepPassiveFeedbackSemantics()
    {
        string html = await RenderParentAsync<ChatBotStatusBanner>(new()
        {
            [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Danger,
            [nameof(ChatBotStatusBanner.MessageCatalogCode)] = ChatBotMessageCodes.GenericDenial,
            [nameof(ChatBotStatusBanner.StateFamily)] = ChatBotFeedbackStateFamily.ObservedForOthersRejectionOrQueueUpdate,
        });
        html.ShouldContain("data-chatbot-feedback-state=\"ObservedForOthersRejectionOrQueueUpdate\"");
        html.ShouldContain("aria-live=\"off\"");
        html.ShouldContain("data-chatbot-live-announced=\"false\"");
        html.ShouldContain("data-chatbot-repeat-rule=\"NoLiveAnnouncement\"");
        html.ShouldNotContain("role=\"alert\"");
        WebUtility.HtmlDecode(html).ShouldContain("The requested action is unavailable.");
    }

    [Fact]
    public async Task FailureBannerShouldRejectCatalogSuccessCode()
    {
        string html = await RenderParentAsync<ChatBotStatusBanner>(new Dictionary<string, object?>
        {
            [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Danger,
            [nameof(ChatBotStatusBanner.MessageCatalogCode)] = ChatBotMessageCodes.AssociationDecisionAccepted,
            [nameof(ChatBotStatusBanner.StateFamily)] = ChatBotFeedbackStateFamily.BlockedAction,
        });

        WebUtility.HtmlDecode(html).ShouldContain("The requested action is unavailable.");
        html.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");
        html.ShouldNotContain("Association decision accepted");
    }

    [Theory]
    [InlineData(false, "retry-later", "retry-later", false)]
    [InlineData(true, "none", "none", false)]
    [InlineData(true, "retry-later", "retry-later", true)]
    public async Task FailureItemShouldRespectRetryPermissionWithCallback(
        bool retryable, string safeAction, string clientAction, bool enabled)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            ProjectConversationItemModel item = JsonSerializer.Deserialize<ProjectConversationItemModel>(
                """{"itemId":"failure-retry","kind":"FailureState","actorKind":"System","actorLabel":"Worker","occurredAt":"2026-01-01T00:00:00Z","failureStatus":"failed","messageCatalogCode":"failed_attachment","messageDetailVisibility":"metadata_only"}""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            item = item with { Retryable = retryable, SafeNextAction = safeAction, ClientAction = clientAction };
            EventCallback callback = EventCallback.Factory.Create(this, () => { });

            string html = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotFailureStateConversationItem>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotFailureStateConversationItem.Item)] = item,
                    [nameof(ChatBotFailureStateConversationItem.OnNextAction)] = callback,
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);

            html.ShouldContain($"aria-disabled=\"{(!enabled).ToString().ToLowerInvariant()}\"");
            if (!enabled)
            {
                html.ShouldContain("aria-describedby=\"failure-retry-safe-action-disabled-reason\"");
                html.ShouldContain("<code class=\"chatbot-code\">none</code>");
                WebUtility.HtmlDecode(html).ShouldContain("State not permitted");
                WebUtility.HtmlDecode(html).ShouldNotContain("No user action is required.");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData("authentication_denied", "Authentication is required before this operation can continue.", "Authenticate")]
    [InlineData("authorization_denied", "The operation is not available to this caller.", "Request access")]
    [InlineData("dependency_degraded", "A required dependency is temporarily degraded.", "Retry")]
    [InlineData("private-disabled-sentinel", "The requested action is unavailable.", "Dismiss")]
    public async Task DisabledComposerShouldRenderSpecificCatalogAction(string code, string reason, string action)
    {
        string html = await RenderParentAsync<ChatBotGovernedComposer>(new Dictionary<string, object?>
        {
            [nameof(ChatBotGovernedComposer.Disabled)] = true,
            [nameof(ChatBotGovernedComposer.DisabledReasonCode)] = code,
        });
        string text = WebUtility.HtmlDecode(html);
        text.ShouldContain(reason);
        text.ShouldContain(action);
        html.ShouldContain("aria-disabled=\"true\"");
        html.ShouldContain("tabindex=\"0\"");
        html.ShouldNotContain("private-disabled-sentinel");
    }

    [Theory]
    [InlineData("governed")]
    [InlineData("association")]
    [InlineData("dashboards")]
    public async Task PageFailureShouldHideSentinelAndWithholdRetry(string page)
    {
        const string sentinel = "restricted-page-secret";
        string html = page switch
        {
            "governed" => await RenderParentAsync<GovernedOperations>([], governed: new(false, null, sentinel)),
            "association" => await RenderParentAsync<AssociationReview>([], association: new(false, false, null, null, "", "", sentinel, null, AssociationReviewErrorScope.Load)),
            _ => await RenderParentAsync<OperationalDashboards>([], dashboards: new(false, null, sentinel)),
        };
        html.ShouldNotContain(sentinel);
        WebUtility.HtmlDecode(html).ShouldContain("The requested action is unavailable.");
        WebUtility.HtmlDecode(html).ShouldContain("Dismiss");
        html.ShouldContain("role=\"alert\"");
        html.ShouldNotContain("data-chatbot-stable-id=\"association-review-retry\"");
    }

    [Theory]
    [InlineData("Not-Authorized", "The operation is not available to this caller.")]
    [InlineData("TIMEOUT", "A required dependency is temporarily degraded.")]
    public async Task AssociationReviewLegacyFailureShouldRenderCatalogGuidance(string alias, string reason)
    {
        string html = await RenderParentAsync<AssociationReview>([],
            association: new(false, false, null, null, "", "", alias, null, AssociationReviewErrorScope.Load));
        WebUtility.HtmlDecode(html).ShouldContain(reason);
        html.ShouldNotContain(alias);
        if (alias == "TIMEOUT")
        {
            Regex.Matches(html, "data-chatbot-action-state=\"Enabled\"").Count.ShouldBe(1);
            html.ShouldNotContain("data-chatbot-stable-id=\"association-review-failed-safe-action\"");
        }
    }

    [Fact]
    public async Task WhyPanelFailureShouldRenderGenericCatalogGuidanceWithoutCallerCode()
    {
        const string sentinel = "restricted-why-panel-secret";
        string html = await RenderParentAsync<ChatBotProjectConversationWorkspace>([],
            project: new ProjectConversationState(false, null, null) with { WhyPanelErrorCode = sentinel });
        WebUtility.HtmlDecode(html).ShouldContain("The requested action is unavailable.");
        WebUtility.HtmlDecode(html).ShouldContain("Dismiss");
        html.ShouldNotContain(sentinel);
    }

    [Theory]
    [InlineData(ChatBotMessageCodes.AuthenticationDenied, "Authentication is required before this operation can continue.")]
    [InlineData(ChatBotMessageCodes.AuthorizationDenied, "The operation is not available to this caller.")]
    [InlineData(ChatBotMessageCodes.UnauthorizedParticipant, "The operation is not available to this participant.")]
    public async Task WorkspaceAuthorizationFailureShouldRenderCatalogGuidance(string code, string reason)
    {
        string html = await RenderParentAsync<ChatBotProjectConversationWorkspace>([], project: new(false, null, code));
        WebUtility.HtmlDecode(html).ShouldContain(reason);
        html.ShouldContain("data-chatbot-stable-id=\"project-workspace-unauthorized\"");
        html.ShouldNotContain("data-chatbot-stable-id=\"project-conversation-retryable\"");
    }

    /// <summary>Two persistent explanations of one workspace failure share one live announcement.</summary>
    /// <param name="code">The workspace load failure.</param>
    /// <param name="authorization">Whether this is an authorization failure.</param>
    [Theory]
    [InlineData(ChatBotMessageCodes.AuthorizationDenied, true)]
    [InlineData(ChatBotMessageCodes.AuthenticationDenied, true)]
    [InlineData(ChatBotMessageCodes.AuditUnavailable, false)]
    [InlineData("private-workspace-error", false)]
    public async Task WorkspaceFailureShouldAnnounceItsCatalogExplanationOnlyOnce(string code, bool authorization)
    {
        ProjectConversationModel conversation = new("project", "Project", null, "Current", "Current", [], null, false,
            20, "synthetic", "metadata_only", "standard", "v1", "correlation", "none");
        string html = await RenderParentAsync<ChatBotProjectConversationWorkspace>([], project: new(false, conversation, code));
        string bannerId = authorization ? "project-conversation-failed" : "project-conversation-retryable";
        string blockedId = authorization ? "project-workspace-unauthorized" : "project-workspace-retryable";
        string banner = Regex.Match(html, $"<[^>]+data-chatbot-stable-id=\"{bannerId}\"[^>]*>").Value;
        string blocked = Regex.Match(html, $"<[^>]+data-chatbot-stable-id=\"{blockedId}\"[^>]*>").Value;
        string key = Regex.Match(banner, "data-chatbot-announcement-key=\"([^\"]+)\"").Groups[1].Value;

        banner.ShouldContain("data-chatbot-live-announced=\"true\"");
        banner.ShouldNotContain("aria-live=\"off\"");
        blocked.ShouldContain("data-chatbot-live-announced=\"false\"");
        blocked.ShouldContain("aria-live=\"off\"");
        key.ShouldNotBeEmpty();
        blocked.ShouldContain($"data-chatbot-announcement-key=\"{key}\"");
        string composer = Regex.Match(html, "<[^>]+data-chatbot-stable-id=\"[^\"]*project-conversation-composer-status\"[^>]*>").Value;
        composer.ShouldContain("aria-live=\"off\"");
        composer.ShouldContain("data-chatbot-live-announced=\"false\"");
        banner.ShouldContain("data-chatbot-repeat-rule=\"OncePerFailureKey\"");
        blocked.ShouldContain("data-chatbot-repeat-rule=\"OncePerFailureKey\"");
        string reason = ChatBotMessageCatalog.Resolve(code).Reason;
        Regex.Matches(WebUtility.HtmlDecode(html), Regex.Escape(reason)).Count.ShouldBeGreaterThanOrEqualTo(2);
        html.ShouldContain($"data-chatbot-stable-id=\"{blockedId}-safe-action\"");
    }

    [Theory]
    [InlineData("stream")]
    [InlineData("association")]
    public async Task EmptyParentShouldKeepTrustedLocalGuidance(string parent)
    {
        string html;
        if (parent == "stream")
        {
            html = await RenderParentAsync<ChatBotConversationStream>(new Dictionary<string, object?>
            {
                [nameof(ChatBotConversationStream.Items)] = Array.Empty<ProjectConversationItemModel>(),
                [nameof(ChatBotConversationStream.EmptyText)] = "Trusted empty conversation explanation",
                [nameof(ChatBotConversationStream.SafeNextAction)] = "Trusted empty conversation action",
            });
            html.ShouldContain("Trusted empty conversation explanation");
            html.ShouldContain("Trusted empty conversation action");
        }
        else
        {
            AssociationReviewModel review = JsonSerializer.Deserialize<AssociationReviewModel>(
                """{"associationId":"association","lifecycleState":"NeedsReview","outcome":"ambiguous","thresholdBand":"review","reasonCodes":[],"candidates":[],"evidence":[],"disabledActionReasonCodes":[],"nextActionReasonCodes":[],"detectedAt":"2026-01-01T00:00:00Z"}""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            html = await RenderParentAsync<AssociationReview>([], association: new(false, false, review, null, "", "", null, null));
            html.ShouldContain("data-chatbot-stable-id=\"association-review-empty\"");
            WebUtility.HtmlDecode(html).ShouldContain("No authorized candidates");
        }
        html.ShouldNotContain("The requested action is unavailable.");
    }

    [Theory]
    [InlineData("private-workspace-error", "DisabledWithReason")]
    [InlineData("invalid_lifecycle_transition", null)]
    [InlineData("audit_unavailable", "Enabled")]
    public async Task WorkspaceFailureShouldOnlyEnableCatalogRetry(string code, string? expectedActionState)
    {
        string html = await RenderParentAsync<ChatBotProjectConversationWorkspace>([], project: new(false, null, code));
        string action = Regex.Match(html, "<span[^>]*data-chatbot-stable-id=\"project-workspace-retryable-safe-action\"[^>]*>").Value;
        if (expectedActionState is null)
        {
            action.ShouldBeEmpty();
        }
        else
        {
            action.ShouldContain($"data-chatbot-action-state=\"{expectedActionState}\"");
        }
        html.ShouldNotContain("data-chatbot-stable-id=\"project-conversation-retryable-safe-action\"");
        html.ShouldNotContain("private-workspace-error");
        html.ShouldContain(code == "audit_unavailable" ? "role=\"status\"" : "role=\"alert\"");
    }

    [Theory]
    [InlineData("Blocked")]
    [InlineData("Stale")]
    [InlineData("Degraded")]
    public async Task WorkspaceBlockedContextShouldKeepLocalReasonAndGuardServerAction(string status)
    {
        ProjectConversationModel conversation = new("project", "Project", null, status, "Current", [], null, false,
            20, "synthetic", "metadata_only", "standard", "v1", "correlation", "private-next-action");
        string html = await RenderParentAsync<ChatBotProjectConversationWorkspace>([], project: new(false, conversation, null));
        WebUtility.HtmlDecode(html).ShouldContain("Conversation context is not current.");
        html.ShouldNotContain("private-next-action");
        WebUtility.HtmlDecode(html).ShouldContain("Review the safe next action for this state.");
        html.ShouldNotContain("data-chatbot-stable-id=\"project-conversation-blocked-safe-action\"");
        if (status is "Blocked" or "Stale")
        {
            WebUtility.HtmlDecode(html).ShouldNotContain("The requested action is unavailable.");
            WebUtility.HtmlDecode(html).ShouldNotContain("A required dependency is temporarily degraded.");
            string composer = Regex.Match(html, "<[^>]+data-chatbot-stable-id=\"[^\"]*project-conversation-composer-status\"[^>]*>").Value;
            composer.ShouldContain("data-chatbot-feedback-state=\"BlockedAction\"");
        }
    }

    /// <summary>Unknown and incompatible acceptance outcomes suppress contradictory completion and retry metadata.</summary>
    /// <param name="code">The incompatible failure code.</param>
    [Theory]
    [InlineData("private-fallback-code")]
    [InlineData(ChatBotMessageCodes.CommandAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionRejected)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionDeferred)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionNeedsReview)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionPropagationPending)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionPropagationComplete)]
    public async Task ContradictoryGenericFailureShouldSuppressResolvedAndRetryMetadata(string code)
    {
        ProjectConversationItemModel item = CreateFailureItem(code, "contradictory") with
        {
            FailureStatus = "resolved", FailureStateKind = "reprocess-created", Retryable = true,
            RetryCount = 17, MaxRetryCount = 99,
            NextRetryAtUtc = DateTimeOffset.Parse("2031-01-01T00:00:00Z", CultureInfo.InvariantCulture),
            LastRetryAtUtc = DateTimeOffset.Parse("2030-01-01T00:00:00Z", CultureInfo.InvariantCulture),
            RetryOperationId = "private-retry-operation", SafeNextAction = "retry-later", ClientAction = "retry-later",
            StatusSummary = new([new("failure", "healthy", "resolved", "command_accepted", "retry-later", new Dictionary<string, string>(),
                false, null, "completed", null, null, null, 17, null, null, null)]),
        };
        string html = await RenderParentAsync<ChatBotFailureStateConversationItem>(new Dictionary<string, object?>
        {
            [nameof(ChatBotFailureStateConversationItem.Item)] = item,
        });
        string text = WebUtility.HtmlDecode(html);
        text.ShouldContain("The requested action is unavailable.");
        text.ShouldContain("Terminal");
        text.ShouldContain("No");
        text.ShouldNotContain("Resolved");
        text.ShouldNotContain("Yes");
        text.ShouldNotContain("Reprocess created");
        html.ShouldNotContain("chatbot-conversation-status-summary");
        html.ShouldNotContain("2031-");
        html.ShouldNotContain("2030-");
        html.ShouldNotContain("private-retry-operation");
        html.ShouldNotContain("retry-later");
        html.ShouldContain("<code class=\"chatbot-code\">generic_denial</code>");
        html.ShouldContain("<code class=\"chatbot-code\">dismiss</code>");
        if (ChatBotMessageCatalog.IsKnown(code))
        {
            text.ShouldNotContain(ChatBotMessageCatalog.Resolve(code).Reason);
            html.ShouldNotContain($"<code class=\"chatbot-code\">{code}</code>");
        }
    }

    /// <summary>Blocked surfaces cannot describe an incompatible acceptance as the current outcome.</summary>
    /// <param name="code">The incompatible acceptance code.</param>
    [Theory]
    [InlineData(ChatBotMessageCodes.CommandAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionRejected)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionDeferred)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionNeedsReview)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionPropagationPending)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionPropagationComplete)]
    public async Task BlockedStateShouldNormalizeIncompatibleAcceptance(string code)
    {
        string html = await RenderParentAsync<ChatBotBlockedState>(new Dictionary<string, object?>
        {
            [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.FailedDependency,
            [nameof(ChatBotBlockedState.MessageCatalogCode)] = code,
            [nameof(ChatBotBlockedState.ReasonText)] = "restricted-acceptance-prose",
            [nameof(ChatBotBlockedState.SafeNextAction)] = "retry-later",
        });

        string text = WebUtility.HtmlDecode(html);
        text.ShouldContain("The requested action is unavailable.");
        text.ShouldContain("Dismiss");
        text.ShouldNotContain(ChatBotMessageCatalog.Resolve(code).Reason);
        html.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");
        html.ShouldContain("aria-live=\"assertive\"");
        html.ShouldNotContain(code);
        html.ShouldNotContain("retry-later");
        html.ShouldNotContain("restricted-acceptance-prose");
    }

    /// <summary>Failure and blocked renderers retain legitimate failure-history outcomes.</summary>
    /// <param name="code">The history-compatible catalog outcome.</param>
    [Theory]
    [InlineData(ChatBotMessageCodes.RetryQueued)]
    [InlineData(ChatBotMessageCodes.RetryAccepted)]
    [InlineData(ChatBotMessageCodes.DuplicateSuppressed)]
    [InlineData(ChatBotMessageCodes.ReprocessCreated)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictCommandExecution)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictMessageIntake)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictCorrection)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictAssociationDecision)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictRetry)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictOutboundDraftCreation)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictOutboundSend)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictParticipantResolution)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictAssociationScoring)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictAssociationThresholdPolicy)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictLowRiskAiAssistance)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictApprovalDecision)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictApprovedAiActionExecution)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictAiActionProposal)]
    public async Task FailureHistoryOutcomesShouldRemainCatalogBackedInBothRenderers(string code)
    {
        string failure = await RenderParentAsync<ChatBotFailureStateConversationItem>(new Dictionary<string, object?>
        {
            [nameof(ChatBotFailureStateConversationItem.Item)] = CreateFailureItem(code, "valid-history"),
        });
        string blocked = await RenderParentAsync<ChatBotBlockedState>(new Dictionary<string, object?>
        {
            [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.FailedDependency,
            [nameof(ChatBotBlockedState.MessageCatalogCode)] = code,
        });

        string banner = await RenderParentAsync<ChatBotStatusBanner>(new Dictionary<string, object?>
        {
            [nameof(ChatBotStatusBanner.Kind)] = ChatBotFeedbackKind.Warning,
            [nameof(ChatBotStatusBanner.MessageCatalogCode)] = code,
        });
        foreach (string html in new[] { failure, blocked, banner })
        {
            WebUtility.HtmlDecode(html).ShouldContain(ChatBotMessageCatalog.Resolve(code).Reason);
            html.ShouldNotContain("The requested action is unavailable.");
            html.ShouldContain(code);
            html.ShouldNotContain("generic_denial");
        }
    }

    /// <summary>Mounted same-code failures announce each new attempt and suppress the repeated attempt.</summary>
    [Fact]
    public async Task MountedBannerShouldActivateEachSameCodeAttemptAndSuppressRepeats()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        try
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            var root = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<BannerTransitionRenderHost>());
            BannerTransitionRenderHost host = BannerTransitionRenderHost.Instance!;

            foreach (string attempt in new[] { "attempt-1", "attempt-2" })
            {
                await host.UpdateAsync(attempt).ConfigureAwait(true);
                string announced = await renderer.Dispatcher.InvokeAsync(root.ToHtmlString).ConfigureAwait(true);
                announced.ShouldContain($"data-chatbot-announcement-key=\"{attempt}-authorization_denied\"");
                announced.ShouldContain("data-chatbot-live-announced=\"true\"");
                announced.ShouldContain("aria-live=\"assertive\"");

                await host.UpdateAsync(attempt).ConfigureAwait(true);
                string repeated = await renderer.Dispatcher.InvokeAsync(root.ToHtmlString).ConfigureAwait(true);
                repeated.ShouldContain("data-chatbot-live-announced=\"false\"");
                repeated.ShouldContain("aria-live=\"off\"");
                repeated.ShouldContain("The operation is not available to this caller.");
            }
        }
        finally
        {
            BannerTransitionRenderHost.Clear();
            CultureInfo.CurrentUICulture = original;
        }
    }

    /// <summary>Passive history stays silent without consuming the interactive announcement key.</summary>
    [Fact]
    public async Task PassiveHistoryShouldStayQuietWhileInteractiveFailureAnnouncesOnce()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        try
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            ProjectConversationItemModel item = CreateFailureItem(ChatBotMessageCodes.FailedCommand, "history-failure") with
            {
                FailureStatus = "terminal", SafeNextAction = "escalate", ClientAction = "escalate",
            };
            string history = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotConversationStream>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotConversationStream.Items)] = new[] { item },
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);
            string article = Regex.Match(history, "<[^>]+data-chatbot-failure-announcement=\"history-failure\"[^>]*>").Value;
            article.ShouldContain("role=\"alert\"");
            article.ShouldContain("aria-live=\"off\"");
            EventCallback callback = EventCallback.Factory.Create(this, () => { });
            async Task<string> Interactive() => await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<ChatBotFailureStateConversationItem>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotFailureStateConversationItem.Item)] = item,
                    [nameof(ChatBotFailureStateConversationItem.OnNextAction)] = callback,
                })).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);
            string first = await Interactive();
            string repeated = await Interactive();
            Regex.Match(first, "<[^>]+data-chatbot-failure-announcement=\"history-failure\"[^>]*>").Value.ShouldContain("aria-live=\"assertive\"");
            Regex.Match(repeated, "<[^>]+data-chatbot-failure-announcement=\"history-failure\"[^>]*>").Value.ShouldContain("aria-live=\"off\"");
            repeated.ShouldContain("role=\"alert\"");
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public async Task StreamShouldAnnounceNewCurrentPageFailureOnceAndKeepHistoryQuiet()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        try
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            var root = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<StreamTransitionRenderHost>());
            StreamTransitionRenderHost host = StreamTransitionRenderHost.Instance!;
            ProjectConversationItemModel current = CreateFailureItem(ChatBotMessageCodes.FailedCommand, "current-failure");
            ProjectConversationItemModel history = CreateFailureItem(ChatBotMessageCodes.FailedCommand, "old-failure");
            IReadOnlySet<string> currentIds = new HashSet<string>(StringComparer.Ordinal) { current.ItemId };

            await host.UpdateAsync([current, history], currentIds).ConfigureAwait(true);
            string first = await renderer.Dispatcher.InvokeAsync(root.ToHtmlString).ConfigureAwait(true);
            Regex.Match(first, "<[^>]+data-chatbot-failure-announcement=\"current-failure\"[^>]*>").Value
                .ShouldContain("aria-live=\"assertive\"");
            Regex.Match(first, "<[^>]+data-chatbot-failure-announcement=\"old-failure\"[^>]*>").Value
                .ShouldContain("aria-live=\"off\"");

            await host.UpdateAsync([current, history], currentIds).ConfigureAwait(true);
            string repeated = await renderer.Dispatcher.InvokeAsync(root.ToHtmlString).ConfigureAwait(true);
            Regex.Match(repeated, "<[^>]+data-chatbot-failure-announcement=\"current-failure\"[^>]*>").Value
                .ShouldContain("aria-live=\"off\"");
        }
        finally
        {
            StreamTransitionRenderHost.Clear();
            CultureInfo.CurrentUICulture = original;
        }
    }

    /// <summary>Stored terminality retains recovery guidance and disables a contradictory retry permission.</summary>
    [Fact]
    public async Task StoredTerminalAttachmentShouldKeepRecoveryRuleAndDenyRetryCallback()
    {
        ProjectConversationItemModel item = CreateFailureItem(ChatBotMessageCodes.FailedAttachment, "terminal-attachment") with
        {
            FailureStatus = "Terminal", FailureStateKind = "terminal-failure", Retryable = true,
            SafeNextAction = "retry-later", ClientAction = "retry-later",
        };
        string html = await RenderParentAsync<ChatBotFailureStateConversationItem>(new Dictionary<string, object?>
        {
            [nameof(ChatBotFailureStateConversationItem.Item)] = item,
            [nameof(ChatBotFailureStateConversationItem.OnNextAction)] = EventCallback.Factory.Create(this, () => { }),
        });
        WebUtility.HtmlDecode(html).ShouldContain("Terminal states stay append-only");
        html.ShouldContain("role=\"alert\"");
        html.ShouldContain("aria-disabled=\"true\"");
        html.ShouldContain("<code class=\"chatbot-code\">none</code>");
        WebUtility.HtmlDecode(html).ShouldNotContain("Yes");
    }


    [Theory]
    [InlineData(ChatBotMessageCodes.GenericDenial, false)]
    [InlineData(ChatBotMessageCodes.DependencyDegraded, true)]
    [InlineData(ChatBotMessageCodes.AuditUnavailable, true)]
    [InlineData("restricted-stream-error", false)]
    [InlineData(ChatBotMessageCodes.RetryQueued, true)]
    public async Task StreamingFailureShouldOnlySuggestCatalogRetry(string code, bool retry)
    {
        ProjectConversationModel conversation = new("project", "Project", null, "Current", "Current", [], null, false,
            20, "synthetic", "metadata_only", "standard", "v1", "correlation", "none");
        string html = await RenderParentAsync<ChatBotProjectConversationWorkspace>([],
            project: new ProjectConversationState(false, conversation, null) with { StreamingErrorCode = code });
        string text = WebUtility.HtmlDecode(html);
        if (retry) text.ShouldContain("Retry response refresh.");
        else text.ShouldNotContain("Retry response refresh.");
        html.ShouldNotContain("restricted-stream-error");
    }

    [Fact]
    public async Task UnknownBlockedAccessibleLabelShouldUseVisibleHeadline()
    {
        string html = await RenderParentAsync<ChatBotBlockedState>(new()
        {
            [nameof(ChatBotBlockedState.Reason)] = ChatBotBlockedReason.FailedDependency,
            [nameof(ChatBotBlockedState.MessageCatalogCode)] = "restricted-blocked-code",
        });
        string label = WebUtility.HtmlDecode(Regex.Match(html, "aria-label=\"([^\"]+)\"").Groups[1].Value);
        label.ShouldStartWith("Request unavailable.");
        label.ShouldNotContain("Dependency failed");
        string article = await RenderParentAsync<ChatBotFailureStateConversationItem>(new()
        {
            [nameof(ChatBotFailureStateConversationItem.Item)] = CreateFailureItem(ChatBotMessageCodes.FailedCommand, "passive-article"),
        });
        string tag = Regex.Match(article, "<article[^>]*>").Value;
        tag.ShouldNotContain("role=");
        tag.ShouldNotContain("aria-live=");
        tag.ShouldNotContain("aria-atomic=");
    }


    [Fact]
    public async Task MissingLocaleAssociationFailureShouldWithholdRetry()
    {
        string html = await RenderParentAsync<AssociationReview>([],
            association: new(false, false, null, null, "", "", "TIMEOUT", null, AssociationReviewErrorScope.Load), culture: "es");
        html.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");
        html.ShouldNotContain("data-chatbot-stable-id=\"association-review-retry\"");
        html.ShouldNotContain("data-chatbot-action-state=\"Enabled\"");
        WebUtility.HtmlDecode(html).ShouldContain("The requested action is unavailable.");
    }

    [Fact]
    public async Task GovernedOperationsLegacyProblemShouldRenderCanonicalGuidance()
    {
        SafeOutcomeRenderDispatcher dispatcher = new();
        await new GovernedOperationsEffects(new GovernedOperationService(new FakeChatBotClient("TIMEOUT")))
            .HandleSubmitAsync(dispatcher).ConfigureAwait(true);
        string code = dispatcher.Actions.OfType<GovernedNoteSubmissionFailedAction>().Single().Error;
        code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        string html = await RenderParentAsync<GovernedOperations>([], governed: new(false, null, code));
        WebUtility.HtmlDecode(html).ShouldContain("A required dependency is temporarily degraded.");
        html.ShouldNotContain("TIMEOUT");
        html.ShouldNotContain("The requested action is unavailable.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MissingFacetHealthShouldRenderUnknownWarning(string? health)
    {
        ProjectConversationItemStatusFacetModel facet = new(
            "command", health!, "unknown", "status_unknown", "none",
            new Dictionary<string, string>(), false, null, null, null, null, null, null, null, null, null);
        string html = await RenderParentAsync<ChatBotConversationItemStatusSummary>(new()
        {
            [nameof(ChatBotConversationItemStatusSummary.ItemId)] = "item-001",
            [nameof(ChatBotConversationItemStatusSummary.Summary)] = new ProjectConversationItemStatusSummaryModel([facet]),
        });
        WebUtility.HtmlDecode(html).ShouldContain("Unknown");
        html.ShouldContain("chatbot-conversation-status-summary__health");
        html.ShouldContain("warning");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task BlankAssociationIdShouldDispatchLoadAndRenderItsSafeFailure(string associationId)
    {
        SafeOutcomeRenderDispatcher dispatcher = new();
        Dictionary<string, object?> parameters = new()
        {
            [nameof(AssociationReview.AssociationId)] = associationId,
        };
        await RenderParentAsync<AssociationReview>(parameters, dispatcher: dispatcher);
        LoadAssociationReviewAction load = dispatcher.Actions.OfType<LoadAssociationReviewAction>().ShouldHaveSingleItem();
        load.AssociationId.ShouldBe(associationId);
        AssociationReviewState empty = new(false, false, null, null, "", "", null, null);
        AssociationReviewState failed = AssociationReviewReducers.ReduceFailed(
            AssociationReviewReducers.ReduceLoad(empty, load), new AssociationReviewFailedAction("association-id-required"));
        string html = await RenderParentAsync<AssociationReview>(parameters, association: failed);
        html.ShouldContain("data-chatbot-stable-id=\"association-review-failed\"");
        html.ShouldContain("data-chatbot-feedback-state=\"TerminalPolicyFailure\"");
        html.ShouldNotContain("data-chatbot-stable-id=\"association-review-retry\"");
        WebUtility.HtmlDecode(html).ShouldContain("The requested action is unavailable.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(999)]
    public async Task RestrictedComparisonShouldNotRenderEvidenceMetadata(int redaction)
    {
        global::Hexalith.ChatBot.UI.E2E.Tests.FakeChatBotClient client = new()
        {
            AssociationEvidenceOverride = new Hexalith.ChatBot.Client.Generated.AssociationEvidenceReference
            {
                EvidenceReference = "private-reference-sentinel",
                EvidenceFingerprint = "private-fingerprint-sentinel",
                EvidenceKind = "private-kind-sentinel",
                RedactionState = (Hexalith.ChatBot.Client.Generated.AssociationEvidenceReferenceRedactionState)redaction,
            },
        };
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        AssociationReviewModel review = await new AssociationReviewService(client, provider.GetRequiredService<ChatBotUiTextLocalizer>())
            .GetAssociationReviewAsync("association-001", TestContext.Current.CancellationToken).ConfigureAwait(true);
        string html = await RenderParentAsync<ChatBotAssociationEvidenceComparison>(new()
        {
            [nameof(ChatBotAssociationEvidenceComparison.Candidate)] = review.Candidates.Single(),
        });
        html.ShouldNotContain("private-reference-sentinel");
        html.ShouldNotContain("private-fingerprint-sentinel");
        html.ShouldNotContain("private-kind-sentinel");
        html.ShouldContain("Evidence restricted");
    }

    private static ProjectConversationItemModel CreateFailureItem(string code, string id)
        => JsonSerializer.Deserialize<ProjectConversationItemModel>(
            """{"itemId":"failure","kind":"FailureState","actorKind":"System","actorLabel":"Worker","occurredAt":"2026-01-01T00:00:00Z","failureStatus":"failed","messageDetailVisibility":"metadata_only"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))! with { ItemId = id, MessageCatalogCode = code };

    private static async Task<string> RenderParentAsync<T>(Dictionary<string, object?> parameters,
        ProjectConversationState? project = null, AssociationReviewState? association = null,
        GovernedOperationsState? governed = null, OperationalDashboardsState? dashboards = null, string culture = "en",
        SafeOutcomeRenderDispatcher? dispatcher = null) where T : IComponent
    {
        if (typeof(T) == typeof(ChatBotProjectConversationWorkspace))
        {
            parameters.TryAdd(nameof(ChatBotProjectConversationWorkspace.HeadingText), "Project conversation");
        }

        CultureInfo original = CultureInfo.CurrentUICulture;
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddFluxor(options => options.ScanAssemblies(typeof(VersionedSafeOutcomeRenderTests).Assembly));
            services.AddSingleton<IState<ProjectConversationState>>(new SafeOutcomeRenderState<ProjectConversationState>(project ?? new(false, null, null)));
            services.AddSingleton<IState<AssociationReviewState>>(new SafeOutcomeRenderState<AssociationReviewState>(association ?? new(false, false, null, null, "", "", null, null)));
            services.AddSingleton<IState<GovernedOperationsState>>(new SafeOutcomeRenderState<GovernedOperationsState>(governed ?? new(false, null, null)));
            services.AddSingleton<IState<OperationalDashboardsState>>(new SafeOutcomeRenderState<OperationalDashboardsState>(dashboards ?? new(false, null, null)));
            services.AddSingleton<IDispatcher>(dispatcher ?? new SafeOutcomeRenderDispatcher());
            services.AddSingleton(new ChatBotHubEndpoint(new Uri("http://127.0.0.1")));
            services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            ServiceProvider provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(true))
            {
                using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
                return await renderer.Dispatcher.InvokeAsync(async () =>
                    (await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters)).ConfigureAwait(true)).ToHtmlString()).ConfigureAwait(true);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = original;
        }
    }

    private sealed class StaticRenderJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }
}
