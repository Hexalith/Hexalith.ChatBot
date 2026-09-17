using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

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
