using System.Globalization;
using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ClaimsServiceClientGrantResolver : IServiceClientGrantResolver
{
    public const string ServiceClientIdClaim = "chatbot:service-client-id";
    public const string ServiceClientClassClaim = "chatbot:service-client-class";
    public const string GrantIdClaim = "chatbot:service-client-grant-id";
    public const string GrantTenantClaim = "chatbot:service-client-grant-tenant";
    public const string GrantExpiryClaim = "chatbot:service-client-grant-expiry";
    public const string GrantRevokedClaim = "chatbot:service-client-grant-revoked";
    public const string GrantScopeClaim = "chatbot:service-client-scope";
    public const string GrantCommandClaim = "chatbot:service-client-command";
    public const string GrantQueryClaim = "chatbot:service-client-query";
    public const string GrantSurfaceClaim = "chatbot:service-client-surface";
    public const string DelegatedUserIdClaim = "chatbot:delegated-user-id";
    public const string OAuthGrantEvidenceFingerprintClaim = "chatbot:oauth-grant-fingerprint";
    public const string CommandSetVersionClaim = "chatbot:service-client-command-set-version";

    public ValueTask<ServiceClientGrantResolution> ResolveAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        cancellationToken.ThrowIfCancellationRequested();

        ClaimsPrincipal principal = actor.Principal;
        if (!TryReadSingleSafe(principal, ServiceClientIdClaim, out string? serviceClientId, out string reasonCode) ||
            !TryReadSingleSafe(principal, GrantIdClaim, out string? grantId, out reasonCode) ||
            !TryReadSingleSafe(principal, GrantTenantClaim, out string? grantTenant, out reasonCode) ||
            !TryReadSingleSafe(principal, CommandSetVersionClaim, out string? commandSetVersion, out reasonCode))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(reasonCode));
        }

        if (!TryReadSingleSafe(principal, ServiceClientClassClaim, out string? classToken, out reasonCode) ||
            !ServiceClientClasses.TryFromWireValue(classToken, out ServiceClientClass clientClass))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing));
        }

        if (!TryReadSingleSafe(principal, GrantSurfaceClaim, out string? surfaceToken, out reasonCode))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(reasonCode));
        }

        ChatBotSurfaceOrigin surfaceOrigin = ChatBotSurfaceOrigins.FromWireValueOrDefault(surfaceToken);
        if (!string.Equals(ChatBotSurfaceOrigins.ToWireValue(surfaceOrigin), surfaceToken, StringComparison.Ordinal))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface));
        }

        if (!TryReadSingleSafe(principal, GrantExpiryClaim, out string? expiresAtToken, out reasonCode) ||
            !DateTimeOffset.TryParse(
                expiresAtToken,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset expiresAt))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing));
        }

        string[] scopes = ReadSafeValues(principal, GrantScopeClaim);
        string[] commandNames = ReadCommandValues(principal, GrantCommandClaim);
        if (scopes.Length == 0 || commandNames.Length == 0)
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing));
        }

        string[] queryNames = ReadSafeValues(principal, GrantQueryClaim);
        bool isRevoked = principal
            .FindAll(GrantRevokedClaim)
            .Select(static claim => claim.Value)
            .Any(static value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        string? delegatedUserId = ReadSingleOptionalSafe(principal, DelegatedUserIdClaim, out reasonCode);
        if (!string.IsNullOrEmpty(reasonCode))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(reasonCode));
        }

        string? oauthFingerprint = ReadSingleOptionalSafe(principal, OAuthGrantEvidenceFingerprintClaim, out reasonCode);
        if (!string.IsNullOrEmpty(reasonCode))
        {
            return ValueTask.FromResult(ServiceClientGrantResolution.Denied(reasonCode));
        }

        return ValueTask.FromResult(ServiceClientGrantResolution.Resolved(new ServiceClientGrant(
            grantId!,
            grantTenant!,
            serviceClientId!,
            clientClass,
            commandNames,
            queryNames,
            surfaceOrigin,
            expiresAt,
            isRevoked,
            scopes,
            commandSetVersion!,
            delegatedUserId,
            oauthFingerprint)));
    }

    private static bool TryReadSingleSafe(ClaimsPrincipal principal, string claimType, out string? value, out string reasonCode)
    {
        string[] values = principal
            .FindAll(claimType)
            .Select(static claim => claim.Value)
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        value = null;
        reasonCode = string.Empty;
        if (values.Length == 0)
        {
            reasonCode = ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing;
            return false;
        }

        if (values.Length != 1)
        {
            reasonCode = ChatBotAuthorizationReasonCodes.ServiceClientGrantAmbiguous;
            return false;
        }

        if (!AuditMetadata.IsSafeStableIdentifier(values[0]))
        {
            reasonCode = ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing;
            return false;
        }

        value = values[0];
        return true;
    }

    private static string[] ReadSafeValues(ClaimsPrincipal principal, string claimType)
        => principal
            .FindAll(claimType)
            .Select(static claim => claim.Value)
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string[] ReadCommandValues(ClaimsPrincipal principal, string claimType)
        => principal
            .FindAll(claimType)
            .Select(static claim => claim.Value)
            .Where(static value => string.Equals(value, "*", StringComparison.Ordinal) || AuditMetadata.IsSafeStableIdentifier(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string? ReadSingleOptionalSafe(ClaimsPrincipal principal, string claimType, out string reasonCode)
    {
        reasonCode = string.Empty;
        string[] values = principal
            .FindAll(claimType)
            .Select(static claim => claim.Value)
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (values.Length == 0)
        {
            return null;
        }

        if (values.Length != 1 || !AuditMetadata.IsSafeStableIdentifier(values[0]))
        {
            reasonCode = values.Length == 1
                ? ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing
                : ChatBotAuthorizationReasonCodes.ServiceClientGrantAmbiguous;
            return null;
        }

        return values[0];
    }
}
