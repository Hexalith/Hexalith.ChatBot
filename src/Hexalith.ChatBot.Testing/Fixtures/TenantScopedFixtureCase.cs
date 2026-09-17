using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// One tenant-scoped fixture case.
/// </summary>
/// <param name="CaseId">The stable case identifier.</param>
/// <param name="TenantId">The case tenant identifier.</param>
/// <param name="Labels">The case labels.</param>
/// <param name="WorkflowChannels">The workflow channels covered by the case.</param>
/// <param name="Partitions">The partitions containing the case.</param>
/// <param name="SourceClassification">The case-level source classification.</param>
/// <param name="TenantOwnedResources">The tenant-owned resources referenced by the case.</param>
/// <param name="ExpectedOutcome">The expected outcome.</param>
/// <param name="RedactionExpectation">The redaction expectation.</param>
/// <param name="AuditExpectedFields">The required audit fields.</param>
/// <param name="RegressionHistory">The case-level regression-history slot.</param>
/// <param name="KernelVersion">Reserved derivation kernel version.</param>
/// <param name="ConfidenceScore">Reserved confidence score.</param>
/// <param name="ThresholdBand">Reserved threshold band.</param>
/// <param name="EvidenceRefs">Reserved evidence references.</param>
/// <param name="PolicySnapshotId">Reserved policy snapshot identifier.</param>
/// <param name="IdempotencyKey">Reserved idempotency key.</param>
/// <param name="StateTransition">Reserved state transition.</param>
/// <param name="EffectSurface">Reserved risk-classifier effect surface.</param>
/// <param name="RequesterAuthorityClass">Reserved requester authority class.</param>
/// <param name="ExpectedRiskClassification">Reserved expected risk classification.</param>
/// <param name="ClassifierDisagreementOutcome">Reserved classifier-disagreement calibration outcome.</param>
/// <param name="TaskIntentExpectedLabel">Expected task-intent evaluation label for scaffold quality reporting.</param>
/// <param name="TaskIntentPredictedLabel">Predicted task-intent evaluation label for scaffold quality reporting.</param>
/// <param name="TaskIntentReviewOutcome">Expected Story 4.2 review outcome/disposition scaffold label.</param>
public sealed record TenantScopedFixtureCase(
    [property: JsonPropertyName("caseId")] string CaseId,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("labels")] IReadOnlyList<string> Labels,
    [property: JsonPropertyName("workflowChannels")] IReadOnlyList<string> WorkflowChannels,
    [property: JsonPropertyName("partitions")] IReadOnlyList<string> Partitions,
    [property: JsonPropertyName("sourceClassification")] string SourceClassification,
    [property: JsonPropertyName("tenantOwnedResources")] IReadOnlyList<TenantScopedFixtureResource> TenantOwnedResources,
    [property: JsonPropertyName("expectedOutcome")] TenantScopedFixtureExpectedOutcome? ExpectedOutcome,
    [property: JsonPropertyName("redactionExpectation")] TenantScopedFixtureRedactionExpectation? RedactionExpectation,
    [property: JsonPropertyName("auditExpectedFields")] IReadOnlyList<string> AuditExpectedFields,
    [property: JsonPropertyName("regressionHistory")] IReadOnlyList<TenantScopedFixtureRegressionHistory> RegressionHistory,
    [property: JsonPropertyName("kernelVersion")] string? KernelVersion = null,
    [property: JsonPropertyName("confidenceScore")] double? ConfidenceScore = null,
    [property: JsonPropertyName("thresholdBand")] string? ThresholdBand = null,
    [property: JsonPropertyName("evidenceRefs")] IReadOnlyList<string>? EvidenceRefs = null,
    [property: JsonPropertyName("policySnapshotId")] string? PolicySnapshotId = null,
    [property: JsonPropertyName("idempotencyKey")] string? IdempotencyKey = null,
    [property: JsonPropertyName("stateTransition")] string? StateTransition = null,
    [property: JsonPropertyName("effectSurface")] string? EffectSurface = null,
    [property: JsonPropertyName("requesterAuthorityClass")] string? RequesterAuthorityClass = null,
    [property: JsonPropertyName("expectedRiskClassification")] string? ExpectedRiskClassification = null,
    [property: JsonPropertyName("classifierDisagreementOutcome")] string? ClassifierDisagreementOutcome = null,
    [property: JsonPropertyName("taskIntentExpectedLabel")] string? TaskIntentExpectedLabel = null,
    [property: JsonPropertyName("taskIntentPredictedLabel")] string? TaskIntentPredictedLabel = null,
    [property: JsonPropertyName("taskIntentReviewOutcome")] string? TaskIntentReviewOutcome = null);
