using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Matched source proof and committed retry intent for the owning workflow.</summary>
internal sealed record WorkflowReviewRetry(ReviewQueueTransition Source, ReviewQueueRetryRequest Retry)
{
    public static bool CanRejoin(ReviewQueueTransition source, ReviewQueueRetryRequest retry)
        => source.RetryTarget is not null && source.Generation > 0 && source.ProducerFamily switch
        {
            Contracts.Enums.OperationalQueueFamily.FailedIngestion => retry.OperationClass is "message-intake" or "ingestion" or "failed-ingestion",
            Contracts.Enums.OperationalQueueFamily.FailedAttachment => retry.OperationClass is "attachment" or "attachment-capture" or "failed-attachment",
            _ => false,
        };

    public string InstanceId => "review-retry-" + ProjectConversationStableId.Hash(System.Text.Json.JsonSerializer.Serialize(
        new object[] { Source.TenantId, Source.StreamId, Source.Version })).ToLowerInvariant();
}
