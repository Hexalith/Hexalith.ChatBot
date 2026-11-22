using System.Text.Json;
using Dapr.Workflow;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Rejoins only the exact workflow or attachment that supplied the failure proof.</summary>
internal sealed class RejoinReviewOperationActivity(IIngestionBindingCoordinator ingestion, IAttachmentCaptureCoordinator attachments, IAuthoritativeEventStreamReader reader) : WorkflowActivity<WorkflowReviewRetry, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, WorkflowReviewRetry input)
    {
        var source = input.Source;
        var retry = input.Retry;
        var token = CancellationToken.None;
        if (source.RetryTarget is not { } target || source.Generation is not { } generation || source.State != "failed" ||
            !WorkflowReviewRetry.CanRejoin(source, retry)) { return false; }
        var identity = new AggregateIdentity(source.TenantId, ChatBotEventStore.DomainName, source.StreamId);
        var observation = await reader.ReadAsync(identity, token).ConfigureAwait(false);
        if (!observation.IsAuthoritative || observation.Stream is not { } stream || stream.Identity != identity)
        { throw new InvalidOperationException("review_retry_owner_unavailable"); }
        var original = stream.Events.FirstOrDefault(e => e.SequenceNumber == source.Version && e.MessageId == retry.EventId);
        var latest = stream.Events.LastOrDefault();
        if (original is null || latest is null || latest.EventTypeName.Split(',', 2)[0] != typeof(WorkflowReviewTransitionRecorded).FullName || original.EventTypeName.Split(',', 2)[0] != typeof(WorkflowReviewTransitionRecorded).FullName)
        { return false; }
        var failed = JsonSerializer.Deserialize<WorkflowReviewTransitionRecorded>(original.Payload, JsonSerializerOptions.Web)?.Transition;
        if (failed is null || failed.StreamId(source.TenantId) != source.StreamId || failed.State != "failed" ||
            failed.Family != source.ProducerFamily || !target.Matches(failed.Target) || failed.Generation != generation || failed.Attempt != source.RetryCount)
        { return false; }
        if (latest.SequenceNumber != source.Version)
        {
            // A crash may follow the retry-start commit and precede the external call. Resume that same attempt only.
            var inFlight = JsonSerializer.Deserialize<WorkflowReviewTransitionRecorded>(latest.Payload, JsonSerializerOptions.Web)?.Transition;
            if (latest.CorrelationId != retry.Correlation || inFlight?.State != "retrying" || inFlight.Attempt != source.RetryCount + 1 ||
                inFlight.Generation != generation || !target.Matches(inFlight.Target)) { return false; }
        }
        string generationKey = input.InstanceId;
        if (source.ProducerFamily == OperationalQueueFamily.FailedIngestion)
        {
            await ingestion.StartAsync(new(source.TenantId, target.AssociationId, target.IntakeId, source.IdentityProject!, generation,
                retry.Correlation, "chatbot-ingestion-retry-" + generationKey, target.Phase, source.RetryCount + 1, generationKey, target.IndexedUnits), token)
                .ConfigureAwait(false);
        }
        else
        {
            var result = await attachments.CaptureAsync(new(source.TenantId, target.IntakeId, generation, retry.Correlation,
                source.IdentityProject, target.AssociationId, target.Phase, source.RetryCount + 1, retry.RetryId), token).ConfigureAwait(false);
            return result.EvaluatedCount == 1 && result.StoredCount == 1 && result.ConfirmedSubjectId == target.Phase && result.ConfirmedGeneration == generation;
        }
        return true;
    }
}
