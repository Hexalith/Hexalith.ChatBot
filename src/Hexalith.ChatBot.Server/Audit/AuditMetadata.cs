namespace Hexalith.ChatBot.Server.Audit;

internal static class AuditMetadata
{
    public const string UnknownCommandName = "unknown_command";
    public const string DefaultActorType = "user";

    public static string SafeCommandName(string? value)
        => IsSafeToken(value, 160) ? value! : UnknownCommandName;

    public static string SafeActorType(string? value)
        => IsSafeActorType(value) ? value! : DefaultActorType;

    public static string? SafeOptionalToken(string? value)
        => IsSafeToken(value, 200) ? value : null;

    public static bool IsSafeStableIdentifier(string? value)
        => IsSafeToken(value, 200);

    private static bool IsSafeActorType(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 64 &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private static bool IsSafeToken(string? value, int maxLength)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= maxLength &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|');

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
