using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Governance.Admin;

internal static class AdminAuthorityEvaluator
{
    public static bool HasHumanAdminScope(ClaimsPrincipal principal, AdminScope requiredScope)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return IsHumanActor(principal) &&
            principal
                .FindAll(ParticipantAuthorizationStage.TenantRoleClaim)
                .Select(static claim => claim.Value)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(TryRole)
                .Where(static role => role.HasValue)
                .SelectMany(static role => AdminScopes.ScopesForRole(role!.Value))
                .Contains(requiredScope);
    }

    public static bool HasHumanRole(ClaimsPrincipal principal, AdminRole requiredRole)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return IsHumanActor(principal) &&
            principal
                .FindAll(ParticipantAuthorizationStage.TenantRoleClaim)
                .Any(claim => AdminRoles.TryFromWireValue(claim.Value, out AdminRole role) && role == requiredRole);
    }

    public static bool HasHumanTenantAdmin(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return IsHumanActor(principal) &&
            principal
                .FindAll(ParticipantAuthorizationStage.TenantRoleClaim)
                .Any(static claim => AdminRoles.TryFromWireValue(claim.Value, out AdminRole role) && role == AdminRole.TenantAdmin);
    }

    /// <summary>
    /// Shared per-project authority check: true when the principal carries a <c>chatbot:project-owner</c> claim
    /// matching <paramref name="projectRef"/>. When <paramref name="allowWildcard"/> is <see langword="true"/> (the
    /// default, matching the gateway/outbound/notification-routing convention) a tenant-wide <c>"*"</c> owner grant
    /// satisfies any project. Callers that require an <b>explicit</b> per-project grant matching a specific resource
    /// token — notably compliance full-detail read-back (Story 9.3 / NFR2) — pass <paramref name="allowWildcard"/>
    /// <see langword="false"/> so the blanket wildcard cannot widen detail beyond the records the principal explicitly
    /// owns.
    /// </summary>
    public static bool HasProjectAuthority(ClaimsPrincipal principal, string projectRef, bool allowWildcard = true)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (string.IsNullOrWhiteSpace(projectRef))
        {
            return false;
        }

        string[] ownedProjects = principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return (allowWildcard && ownedProjects.Contains("*", StringComparer.Ordinal)) ||
            ownedProjects.Contains(projectRef, StringComparer.Ordinal);
    }

    private static bool IsHumanActor(ClaimsPrincipal principal)
        => principal.HasClaim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue);

    private static AdminRole? TryRole(string value)
        => AdminRoles.TryFromWireValue(value, out AdminRole role) ? role : null;
}
