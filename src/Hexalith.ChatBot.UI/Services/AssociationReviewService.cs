using System.Reflection;
using System.Runtime.Serialization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
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
public sealed class AssociationReviewService(IChatBotClient client, ChatBotUiTextLocalizer uiText)
{
    /// <summary>Maximum length of a decision note or correction rationale, after normalization.</summary>
    public const int MaximumNoteLength = 1024;

    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ChatBotUiTextLocalizer _uiText = uiText ?? throw new ArgumentNullException(nameof(uiText));

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
        AssociationReviewModel refreshed = await RefreshAfterAcceptedAsync(review, cancellationToken)
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
        AssociationReviewModel refreshed = await RefreshAfterAcceptedAsync(review, cancellationToken)
            .ConfigureAwait(false);

        return new AssociationCorrectionSubmitResult(
            accepted.CommandId,
            accepted.CorrelationId,
            accepted.TaskId,
            WireValue(accepted.LifecycleState),
            refreshed);
    }

    /// <summary>
    /// Re-reads the association after the command was accepted. The command is already durable at this point,
    /// so a failure of this read must not surface as a submission failure - that would invite the reviewer to
    /// retry an operation that already succeeded. The pre-submit snapshot is returned instead, and the
    /// accepted lifecycle carried on the result tells the surface the projection is still catching up.
    /// </summary>
    private async Task<AssociationReviewModel> RefreshAfterAcceptedAsync(
        AssociationReviewModel review,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GetAssociationReviewAsync(review.AssociationId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return review;
        }
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

        // The fingerprint binds the governed decision to the evidence it was made on. Falling back to another
        // candidate's evidence would stamp the command with a fingerprint the reviewer never saw and defeat
        // the server's evidence-binding check, so a candidate without usable evidence fails closed instead.
        string? fingerprint = selected is null
            ? UsableFingerprint(review.Evidence)
            : UsableFingerprint(selected.Evidence);

        return string.IsNullOrWhiteSpace(fingerprint)
            ? throw new InvalidOperationException("stale-evidence")
            : fingerprint;
    }

    /// <summary>
    /// Returns the first fingerprint whose evidence is actually available. Evidence the surface renders as
    /// stale, redacted, or unauthorized must not be the evidence a durable command is signed with.
    /// </summary>
    private static string? UsableFingerprint(IReadOnlyList<AssociationEvidenceModel> evidence)
        => evidence
            .FirstOrDefault(item => item.State is ChatBotEvidenceState.Available && !string.IsNullOrWhiteSpace(item.Fingerprint))
            ?.Fingerprint;

    private static string CorrectionEvidenceFingerprint(AssociationReviewModel review, string targetProjectId)
    {
        AssociationCandidateModel? target = review.Candidates
            .FirstOrDefault(candidate => string.Equals(candidate.ProjectId, targetProjectId, StringComparison.Ordinal));
        string? fingerprint = target is null
            ? UsableFingerprint(review.Evidence)
            : UsableFingerprint(target.Evidence);
        return string.IsNullOrWhiteSpace(fingerprint)
            ? throw new InvalidOperationException("stale-evidence")
            : fingerprint;
    }

    private static string CurrentProjectIdForCorrection(AssociationReviewModel review, string targetProjectId)
    {
        // Only the server may say what the association previously pointed at. Guessing "any candidate that is
        // not the target" would write an assertion into the audit trail that never happened.
        string? currentProjectId = review.CorrectedProjectId ?? review.PriorProjectId;

        if (string.IsNullOrWhiteSpace(currentProjectId))
        {
            throw new InvalidOperationException("correction-source-required");
        }

        // Re-correcting to the project the association already points at is not a correction.
        return string.Equals(currentProjectId, targetProjectId, StringComparison.Ordinal)
            ? throw new InvalidOperationException("correction-target-unchanged")
            : currentProjectId;
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        // Collapse runs of spaces/tabs within each line but keep the reviewer's line breaks: a deliberately
        // multi-line note is evidence, and silently reflowing it into one line changes what was recorded.
        string[] lines = note.Trim().ReplaceLineEndings("\n").Split('\n');
        string normalized = string.Join(
            '\n',
            lines.Select(static line => string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
        return normalized.Length <= MaximumNoteLength
            ? normalized
            : throw new InvalidOperationException("association-review-note-too-long");
    }

    private AssociationCandidateModel MapCandidate(GeneratedAssociationCandidate candidate)
    {
        AssociationEvidenceModel[] evidence = candidate.EvidenceRefs
            .Select(MapEvidence)
            .ToArray();

        return new AssociationCandidateModel(
            candidate.ProjectId,
            string.IsNullOrWhiteSpace(candidate.DisplayName)
                ? _uiText.Get(
                    ChatBotUiTextKey.AssociationReviewCandidateFallbackLabelTemplate,
                    candidate.Rank.ToString(System.Globalization.CultureInfo.CurrentCulture))
                : candidate.DisplayName,
            candidate.ConfidenceScore,
            candidate.Rank,
            candidate.ReasonCodes.Select(WireValue).ToArray(),
            evidence,
            candidate.RequiredEvidenceComplete);
    }

    private AssociationEvidenceModel MapEvidence(GeneratedAssociationEvidenceReference evidence)
    {
        ChatBotEvidenceState state = ResolveEvidenceState(evidence);

        return new AssociationEvidenceModel(
            evidence.EvidenceReference,
            evidence.EvidenceFingerprint,
            evidence.EvidenceKind,
            state,
            state is ChatBotEvidenceState.Available
                ? string.Empty
                : _uiText[ChatBotUiTextKey.AssociationReviewEvidenceRestricted]);
    }

    private static ChatBotEvidenceState ResolveEvidenceState(GeneratedAssociationEvidenceReference evidence)
    {
        // Honor the server's authoritative structured states first and fail closed. The routing-status
        // contract stamps explicit visibility/redaction/freshness states (for example excluded candidates
        // are emitted as redacted); relying only on keyword sniffing would render the reference whenever the
        // evidence kind/reference text happens to omit a magic word.
        if (evidence.VisibilityState is AssociationEvidenceReferenceVisibilityState.Redacted
            || evidence.RedactionState is AssociationEvidenceReferenceRedactionState.Redacted)
        {
            return ChatBotEvidenceState.Redacted;
        }

        if (evidence.VisibilityState is AssociationEvidenceReferenceVisibilityState.Unavailable
            || evidence.RedactionState is AssociationEvidenceReferenceRedactionState.Unavailable
            || evidence.FreshnessState is AssociationEvidenceReferenceFreshnessState.Stale
                or AssociationEvidenceReferenceFreshnessState.Unavailable)
        {
            return ChatBotEvidenceState.Unavailable;
        }

        // Secondary keyword safety net for evidence that hints restriction even when the server omitted
        // structured states.
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

    /// <summary>
    /// Resolves an enum's wire value. A value the generated client does not define means the server is ahead
    /// of this client; returning its numeric ordinal would put a bare number on screen and silently make every
    /// lifecycle comparison false, so it fails closed instead. Cached because this runs once per lifecycle,
    /// outcome, threshold band, and per reason code on every load and every post-submit refresh.
    /// </summary>
    private static string WireValue<T>(T value)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new InvalidOperationException("unsupported-wire-value");
        }

        return WireValueCache<T>.Values.TryGetValue(value, out string? wire) ? wire : value.ToString();
    }

    private static class WireValueCache<T>
        where T : struct, Enum
    {
        public static readonly IReadOnlyDictionary<T, string> Values = Enum
            .GetValues<T>()
            .Distinct()
            .ToDictionary(
                static item => item,
                static item => typeof(T)
                    .GetField(item.ToString())
                    ?.GetCustomAttribute<EnumMemberAttribute>()
                    ?.Value
                    ?? item.ToString());
    }
}
