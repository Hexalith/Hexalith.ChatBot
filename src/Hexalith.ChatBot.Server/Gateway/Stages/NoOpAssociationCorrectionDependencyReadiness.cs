namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class NoOpAssociationCorrectionDependencyReadiness : IAssociationCorrectionDependencyReadiness
{
    public bool IsProjectionInvalidationReady => true;
}
