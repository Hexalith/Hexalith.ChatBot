using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only source party resolution posture used to fail safe for external senders.
/// </summary>
public sealed record MailboxExternalSenderPosture(
    bool ExternalSender,
    MailboxPartyResolutionState PartyResolutionState,
    string? ResolvedPartyRef,
    IReadOnlyList<string> EvidenceRefs);
