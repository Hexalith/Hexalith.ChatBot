using Hexalith.ChatBot.UI.State.ProjectConversation;

using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Delivers controlled transport events, including events arriving after disposal.</summary>
internal sealed class WorkspaceSubscriptionHubConnection : IProjectConversationHubConnection
{
    private Func<string, Task>? _changed;

    /// <summary>Gets or sets an optional start barrier for overlapping renderer changes.</summary>
    public Task? StartDelay { get; set; }

    /// <summary>Gets the notification that connection start has begun.</summary>
    public TaskCompletionSource StartEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc/>
    public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;

    /// <summary>Gets the number of connection starts.</summary>
    public int StartCount { get; private set; }

    /// <summary>Gets the number of connection disposals.</summary>
    public int DisposeCount { get; private set; }

    /// <summary>Gets the groups joined by the subscriber.</summary>
    public List<string> JoinedTenants { get; } = [];

    /// <inheritdoc/>
    public event Func<Task>? Closed;

    /// <inheritdoc/>
    public event Func<Task>? Reconnected;

    /// <inheritdoc/>
    public void RegisterChanged(string methodName, Func<string, Task> callback) => _changed = callback;

    /// <inheritdoc/>
    public async Task StartAsync()
    {
        StartCount++;
        State = HubConnectionState.Connecting;
        StartEntered.TrySetResult();
        if (StartDelay is { } delay)
        {
            await delay.ConfigureAwait(true);
        }

        State = HubConnectionState.Connected;
    }

    /// <inheritdoc/>
    public Task InvokeAsync(string methodName, string tenantId)
    {
        JoinedTenants.Add(tenantId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        State = HubConnectionState.Disconnected;
        return ValueTask.CompletedTask;
    }

    /// <summary>Delivers a projection change through the callback registered by the subscriber.</summary>
    /// <param name="tenantId">The signal tenant.</param>
    /// <returns>The entire subscriber and workspace callback execution.</returns>
    public Task TriggerChangedAsync(string tenantId) => _changed?.Invoke(tenantId) ?? Task.CompletedTask;

    /// <summary>Delivers a reconnect through the subscriber's registered transport handler.</summary>
    /// <returns>The entire subscriber and workspace callback execution.</returns>
    public Task TriggerReconnectedAsync() => Reconnected?.Invoke() ?? Task.CompletedTask;

    /// <summary>Delivers a late close event to verify that teardown cannot restart transport recovery.</summary>
    /// <returns>The registered close-handler execution.</returns>
    public Task TriggerClosedAsync() => Closed?.Invoke() ?? Task.CompletedTask;
}
