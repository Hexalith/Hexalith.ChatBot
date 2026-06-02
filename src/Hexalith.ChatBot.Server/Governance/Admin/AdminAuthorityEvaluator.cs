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

    public static bool HasHumanTenantAdmin(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return IsHumanActor(principal) &&
            principal
                .FindAll(ParticipantAuthorizationStage.TenantRoleClaim)
                .Any(static claim => AdminRoles.TryFromWireValue(claim.Value, out AdminRole role) && role == AdminRole.TenantAdmin);
    }

    private static bool IsHumanActor(ClaimsPrincipal principal)
        => principal.HasClaim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue);

    private static AdminRole? TryRole(string value)
        => AdminRoles.TryFromWireValue(value, out AdminRole role) ? role : null;
}
