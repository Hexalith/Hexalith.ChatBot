using Dapr.Workflow;

using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1.DerivedStores;

namespace Hexalith.ChatBot.Server.Adapters.Memories;

/// <summary>Publishes one complete ordered association/intake binding atomically in Memories.</summary>
internal sealed class IngestionBindingFinalizeActivity(MemoriesClient memories)
    : WorkflowActivity<IngestionBindingFinalizeInput, bool>
{
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        IngestionBindingFinalizeInput input)
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
                CancellationToken.None)
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
