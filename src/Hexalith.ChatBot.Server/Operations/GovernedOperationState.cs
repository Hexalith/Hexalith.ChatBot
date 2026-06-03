using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Governance.AiActor;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.CommandCapability;
using Hexalith.ChatBot.Server.Governance.Mailbox;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Governance.ServiceClient;

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
    private readonly Dictionary<string, MailboxSourceDisablePendingApproval> _mailboxSourceDisablePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MailboxSourceDisabled> _disabledMailboxSources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceClientDisablePendingApproval> _serviceClientDisablePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceClientDisabled> _disabledServiceClients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActorDisablePendingApproval> _aiActorDisablePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActorDisabled> _disabledAiActors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommandCapabilityDisablePendingApproval> _commandCapabilityDisablePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommandCapabilityDisabled> _disabledCommandCapabilities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutboundChannelDisablePendingApproval> _outboundChannelDisablePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutboundChannelDisabled> _disabledOutboundChannels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommandCapabilityQuarantinePendingApproval> _commandCapabilityQuarantinePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommandCapabilityQuarantined> _quarantinedCommandCapabilities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActorQuarantinePendingApproval> _aiActorQuarantinePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActorQuarantined> _quarantinedAiActors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceClientQuarantinePendingApproval> _serviceClientQuarantinePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceClientQuarantined> _quarantinedServiceClients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MailboxSourceQuarantinePendingApproval> _mailboxSourceQuarantinePendingApprovals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MailboxSourceQuarantined> _quarantinedMailboxSources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MailboxSourceRateLimitConfigured> _mailboxSourceRateLimits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceClientRateLimitConfigured> _serviceClientRateLimits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiActorRateLimitConfigured> _aiActorRateLimits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommandCapabilityRateLimitConfigured> _commandCapabilityRateLimits = new(StringComparer.Ordinal);
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

    public IReadOnlyDictionary<string, MailboxSourceDisablePendingApproval> MailboxSourceDisablePendingApprovals => _mailboxSourceDisablePendingApprovals;

    public IReadOnlyDictionary<string, MailboxSourceDisabled> DisabledMailboxSources => _disabledMailboxSources;

    public IReadOnlyDictionary<string, ServiceClientDisablePendingApproval> ServiceClientDisablePendingApprovals => _serviceClientDisablePendingApprovals;

    public IReadOnlyDictionary<string, ServiceClientDisabled> DisabledServiceClients => _disabledServiceClients;

    public IReadOnlyDictionary<string, AiActorDisablePendingApproval> AiActorDisablePendingApprovals => _aiActorDisablePendingApprovals;

    public IReadOnlyDictionary<string, AiActorDisabled> DisabledAiActors => _disabledAiActors;

    public IReadOnlyDictionary<string, CommandCapabilityDisablePendingApproval> CommandCapabilityDisablePendingApprovals => _commandCapabilityDisablePendingApprovals;

    public IReadOnlyDictionary<string, CommandCapabilityDisabled> DisabledCommandCapabilities => _disabledCommandCapabilities;

    public IReadOnlyDictionary<string, OutboundChannelDisablePendingApproval> OutboundChannelDisablePendingApprovals => _outboundChannelDisablePendingApprovals;

    public IReadOnlyDictionary<string, OutboundChannelDisabled> DisabledOutboundChannels => _disabledOutboundChannels;

    public IReadOnlyDictionary<string, CommandCapabilityQuarantinePendingApproval> CommandCapabilityQuarantinePendingApprovals => _commandCapabilityQuarantinePendingApprovals;

    public IReadOnlyDictionary<string, CommandCapabilityQuarantined> QuarantinedCommandCapabilities => _quarantinedCommandCapabilities;

    public IReadOnlyDictionary<string, AiActorQuarantinePendingApproval> AiActorQuarantinePendingApprovals => _aiActorQuarantinePendingApprovals;

    public IReadOnlyDictionary<string, AiActorQuarantined> QuarantinedAiActors => _quarantinedAiActors;

    public IReadOnlyDictionary<string, ServiceClientQuarantinePendingApproval> ServiceClientQuarantinePendingApprovals => _serviceClientQuarantinePendingApprovals;

    public IReadOnlyDictionary<string, ServiceClientQuarantined> QuarantinedServiceClients => _quarantinedServiceClients;

    public IReadOnlyDictionary<string, MailboxSourceQuarantinePendingApproval> MailboxSourceQuarantinePendingApprovals => _mailboxSourceQuarantinePendingApprovals;

    public IReadOnlyDictionary<string, MailboxSourceQuarantined> QuarantinedMailboxSources => _quarantinedMailboxSources;

    /// <summary>
    /// Gets the per-source rate-limit budgets, keyed by safe <see cref="MailboxSourceRateLimitConfigured.MailboxSourceRef"/>.
    /// Each entry is independent (NFR30 isolation): one source's budget never affects a sibling source's.
    /// </summary>
    public IReadOnlyDictionary<string, MailboxSourceRateLimitConfigured> MailboxSourceRateLimits => _mailboxSourceRateLimits;

    /// <summary>
    /// Gets the per-service-client rate-limit budgets, keyed by safe <see cref="ServiceClientRateLimitConfigured.ServiceClientRef"/>.
    /// Each entry is independent (NFR30 isolation): one client's budget never affects a sibling client's.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceClientRateLimitConfigured> ServiceClientRateLimits => _serviceClientRateLimits;

    /// <summary>
    /// Gets the per-AI-actor rate-limit budgets, keyed by safe <see cref="AiActorRateLimitConfigured.AiActorRef"/>.
    /// Each entry is independent (NFR30 isolation): one AI actor's budget never affects a sibling AI actor's, a
    /// service client's, or another tenant's.
    /// </summary>
    public IReadOnlyDictionary<string, AiActorRateLimitConfigured> AiActorRateLimits => _aiActorRateLimits;

    /// <summary>
    /// Gets the per-(tenant × command-type) command-capability rate-limit budgets, keyed by safe
    /// <see cref="CommandCapabilityRateLimitConfigured.CommandCapabilityRef"/> (the command type name). Each entry
    /// is independent (NFR30 isolation): one command type's budget never affects a sibling command type's, an
    /// actor's, or another tenant's.
    /// </summary>
    public IReadOnlyDictionary<string, CommandCapabilityRateLimitConfigured> CommandCapabilityRateLimits => _commandCapabilityRateLimits;

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

    public void Apply(MailboxSourceDisablePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_mailboxSourceDisablePendingApprovals.ContainsKey(e.DisableChangeId))
        {
            _mailboxSourceDisablePendingApprovals[e.DisableChangeId] = e;
        }
    }

    public void Apply(MailboxSourceDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _disabledMailboxSources[e.MailboxSourceRef] = e;
        _ = _mailboxSourceDisablePendingApprovals.Remove(e.DisableChangeId);
    }

    public void Apply(ServiceClientDisablePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_serviceClientDisablePendingApprovals.ContainsKey(e.DisableChangeId))
        {
            _serviceClientDisablePendingApprovals[e.DisableChangeId] = e;
        }
    }

    public void Apply(ServiceClientDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _disabledServiceClients[e.ServiceClientRef] = e;
        _ = _serviceClientDisablePendingApprovals.Remove(e.DisableChangeId);
    }

    public void Apply(AiActorDisablePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_aiActorDisablePendingApprovals.ContainsKey(e.DisableChangeId))
        {
            _aiActorDisablePendingApprovals[e.DisableChangeId] = e;
        }
    }

    public void Apply(AiActorDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _disabledAiActors[e.AiActorRef] = e;
        _ = _aiActorDisablePendingApprovals.Remove(e.DisableChangeId);
    }

    public void Apply(CommandCapabilityDisablePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_commandCapabilityDisablePendingApprovals.ContainsKey(e.DisableChangeId))
        {
            _commandCapabilityDisablePendingApprovals[e.DisableChangeId] = e;
        }
    }

    public void Apply(CommandCapabilityDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _disabledCommandCapabilities[e.CommandCapabilityRef] = e;
        _ = _commandCapabilityDisablePendingApprovals.Remove(e.DisableChangeId);
    }

    public void Apply(OutboundChannelDisablePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_outboundChannelDisablePendingApprovals.ContainsKey(e.DisableChangeId))
        {
            _outboundChannelDisablePendingApprovals[e.DisableChangeId] = e;
        }
    }

    public void Apply(OutboundChannelDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _disabledOutboundChannels[e.OutboundChannelRef] = e;
        _ = _outboundChannelDisablePendingApprovals.Remove(e.DisableChangeId);
    }

    public void Apply(CommandCapabilityQuarantinePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_commandCapabilityQuarantinePendingApprovals.ContainsKey(e.QuarantineChangeId))
        {
            _commandCapabilityQuarantinePendingApprovals[e.QuarantineChangeId] = e;
        }
    }

    public void Apply(CommandCapabilityQuarantined e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _quarantinedCommandCapabilities[e.CommandCapabilityRef] = e;
        _ = _commandCapabilityQuarantinePendingApprovals.Remove(e.QuarantineChangeId);
    }

    public void Apply(AiActorQuarantinePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_aiActorQuarantinePendingApprovals.ContainsKey(e.QuarantineChangeId))
        {
            _aiActorQuarantinePendingApprovals[e.QuarantineChangeId] = e;
        }
    }

    public void Apply(AiActorQuarantined e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _quarantinedAiActors[e.AiActorRef] = e;
        _ = _aiActorQuarantinePendingApprovals.Remove(e.QuarantineChangeId);
    }

    public void Apply(ServiceClientQuarantinePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_serviceClientQuarantinePendingApprovals.ContainsKey(e.QuarantineChangeId))
        {
            _serviceClientQuarantinePendingApprovals[e.QuarantineChangeId] = e;
        }
    }

    public void Apply(ServiceClientQuarantined e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _quarantinedServiceClients[e.ServiceClientRef] = e;
        _ = _serviceClientQuarantinePendingApprovals.Remove(e.QuarantineChangeId);
    }

    public void Apply(MailboxSourceQuarantinePendingApproval e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_mailboxSourceQuarantinePendingApprovals.ContainsKey(e.QuarantineChangeId))
        {
            _mailboxSourceQuarantinePendingApprovals[e.QuarantineChangeId] = e;
        }
    }

    public void Apply(MailboxSourceQuarantined e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _quarantinedMailboxSources[e.MailboxSourceRef] = e;
        _ = _mailboxSourceQuarantinePendingApprovals.Remove(e.QuarantineChangeId);
    }

    public void Apply(MailboxSourceRateLimitConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _mailboxSourceRateLimits[e.MailboxSourceRef] = e;
    }

    public void Apply(ServiceClientRateLimitConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _serviceClientRateLimits[e.ServiceClientRef] = e;
    }

    public void Apply(AiActorRateLimitConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _aiActorRateLimits[e.AiActorRef] = e;
    }

    public void Apply(CommandCapabilityRateLimitConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _commandCapabilityRateLimits[e.CommandCapabilityRef] = e;
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
