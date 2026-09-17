using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed per-class redaction-decision dimension (AC1, NFR45). Distinct from the source
/// <see cref="DataClassRedactionSensitivities"/> dimension — this is the export-time decision token, never raw
/// content. Mirrors <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportRedactionDecisions
{
    public const string MetadataOnly = "metadata-only";
    public const string Redacted = "redacted";
    public const string None = "none";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([MetadataOnly, Redacted, None], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
