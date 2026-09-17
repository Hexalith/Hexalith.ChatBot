namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IAssociationCorrectionDependencyReadiness
{
    AssociationCorrectionDependencyReadinessStatus Status { get; }

    bool IsProjectionInvalidationReady { get; }
}
