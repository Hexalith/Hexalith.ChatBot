namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Coordinates deterministic start-or-rejoin scheduling for ingestion binding.</summary>
internal sealed class DaprIngestionBindingCoordinator(IIngestionBindingWorkflowRuntime runtime)
    : IIngestionBindingCoordinator
{
    public bool IsReady => runtime.IsAvailable;

    public ValueTask StartAsync(IngestionBindingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return runtime.ScheduleAsync(request, cancellationToken);
    }

    public static string WorkflowInstanceIdFor(
        string tenantId,
        string associationId,
        string intakeId,
        long sourceVersion)
        => $"{tenantId}:chatbot:ingestion-binding:{associationId}:{intakeId}:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
