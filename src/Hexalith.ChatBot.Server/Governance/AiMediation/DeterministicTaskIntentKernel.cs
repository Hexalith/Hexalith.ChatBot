using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class DeterministicTaskIntentKernel
{
    public const string CurrentKernelVersion = "chatbot.task-intent.kernel.m0.v1";
    public const string CurrentSchemaVersion = "chatbot.task-intent-record.v1";
    public const int SummaryMaxLength = 280;

    public static TaskIntentDetectionResult Detect(TaskIntentDetectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        TaskIntentDetectionResult? rejection = ValidatePreconditions(request);
        if (rejection is not null)
        {
            return rejection;
        }

        ProjectConversationDetectedActionKind actionKind = ResolveActionKind(request.SafeIntentSignals);
        if (actionKind is ProjectConversationDetectedActionKind.InformOnly)
        {
            return new TaskIntentDetectionResult(
                TaskIntentState.NotActionable,
                TaskIntentReasonCodes.NotActionable,
                null,
                TaskIntentReasonCodes.NotActionable);
        }

        string summary = SummaryFor(actionKind);
        string recordId = TaskIntentIdempotency.ComposeKey(
            request.TenantId,
            request.ProjectId,
            request.SourceMessageId,
            request.RequesterPartyId,
            request.KernelVersion,
            actionKind,
            request.SourceEvidenceOffsets);
        TaskIntentRecord record = new(
            recordId,
            request.TenantId,
            request.ProjectId,
            request.SourceMessageId,
            request.RequesterPartyId,
            summary,
            actionKind,
            NormalizeEvidence(request.SourceEvidenceOffsets),
            request.KernelVersion,
            request.ConfidenceScore,
            (request.DetectedAt ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            TaskIntentState.Captured,
            request.SchemaVersion,
            TaskIntentReasonCodes.Captured,
            request.SourceProvenance,
            request.RedactionState,
            request.RetentionClass,
            request.SourceVersion,
            request.CorrelationId,
            request.PolicySnapshotId,
            request.CorrectionLineageId,
            ConversionReadinessBlocked: false,
            SafeNextAction: SafeNextActionFor(actionKind));

        return new TaskIntentDetectionResult(TaskIntentState.Captured, TaskIntentReasonCodes.Captured, record, TaskIntentReasonCodes.Captured);
    }

    private static TaskIntentDetectionResult? ValidatePreconditions(TaskIntentDetectionRequest request)
    {
        if (!request.TenantScopeResolved || string.IsNullOrWhiteSpace(request.TenantId))
        {
            return Reject(TaskIntentReasonCodes.MissingTenantScope);
        }

        if (!request.ProjectAuthorized || string.IsNullOrWhiteSpace(request.ProjectId))
        {
            return Reject(TaskIntentReasonCodes.MissingProjectAuthorization);
        }

        if (!request.SourceMessageAuthorized || string.IsNullOrWhiteSpace(request.SourceMessageId))
        {
            return Reject(TaskIntentReasonCodes.MissingSourceAuthorization);
        }

        if (string.IsNullOrWhiteSpace(request.RequesterPartyId))
        {
            return Reject(TaskIntentReasonCodes.MissingRequesterParty);
        }

        if (!request.AuditReady)
        {
            return Reject(TaskIntentReasonCodes.MissingAuditReadiness);
        }

        if (request.SourceEvidenceOffsets is not { Count: > 0 } ||
            request.SourceEvidenceOffsets.Any(static evidence => string.IsNullOrWhiteSpace(evidence.EvidenceReference)))
        {
            return Reject(TaskIntentReasonCodes.MissingSourceEvidence);
        }

        if (!request.CorrectedContextReady)
        {
            return Block(TaskIntentReasonCodes.StaleCorrectedContext, request);
        }

        if (string.Equals(request.RedactionState, "redacted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.RedactionState, "unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return Reject(TaskIntentReasonCodes.RedactedSource);
        }

        if (double.IsNaN(request.ConfidenceScore) ||
            double.IsInfinity(request.ConfidenceScore) ||
            request.ConfidenceScore is < 0 or > 1)
        {
            return Reject(TaskIntentReasonCodes.InvalidConfidence);
        }

        return null;
    }

    private static TaskIntentDetectionResult Reject(string reasonCode)
        => new(TaskIntentState.Rejected, reasonCode, null, reasonCode);

    private static TaskIntentDetectionResult Block(string reasonCode, TaskIntentDetectionRequest request)
    {
        TaskIntentRecord record = new(
            TaskIntentIdempotency.ComposeKey(
                request.TenantId,
                request.ProjectId,
                request.SourceMessageId,
                request.RequesterPartyId,
                request.KernelVersion,
                ResolveActionKind(request.SafeIntentSignals),
                request.SourceEvidenceOffsets),
            request.TenantId,
            request.ProjectId,
            request.SourceMessageId,
            request.RequesterPartyId,
            "task intent blocked pending correction readiness",
            ResolveActionKind(request.SafeIntentSignals),
            NormalizeEvidence(request.SourceEvidenceOffsets),
            request.KernelVersion,
            Math.Clamp(request.ConfidenceScore, 0, 1),
            (request.DetectedAt ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            TaskIntentState.Blocked,
            request.SchemaVersion,
            reasonCode,
            request.SourceProvenance,
            request.RedactionState,
            request.RetentionClass,
            request.SourceVersion,
            request.CorrelationId,
            request.PolicySnapshotId,
            request.CorrectionLineageId,
            ConversionReadinessBlocked: true,
            SafeNextAction: "wait-for-correction-propagation");

        return new TaskIntentDetectionResult(TaskIntentState.Blocked, reasonCode, record, reasonCode);
    }

    private static ProjectConversationDetectedActionKind ResolveActionKind(IReadOnlyList<string> signals)
    {
        if (signals.Any(static signal => signal.Contains("decision", StringComparison.OrdinalIgnoreCase) || signal.Contains("approve", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectConversationDetectedActionKind.RequestDecision;
        }

        if (signals.Any(static signal => signal.Contains("question", StringComparison.OrdinalIgnoreCase) || signal.Contains("information", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectConversationDetectedActionKind.RequestInformation;
        }

        if (signals.Any(static signal => signal.Contains("task", StringComparison.OrdinalIgnoreCase) || signal.Contains("action", StringComparison.OrdinalIgnoreCase) || signal.Contains("todo", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectConversationDetectedActionKind.RequestAction;
        }

        return ProjectConversationDetectedActionKind.InformOnly;
    }

    private static string SummaryFor(ProjectConversationDetectedActionKind actionKind)
        => actionKind switch
        {
            ProjectConversationDetectedActionKind.RequestDecision => "authorized conversation item requests a decision",
            ProjectConversationDetectedActionKind.RequestInformation => "authorized conversation item requests information",
            ProjectConversationDetectedActionKind.RequestAction => "authorized conversation item requests action",
            _ => "authorized conversation item is informational",
        };

    private static string SafeNextActionFor(ProjectConversationDetectedActionKind actionKind)
        => actionKind switch
        {
            ProjectConversationDetectedActionKind.RequestDecision => "review-task-intent-decision",
            ProjectConversationDetectedActionKind.RequestInformation => "review-task-intent-information",
            ProjectConversationDetectedActionKind.RequestAction => "review-task-intent-action",
            _ => "none",
        };

    private static TaskIntentSourceEvidenceOffset[] NormalizeEvidence(IReadOnlyList<TaskIntentSourceEvidenceOffset> offsets)
        => offsets
            .Where(static offset => !string.IsNullOrWhiteSpace(offset.EvidenceReference))
            .OrderBy(static offset => offset.EvidenceReference, StringComparer.Ordinal)
            .ThenBy(static offset => offset.StartOffset)
            .ThenBy(static offset => offset.EndOffset)
            .ThenBy(static offset => offset.Token, StringComparer.Ordinal)
            .ToArray();
}
