using System.Security.Cryptography;
using System.Text;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Closed controller-secret and tenant guard for recovery sandbox HTTP routes.</summary>
internal static class RecoverySandboxAuthorization
{
    /// <summary>Returns whether the request may mutate or observe the configured recovery tenant.</summary>
    public static bool Authorized(
        string requestedTenant,
        string configuredTenant,
        string configuredSecret,
        string? presentedSecret)
    {
        if (!string.Equals(requestedTenant, configuredTenant, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(presentedSecret))
        {
            return false;
        }

        byte[] configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredSecret));
        byte[] presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presentedSecret));
        return CryptographicOperations.FixedTimeEquals(configuredHash, presentedHash);
    }
}
