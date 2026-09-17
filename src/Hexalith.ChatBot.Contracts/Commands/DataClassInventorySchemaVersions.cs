using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.7 data-class inventory artifact and its governed change command. Mirrors
/// <see cref="ComplianceAdministrationSchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class DataClassInventorySchemaVersions
{
    public const string V1 = "data-class-inventory-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
