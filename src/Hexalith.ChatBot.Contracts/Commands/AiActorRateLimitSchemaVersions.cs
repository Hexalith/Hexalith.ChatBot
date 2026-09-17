using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74/FR75 AI-actor rate-limit (Story 7.20) command. A dedicated constant —
/// not <see cref="AiActorControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>AiActorControlState</c> old/new-state fields. Mirrors
/// <see cref="ServiceClientRateLimitSchemaVersions"/>.
/// </summary>
public static class AiActorRateLimitSchemaVersions
{
    public const string V1 = "ai-actor-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
