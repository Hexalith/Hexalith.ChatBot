using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderServiceClientGrantEvidence(
    string ServiceClientId,
    bool HasOutboundGrant,
    string EvidenceRef);
