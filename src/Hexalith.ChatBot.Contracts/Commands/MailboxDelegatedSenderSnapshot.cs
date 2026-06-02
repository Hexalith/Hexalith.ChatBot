using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only delegated sender posture captured from provider identity and selected header evidence.
/// </summary>
public sealed record MailboxDelegatedSenderSnapshot(
    MailboxDelegatedSenderState State,
    MailboxParticipantIdentity? Delegate,
    MailboxParticipantIdentity? PrincipalFor,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<MailboxHeaderDiscrepancyKind> Discrepancies);
