using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Governance.AiActor;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.CommandCapability;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Governance.Mailbox;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Governance.ServiceClient;

namespace Hexalith.ChatBot.Server.Operations;

public sealed record CorrectionPropagationStoreAcknowledgement(
    string StoreKey,
    long SourceVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string Outcome,
    string? FailureReasonCode,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion)
{
    public bool IsSuccessful => string.Equals(Outcome, "success", StringComparison.Ordinal);
}
