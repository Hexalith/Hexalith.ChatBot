namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded per-recipient notification ceiling set (Story 7.9, NFR46/FR75d). Exactly two declared
/// window dimensions — <see cref="HourlyCeiling"/> (rolling 60 minutes) and <see cref="DailyCeiling"/> (rolling 24
/// hours) — each a bounded non-negative integer. This is deliberately NOT a free-form map or expression: a tenant
/// cannot introduce a new window dimension or a custom formula, and it can only ever <em>lower</em> a ceiling — the
/// inclusive maximums (<see cref="HourlyMaximum"/> = 8, <see cref="DailyMaximum"/> = 30) are the hard NFR46 governance
/// cap. Out-of-range (including above the maximum), wrong-type, NaN/Infinity, or undeclared values are rejected by the
/// Tenant Policy Schema with the existing safe reason codes and the evaluator falls back to <see cref="SafeDefaults"/>
/// (8/hr, 30/day = the NFR46 maximums). This is a Standard triage-tuning knob — not security-sensitive — so no blanket
/// two-person rule applies.
/// </summary>
/// <param name="HourlyCeiling">Maximum immediate pushes delivered to a recipient per rolling 60 minutes.</param>
/// <param name="DailyCeiling">Maximum immediate pushes delivered to a recipient per rolling 24 hours.</param>
public sealed record NotificationThrottleCeilings(
    int HourlyCeiling,
    int DailyCeiling)
{
    /// <summary>The inclusive lower bound for every declared ceiling (non-negative; a tenant may lower to 0).</summary>
    public const int Minimum = 0;

    /// <summary>The inclusive upper bound for the hourly ceiling — the hard NFR46 governance cap.</summary>
    public const int HourlyMaximum = 8;

    /// <summary>The inclusive upper bound for the daily ceiling — the hard NFR46 governance cap.</summary>
    public const int DailyMaximum = 30;

    /// <summary>
    /// The declared safe defaults applied when the tenant has not set the knob or set an invalid value. They equal the
    /// NFR46 governance maximums (8/hr, 30/day): a recipient receives the most immediate pushes the cap allows.
    /// </summary>
    public static NotificationThrottleCeilings SafeDefaults { get; } = new(HourlyMaximum, DailyMaximum);

    /// <summary>Gets a value indicating whether both declared ceilings are within their non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => HourlyCeiling >= Minimum && HourlyCeiling <= HourlyMaximum &&
            DailyCeiling >= Minimum && DailyCeiling <= DailyMaximum;
}
