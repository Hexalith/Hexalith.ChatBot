using System.Reflection;
using System.Runtime.Serialization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.State.ProjectConversation;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// UI-owned S1 project conversation read service. Reads only through <see cref="IChatBotClient"/>.
/// </summary>
public sealed class ProjectConversationService(IChatBotClient client)
{
    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<ProjectConversationModel> GetProjectConversationAsync(
        string projectId,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        ProjectConversationResponse response = await _client
            .GetProjectConversationAsync(projectId, cursor, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ProjectConversationModel(
            response.ProjectId,
            string.IsNullOrWhiteSpace(response.ProjectDisplayName) ? response.ProjectId : response.ProjectDisplayName,
            response.TenantContext,
            response.Status.ToString(),
            response.ConversationState.ToString(),
            response.Items.Select(MapItem).ToArray(),
            response.Page.NextCursor,
            response.Page.HasMore,
            response.Page.PageSize,
            response.SourceProvenance.ToString(),
            response.RedactionState.ToString(),
            response.RetentionClass.ToString(),
            response.SchemaVersion.ToString(),
            response.CorrelationId,
            string.IsNullOrWhiteSpace(response.SafeNextAction) ? "none" : response.SafeNextAction);
    }

    public async Task<ProjectAssociationWhyPanelModel> GetAssociationWhyPanelAsync(
        string projectId,
        string associationId,
        CancellationToken cancellationToken = default)
    {
        AssociationRoutingStatus status = await _client
            .GetAssociationRoutingStatusAsync(associationId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ProjectAssociationWhyPanelModel(
            projectId,
            status.AssociationId,
            status.IntakeId,
            status.SourceMailboxId,
            status.SourceConversationId,
            status.SourceThreadId,
            status.LifecycleState.ToString(),
            status.Outcome.ToString(),
            status.ThresholdBand.ToString(),
            status.ConfidenceScore,
            status.ThresholdPolicyVersion,
            status.KernelVersion,
            status.DecidedAt ?? status.CorrectedAt ?? status.DetectedAt,
            status.DecisionActorId ?? status.CorrectionActorId,
            status.DecisionActorType ?? status.CorrectionActorType,
            status.SourceProvenance.ToString(),
            status.RedactionState.ToString(),
            status.SchemaVersion,
            status.SourceVersion,
            status.CorrelationId,
            status.ReasonCodes.Select(WireToken).ToArray(),
            status.EvidenceRefs.Select(MapWhyEvidence).ToArray(),
            status.PriorProjectId,
            status.CorrectedProjectId,
            status.PredecessorAssociationId,
            status.SupersedesAssociationId,
            status.SupersededByAssociationId,
            status.SupersedingCorrectionId,
            status.SupersedingCorrectionLink,
            status.CorrectionPanelAvailable ?? false,
            status.PropagationStatus,
            status.DownstreamImpactStatus,
            status.IsCorrectedContextStale ?? false,
            string.IsNullOrWhiteSpace(status.SafeNextAction) ? "none" : status.SafeNextAction);
    }

    public async Task<TaskIntentReviewModel> GetTaskIntentReviewAsync(
        string projectId,
        string taskIntentId,
        CancellationToken cancellationToken = default)
    {
        TaskIntentReview review = await _client
            .GetTaskIntentReviewAsync(projectId, taskIntentId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new TaskIntentReviewModel(
            review.ProjectId,
            review.TaskIntentId,
            review.Available,
            review.ReasonCode,
            review.SourceMessage?.Content,
            review.SourceMessage?.ContentType,
            review.AvailableTransitions
                .Select(static transition => new TaskIntentAvailableTransitionModel(
                    transition.Transition,
                    transition.Label,
                    transition.Enabled,
                    transition.DisabledReasonCode,
                    transition.RequiresPredecessorTaskIntentId ?? false))
                .ToArray(),
            review.AuditHistory
                .Select(static audit => new TaskIntentTransitionAuditSummaryModel(
                    audit.OperationId,
                    audit.Status,
                    audit.ActorId,
                    audit.DecidedAtUtc,
                    audit.ReasonCode,
                    audit.CorrelationId,
                    audit.RedactionState.ToString()))
                .ToArray(),
            review.CurrentState?.ToString(),
            review.SourceVersion,
            review.CorrelationId,
            review.RedactionState.ToString(),
            review.SchemaVersion);
    }

    private static ProjectConversationItemModel MapItem(ProjectConversationItem item)
        => new(
            item.ItemId,
            item.Kind.ToString(),
            item.ActorKind.ToString(),
            item.ActorLabel,
            item.OccurredAt,
            item.LifecycleState.ToString(),
            item.ThresholdBand.ToString(),
            item.ConfidenceScore,
            item.AssociationId,
            item.SourceMailboxId,
            item.SourceProviderMessageId,
            item.InternetMessageId,
            item.SourceConversationId,
            item.SourceThreadId,
            item.SourceReceivedAtUtc,
            item.SourceSentAtUtc,
            item.SourceCreatedAtUtc,
            item.SourceTimezone,
            item.SourceProvenanceDisplayToken,
            item.SourceProvenance.ToString(),
            item.RedactionState.ToString(),
            item.RetentionClass.ToString(),
            item.SchemaVersion.ToString(),
            item.SourceVersion,
            item.CorrelationId,
            item.ProjectId,
            item.ProjectDisplayName,
            item.DecisionLabel,
            item.SafeNextAction,
            item.ParticipantResolutionId,
            item.SourceParticipantId,
            item.PartyId,
            item.ParticipantStatus?.ToString(),
            item.ParticipantBlockedReason?.ToString(),
            item.ParticipantDisplayKind?.ToString(),
            item.ParticipantEvidenceReference,
            item.ParticipantEvidenceFingerprint,
            item.ParticipantAllowedReviewActions?.Select(static action => action.ToString()).ToArray() ?? [],
            item.ParticipantRedactionState?.ToString(),
            item.SourceProviderAttachmentId,
            item.AttachmentDisplayName,
            item.AttachmentContentType,
            item.AttachmentSizeInBytes,
            item.AttachmentCaptureStatus?.ToString(),
            item.AttachmentStorageStatus?.ToString(),
            item.AttachmentScanStatus?.ToString(),
            item.AttachmentFolderId,
            item.AttachmentFileId,
            item.AttachmentDuplicateState,
            item.AttachmentRetryState,
            item.AttachmentAiContextEligibility,
            item.AttachmentAllowedActions?.ToArray() ?? [],
            item.AttachmentRedactionState?.ToString(),
            WireToken(item.DecisionKind),
            item.DecisionActorId,
            item.DecisionActorType,
            item.DecidedAtUtc,
            WireToken(item.DecisionNoteRedactionState),
            item.SurfaceOrigin,
            item.PolicySnapshotVersion,
            item.EvidenceReferenceSummary?.ToArray() ?? [],
            WireToken(item.CorrectionKind),
            item.PriorProjectId,
            item.CorrectedProjectId,
            item.PredecessorAssociationId,
            item.SupersedesAssociationId,
            item.SupersededByAssociationId,
            WireToken(item.CorrectionRationaleRedactionState),
            item.CorrectionActorId,
            item.CorrectionActorType,
            item.CorrectedAtUtc,
            item.DownstreamImpactStatus,
            item.CorrectionId,
            item.WorkflowInstanceId,
            item.RequiredStoreKeys?.ToArray() ?? [],
            item.CompletedStoreKeys?.ToArray() ?? [],
            item.FailedStoreKeys?.ToArray() ?? [],
            item.PropagationProgressNumerator,
            item.PropagationProgressDenominator,
            item.PropagationStartedAtUtc,
            item.PropagationCompletedAtUtc,
            item.PropagationEstimatedCompletionAtUtc,
            item.PropagationStatus,
            item.IsCorrectedContextStale,
            item.ResponsibleOwnerRole,
            item.ApprovalId,
            WireToken(item.ApprovalEventKind),
            WireToken(item.ApprovalStatus),
            WireToken(item.ApprovalDecisionKind),
            item.ApprovalRequesterId,
            item.ApprovalRequesterActorType,
            item.ApprovalRequestedAtUtc,
            item.ApprovalDecisionActorId,
            item.ApprovalDecisionActorType,
            item.ApprovalDecidedAtUtc,
            item.ApprovalOutcomeAtUtc,
            item.ApprovalProposalId,
            item.ApprovalSourceMessageId,
            item.ApprovalSourceConversationItemId,
            item.ApprovalCommandName,
            item.ApprovalCommandAllowlistVersion,
            WireToken(item.ApprovalRiskClass),
            item.ApprovalRiskActionClasses?.ToArray() ?? [],
            item.ApprovalPolicySnapshotId,
            WireToken(item.ApprovalPolicySnapshotVisibility),
            item.ApprovalEvidenceReferences?.ToArray() ?? [],
            item.ApprovalEvidenceFreshnessStates?.Select(WireToken).ToArray() ?? [],
            item.ApprovalAffectedResourceReferences?.ToArray() ?? [],
            item.ApprovalRecipientReferences?.ToArray() ?? [],
            item.ApprovalSenderAuthorityClass,
            WireToken(item.ApprovalExpectedPostStateRedactionState),
            WireToken(item.ApprovalActionSummaryRedactionState),
            WireToken(item.ApprovalDecisionRationaleRedactionState),
            item.ApprovalAuthorityResult,
            WireToken(item.ApprovalDisabledReason),
            item.ApprovalAuditOperationId,
            item.ApprovalAuditStatus,
            item.ApprovalCommandOutcomeStatus,
            item.ApprovalProjectedOutcomeItemId,
            item.ApprovalFailureCode,
            item.ApprovalRetryability,
            item.SupersedesApprovalId,
            item.SupersededByApprovalId,
            WireToken(item.FailureStateKind),
            WireToken(item.FailureStatus),
            WireToken(item.MessageCatalogCode),
            WireToken(item.MessageCatalogVersion),
            WireToken(item.MessageDetailVisibility),
            item.FailureCategory,
            item.FailureScope,
            item.FailureReasonCode,
            WireToken(item.BlockedReason),
            item.Retryable,
            item.RetryCount,
            item.MaxRetryCount,
            item.NextRetryAtUtc,
            item.LastRetryAtUtc,
            item.RetryOperationId,
            item.SupersedesWorkflowInstanceId,
            item.SupersededByWorkflowInstanceId,
            item.TaskId,
            item.OperationId,
            item.AuditOperationId,
            item.AuditStatus,
            item.ClientAction,
            item.DuplicateSafetyState,
            item.DuplicateSuppressionId,
            item.DependencyName,
            item.DegradedUntilUtc,
            item.EscalationTargetRole,
            item.ReprocessCreatedWorkflowInstanceId,
            WireToken(item.AiOutcomeKind),
            WireToken(item.AiOutcomeStatus),
            item.AiActorId,
            item.AiActorType,
            item.AiProposalId,
            item.AiRequestId,
            item.AiRequesterId,
            item.AiSourceConversationItemId,
            item.AiSourceMessageId,
            item.AiOperationId,
            item.AiCorrelationId,
            WireToken(item.AiRiskClass),
            item.AiRiskActionClasses?.ToArray() ?? [],
            item.AiPolicySnapshotId,
            item.AiPolicySnapshotVisibility,
            item.AiContextPackageId,
            item.AiContextPackageVersion,
            item.AiContextRedactionState,
            item.AiAuthorizedContextReferences?.ToArray() ?? [],
            item.AiExcludedContextReasons?.ToArray() ?? [],
            item.AiGeneratedSummaryRedactionState,
            item.AiGeneratedContentVisibility,
            item.AiCommandName,
            item.AiCommandAllowlistVersion,
            item.AiApprovalId,
            item.AiApprovalStatus,
            item.AiExecutionStatus,
            item.AiExecutionOutcomeCode,
            item.AiAuditOperationId,
            item.AiAuditStatus,
            item.AiFailureCode,
            item.AiRetryability,
            item.AiSafeNextAction,
            item.SupersedesAiOutcomeId,
            item.SupersededByAiOutcomeId,
            MapStatusSummary(item.StatusSummary),
            MapClassification(item.Classification),
            MapDetectedIntent(item.DetectedIntent),
            MapAiSummaryProvenance(item.AiSummaryProvenance),
            item.ReviewHistory?.Select(MapReviewHistoryEntry).ToArray() ?? []);

    private static ProjectConversationItemStatusSummaryModel MapStatusSummary(ProjectConversationItemStatusSummary? summary)
        => new((summary?.Facets ?? [])
            .Select(static facet => new ProjectConversationItemStatusFacetModel(
                WireToken(facet.Domain),
                WireToken(facet.Health),
                facet.SourceState,
                facet.MessageCode,
                facet.SafeNextAction,
                facet.SafeMetadataIds is null
                    ? new Dictionary<string, string>(StringComparer.Ordinal)
                    : new Dictionary<string, string>(facet.SafeMetadataIds, StringComparer.Ordinal),
                facet.NotApplicable ?? false,
                facet.OperationId,
                facet.CompletionStatus,
                facet.ProjectionStatus,
                facet.AuditStatus,
                facet.CorrelationId,
                facet.RetryCount,
                facet.TerminalReasonCode,
                facet.ResponsibleOwnerRole,
                facet.DuplicateSafetyState))
            .ToArray());

    private static ProjectConversationItemClassificationModel? MapClassification(ProjectConversationItemClassification? classification)
        => classification is null
            ? null
            : new ProjectConversationItemClassificationModel(
                WireToken(classification.Kind),
                classification.KernelVersion,
                classification.ConfidenceScore,
                classification.MessageCode,
                classification.SourceEvidenceIds?.ToArray() ?? [],
                WireToken(classification.RedactionState));

    private static ProjectConversationDetectedIntentModel? MapDetectedIntent(ProjectConversationDetectedIntent? intent)
        => intent is null
            ? null
            : new ProjectConversationDetectedIntentModel(
                intent.Summary,
                WireToken(intent.ActionKind),
                intent.SourceEvidenceIds?.ToArray() ?? [],
                intent.SafeNextAction,
                intent.MessageCode,
                WireToken(intent.RedactionState));

    private static ProjectConversationAiSummaryProvenanceModel? MapAiSummaryProvenance(ProjectConversationAiSummaryProvenance? provenance)
        => provenance is null
            ? null
            : new ProjectConversationAiSummaryProvenanceModel(
                provenance.GeneratedBy,
                provenance.GeneratedAtUtc,
                provenance.SourceEvidenceIds?.ToArray() ?? [],
                provenance.ContextPackageId,
                provenance.ContextPackageVersion,
                WireToken(provenance.RedactionState));

    private static ProjectConversationReviewHistoryEntryModel MapReviewHistoryEntry(ProjectConversationReviewHistoryEntry entry)
        => new(
            entry.ReviewedResourceKind,
            entry.ReviewedResourceId,
            entry.ActionCode,
            entry.DecisionCode,
            entry.ActorKind,
            entry.ActorLabel,
            entry.ReviewedAtUtc,
            entry.SurfaceOrigin,
            entry.CorrelationId,
            entry.OperationId,
            WireToken(entry.RedactionState),
            entry.ReasonCode);

    private static ProjectAssociationWhyEvidenceModel MapWhyEvidence(AssociationEvidenceReference evidence)
        => new(
            evidence.EvidenceKind,
            WireToken(evidence.SignalClass),
            string.IsNullOrWhiteSpace(evidence.MatchedValueDisplayToken) ? evidence.EvidenceReference : evidence.MatchedValueDisplayToken,
            evidence.EvidenceFingerprint,
            evidence.EvidenceReference,
            WireToken(evidence.VisibilityState) ?? "available",
            WireToken(evidence.RedactionState) ?? "metadata_only",
            WireToken(evidence.FreshnessState) ?? "fresh",
            evidence.ConfidenceContribution);

    private static string? WireToken<TEnum>(TEnum? value)
        where TEnum : struct, Enum
        => value is null ? null : WireToken(value.Value);

    private static string WireToken<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string? memberName = Enum.GetName(value);
        if (memberName is null)
        {
            return value.ToString();
        }

        MemberInfo? member = typeof(TEnum).GetMember(memberName).FirstOrDefault();
        return member?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? value.ToString();
    }
}
