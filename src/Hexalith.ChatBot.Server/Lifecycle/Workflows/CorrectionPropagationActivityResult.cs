namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationActivityResult(
    string StoreKey,
    string Outcome,
    string? FailureReasonCode,
    DateTimeOffset CompletedAtUtc)
{
    public bool IsSuccessful => string.Equals(Outcome, "success", StringComparison.Ordinal);
}
