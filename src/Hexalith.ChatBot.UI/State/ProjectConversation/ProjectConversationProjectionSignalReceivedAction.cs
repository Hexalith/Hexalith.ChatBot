namespace Hexalith.ChatBot.UI.State.ProjectConversation;

// Raised by the workspace when the signal-only EventStore projection-changed SignalR transport reports the
// project-conversation projection changed for the current tenant. Carries no version/sequence (signal-only); the
// effect synthesizes a forward-looking metadata-only nudge for the current conversation and re-queries authoritative
// server state. ProjectId/TenantId let the effect fail closed on a signal that does not match the loaded conversation.
public sealed record ProjectConversationProjectionSignalReceivedAction(string ProjectId, string TenantId)
{
    public long ScopeVersion { get; init; }
}
