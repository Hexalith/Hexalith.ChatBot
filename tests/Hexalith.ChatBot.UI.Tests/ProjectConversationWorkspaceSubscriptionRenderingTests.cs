using System.Reflection;

using Fluxor;

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

/// <summary>Exercises real workspace lifecycle callbacks through a controlled streaming transport.</summary>
public sealed class ProjectConversationWorkspaceSubscriptionRenderingTests
{
    private static readonly FieldInfo SubscriberField = typeof(ChatBotProjectConversationWorkspace)
        .GetField("_streamingSubscriber", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo AfterRenderMethod = typeof(ChatBotProjectConversationWorkspace)
        .GetMethod("OnAfterRenderAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>Hub notifications reach the mounted workspace dispatcher and stop after actual disposal.</summary>
    [Fact]
    public async Task MountedWorkspaceShouldDispatchTypedSignalAndReconnectActionsAndIgnoreLateEventsAfterDisposal()
    {
        WorkspaceSubscriptionRenderState state = new(Snapshot("project-alpha", "tenant-alpha", 7));
        WorkspaceSubscriptionHubConnection connection = new();
        WorkspaceSubscriptionHubConnectionFactory factory = new(connection);
        List<object> actions = [];
        ServiceProvider provider = CreateProvider(state, actions);
        await using var providerLifetime = provider.ConfigureAwait(true);
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
        ChatBotProjectConversationWorkspace workspace = await MountAsync(renderer, factory, "project-alpha");
        await renderer.Dispatcher.InvokeAsync(() => InvokeAfterRenderAsync(workspace));
        connection.JoinedTenants.ShouldBe(["tenant-alpha"]);
        actions.Clear();

        await connection.TriggerChangedAsync("tenant-other");
        actions.ShouldBeEmpty();
        await connection.TriggerChangedAsync("tenant-alpha");
        await connection.TriggerReconnectedAsync();
        actions.ShouldBe(
        [
            new ProjectConversationProjectionSignalReceivedAction("project-alpha", "tenant-alpha") { ScopeVersion = 7 },
            new ProjectConversationAiResponseReconnectAction("project-alpha") { ScopeVersion = 7 },
        ]);

        await renderer.Dispatcher.InvokeAsync(async () => await ((IAsyncDisposable)workspace).DisposeAsync().ConfigureAwait(true));
        actions.Clear();
        int joinsBeforeLateEvents = connection.JoinedTenants.Count;
        await connection.TriggerChangedAsync("tenant-alpha");
        await connection.TriggerReconnectedAsync();
        await connection.TriggerClosedAsync();
        await renderer.Dispatcher.InvokeAsync(() => InvokeAfterRenderAsync(workspace));
        actions.ShouldBeEmpty();
        connection.JoinedTenants.Count.ShouldBe(joinsBeforeLateEvents);
        connection.DisposeCount.ShouldBe(1);
        factory.CreateCount.ShouldBe(1);
    }

    /// <summary>A render skipped during connection start is rebound before the original render completes.</summary>
    /// <param name="changeTenant">Whether the new project belongs to a different tenant.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjectAndScopeChangedDuringConnectionStartShouldRebindActualWorkspaceCallbacks(bool changeTenant)
    {
        WorkspaceSubscriptionRenderState state = new(Snapshot("project-old", "tenant-alpha", 10));
        TaskCompletionSource releaseStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
        WorkspaceSubscriptionHubConnection first = new() { StartDelay = releaseStart.Task };
        WorkspaceSubscriptionHubConnection replacement = new();
        WorkspaceSubscriptionHubConnectionFactory factory = new(first, replacement);
        List<object> actions = [];
        ServiceProvider provider = CreateProvider(state, actions);
        await using var providerLifetime = provider.ConfigureAwait(true);
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
        ChatBotProjectConversationWorkspace workspace = await MountAsync(renderer, factory, "project-old");
        Task subscription = renderer.Dispatcher.InvokeAsync(() => InvokeAfterRenderAsync(workspace));
        await first.StartEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        subscription.IsCompleted.ShouldBeFalse();
        string currentTenant = changeTenant ? "tenant-beta" : "tenant-alpha";
        try
        {
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                state.Value = Snapshot("project-current", currentTenant, 11);
                await workspace.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotProjectConversationWorkspace.ProjectId)] = "project-current",
                })).ConfigureAwait(true);
                // Reproduce the overlapping render that observes _subscribing and returns immediately.
                await InvokeAfterRenderAsync(workspace).ConfigureAwait(true);
            });
        }
        finally
        {
            releaseStart.TrySetResult();
        }

        await subscription.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        WorkspaceSubscriptionHubConnection current = changeTenant ? replacement : first;
        first.StartCount.ShouldBe(1);
        factory.CreateCount.ShouldBe(changeTenant ? 2 : 1);
        if (changeTenant)
        {
            first.DisposeCount.ShouldBe(1);
            current.JoinedTenants.ShouldBe(["tenant-beta"]);
            actions.Clear();
            await first.TriggerChangedAsync("tenant-alpha");
            await first.TriggerReconnectedAsync();
            actions.ShouldBeEmpty();
        }

        actions.Clear();
        await current.TriggerChangedAsync(currentTenant);
        await current.TriggerReconnectedAsync();
        actions.ShouldBe(
        [
            new ProjectConversationProjectionSignalReceivedAction("project-current", currentTenant) { ScopeVersion = 11 },
            new ProjectConversationAiResponseReconnectAction("project-current") { ScopeVersion = 11 },
        ]);
    }

    private static ProjectConversationState Snapshot(string projectId, string tenant, long scopeVersion)
        => new(false, new ProjectConversationModel(
            projectId, "Project conversation", tenant, "Ready", "Current", [], null, false, 25,
            "governed", "metadata_only", "standard", "1", "correlation", "none"), null)
        {
            RequestedProjectId = projectId,
            ProjectScopeVersion = scopeVersion,
        };

    private static ServiceProvider CreateProvider(WorkspaceSubscriptionRenderState state, List<object> actions)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddFluentUIComponents();
        services.AddFluxor(options => options.ScanAssemblies(typeof(ProjectConversationWorkspaceSubscriptionRenderingTests).Assembly));
        services.AddSingleton<IState<ProjectConversationState>>(state);
        services.AddSingleton<IDispatcher>(new DashboardFailureDispatcher(actions.Add));
        services.AddSingleton(new ChatBotHubEndpoint(new Uri("https://chatbot.example/base/")));
        services.AddSingleton<IJSRuntime, AssociationRenderJsRuntime>();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        services.AddSingleton<ChatBotCultureFormatter>();
        services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
        return services.BuildServiceProvider();
    }

    private static async Task<ChatBotProjectConversationWorkspace> MountAsync(HtmlRenderer renderer,
        WorkspaceSubscriptionHubConnectionFactory factory, string projectId)
    {
        ChatBotProjectConversationWorkspace? workspace = null;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<FluentStack>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(FluentStack.ChildContent)] = (RenderFragment)(builder =>
                {
                    builder.OpenComponent<ChatBotProjectConversationWorkspace>(0);
                    builder.AddAttribute(1, nameof(ChatBotProjectConversationWorkspace.ProjectId), projectId);
                    builder.AddAttribute(2, nameof(ChatBotProjectConversationWorkspace.HeadingText), "Project conversation");
                    builder.AddComponentReferenceCapture(3, instance => workspace = (ChatBotProjectConversationWorkspace)instance);
                    builder.CloseComponent();
                }),
            })).ConfigureAwait(true);
            root.ToHtmlString().ShouldContain("data-chatbot-conversation-stream");
            workspace.ShouldNotBeNull();
            var original = (ProjectConversationStreamingSubscriber)SubscriberField.GetValue(workspace)!;
            await original.DisposeAsync().ConfigureAwait(true);
            SubscriberField.SetValue(workspace, new ProjectConversationStreamingSubscriber(
                new ChatBotHubEndpoint(new Uri("https://chatbot.example/base/")), factory));
        }).ConfigureAwait(true);
        return workspace!;
    }

    private static Task InvokeAfterRenderAsync(ChatBotProjectConversationWorkspace workspace)
        => (Task)AfterRenderMethod.Invoke(workspace, [false])!;
}
