using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 command-capability control (disable) commands. A dedicated constant —
/// not <see cref="AiActorControlSchemaVersions.V1"/> — because the subject diverges from the AI-actor control
/// plane: the command capability is identified by its safe command <em>type name</em>
/// (<c>CommandCapabilityRef</c>), not an actor id. Mirrors <see cref="AiActorControlSchemaVersions"/>.
/// </summary>
public static class CommandCapabilityControlSchemaVersions
{
    public const string V1 = "command-capability-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
