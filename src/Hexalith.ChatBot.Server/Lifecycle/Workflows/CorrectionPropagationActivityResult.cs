namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationActivityResult(
    string StoreKey,
    string Outcome,
    string? FailureReasonCode,
    DateTimeOffset CompletedAtUtc,
    string? RemoteOperationId = null)
{
    public bool IsSuccessful => string.Equals(Outcome, "success", StringComparison.Ordinal);

    public bool IsPending => string.Equals(Outcome, "awaiting-completion", StringComparison.Ordinal);
}
