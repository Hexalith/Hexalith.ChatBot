using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed denial/exclusion-reason dimension (AC1/AC2). <see cref="WormRetained"/> is the absolute WORM-class
/// reason (audit-records are never destroyed); <see cref="Unauthorized"/> is the only signal a hidden resource ever
/// produces — never the resource identity (NFR2). Mirrors <see cref="TenantExportExclusionReasons"/>.
/// </summary>
public static class DeletionErasureExclusionReasons
{
    public const string WormRetained = "worm-retained";
    public const string Unauthorized = "unauthorized";
    public const string NotRequested = "not-requested";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([WormRetained, Unauthorized, NotRequested], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
