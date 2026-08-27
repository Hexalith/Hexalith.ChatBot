using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
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
    ApprovalProjectionHandler approvalHandler,
    IAiExecutionCoordinator aiExecutionCoordinator) : IDomainProjectionHandler
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

        // Provider work is created only from the event after EventStore has persisted it and invoked this named
        // projection. Replay is safe because the outbox upsert is idempotent on the full generation identity.
        if (string.Equals(projectionEvent.EventTypeName, typeof(LowRiskAiAssistanceExecutionStarted).FullName, StringComparison.Ordinal) &&
            DeserializePayload<LowRiskAiAssistanceExecutionStarted>(projectionEvent) is { } started)
        {
            await aiExecutionCoordinator
                .RecordStartedAsync(request.TenantId, request.AggregateId, projectionEvent.SequenceNumber, started, cancellationToken)
                .ConfigureAwait(false);
        }

        LowRiskAiAssistanceExecutionRecord? terminalExecution =
            DeserializeWhen<LowRiskAiAssistanceExecutionSucceeded>(projectionEvent)?.Record ??
            DeserializeWhen<LowRiskAiAssistanceExecutionFailed>(projectionEvent)?.Record ??
            DeserializeWhen<LowRiskAiAssistanceRoutedToApproval>(projectionEvent)?.Record;
        if (terminalExecution is not null)
        {
            string terminalProjectId = DeserializeWhen<LowRiskAiAssistanceExecutionSucceeded>(projectionEvent)?.ProjectId ??
                DeserializeWhen<LowRiskAiAssistanceExecutionFailed>(projectionEvent)?.ProjectId ??
                DeserializeWhen<LowRiskAiAssistanceRoutedToApproval>(projectionEvent)?.ProjectId ??
                request.AggregateId;
            await aiExecutionCoordinator.RecordTerminalObservedAsync(
                request.TenantId,
                request.AggregateId,
                terminalProjectId,
                terminalExecution.ProposalId,
                terminalExecution.ExecutionId,
                cancellationToken).ConfigureAwait(false);
        }

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
            if (flatConversationEvent.AiResponseCancellation is { } cancellation)
            {
                await aiExecutionCoordinator
                    .RecordCancellationRequestedAsync(cancellation, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (flatConversationEvent.AiResponseCancellationConfirmed is { } confirmation)
            {
                await aiExecutionCoordinator.RecordTerminalObservedAsync(
                    confirmation.TenantId,
                    request.AggregateId,
                    confirmation.ProjectId,
                    confirmation.ResponseId,
                    confirmation.GenerationId,
                    cancellationToken).ConfigureAwait(false);
            }

            if (flatConversationEvent.AiResponseCancellationFailed is { } cancellationFailure)
            {
                await aiExecutionCoordinator
                    .RecordCancellationFailedAsync(cancellationFailure, cancellationToken)
                    .ConfigureAwait(false);
            }

            return await taskIntentHandler.HandleAsync(flatConversationEvent, cancellationToken).ConfigureAwait(false)
                is TaskIntentProjectionHandler.ProjectionOutcome.Applied;
        }

        // The EventStore domain projection payload is the concrete domain-event payload, not one of the
        // Published* adapter envelopes used by the public projection endpoints. Materialize the nested adapter
        // slot explicitly for concrete events whose payload property names do not happen to match that envelope.
        // Without this, a converted task intent deserializes with Record == null and is silently ignored even
        // though both the conversion and approval request are durably present in EventStore.
        if (TryCreateFlatTaskIntentEvent(request, projectionEvent) is { } flatTaskIntentEvent)
        {
            return await taskIntentHandler.HandleAsync(flatTaskIntentEvent, cancellationToken).ConfigureAwait(false)
                is TaskIntentProjectionHandler.ProjectionOutcome.Applied;
        }

        if (TryCreateFlatApprovalEvent(request, projectionEvent) is { } flatApprovalEvent)
        {
            return await approvalHandler.HandleAsync(flatApprovalEvent, cancellationToken).ConfigureAwait(false)
                is ApprovalProjectionHandler.ProjectionOutcome.Applied;
        }

        if (TryCreateFlatAiActionExecutionEvent(request, projectionEvent) is { } flatExecutionEvent)
        {
            return await aiOutcomeHandler.HandleAsync(flatExecutionEvent, cancellationToken).ConfigureAwait(false)
                is AiOutcomeProjectionHandler.ProjectionOutcome.Applied;
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

    private static PublishedTaskIntentEvent? TryCreateFlatTaskIntentEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        if (!string.Equals(
                projectionEvent.EventTypeName,
                typeof(TaskIntentConvertedToAiActionProposal).FullName,
                StringComparison.Ordinal))
        {
            return null;
        }

        TaskIntentConvertedToAiActionProposal? converted = DeserializePayload<TaskIntentConvertedToAiActionProposal>(projectionEvent);
        return converted is null
            ? null
            : NewTaskIntentEnvelope(
                request,
                projectionEvent,
                record: converted.TaskIntent,
                proposal: converted.Proposal);
    }

    private static PublishedAiActionApprovalEvent? TryCreateFlatApprovalEvent(
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

    private static PublishedAiActionExecutionEvent? TryCreateFlatAiActionExecutionEvent(
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

    private static PublishedTaskIntentEvent? TryCreateFlatConversationEvent(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent)
    {
        // Deserialize the flat conversation event into its concrete type and place it in the correct nested
        // PublishedTaskIntentEvent slot so the projection actually fires in production.
        if (string.Equals(projectionEvent.EventTypeName, typeof(ProjectConversationMessageAppended).FullName, StringComparison.Ordinal))
        {
            ProjectConversationMessageAppended? userMessage = DeserializePayload<ProjectConversationMessageAppended>(projectionEvent);
            return userMessage is null
                ? null
                : NewTaskIntentEnvelope(
                    request,
                    projectionEvent,
                    userMessage: userMessage with { SourceVersion = projectionEvent.SequenceNumber });
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationRequested).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationRequested? cancellation = DeserializePayload<AiResponseGenerationCancellationRequested>(projectionEvent);
            return cancellation is null ? null : NewTaskIntentEnvelope(request, projectionEvent, cancellation: cancellation);
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationConfirmed).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationConfirmed? confirmation = DeserializePayload<AiResponseGenerationCancellationConfirmed>(projectionEvent);
            return confirmation is null ? null : NewTaskIntentEnvelope(request, projectionEvent, confirmation: confirmation);
        }

        if (string.Equals(projectionEvent.EventTypeName, typeof(AiResponseGenerationCancellationFailed).FullName, StringComparison.Ordinal))
        {
            AiResponseGenerationCancellationFailed? failure = DeserializePayload<AiResponseGenerationCancellationFailed>(projectionEvent);
            return failure is null ? null : NewTaskIntentEnvelope(request, projectionEvent, cancellationFailure: failure);
        }

        return null;
    }

    private static PublishedTaskIntentEvent NewTaskIntentEnvelope(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent,
        Hexalith.ChatBot.Contracts.Queries.TaskIntentRecord? record = null,
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
            Record: record,
            Proposal: proposal,
            UserMessage: userMessage,
            AiResponseCancellation: cancellation,
            AiResponseCancellationConfirmed: confirmation,
            AiResponseCancellationFailed: cancellationFailure);

    private static T? DeserializePayload<T>(ProjectionEventDto projectionEvent)
        => projectionEvent.Payload.Length == 0
            ? default
            : JsonSerializer.Deserialize<T>(projectionEvent.Payload, JsonOptions);

    private static T? DeserializeWhen<T>(ProjectionEventDto projectionEvent)
        => string.Equals(projectionEvent.EventTypeName, typeof(T).FullName, StringComparison.Ordinal)
            ? DeserializePayload<T>(projectionEvent)
            : default;

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
