using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>Maps accepted Memories owner APIs to the ChatBot ingestion boundary; it never writes owner stores.</summary>
internal sealed class MemoriesIngestionBindingSourceAdapter(MemoriesClient memories) : IIngestionBindingSourceAdapter
{
    /// <inheritdoc />
    public Task<string> StartAsync(IngestionBindingSourceRequest input, string sourceUri, byte[] content, string contentType, string identity, CancellationToken cancellationToken)
        => memories.IngestAsync(input.Request.TenantId, input.Context.PriorCaseId, sourceUri, content, contentType,
            "hexalith-chatbot", metadata: null, idempotencyToken: identity, cancellationToken);

    /// <inheritdoc />
    public async Task<IngestionBindingSourceStatus> GetStatusAsync(IngestionBindingSourceOperation input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        IngestionWorkflowStatus status = await memories
            .GetIngestionWorkflowStatusAsync(input.InstanceId, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(status.InstanceId, input.InstanceId, StringComparison.Ordinal)
            || !string.Equals(status.TenantId, input.Source.Request.TenantId, StringComparison.Ordinal)
            || !string.Equals(status.CaseId, input.Source.Context.PriorCaseId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("ingestion_binding_status_identity_mismatch");
        }

        return new IngestionBindingSourceStatus(
            status.RuntimeStatus,
            status.MemoryUnitId,
            status.MemoryUnitStatus is MemoryUnitStatus.Indexed);
    }

    /// <inheritdoc />
    public async Task<bool> FinalizeAsync(IngestionBindingFinalizeInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        DerivedStoreBinding binding = await memories
            .FinalizeDerivedStoreBindingAsync(
                input.Request.TenantId,
                new FinalizeDerivedStoreBindingRequest(
                    input.Request.AssociationId,
                    input.Request.IntakeId,
                    input.Request.SourceVersion,
                    input.Context.PriorCaseId,
                    input.Context.Source.Attachments.Count,
                    [.. input.CompletedSources.Select(static source => new DerivedStoreBindingEntry(
                        ToMemoriesKind(source.RecordKind),
                        source.Ordinal,
                        source.MemoryUnitId))]),
                cancellationToken)
            .ConfigureAwait(false);
        return string.Equals(binding.AssociationId, input.Request.AssociationId, StringComparison.Ordinal)
            && string.Equals(binding.IntakeId, input.Request.IntakeId, StringComparison.Ordinal)
            && string.Equals(binding.PriorCaseId, input.Context.PriorCaseId, StringComparison.Ordinal)
            && binding.SourceVersion == input.Request.SourceVersion;
    }

    private static DerivedStoreRecordKind ToMemoriesKind(IngestionBindingRecordKind kind)
        => kind switch
        {
            IngestionBindingRecordKind.Message => DerivedStoreRecordKind.Message,
            IngestionBindingRecordKind.Attachment => DerivedStoreRecordKind.Attachment,
            _ => throw new InvalidOperationException("ingestion_binding_record_kind_invalid"),
        };
}
