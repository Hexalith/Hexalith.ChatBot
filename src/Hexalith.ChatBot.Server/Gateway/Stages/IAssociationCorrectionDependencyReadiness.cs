namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IAssociationCorrectionDependencyReadiness
{
    bool IsProjectionInvalidationReady { get; }
}
