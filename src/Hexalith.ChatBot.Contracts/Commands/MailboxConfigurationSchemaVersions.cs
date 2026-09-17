using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class MailboxConfigurationSchemaVersions
{
    public const string V1 = "mailbox-config-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
