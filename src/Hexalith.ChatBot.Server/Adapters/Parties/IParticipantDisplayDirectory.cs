using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal interface IParticipantDisplayDirectory
{
    Task<ParticipantDisplaySnapshot> GetSafeDisplayAsync(
        string tenantId,
        string partyId,
        CancellationToken cancellationToken = default);
}
