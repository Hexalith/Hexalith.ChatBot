namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Validates the canonical metadata bounds published in the API contract.</summary>
internal static class CommandOperationMetadataValidation
{
    /// <summary>Validates the published bounded actor token.</summary>
    internal static bool IsValidActorId(string? actorId) => IsValidToken(actorId, allowAt: true);

    /// <summary>Validates the published bounded resource token.</summary>
    internal static bool IsValidResourceId(string? resourceId) => IsValidToken(resourceId, allowAt: false);

    private static bool IsValidToken(string? value, bool allowAt)
        => value is { Length: > 0 and <= 240 } && value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is ':' or '.' or '_' or '-' || (allowAt && character == '@'));
}
