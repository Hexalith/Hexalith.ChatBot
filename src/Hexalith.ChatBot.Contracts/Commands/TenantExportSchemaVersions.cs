using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.8 tenant-export artifact and its governed request command. Mirrors
/// <see cref="DataClassInventorySchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class TenantExportSchemaVersions
{
    public const string V1 = "tenant-export-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
