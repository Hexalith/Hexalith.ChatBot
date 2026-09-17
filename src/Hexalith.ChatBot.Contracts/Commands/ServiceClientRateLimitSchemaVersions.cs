using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74/FR75 service-client rate-limit (Story 7.17) command. A dedicated constant —
/// not <see cref="ServiceClientControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>ServiceClientControlState</c> old/new-state fields.
/// </summary>
public static class ServiceClientRateLimitSchemaVersions
{
    public const string V1 = "service-client-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
