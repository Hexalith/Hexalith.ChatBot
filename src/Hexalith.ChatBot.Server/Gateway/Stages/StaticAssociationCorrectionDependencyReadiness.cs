namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class StaticAssociationCorrectionDependencyReadiness(
    AssociationCorrectionDependencyReadinessStatus status) : IAssociationCorrectionDependencyReadiness
{
    public AssociationCorrectionDependencyReadinessStatus Status { get; } = status;

    public bool IsProjectionInvalidationReady => Status.IsReady;
}
