namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectedContextReadinessPolicy
{
    ValueTask<CorrectedContextReadiness> EvaluateAsync(
        string tenantId,
        string associationId,
        long sourceVersion,
        CancellationToken cancellationToken);
}
