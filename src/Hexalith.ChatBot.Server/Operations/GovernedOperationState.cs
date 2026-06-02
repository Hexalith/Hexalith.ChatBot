using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.Policy;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Replayed state for the governed note aggregate. Reconstructed by applying the aggregate's events
/// in order; never mutated directly. Reference type with a parameterless constructor as required by
/// <see cref="Hexalith.EventStore.Client.Aggregates.EventStoreAggregate{TState}"/>.
/// </summary>
public sealed class GovernedOperationState
{
    private readonly HashSet<string> _participantResolutionIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _associationIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _associationDecisionIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _associationCorrectionIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CorrectionPropagationStoreAcknowledgement> _correctionPropagationStores = new(StringComparer.Ordinal);
    private readonly HashSet<string> _correctionPropagationRequiredStores = new(StringComparer.Ordinal);
    private readonly HashSet<string> _thresholdPolicyVersions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _workflowRetryIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _lowRiskAiExecutionIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ApprovedAiActionExecutionStarted> _approvedAiExecutions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutboundDraftCreated> _outboundDrafts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutboundApprovalRequested> _outboundApprovalRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutboundApprovalDecisionRecorded> _outboundApprovalDecisions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutboundSendStarted> _outboundSends = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActionProposalRecord> _aiActionProposals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActionProposalInvalidatedByCorrection> _invalidatedAiActionProposals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActionApprovalRequested> _approvalRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActionApprovalDecisionRecorded> _approvalDecisions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TaskIntentRecord> _taskIntents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _taskIntentTransitionIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TenantPolicyChangePendingApproval> _tenantPolicyPendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TenantPolicySnapshotActivated> _tenantPolicySnapshots = new(StringComparer.Ordinal);
    private double _associationTHigh = AssociationThresholdPolicySnapshot.DefaultM0High;
    private double _associationTLow = AssociationThresholdPolicySnapshot.DefaultM0Low;
    private string _associationThresholdPolicyVersion = AssociationThresholdPolicySnapshot.DefaultM0.PolicyVersion;

    /// <summary>
    /// Gets a value indicating whether a governed note has already been recorded for this aggregate.
    /// </summary>
    public bool IsRecorded { get; private set; }

    /// <summary>
    /// Gets the ULID of the recorded governed note, or <see langword="null"/> before recording.
    /// </summary>
    public string? NoteId { get; private set; }

    public bool IsMailboxIntakeCaptured { get; private set; }

    public string? MailboxIntakeId { get; private set; }

    public IReadOnlySet<string> ParticipantResolutionIds => _participantResolutionIds;

    public IReadOnlySet<string> AssociationIds => _associationIds;

    public IReadOnlySet<string> AssociationDecisionIds => _associationDecisionIds;

    public IReadOnlySet<string> AssociationCorrectionIds => _associationCorrectionIds;

    public IReadOnlySet<string> WorkflowRetryIds => _workflowRetryIds;

    public IReadOnlySet<string> LowRiskAiExecutionIds => _lowRiskAiExecutionIds;

    public IReadOnlyDictionary<string, ApprovedAiActionExecutionStarted> ApprovedAiExecutions => _approvedAiExecutions;

    public IReadOnlyDictionary<string, OutboundDraftCreated> OutboundDrafts => _outboundDrafts;

    public IReadOnlyDictionary<string, OutboundApprovalRequested> OutboundApprovalRequests => _outboundApprovalRequests;

    public IReadOnlyDictionary<string, OutboundApprovalDecisionRecorded> OutboundApprovalDecisions => _outboundApprovalDecisions;

    public IReadOnlyDictionary<string, OutboundSendStarted> OutboundSends => _outboundSends;

    public IReadOnlyDictionary<string, AiActionProposalRecord> AiActionProposals => _aiActionProposals;

    public IReadOnlyDictionary<string, AiActionProposalInvalidatedByCorrection> InvalidatedAiActionProposals => _invalidatedAiActionProposals;

    public IReadOnlyDictionary<string, AiActionApprovalRequested> ApprovalRequests => _approvalRequests;

    public IReadOnlyDictionary<string, AiActionApprovalDecisionRecorded> ApprovalDecisions => _approvalDecisions;

