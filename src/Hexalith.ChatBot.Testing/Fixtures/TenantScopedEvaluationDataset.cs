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
