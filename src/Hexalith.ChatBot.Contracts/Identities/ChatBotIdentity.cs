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
        if (!string.IsNullOrWhiteSpace(value) &&
            value.Length == 26 &&
            value.All(IsCrockfordBase32UlidCharacter) &&
            Ulid.TryParse(value, null, out Ulid ulid))
        {
            normalized = ulid.ToString();
            return true;
        }

        normalized = null;
        return false;
    }

    private static bool IsCrockfordBase32UlidCharacter(char value)
        => char.ToUpperInvariant(value) is >= '0' and <= '9' or
            'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G' or 'H' or
            'J' or 'K' or 'M' or 'N' or 'P' or 'Q' or 'R' or 'S' or
            'T' or 'V' or 'W' or 'X' or 'Y' or 'Z';
}
