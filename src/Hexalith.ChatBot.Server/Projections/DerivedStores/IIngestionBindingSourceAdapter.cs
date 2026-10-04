using Hexalith.ChatBot.Server.Lifecycle.Workflows;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>Invokes the accepted source-owner ingestion boundary without exposing its transport to workflow runtime.</summary>
internal interface IIngestionBindingSourceAdapter
{
    /// <summary>Starts or rejoins deterministic owner ingestion for one authorized source.</summary>
    Task<string> StartAsync(IngestionBindingSourceRequest input, string sourceUri, byte[] content, string contentType, string identity, CancellationToken cancellationToken);

    /// <summary>Reads and validates the owner status against the exact tenant, case, and instance identity.</summary>
    Task<IngestionBindingSourceStatus> GetStatusAsync(IngestionBindingSourceOperation input, CancellationToken cancellationToken);

    /// <summary>Finalizes the complete ordered binding through the owner API.</summary>
    Task<bool> FinalizeAsync(IngestionBindingFinalizeInput input, CancellationToken cancellationToken);
}