    public IReadOnlySet<string> TaskIntentIds => _taskIntents.Keys.ToHashSet(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, TaskIntentRecord> TaskIntents => _taskIntents;

    public IReadOnlyDictionary<string, string> TaskIntentTransitionIds => _taskIntentTransitionIds;

    public AssociationDecisionSourceSnapshot? AssociationDecisionSource { get; private set; }

    public IReadOnlyDictionary<string, TenantPolicyChangePendingApproval> TenantPolicyPendingApprovals => _tenantPolicyPendingApprovals;

    public IReadOnlyDictionary<string, TenantPolicySnapshotActivated> TenantPolicySnapshots => _tenantPolicySnapshots;

    public long? LastAssociationDecisionSourceVersion { get; private set; }

    public LifecycleState? AssociationLifecycleState { get; private set; }

    public string? CurrentAssociationProjectId { get; private set; }

    public string? CurrentAssociationProjectDisplayName { get; private set; }

    public string? PriorAssociationProjectId { get; private set; }

    public string? PredecessorAssociationId { get; private set; }

    public string? SupersedesAssociationId { get; private set; }

    public string? CorrectionPropagationCorrectionId { get; private set; }

    public string? CorrectionPropagationWorkflowInstanceId { get; private set; }

    public long? CorrectionPropagationSourceVersion { get; private set; }

    public DateTimeOffset? CorrectionPropagationStartedAtUtc { get; private set; }

    public DateTimeOffset? CorrectionPropagationCompletedAtUtc { get; private set; }

    public DateTimeOffset? CorrectionPropagationEstimatedCompletionAtUtc { get; private set; }

    public bool IsCorrectionPropagationDelayed { get; private set; }

    public string? CorrectionPropagationResponsibleOwnerRole { get; private set; }

    public string? CorrectionPropagationNextSafeAction { get; private set; }

    public IReadOnlyDictionary<string, CorrectionPropagationStoreAcknowledgement> CorrectionPropagationStores => _correctionPropagationStores;

    public IReadOnlySet<string> CorrectionPropagationRequiredStores => _correctionPropagationRequiredStores;

    public int CorrectionPropagationCompletedStoreCount => _correctionPropagationStores.Values.Count(static ack => ack.IsSuccessful);

    public int CorrectionPropagationRequiredStoreCount => _correctionPropagationRequiredStores.Count;

    public IReadOnlySet<string> ThresholdPolicyVersions => _thresholdPolicyVersions;

    public double AssociationTHigh => _associationTHigh;

    public double AssociationTLow => _associationTLow;

    public string AssociationThresholdPolicyVersion => _associationThresholdPolicyVersion;

    /// <summary>
    /// Applies the recorded-note event. Idempotent on replay: a duplicate event leaves state unchanged.
    /// </summary>
    /// <param name="e">The recorded-note event.</param>
    public void Apply(GovernedNoteRecorded e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (IsRecorded)
        {
            return;
        }

        IsRecorded = true;
        NoteId = e.NoteId;
    }

    public void Apply(MailboxMessageIntakeCaptured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (IsMailboxIntakeCaptured)
        {
            return;
        }

        IsMailboxIntakeCaptured = true;
        MailboxIntakeId = e.IntakeId;
    }

    public void Apply(WorkflowRetryRequested e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _workflowRetryIds.Add(e.RetryId);
    }

    public void Apply(TaskIntentCaptured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        UpsertTaskIntent(e.Record);
    }

    public void Apply(TaskIntentConvertedToAiActionProposal e)
    {
        ArgumentNullException.ThrowIfNull(e);
        UpsertTaskIntent(e.TaskIntent);
        _aiActionProposals[e.Proposal.ProposalId] = e.Proposal;
        if (!string.IsNullOrWhiteSpace(e.TaskIntent.TransitionId))
        {
            _taskIntentTransitionIds[e.TaskIntent.TransitionId] = e.TaskIntent.TaskIntentId;
        }
    }

    public void Apply(AiActionProposalInvalidatedByCorrection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_invalidatedAiActionProposals.TryGetValue(e.ProposalId, out AiActionProposalInvalidatedByCorrection? existing) ||
            e.EvidenceSnapshotSourceVersion >= existing.EvidenceSnapshotSourceVersion)
        {
            _invalidatedAiActionProposals[e.ProposalId] = e;
        }
    }

