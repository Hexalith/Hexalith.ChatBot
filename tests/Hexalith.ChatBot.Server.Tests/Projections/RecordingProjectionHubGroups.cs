using Microsoft.AspNetCore.SignalR;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>Records direct hub guard effects without starting a transport.</summary>
internal sealed class RecordingProjectionHubGroups : IGroupManager
{
    /// <summary>The groups joined by the explicit direct invocation.</summary>
    public List<string> Joined { get; } = [];

    /// <inheritdoc />
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Joined.Add(groupName);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
