using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.ProjectConversation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Exercises actual repeated conversation components and their accessibility references.</summary>
public sealed class ConversationAccessibilityRenderingTests
{
    /// <summary>Keeps draft entry and submission unavailable until the interactive circuit can handle them.</summary>
    [Fact]
    public async Task PrerenderedComposersShouldDisableInputAndActions()
    {
        List<string> renders = await RenderPairAsync<ChatBotGovernedComposer>(_ => new(), []);
        string[] composers = ComposerSections(renders[0]).ToArray();
        composers.Length.ShouldBe(2);
        foreach (string composer in composers)
        {
            MatchCollection controls = Regex.Matches(composer, @"<fluent-(?:button|text-?area)\b[^>]*>");
            controls.Count.ShouldBe(4);
            foreach (Match control in controls)
            {
                Regex.IsMatch(control.Value, @"\sdisabled(?=[=\s>])").ShouldBeTrue(control.Value);
            }
        }
    }

    /// <summary>Validation and status transitions preserve each composer's identity and references.</summary>
    [Fact]
    public async Task RepeatedComposersShouldKeepLocalReferencesAcrossValidationStatusAndReceiptTransitions()
    {
        ProjectConversationSubmissionReceiptModel receipt = new(
            ProjectConversationComposerMode.Message, "command-safe", "correlation-safe", null,
            "Accepted", DateTimeOffset.Parse("2026-10-28T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture), "none");
        Dictionary<string, object?>[] transitions =
        [
            new()
            {
                [nameof(ChatBotGovernedComposer.ValidationErrorCode)] = "message-required",
                [nameof(ChatBotGovernedComposer.SubmissionErrorCode)] = "authorization_denied",
            },
            new()
            {
                [nameof(ChatBotGovernedComposer.ValidationErrorCode)] = null,
                [nameof(ChatBotGovernedComposer.SubmissionErrorCode)] = null,
                [nameof(ChatBotGovernedComposer.PendingSubmission)] = receipt,
            },
            new()
            {
                [nameof(ChatBotGovernedComposer.PendingSubmission)] = null,
            },
        ];
        List<string> renders = await RenderPairAsync<ChatBotGovernedComposer>(_ => new(), transitions);
        string[] inputIds = InputIds(renders[0]);
        inputIds.Length.ShouldBe(2);
        foreach (string html in renders)
        {
            AssertUniqueIds(html);
            InputIds(html).ShouldBe(inputIds);
            foreach (string composer in ComposerSections(html))
            {
                AssertReferencesResolve(composer);
            }
        }

        foreach (string html in renders.Skip(1))
        {
            PersistentIds(html).ShouldBe(PersistentIds(renders[0]));
        }

        Regex.Matches(renders[1], "data-chatbot-composer-validation=\"true\"").Count.ShouldBe(2);
        Regex.Matches(renders[1], "aria-errormessage=\"").Count.ShouldBe(2);
        Regex.Matches(renders[2], "data-chatbot-pending-correlation-id=\"correlation-safe\"").Count.ShouldBe(2);
        renders[2].ShouldNotContain("aria-errormessage=");
        renders[3].ShouldNotContain("data-chatbot-stable-id=");
    }

    /// <summary>Caller-owned focus targets remain stable and point to the actual labelled Fluent input.</summary>
    [Fact]
    public async Task ExplicitFocusTargetsShouldIdentifyOnlyTheirOwnComposerInput()
    {
        List<string> renders = await RenderPairAsync<ChatBotGovernedComposer>(index => new()
        {
            [nameof(ChatBotGovernedComposer.InputId)] = $"focus-target-{index}",
        }, [new() { [nameof(ChatBotGovernedComposer.IsSubmitting)] = true }]);
        foreach (string html in renders)
        {
            InputIds(html).ShouldBe(["focus-target-0", "focus-target-1"]);
            AssertUniqueIds(html);
            foreach (string composer in ComposerSections(html))
            {
                AssertReferencesResolve(composer);
            }
        }
    }

    /// <summary>Empty-state headings are subordinate to the stream heading and remain instance-local.</summary>
    [Fact]
    public async Task RepeatedEmptyStreamsShouldUseUniqueStableHeadingsWithNestedReasonAtLevelThree()
    {
        List<string> renders = await RenderPairAsync<ChatBotConversationStream>(_ => new(),
            [new() { [nameof(ChatBotConversationStream.Title)] = "Updated conversation" }]);
        foreach (string html in renders)
        {
            AssertUniqueIds(html);
            AssertReferencesResolve(html);
            Regex.Matches(html, "<h([23])(?: |>)").Select(match => match.Groups[1].Value)
                .ShouldBe(["2", "3", "2", "3"]);
        }

        Ids(renders[1]).ShouldBe(Ids(renders[0]));
    }

    /// <summary>Two populated streams with identical items keep every item-derived ID and reference local.</summary>
    /// <param name="explicitScope">Whether the caller supplies the ID scope instead of the stream instance default.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedPopulatedStreamsShouldScopeItemIdsAndResolveReferencesPerStream(bool explicitScope)
    {
        ProjectConversationItemModel[] items = PopulatedStreamItems();
        List<string> renders = await RenderPairAsync<ChatBotConversationStream>(index => explicitScope
            ? new()
            {
                [nameof(ChatBotConversationStream.Items)] = items,
                [nameof(ChatBotConversationStream.IdScope)] = $"feed-{index}",
            }
            : new() { [nameof(ChatBotConversationStream.Items)] = items },
            [new() { [nameof(ChatBotConversationStream.Title)] = "Updated conversation" }]);
        foreach (string html in renders)
        {
            AssertUniqueIds(html);
            string[] streams = StreamSections(html);
            streams.Length.ShouldBe(2);
            for (int index = 0; index < streams.Length; index++)
            {
                string stream = streams[index];
                AssertReferencesResolve(stream);
                string titleId = Regex.Match(stream, "aria-labelledby=\"([^\"]+)\"").Groups[1].Value;
                string scope = explicitScope ? $"feed-{index}" : titleId[..^"-title".Length];
                Ids(stream).Where(id => id != titleId).ShouldBe(
                [
                    $"chatbot-evidence-item:participant-{scope}-participant-evidence-reason",
                    $"participant-item:participant-{scope}-unavailable-reason",
                    $"chatbot-evidence-item:participant-{scope}-participant-evidence-chip-reason",
                    $"chatbot-evidence-item:attachment-{scope}-attachment-evidence-reason",
                    $"attachment-item:attachment-{scope}-unavailable-reason",
                    $"chatbot-evidence-item:attachment-{scope}-attachment-evidence-chip-reason",
                    $"chatbot-evidence-item:approval-{scope}-reason",
                    $"chatbot-approval-item:approval-{scope}-approve-reason",
                    $"item:failure-{scope}-failure-announcement",
                    $"item:failure-{scope}-safe-action-disabled-reason",
                ]);
            }

            // The failure announcement data attribute stays unscoped, like its deduplication key.
            Regex.Matches(html, "data-chatbot-failure-announcement=\"item:failure\"").Count.ShouldBe(2);
        }

        Ids(renders[1]).ShouldBe(Ids(renders[0]));
    }

    /// <summary>Route-level blocked-state headings retain their default level two.</summary>
    [Fact]
    public async Task StandaloneBlockedStateShouldKeepHeadingAtLevelTwo()
    {
        List<string> renders = await RenderPairAsync<ChatBotBlockedState>(index => new()
        {
            [nameof(ChatBotBlockedState.StableId)] = $"standalone-blocked-{index}",
        }, []);
        Regex.Matches(renders[0], "<h([1-6])(?: |>)").Select(match => match.Groups[1].Value)
            .ShouldBe(["2", "2"]);
    }

    /// <summary>A retained outcome keeps its announcement identity when its composer is remounted.</summary>
    /// <param name="accepted">Whether the retained outcome is an accepted receipt rather than a failure.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetainedOutcomeShouldNotAnnounceAgainAfterComposerRemount(bool accepted)
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Dictionary<string, object?> parameters = accepted
                ? new()
                {
                    [nameof(ChatBotGovernedComposer.PendingSubmission)] = new ProjectConversationSubmissionReceiptModel(
                        ProjectConversationComposerMode.Message, "retained-command", "retained-correlation", null,
                        "Accepted", new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.Zero), "none"),
                }
                : new()
                {
                    [nameof(ChatBotGovernedComposer.SubmissionErrorCode)] = "authorization_denied",
                };
            ServiceProvider provider = CreateProvider();
            await using var providerLifetime = provider.ConfigureAwait(false);
            List<string> renders = [];
            for (int mount = 0; mount < 2; mount++)
            {
                // Disposing each renderer destroys its component tree; the circuit's announcement state survives.
                using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
                string html = await renderer.Dispatcher.InvokeAsync(async () =>
                {
                    var root = await renderer.RenderComponentAsync<ChatBotGovernedComposer>(ParameterView.FromDictionary(parameters))
                        .ConfigureAwait(true);
                    return root.ToHtmlString();
                }).ConfigureAwait(true);
                renders.Add(html);
            }

            string firstBanner = StatusBanner(renders[0]);
            string secondBanner = StatusBanner(renders[1]);
            firstBanner.ShouldContain("data-chatbot-live-announced=\"true\"");
            secondBanner.ShouldContain("data-chatbot-live-announced=\"false\"");
            secondBanner.ShouldContain("aria-live=\"off\"");
            string firstKey = Regex.Match(firstBanner, "data-chatbot-announcement-key=\"([^\"]+)\"").Groups[1].Value;
            firstKey.ShouldBe(accepted
                ? "project-conversation-composer-initial-attempt-0"
                : "project-conversation-composer-initial-attempt-0-authorization_denied");
            secondBanner.ShouldContain($"data-chatbot-announcement-key=\"{firstKey}\"");
            InputIds(renders[0]).Single().ShouldNotBe(InputIds(renders[1]).Single(), "a fresh DOM identity must not create a fresh outcome announcement");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private static string StatusBanner(string html)
        => Regex.Match(html, "<[^>]+data-chatbot-stable-id=\"[^\"]*project-conversation-composer-status\"[^>]*>").Value;

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddFluentUIComponents();
        services.AddSingleton<IJSRuntime, AssociationRenderJsRuntime>();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        services.AddSingleton<ChatBotCultureFormatter>();
        services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
        services.AddSingleton(new ProjectConversationService(DispatchProxy.Create<IChatBotClient, RenderOnlyChatBotClient>()));
        return services.BuildServiceProvider();
    }

