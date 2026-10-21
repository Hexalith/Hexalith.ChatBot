using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotGatewayContext(
    ChatBotCommandSubmission Submission,
    ChatBotAuthenticatedActor Actor,
    ChatBotTenantBinding TenantBinding,
    ServiceClientGrantEvidence? ServiceClientGrantEvidence = null,
    IReadOnlyList<string>? AuthorityEvidenceReferences = null)
{
    private Func<string, CancellationToken, ValueTask<bool>>? _dispatchTargetBinding;

    /// <summary>The single profile selected by the central product catalog.</summary>
    public ChatBotAdmissionProfile? AdmissionProfile { get; private set; }

    /// <summary>Sets the centrally derived profile exactly once.</summary>
    public void SelectAdmissionProfile(ChatBotAdmissionProfile profile)
    {
        if (AdmissionProfile is not null || !Enum.IsDefined(profile))
        {
            throw new InvalidOperationException("Admission profile cannot change during a command.");
        }
        AdmissionProfile = profile;
    }

    /// <summary>Current operation-bound owner proof read once before classification or applicable profile checks.</summary>
    public ChatBotProfileAdmissionEvidence? AdmissionEvidence { get; private set; }

    /// <summary>Caches validated current owner identity and concurrency evidence without writing state.</summary>
    public void SetAdmissionEvidence(ChatBotProfileAdmissionEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        AdmissionEvidence = evidence;
    }

    /// <summary>Canonical mutation bookkeeping constructed without persisting anything before commit participation.</summary>
    public AuditEnvelope? AdmissionEnvelope { get; private set; }

    /// <summary>Retains the canonical envelope for the atomic owner boundary.</summary>
    public void SetAdmissionEnvelope(AuditEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        AdmissionEnvelope = envelope;
    }

    public CoarseIdempotencyMetadata? Idempotency { get; private set; }

    public ChatBotRiskClassification? RiskClassification { get; private set; }

    public ChatBotApprovalResult? ApprovalResult { get; private set; }

    /// <summary>The UTC acceptance identity prepared before external dispatch.</summary>
    public DateTimeOffset? PreparedAcceptedAt { get; private set; }

    /// <summary>The aggregate target durably bound to this prepared dispatch ownership.</summary>
    public string? PreparedAggregateId { get; private set; }

    /// <summary>Whether the actual SDK submission returned successfully for this bound dispatch.</summary>
    public bool SdkSubmissionAccepted { get; private set; }

    /// <summary>Observes SDK acceptance only after the bound command's submission has returned successfully.</summary>
    public void MarkSdkSubmissionAccepted()
    {
        if (string.IsNullOrWhiteSpace(PreparedAggregateId))
        {
            throw new InvalidOperationException("SDK acceptance requires a bound prepared dispatch target.");
        }

        SdkSubmissionAccepted = true;
    }

    /// <summary>Installs the gateway's prepared-outcome and owner-fenced target binding.</summary>
    public void SetDispatchTargetBinding(Func<string, CancellationToken, ValueTask<bool>> binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _dispatchTargetBinding = binding;
    }

    /// <summary>Requires durable target binding before the dispatch plan can be submitted to EventStore.</summary>
    public async ValueTask<bool> BindDispatchTargetAsync(string aggregateId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId) || _dispatchTargetBinding is null ||
            !await _dispatchTargetBinding(aggregateId, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        PreparedAggregateId = aggregateId;
        return true;
    }

    /// <summary>Whether dispatch has attempted an external write whose outcome may be uncertain.</summary>
    public bool ExternalEffectAttempted { get; private set; }

    /// <summary>Marks the boundary before invoking an external writer or EventStore submission.</summary>
    public void MarkExternalEffectAttempted() => ExternalEffectAttempted = true;

    /// <summary>Uses the same acceptance instant in dispatch evidence and the durable response.</summary>
    public void SetPreparedAcceptedAt(DateTimeOffset acceptedAt) => PreparedAcceptedAt = acceptedAt.ToUniversalTime();

    public void SetIdempotency(CoarseIdempotencyMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Idempotency = metadata;
    }

    public void SetRiskClassification(ChatBotRiskClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);
        RiskClassification = classification;
    }

    public void SetApprovalResult(ChatBotApprovalResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ApprovalResult = result;
    }
}
