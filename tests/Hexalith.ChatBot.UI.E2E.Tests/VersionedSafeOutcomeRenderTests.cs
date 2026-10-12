using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;

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

    private sealed class StaticRenderJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }
}
