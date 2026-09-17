using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record EscalationPolicyChangeResult(
    bool Accepted,
    string EscalationPolicyChangeId,
    string ActiveSnapshotRef,
    string ReasonCode);
