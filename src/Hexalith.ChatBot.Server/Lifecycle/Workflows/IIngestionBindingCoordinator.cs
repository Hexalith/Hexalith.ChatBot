namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Starts or rejoins durable ingestion binding after an accepted association.</summary>
internal interface IIngestionBindingCoordinator
{
    bool IsReady { get; }

    ValueTask StartAsync(IngestionBindingRequest request, CancellationToken cancellationToken);
}
