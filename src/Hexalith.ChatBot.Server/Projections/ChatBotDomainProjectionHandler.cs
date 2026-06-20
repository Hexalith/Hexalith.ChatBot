using System.Text.Json;

using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ChatBotDomainProjectionHandler(
    GovernedOperationProjectionHandler governedOperationHandler,
    GovernedControlStateProjectionHandler controlStateHandler,
    AssociationProjectionHandler associationHandler,
    ParticipantResolutionProjectionHandler participantResolutionHandler,
    AiOutcomeProjectionHandler aiOutcomeHandler,
    TaskIntentProjectionHandler taskIntentHandler,
    ApprovalProjectionHandler approvalHandler) : IDomainProjectionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => ChatBotEventStore.DomainName;

    public ProjectionResponse Project(ProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ProjectionDispatchSummary summary = ProjectAsync(request, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return new ProjectionResponse(ChatBotEventStore.DomainName, JsonSerializer.SerializeToElement(summary, JsonOptions));
    }

    private async Task<ProjectionDispatchSummary> ProjectAsync(ProjectionRequest request, CancellationToken cancellationToken)
    {
        int applied = 0;
        int ignored = 0;

        foreach (ProjectionEventDto projectionEvent in request.Events ?? [])
        {
            if (await ProjectEventAsync(request, projectionEvent, cancellationToken).ConfigureAwait(false))
            {
                applied++;
            }
            else
            {
                ignored++;
            }
        }

        return new ProjectionDispatchSummary(request.TenantId, request.Domain, request.AggregateId, applied, ignored);
    }

    private async Task<bool> ProjectEventAsync(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent,
        CancellationToken cancellationToken)
    {
        PublishedGovernedOperationEvent governedEvent = new(
            request.TenantId,
            request.Domain,
            request.AggregateId,
            projectionEvent.EventTypeName,
            projectionEvent.SequenceNumber,
            projectionEvent.CorrelationId,
            projectionEvent.MessageId,
            projectionEvent.Timestamp,
            projectionEvent.Payload);

        GovernedNoteRecordedNotification? governedNotification = GovernedOperationProjectionTranslator.TryCreateNotification(governedEvent);
        if (governedNotification is not null)
        {
            _ = await governedOperationHandler.HandleAsync(governedNotification, cancellationToken).ConfigureAwait(false);
            return true;
        }

        GovernedControlStateProjectionNotification? controlNotification = GovernedControlStateProjectionTranslator.TryCreateNotification(governedEvent);
        if (controlNotification is not null)
        {
            _ = await controlStateHandler.HandleAsync(controlNotification, cancellationToken).ConfigureAwait(false);
            return true;
        }

        // Flat conversation events (ProjectConversationMessageAppended, AiResponseGenerationCancellationRequested)
        // must be matched by event type and routed BEFORE the generic TryCreatePublishedEvent chain: their flat
        // payload cannot populate the nested PublishedTaskIntentEvent slots, and their top-level metadata (e.g.
        // schemaVersion string) collides with other published-event shapes, which makes the generic deserialization
        // throw instead of falling through.
        if (TryCreateFlatConversationEvent(request, projectionEvent) is { } flatConversationEvent)
        {
            return await taskIntentHandler.HandleAsync(flatConversationEvent, cancellationToken).ConfigureAwait(false)
                is TaskIntentProjectionHandler.ProjectionOutcome.Applied;
        }

        if (TryCreatePublishedEvent<PublishedMailboxIntakeEvent>(request, projectionEvent) is { } mailboxEvent &&
            MailboxIntakeProjectionTranslator.TryCreateNotification(mailboxEvent) is { } mailboxNotification)
        {
            _ = await associationHandler.HandleAsync(
                    mailboxNotification.Captured,
                    mailboxNotification.TenantId,
                    mailboxNotification.SourceVersion,
                    mailboxNotification.CorrelationId,
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        if (TryCreatePublishedEvent<PublishedAssociationEvent>(request, projectionEvent) is { } associationEvent &&
            AssociationProjectionTranslator.TryCreateNotification(associationEvent) is { } associationNotification)
        {
            _ = await associationHandler.HandleAsync(associationNotification, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (TryCreatePublishedEvent<PublishedParticipantResolutionEvent>(request, projectionEvent) is { } participantEvent &&
            ParticipantResolutionProjectionTranslator.TryCreateNotification(participantEvent) is { } participantNotification)
        {
            _ = await participantResolutionHandler.HandleAsync(participantNotification, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (TryCreatePublishedEvent<PublishedTaskIntentEvent>(request, projectionEvent) is { } taskIntentEvent &&
            await taskIntentHandler.HandleAsync(taskIntentEvent, cancellationToken).ConfigureAwait(false) is TaskIntentProjectionHandler.ProjectionOutcome.Applied)
        {
            return true;
        }

        if (TryCreatePublishedEvent<PublishedAiActionApprovalEvent>(request, projectionEvent) is { } approvalEvent &&
            await approvalHandler.HandleAsync(approvalEvent, cancellationToken).ConfigureAwait(false) is ApprovalProjectionHandler.ProjectionOutcome.Applied)
        {
            return true;
        }

        if (TryCreatePublishedEvent<PublishedAiActionExecutionEvent>(request, projectionEvent) is { } aiExecutionEvent &&
            await aiOutcomeHandler.HandleAsync(aiExecutionEvent, cancellationToken).ConfigureAwait(false) is AiOutcomeProjectionHandler.ProjectionOutcome.Applied)
        {
            return true;
        }

        return false;
    }

    private static PublishedTaskIntentEvent? TryCreateFlatConversationEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        // Deserialize the flat conversation event into its concrete type and place it in the correct nested
        // PublishedTaskIntentEvent slot so the projection actually fires in production.
        if (string.Equals(projectionEvent.EventTypeName, typeof(ProjectConversationMessageAppended).FullName, StringComparison.Ordinal))
        {
            ProjectConversationMessageAppended? userMessage = DeserializePayload<ProjectConversationMessageAppended>(projectionEvent);
            return userMessage is null ? null : NewTaskIntentEnvelope(request, projectionEvent, userMessage: userMessage);
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationRequested).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationRequested? cancellation = DeserializePayload<AiResponseGenerationCancellationRequested>(projectionEvent);
            return cancellation is null ? null : NewTaskIntentEnvelope(request, projectionEvent, cancellation: cancellation);
        }

        return null;
    }

    private static PublishedTaskIntentEvent NewTaskIntentEnvelope(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent,
        ProjectConversationMessageAppended? userMessage = null,
        AiResponseGenerationCancellationRequested? cancellation = null)
        => new(
            request.TenantId,
            request.Domain,
            request.AggregateId,
            projectionEvent.EventTypeName,
            projectionEvent.SequenceNumber,
            projectionEvent.Timestamp,
            projectionEvent.CorrelationId,
            Record: null,
            UserMessage: userMessage,
            AiResponseCancellation: cancellation);

    private static T? DeserializePayload<T>(ProjectionEventDto projectionEvent)
        => projectionEvent.Payload.Length == 0
            ? default
            : JsonSerializer.Deserialize<T>(projectionEvent.Payload, JsonOptions);

    private static TPublished? TryCreatePublishedEvent<TPublished>(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        if (projectionEvent.Payload.Length == 0)
        {
            return default;
        }

        using JsonDocument payload = JsonDocument.Parse(projectionEvent.Payload);
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
        => name is "tenantId" or "domain" or "aggregateId" or "eventTypeName" or "sequenceNumber" or "correlationId" or "messageId" or "timestamp";

    private sealed record ProjectionDispatchSummary(
        string TenantId,
        string Domain,
        string AggregateId,
        int AppliedEventCount,
        int IgnoredEventCount);
}
