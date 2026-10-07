using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>Binds only authenticated evidence and rejects ambiguous or malformed evidence.</summary>
internal sealed class ChatBotRequestContextResolver(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Resolves the authenticated transport context for SDK reads.</summary>
    public ChatBotRequestContext? ResolveCurrent()
    {
        HttpContext? http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return null;
        }

        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigins.FromWireValueOrDefault(http.Request.Headers["X-Hexalith-Surface-Origin"].ToString());
        return TryResolve(http.User, origin, out ChatBotRequestContext? context, out _) && context?.TenantId is not null ? context : null;
    }

    /// <summary>Creates a snapshot from authenticated identities without trusting supplemental identities.</summary>
    public static bool TryResolve(ClaimsPrincipal principal, ChatBotSurfaceOrigin origin, out ChatBotRequestContext? context, out string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(principal);
        context = null;
        reasonCode = ChatBotAuthorizationReasonCodes.AuthenticationDenied;
        ClaimsIdentity[] identities = principal.Identities.Where(static identity => identity.IsAuthenticated).ToArray();
        if (identities.Length == 0)
        {
            return false;
        }

        ClaimsPrincipal authenticated = new(identities.Select(static identity => identity.Clone()));
        if (!TrySingle(authenticated, ["sub", ClaimTypes.NameIdentifier], true, out string? subject) ||
            !TrySingle(authenticated, [ParticipantAuthorizationStage.ActorTypeClaim, "actor_type"], false, out string? actor) ||
            !TrySingle(authenticated, [ClaimsServiceClientGrantResolver.ServiceClientIdClaim], false, out string? serviceClient))
        {
            return false;
        }

        if (!TrySingle(authenticated, ["eventstore:tenant", "tenant"], false, out string? tenant))
        {
            reasonCode = ChatBotAuthorizationReasonCodes.TenantMissing;
            return false;
        }

        Claim[] preferredUsernames = authenticated.Claims.Where(static claim => string.Equals(claim.Type, "preferred_username", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (preferredUsernames.Any(static claim => claim.Type != "preferred_username"))
        {
            return false;
        }

        string[] serviceAccounts = preferredUsernames
            .Select(static claim => claim.Value)
            .Where(static value => value.StartsWith("service-account-", StringComparison.Ordinal))
            .Select(static value => value["service-account-".Length..]).ToArray();
        if (serviceAccounts.Any(static value => !AuditMetadata.IsSafeStableIdentifier(value)) ||
            serviceAccounts.Distinct(StringComparer.Ordinal).Count() > 1 ||
            (serviceClient is not null && serviceAccounts.Any(value => !string.Equals(value, serviceClient, StringComparison.Ordinal))))
        {
            return false;
        }

        serviceClient ??= serviceAccounts.FirstOrDefault();
        actor ??= serviceClient is null ? "human" : "service";
        actor = actor == "user" ? "human" : actor;
        if (actor is not ("human" or "service" or "ai") ||
            (serviceClient is not null && actor == "human") ||
            (actor is "service" or "ai" && serviceClient is null) || !Enum.IsDefined(origin))
        {
            return false;
        }

        // azp/client_id identify the OAuth application on human tokens, not a machine subject.
        context = new ChatBotRequestContext(subject!, tenant, actor, serviceClient, origin, authenticated);
        reasonCode = string.Empty;
        return true;
    }

    private static bool TrySingle(ClaimsPrincipal principal, string[] types, bool required, out string? value)
    {
        Claim[] claims = principal.Claims.Where(claim => types.Contains(claim.Type, StringComparer.OrdinalIgnoreCase)).ToArray();
        value = null;
        if (claims.Any(claim => !types.Contains(claim.Type, StringComparer.Ordinal)))
        {
            return false;
        }

        string[] values = claims.Select(static claim => claim.Value).ToArray();
        if (values.Length == 0)
        {
            return !required;
        }

        if (values.Any(static candidate => !AuditMetadata.IsSafeStableIdentifier(candidate)) || values.Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return false;
        }

        value = values[0];
        return true;
    }
}
