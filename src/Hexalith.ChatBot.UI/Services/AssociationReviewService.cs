using System.Reflection;
using System.Runtime.Serialization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.State.AssociationReview;

using GeneratedAssociationCandidate = Hexalith.ChatBot.Client.Generated.AssociationCandidate;
using GeneratedAssociationEvidenceReference = Hexalith.ChatBot.Client.Generated.AssociationEvidenceReference;
using ContractAssociationDecisionKind = Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind;
using ContractAssociationCorrectionKind = Hexalith.ChatBot.Contracts.Enums.AssociationCorrectionKind;
using ContractAssociateEmailToProject = Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject;
using ContractCorrectEmailProjectAssociation = Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation;
using ContractRejectEmailProjectAssociation = Hexalith.ChatBot.Contracts.Commands.RejectEmailProjectAssociation;
using ContractDeferEmailProjectAssociation = Hexalith.ChatBot.Contracts.Commands.DeferEmailProjectAssociation;
using ContractMarkEmailAssociationNeedsReview = Hexalith.ChatBot.Contracts.Commands.MarkEmailAssociationNeedsReview;

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
            status.SourceVersion,
            status.CorrelationId,
            status.CorrectedProjectId,
            status.PriorProjectId,
            status.PredecessorAssociationId,
            status.SupersedesAssociationId,
            null,
            status.DownstreamImpactStatus,
            status.PropagationStatus,
            status.PropagationProgressNumerator,
            status.PropagationProgressDenominator,
            status.PropagationEstimatedCompletionAtUtc,
            status.IsCorrectedContextStale ?? false,
            status.ResponsibleOwnerRole,
            status.SafeNextAction);
    }

    public async Task<AssociationDecisionSubmitResult> SubmitDecisionAsync(
        AssociationReviewModel review,
        string decisionCode,
        string? selectedCandidateId,
        string? decisionNote,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        string? normalizedNote = NormalizeNote(decisionNote);
        IChatBotCommand command = BuildDecisionCommand(review, decisionCode, selectedCandidateId, normalizedNote);
        CommandSubmissionResponse accepted = await _client
            .SubmitAsync(command, review.CorrelationId, origin: ChatBotSurfaceOrigin.Ui, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        AssociationReviewModel refreshed = await GetAssociationReviewAsync(review.AssociationId, cancellationToken)
            .ConfigureAwait(false);

        return new AssociationDecisionSubmitResult(
            accepted.CommandId,
            accepted.CorrelationId,
            accepted.TaskId,
            WireValue(accepted.LifecycleState),
            refreshed);
    }

    public async Task<AssociationCorrectionSubmitResult> SubmitCorrectionAsync(
        AssociationReviewModel review,
        string targetProjectId,
        string? correctionRationale,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        if (string.IsNullOrWhiteSpace(targetProjectId))
        {
            throw new InvalidOperationException("correction-target-required");
        }

        string? normalizedRationale = NormalizeNote(correctionRationale);
        string evidenceFingerprint = CorrectionEvidenceFingerprint(review, targetProjectId);
        string priorProjectId = CurrentProjectIdForCorrection(review, targetProjectId);
        IChatBotCommand command = new ContractCorrectEmailProjectAssociation(
            review.AssociationId,
            review.IntakeId,
            priorProjectId,
            targetProjectId,
            ContractAssociationCorrectionKind.ProjectReassignment,
            normalizedRationale,
            string.IsNullOrWhiteSpace(review.PredecessorAssociationId) ? review.AssociationId : review.PredecessorAssociationId,
            evidenceFingerprint,
            review.SourceVersion,
            "chatbot.association-correction-command.v1");
        CommandSubmissionResponse accepted = await _client
            .SubmitAsync(command, review.CorrelationId, origin: ChatBotSurfaceOrigin.Ui, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        AssociationReviewModel refreshed = await GetAssociationReviewAsync(review.AssociationId, cancellationToken)
            .ConfigureAwait(false);

        return new AssociationCorrectionSubmitResult(
            accepted.CommandId,
            accepted.CorrelationId,
            accepted.TaskId,
            WireValue(accepted.LifecycleState),
            refreshed);
    }

    private static IChatBotCommand BuildDecisionCommand(
        AssociationReviewModel review,
        string decisionCode,
        string? selectedCandidateId,
        string? decisionNote)
    {
        string evidenceFingerprint = DecisionEvidenceFingerprint(review, selectedCandidateId);
        return decisionCode switch
        {
            "choose-candidate" => new ContractAssociateEmailToProject(
                review.AssociationId,
                review.IntakeId,
                RequiredSelectedCandidate(review, selectedCandidateId).ProjectId,
                ContractAssociationDecisionKind.Associate,
                decisionNote,
                evidenceFingerprint,
                review.SourceVersion,
                "chatbot.association-decision-command.v1"),
            "reject-all" => new ContractRejectEmailProjectAssociation(
                review.AssociationId,
                review.IntakeId,
                ContractAssociationDecisionKind.Reject,
                decisionNote,
                evidenceFingerprint,
                review.SourceVersion,
                "chatbot.association-decision-command.v1"),
            "defer" => new ContractDeferEmailProjectAssociation(
                review.AssociationId,
                review.IntakeId,
                ContractAssociationDecisionKind.Defer,
                decisionNote,
                evidenceFingerprint,
                review.SourceVersion,
                "chatbot.association-decision-command.v1"),
            "mark-needs-review" => new ContractMarkEmailAssociationNeedsReview(
                review.AssociationId,
                review.IntakeId,
                ContractAssociationDecisionKind.NeedsReview,
                decisionNote,
                evidenceFingerprint,
                review.SourceVersion,
                "chatbot.association-decision-command.v1"),
            _ => throw new ArgumentException("Unknown association decision action.", nameof(decisionCode)),
        };
    }

    private static AssociationCandidateModel RequiredSelectedCandidate(AssociationReviewModel review, string? selectedCandidateId)
        => review.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, selectedCandidateId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("candidate-required");

    private static string DecisionEvidenceFingerprint(AssociationReviewModel review, string? selectedCandidateId)
    {
        AssociationCandidateModel? selected = review.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, selectedCandidateId, StringComparison.Ordinal));
        string? fingerprint = selected?.Evidence.FirstOrDefault()?.Fingerprint
            ?? review.Evidence.FirstOrDefault()?.Fingerprint
            ?? review.Candidates.SelectMany(static candidate => candidate.Evidence).FirstOrDefault()?.Fingerprint;
        return string.IsNullOrWhiteSpace(fingerprint)
            ? throw new InvalidOperationException("stale-evidence")
            : fingerprint;
    }

    private static string CorrectionEvidenceFingerprint(AssociationReviewModel review, string targetProjectId)
    {
        string? fingerprint = review.Candidates
            .FirstOrDefault(candidate => string.Equals(candidate.ProjectId, targetProjectId, StringComparison.Ordinal))
            ?.Evidence
            .FirstOrDefault()
            ?.Fingerprint
            ?? review.Evidence.FirstOrDefault()?.Fingerprint
            ?? review.Candidates.SelectMany(static candidate => candidate.Evidence).FirstOrDefault()?.Fingerprint;
        return string.IsNullOrWhiteSpace(fingerprint)
            ? throw new InvalidOperationException("stale-evidence")
            : fingerprint;
    }

    private static string CurrentProjectIdForCorrection(AssociationReviewModel review, string targetProjectId)
    {
        string? currentProjectId = review.CorrectedProjectId
            ?? review.PriorProjectId
            ?? review.Candidates.FirstOrDefault(candidate => !string.Equals(candidate.ProjectId, targetProjectId, StringComparison.Ordinal))?.ProjectId;

        return string.IsNullOrWhiteSpace(currentProjectId)
            ? throw new InvalidOperationException("correction-source-required")
            : currentProjectId;
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        string normalized = string.Join(' ', note.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 1024 ? normalized : throw new InvalidOperationException("association-review-note-too-long");
    }

    private static AssociationCandidateModel MapCandidate(GeneratedAssociationCandidate candidate)
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

    private static AssociationEvidenceModel MapEvidence(GeneratedAssociationEvidenceReference evidence)
    {
        ChatBotEvidenceState state = ResolveEvidenceState(evidence);

        return new AssociationEvidenceModel(
            evidence.EvidenceReference,
            evidence.EvidenceFingerprint,
            evidence.EvidenceKind,
            state,
            state is ChatBotEvidenceState.Available ? string.Empty : "Evidence restricted");
    }

    private static ChatBotEvidenceState ResolveEvidenceState(GeneratedAssociationEvidenceReference evidence)
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
