using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Closed unused collaborator for the governed-note command-execution probe.</summary>
internal sealed class RecoveryParticipantResolutionOrchestrator : IParticipantResolutionOrchestrator
{
    /// <inheritdoc />
    public ValueTask<ResolveMailboxMessageParticipants> ResolveAsync(
        ResolveMailboxMessageParticipants command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
        => throw new NotSupportedException("The recovery command probe never resolves mailbox participants.");
}
