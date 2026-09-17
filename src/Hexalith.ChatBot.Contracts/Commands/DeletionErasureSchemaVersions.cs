using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.9 deletion/erasure run artifact and its governed request command. Mirrors
/// <see cref="TenantExportSchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class DeletionErasureSchemaVersions
{
    public const string V1 = "deletion-erasure-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
