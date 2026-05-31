using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// The subset of the EventStore-published event envelope (delivered as the CloudEvent <c>data</c> on the
/// <c>chatbot-pubsub</c> topic) that the governed-operation projection consumes. Every field here is
/// EventStore-stamped metadata produced by the trusted persist→publish path — never a value the original
/// command submitter controls — so the projection derives tenant and source version from this envelope only,
/// not from any request body (tenant isolation / S7). All members are nullable because the wire payload is
/// untrusted JSON; the translator validates them before projecting.
/// </summary>
/// <param name="TenantId">The owning tenant, stamped by EventStore from the authenticated command.</param>
/// <param name="Domain">The EventStore domain name (must be <c>chatbot</c> to be projected here).</param>
/// <param name="AggregateId">The governed-note aggregate ULID (the note id).</param>
/// <param name="EventTypeName">The fully-qualified event type name stamped by EventStore.</param>
/// <param name="SequenceNumber">The aggregate sequence number — the order-tolerant source version stamp.</param>
/// <param name="CorrelationId">The correlation id carried through the spine.</param>
/// <param name="MessageId">The originating message ULID (idempotency evidence).</param>
/// <param name="Timestamp">The UTC instant the event was persisted.</param>
public sealed record PublishedGovernedOperationEvent(
    [property: JsonPropertyName("tenantId")] string? TenantId,
    [property: JsonPropertyName("domain")] string? Domain,
    [property: JsonPropertyName("aggregateId")] string? AggregateId,
    [property: JsonPropertyName("eventTypeName")] string? EventTypeName,
    [property: JsonPropertyName("sequenceNumber")] long SequenceNumber,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);
