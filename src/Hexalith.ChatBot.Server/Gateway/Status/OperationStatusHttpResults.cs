using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Status;

internal static class OperationStatusHttpResults
{
    public static IResult Ok(OperationStatusRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return Results.Json(ToWire(record), statusCode: StatusCodes.Status200OK);
    }

    private static OperationStatusWireModel ToWire(OperationStatusRecord record)
        => new(
            record.OperationId,
            record.CommandId,
            record.CorrelationId,
            Lifecycle(record.LifecycleState),
            record.RetryCount,
            record.CompletionStatus,
            record.AuditStatus,
            new OperationStatusPartialOutputsWireModel(record.AcceptedAt, record.CompletionStatus, record.AuditStatus),
            record.SafeNextActions,
            record.TerminalReason,
            record.AcceptedAt,
            record.LastUpdatedAt);

    private static string Lifecycle(LifecycleState state)
        => state switch
        {
            LifecycleState.Received => "Received",
            LifecycleState.Proposed => "Proposed",
            LifecycleState.Associated => "Associated",
            LifecycleState.Rejected => "Rejected",
            LifecycleState.Deferred => "Deferred",
            LifecycleState.NeedsReview => "NeedsReview",
            LifecycleState.Failed => "Failed",
            LifecycleState.Skipped => "Skipped",
            LifecycleState.Corrected => "Corrected",
            LifecycleState.Correcting => "Correcting",
            LifecycleState.CorrectionDelayed => "Correction-delayed",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported lifecycle state."),
        };

    private sealed record OperationStatusWireModel(
        string OperationId,
        string CommandId,
        string CorrelationId,
        string LifecycleState,
        int RetryCount,
        string CompletionStatus,
        string AuditStatus,
        OperationStatusPartialOutputsWireModel PartialOutputs,
        IReadOnlyList<string> SafeNextActions,
        string? TerminalReason,
        DateTimeOffset AcceptedAt,
        DateTimeOffset LastUpdatedAt);

    private sealed record OperationStatusPartialOutputsWireModel(
        DateTimeOffset AcceptedAt,
        [property: JsonPropertyName("completionStatus")] string CompletionStatus,
        [property: JsonPropertyName("auditStatus")] string AuditStatus);
}
