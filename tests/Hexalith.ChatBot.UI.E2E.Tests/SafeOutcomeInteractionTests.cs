using System.Globalization;
using Fluxor;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Components.Pages;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.AssociationReview;
using Hexalith.ChatBot.UI.State.ProjectConversation;
using Hexalith.ChatBot.UI.State.OperationalDashboards;
using Hexalith.ChatBot.UI.State.GovernedOperations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Verifies actual action wiring and live-region transitions on mounted UI components.</summary>
public sealed class SafeOutcomeInteractionTests
{
    /// <summary>The rendered retry control dispatches a fresh authoritative reload.</summary>
    [Fact]
    public Task WorkspaceRetryShouldDispatchFreshReload()
        => RunAsync(async (renderer, dispatcher) =>
        {
            ChatBotProjectConversationWorkspace workspace = await renderer.MountAsync<ChatBotProjectConversationWorkspace>(new()
            {
                [nameof(ChatBotProjectConversationWorkspace.ProjectId)] = "project-alpha",
                [nameof(ChatBotProjectConversationWorkspace.HeadingText)] = "Project conversation",
                [nameof(ChatBotProjectConversationWorkspace.PageTitle)] = "Project conversation",
            }).ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                ChatBotGovernedAction action = renderer.Find<ChatBotGovernedAction>(workspace)
                    .Single(item => item.StableId == "project-workspace-retryable-safe-action");
                action.State.ShouldBe(ChatBotGovernedActionState.Enabled);
                await renderer.Find<FluentButton>(action).Single().OnClick.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true);
            }).ConfigureAwait(true);
            LoadProjectConversationAction[] loads = [.. dispatcher.Actions.OfType<LoadProjectConversationAction>()];
            loads.Length.ShouldBe(2);
            loads[1].ProjectId.ShouldBe("project-alpha");
            loads[1].RequestId.ShouldNotBe(loads[0].RequestId);
        });

    /// <summary>Each Refresh announces a fresh failure; navigation retains silent initial-load deduplication.</summary>
    [Fact]
    public Task DashboardRefreshShouldAnnounceEachAttemptAcrossRemount()
        => RunAsync(async (renderer, dispatcher) =>
        {
            HashSet<string> keys = [];
            for (int mount = 0; mount < 2; mount++)
            {
                OperationalDashboards dashboard = await renderer.MountAsync<OperationalDashboards>().ConfigureAwait(true);
                await renderer.Dispatcher.InvokeAsync(async () =>
                {
                    ChatBotStatusBanner banner = renderer.Find<ChatBotStatusBanner>(dashboard)
                        .Single(item => item.StableId == "operational-dashboards-failed");
                    renderer.Attribute(banner, "aria-live").ShouldBe(mount == 0 ? "polite" : "off");
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        ChatBotGovernedAction refresh = renderer.Find<ChatBotGovernedAction>(dashboard)
                            .Single(item => item.StableId == "operational-dashboards-refresh");
                        await renderer.Find<FluentButton>(refresh).Single().OnClick.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true);
                        keys.Add(renderer.Attribute(banner, "data-chatbot-announcement-key")!).ShouldBeTrue();
                        renderer.Attribute(banner, "aria-live").ShouldBe("polite");
                        renderer.Attribute(banner, "data-chatbot-live-announced").ShouldBe("true");
                    }
                }).ConfigureAwait(true);
            }
            dispatcher.Actions.OfType<LoadOperationalDashboardAction>().Count().ShouldBe(6);
        });

    /// <summary>A terminal or untranslated dashboard denial cannot activate a new load.</summary>
    /// <param name="code">The current dashboard failure code.</param>
    /// <param name="culture">The requested locale.</param>
    /// <param name="canRetry">Whether the localized catalog permits retry.</param>
    [Theory]
    [InlineData(null, "en", true)]
    [InlineData(ChatBotMessageCodes.DependencyDegraded, "en", true)]
    [InlineData(ChatBotMessageCodes.DependencyDegraded, "fr", true)]
    [InlineData("restricted-dashboard-secret", "en", false)]
    [InlineData(ChatBotMessageCodes.AuthorizationDenied, "fr", false)]
    [InlineData(ChatBotMessageCodes.DependencyDegraded, "ja", false)]
    public Task DashboardRefreshShouldRespectLocalizedRetryPermission(string? code, string culture, bool canRetry)
        => RunAsync(async (renderer, dispatcher) =>
        {
            OperationalDashboards dashboard = await renderer.MountAsync<OperationalDashboards>().ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                ChatBotGovernedAction refresh = renderer.Find<ChatBotGovernedAction>(dashboard)
                    .Single(item => item.StableId == "operational-dashboards-refresh");
                refresh.State.ShouldBe(canRetry ? ChatBotGovernedActionState.Enabled : ChatBotGovernedActionState.DisabledWithReason);
                if (!canRetry)
                {
                    refresh.DisabledReason.ShouldNotBeNullOrWhiteSpace();
                    renderer.Attribute(refresh, "aria-describedby").ShouldBe("operational-dashboards-refresh-disabled-reason");
                    renderer.Attribute(refresh, "tabindex").ShouldBe("0");
                }
                await renderer.Find<FluentButton>(refresh).Single().OnClick.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true);
            }).ConfigureAwait(true);
            dispatcher.Actions.OfType<LoadOperationalDashboardAction>().Count().ShouldBe(canRetry ? 2 : 1);
        }, culture, code);

    /// <summary>A fresh governed submission announces after navigation without re-announcing passive state.</summary>
    [Fact]
    public Task GovernedSubmissionShouldAnnounceAcrossRemount()
    {
        const string Failure = ChatBotMessageCodes.DependencyDegraded;
        SafeOutcomeRenderState<GovernedOperationsState> state = new(new(false, null, Failure));
        return RunAsync(async (renderer, dispatcher) =>
        {
            GovernedOperations page = await renderer.MountAsync<GovernedOperations>().ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                ChatBotStatusBanner Banner() => renderer.Find<ChatBotStatusBanner>(page)
                    .Single(item => item.StableId == "governed-note-failed");
                string expectedKey = $"governed-note-failed-{Failure}";
                renderer.Attribute(Banner(), "aria-live").ShouldBe("polite");
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    ChatBotGovernedAction submit = renderer.Find<ChatBotGovernedAction>(page)
                        .Single(item => item.StableId == "record-governed-note");
                    await renderer.Find<FluentButton>(submit).Single().OnClick.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true);
                    state.Value = GovernedOperationsReducers.ReduceSubmit(state.Value);
                    await renderer.UpdateAsync(page, []).ConfigureAwait(true);
                    page = await renderer.MountAsync<GovernedOperations>().ConfigureAwait(true);
                    state.Value = GovernedOperationsReducers.ReduceFailed(state.Value, new(Failure));
                    await renderer.UpdateAsync(page, []).ConfigureAwait(true);

                    renderer.Attribute(Banner(), "data-chatbot-announcement-key").ShouldBe(expectedKey);
                    renderer.Attribute(Banner(), "data-chatbot-live-announced").ShouldBe("true");
                    page = await renderer.MountAsync<GovernedOperations>().ConfigureAwait(true);
                    renderer.Attribute(Banner(), "aria-live").ShouldBe("off");
                }
            }).ConfigureAwait(true);
            dispatcher.Actions.OfType<SubmitGovernedNoteAction>().Count().ShouldBe(2);
        }, governedState: state);
    }

    /// <summary>Each retry that fails again with the same code announces and takes focus once.</summary>
    [Fact]
    public Task AssociationReviewRetryShouldAnnounceAndFocusEachSameCodeFailure()
    {
        const string AssociationId = "association-001";
        const string Failure = ChatBotMessageCodes.DependencyDegraded;
        SafeOutcomeRenderState<AssociationReviewState> state = new(AssociationReviewReducers.ReduceFailed(
            AssociationReviewReducers.ReduceLoad(new(false, false, null, null, "", "", null, null), new(AssociationId)),
            new(Failure)));
        InteractionRenderJsRuntime js = new();
        return RunAsync(async (renderer, dispatcher) =>
        {
            Dictionary<string, object?> route = new() { [nameof(AssociationReview.AssociationId)] = AssociationId };
            AssociationReview page = await renderer.MountAsync<AssociationReview>(route).ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                IEnumerable<ChatBotStatusBanner> Banners() => renderer.Find<ChatBotStatusBanner>(page)
                    .Where(item => item.StableId == "association-review-failed");
                async Task RenderAsync(AssociationReviewState next)
                {
                    state.Value = next;
                    await renderer.UpdateAsync(page, route).ConfigureAwait(true);
                }

                async Task<LoadAssociationReviewAction> RetryAsync()
                {
                    int loads = dispatcher.Actions.OfType<LoadAssociationReviewAction>().Count();
                    ChatBotGovernedAction retry = renderer.Find<ChatBotGovernedAction>(page)
                        .Single(item => item.StableId == "association-review-retry");
                    await renderer.Find<FluentButton>(retry).Single().OnClick.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true);
                    LoadAssociationReviewAction[] dispatched = [.. dispatcher.Actions.OfType<LoadAssociationReviewAction>()];
                    dispatched.Length.ShouldBe(loads + 1);
                    dispatched[^1].AssociationId.ShouldBe(AssociationId);
                    return dispatched[^1];
                }

                async Task ShouldAnnounceAndFocusOnceAsync(long activation)
                {
                    state.Value.FailureActivation.ShouldBe(activation);
                    ChatBotStatusBanner banner = Banners().ShouldHaveSingleItem();
                    renderer.Attribute(banner, "data-chatbot-announcement-key").ShouldBe($"association-review-failed-{Failure}");
                    renderer.Attribute(banner, "data-chatbot-live-announced").ShouldBe("true");
                    renderer.Attribute(banner, "aria-live").ShouldBe("polite");
                    js.FocusedElements.Count.ShouldBe((int)activation);

                    await RenderAsync(state.Value).ConfigureAwait(true);
                    renderer.Attribute(Banners().ShouldHaveSingleItem(), "data-chatbot-live-announced").ShouldBe("false");
                    renderer.Attribute(Banners().Single(), "aria-live").ShouldBe("off");
                    js.FocusedElements.Count.ShouldBe((int)activation);
                }

                await ShouldAnnounceAndFocusOnceAsync(1).ConfigureAwait(true);

                // A retry whose loading state renders before the same failure returns.
                LoadAssociationReviewAction retry = await RetryAsync().ConfigureAwait(true);
                await RenderAsync(AssociationReviewReducers.ReduceLoad(state.Value, retry)).ConfigureAwait(true);
                Banners().ShouldBeEmpty();
                await RenderAsync(AssociationReviewReducers.ReduceFailed(state.Value, new(Failure))).ConfigureAwait(true);
                await ShouldAnnounceAndFocusOnceAsync(2).ConfigureAwait(true);

                // A retry that fails before any render keeps the same banner and summary on screen.
                retry = await RetryAsync().ConfigureAwait(true);
                await RenderAsync(AssociationReviewReducers.ReduceFailed(
                    AssociationReviewReducers.ReduceLoad(state.Value, retry), new(Failure))).ConfigureAwait(true);
                await ShouldAnnounceAndFocusOnceAsync(3).ConfigureAwait(true);

                // Returning to the page leaves the existing focus and announcement alone.
                await renderer.UnmountAsync(page).ConfigureAwait(true);
                page = await renderer.MountAsync<AssociationReview>(route).ConfigureAwait(true);
                renderer.Attribute(Banners().ShouldHaveSingleItem(), "aria-live").ShouldBe("off");
                js.FocusedElements.Count.ShouldBe(3);

                retry = await RetryAsync().ConfigureAwait(true);
                await RenderAsync(AssociationReviewReducers.ReduceFailed(
                    AssociationReviewReducers.ReduceLoad(state.Value, retry), new(Failure))).ConfigureAwait(true);
                await ShouldAnnounceAndFocusOnceAsync(4).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }, associationState: state, jsRuntime: js);
    }

    /// <summary>A mounted failure summary takes focus while validation stays with the form.</summary>
    [Fact]
    public Task AssociationReviewValidationAfterFailureShouldKeepFocusInTheForm()
    {
        const string AssociationId = "association-001";
        AssociationReviewModel review = new(
            AssociationId, "intake-001", "mailbox-001", "conversation-001", null, "NeedsReview",
            "CandidatesGenerated", "Ambiguous", 0.5, [], [], [], [], [], "policy", "kernel",
            DateTimeOffset.UnixEpoch, "mailbox", "metadata_only", "collaboration_input", "v1", 1, "correlation-001");
        SafeOutcomeRenderState<AssociationReviewState> state = new(new(false, false, review, null, "", "", null, null));
        InteractionRenderJsRuntime js = new();
        return RunAsync(async (renderer, _) =>
        {
            Dictionary<string, object?> route = new() { [nameof(AssociationReview.AssociationId)] = AssociationId };
            AssociationReview page = await renderer.MountAsync<AssociationReview>(route).ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                js.FocusedElements.ShouldBeEmpty();

                state.Value = AssociationReviewReducers.ReduceSubmitFailed(
                    AssociationReviewReducers.ReduceSubmitStarted(state.Value), new(ChatBotMessageCodes.DependencyDegraded));
                await renderer.UpdateAsync(page, route).ConfigureAwait(true);
                renderer.Find<ChatBotStatusBanner>(page).ShouldContain(banner => banner.StableId == "association-review-failed");
                ElementReference focusedSummary = js.FocusedElements.ShouldHaveSingleItem();

                state.Value = AssociationReviewReducers.ReduceDecisionRejected(
                    AssociationReviewReducers.ReduceSubmitStarted(state.Value), new("candidate-required"));
                await renderer.UpdateAsync(page, route).ConfigureAwait(true);
                renderer.Find<ChatBotStatusBanner>(page).ShouldNotContain(banner => banner.StableId == "association-review-failed");
                ChatBotAssociationReviewActions actions = renderer.Find<ChatBotAssociationReviewActions>(page).ShouldHaveSingleItem();
                renderer.Find<ChatBotStatusBanner>(actions).ShouldContain(
                    banner => banner.StableId.EndsWith("-association-review-validation", StringComparison.Ordinal));
                js.FocusedElements.ShouldHaveSingleItem().ShouldBe(focusedSummary);
            }).ConfigureAwait(true);
        }, associationState: state, jsRuntime: js);
    }

    /// <summary>A new composer instance can announce a same-code submission in the same circuit.</summary>
    [Fact]
    public Task ComposerSubmissionShouldAnnounceAcrossRemount()
        => RunAsync(async (renderer, _) =>
        {
            HashSet<string> keys = [];
            for (int mount = 0; mount < 2; mount++)
            {
                ChatBotGovernedComposer composer = await renderer.MountAsync<ChatBotGovernedComposer>(new()
                {
                    [nameof(ChatBotGovernedComposer.SubmissionErrorCode)] = ChatBotMessageCodes.AuthorizationDenied,
                    [nameof(ChatBotGovernedComposer.OnSubmit)] = EventCallback.Factory.Create<string>(this, _ => { }),
                }).ConfigureAwait(true);
                await renderer.Dispatcher.InvokeAsync(async () =>
                {
                    ChatBotStatusBanner initial = renderer.Find<ChatBotStatusBanner>(composer).Single();
                    renderer.Attribute(initial, "aria-live").ShouldBe(mount == 0 ? "assertive" : "off");
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        await renderer.Find<FluentButton>(composer).Last().OnClick.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true);
                        ChatBotStatusBanner banner = renderer.Find<ChatBotStatusBanner>(composer).Single();
                        keys.Add(renderer.Attribute(banner, "data-chatbot-announcement-key")!).ShouldBeTrue();
                        renderer.Attribute(banner, "aria-live").ShouldBe("assertive");
                    }
                }).ConfigureAwait(true);
            }
        });

    /// <summary>The stable live region receives concise catalog feedback after the article renders.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public Task FailureAnnouncementShouldContainAccessibleSummaryOnly(string culture)
        => RunAsync(async (renderer, _) =>
        {
            ProjectConversationItemModel item = System.Text.Json.JsonSerializer.Deserialize<ProjectConversationItemModel>(
                """{"itemId":"failure","kind":"FailureState","actorKind":"System","actorLabel":"Worker","occurredAt":"2026-01-01T00:00:00Z","failureStatus":"failed","messageDetailVisibility":"metadata_only"}""",
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))! with
            {
                MessageCatalogCode = ChatBotMessageCodes.FailedCommand,
                OperationId = "private-operation-id", WorkflowInstanceId = "private-workflow-id", CorrelationId = "private-correlation-id",
            };
            ChatBotFailureStateConversationItem card = await renderer.MountAsync<ChatBotFailureStateConversationItem>(new()
            {
                [nameof(ChatBotFailureStateConversationItem.Item)] = item,
                [nameof(ChatBotFailureStateConversationItem.Announce)] = true,
            }).ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(() =>
            {
                FluentText announcement = renderer.Find<FluentText>(card).Single(text => text.Class == "chatbot-visually-hidden");
                renderer.Attribute(announcement, "aria-live").ShouldBe("assertive");
                string text = renderer.Text(announcement);
                text.ShouldContain(culture == "en" ? "The command could not be completed safely." : "La commande a échoué sans exposer de détail fournisseur.");
                text.ShouldContain(culture == "en" ? "Escalate to the configured owner or operations role." : "Escalader vers le propriétaire ou rôle opérations configuré.");
                text.ShouldContain(culture == "en" ? "State not permitted" : "État non autorisé");
                renderer.Text(announcement).ShouldNotContain("private-");
                renderer.Attribute(card, "role").ShouldBe("alert");
            }).ConfigureAwait(true);
        }, culture);

    /// <summary>Prepending quiet history retains each keyed current item and its after-render summary.</summary>
    [Fact]
    public Task PaginationInsertionShouldPreserveCurrentSummariesAndKeepHistoryQuiet()
        => RunAsync(async (renderer, _) =>
        {
            static ProjectConversationItemModel Item(string id, string code)
                => System.Text.Json.JsonSerializer.Deserialize<ProjectConversationItemModel>(
                    """{"itemId":"failure","kind":"FailureState","actorKind":"System","actorLabel":"Worker","occurredAt":"2026-01-01T00:00:00Z","failureStatus":"failed","messageDetailVisibility":"metadata_only"}""",
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))! with
                { ItemId = id, MessageCatalogCode = code };
            ChatBotConversationStream stream = await renderer.MountAsync<ChatBotConversationStream>().ConfigureAwait(true);
            ProjectConversationItemModel first = Item("current-command", ChatBotMessageCodes.FailedCommand);
            ProjectConversationItemModel second = Item("current-attachment", ChatBotMessageCodes.FailedAttachment);
            IReadOnlySet<string> currentIds = new HashSet<string> { first.ItemId, second.ItemId };
            await renderer.UpdateAsync(stream, new()
            {
                [nameof(ChatBotConversationStream.Items)] = new[] { first, second },
                [nameof(ChatBotConversationStream.CurrentPageItemIds)] = currentIds,
            }).ConfigureAwait(true);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                Dictionary<string, ChatBotFailureStateConversationItem> cards = renderer.Find<ChatBotFailureStateConversationItem>(stream)
                    .ToDictionary(card => card.Item.ItemId);
                Dictionary<string, string> summaries = cards.ToDictionary(pair => pair.Key,
                    pair => renderer.Text(renderer.Find<FluentText>(pair.Value).Single(text => text.Class == "chatbot-visually-hidden")));
                summaries[first.ItemId].ShouldContain("The command could not be completed safely.");
                summaries[second.ItemId].ShouldContain("The attachment could not be processed safely.");
                ProjectConversationItemModel history = Item("older-history", ChatBotMessageCodes.TerminalFailure);
                await renderer.UpdateAsync(stream, new()
                {
                    [nameof(ChatBotConversationStream.Items)] = new[] { history, first, second },
                    [nameof(ChatBotConversationStream.CurrentPageItemIds)] = currentIds,
                }).ConfigureAwait(true);
                Dictionary<string, ChatBotFailureStateConversationItem> updated = renderer.Find<ChatBotFailureStateConversationItem>(stream)
                    .ToDictionary(card => card.Item.ItemId);
                foreach (string id in currentIds)
                {
                    updated[id].ShouldBeSameAs(cards[id]);
                    FluentText announcement = renderer.Find<FluentText>(updated[id]).Single(text => text.Class == "chatbot-visually-hidden");
                    renderer.Text(announcement).ShouldBe(summaries[id]);
                    renderer.Attribute(announcement, "aria-live").ShouldBe("off");
                }
                FluentText oldAnnouncement = renderer.Find<FluentText>(updated[history.ItemId]).Single(text => text.Class == "chatbot-visually-hidden");
                renderer.Text(oldAnnouncement).ShouldBeEmpty();
                renderer.Attribute(oldAnnouncement, "aria-live").ShouldBe("off");
            }).ConfigureAwait(true);
        });

    private static async Task RunAsync(Func<SafeOutcomeInteractiveRenderer, SafeOutcomeRenderDispatcher, Task> test, string culture = "en", string? dashboardError = ChatBotMessageCodes.DependencyDegraded,
        SafeOutcomeRenderState<GovernedOperationsState>? governedState = null, SafeOutcomeRenderState<AssociationReviewState>? associationState = null,
        InteractionRenderJsRuntime? jsRuntime = null)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddLocalization();
            services.AddFluentUIComponents();
            services.AddFluxor(options => options.ScanAssemblies(typeof(SafeOutcomeInteractionTests).Assembly));
            services.AddSingleton<NavigationManager, StaticRenderNavigationManager>();
            services.AddSingleton<Hexalith.ChatBot.Client.IChatBotClient>(new FakeChatBotClient());
            services.AddSingleton<IState<ProjectConversationState>>(new SafeOutcomeRenderState<ProjectConversationState>(new(false, null, ChatBotMessageCodes.AuditUnavailable)));
            services.AddSingleton<IState<OperationalDashboardsState>>(new SafeOutcomeRenderState<OperationalDashboardsState>(new(false, null, dashboardError)));
            services.AddSingleton<IState<GovernedOperationsState>>(governedState
                ?? new SafeOutcomeRenderState<GovernedOperationsState>(new(false, null, ChatBotMessageCodes.DependencyDegraded)));
            services.AddSingleton<IState<AssociationReviewState>>(associationState
                ?? new SafeOutcomeRenderState<AssociationReviewState>(new(false, false, null, null, "", "", null, null)));
            SafeOutcomeRenderDispatcher dispatcher = new();
            services.AddSingleton<IDispatcher>(dispatcher);
            services.AddSingleton(new ChatBotHubEndpoint(new Uri("http://127.0.0.1")));
            services.AddSingleton<IJSRuntime>(jsRuntime ?? new InteractionRenderJsRuntime());
            services.AddSingleton<ChatBotUiTextLocalizer>();
            services.AddSingleton<ChatBotCultureFormatter>();
            services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
            services.AddScoped<AssociationReviewFocusState>();
            ServiceProvider provider = services.BuildServiceProvider();
            await using var providerLifetime = provider.ConfigureAwait(true);
            AsyncServiceScope scope = provider.CreateAsyncScope();
            await using var scopeLifetime = scope.ConfigureAwait(true);
            IServiceProvider scopedServices = scope.ServiceProvider;
            SafeOutcomeInteractiveRenderer renderer = new(scopedServices, scopedServices.GetRequiredService<ILoggerFactory>());
            await using var rendererLifetime = renderer.ConfigureAwait(true);
            await test(renderer, dispatcher).ConfigureAwait(true);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
