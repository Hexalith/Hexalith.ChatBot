using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Tenant-scoped evaluation dataset scaffold loaded from the Story 1.13 manifest.
/// </summary>
/// <param name="SchemaVersion">The dataset schema version.</param>
/// <param name="DatasetId">The stable dataset identifier.</param>
/// <param name="Owner">The fixture owner.</param>
/// <param name="SourceClassification">The top-level source classification.</param>
/// <param name="IsScaffold">Whether the dataset is a scaffold rather than the full A9a corpus.</param>
/// <param name="CreatedAt">The fixture creation timestamp.</param>
/// <param name="RedactionReviewStatus">The redaction review status.</param>
/// <param name="TenantPartitions">The tenant partitions declared by the fixture.</param>
/// <param name="WorkflowChannels">The workflow channels covered by the fixture.</param>
/// <param name="RequiredLabels">The A9a labels covered by the fixture.</param>
/// <param name="Partitions">The dataset partitions.</param>
/// <param name="Cases">The fixture cases.</param>
/// <param name="RegressionHistory">The top-level regression-history slot.</param>
public sealed record TenantScopedEvaluationDataset(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("datasetId")] string DatasetId,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("sourceClassification")] string SourceClassification,
    [property: JsonPropertyName("isScaffold")] bool IsScaffold,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("redactionReviewStatus")] string RedactionReviewStatus,
    [property: JsonPropertyName("tenantPartitions")] IReadOnlyList<TenantScopedFixtureTenantPartition> TenantPartitions,
    [property: JsonPropertyName("workflowChannels")] IReadOnlyList<string> WorkflowChannels,
    [property: JsonPropertyName("requiredLabels")] IReadOnlyList<string> RequiredLabels,
    [property: JsonPropertyName("partitions")] IReadOnlyList<TenantScopedFixturePartition> Partitions,
    [property: JsonPropertyName("cases")] IReadOnlyList<TenantScopedFixtureCase> Cases,
    [property: JsonPropertyName("regressionHistory")] IReadOnlyList<TenantScopedFixtureRegressionHistory> RegressionHistory);

/// <summary>
/// Tenant partition metadata.
/// </summary>
/// <param name="TenantId">The synthetic tenant identifier.</param>
/// <param name="Alias">The partition alias used in diagnostics.</param>
/// <param name="Role">The tenant's fixture role.</param>
public sealed record TenantScopedFixtureTenantPartition(
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("alias")] string Alias,
    [property: JsonPropertyName("role")] string Role);

/// <summary>
/// Dataset partition metadata.
/// </summary>
/// <param name="Name">The partition name.</param>
/// <param name="Purpose">The partition purpose.</param>
public sealed record TenantScopedFixturePartition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose);

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
    [property: JsonPropertyName("expectedRiskClassification")] string? ExpectedRiskClassification = null);

/// <summary>
/// Tenant-owned resource reference.
/// </summary>
/// <param name="ResourceType">The resource type.</param>
/// <param name="TenantId">The tenant that owns the resource.</param>
/// <param name="ResourceId">The stable resource identifier.</param>
public sealed record TenantScopedFixtureResource(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("resourceId")] string ResourceId);

/// <summary>
/// Fixture expected outcome.
/// </summary>
/// <param name="State">The expected state.</param>
/// <param name="ReasonCode">The expected reason code.</param>
/// <param name="RedactionState">The expected redaction state.</param>
/// <param name="AuditExpectation">The expected audit behavior.</param>
public sealed record TenantScopedFixtureExpectedOutcome(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("reasonCode")] string ReasonCode,
    [property: JsonPropertyName("redactionState")] string RedactionState,
    [property: JsonPropertyName("auditExpectation")] string AuditExpectation);

/// <summary>
/// Redaction expectation for a fixture case.
/// </summary>
/// <param name="Mode">The expected redaction mode.</param>
/// <param name="ForbiddenPayloadClasses">Payload classes that must not appear in diagnostics.</param>
public sealed record TenantScopedFixtureRedactionExpectation(
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("forbiddenPayloadClasses")] IReadOnlyList<string> ForbiddenPayloadClasses);

/// <summary>
/// Regression history slot.
/// </summary>
/// <param name="RunId">The optional run identifier.</param>
/// <param name="Outcome">The optional run outcome.</param>
/// <param name="RecordedAt">The optional run timestamp.</param>
public sealed record TenantScopedFixtureRegressionHistory(
    [property: JsonPropertyName("runId")] string? RunId,
    [property: JsonPropertyName("outcome")] string? Outcome,
    [property: JsonPropertyName("recordedAt")] DateTimeOffset? RecordedAt);
