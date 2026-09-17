using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74/FR75 outbound-channel rate-limit (Story 7.26) command. A dedicated constant —
/// not <see cref="OutboundChannelControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>OutboundChannelControlState</c> old/new-state fields. Mirrors
/// <see cref="CommandCapabilityRateLimitSchemaVersions"/>.
/// </summary>
public static class OutboundChannelRateLimitSchemaVersions
{
    public const string V1 = "outbound-channel-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
