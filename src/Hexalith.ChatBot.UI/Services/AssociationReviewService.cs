using System.Reflection;
using System.Runtime.Serialization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.State.AssociationReview;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// UI-owned read service for the S2 association review surface. It reads metadata-only routing status through
/// <see cref="IChatBotClient"/> and maps generated contract DTOs into display view models without touching
/// Server projections, stores, DAPR, or EventStore internals.
/// </summary>
public sealed class AssociationReviewService(IChatBotClient client)
{
    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<AssociationReviewModel> GetAssociationReviewAsync(
        string associationId,
        CancellationToken cancellationToken = default)
    {
        AssociationRoutingStatus status = await _client
            .GetAssociationRoutingStatusAsync(associationId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        AssociationCandidateModel[] candidates = status.Candidates
            .OrderBy(static candidate => candidate.Rank)
            .Select(MapCandidate)
            .ToArray();
        AssociationEvidenceModel[] evidence = status.EvidenceRefs
            .Select(MapEvidence)
            .ToArray();

        return new AssociationReviewModel(
            status.AssociationId,
            status.IntakeId,
            status.SourceMailboxId,
            status.SourceConversationId,
            status.SourceThreadId,
            WireValue(status.LifecycleState),
            WireValue(status.Outcome),
            WireValue(status.ThresholdBand),
            status.ConfidenceScore,
            status.ReasonCodes.Select(WireValue).ToArray(),
            candidates,
            evidence,
            status.DisabledActionReasonCodes.ToArray(),
            status.NextActionReasonCodes.Select(WireValue).ToArray(),
            status.ThresholdPolicyVersion,
            status.KernelVersion,
            status.DetectedAt,
            WireValue(status.SourceProvenance),
            WireValue(status.RedactionState),
            WireValue(status.RetentionClass),
            status.SchemaVersion,
            status.CorrelationId);
    }

    private static AssociationCandidateModel MapCandidate(AssociationCandidate candidate)
    {
        AssociationEvidenceModel[] evidence = candidate.EvidenceRefs
            .Select(MapEvidence)
            .ToArray();

        return new AssociationCandidateModel(
            candidate.ProjectId,
            string.IsNullOrWhiteSpace(candidate.DisplayName) ? $"Project candidate {candidate.Rank}" : candidate.DisplayName,
            candidate.ConfidenceScore,
            candidate.Rank,
            candidate.ReasonCodes.Select(WireValue).ToArray(),
            evidence,
            candidate.RequiredEvidenceComplete);
    }

    private static AssociationEvidenceModel MapEvidence(AssociationEvidenceReference evidence)
    {
        ChatBotEvidenceState state = ResolveEvidenceState(evidence);

        return new AssociationEvidenceModel(
            evidence.EvidenceReference,
            evidence.EvidenceFingerprint,
            evidence.EvidenceKind,
            state,
            state is ChatBotEvidenceState.Available ? string.Empty : "Evidence restricted");
    }

    private static ChatBotEvidenceState ResolveEvidenceState(AssociationEvidenceReference evidence)
    {
        string evidenceClassification = $"{evidence.EvidenceKind} {evidence.EvidenceReference}";

        if (ContainsAny(evidenceClassification, "unauthorized", "restricted", "suppressed"))
        {
            return ChatBotEvidenceState.Unauthorized;
        }

        if (ContainsAny(evidenceClassification, "redacted"))
        {
            return ChatBotEvidenceState.Redacted;
        }

        if (ContainsAny(evidenceClassification, "unavailable", "expired", "stale"))
        {
            return ChatBotEvidenceState.Unavailable;
        }

        return ChatBotEvidenceState.Available;
    }

    private static bool ContainsAny(string value, params string[] needles)
        => needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static string WireValue<T>(T value)
        where T : struct, Enum
        => typeof(T)
            .GetField(value.ToString())
            ?.GetCustomAttribute<EnumMemberAttribute>()
            ?.Value
            ?? value.ToString();
}
