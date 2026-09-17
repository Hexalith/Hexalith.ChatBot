using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 mailbox-source control (disable + quarantine) commands.
/// Quarantine (Story 7.13) reuses <see cref="V1"/> — the two-person submit→approve command shape is identical
/// to disable, so a dedicated schema-version constant is not required.
/// </summary>
public static class MailboxSourceControlSchemaVersions
{
    public const string V1 = "mailbox-source-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
