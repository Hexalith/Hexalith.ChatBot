using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed run-status dimension (AC3). Mirrors <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportRunStatuses
{
    public const string Completed = "completed";
    public const string PartialFailure = "partial-failure";
    public const string Failed = "failed";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Completed, PartialFailure, Failed], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
