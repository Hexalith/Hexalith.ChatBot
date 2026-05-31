using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Safe unresolved participant state for reviewer action without exposing additional identity data.
/// </summary>
public sealed record UnresolvedMailboxParticipantEvidence(
    string SourceParticipantId,
    string EvidenceReference,
    string EvidenceFingerprint,
    ParticipantResolutionBlockedReason Reason,
    IReadOnlyList<ParticipantReviewAction> AllowedReviewActions);
