using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record PublishedParticipantResolutionEvent(
    [property: JsonPropertyName("tenantId")] string? TenantId,
    [property: JsonPropertyName("domain")] string? Domain,
    [property: JsonPropertyName("aggregateId")] string? AggregateId,
    [property: JsonPropertyName("eventTypeName")] string? EventTypeName,
    [property: JsonPropertyName("sequenceNumber")] long SequenceNumber,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("intakeId")] string? IntakeId,
    [property: JsonPropertyName("sourceMailboxId")] string? SourceMailboxId,
    [property: JsonPropertyName("sourceParticipantId")] string? SourceParticipantId,
    [property: JsonPropertyName("partyId")] string? PartyId,
    [property: JsonPropertyName("reason")] ParticipantResolutionBlockedReason? Reason,
    [property: JsonPropertyName("evidenceReference")] string? EvidenceReference,
    [property: JsonPropertyName("evidenceFingerprint")] string? EvidenceFingerprint);