    public void Apply(TaskIntentDispositionMarked e)
    {
        ArgumentNullException.ThrowIfNull(e);
        UpsertTaskIntent(e.TaskIntent);
        if (!string.IsNullOrWhiteSpace(e.TaskIntent.TransitionId))
        {
            _taskIntentTransitionIds[e.TaskIntent.TransitionId] = e.TaskIntent.TaskIntentId;
        }
    }

    public void Apply(LowRiskAiAssistanceExecutionStarted e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _lowRiskAiExecutionIds.Add(e.ExecutionId);
    }

    public void Apply(LowRiskAiAssistanceRoutedToApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _lowRiskAiExecutionIds.Add(e.Record.ExecutionId);
    }

    public void Apply(ApprovedAiActionExecutionStarted e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_approvedAiExecutions.ContainsKey(e.ExecutionId))
        {
            _approvedAiExecutions[e.ExecutionId] = e;
        }
    }

    public void Apply(OutboundDraftCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_outboundDrafts.ContainsKey(e.DraftId))
        {
            _outboundDrafts[e.DraftId] = e;
        }
    }

    public void Apply(OutboundApprovalRequested e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_outboundApprovalRequests.TryGetValue(e.ApprovalId, out OutboundApprovalRequested? existing) ||
            e.SourceVersion >= existing.SourceVersion)
        {
            _outboundApprovalRequests[e.ApprovalId] = e;
        }
    }

    public void Apply(OutboundApprovalDecisionRecorded e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_outboundApprovalDecisions.TryGetValue(e.ApprovalId, out OutboundApprovalDecisionRecorded? existing) ||
            e.SourceVersion >= existing.SourceVersion)
        {
            _outboundApprovalDecisions[e.ApprovalId] = e;
        }
    }

    public void Apply(OutboundSendStarted e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_outboundSends.ContainsKey(e.SendKey))
        {
            _outboundSends[e.SendKey] = e;
        }
    }

    public void Apply(AiActionApprovalRequested e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_approvalRequests.TryGetValue(e.ApprovalId, out AiActionApprovalRequested? existing) ||
            e.SourceVersion >= existing.SourceVersion)
        {
            _approvalRequests[e.ApprovalId] = e;
        }
    }

    public void Apply(AiActionApprovalDecisionRecorded e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_approvalDecisions.TryGetValue(e.ApprovalId, out AiActionApprovalDecisionRecorded? existing) ||
            e.SourceVersion >= existing.SourceVersion)
        {
            _approvalDecisions[e.ApprovalId] = e;
        }
    }

    public void Apply(TenantPolicyChangePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_tenantPolicyPendingApprovals.ContainsKey(e.PolicyChangeId))
        {
            _tenantPolicyPendingApprovals[e.PolicyChangeId] = e;
        }
    }

    public void Apply(TenantPolicySnapshotActivated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _tenantPolicySnapshots[e.ActivatedPolicySnapshotId] = e;
        _ = _tenantPolicyPendingApprovals.Remove(e.PolicyChangeId);
    }

    public void Apply(MailboxParticipantResolved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _participantResolutionIds.Add(e.ResolutionId);
    }

    public void Apply(MailboxParticipantUnresolved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _participantResolutionIds.Add(e.ResolutionId);
    }

    public void Apply(MailboxAssociationCandidatesGenerated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationIds.Add(e.AssociationId);
        AssociationDecisionSource = new AssociationDecisionSourceSnapshot(
            e.AssociationId,
            e.IntakeId,
            e.TenantId,
            e.SourceMailboxId,
            e.SourceConversationId,
            e.SourceThreadId,
            e.Candidates,
            e.Exclusions,
            e.LifecycleState,
            e.ConfidenceScore,
            e.ThresholdBand,
            e.ReasonCodes,
            e.ThresholdPolicyVersion,
            e.DerivationKernelVersion,
            e.DetectedAt,
            e.RedactionState,
            e.RetentionClass,
            e.SourceVersion,
            e.SchemaVersion,
            e.CorrelationId);
        AssociationLifecycleState = e.LifecycleState;
    }

    public void Apply(MailboxEmailAssociatedToProject e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationIds.Add(e.AssociationId);
        AssociationDecisionSource = new AssociationDecisionSourceSnapshot(
            e.AssociationId,
            e.IntakeId,
            e.TenantId,
            e.SourceMailboxId,
            e.SourceConversationId,
            e.SourceThreadId,
            [],
            [],
            LifecycleState.Associated,
            e.ConfidenceScore,
            e.ThresholdBand,
            e.ReasonCodes,
            e.ThresholdPolicyVersion,
            e.DerivationKernelVersion,
            e.DetectedAt,
            e.RedactionState,
            e.RetentionClass,
            e.SourceVersion,
            e.SchemaVersion,
            e.CorrelationId);
        AssociationLifecycleState = LifecycleState.Associated;
        CurrentAssociationProjectId = e.ProjectId;
        CurrentAssociationProjectDisplayName = e.ProjectDisplayName;
        LastAssociationDecisionSourceVersion = e.SourceVersion;
    }

    public void Apply(MailboxAssociationScoringFailedClosed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationIds.Add(e.AssociationId);
        AssociationDecisionSource = new AssociationDecisionSourceSnapshot(
            e.AssociationId,
            e.IntakeId,
            e.TenantId,
            e.SourceMailboxId,
            e.SourceConversationId,
            e.SourceThreadId,
            [],
            e.Exclusions,
            e.LifecycleState,
            e.ConfidenceScore,
            e.ThresholdBand,
            e.ReasonCodes,
            e.ThresholdPolicyVersion,
            e.DerivationKernelVersion,
            e.DetectedAt,
            e.RedactionState,
            e.RetentionClass,
            e.SourceVersion,
            e.SchemaVersion,
            e.CorrelationId);
        AssociationLifecycleState = e.LifecycleState;
    }

    public void Apply(MailboxEmailAssociationConfirmed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationDecisionIds.Add(e.AssociationId);
        LastAssociationDecisionSourceVersion = e.SourceVersion;
        AssociationLifecycleState = LifecycleState.Associated;
        CurrentAssociationProjectId = e.ProjectId;
        CurrentAssociationProjectDisplayName = e.ProjectDisplayName;
    }

    public void Apply(MailboxEmailAssociationRejected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationDecisionIds.Add(e.AssociationId);
        LastAssociationDecisionSourceVersion = e.SourceVersion;
        AssociationLifecycleState = LifecycleState.Rejected;
    }

    public void Apply(MailboxEmailAssociationDeferred e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationDecisionIds.Add(e.AssociationId);
        LastAssociationDecisionSourceVersion = e.SourceVersion;
        AssociationLifecycleState = LifecycleState.Deferred;
    }

    public void Apply(MailboxEmailAssociationMarkedNeedsReview e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationDecisionIds.Add(e.AssociationId);
        LastAssociationDecisionSourceVersion = e.SourceVersion;
        AssociationLifecycleState = LifecycleState.NeedsReview;
    }

    public void Apply(MailboxEmailAssociationCorrected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _associationCorrectionIds.Add($"{e.AssociationId}:{e.CorrectionKind}:{e.SourceVersion}");
        LastAssociationDecisionSourceVersion = e.SourceVersion;
        AssociationLifecycleState = LifecycleState.Corrected;
        PriorAssociationProjectId = e.PriorProjectId;
        CurrentAssociationProjectId = e.CorrectedProjectId;
        CurrentAssociationProjectDisplayName = e.CorrectedProjectDisplayName;
        PredecessorAssociationId = e.PredecessorAssociationId;
        SupersedesAssociationId = e.SupersedesAssociationId;
    }

    public void Apply(MailboxAssociationCorrectionPropagationStarted e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (CorrectionPropagationSourceVersion is > 0 && e.SourceVersion < CorrectionPropagationSourceVersion)
        {
            return;
        }

        CorrectionPropagationCorrectionId = e.CorrectionId;
        CorrectionPropagationWorkflowInstanceId = e.WorkflowInstanceId;
        CorrectionPropagationSourceVersion = e.SourceVersion;
        CorrectionPropagationStartedAtUtc = e.StartedAtUtc;
        CorrectionPropagationEstimatedCompletionAtUtc = e.EstimatedCompletionAtUtc;
        CorrectionPropagationCompletedAtUtc = null;
        IsCorrectionPropagationDelayed = false;
        CorrectionPropagationResponsibleOwnerRole = e.ResponsibleOwnerRole;
        CorrectionPropagationNextSafeAction = e.NextSafeAction;
        _correctionPropagationStores.Clear();
        _correctionPropagationRequiredStores.Clear();
        foreach (string storeKey in e.RequiredStoreKeys.Where(static key => !string.IsNullOrWhiteSpace(key)))
        {
            _ = _correctionPropagationRequiredStores.Add(storeKey);
        }

        AssociationLifecycleState = LifecycleState.Correcting;
    }

    public void Apply(MailboxAssociationCorrectionStoreInvalidated e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (!IsCurrentCorrectionPropagation(e.CorrectionId, e.WorkflowInstanceId, e.SourceVersion))
        {
            return;
        }

        _correctionPropagationStores[e.StoreKey] = new CorrectionPropagationStoreAcknowledgement(
            e.StoreKey,
            e.SourceVersion,
            e.StartedAtUtc,
            e.CompletedAtUtc,
            e.Outcome,
            e.FailureReasonCode,
            e.RedactionState,
            e.RetentionClass,
            e.SchemaVersion);
    }

    public void Apply(MailboxAssociationCorrectionPropagationCompleted e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (!IsCurrentCorrectionPropagation(e.CorrectionId, e.WorkflowInstanceId, e.SourceVersion))
        {
            return;
        }

        CorrectionPropagationCompletedAtUtc = e.CompletedAtUtc;
        IsCorrectionPropagationDelayed = false;
        CorrectionPropagationNextSafeAction = "none";
        AssociationLifecycleState = LifecycleState.Corrected;
    }

    public void Apply(MailboxAssociationCorrectionPropagationDelayed e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (!IsCurrentCorrectionPropagation(e.CorrectionId, e.WorkflowInstanceId, e.SourceVersion))
        {
            return;
        }

        IsCorrectionPropagationDelayed = true;
        CorrectionPropagationResponsibleOwnerRole = e.ResponsibleOwnerRole;
        CorrectionPropagationNextSafeAction = e.NextSafeAction;
        AssociationLifecycleState = LifecycleState.CorrectionDelayed;
    }

    public void Apply(AssociationConfidenceThresholdsChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _thresholdPolicyVersions.Add(e.PolicyVersion);
        _associationTHigh = e.THigh;
        _associationTLow = e.TLow;
        _associationThresholdPolicyVersion = e.PolicyVersion;
    }

    private bool IsCurrentCorrectionPropagation(string correctionId, string workflowInstanceId, long sourceVersion)
        => sourceVersion == CorrectionPropagationSourceVersion &&
            string.Equals(correctionId, CorrectionPropagationCorrectionId, StringComparison.Ordinal) &&
            string.Equals(workflowInstanceId, CorrectionPropagationWorkflowInstanceId, StringComparison.Ordinal);

    private void UpsertTaskIntent(TaskIntentRecord record)
    {
        if (!_taskIntents.TryGetValue(record.TaskIntentId, out TaskIntentRecord? existing) ||
            ShouldReplaceTaskIntent(existing, record))
        {
            _taskIntents[record.TaskIntentId] = record;
        }
    }

    private static bool ShouldReplaceTaskIntent(TaskIntentRecord existing, TaskIntentRecord incoming)
        => incoming.SourceVersion > existing.SourceVersion ||
            incoming.SourceVersion == existing.SourceVersion &&
            TaskIntentStateRank(incoming.State) >= TaskIntentStateRank(existing.State);

    private static int TaskIntentStateRank(TaskIntentState state)
        => state switch
        {
            TaskIntentState.Captured => 0,
            TaskIntentState.Blocked or TaskIntentState.Rejected => 1,
            TaskIntentState.Converted or
                TaskIntentState.NotActionable or
                TaskIntentState.Duplicate or
                TaskIntentState.AlreadyHandled or
                TaskIntentState.OutOfScope => 2,
            _ => 0,
        };
}

public sealed record CorrectionPropagationStoreAcknowledgement(
    string StoreKey,
    long SourceVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string Outcome,
    string? FailureReasonCode,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion)
{
    public bool IsSuccessful => string.Equals(Outcome, "success", StringComparison.Ordinal);
}
