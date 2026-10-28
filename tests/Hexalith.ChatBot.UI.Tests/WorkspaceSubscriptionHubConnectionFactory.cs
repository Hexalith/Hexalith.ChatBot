using Hexalith.ChatBot.UI.State.ProjectConversation;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Supplies controlled hub connections to the real streaming subscriber.</summary>
/// <param name="connections">The connections returned in creation order.</param>
internal sealed class WorkspaceSubscriptionHubConnectionFactory(params WorkspaceSubscriptionHubConnection[] connections)
    : IProjectConversationHubConnectionFactory
{
    private readonly Queue<WorkspaceSubscriptionHubConnection> _connections = new(connections);

    /// <summary>Gets the number of created connections.</summary>
    public int CreateCount { get; private set; }

    /// <inheritdoc/>
    public IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider)
    {
        CreateCount++;
        return _connections.Dequeue();
    }
}
