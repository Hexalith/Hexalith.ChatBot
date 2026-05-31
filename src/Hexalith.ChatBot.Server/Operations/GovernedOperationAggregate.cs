using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Event-sourced aggregate (Pattern A) for the Story 1.9 walking-skeleton governed note. The base
/// <see cref="EventStoreAggregate{TState}"/> is itself the <c>IDomainProcessor</c>: it reflection-discovers
/// the typed <see cref="Handle(RecordGovernedNote, GovernedOperationState?)"/> and the state's <c>Apply</c>
/// method. <see cref="Handle"/> is pure — no I/O, DAPR, authorization, or sibling calls — and never throws for
/// a business-rule violation (it returns a structured rejection so the idempotency cache is honored).
/// </summary>
public sealed class GovernedOperationAggregate : EventStoreAggregate<GovernedOperationState>
{
    /// <summary>
    /// Records a governed note. Fine-grained (aggregate-altitude) idempotency: recording a second note
    /// against an already-recorded aggregate yields a structured rejection rather than a duplicate event,
    /// so a repeated submission resolves to exactly one durable effect.
    /// </summary>
    /// <param name="command">The governed note command.</param>
    /// <param name="state">The replayed aggregate state, or <see langword="null"/> for a new aggregate.</param>
    /// <returns>A success result carrying the recorded-note event, or a structured rejection.</returns>
    public static DomainResult Handle(RecordGovernedNote command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (state is { IsRecorded: true })
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new GovernedNoteAlreadyRecordedRejection(command.NoteId),
            });
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new GovernedNoteRecorded(command.NoteId),
        });
    }

    public static DomainResult Handle(CaptureMailboxMessageIntake command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return Invalid(command.IntakeId, "invalid_intake_id");
        }

        if (state is { IsMailboxIntakeCaptured: true })
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new MailboxMessageIntakeAlreadyCapturedRejection(command.IntakeId),
            });
        }

        if (command.Source is null ||
            command.Recipients is null ||
            command.Attachments is null ||
            command.Source.Sender is null ||
            string.IsNullOrWhiteSpace(command.Source.ProviderMessageId) ||
            string.IsNullOrWhiteSpace(command.Source.MailboxId) ||
            string.IsNullOrWhiteSpace(command.Source.InternetMessageId) ||
            string.IsNullOrWhiteSpace(command.Source.ConversationId) ||
            string.IsNullOrWhiteSpace(command.Source.SourceContext) ||
            string.IsNullOrWhiteSpace(command.Source.Sender.Address) ||
            command.Source.SourceSchemaVersion <= 0 ||
            command.Recipients.Count == 0 ||
            command.Recipients.Any(static recipient => string.IsNullOrWhiteSpace(recipient.Address) || string.IsNullOrWhiteSpace(recipient.Kind)) ||
            command.Attachments.Any(static attachment => string.IsNullOrWhiteSpace(attachment.ProviderAttachmentId)))
        {
            return Invalid(command.IntakeId, "missing_source_identity");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxMessageIntakeCaptured(
                command.IntakeId,
                command.Source.ProviderMessageId,
                command.Source.InternetMessageId,
                command.Source.ConversationId,
                command.Source.ThreadId,
                command.Source.MailboxId,
                command.Source.Sender,
                command.Recipients,
                command.Source.ReceivedAt.ToUniversalTime(),
                command.Source.SentAt?.ToUniversalTime(),
                command.Source.CreatedAt?.ToUniversalTime(),
                command.Attachments,
                command.Source.SourceTimezone,
                command.Source.SourceContext,
                "m365-mailbox-intake",
                "mailbox-intake.kernel.v1",
                "metadata_only",
            "collaboration_input",
            command.Source.SourceSchemaVersion),
        });
    }

    public static DomainResult Handle(RequestFailedWorkflowRetry command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!ChatBotIdentity.IsValidUlid(command.RetryId) ||
            !ChatBotIdentity.IsValidUlid(command.FailedEventId) ||
            string.IsNullOrWhiteSpace(command.FailedOperationClass) ||
            string.IsNullOrWhiteSpace(command.FailureReasonCode) ||
            command.ExpectedFailedSourceVersion <= 0)
        {
            return InvalidRetry(command.RetryId, "invalid_workflow_retry_payload");
        }

        if (state?.WorkflowRetryIds.Contains(command.RetryId) == true)
        {
            return InvalidRetry(command.RetryId, "workflow_retry_already_recorded");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new WorkflowRetryRequested(
                command.RetryId,
                command.FailedEventId,
                command.FailedOperationClass,
                command.FailureReasonCode,
                command.ExpectedFailedSourceVersion,
                command.Rationale),
        });
    }

    public static DomainResult Handle(ResolveMailboxMessageParticipants command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!ParticipantResolutionId.TryParse(command.ResolutionId, out _) ||
            !MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return InvalidResolution(command.ResolutionId, "invalid_resolution_identity");
        }

        if (state?.ParticipantResolutionIds.Contains(command.ResolutionId) == true)
        {
            return InvalidResolution(command.ResolutionId, "participant_resolution_already_recorded");
        }

        if (command.SourceParticipants is null ||
            command.ResolvedParticipants is null ||
            command.UnresolvedParticipants is null ||
            string.IsNullOrWhiteSpace(command.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(command.ResolutionKernelVersion) ||
            command.SourceParticipants.Count == 0 ||
            (command.ResolvedParticipants.Count + command.UnresolvedParticipants.Count) == 0)
        {
            return InvalidResolution(command.ResolutionId, "missing_participant_resolution");
        }

        HashSet<string> sourceIds = command.SourceParticipants
            .Select(static source => source.SourceParticipantId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        if (sourceIds.Count != command.SourceParticipants.Count)
        {
            return InvalidResolution(command.ResolutionId, "invalid_source_participant");
        }

        List<IEventPayload> events = [];
        foreach (ResolvedMailboxParticipantReference resolved in command.ResolvedParticipants)
        {
            if (!sourceIds.Contains(resolved.SourceParticipantId) ||
                resolved.Status != ParticipantResolutionStatus.Resolved ||
                string.IsNullOrWhiteSpace(resolved.PartyId) ||
                string.IsNullOrWhiteSpace(resolved.PartyTenantId) ||
                string.IsNullOrWhiteSpace(resolved.EvidenceReference) ||
                string.IsNullOrWhiteSpace(resolved.EvidenceFingerprint))
            {
                return InvalidResolution(command.ResolutionId, "invalid_resolved_participant");
            }

            events.Add(new MailboxParticipantResolved(
                command.ResolutionId,
                command.IntakeId,
                resolved.SourceParticipantId,
                resolved.PartyId,
                resolved.PartyTenantId,
                resolved.EvidenceReference,
                resolved.EvidenceFingerprint,
                command.SourceMailboxId,
                "m365-mailbox-intake",
                command.ResolutionKernelVersion,
                "metadata_only",
                "collaboration_input",
                1,
                "chatbot.participant-resolution-event.v1"));
        }

        foreach (UnresolvedMailboxParticipantEvidence unresolved in command.UnresolvedParticipants)
        {
            if (!sourceIds.Contains(unresolved.SourceParticipantId) ||
                string.IsNullOrWhiteSpace(unresolved.EvidenceReference) ||
                string.IsNullOrWhiteSpace(unresolved.EvidenceFingerprint) ||
                unresolved.AllowedReviewActions is null ||
                unresolved.AllowedReviewActions.Count == 0)
            {
                return InvalidResolution(command.ResolutionId, "invalid_unresolved_participant");
            }

            events.Add(new MailboxParticipantUnresolved(
                command.ResolutionId,
                command.IntakeId,
                unresolved.SourceParticipantId,
                unresolved.EvidenceReference,
                unresolved.EvidenceFingerprint,
                unresolved.Reason,
                unresolved.AllowedReviewActions,
                command.SourceMailboxId,
                "m365-mailbox-intake",
                command.ResolutionKernelVersion,
                "metadata_only",
                "collaboration_input",
                1,
                "chatbot.participant-resolution-event.v1"));
        }

        return DomainResult.Success(events);
    }

    public static DomainResult Handle(ScoreMailboxMessageAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!AssociationWorkflowId.TryParse(command.AssociationId, out _) ||
            !MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_identity");
        }

        if (state?.AssociationIds.Contains(command.AssociationId) == true)
        {
            return InvalidAssociation(command.AssociationId, "association_scoring_already_recorded");
        }

        if (command.DeterministicSignals is null ||
            command.Candidates is null ||
            command.Exclusions is null ||
            command.Result is null ||
            command.ThresholdPolicy is null ||
            command.DeterministicSignals.Count == 0 ||
            string.IsNullOrWhiteSpace(command.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(command.SourceConversationId) ||
            string.IsNullOrWhiteSpace(command.ScoringKernelVersion) ||
            !AssociationThresholdPolicyValidator.IsValid(command.ThresholdPolicy) ||
            !IsValidScore(command.Result.ConfidenceScore) ||
            !IsConsistentAssociationResult(command, envelope))
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_scoring_payload");
        }

        if (command.Candidates.Any(static candidate =>
            string.IsNullOrWhiteSpace(candidate.ProjectId) ||
            candidate.ConfidenceScore < 0.0 ||
            candidate.ConfidenceScore > 1.0 ||
            candidate.Rank <= 0 ||
            candidate.ReasonCodes is null ||
            candidate.EvidenceRefs is null ||
            candidate.ConfidenceInputs is null))
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_candidate");
        }

        if (IsAutoAssociatedButInvalid(command, command.Result))
        {
            return InvalidAssociation(command.AssociationId, "invalid_auto_association_scoring_payload");
        }

        if (command.Result.Outcome == AssociationScoringOutcome.FailedClosed && command.Candidates.Count != 0)
        {
            return InvalidAssociation(command.AssociationId, "invalid_fail_closed_association_scoring_payload");
        }

        if (RoutesToReview(command.Result) && !IsValidReviewTransition())
        {
            return InvalidAssociation(command.AssociationId, "invalid_association_lifecycle_transition");
        }

        string tenantId = envelope.TenantId;
        AssociationScoringResult result = command.Result;
        return result.Outcome switch
        {
            AssociationScoringOutcome.AutoAssociated when IsValidAutoAssociation(command, result) =>
                DomainResult.Success(new IEventPayload[]
                {
                    new MailboxEmailAssociatedToProject(
                        command.AssociationId,
                        command.IntakeId,
                        tenantId,
                        command.Candidates[0].ProjectId,
                        command.Candidates[0].DisplayName,
                        command.SourceMailboxId,
                        command.SourceConversationId,
                        command.SourceThreadId,
                        command.Candidates[0].EvidenceRefs,
                        command.Candidates[0].ConfidenceInputs,
                        result.ConfidenceScore,
                        result.ThresholdBand,
                        result.ReasonCodes,
                        command.ThresholdPolicy.PolicyVersion,
                        result.KernelVersion,
                        result.DetectedAt.ToUniversalTime(),
                        result.RedactionState,
                        result.RetentionClass,
                        1,
                        result.SchemaVersion,
                        envelope.CorrelationId,
                        envelope.UserId,
                        ActorType(envelope, "system"),
                        "associate",
                        SurfaceOrigin(envelope, "worker"),
                        result.DetectedAt.ToUniversalTime()),
                }),
            AssociationScoringOutcome.FailedClosed =>
                DomainResult.Success(new IEventPayload[]
                {
                    new MailboxAssociationScoringFailedClosed(
                        command.AssociationId,
                        command.IntakeId,
                        tenantId,
                        command.SourceMailboxId,
                        command.SourceConversationId,
                        command.SourceThreadId,
                        command.Exclusions,
                        LifecycleState.NeedsReview,
                        result.ConfidenceScore,
                        result.ThresholdBand,
                        result.ReasonCodes,
                        command.ThresholdPolicy.PolicyVersion,
                        result.KernelVersion,
                        result.DetectedAt.ToUniversalTime(),
                        result.RedactionState,
                        result.RetentionClass,
                        1,
                        result.SchemaVersion,
                        envelope.CorrelationId),
                }),
            _ =>
                DomainResult.Success(new IEventPayload[]
                {
                    new MailboxAssociationCandidatesGenerated(
                        command.AssociationId,
                        command.IntakeId,
                        tenantId,
                        command.SourceMailboxId,
                        command.SourceConversationId,
                        command.SourceThreadId,
                        command.Candidates,
                        command.Exclusions,
                        LifecycleState.NeedsReview,
                        result.ConfidenceScore,
                        result.ThresholdBand,
                        result.Outcome,
                        result.ReasonCodes,
                        command.ThresholdPolicy.PolicyVersion,
                        result.KernelVersion,
                        result.DetectedAt.ToUniversalTime(),
                        result.RedactionState,
                        result.RetentionClass,
                        1,
                        result.SchemaVersion,
                        envelope.CorrelationId),
                }),
        };
    }

    public static DomainResult Handle(SetAssociationConfidenceThresholds command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(command.PolicyId) ||
            string.IsNullOrWhiteSpace(command.PolicyVersion))
        {
            return InvalidThreshold(command.PolicyId, "invalid_threshold_policy");
        }

        if (state?.ThresholdPolicyVersions.Contains(command.PolicyVersion) == true)
        {
            return InvalidThreshold(command.PolicyId, "threshold_policy_already_recorded");
        }

        if (!AssociationThresholdPolicyValidator.IsValid(command.THigh, command.TLow, command.EvaluationRunReference))
        {
            return InvalidThreshold(command.PolicyId, "invalid_threshold_policy");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new AssociationConfidenceThresholdsChanged(
                command.PolicyId,
                envelope.TenantId,
                state?.AssociationTHigh ?? AssociationThresholdPolicySnapshot.DefaultM0High,
                state?.AssociationTLow ?? AssociationThresholdPolicySnapshot.DefaultM0Low,
                state?.AssociationThresholdPolicyVersion ?? AssociationThresholdPolicySnapshot.DefaultM0.PolicyVersion,
                command.THigh,
                command.TLow,
                command.PolicyVersion,
                command.EvaluationRunReference,
                envelope.UserId,
                envelope.CorrelationId,
                (command.ChangedAt ?? DateTimeOffset.UnixEpoch).ToUniversalTime(),
                "metadata_only",
                "collaboration_input",
                1,
                "chatbot.association-threshold-policy-event.v1"),
        });
    }

    public static DomainResult Handle(AssociateEmailToProject command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.Associate,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        AssociationCandidate? selected = source.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, command.ProjectId, StringComparison.Ordinal));
        if (selected is null)
        {
            return InvalidAssociationDecision(command.AssociationId, "missing_authorized_candidate");
        }

        if (!EvidenceFingerprintMatches(selected.EvidenceRefs, command.CandidateEvidenceFingerprint))
        {
            return InvalidAssociationDecision(command.AssociationId, "stale_evidence");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationConfirmed(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                selected.ProjectId,
                selected.DisplayName,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                selected.EvidenceRefs,
                selected.ConfidenceInputs,
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(RejectEmailProjectAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.Reject,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationRejected(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                AllEvidenceRefs(source),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(DeferEmailProjectAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.Defer,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationDeferred(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                AllEvidenceRefs(source),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(MarkEmailAssociationNeedsReview command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionValidation validation = ValidateDecision(
            command.AssociationId,
            command.IntakeId,
            command.DecisionKind,
            AssociationDecisionKind.NeedsReview,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion,
            command.SchemaVersion,
            command.DecisionNote,
            state);
        if (!validation.IsValid)
        {
            return InvalidAssociationDecision(command.AssociationId, validation.ReasonCode);
        }

        AssociationDecisionSourceSnapshot source = validation.Source!;
        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationMarkedNeedsReview(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.DecisionKind,
                source.Candidates.Select(static candidate => candidate.ProjectId).ToArray(),
                AllEvidenceRefs(source),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                source.SourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                validation.SanitizedNote,
                "metadata_only",
                source.ThresholdPolicyVersion),
        });
    }

    public static DomainResult Handle(CorrectEmailProjectAssociation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!AssociationWorkflowId.TryParse(command.AssociationId, out _) ||
            !MailboxMessageIntakeId.TryParse(command.IntakeId, out _) ||
            !AssociationWorkflowId.TryParse(command.PredecessorAssociationId, out _) ||
            command.CorrectionKind != AssociationCorrectionKind.ProjectReassignment ||
            string.IsNullOrWhiteSpace(command.PriorProjectId) ||
            string.IsNullOrWhiteSpace(command.TargetProjectId) ||
            string.IsNullOrWhiteSpace(command.CandidateEvidenceFingerprint) ||
            command.SourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(command.SchemaVersion))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_association_correction_payload");
        }

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (source is null ||
            !string.Equals(source.AssociationId, command.AssociationId, StringComparison.Ordinal) ||
            !string.Equals(source.IntakeId, command.IntakeId, StringComparison.Ordinal))
        {
            return InvalidAssociationCorrection(command.AssociationId, "missing_association_evidence");
        }

        if (state!.AssociationLifecycleState is not (LifecycleState.Associated or LifecycleState.Corrected))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_association_lifecycle_transition");
        }

        long currentSourceVersion = state.LastAssociationDecisionSourceVersion ?? source.SourceVersion;
        if (currentSourceVersion != command.SourceVersion)
        {
            return InvalidAssociationCorrection(command.AssociationId, "stale_evidence");
        }

        if (string.IsNullOrWhiteSpace(state.CurrentAssociationProjectId) ||
            !string.Equals(state.CurrentAssociationProjectId, command.PriorProjectId, StringComparison.Ordinal) ||
            string.Equals(state.CurrentAssociationProjectId, command.TargetProjectId, StringComparison.Ordinal))
        {
            return InvalidAssociationCorrection(command.AssociationId, "association_already_corrected");
        }

        if (!EvidenceFingerprintMatches(AllEvidenceRefs(source), command.CandidateEvidenceFingerprint))
        {
            return InvalidAssociationCorrection(command.AssociationId, "stale_evidence");
        }

        if (!TrySanitizeDecisionNote(command.CorrectionRationale, out string? sanitizedRationale))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_rationale");
        }

        AssociationEvidenceReference correctionEvidence = new(
            "association:correction",
            command.CandidateEvidenceFingerprint,
            "association-correction");

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxEmailAssociationCorrected(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                envelope.UserId,
                ActorType(envelope),
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionKind,
                state.CurrentAssociationProjectId,
                command.TargetProjectId,
                null,
                command.PredecessorAssociationId,
                command.PredecessorAssociationId,
                source.Candidates.Select(static candidate => candidate.ProjectId).Append(command.TargetProjectId).Distinct(StringComparer.Ordinal).ToArray(),
                [correctionEvidence],
                source.Candidates.SelectMany(static candidate => candidate.ConfidenceInputs).ToArray(),
                source.ConfidenceScore,
                source.ThresholdBand,
                source.ReasonCodes,
                source.ThresholdPolicyVersion,
                source.DerivationKernelVersion,
                source.DetectedAt,
                DecisionTimestamp(envelope, source.DetectedAt),
                AssociationCandidateView.MailboxSourceProvenance,
                source.RedactionState,
                source.RetentionClass,
                currentSourceVersion + 1,
                command.SchemaVersion,
                envelope.CorrelationId,
                SurfaceOrigin(envelope),
                sanitizedRationale,
                "metadata_only",
                source.ThresholdPolicyVersion,
                CorrectionPropagationStatuses.Pending),
        });
    }

    public static DomainResult Handle(StartMailboxAssociationCorrectionPropagation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, command.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not LifecycleState.Corrected ||
            !string.Equals(source.AssociationId, command.AssociationId, StringComparison.Ordinal) ||
            !string.Equals(source.IntakeId, command.IntakeId, StringComparison.Ordinal) ||
            state.LastAssociationDecisionSourceVersion != command.SourceVersion ||
            string.IsNullOrWhiteSpace(command.PriorProjectId) ||
            string.IsNullOrWhiteSpace(command.CorrectedProjectId) ||
            command.RequiredStoreKeys is not { Count: > 0 } ||
            command.RequiredStoreKeys.Any(static key => !CorrectionPropagationStoreKeys.RequiredM0Set.Contains(key)) ||
            command.EstimatedCompletionAtUtc < command.StartedAtUtc ||
            string.IsNullOrWhiteSpace(command.ResponsibleOwnerRole) ||
            string.IsNullOrWhiteSpace(command.NextSafeAction))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_propagation_start");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionPropagationStarted(
                command.AssociationId,
                command.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.WorkflowInstanceId,
                command.PriorProjectId,
                command.CorrectedProjectId,
                command.RequiredStoreKeys.Distinct(StringComparer.Ordinal).ToArray(),
                command.StartedAtUtc.ToUniversalTime(),
                command.EstimatedCompletionAtUtc.ToUniversalTime(),
                command.ResponsibleOwnerRole,
                command.NextSafeAction,
                source.RedactionState,
                source.RetentionClass,
                command.SourceVersion,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    public static DomainResult Handle(AcknowledgeMailboxAssociationCorrectionStoreInvalidated command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, source?.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not (LifecycleState.Correcting or LifecycleState.CorrectionDelayed) ||
            !string.Equals(state.CorrectionPropagationCorrectionId, command.CorrectionId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrectionPropagationWorkflowInstanceId, command.WorkflowInstanceId, StringComparison.Ordinal) ||
            state.CorrectionPropagationSourceVersion != command.SourceVersion ||
            !state.CorrectionPropagationRequiredStores.Contains(command.StoreKey) ||
            !string.Equals(command.PriorProjectId, state.PriorAssociationProjectId, StringComparison.Ordinal) ||
            !string.Equals(command.CorrectedProjectId, state.CurrentAssociationProjectId, StringComparison.Ordinal) ||
            command.CompletedAtUtc < command.StartedAtUtc ||
            command.Outcome is not ("success" or "failed") ||
            (string.Equals(command.Outcome, "failed", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(command.FailureReasonCode)) ||
            !IsMetadataOnly(command.RedactionState, command.RetentionClass))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_store_acknowledgement");
        }

        if (state.CorrectionPropagationStores.TryGetValue(command.StoreKey, out CorrectionPropagationStoreAcknowledgement? existing) &&
            existing.SourceVersion == command.SourceVersion &&
            string.Equals(existing.Outcome, command.Outcome, StringComparison.Ordinal) &&
            string.Equals(existing.FailureReasonCode, command.FailureReasonCode, StringComparison.Ordinal) &&
            existing.StartedAtUtc == command.StartedAtUtc.ToUniversalTime() &&
            existing.CompletedAtUtc == command.CompletedAtUtc.ToUniversalTime())
        {
            return DomainResult.NoOp();
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionStoreInvalidated(
                command.AssociationId,
                source.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.StoreKey,
                command.WorkflowInstanceId,
                command.SourceVersion,
                command.PriorProjectId,
                command.CorrectedProjectId,
                command.StartedAtUtc.ToUniversalTime(),
                command.CompletedAtUtc.ToUniversalTime(),
                command.Outcome,
                command.FailureReasonCode,
                command.RedactionState,
                command.RetentionClass,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    public static DomainResult Handle(CompleteMailboxAssociationCorrectionPropagation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, source?.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not (LifecycleState.Correcting or LifecycleState.CorrectionDelayed) ||
            !string.Equals(state.CorrectionPropagationCorrectionId, command.CorrectionId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrectionPropagationWorkflowInstanceId, command.WorkflowInstanceId, StringComparison.Ordinal) ||
            state.CorrectionPropagationSourceVersion != command.SourceVersion ||
            string.IsNullOrWhiteSpace(state.PriorAssociationProjectId) ||
            string.IsNullOrWhiteSpace(state.CurrentAssociationProjectId) ||
            !string.Equals(command.DownstreamImpactStatus, CorrectionPropagationStatuses.Complete, StringComparison.Ordinal) ||
            state.CorrectionPropagationRequiredStores.Count == 0 ||
            state.CorrectionPropagationRequiredStores.Any(storeKey => !state.CorrectionPropagationStores.TryGetValue(storeKey, out CorrectionPropagationStoreAcknowledgement? ack) || !ack.IsSuccessful))
        {
            return InvalidAssociationCorrection(command.AssociationId, "incomplete_correction_propagation");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionPropagationCompleted(
                command.AssociationId,
                source.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.WorkflowInstanceId,
                command.SourceVersion,
                state.PriorAssociationProjectId ?? string.Empty,
                state.CurrentAssociationProjectId ?? string.Empty,
                state.CorrectionPropagationStores.Values.Where(static ack => ack.IsSuccessful).Select(static ack => ack.StoreKey).Order(StringComparer.Ordinal).ToArray(),
                command.CompletedAtUtc.ToUniversalTime(),
                command.DownstreamImpactStatus,
                source.RedactionState,
                source.RetentionClass,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    public static DomainResult Handle(DelayMailboxAssociationCorrectionPropagation command, GovernedOperationState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (!IsValidPropagationCommand(command.AssociationId, source?.IntakeId, command.CorrectionId, command.WorkflowInstanceId, command.SourceVersion, command.SchemaVersion) ||
            source is null ||
            state is null ||
            state.AssociationLifecycleState is not LifecycleState.Correcting ||
            !string.Equals(state.CorrectionPropagationCorrectionId, command.CorrectionId, StringComparison.Ordinal) ||
            !string.Equals(state.CorrectionPropagationWorkflowInstanceId, command.WorkflowInstanceId, StringComparison.Ordinal) ||
            state.CorrectionPropagationSourceVersion != command.SourceVersion ||
            string.IsNullOrWhiteSpace(state.PriorAssociationProjectId) ||
            string.IsNullOrWhiteSpace(state.CurrentAssociationProjectId) ||
            string.IsNullOrWhiteSpace(command.ResponsibleOwnerRole) ||
            string.IsNullOrWhiteSpace(command.NextSafeAction) ||
            string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return InvalidAssociationCorrection(command.AssociationId, "invalid_correction_propagation_delay");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxAssociationCorrectionPropagationDelayed(
                command.AssociationId,
                source.IntakeId,
                envelope.TenantId,
                source.SourceMailboxId,
                source.SourceConversationId,
                source.SourceThreadId,
                command.CorrectionId,
                command.WorkflowInstanceId,
                command.SourceVersion,
                state.PriorAssociationProjectId ?? string.Empty,
                state.CurrentAssociationProjectId ?? string.Empty,
                command.DelayedAtUtc.ToUniversalTime(),
                command.ResponsibleOwnerRole,
                command.NextSafeAction,
                command.ReasonCode,
                source.RedactionState,
                source.RetentionClass,
                command.SchemaVersion,
                envelope.CorrelationId),
        });
    }

    private static DomainResult Invalid(string? intakeId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxMessageIntakeInvalidRejection(intakeId, reasonCode),
        });

    private static DomainResult InvalidResolution(string? resolutionId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxParticipantResolutionInvalidRejection(resolutionId, reasonCode),
        });

    private static DomainResult InvalidAssociation(string? associationId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxAssociationInvalidRejection(associationId, reasonCode),
        });

    private static DomainResult InvalidAssociationDecision(string? associationId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxAssociationDecisionInvalidRejection(associationId, reasonCode),
        });

    private static DomainResult InvalidAssociationCorrection(string? associationId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxAssociationCorrectionInvalidRejection(associationId, reasonCode),
        });

    private static DomainResult InvalidThreshold(string? policyId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new AssociationThresholdPolicyInvalidRejection(policyId, reasonCode),
        });

    private static DomainResult InvalidRetry(string? retryId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new WorkflowRetryInvalidRejection(retryId, reasonCode),
        });

    private static bool IsValidScore(double score)
        => double.IsFinite(score) && score >= 0.0 && score <= 1.0;

    private static bool IsValidPropagationCommand(
        string? associationId,
        string? intakeId,
        string? correctionId,
        string? workflowInstanceId,
        long sourceVersion,
        string? schemaVersion)
        => AssociationWorkflowId.TryParse(associationId, out _) &&
            MailboxMessageIntakeId.TryParse(intakeId, out _) &&
            !string.IsNullOrWhiteSpace(correctionId) &&
            !string.IsNullOrWhiteSpace(workflowInstanceId) &&
            sourceVersion > 0 &&
            !string.IsNullOrWhiteSpace(schemaVersion);

    private static bool IsMetadataOnly(string redactionState, string retentionClass)
        => string.Equals(redactionState, "metadata_only", StringComparison.Ordinal) &&
            string.Equals(retentionClass, "collaboration_input", StringComparison.Ordinal);

    private static bool RoutesToReview(AssociationScoringResult result)
        => result.Outcome is AssociationScoringOutcome.CandidatesGenerated or AssociationScoringOutcome.FailedClosed;

    private static bool IsValidReviewTransition()
        => LifecycleTransitionValidator
            .Validate(new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.NeedsReview))
            .IsValid;

    private static bool IsAutoAssociatedButInvalid(
        ScoreMailboxMessageAssociation command,
        AssociationScoringResult result)
        => result.Outcome == AssociationScoringOutcome.AutoAssociated && !IsValidAutoAssociation(command, result);

    private static bool IsValidAutoAssociation(
        ScoreMailboxMessageAssociation command,
        AssociationScoringResult result)
    {
        if (command.ThresholdPolicy is null ||
            result.Outcome != AssociationScoringOutcome.AutoAssociated ||
            result.ThresholdBand != AssociationThresholdBand.Auto ||
            command.Candidates is not { Count: 1 })
        {
            return false;
        }

        AssociationCandidate candidate = command.Candidates[0];
        return candidate.RequiredEvidenceComplete &&
            IsValidScore(candidate.ConfidenceScore) &&
            candidate.ConfidenceScore >= command.ThresholdPolicy.THigh &&
            result.ConfidenceScore >= command.ThresholdPolicy.THigh &&
            Math.Abs(result.ConfidenceScore - candidate.ConfidenceScore) <= 0.000001 &&
            result.ReasonCodes.Contains(AssociationReasonCode.RequiredEvidencePresent);
    }

    private static bool IsConsistentAssociationResult(ScoreMailboxMessageAssociation command, CommandEnvelope envelope)
    {
        AssociationScoringResult result = command.Result!;
        return string.Equals(result.IntakeId, command.IntakeId, StringComparison.Ordinal) &&
            string.Equals(result.SourceMailboxId, command.SourceMailboxId, StringComparison.Ordinal) &&
            string.Equals(result.SourceConversationId, command.SourceConversationId, StringComparison.Ordinal) &&
            string.Equals(result.SourceThreadId, command.SourceThreadId, StringComparison.Ordinal) &&
            string.Equals(result.CorrelationId, envelope.CorrelationId, StringComparison.Ordinal) &&
            string.Equals(result.KernelVersion, command.ScoringKernelVersion, StringComparison.Ordinal) &&
            result.ReasonCodes is { Count: > 0 } &&
            !string.IsNullOrWhiteSpace(result.RedactionState) &&
            !string.IsNullOrWhiteSpace(result.RetentionClass) &&
            !string.IsNullOrWhiteSpace(result.SchemaVersion) &&
            result.DetectedAt != default;
    }

    private static AssociationDecisionValidation ValidateDecision(
        string associationId,
        string intakeId,
        AssociationDecisionKind actualKind,
        AssociationDecisionKind expectedKind,
        string candidateEvidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? decisionNote,
        GovernedOperationState? state)
    {
        if (!AssociationWorkflowId.TryParse(associationId, out _) ||
            !MailboxMessageIntakeId.TryParse(intakeId, out _) ||
            actualKind != expectedKind ||
            sourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(schemaVersion) ||
            string.IsNullOrWhiteSpace(candidateEvidenceFingerprint))
        {
            return AssociationDecisionValidation.Invalid("invalid_association_decision_payload");
        }

        AssociationDecisionSourceSnapshot? source = state?.AssociationDecisionSource;
        if (source is null ||
            !string.Equals(source.AssociationId, associationId, StringComparison.Ordinal) ||
            !string.Equals(source.IntakeId, intakeId, StringComparison.Ordinal))
        {
            return AssociationDecisionValidation.Invalid("missing_association_evidence");
        }

        if (state!.AssociationDecisionIds.Contains(associationId))
        {
            return AssociationDecisionValidation.Invalid("association_already_decided");
        }

        if (state.AssociationLifecycleState != LifecycleState.NeedsReview)
        {
            return AssociationDecisionValidation.Invalid("invalid_association_lifecycle_transition");
        }

        if (source.SourceVersion != sourceVersion)
        {
            return AssociationDecisionValidation.Invalid("stale_evidence");
        }

        if (!EvidenceFingerprintMatches(AllEvidenceRefs(source), candidateEvidenceFingerprint))
        {
            return AssociationDecisionValidation.Invalid("stale_evidence");
        }

        if (!TrySanitizeDecisionNote(decisionNote, out string? sanitizedNote))
        {
            return AssociationDecisionValidation.Invalid("invalid_decision_note");
        }

        return AssociationDecisionValidation.Valid(source, sanitizedNote);
    }

    private static IReadOnlyList<AssociationEvidenceReference> AllEvidenceRefs(AssociationDecisionSourceSnapshot source)
        => source.Candidates
            .SelectMany(static candidate => candidate.EvidenceRefs)
            .Concat(source.Exclusions.Select(static exclusion => new AssociationEvidenceReference(
                exclusion.EvidenceReference,
                exclusion.EvidenceFingerprint,
                "association-exclusion")))
            .GroupBy(static evidence => evidence.EvidenceFingerprint, StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToArray();

    private static bool EvidenceFingerprintMatches(
        IReadOnlyList<AssociationEvidenceReference> evidenceRefs,
        string fingerprint)
        => evidenceRefs.Any(evidence => string.Equals(evidence.EvidenceFingerprint, fingerprint, StringComparison.Ordinal));

    private static bool TrySanitizeDecisionNote(string? note, out string? sanitized)
    {
        sanitized = null;
        if (string.IsNullOrWhiteSpace(note))
        {
            return true;
        }

        string normalized = Regex.Replace(note.Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ");
        if (normalized.Length > 1024 ||
            normalized.Any(char.IsControl) ||
            ContainsUnsafeNoteMarker(normalized))
        {
            return false;
        }

        sanitized = normalized;
        return true;
    }

    private static bool ContainsUnsafeNoteMarker(string value)
    {
        string[] markers = ["secret", "bearer ", "raw-body", "provider payload", "sender@", "/home/", "C:\\"];
        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string ActorType(CommandEnvelope envelope, string fallback = "human")
        => envelope.Extensions is not null &&
            envelope.Extensions.TryGetValue("actorType", out string? actorType) &&
            !string.IsNullOrWhiteSpace(actorType)
                ? actorType
                : fallback;

    private static string SurfaceOrigin(CommandEnvelope envelope, string fallback = "api")
        => envelope.Extensions is not null &&
            envelope.Extensions.TryGetValue("surfaceOrigin", out string? origin) &&
            !string.IsNullOrWhiteSpace(origin)
                ? origin
                : fallback;

    private static DateTimeOffset DecisionTimestamp(CommandEnvelope envelope, DateTimeOffset fallback)
        => envelope.Extensions is not null &&
            envelope.Extensions.TryGetValue("decidedAt", out string? decidedAt) &&
            DateTimeOffset.TryParse(decidedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
                ? parsed.ToUniversalTime()
                : fallback.ToUniversalTime();

    private sealed record AssociationDecisionValidation(
        bool IsValid,
        string ReasonCode,
        AssociationDecisionSourceSnapshot? Source,
        string? SanitizedNote)
    {
        public static AssociationDecisionValidation Valid(AssociationDecisionSourceSnapshot source, string? sanitizedNote)
            => new(true, string.Empty, source, sanitizedNote);

        public static AssociationDecisionValidation Invalid(string reasonCode)
            => new(false, reasonCode, null, null);
    }
}
