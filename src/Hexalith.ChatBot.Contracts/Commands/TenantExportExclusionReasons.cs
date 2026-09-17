using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed exclusion-reason dimension (AC1/AC2). <see cref="Unauthorized"/> is the only signal a hidden
/// resource ever produces — never the resource identity (NFR2). Mirrors <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportExclusionReasons
{
    public const string NotExportable = "not-exportable";
    public const string Unauthorized = "unauthorized";
    public const string NotRequested = "not-requested";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([NotExportable, Unauthorized, NotRequested], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
