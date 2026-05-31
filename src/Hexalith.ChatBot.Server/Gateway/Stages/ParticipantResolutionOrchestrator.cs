using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Adapters.Parties;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ParticipantResolutionOrchestrator(IParticipantDirectory directory) : IParticipantResolutionOrchestrator
{
    public async ValueTask<ResolveMailboxMessageParticipants> ResolveAsync(
        ResolveMailboxMessageParticipants command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        List<ResolvedMailboxParticipantReference> resolved = [];
        List<UnresolvedMailboxParticipantEvidence> unresolved = [];
        foreach (MailboxParticipantSourceReference source in command.SourceParticipants)
        {
            ParticipantDirectoryResolution resolution = await directory
                .ResolveEmailEvidenceAsync(
                    new ParticipantDirectoryLookup(
                        context.TenantBinding.TenantId,
                        source.SourceParticipantId,
                        source.AddressEvidence,
                        source.EvidenceReference,
                        source.EvidenceFingerprint),
                    cancellationToken)
                .ConfigureAwait(false);

            if (resolution.Resolved is not null)
            {
                resolved.Add(resolution.Resolved);
            }

            if (resolution.Unresolved is not null)
            {
                unresolved.Add(resolution.Unresolved);
            }
        }

        return command with
        {
            ResolvedParticipants = resolved,
            UnresolvedParticipants = unresolved,
        };
    }
}
