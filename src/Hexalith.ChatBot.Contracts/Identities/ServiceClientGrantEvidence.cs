using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Identities;

public sealed record ServiceClientGrantEvidence(
    string ServiceClientId,
    ServiceClientClass ClientClass,
    string TenantId,
    string GrantId,
    IReadOnlyList<string> Scopes,
    DateTimeOffset ExpiresAt,
    ChatBotSurfaceOrigin SurfaceOrigin,
    string CommandSetVersion,
    string? DelegatedUserId,
    string? OAuthGrantEvidenceFingerprint);
