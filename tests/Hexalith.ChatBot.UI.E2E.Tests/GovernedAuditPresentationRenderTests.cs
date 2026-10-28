using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;

using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Components.Pages;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.GovernedOperations;
using Hexalith.ChatBot.UI.State.ProjectConversation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Verifies localized audit presentation and the real shared Fluent metadata row.</summary>
public sealed class GovernedAuditPresentationRenderTests
{
    [Theory]
    [InlineData("en", "Audit phase", "Audit outcome", "Surface origin")]
    [InlineData("fr", "Phase d’audit", "Résultat d’audit", "Surface d’origine")]
    public async Task GovernedPageShouldLocalizeStructuredAuditReadFromTheClient(
        string culture, string phaseLabel, string outcomeLabel, string originLabel)
    {
        OperationOutcome outcome = await new GovernedOperationService(new FakeChatBotClient())
            .SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);
        string html = await RenderAsync<GovernedOperations>([], culture, outcome);
        string audit = WebUtility.HtmlDecode(Regex.Match(html,
            "<ul[^>]*data-chatbot-audit-history=\"metadata-only\"[^>]*>[\\s\\S]*?</ul>").Value);

        audit.ShouldNotBeEmpty();
        audit.ShouldContain(phaseLabel);
        audit.ShouldContain(outcomeLabel);
        audit.ShouldContain(originLabel);
        audit.ShouldContain(">PostCommit</code>");
        audit.ShouldContain(">allow</code>");
        audit.ShouldContain(">proposed</code>");
        audit.ShouldContain(">Committed</code>");
        audit.ShouldContain(">Ui</code>");
        audit.ShouldContain($">{outcome.AuditHistory.Entries.Single().CorrelationId}</code>");
        audit.ShouldNotContain("audit:");
        audit.ShouldNotContain("origin:");
        audit.ShouldNotContain("correlation:");
        if (culture == "fr")
        {
            audit.ShouldNotContain("Audit phase");
            audit.ShouldNotContain("Surface origin");
        }
    }

    [Theory]
    [InlineData("en", OperationAuditStatus.Reconciling, "pending", "Awaiting the post-commit audit record.")]
    [InlineData("fr", OperationAuditStatus.Reconciling, "pending", "En attente de l’enregistrement d’audit après validation.")]
    [InlineData("en", OperationAuditStatus.Committed, "empty", "No audit records are available.")]
    [InlineData("fr", OperationAuditStatus.Committed, "empty", "Aucun enregistrement d’audit n’est disponible.")]
    public async Task EmptyHistoryShouldRenderLocalizedGuidanceAndItsOwnReadStatus(
        string culture, OperationAuditStatus status, string state, string guidance)
    {
        FakeChatBotClient client = new()
        {
            AuditHistoryOverride = new OperationAuditHistory { AuditStatus = status, Entries = [] },
        };
        OperationOutcome outcome = await new GovernedOperationService(client)
            .SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);
        string html = await RenderAsync<GovernedOperations>([], culture, outcome);
        string text = WebUtility.HtmlDecode(html);

        html.ShouldContain($"data-chatbot-audit-history=\"{state}\"");
        text.ShouldContain(guidance);
        text.ShouldContain($">{status}</code>");
        html.ShouldNotContain("data-chatbot-audit-history=\"metadata-only\"");
        text.ShouldNotContain("awaiting post-commit record");
        if (status is OperationAuditStatus.Committed)
        {
            text.ShouldNotContain("Awaiting the post-commit audit record.");
            text.ShouldNotContain("En attente de l’enregistrement d’audit après validation.");
            html.ShouldNotContain("data-chatbot-audit-history=\"pending\"");
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task GovernedAuditShouldWithholdOmittedTransportFieldsThroughServiceAndPage(string culture)
    {
        string[] withheld =
        [
            "private-history-operation-sentinel",
            "private-resource-secret-project-sentinel",
            "private-reason-payload-sentinel",
            "private-transition-/home/secret-sentinel",
            "2099-11-23",
        ];
        FakeChatBotClient client = new()
        {
            AuditHistoryOverride = new OperationAuditHistory
            {
                OperationId = withheld[0],
                AuditStatus = OperationAuditStatus.Committed,
                Entries =
                [
                    new AuditHistoryEntry
                    {
                        Phase = AuditHistoryPhase.PostCommit,
                        Decision = "allow",
                        Outcome = "proposed",
                        SurfaceOrigin = SurfaceOrigin.Ui,
                        CorrelationId = "safe-correlation-opaque",
                        ResourceId = withheld[1],
                        ReasonCode = withheld[2],
                        StateTransition = withheld[3],
                        RecordedAt = new DateTimeOffset(2099, 11, 23, 8, 0, 0, TimeSpan.Zero),
                        RedactionDecision = AuditHistoryEntryRedactionDecision.Metadata_only,
                    },
                ],
            },
        };

        OperationOutcome outcome = await new GovernedOperationService(client)
            .SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);
        string serialized = JsonSerializer.Serialize(outcome.AuditHistory);
        string html = await RenderAsync<GovernedOperations>([], culture, outcome);
        string decoded = WebUtility.HtmlDecode(html);
        foreach (string sentinel in withheld)
        {
            serialized.ShouldNotContain(sentinel);
            decoded.ShouldNotContain(sentinel);
        }

        foreach (string omittedField in new[] { "ResourceId", "ReasonCode", "StateTransition", "RecordedAt", "RedactionDecision" })
        {
            serialized.ShouldNotContain(omittedField);
        }

        decoded.ShouldContain("safe-correlation-opaque");
        decoded.ShouldContain(">allow</code>");
        decoded.ShouldContain(">proposed</code>");
    }

    [Theory]
    [InlineData("en", "Requested at", "Decided at", "Outcome at", "Proposal ID")]
    [InlineData("fr", "Demandé à", "Décidé à", "Résultat à", "ID de proposition")]
    public async Task ApprovalCallerShouldFormatTimestampInActiveCultureAndSuppressBlankRows(
        string culture, string requestedLabel, string decidedLabel, string outcomeLabel, string proposalLabel)
    {
        DateTimeOffset timestamp = new(2026, 10, 28, 12, 30, 0, TimeSpan.FromHours(2));
        ProjectConversationModel conversation = await new ProjectConversationService(new FakeChatBotClient())
            .GetProjectConversationAsync("project-alpha", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel item = conversation.Items.First() with
        {
            ApprovalRequestedAtUtc = timestamp,
            ApprovalDecidedAtUtc = null,
            ApprovalOutcomeAtUtc = null,
            ApprovalProposalId = " ",
        };
        string html = await RenderAsync<ChatBotApprovalConversationItem>(new()
        {
            [nameof(ChatBotApprovalConversationItem.Item)] = item,
        }, culture);

        AssertCallerTimestamp(html, timestamp, culture);
        string decoded = WebUtility.HtmlDecode(html);
        decoded.ShouldContain(requestedLabel);
        decoded.ShouldNotContain(decidedLabel);
        decoded.ShouldNotContain(outcomeLabel);
        decoded.ShouldNotContain(proposalLabel);

        string absent = await RenderAsync<ChatBotApprovalConversationItem>(new()
        {
            [nameof(ChatBotApprovalConversationItem.Item)] = item with { ApprovalRequestedAtUtc = null },
        }, culture);
        WebUtility.HtmlDecode(absent).ShouldNotContain(requestedLabel);
        WebUtility.HtmlDecode(absent).ShouldNotContain(timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("en", "Decided at", "Decision actor")]
    [InlineData("fr", "Décidé à", "Acteur de décision")]
    public async Task WhyProjectCallerShouldFormatTimestampInActiveCultureAndSuppressBlankRows(
        string culture, string decidedLabel, string actorLabel)
    {
        DateTimeOffset timestamp = new(2026, 10, 28, 12, 30, 0, TimeSpan.FromHours(2));
        ProjectAssociationWhyPanelModel panel = await new ProjectConversationService(new FakeChatBotClient())
            .GetAssociationWhyPanelAsync("project-alpha", "association-alpha", TestContext.Current.CancellationToken);
        panel = panel with { DecisionTimestamp = timestamp, DecisionActorId = " ", DecisionActorType = null };
        string html = await RenderAsync<ChatBotWhyProjectPanel>(new()
        {
            [nameof(ChatBotWhyProjectPanel.Panel)] = panel,
        }, culture);

        AssertCallerTimestamp(html, timestamp, culture);
        string decoded = WebUtility.HtmlDecode(html);
        decoded.ShouldContain(decidedLabel);
        decoded.ShouldNotContain(actorLabel);

        string absent = await RenderAsync<ChatBotWhyProjectPanel>(new()
        {
            [nameof(ChatBotWhyProjectPanel.Panel)] = panel with { DecisionTimestamp = default },
        }, culture);
        WebUtility.HtmlDecode(absent).ShouldNotContain(decidedLabel);
        absent.ShouldNotContain("<time");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SharedRowShouldEncodeLabelTextAndValueAndPreserveTokenPresentation(bool isCode)
    {
        string html = await RenderAsync<ChatBotMetadataRow>(new()
        {
            [nameof(ChatBotMetadataRow.Label)] = "Label <untrusted-label>",
            [nameof(ChatBotMetadataRow.Text)] = "Description <untrusted-description>",
            [nameof(ChatBotMetadataRow.Value)] = "value <untrusted-value>",
            [nameof(ChatBotMetadataRow.IsCode)] = isCode,
        });

        html.ShouldContain("&lt;untrusted-label&gt;");
        html.ShouldContain("&lt;untrusted-description&gt;");
        html.ShouldContain("&lt;untrusted-value&gt;");
        html.ShouldNotContain("<untrusted-");
        html.Contains("<code class=\"chatbot-code\">", StringComparison.Ordinal).ShouldBe(isCode);
    }

    [Fact]
    public async Task SharedTimestampRowShouldRetainInvariantUtcMachineValueWithoutMonospace()
    {
        DateTimeOffset timestamp = new(2026, 10, 28, 12, 30, 0, TimeSpan.FromHours(2));
        string html = await RenderAsync<ChatBotMetadataRow>(new()
        {
            [nameof(ChatBotMetadataRow.Label)] = "Date",
            [nameof(ChatBotMetadataRow.Value)] = "28 octobre 2026 à 12:30",
            [nameof(ChatBotMetadataRow.Timestamp)] = timestamp,
        }, "fr");

        WebUtility.HtmlDecode(html).ShouldContain("datetime=\"2026-10-28T10:30:00.0000000+00:00\"");
        WebUtility.HtmlDecode(html).ShouldContain("28 octobre 2026 à 12:30");
        html.ShouldNotContain("<code");
        html.ShouldNotContain("chatbot-code");
    }

    private static void AssertCallerTimestamp(string html, DateTimeOffset timestamp, string culture)
    {
        string decoded = WebUtility.HtmlDecode(html);
        string invariant = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        Match rendered = Regex.Matches(decoded, "<time\\b[^>]*datetime=\"(?<machine>[^\"]+)\"[^>]*>(?<text>[\\s\\S]*?)</time>")
            .Cast<Match>().Single(match => match.Groups["machine"].Value == invariant);
        rendered.Groups["text"].Value.Trim().ShouldBe(timestamp.ToLocalTime().ToString("f", CultureInfo.GetCultureInfo(culture)));
        rendered.Value.ShouldNotContain("chatbot-code");
    }

    private static async Task<string> RenderAsync<T>(Dictionary<string, object?> parameters,
        string culture = "en", OperationOutcome? outcome = null) where T : IComponent
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddFluxor(options => options.ScanAssemblies(typeof(GovernedAuditPresentationRenderTests).Assembly));
            services.AddSingleton<IState<GovernedOperationsState>>(new SafeOutcomeRenderState<GovernedOperationsState>(new(false, outcome, null)));
            services.AddSingleton<IDispatcher>(new SafeOutcomeRenderDispatcher());
            services.AddSingleton<IJSRuntime, AuditPresentationJsRuntime>();
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton(new ProjectConversationService(new FakeChatBotClient()));
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            ServiceProvider provider = services.BuildServiceProvider();
            await using var cleanup = provider.ConfigureAwait(true);
            using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
            return await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters)).ConfigureAwait(true))
                .ToHtmlString()).ConfigureAwait(true);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
