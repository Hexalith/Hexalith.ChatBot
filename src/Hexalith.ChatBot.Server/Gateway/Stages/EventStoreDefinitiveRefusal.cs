using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Problems;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Classifies the EventStore submission failures that are authoritative proof the submitted command was not committed.
/// </summary>
/// <remarks>
/// EventStore answers <c>POST /api/v1/commands</c> synchronously. A typed domain rejection (the aggregate evaluated
/// the command and rejected it) and back-pressure (the aggregate refused to accept another pending command) both prove
/// that the command's effect did not commit, and EventStore deduplicates any later resubmission of the same message ID.
/// Every other failure — transport, sidecar or time-out faults, an unknown idempotency outcome, an identity or
/// concurrency conflict, an unparseable response — remains uncertain and must keep its dispatch ownership.
/// </remarks>
internal static class EventStoreDefinitiveRefusal
{
    /// <summary>The problem type prefix EventStore emits for a typed domain rejection.</summary>
    internal const string DomainRejectionTypePrefix = "https://hexalith.io/problems/domain-rejections/";

    /// <summary>The problem type EventStore emits when an aggregate is under back-pressure.</summary>
    internal const string BackpressureExceededType = "https://hexalith.io/problems/backpressure-exceeded";

    /// <summary>Returns whether the failure proves EventStore refused the submission without committing it.</summary>
    /// <param name="exception">The failure raised by the EventStore gateway client.</param>
    /// <returns><see langword="true"/> for a typed domain rejection or back-pressure refusal.</returns>
    public static bool IsDefinitive(Exception exception)
        => exception is EventStoreGatewayException refusal && (IsDomainRejection(refusal) || IsBackpressure(refusal));

    private static bool IsDomainRejection(EventStoreGatewayException refusal)
        => refusal.StatusCode is StatusCodes.Status404NotFound or StatusCodes.Status409Conflict or StatusCodes.Status422UnprocessableEntity
            && refusal.Type is { } type
            && type.StartsWith(DomainRejectionTypePrefix, StringComparison.Ordinal)
            && refusal.Extensions.TryGetValue(GatewayProblemDetailsExtensions.RejectionType, out JsonElement rejectionType)
            && rejectionType.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(rejectionType.GetString());

    private static bool IsBackpressure(EventStoreGatewayException refusal)
        => refusal.StatusCode == StatusCodes.Status429TooManyRequests
            && string.Equals(refusal.Type, BackpressureExceededType, StringComparison.Ordinal);
}
