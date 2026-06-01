using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Identities;

public sealed record ServiceClientGrant(
    string GrantId,
    string TenantId,
    string ServiceClientId,
    ServiceClientClass ClientClass,
    IReadOnlyList<string> AllowedCommandNames,
    IReadOnlyList<string> AllowedQueryNames,
    ChatBotSurfaceOrigin SurfaceOrigin,
    DateTimeOffset ExpiresAt,
    bool IsRevoked,
    IReadOnlyList<string> Scopes,
    string CommandSetVersion,
    string? DelegatedUserId = null,
    string? OAuthGrantEvidenceFingerprint = null);
