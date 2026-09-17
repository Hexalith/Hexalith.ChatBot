using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed export-eligibility dimension (NFR52, Story 9.8 export consumes it).
/// </summary>
public static class DataClassExportEligibilities
{
    public const string Exportable = "exportable";
    public const string RedactedExport = "redacted-export";
    public const string NotExportable = "not-exportable";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Exportable, RedactedExport, NotExportable], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
