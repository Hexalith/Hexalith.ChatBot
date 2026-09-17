using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74/FR75 mailbox-source rate-limit (Story 7.14) command. A dedicated constant —
/// not <see cref="MailboxSourceControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>MailboxSourceControlState</c> old/new-state fields.
/// </summary>
public static class MailboxSourceRateLimitSchemaVersions
{
    public const string V1 = "mailbox-source-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
