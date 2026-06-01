using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal sealed class UnavailableParticipantDisplayDirectory : IParticipantDisplayDirectory
{
    public Task<ParticipantDisplaySnapshot> GetSafeDisplayAsync(
        string tenantId,
        string partyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(partyId);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ParticipantDisplaySnapshot(
            ProjectConversationParticipantDisplayKind.RestrictedParticipant,
            null));
    }
}
