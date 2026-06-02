namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded weight set for approval-queue prioritization (Story 7.8, NFR46/FR75d). Exactly three
/// declared dimensions — risk-class, affected-party authority, and time-in-queue — each a bounded non-negative weight
/// within <see cref="MinimumWeight"/>/<see cref="MaximumWeight"/>. This is deliberately NOT a free-form map or
/// expression: tenants cannot introduce new weight dimensions or a custom formula; the only configurable surface is the
/// relative contribution of the three declared dimensions. Out-of-range, NaN, or Infinity weights are rejected by the
/// Tenant Policy Schema with a safe reason code and the evaluator falls back to <see cref="SafeDefaults"/>.
/// </summary>
/// <param name="RiskWeight">Relative contribution of the risk-class rank.</param>
/// <param name="AuthorityWeight">Relative contribution of the affected-party authority rank.</param>
/// <param name="TimeInQueueWeight">Relative contribution of server-measured time-in-queue.</param>
public sealed record ApprovalPriorityWeights(
    double RiskWeight,
    double AuthorityWeight,
    double TimeInQueueWeight)
{
    /// <summary>The inclusive lower bound for every declared weight (non-negative).</summary>
    public const double MinimumWeight = 0.0;

    /// <summary>The inclusive upper bound for every declared weight.</summary>
    public const double MaximumWeight = 100.0;

    /// <summary>
    /// The declared safe defaults applied when the tenant has not set the knob or set an invalid value. Equal unit
    /// weights reproduce the epic's deterministic intent: highest-authority × highest-risk × oldest first.
    /// </summary>
    public static ApprovalPriorityWeights SafeDefaults { get; } = new(1.0, 1.0, 1.0);

    /// <summary>Gets a value indicating whether every declared weight is a finite, in-range, non-negative number.</summary>
    public bool IsWithinBounds
        => IsBounded(RiskWeight) && IsBounded(AuthorityWeight) && IsBounded(TimeInQueueWeight);

    private static bool IsBounded(double weight)
        => !double.IsNaN(weight) && !double.IsInfinity(weight) && weight >= MinimumWeight && weight <= MaximumWeight;
}
