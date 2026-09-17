using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

internal interface IProjectConversationHubConnection : IAsyncDisposable
{
    HubConnectionState State { get; }

    event Func<Task>? Closed;

    event Func<Task>? Reconnected;

    void RegisterChanged(string methodName, Action<string> callback);

    Task StartAsync();

    Task InvokeAsync(string methodName, string tenantId);
}
