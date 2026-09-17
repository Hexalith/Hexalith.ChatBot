using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Mailbox;

/// <summary>
/// Structured rejection for an invalid or unauthorized mailbox-source disable submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record MailboxSourceDisableRejected(
    string DisableChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
