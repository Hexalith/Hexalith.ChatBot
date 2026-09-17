using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed redaction-sensitivity dimension (NFR52, architecture cross-cutting #7). Tenants/editors may select
/// within this bounded set but never invent members. Mirrors the <see cref="ComplianceRetentionClassIds"/> shape.
/// </summary>
public static class DataClassRedactionSensitivities
{
    public const string Restricted = "restricted";
    public const string Sensitive = "sensitive";
    public const string Internal = "internal";
    public const string MetadataOnly = "metadata-only";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Restricted, Sensitive, Internal, MetadataOnly], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
