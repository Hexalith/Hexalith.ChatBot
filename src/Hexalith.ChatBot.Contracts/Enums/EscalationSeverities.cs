namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Wire-token companion for <see cref="EscalationSeverity"/>. Adds a deterministic ordering helper and a mapping
/// from the queue item Risk proxy to the declared severity ladder. The declared tokens are the only ones the
/// escalation schema accepts after the trust boundary.
/// </summary>
public static class EscalationSeverities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";

    public static IReadOnlyList<EscalationSeverity> All { get; } =
    [
        EscalationSeverity.Low,
        EscalationSeverity.Medium,
        EscalationSeverity.High,
    ];

    /// <summary>Deterministic rank: low(0) &lt; medium(1) &lt; high(2). Used for at-or-above threshold comparisons.</summary>
    public static int Rank(EscalationSeverity severity)
        => severity switch
        {
            EscalationSeverity.Low => 0,
            EscalationSeverity.Medium => 1,
            EscalationSeverity.High => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported escalation severity."),
        };

    /// <summary>Returns <see langword="true"/> when <paramref name="candidate"/> meets or exceeds <paramref name="threshold"/>.</summary>
    public static bool MeetsOrExceeds(EscalationSeverity candidate, EscalationSeverity threshold)
        => Rank(candidate) >= Rank(threshold);

    public static bool TryFromWireValue(string? value, out EscalationSeverity severity)
    {
        severity = EscalationSeverity.Low;
        switch (value?.Trim().ToLowerInvariant())
        {
            case Low:
                severity = EscalationSeverity.Low;
                return true;
            case Medium:
                severity = EscalationSeverity.Medium;
                return true;
            case High:
                severity = EscalationSeverity.High;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Maps the queue item Risk proxy (<c>low</c>/<c>medium</c>/<c>high</c>) to the declared severity ladder,
    /// defaulting to <see cref="EscalationSeverity.Medium"/> for unknown/missing values. Free-form risk strings are
    /// never compared directly after the trust boundary — they are projected onto this finite ladder first.
    /// </summary>
    public static EscalationSeverity FromRisk(string? risk)
        => TryFromWireValue(risk, out EscalationSeverity severity) ? severity : EscalationSeverity.Medium;

    public static string ToWireValue(EscalationSeverity severity)
        => severity switch
        {
            EscalationSeverity.Low => Low,
            EscalationSeverity.Medium => Medium,
            EscalationSeverity.High => High,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported escalation severity."),
        };
}
