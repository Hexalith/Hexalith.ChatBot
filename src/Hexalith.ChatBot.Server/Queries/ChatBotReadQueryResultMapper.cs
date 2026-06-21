using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Queries;

internal static class ChatBotReadQueryResultMapper
{
    private static readonly JsonSerializerOptions ProjectConversationEtagJsonOptions = new(JsonSerializerDefaults.Web);

    public static IResult ProjectConversationHttpResult(HttpContext httpContext, ProjectConversationResponse response)
    {
        string etag = ProjectConversationEtagFor(response);
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = "private, no-cache";
        return RequestMatchesEtag(httpContext, etag)
            ? Results.StatusCode(StatusCodes.Status304NotModified)
            : Results.Ok(response);
    }

    public static string? EncodeNextCursor(ProjectConversationPage page, Func<ProjectConversationCursorPosition, string> encode)
        => page.NextCursorPosition is null ? null : encode(page.NextCursorPosition);

    public static ProjectConversationResponse BuildProjectConversationResponse(
        string projectId,
        string tenantId,
        ProjectConversationPage page,
        string? nextCursor,
        string requestCorrelationId,
        ProjectAiContextPackage? aiContextPackage)
    {
        ProjectConversationReadStatus status = ProjectConversationReadStatus.Empty;
        LifecycleState state = LifecycleState.Proposed;
        string? safeNextAction = "none";
        if (page.Items.Count > 0)
        {
            ProjectConversationItemView latest = page.LatestItem ?? ProjectConversationItemView.LatestOf(page.Items)!;
            state = latest.LifecycleState;
            status = latest.LifecycleState switch
            {
                LifecycleState.Correcting or LifecycleState.CorrectionDelayed => ProjectConversationReadStatus.Blocked,
                LifecycleState.Failed => ProjectConversationReadStatus.Degraded,
                LifecycleState.Corrected when latest.SafeNextAction is not null => ProjectConversationReadStatus.Stale,
                _ => ProjectConversationReadStatus.Current,
            };
            safeNextAction = latest.SafeNextAction ?? (status == ProjectConversationReadStatus.Current ? "none" : "review-status");
        }

        return new ProjectConversationResponse(
            projectId,
            page.Items.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item.ProjectDisplayName))?.ProjectDisplayName,
            // Story 10.6b: surface the requester's own (kebab) tenant id so the UI can join the tenant-scoped
            // project-conversation projection-changed SignalR group for AI response streaming re-query. Same-tenant
            // metadata only — never another tenant's identifier.
            tenantId,
            status,
            state,
            page.Items.Select(ToContractItem).ToArray(),
            new ProjectConversationCursorPage(nextCursor, page.HasMore, page.PageSize),
            AssociationCandidateView.MailboxSourceProvenance,
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-response.v1",
            requestCorrelationId,
            safeNextAction,
            aiContextPackage);
    }

    public static TaskIntentReview TaskIntentReviewUnavailable(string projectId, string taskIntentId, string reasonCode, string requestCorrelationId)
        => new(
            projectId,
            taskIntentId,
            Available: false,
            reasonCode,
            null,
            null,
            [],
            [],
            null,
            null,
            requestCorrelationId,
            "unavailable",
            "chatbot.task-intent-review.v1");

    public static TaskIntentReview BuildTaskIntentReview(
        TaskIntentRecord record,
        MailboxMessageContentResult source,
        string requestCorrelationId)
        => new(
            record.ProjectId,
            record.TaskIntentId,
            Available: true,
            record.ReasonCode,
            record,
            new TaskIntentReviewSourceMessage(
                record.SourceMessageId,
                source.Content ?? string.Empty,
                source.ContentType,
                source.RedactionState,
                record.SourceVersion.ToString(CultureInfo.InvariantCulture),
                record.SourceEvidenceOffsets.Select(static evidence => evidence.EvidenceReference).ToArray()),
            AvailableTransitionsFor(record),
            AuditHistoryFor(record),
            record.State,
            record.SourceVersion,
            string.IsNullOrWhiteSpace(record.CorrelationId) ? requestCorrelationId : record.CorrelationId,
            record.RedactionState,
            "chatbot.task-intent-review.v1");

    public static AssociationRoutingStatus BuildAssociationRoutingStatus(AssociationCandidateView view, string requestCorrelationId)
    {
        string[] disabledReasons = BuildAssociationDisabledReasons(view);
        string[] nextActions = BuildAssociationNextActionCodes(view);

        return new AssociationRoutingStatus(
            view.AssociationId,
            view.IntakeId,
            view.SourceMailboxId,
            view.SourceConversationId,
            view.SourceThreadId,
            view.LifecycleState,
            view.Outcome,
            view.ThresholdBand,
            view.ConfidenceScore,
            BuildAssociationReasonCodes(view),
            view.Candidates,
            view.Exclusions,
            view.ThresholdPolicyVersion,
            BuildAssociationEvidenceRefs(view),
            view.DerivationKernelVersion,
            view.DetectedAt,
            view.SourceProvenance,
            view.RedactionState,
            view.RetentionClass,
            "chatbot.association-routing-status.v1",
            view.SourceVersion,
            string.IsNullOrWhiteSpace(view.CorrelationId) ? requestCorrelationId : view.CorrelationId,
            disabledReasons,
            nextActions,
            view.DecisionKind,
            view.DecidedAt,
            view.DecisionActorId,
            view.DecisionActorType,
            view.DecisionNoteRedactionState,
            view.CorrectedProjectId,
            view.PriorProjectId,
            view.PredecessorAssociationId,
            view.SupersedesAssociationId,
            view.SupersededByAssociationId,
            view.CorrectionId,
            SupersedingCorrectionLinkFor(view),
            !string.IsNullOrWhiteSpace(view.SupersededByAssociationId) || !string.IsNullOrWhiteSpace(view.CorrectionId),
            view.CorrectionKind,
            view.CorrectedAt,
            view.CorrectionActorId,
            view.CorrectionActorType,
            view.CorrectionRationaleRedactionState,
            view.DownstreamImpactStatus,
            view.PropagationStatus,
            view.PropagationProgressNumerator,
            view.PropagationProgressDenominator,
            view.PropagationEstimatedCompletionAtUtc,
            view.IsCorrectedContextStale,
            view.ResponsibleOwnerRole,
            view.SafeNextAction,
            view.WorkflowInstanceId,
            view.RequiredStoreKeys,
            view.CompletedStoreKeys,
            view.FailedStoreKeys,
            view.ExternalSender,
            view.StrictnessPolicy,
            view.RoutingReason);
    }

    private static bool RequestMatchesEtag(HttpContext httpContext, string etag)
    {
        if (!httpContext.Request.Headers.TryGetValue("If-None-Match", out Microsoft.Extensions.Primitives.StringValues values))
        {
            return false;
        }

        return values
            .SelectMany(static value => value?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [])
            .Any(candidate => string.Equals(candidate, "*", StringComparison.Ordinal) || string.Equals(candidate, etag, StringComparison.Ordinal));
    }

    private static string ProjectConversationEtagFor(ProjectConversationResponse response)
    {
        ProjectConversationResponse stableResponse = response with
        {
            CorrelationId = string.Empty,
            AiContextPackage = response.AiContextPackage is null
                ? null
                : response.AiContextPackage with { CorrelationId = string.Empty },
        };
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(stableResponse, ProjectConversationEtagJsonOptions);
        return $"\"{Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant()}\"";
    }

    private static IReadOnlyList<TaskIntentAvailableTransition> AvailableTransitionsFor(TaskIntentRecord record)
    {
        if (record.State is TaskIntentState.Captured && !record.ConversionReadinessBlocked)
        {
            return
            [
                new("convert", "Convert to AI action proposal", Enabled: true),
                new("not-actionable", "Not actionable", Enabled: true),
                new("duplicate", "Duplicate", Enabled: true, RequiresPredecessorTaskIntentId: true),
                new("already-handled", "Already handled", Enabled: true),
                new("out-of-scope", "Out of scope", Enabled: true),
            ];
        }

        string reason = record.ConversionReadinessBlocked
            ? TaskIntentReasonCodes.StaleCorrectedContext
            : record.State is TaskIntentState.Converted
                ? TaskIntentReasonCodes.AlreadyConverted
                : TaskIntentReasonCodes.TerminalState;
        return
        [
            new("convert", "Convert to AI action proposal", Enabled: false, reason),
            new("not-actionable", "Not actionable", Enabled: false, reason),
            new("duplicate", "Duplicate", Enabled: false, reason, RequiresPredecessorTaskIntentId: true),
            new("already-handled", "Already handled", Enabled: false, reason),
            new("out-of-scope", "Out of scope", Enabled: false, reason),
        ];
    }

    private static IReadOnlyList<TaskIntentTransitionAuditSummary> AuditHistoryFor(TaskIntentRecord record)
        => string.IsNullOrWhiteSpace(record.AuditOperationId) ||
            string.IsNullOrWhiteSpace(record.ReviewerActorId) ||
            record.DecidedAtUtc is null
                ? []
                : [new TaskIntentTransitionAuditSummary(
                    record.AuditOperationId,
                    "recorded",
                    record.ReviewerActorId,
                    record.DecidedAtUtc.Value,
                    record.ReasonCode,
                    record.CorrelationId,
                    record.RedactionState)];

    private static ProjectConversationItem ToContractItem(ProjectConversationItemView item)
        => new(
            item.ItemId,
            item.Kind,
            item.ActorKind,
            item.ActorLabel,
            item.OccurredAt,
            item.LifecycleState,
            item.ThresholdBand,
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
            item.SourceProvenance,
            item.RedactionState,
            item.RetentionClass,
            item.SchemaVersion,
            item.SourceVersion,
            item.CorrelationId,
            item.ProjectId,
            item.ProjectDisplayName,
            item.DecisionLabel,
            item.SafeNextAction,
            item.ParticipantResolutionId,
            item.SourceParticipantId,
            item.PartyId,
            item.ParticipantStatus,
            item.ParticipantBlockedReason,
            item.ParticipantDisplayKind,
            item.ParticipantEvidenceReference,
            item.ParticipantEvidenceFingerprint,
            item.ParticipantAllowedReviewActions,
            item.ParticipantRedactionState,
            item.SourceProviderAttachmentId,
            item.AttachmentDisplayName,
            item.AttachmentContentType,
            item.AttachmentSizeInBytes,
            item.AttachmentCaptureStatus,
            item.AttachmentStorageStatus,
            item.AttachmentScanStatus,
            item.AttachmentFolderId,
            item.AttachmentFileId,
            item.AttachmentDuplicateState,
            item.AttachmentRetryState,
            item.AttachmentAiContextEligibility,
            item.AttachmentAllowedActions,
            item.AttachmentRedactionState,
            item.DecisionKind,
            item.DecisionActorId,
            item.DecisionActorType,
            item.DecidedAtUtc,
            item.DecisionNoteRedactionState,
            item.SurfaceOrigin,
            item.PolicySnapshotVersion,
            item.EvidenceReferenceSummary,
            item.CorrectionKind,
            item.PriorProjectId,
            item.CorrectedProjectId,
            item.PredecessorAssociationId,
            item.SupersedesAssociationId,
            item.SupersededByAssociationId,
            item.CorrectionRationaleRedactionState,
            item.CorrectionActorId,
            item.CorrectionActorType,
            item.CorrectedAtUtc,
            item.DownstreamImpactStatus,
            item.CorrectionId,
            item.WorkflowInstanceId,
            item.RequiredStoreKeys,
            item.CompletedStoreKeys,
            item.FailedStoreKeys,
            item.PropagationProgressNumerator,
            item.PropagationProgressDenominator,
            item.PropagationStartedAtUtc,
            item.PropagationCompletedAtUtc,
            item.PropagationEstimatedCompletionAtUtc,
            item.PropagationStatus,
            item.IsCorrectedContextStale,
            item.ResponsibleOwnerRole,
            item.ApprovalId,
            item.ApprovalEventKind,
            item.ApprovalStatus,
            item.ApprovalDecisionKind,
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
            item.ApprovalRiskClass,
            item.ApprovalRiskActionClasses,
            item.ApprovalAiRiskClass,
            item.ApprovalAiRiskActionClasses,
            item.ApprovalAiRiskInputTuple,
            item.ApprovalPolicySnapshotId,
            item.ApprovalPolicySnapshotVisibility,
            item.ApprovalEvidenceReferences,
            item.ApprovalEvidenceFreshnessStates,
            item.ApprovalAffectedResourceReferences,
            item.ApprovalRecipientReferences,
            item.ApprovalSenderAuthorityClass,
            item.ApprovalExpectedPostStateRedactionState,
            item.ApprovalActionSummaryRedactionState,
            item.ApprovalDecisionRationaleRedactionState,
            item.ApprovalAuthorityResult,
            item.ApprovalDisabledReason,
            item.ApprovalAuditOperationId,
            item.ApprovalAuditStatus,
            item.ApprovalCommandOutcomeStatus,
            item.ApprovalProjectedOutcomeItemId,
            item.ApprovalFailureCode,
            item.ApprovalRetryability,
            item.SupersedesApprovalId,
            item.SupersededByApprovalId,
            item.FailureStateKind,
            item.FailureStatus,
            item.MessageCatalogCode,
            item.MessageCatalogVersion,
            item.MessageDetailVisibility,
            item.FailureCategory,
            item.FailureScope,
            item.FailureReasonCode,
            item.BlockedReason,
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
            item.AiOutcomeKind,
            item.AiOutcomeStatus,
            item.AiActorId,
            item.AiActorType,
            item.AiProposalId,
            item.AiRequestId,
            item.AiRequesterId,
            item.AiSourceConversationItemId,
            item.AiSourceMessageId,
            item.AiOperationId,
            item.AiCorrelationId,
            item.AiRiskClass,
            item.AiRiskActionClasses,
            item.AiPolicyReasonCode,
            item.AiClassifierVersion,
            item.AiRiskInputTuple,
            item.AiRequesterAuthorityClass,
            item.AiIndeterminateReason,
            item.AiPolicySnapshotId,
            item.AiPolicySnapshotVisibility,
            item.AiContextPackageId,
            item.AiContextPackageVersion,
            item.AiContextRedactionState,
            item.AiAuthorizedContextReferences,
            item.AiExcludedContextReasons,
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
            item.BuildAiResponseProgress(),
            item.BuildStatusSummary(),
            item.BuildClassification(),
            item.BuildDetectedIntent(),
            item.BuildAiSummaryProvenance(),
            item.BuildReviewHistory(),
            item.Authenticity,
            item.DelegatedSender,
            item.ExternalSender);

    private static IReadOnlyList<AssociationReasonCode> BuildAssociationReasonCodes(AssociationCandidateView view)
    {
        AssociationReasonCode[] fromCandidates = view.Candidates
            .SelectMany(static candidate => candidate.ReasonCodes)
            .ToArray();
        AssociationReasonCode[] fromExclusions = view.Exclusions
            .Select(static exclusion => exclusion.ReasonCode)
            .ToArray();

        return fromCandidates
            .Concat(fromExclusions)
            .DefaultIfEmpty(AssociationReasonCode.NoAuthorizedCandidate)
            .Distinct()
            .ToArray();
    }

    private static IReadOnlyList<AssociationEvidenceReference> BuildAssociationEvidenceRefs(AssociationCandidateView view)
    {
        Dictionary<string, AssociationConfidenceInput> confidenceByReference = view.Candidates
            .SelectMany(static candidate => candidate.ConfidenceInputs)
            .GroupBy(static input => input.EvidenceReference, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        AssociationEvidenceReference[] candidateRefs = view.Candidates
            .SelectMany(static candidate => candidate.EvidenceRefs)
            .Select(reference => EnrichAssociationEvidence(reference, confidenceByReference))
            .ToArray();
        AssociationEvidenceReference[] exclusionRefs = view.Exclusions
            .Select(static exclusion => new AssociationEvidenceReference(
                exclusion.EvidenceReference,
                exclusion.EvidenceFingerprint,
                exclusion.State.ToString(),
                VisibilityState: "redacted",
                RedactionState: "redacted",
                FreshnessState: exclusion.State is AssociationExclusionState.Stale ? "stale" : "unavailable"))
            .ToArray();

        return candidateRefs
            .Concat(exclusionRefs)
            .GroupBy(static evidence => evidence.EvidenceReference, StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToArray();
    }

    private static AssociationEvidenceReference EnrichAssociationEvidence(
        AssociationEvidenceReference reference,
        IReadOnlyDictionary<string, AssociationConfidenceInput> confidenceByReference)
    {
        if (!confidenceByReference.TryGetValue(reference.EvidenceReference, out AssociationConfidenceInput? input))
        {
            return reference with
            {
                MatchedValueDisplayToken = reference.MatchedValueDisplayToken ?? SafeEvidenceDisplayToken(reference.EvidenceReference),
                VisibilityState = reference.VisibilityState ?? "available",
                RedactionState = reference.RedactionState ?? "metadata_only",
                FreshnessState = reference.FreshnessState ?? "fresh",
            };
        }

        return reference with
        {
            SignalClass = reference.SignalClass ?? SignalClassWireValue(input.SignalClass),
            MatchedValueDisplayToken = reference.MatchedValueDisplayToken ?? SafeEvidenceDisplayToken(reference.EvidenceReference),
            VisibilityState = reference.VisibilityState ?? "available",
            RedactionState = reference.RedactionState ?? "metadata_only",
            FreshnessState = reference.FreshnessState ?? "fresh",
            ConfidenceContribution = reference.ConfidenceContribution ?? input.Weight,
        };
    }

    private static string? SupersedingCorrectionLinkFor(AssociationCandidateView view)
        => string.IsNullOrWhiteSpace(view.SupersededByAssociationId)
            ? null
            : $"association:{view.SupersededByAssociationId}";

    private static string SafeEvidenceDisplayToken(string evidenceReference)
    {
        int separator = evidenceReference.IndexOf(':', StringComparison.Ordinal);
        return separator <= 0 ? "evidence-reference" : $"{evidenceReference[..separator]}:metadata";
    }

    private static string SignalClassWireValue(AssociationSignalClass signalClass)
        => signalClass switch
        {
            AssociationSignalClass.ExplicitProjectIdentifier => "explicit-project-identifier",
            AssociationSignalClass.MailboxRoutingRule => "mailbox-routing-rule",
            AssociationSignalClass.ConversationThreadIdentifier => "conversation-thread-identifier",
            AssociationSignalClass.HumanSelection => "human-selection",
            AssociationSignalClass.Correction => "correction",
            _ => signalClass.ToString(),
        };

    private static string[] BuildAssociationDisabledReasons(AssociationCandidateView view)
    {
        List<string> reasons = [];

        if (view.Candidates.Count == 0)
        {
            reasons.Add("candidate-required");
        }

        if (view.LifecycleState is LifecycleState.Rejected or LifecycleState.Failed or LifecycleState.Skipped)
        {
            reasons.Add("terminal-state");
        }

        if (view.LifecycleState is LifecycleState.Correcting or LifecycleState.CorrectionDelayed ||
            string.Equals(view.PropagationStatus, Association.CorrectionPropagationStatuses.Pending, StringComparison.Ordinal) ||
            string.Equals(view.PropagationStatus, Association.CorrectionPropagationStatuses.Correcting, StringComparison.Ordinal) ||
            view.IsCorrectedContextStale)
        {
            reasons.Add("projection-pending");
            reasons.Add("corrected-context-stale");
        }

        if (view.LifecycleState is LifecycleState.CorrectionDelayed ||
            string.Equals(view.PropagationStatus, "delayed", StringComparison.Ordinal))
        {
            reasons.Add("correction-delayed");
        }

        if (view.Exclusions.Any(static exclusion => exclusion.State is AssociationExclusionState.Unauthorized))
        {
            reasons.Add("not-authorized");
        }

        return reasons.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string[] BuildAssociationNextActionCodes(AssociationCandidateView view)
        => view switch
        {
            { LifecycleState: LifecycleState.Correcting } => [ChatBotMessageCodes.AssociationCorrectionPropagationPending],
            { LifecycleState: LifecycleState.CorrectionDelayed } => [ChatBotMessageCodes.AssociationCorrectionPropagationDelayed],
            { Candidates.Count: 0, LifecycleState: LifecycleState.Failed } => [ChatBotMessageCodes.AssociationScorerFailedClosed],
            { Candidates.Count: 0 } => [ChatBotMessageCodes.AssociationContextUnavailable],
            { Exclusions.Count: > 0 } => [ChatBotMessageCodes.AssociationCandidateSuppressed],
            { DownstreamImpactStatus: "complete" } => [ChatBotMessageCodes.AssociationCorrectionPropagationComplete],
            _ => [ChatBotMessageCodes.AssociationAmbiguousRouted],
        };
}
