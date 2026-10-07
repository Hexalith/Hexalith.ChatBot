using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Metadata-only current evidence. No names, emails, credentials or owner response payloads are retained.</summary>
/// <param name="Request">The exact request proven by the owner.</param>
/// <param name="EvidenceId">The safe evidence reference.</param>
/// <param name="Version">The nonempty immutable owner version.</param>
/// <param name="ObservedAt">When the authority was observed.</param>
/// <param name="RevocationCheckedAt">When current revocation was checked.</param>
/// <param name="ExpiresAt">The owner validity limit.</param>
/// <param name="IsAllowed">Whether the owner granted the exact authority.</param>
/// <param name="IsRevoked">Whether revocation is known.</param>
/// <param name="ServiceGrant">The current scoped machine grant when requested.</param>
internal sealed record ChatBotOwnerAuthorityEvidence(ChatBotOwnerAuthorityRequest Request, string EvidenceId, string Version, DateTimeOffset ObservedAt, DateTimeOffset RevocationCheckedAt, DateTimeOffset ExpiresAt, bool IsAllowed, bool IsRevoked, ServiceClientGrant? ServiceGrant = null);
