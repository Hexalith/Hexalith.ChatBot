namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Immutable threshold policy values used by one scorer execution.
/// </summary>
public sealed record AssociationThresholdPolicySnapshot(
    double THigh,
    double TLow,
    string PolicyVersion,
    string? EvaluationRunReference)
{
    public const double DefaultM0High = 0.90;
    public const double DefaultM0Low = 0.60;

    public static AssociationThresholdPolicySnapshot DefaultM0 { get; } = new(
        DefaultM0High,
        DefaultM0Low,
        "association-thresholds.m0.default.v1",
        null);
}
