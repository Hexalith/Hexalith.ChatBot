using Hexalith.ChatBot.Server.Lifecycle.Workflows;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Observes the existing workflow-start boundary before SDK confirmation persistence.</summary>
internal sealed class SdkConfirmationWorkflowCoordinator(Action onStart) : ICorrectionPropagationCoordinator
{
    /// <summary>Gets whether the controlled workflow runtime is ready.</summary>
    public bool IsReady => true;

    /// <summary>Gets the number of workflow starts observed.</summary>
    public int Starts { get; private set; }

    /// <summary>Records workflow startup and invokes the boundary observer.</summary>
    public ValueTask StartAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
    {
        Starts++;
        onStart();
        return ValueTask.CompletedTask;
    }
}
