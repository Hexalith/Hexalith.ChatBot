namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Maps recorded post-commit audit envelopes onto the metadata-only audit-history wire response (Story 1.9 M3).
/// It projects ONLY stable codes and opaque ULID tokens — never the tenant id, command payload, or any raw
/// text — so the UI's audit-history surface is a faithful, redacted read of the post-commit envelope summary.
/// </summary>
internal static class OperationAuditHistoryHttpResults
{
    public static IResult Ok(string operationId, string auditStatus, IReadOnlyList<AuditEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(auditStatus);
        ArgumentNullException.ThrowIfNull(envelopes);

        OperationAuditHistoryWireModel model = new(
            operationId,
            auditStatus,
            [.. envelopes.Select(ToEntry)]);

        return Results.Json(model, statusCode: StatusCodes.Status200OK);
    }

    private static AuditHistoryEntryWireModel ToEntry(AuditEnvelope envelope)
        => new(
            PhaseName(envelope.Phase),
            envelope.Decision,
            envelope.ReasonCode,
            envelope.Outcome,
            envelope.StateTransition,
            envelope.RedactionDecision,
            envelope.SurfaceOrigin,
            envelope.ResourceId,
            envelope.CorrelationId,
            envelope.Timestamp);

    private static string PhaseName(AuditCommitPhase phase)
        => phase switch
        {
            AuditCommitPhase.PreCommit => "pre-commit",
            AuditCommitPhase.PostCommit => "post-commit",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported audit phase."),
        };

    private sealed record OperationAuditHistoryWireModel(
        string OperationId,
        string AuditStatus,
        IReadOnlyList<AuditHistoryEntryWireModel> Entries);

    private sealed record AuditHistoryEntryWireModel(
        string Phase,
        string Decision,
        string ReasonCode,
        string Outcome,
        string StateTransition,
        string RedactionDecision,
        string SurfaceOrigin,
        string ResourceId,
        string CorrelationId,
        DateTimeOffset RecordedAt);
}
