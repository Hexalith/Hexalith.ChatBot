using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Association.Scoring;

internal static class AssociationThresholdPolicyValidator
{
    public const double MinimumM0High = 0.80;
    public const double MinimumM0Low = 0.50;

    public static bool IsValid(AssociationThresholdPolicySnapshot? policy)
        => policy is not null &&
            IsValid(policy.THigh, policy.TLow, policy.EvaluationRunReference);

    public static bool IsValid(double high, double low, string? evaluationRunReference)
    {
        if (!double.IsFinite(high) ||
            !double.IsFinite(low) ||
            low < 0.0 ||
            high > 1.0 ||
            low >= high)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(evaluationRunReference) ||
            (high >= MinimumM0High && low >= MinimumM0Low);
    }
}
