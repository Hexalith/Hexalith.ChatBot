using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record PublishedAssociationEvent(
    [property: JsonPropertyName("tenantId")] string? TenantId,
    [property: JsonPropertyName("domain")] string? Domain,
    [property: JsonPropertyName("aggregateId")] string? AggregateId,
    [property: JsonPropertyName("eventTypeName")] string? EventTypeName,
    [property: JsonPropertyName("sequenceNumber")] long SequenceNumber,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("intakeId")] string? IntakeId,
    [property: JsonPropertyName("sourceMailboxId")] string? SourceMailboxId,
    [property: JsonPropertyName("sourceConversationId")] string? SourceConversationId,
    [property: JsonPropertyName("sourceThreadId")] string? SourceThreadId,
    [property: JsonPropertyName("projectId")] string? ProjectId,
    [property: JsonPropertyName("projectDisplayName")] string? ProjectDisplayName,
    [property: JsonPropertyName("candidates")] IReadOnlyList<AssociationCandidate>? Candidates,
    [property: JsonPropertyName("exclusions")] IReadOnlyList<AssociationExclusion>? Exclusions,
    [property: JsonPropertyName("confidenceScore")] double ConfidenceScore,
    [property: JsonPropertyName("thresholdBand")] AssociationThresholdBand ThresholdBand,
    [property: JsonPropertyName("outcome")] AssociationScoringOutcome? Outcome,
    [property: JsonPropertyName("lifecycleState")] LifecycleState? LifecycleState,
    [property: JsonPropertyName("thresholdPolicyVersion")] string? ThresholdPolicyVersion,
    [property: JsonPropertyName("derivationKernelVersion")] string? DerivationKernelVersion,
    [property: JsonPropertyName("detectedAt")] DateTimeOffset DetectedAt,
    [property: JsonPropertyName("redactionState")] string? RedactionState,
    [property: JsonPropertyName("retentionClass")] string? RetentionClass);
