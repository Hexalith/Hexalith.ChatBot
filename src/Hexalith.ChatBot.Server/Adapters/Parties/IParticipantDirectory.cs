using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal interface IParticipantDirectory
{
    ValueTask<ParticipantDirectoryResolution> ResolveEmailEvidenceAsync(
        ParticipantDirectoryLookup lookup,
        CancellationToken cancellationToken);
}

internal sealed record ParticipantDirectoryLookup(
    string TenantId,
    string SourceParticipantId,
    string AddressEvidence,
    string EvidenceReference,
    string EvidenceFingerprint);

internal sealed record ParticipantDirectoryResolution(
    ParticipantResolutionStatus Status,
    ResolvedMailboxParticipantReference? Resolved,
    UnresolvedMailboxParticipantEvidence? Unresolved)
{
    public static ParticipantDirectoryResolution FromResolved(ResolvedMailboxParticipantReference resolved)
        => new(ParticipantResolutionStatus.Resolved, resolved, null);

    public static ParticipantDirectoryResolution FromUnresolved(
        ParticipantDirectoryLookup lookup,
        ParticipantResolutionBlockedReason reason)
        => new(
            ParticipantResolutionStatus.Unresolved,
            null,
            new UnresolvedMailboxParticipantEvidence(
                lookup.SourceParticipantId,
                lookup.EvidenceReference,
                lookup.EvidenceFingerprint,
                reason,
                AllowedReviewActions()));

    private static IReadOnlyList<ParticipantReviewAction> AllowedReviewActions()
        =>
        [
            ParticipantReviewAction.Link,
            ParticipantReviewAction.CreatePending,
            ParticipantReviewAction.Reject,
            ParticipantReviewAction.Quarantine,
        ];
}
