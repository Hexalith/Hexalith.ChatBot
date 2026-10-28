using System.Reflection;

using Fluxor;

using Hexalith.ChatBot.UI.Components.Governed;
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

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Exercises the mounted workspace dispatch boundary across disposal and queued notifications.</summary>
public sealed class ProjectConversationWorkspaceLifecycleRenderTests
{
    /// <summary>Active notifications dispatch once, while pending and late notifications become harmless after disposal.</summary>
    [Fact]
    public async Task QueuedAndLateStreamingNotificationsShouldNotDispatchAfterWorkspaceDisposal()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddFluentUIComponents();
        services.AddFluxor(options => options.ScanAssemblies(typeof(ProjectConversationWorkspaceLifecycleRenderTests).Assembly));
        services.AddSingleton<IState<ProjectConversationState>>(new SafeOutcomeRenderState<ProjectConversationState>(new(false, null, null)));
        SafeOutcomeRenderDispatcher dispatcher = new();
        services.AddSingleton<IDispatcher>(dispatcher);
        services.AddSingleton(new ChatBotHubEndpoint(new Uri("http://127.0.0.1")));
        services.AddSingleton<IJSRuntime, InteractionRenderJsRuntime>();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        services.AddSingleton<ChatBotCultureFormatter>();
        services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
        ServiceProvider provider = services.BuildServiceProvider();
        await using var cleanup = provider.ConfigureAwait(true);
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
        ChatBotProjectConversationWorkspace? workspace = null;
        await renderer.Dispatcher.InvokeAsync(async () =>
            await renderer.RenderComponentAsync<WorkspaceLifecycleRenderHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(WorkspaceLifecycleRenderHost.Capture)] = (Action<ChatBotProjectConversationWorkspace>)(component => workspace = component),
            })).ConfigureAwait(true));
        workspace.ShouldNotBeNull();
        dispatcher.Actions.Clear();
        MethodInfo method = typeof(ChatBotProjectConversationWorkspace).GetMethod("DispatchStreamingActionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Func<object, Task> dispatch = method.CreateDelegate<Func<object, Task>>(workspace);
        ProjectConversationAiResponseReconnectAction active = new("active-project");
        await dispatch(active).ConfigureAwait(true);
        dispatcher.Actions.ShouldBe([active]);

        Task? queued = null;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            SynchronizationContext? context = SynchronizationContext.Current;
            try
            {
                // Queue through the real renderer while holding its dispatcher; disposal wins before dispatch.
                SynchronizationContext.SetSynchronizationContext(null);
                queued = dispatch(new ProjectConversationAiResponseReconnectAction("queued-project"));
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(context);
            }

            await ((IAsyncDisposable)workspace).DisposeAsync().ConfigureAwait(true);
        });
        _ = queued.ShouldNotBeNull();
        await queued.ConfigureAwait(true);
        await dispatch(new ProjectConversationAiResponseReconnectAction("late-project")).ConfigureAwait(true);
        await ((IAsyncDisposable)workspace).DisposeAsync().ConfigureAwait(true);
        dispatcher.Actions.ShouldBe([active]);
    }
}
