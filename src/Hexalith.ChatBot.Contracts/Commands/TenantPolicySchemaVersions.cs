using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class TenantPolicySchemaVersions
{
    public const string M0 = "tenant-policy-schema.m0.v1";
    public const string M1Preview = "tenant-policy-schema.m1-preview.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([M0, M1Preview], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}
