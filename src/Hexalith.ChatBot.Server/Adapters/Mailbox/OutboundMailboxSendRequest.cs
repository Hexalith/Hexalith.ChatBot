using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal sealed record OutboundMailboxSendRequest(
    string TenantId,
    string ProjectId,
    string DraftId,
    string ApprovalId,
    string SendId,
    string RequesterId,
    string SendActorId,
    SenderAuthorityClass SenderAuthorityClass,
    string AdapterMode,
    string CorrelationId,
    // Story 9.4 (FR95a): the replay/simulation run that issued this send, or null for a production send. Populated at the
    // dispatcher send seam from the immutable ChatBotCommandSubmission.ReplayRunId so the outbound-trace record carries
    // the same structurally-un-rewritable marker as the audit envelope. Production sends leave it null by omission.
    string? ReplayRunId = null);
