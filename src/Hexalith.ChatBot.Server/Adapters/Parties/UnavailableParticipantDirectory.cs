using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal sealed class UnavailableParticipantDirectory : IParticipantDirectory
{
    public ValueTask<ParticipantDirectoryResolution> ResolveEmailEvidenceAsync(
        ParticipantDirectoryLookup lookup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ParticipantDirectoryResolution.FromUnresolved(
            lookup,
            ParticipantResolutionBlockedReason.DirectoryUnavailable));
    }
}
