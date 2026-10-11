using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>Computes the single EventStore stream for one exact ChatBot authority grant.</summary>
public static class ChatBotAuthorityGrantKey
{
    /// <summary>Returns the deterministic aggregate id for the complete grant scope.</summary>
    public static string For(string tenantId, string principalId, string resourceId, string operation, string authority, string actorClass, ChatBotSurfaceOrigin origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorClass);
        if (!Enum.IsDefined(origin))
        {
            throw new ArgumentOutOfRangeException(nameof(origin));
        }

        string scope = string.Join('\0', tenantId, principalId, resourceId, operation, authority, actorClass, ChatBotSurfaceOrigins.ToWireValue(origin));
        return $"authority-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope))).ToLowerInvariant()}";
    }
}
