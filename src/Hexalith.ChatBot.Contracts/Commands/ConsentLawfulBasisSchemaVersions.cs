using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.10 consent/lawful-basis record artifact and its governed recording command.
/// Mirrors <see cref="DeletionErasureSchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class ConsentLawfulBasisSchemaVersions
{
    public const string V1 = "consent-lawful-basis-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
