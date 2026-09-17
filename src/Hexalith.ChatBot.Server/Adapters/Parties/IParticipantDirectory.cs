using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal interface IParticipantDirectory
{
    ValueTask<ParticipantDirectoryResolution> ResolveEmailEvidenceAsync(
        ParticipantDirectoryLookup lookup,
        CancellationToken cancellationToken);
}
