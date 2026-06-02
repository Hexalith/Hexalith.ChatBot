namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Wire-token companion for <see cref="RiskClass"/>. Adds a deterministic ordering helper (the priority "risk-class"
/// dimension for Story 7.8 approval prioritization) and a mapping from the queue item Risk proxy
/// (<c>low</c>/<c>medium</c>/<c>high</c>/<c>critical</c>) onto the declared <see cref="RiskClass"/> ladder. The declared
/// tokens are the only ones compared after the trust boundary; unknown/undeclared values collapse to the lowest declared
/// rank (<see cref="RiskClass.None"/>) — fail-safe, never fail-open to top priority.
/// </summary>
public static class RiskClasses
{
    public const string None = "none";
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Blocked = "blocked";

    public static IReadOnlyList<RiskClass> All { get; } =
    [
        RiskClass.None,
        RiskClass.Low,
        RiskClass.Medium,
        RiskClass.High,
        RiskClass.Blocked,
    ];

    /// <summary>Deterministic rank: none(0) &lt; low(1) &lt; medium(2) &lt; high(3) &lt; blocked(4).</summary>
    public static int Rank(RiskClass riskClass)
        => riskClass switch
        {
            RiskClass.None => 0,
            RiskClass.Low => 1,
            RiskClass.Medium => 2,
            RiskClass.High => 3,
            RiskClass.Blocked => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(riskClass), riskClass, "Unsupported risk class."),
        };

    /// <summary>Returns <see langword="true"/> when <paramref name="candidate"/> meets or exceeds <paramref name="threshold"/>.</summary>
    public static bool MeetsOrExceeds(RiskClass candidate, RiskClass threshold)
        => Rank(candidate) >= Rank(threshold);

    public static bool TryFromWireValue(string? value, out RiskClass riskClass)
    {
        riskClass = RiskClass.None;
        switch (value?.Trim().ToLowerInvariant())
        {
            case None:
                riskClass = RiskClass.None;
                return true;
            case Low:
                riskClass = RiskClass.Low;
                return true;
            case Medium:
                riskClass = RiskClass.Medium;
                return true;
            case High:
                riskClass = RiskClass.High;
                return true;
            case Blocked:
                riskClass = RiskClass.Blocked;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Maps the operational-queue Risk proxy (<c>low</c>/<c>medium</c>/<c>high</c>/<c>critical</c>) onto the declared
    /// <see cref="RiskClass"/> ladder, collapsing the queue's <c>critical</c> proxy onto <see cref="RiskClass.High"/>
    /// and any unknown/missing value onto <see cref="RiskClass.None"/> (fail-safe lowest). Free-form risk strings are
    /// never compared directly after the trust boundary — they are projected onto this finite ladder first.
    /// </summary>
    public static RiskClass FromRiskProxy(string? risk)
        => risk?.Trim().ToLowerInvariant() switch
        {
            "critical" => RiskClass.High,
            High => RiskClass.High,
            Medium => RiskClass.Medium,
            Low => RiskClass.Low,
            None => RiskClass.None,
            _ => RiskClass.None,
        };

    public static string ToWireValue(RiskClass riskClass)
        => riskClass switch
        {
            RiskClass.None => None,
            RiskClass.Low => Low,
            RiskClass.Medium => Medium,
            RiskClass.High => High,
            RiskClass.Blocked => Blocked,
            _ => throw new ArgumentOutOfRangeException(nameof(riskClass), riskClass, "Unsupported risk class."),
        };
}
