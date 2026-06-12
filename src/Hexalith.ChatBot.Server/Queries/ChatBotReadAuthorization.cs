using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Queries;

internal static class ChatBotReadAuthorization
{
    public static bool TryResolveTenant(
        ClaimsPrincipal principal,
        out string? tenantId,
        out string? userId,
        out string reasonCode)
    {
        tenantId = null;
        userId = null;
        reasonCode = ChatBotAuthorizationReasonCodes.AuthenticationDenied;

        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return false;
        }

        string? actorId = principal.FindFirstValue("sub");
        if (!AuditMetadata.IsSafeStableIdentifier(actorId))
        {
            return false;
        }

        string[] tenantClaims = ["eventstore:tenant", "tenant"];
        string[] tenants = tenantClaims
            .SelectMany(principal.FindAll)
            .Select(static claim => claim.Value)
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (tenants.Length != 1)
        {
            reasonCode = ChatBotAuthorizationReasonCodes.TenantMissing;
            return false;
        }

        tenantId = tenants[0];
        userId = actorId;
        reasonCode = string.Empty;
        return true;
    }

    public static bool TryAuthorizeProjectRead(ClaimsPrincipal principal, string projectId, out bool hasProjectScopeClaims)
    {
        hasProjectScopeClaims = false;
        string[] projectClaims = principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (projectClaims.Length == 0)
        {
            return false;
        }

        hasProjectScopeClaims = true;
        if (projectClaims.Any(static value => !string.Equals(value, "*", StringComparison.Ordinal) && !AuditMetadata.IsSafeStableIdentifier(value)))
        {
            return false;
        }

        return projectClaims.Contains("*", StringComparer.Ordinal) ||
            projectClaims.Contains(projectId, StringComparer.Ordinal);
    }

    public static bool CanSearchTenantAudit(ClaimsPrincipal principal)
        => AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance);

    public static IReadOnlyList<string> ExplicitProjectGrants(ClaimsPrincipal principal)
        => principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => AuditMetadata.IsSafeStableIdentifier(value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public static string ReadDenialReason(string reasonCode)
        => string.Equals(reasonCode, ChatBotAuthorizationReasonCodes.AuthenticationDenied, StringComparison.Ordinal)
            ? reasonCode
            : ChatBotAuthorizationReasonCodes.SafeNotFound;
}
