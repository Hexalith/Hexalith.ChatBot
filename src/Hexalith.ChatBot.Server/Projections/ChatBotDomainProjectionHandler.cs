using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using static Hexalith.ChatBot.Server.Projections.ChatBotProjectionPayload;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ChatBotDomainProjectionHandler(
    GovernedOperationProjectionHandler governedOperationHandler,
    GovernedControlStateProjectionHandler controlStateHandler,
    AssociationProjectionHandler associationHandler,
    ParticipantResolutionProjectionHandler participantResolutionHandler,
    AiOutcomeProjectionHandler aiOutcomeHandler,
    TaskIntentProjectionHandler taskIntentHandler,
    ApprovalProjectionHandler approvalHandler,
    IAiExecutionCoordinator aiExecutionCoordinator,
    ReviewQueueStore? queues = null, Lifecycle.Workflows.ReviewRetryScheduler? retryScheduler = null) : IDomainProjectionHandler
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
        if (request.Domain != Domain || string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.AggregateId))
        {
            throw new ArgumentException("Invalid projection scope.", nameof(request));
        }
        int applied = 0;
        int ignored = 0;

        foreach (ProjectionEventDto projectionEvent in request.Events ?? [])
        {
            List<ReviewQueueTransition> queueFacts = [];
            var normalized = projectionEvent with { EventTypeName = projectionEvent.EventTypeName.Split(',', 2)[0] };
            if (await ProjectEventAsync(request, normalized, queueFacts, cancellationToken).ConfigureAwait(false))
            {
                applied++;
            }
            else
            {
                ignored++;
            }
            if (queues is not null && projectionEvent.SequenceNumber > 0 && projectionEvent.Timestamp != default &&
                (queueFacts.Count > 0 || !RequiresQueueFact(normalized.EventTypeName)))
            {
                await queues.RecordCoverageAsync(request, projectionEvent, cancellationToken, queueFacts).ConfigureAwait(false);
            }
        }

        return new ProjectionDispatchSummary(request.TenantId, request.Domain, request.AggregateId, applied, ignored);
    }

    private async Task<bool> ProjectEventAsync(
        ProjectionRequest request,
        ProjectionEventDto projectionEvent,
        List<ReviewQueueTransition> queueFacts,
        CancellationToken cancellationToken)
    {
        if (projectionEvent.SequenceNumber <= 0 || projectionEvent.Timestamp == default)
        {
            return false;
        }
        async Task PersistQueueAsync(ReviewQueueTransition fact, CancellationToken token)
        {
            if (queues is null) { return; }
            await queues.ApplyAsync(fact, token).ConfigureAwait(false);
            queueFacts.Add(fact);
            if (fact.Family == OperationalQueueFamily.RetryableOperation && fact.OpensWork && fact.SourceMessageId is { } message)
            {
                var ready = await queues.LinkFailureAsync(fact, message, token).ConfigureAwait(false);
                await StartRetriesAsync(ready, token).ConfigureAwait(false);
            }
        }

        ReviewQueueTransition Fact(OperationalQueueFamily family, string subject, string? identityProject, string[] projects,
            string state, bool terminal, string? reason, string owner, string next)
            => new(request.TenantId, request.AggregateId, projectionEvent.SequenceNumber, projectionEvent.MessageId ?? string.Empty,
                projectionEvent.CorrelationId, family, subject, identityProject, projects, state, terminal, reason, owner, next,
                projectionEvent.Timestamp.ToUniversalTime(), Actor: projectionEvent.UserId, SourceMessageId: projectionEvent.MessageId);

        if (DeserializeWhen<WorkflowRetryRequested>(projectionEvent) is { } retry)
        {
            if (queues is not null)
            {
                var ready = await queues.RecordRetryAsync(request.TenantId, new(retry.RetryId, retry.FailedEventId, retry.ExpectedFailedSourceVersion,
                    retry.FailedOperationClass, request.AggregateId, projectionEvent.SequenceNumber, projectionEvent.Timestamp,
                    projectionEvent.CorrelationId, projectionEvent.UserId), cancellationToken).ConfigureAwait(false);
                await StartRetriesAsync(ready, cancellationToken).ConfigureAwait(false);
            }
            return true;
        }
        if (DeserializeWhen<WorkflowReviewTransitionRecorded>(projectionEvent) is { } workflow)
        {
            var command = workflow.Transition;
            if (command.StreamId(request.TenantId) != request.AggregateId)
            { throw new InvalidDataException("workflow_review_source_mismatch"); }
            bool recovered = command.State == "recovered";
            var fact = Fact(command.Family, command.SubjectId, command.ProjectId, [command.ProjectId], command.State, recovered || command.State is "exhausted" or "nonretryable",
                command.Reason, "operations-admin", recovered ? "none" : command.State is "exhausted" or "nonretryable" ? "escalate" : "retry")
                with { RetryCount = command.Attempt, OpensWork = !recovered, Generation = command.Generation,
                    ProducerFamily = command.Family, RetryTarget = command.Target };
            await PersistQueueAsync(fact, cancellationToken).ConfigureAwait(false);
            await PersistQueueAsync(fact with { Family = OperationalQueueFamily.RetryableOperation, SubjectId = request.AggregateId }, cancellationToken).ConfigureAwait(false);
            return true;
        }

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
            if (ApprovalProjectionTranslator.TryCreateView(flatApprovalEvent) is { } view)
            {
                var fact = Fact(OperationalQueueFamily.PendingApproval, view.ApprovalId, view.ProjectId, [view.ProjectId],
                    view.Status.ToString(), view.Status != ApprovalStatus.Pending, view.DecisionKind?.ToString() ?? view.DisabledReason,
                    "approval-reviewer", view.SafeNextAction ?? "review") with
                {
                    At = view.OccurredAtUtc, Risk = view.RiskClass?.ToString().ToLowerInvariant(),
                    ResourceIds = view.AffectedResourceReferences?.ToArray(),
                    Actor = view.DecisionActorId ?? view.RequesterId, OperationId = view.AuditOperationId ?? projectionEvent.MessageId ?? string.Empty,
                };
                await PersistQueueAsync(fact, cancellationToken).ConfigureAwait(false);
            }
            return await approvalHandler.HandleAsync(flatApprovalEvent, cancellationToken).ConfigureAwait(false)
                is ApprovalProjectionHandler.ProjectionOutcome.Applied;
        }

        if (TryCreateFlatAiActionExecutionEvent(request, projectionEvent) is { } flatExecutionEvent)
        {
            var outcome = ApprovedAiActionOutcomeProjectionTranslator.TryCreatePublishedEvents(flatExecutionEvent).FirstOrDefault()
                ?? LowRiskAiOutcomeProjectionTranslator.TryCreatePublishedEvents(flatExecutionEvent).FirstOrDefault();
            if (outcome is not null && outcome.ProposalId is { } proposal)
            {
                if (outcome.OutcomeStatus == AiOutcomeStatus.Invalidated && outcome.ApprovalId is { } approval)
                {
                    await PersistQueueAsync(Fact(OperationalQueueFamily.PendingApproval, approval, outcome.ProjectId, [outcome.ProjectId],
                        "Superseded", true, outcome.FailureCode, "approval-reviewer", "none"), cancellationToken).ConfigureAwait(false);
                }
                if (outcome.OutcomeStatus is AiOutcomeStatus.Failed or AiOutcomeStatus.Succeeded or AiOutcomeStatus.Invalidated or AiOutcomeStatus.Blocked)
                {
                    bool retryable = outcome.Retryability == "retryable";
                    await PersistQueueAsync(Fact(OperationalQueueFamily.RetryableOperation, proposal, outcome.ProjectId, [outcome.ProjectId],
                        outcome.OutcomeStatus.ToString(), !retryable, outcome.FailureCode, "operations-admin", outcome.SafeNextAction ?? "none")
                        with { OpensWork = retryable, At = outcome.OccurredAtUtc, Risk = outcome.RiskClass?.ToString(),
                            OperationId = outcome.OperationId ?? projectionEvent.MessageId ?? string.Empty }, cancellationToken).ConfigureAwait(false);
                }
            }
            return await aiOutcomeHandler.HandleAsync(flatExecutionEvent, cancellationToken).ConfigureAwait(false)
                is AiOutcomeProjectionHandler.ProjectionOutcome.Applied;
        }

        if (projectionEvent.EventTypeName == MailboxIntakeProjectionTranslator.IntakeCapturedEventType &&
            TryCreatePublishedEvent<PublishedMailboxIntakeEvent>(request, projectionEvent) is { } mailboxEvent &&
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
            var association = associationNotification;
            bool terminal = association.LifecycleState is not (LifecycleState.NeedsReview or LifecycleState.Deferred or LifecycleState.CorrectionDelayed);
            string[] projects = association.ProjectId is { } project ? [project]
                : association.Candidates.Select(c => c.ProjectId).Distinct(StringComparer.Ordinal).ToArray();
            var fact = Fact(OperationalQueueFamily.AmbiguousAssociation, association.AssociationId, null, projects,
                association.LifecycleState.ToString(), terminal, association.RoutingReason ?? association.Outcome.ToString(),
                association.ResponsibleOwnerRole ?? "association-reviewer", association.SafeNextAction ?? (terminal ? "none" : "review"))
                with { Confidence = (decimal)association.ConfidenceScore, Mailbox = association.SourceMailboxId, Intake = association.IntakeId,
                    At = association.CorrectedAt ?? association.DecidedAt ?? projectionEvent.Timestamp, OpensWork = !terminal };
            await PersistQueueAsync(fact, cancellationToken).ConfigureAwait(false);
            if (association.SupersedesAssociationId is { } prior && prior != association.AssociationId)
            {
                await PersistQueueAsync(fact with { SubjectId = prior, State = "Superseded", Terminal = true,
                    NextAction = "none", OpensWork = false }, cancellationToken).ConfigureAwait(false);
            }
            _ = await associationHandler.HandleAsync(associationNotification, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (TryCreatePublishedEvent<PublishedParticipantResolutionEvent>(request, projectionEvent) is { } participantEvent &&
            ParticipantResolutionProjectionTranslator.TryCreateNotification(participantEvent) is { } participantNotification)
        {
            var participant = participantNotification;
            bool resolved = participant.Status == ParticipantResolutionStatus.Resolved;
            string participantKey = ProjectConversationStableId.Hash(JsonSerializer.Serialize(new[] { participant.IntakeId, participant.SourceParticipantId }));
            await PersistQueueAsync(Fact(OperationalQueueFamily.UnresolvedParticipant, participantKey, null, [],
                participant.Status.ToString(), resolved, participant.Reason?.ToString(), "participant-reviewer",
                resolved ? "none" : participant.AllowedReviewActions.Count == 0 ? "review" : participant.AllowedReviewActions[0].ToString())
                with { Mailbox = participant.SourceMailboxId, Intake = participant.IntakeId, OpensWork = !resolved }, cancellationToken).ConfigureAwait(false);
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

    private static bool RequiresQueueFact(string eventType)
        => eventType == typeof(WorkflowReviewTransitionRecorded).FullName ||
            eventType == typeof(AiActionApprovalRequested).FullName || eventType == typeof(AiActionApprovalDecisionRecorded).FullName ||
            eventType == typeof(ApprovedAiActionExecutionFailed).FullName || eventType == typeof(LowRiskAiAssistanceExecutionFailed).FullName ||
            eventType == typeof(AiActionProposalInvalidatedByCorrection).FullName ||
            eventType == ParticipantResolutionProjectionTranslator.ResolvedEventType || eventType == ParticipantResolutionProjectionTranslator.UnresolvedEventType ||
            AssociationProjectionTranslator.IsAssociationEvent(eventType);

    private async Task StartRetriesAsync(IEnumerable<Lifecycle.Workflows.WorkflowReviewRetry> ready, CancellationToken token)
    {
        foreach (var work in ready)
        {
            if (retryScheduler is null) { throw new InvalidOperationException("review_retry_scheduler_unavailable"); }
            await retryScheduler.StartOrJoinAsync(work, token).ConfigureAwait(false);
        }
    }

    private sealed record ProjectionDispatchSummary(
        string TenantId,
        string Domain,
        string AggregateId,
        int AppliedEventCount,
        int IgnoredEventCount);
}
