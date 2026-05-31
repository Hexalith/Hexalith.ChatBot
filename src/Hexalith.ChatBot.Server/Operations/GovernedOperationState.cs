using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Association;

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
    private readonly HashSet<string> _thresholdPolicyVersions = new(StringComparer.Ordinal);
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

    public AssociationDecisionSourceSnapshot? AssociationDecisionSource { get; private set; }

    public long? LastAssociationDecisionSourceVersion { get; private set; }

    public LifecycleState? AssociationLifecycleState { get; private set; }

    public string? CurrentAssociationProjectId { get; private set; }

    public string? CurrentAssociationProjectDisplayName { get; private set; }

    public string? PredecessorAssociationId { get; private set; }

    public string? SupersedesAssociationId { get; private set; }

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
        CurrentAssociationProjectId = e.CorrectedProjectId;
        CurrentAssociationProjectDisplayName = e.CorrectedProjectDisplayName;
        PredecessorAssociationId = e.PredecessorAssociationId;
        SupersedesAssociationId = e.SupersedesAssociationId;
    }

    public void Apply(AssociationConfidenceThresholdsChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = _thresholdPolicyVersions.Add(e.PolicyVersion);
        _associationTHigh = e.THigh;
        _associationTLow = e.TLow;
        _associationThresholdPolicyVersion = e.PolicyVersion;
    }
}
