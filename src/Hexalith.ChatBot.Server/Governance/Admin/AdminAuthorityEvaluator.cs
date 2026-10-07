using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authorization;

namespace Hexalith.ChatBot.Server.Governance.Admin;

/// <summary>Reads only server-issued current owner decisions; token roles and wildcards confer no authority.</summary>
internal static class AdminAuthorityEvaluator
{
    /// <summary>Checks the exact operation-scoped human grant.</summary>
    public static bool HasHumanAdminScope(ClaimsPrincipal principal, AdminScope requiredScope)
        => principal is ChatBotAuthorityPrincipal { Context.IsMachine: false } authority && authority.HasAdminScope(AdminScopes.ToWireValue(requiredScope));

    /// <summary>Checks a current human role within the authorized operation scope.</summary>
    public static bool HasHumanRole(ClaimsPrincipal principal, AdminRole requiredRole)
        => principal is ChatBotAuthorityPrincipal { Context.IsMachine: false } authority && authority.HasAdminRole(requiredRole);

    /// <summary>Checks the exact tenant administration grant and current TenantOwner evidence.</summary>
    public static bool HasHumanTenantAdmin(ClaimsPrincipal principal)
        => principal is ChatBotAuthorityPrincipal { Context.IsMachine: false } authority && authority.HasAdminScope("tenant");

    /// <summary>Checks exact current Projects authority. Wildcards never widen scope.</summary>
    public static bool HasProjectAuthority(ClaimsPrincipal principal, string projectRef)
        => principal is ChatBotAuthorityPrincipal authority && authority.HasProject(projectRef);
}
