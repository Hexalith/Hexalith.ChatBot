using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

internal sealed class SignalRProjectConversationHubConnection : IProjectConversationHubConnection
{
    private readonly HubConnection _connection;

    public SignalRProjectConversationHubConnection(HubConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _connection.Closed += OnClosed;
        _connection.Reconnected += OnReconnected;
    }

    public HubConnectionState State => _connection.State;

    public event Func<Task>? Closed;

    public event Func<Task>? Reconnected;

    public void RegisterChanged(string methodName, Action<string> callback)
        => _ = _connection.On(methodName, callback);

    public Task StartAsync() => _connection.StartAsync();

    public Task InvokeAsync(string methodName, string tenantId) => _connection.InvokeAsync(methodName, tenantId);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    private Task OnClosed(Exception? exception) => Closed?.Invoke() ?? Task.CompletedTask;

    private Task OnReconnected(string? connectionId) => Reconnected?.Invoke() ?? Task.CompletedTask;
}
