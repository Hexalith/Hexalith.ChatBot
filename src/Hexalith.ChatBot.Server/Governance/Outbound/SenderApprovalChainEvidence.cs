using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderApprovalChainEvidence(
    string? ApprovalId,
    bool HasPairedApprovalRecord,
    string EvidenceRef);
