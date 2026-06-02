namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded reviewer-backlog alert threshold (Story 7.10, NFR46/FR75d). Exactly one declared
/// dimension — <see cref="OpenItemThreshold"/>, a bounded non-negative integer — the count of a reviewer's open approval
/// items above which a backlog alert fires for that <c>(tenant × reviewer)</c> pair. This is deliberately NOT a free-form
/// map or expression: a tenant cannot introduce a new dimension or a custom formula, and it can only ever <em>lower</em>
/// the threshold (to be alerted sooner) — the inclusive maximum (<see cref="Maximum"/> = 25) is the hard NFR46
/// governance cap a tenant can never raise above (which would suppress alerts and hide a bottleneck). Out-of-range
/// (including above the maximum), wrong-type, NaN/Infinity, or undeclared values are rejected by the Tenant Policy Schema
/// with the existing safe reason codes and the evaluator falls back to <see cref="SafeDefault"/> (= 25, the NFR46
/// maximum). This is a Standard triage-tuning knob — not security-sensitive — so no blanket two-person rule applies.
/// </summary>
/// <param name="OpenItemThreshold">The count of open approval items a reviewer may hold before a backlog alert fires (alert fires strictly above this value).</param>
public sealed record ReviewerBacklogThreshold(int OpenItemThreshold)
{
    /// <summary>The inclusive lower bound for the threshold (non-negative; a tenant may lower to 0 to alert on any open item).</summary>
    public const int Minimum = 0;

    /// <summary>The inclusive upper bound for the threshold — the hard NFR46 governance cap (alert at &gt; 25 open items).</summary>
    public const int Maximum = 25;

    /// <summary>
    /// The declared safe default applied when the tenant has not set the knob or set an invalid value. It equals the
    /// NFR46 governance maximum (25): a reviewer must exceed 25 open items before the tenant admin is alerted.
    /// </summary>
    public static ReviewerBacklogThreshold SafeDefault { get; } = new(Maximum);

    /// <summary>Gets a value indicating whether the declared threshold is within its non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => OpenItemThreshold >= Minimum && OpenItemThreshold <= Maximum;
}
