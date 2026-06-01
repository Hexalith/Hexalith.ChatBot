namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Precision/recall report for task-intent labels in the A9a scaffold.
/// </summary>
public sealed record TaskIntentEvaluationReport(
    int TruePositiveCount,
    int FalsePositiveCount,
    int FalseNegativeCount,
    double Precision,
    double Recall,
    bool IsScaffold,
    double M0PrecisionTarget,
    double M0RecallTarget,
    double M1PrecisionRatchet,
    double M1RecallRatchet)
{
    public const double RequiredM0PrecisionTarget = 0.80;
    public const double RequiredM0RecallTarget = 0.75;
    public const double DocumentedM1PrecisionRatchet = 0.90;
    public const double DocumentedM1RecallRatchet = 0.85;

    public bool MeetsM0Targets => Precision >= M0PrecisionTarget && Recall >= M0RecallTarget;
}
