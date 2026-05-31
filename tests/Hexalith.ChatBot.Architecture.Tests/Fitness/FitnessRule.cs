using NetArchTestResult = NetArchTest.Rules.TestResult;

namespace Hexalith.ChatBot.Architecture.Tests.Fitness;

/// <summary>
/// Builds the assertion message that makes a fitness-rule failure name the forbidden edge (FR86 / AC5):
/// a bare <c>IsSuccessful</c> assertion is insufficient — the failure must identify the offending type(s).
/// The eNhanced NetArchTest fork exposes the offenders via <c>TestResult.FailingTypes</c> (each carries a
/// <c>FullName</c>); the original fork's <c>FailingTypeNames</c> does not exist here.
/// </summary>
internal static class FitnessRule
{
    /// <summary>
    /// Describes a NetArchTest result, naming the offending type(s) when the rule fails.
    /// </summary>
    /// <param name="result">The NetArchTest result to describe.</param>
    /// <returns>A metadata-only message (type/namespace identifiers only — never file contents).</returns>
    internal static string Describe(NetArchTestResult result)
    {
        if (result.IsSuccessful)
        {
            return "rule satisfied";
        }

        string offenders = string.Join(", ", FailingTypeNames(result));
        return offenders.Length == 0
            ? "rule failed but surfaced no failing types (likely a misconfigured/typo'd namespace)"
            : "forbidden edge — offending type(s): " + offenders;
    }

    /// <summary>
    /// Projects the offending types of a failed result to their full names, tolerating the fork's exact
    /// collection/nullability shape for <c>FailingTypes</c>.
    /// </summary>
    /// <param name="result">The NetArchTest result.</param>
    /// <returns>The full names of the failing types.</returns>
    internal static IEnumerable<string?> FailingTypeNames(NetArchTestResult result)
    {
        var failing = result.FailingTypes;
        if (failing is null)
        {
            yield break;
        }

        foreach (var type in failing)
        {
            yield return type.FullName;
        }
    }
}
