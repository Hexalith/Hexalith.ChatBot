namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IAssociationCorrectionDependencyReadiness
{
    AssociationCorrectionDependencyReadinessStatus Status { get; }

    bool IsProjectionInvalidationReady { get; }
}

internal sealed record AssociationCorrectionDependencyReadinessStatus(
    bool IsWorkflowRuntimeReady,
    bool IsProjectionInvalidationReady,
    bool IsAuditWriterReady,
    bool IsIdempotencyStoreReady)
{
    public bool IsReady => IsWorkflowRuntimeReady && IsProjectionInvalidationReady && IsAuditWriterReady && IsIdempotencyStoreReady;

    public static AssociationCorrectionDependencyReadinessStatus Ready { get; } = new(true, true, true, true);
}