    private static async Task<List<string>> RenderPairAsync<T>(Func<int, Dictionary<string, object?>> parameters,
        IReadOnlyList<Dictionary<string, object?>> transitions)
        where T : IComponent
    {
        ServiceProvider provider = CreateProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
        List<IComponent> instances = [];
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<FluentStack>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(FluentStack.ChildContent)] = (RenderFragment)(builder =>
                {
                    for (int index = 0; index < 2; index++)
                    {
                        builder.OpenComponent<T>(0);
                        foreach ((string name, object? value) in parameters(index))
                        {
                            builder.AddAttribute(1, name, value);
                        }

                        builder.AddComponentReferenceCapture(2, instance => instances.Add((IComponent)instance));
                        builder.CloseComponent();
                    }
                }),
            })).ConfigureAwait(true);
            List<string> renders = [root.ToHtmlString()];
            foreach (IComponent instance in instances)
            {
                await instance.SetParametersAsync(ParameterView.Empty).ConfigureAwait(true);
            }

            Ids(root.ToHtmlString()).ShouldBe(Ids(renders[0]), "IDs must survive a rerender without parameter changes");
            foreach (Dictionary<string, object?> transition in transitions)
            {
                foreach (IComponent instance in instances)
                {
                    await instance.SetParametersAsync(ParameterView.FromDictionary(transition)).ConfigureAwait(true);
                }

                renders.Add(root.ToHtmlString());
            }

            return renders;
        }).ConfigureAwait(true);
    }

    private static string[] ComposerSections(string html)
        => Regex.Split(html, "(?=<section class=\"chatbot-governed-composer)")
            .Where(part => part.StartsWith("<section", StringComparison.Ordinal)).ToArray();

    private static string[] StreamSections(string html)
        => Regex.Split(html, "(?=<section class=\"chatbot-conversation-stream\")")
            .Where(part => part.StartsWith("<section", StringComparison.Ordinal)).ToArray();

    private static ProjectConversationItemModel[] PopulatedStreamItems()
    {
        ProjectConversationItemModel item = System.Text.Json.JsonSerializer.Deserialize<ProjectConversationItemModel>(
            """
            {
              "itemId": "item:email", "kind": "EmailDerived", "actorKind": "Mailbox", "actorLabel": "Mailbox intake",
              "occurredAt": "2026-01-01T00:00:00Z", "lifecycleState": "Associated", "thresholdBand": "high",
              "confidenceScore": 0.9, "associationId": "association-shared", "sourceMailboxId": "mailbox-shared",
              "sourceConversationId": "conversation-shared", "sourceProvenance": "Mailbox intake",
              "redactionState": "metadata_only", "retentionClass": "standard", "schemaVersion": "v1",
              "sourceVersion": 1, "correlationId": "correlation-shared",
              "participantAllowedReviewActions": [], "attachmentAllowedActions": [], "evidenceReferenceSummary": [],
              "requiredStoreKeys": [], "completedStoreKeys": [], "failedStoreKeys": [],
              "approvalRiskActionClasses": [], "approvalAiRiskActionClasses": [], "approvalEvidenceReferences": [],
              "approvalEvidenceFreshnessStates": [], "approvalAffectedResourceReferences": [], "approvalRecipientReferences": []
            }
            """,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The populated conversation item fixture could not be created.");
        return
        [
            item with { ItemId = "item:participant", Kind = "Participant", ParticipantDisplayKind = "UnresolvedParticipant", ParticipantStatus = "Unresolved" },
            item with { ItemId = "item:attachment", Kind = "Attachment", AttachmentScanStatus = "Unavailable", AttachmentRedactionState = "metadata_only" },
            item with { ItemId = "item:decision", Kind = "SystemDecision", DecisionKind = "Associate" },
            item with { ItemId = "item:approval", Kind = "ApprovalEvent", ApprovalStatus = "pending", ApprovalDisabledReason = "policy-blocked", ApprovalEvidenceReferences = ["evidence:expired"], ApprovalEvidenceFreshnessStates = ["expired"] },
            item with { ItemId = "item:failure", Kind = "FailureState", MessageCatalogCode = ChatBotMessageCodes.FailedCommand, FailureStatus = "failed", MessageDetailVisibility = "metadata_only" },
            item with { ItemId = "item:ai-outcome", Kind = "AiOutcome", AiOutcomeStatus = "Succeeded", AiAuthorizedContextReferences = ["evidence:file:requirements"] },
            item,
        ];
    }

    private static string[] InputIds(string html)
        => Regex.Matches(html, "<fluent-textarea[^>]*id=\"([^\"]+)\"")
            .Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).ToArray();

    private static string[] Ids(string html)
        => Regex.Matches(html, "(?<![\\w-])id=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();

    private static string[] PersistentIds(string html)
        => Ids(html).Where(id => id.EndsWith("-title", StringComparison.Ordinal) ||
            id.EndsWith("-help", StringComparison.Ordinal) || id.StartsWith("project-conversation-composer-input-", StringComparison.Ordinal)).ToArray();

    private static void AssertUniqueIds(string html)
    {
        string[] ids = Ids(html);
        ids.ShouldNotBeEmpty();
        ids.Distinct(StringComparer.Ordinal).Count().ShouldBe(ids.Length);
    }

    private static void AssertReferencesResolve(string html)
    {
        string[] ids = Ids(html);
        foreach (Match attribute in Regex.Matches(html, "(?:aria-labelledby|aria-describedby|aria-errormessage|for)=\"([^\"]+)\""))
        {
            foreach (string reference in WebUtility.HtmlDecode(attribute.Groups[1].Value).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                ids.Count(id => id == reference).ShouldBe(1, $"{reference} must resolve exactly once within this component");
            }
        }
    }
}
