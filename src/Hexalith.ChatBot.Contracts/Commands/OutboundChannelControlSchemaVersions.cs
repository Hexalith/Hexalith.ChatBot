using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 outbound-channel control (disable) commands. A dedicated constant — not
/// <see cref="CommandCapabilityControlSchemaVersions.V1"/> — because the subject diverges from the command-capability
/// control plane: the outbound channel is identified by its safe channel ref (<c>OutboundChannelRef</c> — the
/// <c>AdapterRef</c> token, e.g. <c>adapter:mailbox-outbound</c>), not a command type name or actor id. Mirrors
/// <see cref="CommandCapabilityControlSchemaVersions"/>.
/// </summary>
public static class OutboundChannelControlSchemaVersions
{
    public const string V1 = "outbound-channel-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
