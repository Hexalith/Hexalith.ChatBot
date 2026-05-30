using System.Diagnostics.CodeAnalysis;

using Ulid = ByteAether.Ulid.Ulid;

namespace Hexalith.ChatBot.Contracts.Identities;

public static class ChatBotIdentity
{
    public static bool IsValidUlid([NotNullWhen(true)] string? value)
        => TryNormalizeUlid(value, out _);

    public static string NewUlid()
        => Ulid.New().ToString();

    internal static bool TryNormalizeUlid(string? value, [NotNullWhen(true)] out string? normalized)
    {
        if (!string.IsNullOrWhiteSpace(value) && Ulid.TryParse(value, null, out Ulid ulid))
        {
            normalized = ulid.ToString();
            return true;
        }

        normalized = null;
        return false;
    }
}
