using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderDelegationEvidence(
    string DelegateRequesterId,
    string PrincipalForId,
    bool RevokedSincePolicySnapshot,
    string EvidenceRef);
