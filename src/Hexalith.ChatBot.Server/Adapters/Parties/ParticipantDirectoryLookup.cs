using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal sealed record ParticipantDirectoryLookup(
    string TenantId,
    string SourceParticipantId,
    string AddressEvidence,
    string EvidenceReference,
    string EvidenceFingerprint);
