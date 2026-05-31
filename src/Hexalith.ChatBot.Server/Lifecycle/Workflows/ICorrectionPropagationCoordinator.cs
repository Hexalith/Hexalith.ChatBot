namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationCoordinator
{
    bool IsReady { get; }

    ValueTask StartAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken);
}
