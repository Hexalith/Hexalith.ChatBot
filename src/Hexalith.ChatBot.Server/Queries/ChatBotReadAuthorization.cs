using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Queries;

/// <summary>Compatibility binding helper; all read authority is enforced by the shared SDK handler boundary.</summary>
internal static class ChatBotReadAuthorization
{
    /// <summary>Resolves only the immutable authenticated subject and tenant.</summary>
    public static bool TryResolveTenant(ClaimsPrincipal principal, out string? tenantId, out string? userId, out string reasonCode)
    {
        tenantId = null;
        userId = null;
        if (!ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out reasonCode))
        {
            return false;
        }

        if (context?.TenantId is null)
        {
            reasonCode = ChatBotAuthorizationReasonCodes.TenantMissing;
            return false;
        }

        tenantId = context.TenantId;
        userId = context.SubjectId;
        return true;
    }

    /// <summary>Normalizes existence-neutral read denials.</summary>
    public static string ReadDenialReason(string reasonCode)
        => reasonCode == ChatBotAuthorizationReasonCodes.AuthenticationDenied ? reasonCode : ChatBotAuthorizationReasonCodes.SafeNotFound;
}
