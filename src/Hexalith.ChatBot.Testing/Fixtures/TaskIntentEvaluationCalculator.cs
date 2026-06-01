namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Deterministic precision/recall calculator for scaffold task-intent labels.
/// </summary>
public static class TaskIntentEvaluationCalculator
{
    public const string PositiveLabel = "task-intent";
    public const string NegativeLabel = "not-task-intent";

    public static TaskIntentEvaluationReport Calculate(TenantScopedEvaluationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        TenantScopedFixtureCase[] cases = dataset.Cases
            .Where(static fixtureCase =>
                !string.IsNullOrWhiteSpace(fixtureCase.TaskIntentExpectedLabel) &&
                !string.IsNullOrWhiteSpace(fixtureCase.TaskIntentPredictedLabel))
            .ToArray();
        int truePositive = cases.Count(static fixtureCase =>
            string.Equals(fixtureCase.TaskIntentExpectedLabel, PositiveLabel, StringComparison.Ordinal) &&
            string.Equals(fixtureCase.TaskIntentPredictedLabel, PositiveLabel, StringComparison.Ordinal));
        int falsePositive = cases.Count(static fixtureCase =>
            string.Equals(fixtureCase.TaskIntentExpectedLabel, NegativeLabel, StringComparison.Ordinal) &&
            string.Equals(fixtureCase.TaskIntentPredictedLabel, PositiveLabel, StringComparison.Ordinal));
        int falseNegative = cases.Count(static fixtureCase =>
            string.Equals(fixtureCase.TaskIntentExpectedLabel, PositiveLabel, StringComparison.Ordinal) &&
            string.Equals(fixtureCase.TaskIntentPredictedLabel, NegativeLabel, StringComparison.Ordinal));

        return new TaskIntentEvaluationReport(
            truePositive,
            falsePositive,
            falseNegative,
            Divide(truePositive, truePositive + falsePositive),
            Divide(truePositive, truePositive + falseNegative),
            dataset.IsScaffold,
            TaskIntentEvaluationReport.RequiredM0PrecisionTarget,
            TaskIntentEvaluationReport.RequiredM0RecallTarget,
            TaskIntentEvaluationReport.DocumentedM1PrecisionRatchet,
            TaskIntentEvaluationReport.DocumentedM1RecallRatchet);
    }

    private static double Divide(int numerator, int denominator)
        => denominator == 0 ? 0 : (double)numerator / denominator;
}
