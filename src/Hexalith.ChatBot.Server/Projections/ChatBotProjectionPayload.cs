using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Adapts concrete EventStore event payloads for both projection delivery paths.</summary>
internal static class ChatBotProjectionPayload
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Maps concrete task-intent events into the task adapter.</summary>
    public static PublishedTaskIntentEvent? TryCreateFlatTaskIntentEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        if (DeserializeWhen<TaskIntentConvertedToAiActionProposal>(projectionEvent) is { } converted)
        {
            return NewTaskIntentEnvelope(request, projectionEvent, record: converted.TaskIntent, proposal: converted.Proposal);
        }

        TaskIntentRecord? record = DeserializeWhen<TaskIntentCaptured>(projectionEvent)?.Record
            ?? DeserializeWhen<TaskIntentDispositionMarked>(projectionEvent)?.TaskIntent;
        return record is null ? null : NewTaskIntentEnvelope(request, projectionEvent, record: record);
    }

    /// <summary>Maps a concrete approval request or decision into the approval adapter.</summary>
    public static PublishedAiActionApprovalEvent? TryCreateFlatApprovalEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        AiActionApprovalRequested? approvalRequest = string.Equals(
            projectionEvent.EventTypeName,
            typeof(AiActionApprovalRequested).FullName,
            StringComparison.Ordinal)
                ? DeserializePayload<AiActionApprovalRequested>(projectionEvent)
                : null;
        AiActionApprovalDecisionRecorded? decision = string.Equals(
            projectionEvent.EventTypeName,
            typeof(AiActionApprovalDecisionRecorded).FullName,
            StringComparison.Ordinal)
                ? DeserializePayload<AiActionApprovalDecisionRecorded>(projectionEvent)
                : null;
        if (approvalRequest is null && decision is null)
        {
            return null;
        }

        return new PublishedAiActionApprovalEvent(
            request.TenantId,
            request.Domain,
            request.AggregateId,
            projectionEvent.EventTypeName,
            projectionEvent.SequenceNumber,
            projectionEvent.Timestamp,
            projectionEvent.CorrelationId,
            approvalRequest,
            decision);
    }

    /// <summary>Maps a concrete AI execution event into its corresponding adapter member.</summary>
    public static PublishedAiActionExecutionEvent? TryCreateFlatAiActionExecutionEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        ApprovedAiActionExecutionStarted? started = DeserializeWhen<ApprovedAiActionExecutionStarted>(projectionEvent);
        ApprovedAiActionExecutionSucceeded? succeeded = DeserializeWhen<ApprovedAiActionExecutionSucceeded>(projectionEvent);
        ApprovedAiActionExecutionFailed? failed = DeserializeWhen<ApprovedAiActionExecutionFailed>(projectionEvent);
        ApprovedAiActionExecutionRejected? rejected = DeserializeWhen<ApprovedAiActionExecutionRejected>(projectionEvent);
        AiActionProposalInvalidatedByCorrection? invalidated = DeserializeWhen<AiActionProposalInvalidatedByCorrection>(projectionEvent);
        LowRiskAiAssistanceExecutionStarted? lowRiskStarted = DeserializeWhen<LowRiskAiAssistanceExecutionStarted>(projectionEvent);
        LowRiskAiAssistanceExecutionSucceeded? lowRiskSucceeded = DeserializeWhen<LowRiskAiAssistanceExecutionSucceeded>(projectionEvent);
        LowRiskAiAssistanceExecutionFailed? lowRiskFailed = DeserializeWhen<LowRiskAiAssistanceExecutionFailed>(projectionEvent);
        LowRiskAiAssistanceRoutedToApproval? lowRiskRoutedToApproval = DeserializeWhen<LowRiskAiAssistanceRoutedToApproval>(projectionEvent);
        if (started is null && succeeded is null && failed is null && rejected is null && invalidated is null &&
            lowRiskStarted is null && lowRiskSucceeded is null && lowRiskFailed is null && lowRiskRoutedToApproval is null)
        {
            return null;
        }

        return new PublishedAiActionExecutionEvent(
            request.TenantId,
            request.Domain,
            request.AggregateId,
            projectionEvent.EventTypeName,
            projectionEvent.SequenceNumber,
            projectionEvent.Timestamp,
            projectionEvent.CorrelationId,
            started,
            succeeded,
            failed,
            rejected,
            invalidated,
            lowRiskStarted,
            lowRiskSucceeded,
            lowRiskFailed,
            lowRiskRoutedToApproval);
    }

    /// <summary>Maps a concrete conversation message or cancellation event into the conversation adapter.</summary>
    public static PublishedTaskIntentEvent? TryCreateFlatConversationEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        // Deserialize the flat conversation event into its concrete type and place it in the correct nested
        // PublishedTaskIntentEvent slot so the projection actually fires in production.
        if (string.Equals(projectionEvent.EventTypeName, typeof(ProjectConversationMessageAppended).FullName, StringComparison.Ordinal))
        {
            ProjectConversationMessageAppended? userMessage = DeserializePayload<ProjectConversationMessageAppended>(projectionEvent);
            return userMessage is null || userMessage.TenantId != request.TenantId
                ? null
                : NewTaskIntentEnvelope(
                    request,
                    projectionEvent,
                    userMessage: userMessage with { SourceVersion = projectionEvent.SequenceNumber });
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationRequested).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationRequested? cancellation = DeserializePayload<AiResponseGenerationCancellationRequested>(projectionEvent);
            return cancellation is null || cancellation.TenantId != request.TenantId ? null : NewTaskIntentEnvelope(request, projectionEvent, cancellation: cancellation);
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationConfirmed).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationConfirmed? confirmation = DeserializePayload<AiResponseGenerationCancellationConfirmed>(projectionEvent);
            return confirmation is null || confirmation.TenantId != request.TenantId ? null : NewTaskIntentEnvelope(request, projectionEvent, confirmation: confirmation);
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationFailed).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationFailed? failure = DeserializePayload<AiResponseGenerationCancellationFailed>(projectionEvent);
            return failure is null || failure.TenantId != request.TenantId ? null : NewTaskIntentEnvelope(request, projectionEvent, cancellationFailure: failure);
        }

        return null;
    }

    private static PublishedTaskIntentEvent NewTaskIntentEnvelope(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent,
        TaskIntentRecord? record = null,
        AiActionProposalRecord? proposal = null,
        ProjectConversationMessageAppended? userMessage = null,
        AiResponseGenerationCancellationRequested? cancellation = null,
        AiResponseGenerationCancellationConfirmed? confirmation = null,
        AiResponseGenerationCancellationFailed? cancellationFailure = null)
        => new(
            request.TenantId,
            request.Domain,
            request.AggregateId,
            projectionEvent.EventTypeName,
            projectionEvent.SequenceNumber,
            projectionEvent.Timestamp,
            projectionEvent.CorrelationId,
            Record: record is null ? null : record with { CorrelationId = projectionEvent.CorrelationId },
            Proposal: proposal is null ? null : proposal with { CorrelationId = projectionEvent.CorrelationId },
            UserMessage: userMessage is null ? null : userMessage with { CorrelationId = projectionEvent.CorrelationId },
            AiResponseCancellation: cancellation is null ? null : cancellation with { CorrelationId = projectionEvent.CorrelationId },
            AiResponseCancellationConfirmed: confirmation is null ? null : confirmation with { CorrelationId = projectionEvent.CorrelationId },
            AiResponseCancellationFailed: cancellationFailure is null ? null : cancellationFailure with { CorrelationId = projectionEvent.CorrelationId });

    /// <summary>Reads the concrete event payload using the shared projection JSON options.</summary>
    public static T? DeserializePayload<T>(ProjectionEventDto projectionEvent)
        => projectionEvent.Payload.Length == 0
            ? default
            : JsonSerializer.Deserialize<T>(projectionEvent.Payload, JsonOptions);

    /// <summary>Reads the payload when the event type matches the requested concrete type.</summary>
    public static T? DeserializeWhen<T>(ProjectionEventDto projectionEvent)
        => string.Equals(projectionEvent.EventTypeName, typeof(T).FullName, StringComparison.Ordinal)
            ? DeserializePayload<T>(projectionEvent)
            : default;

    /// <summary>Binds payload fields to a projection adapter using the EventStore metadata.</summary>
    public static TPublished? TryCreatePublishedEvent<TPublished>(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        if (projectionEvent.Payload.Length == 0)
        {
            return default;
        }

        using JsonDocument payload = JsonDocument.Parse(projectionEvent.Payload);
        if (payload.RootElement.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("tenantId", request.TenantId);
            writer.WriteString("domain", request.Domain);
            writer.WriteString("aggregateId", request.AggregateId);
            writer.WriteString("eventTypeName", projectionEvent.EventTypeName);
            writer.WriteNumber("sequenceNumber", projectionEvent.SequenceNumber);
            writer.WriteString("correlationId", projectionEvent.CorrelationId);
            writer.WriteString("messageId", projectionEvent.MessageId);
            writer.WriteString("timestamp", projectionEvent.Timestamp);
            foreach (JsonProperty property in payload.RootElement.EnumerateObject())
            {
                if (IsTrustedMetadataProperty(property.Name))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return JsonSerializer.Deserialize<TPublished>(stream.ToArray(), JsonOptions);
    }

    private static bool IsTrustedMetadataProperty(string name)
        => TrustedMetadataNames.Contains(name);

    private static readonly HashSet<string> TrustedMetadataNames = new(StringComparer.OrdinalIgnoreCase)
        { "tenantId", "domain", "aggregateId", "eventTypeName", "sequenceNumber", "correlationId", "messageId", "timestamp" };
}
