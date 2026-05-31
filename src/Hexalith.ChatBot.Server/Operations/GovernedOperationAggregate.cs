using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

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
                        envelope.CorrelationId),
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

    private static DomainResult InvalidThreshold(string? policyId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new AssociationThresholdPolicyInvalidRejection(policyId, reasonCode),
        });

    private static bool IsValidScore(double score)
        => double.IsFinite(score) && score >= 0.0 && score <= 1.0;

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
}
