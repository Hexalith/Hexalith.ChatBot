using System.Diagnostics.CodeAnalysis;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Validates the tenant-scoped evaluation fixture scaffold.
/// </summary>
public static class TenantScopedFixtureValidator
{
    /// <summary>
    /// Validates a dataset and throws a metadata-only exception on the first failure.
    /// </summary>
    /// <param name="dataset">The dataset to validate.</param>
    public static void Validate(TenantScopedEvaluationDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        // Fail closed with a controlled metadata-only message (not a raw ArgumentNull/NullReference from LINQ internals)
        // when a required collection key is absent from the manifest JSON. Empty-collection rules are handled later.
        RequireListPresent(dataset.RequiredLabels, "requiredLabels");
        RequireListPresent(dataset.WorkflowChannels, "workflowChannels");
        RequireListPresent(dataset.Partitions, "partitions");
        RequireListPresent(dataset.TenantPartitions, "tenantPartitions");
        RequireListPresent(dataset.Cases, "cases");

        ValidateHeader(dataset);
        ValidateExactSet("requiredLabels", dataset.RequiredLabels, TenantScopedFixtureConstants.RequiredLabels);
        ValidateExactSet("workflowChannels", dataset.WorkflowChannels, TenantScopedFixtureConstants.RequiredWorkflowChannels);
        ValidateExactSet("partitions", dataset.Partitions.Select(static partition => partition.Name), TenantScopedFixtureConstants.RequiredPartitions);

        Dictionary<string, TenantScopedFixtureTenantPartition> tenants = ValidateTenantPartitions(dataset);
        HashSet<string> partitions = dataset.Partitions.Select(static partition => partition.Name).ToHashSet(StringComparer.Ordinal);
        ValidateCases(dataset, tenants, partitions);
        ValidateCoverage(dataset, partitions);
    }

    private static void ValidateHeader(TenantScopedEvaluationDataset dataset)
    {
        RequireText(dataset.SchemaVersion, "schemaVersion");
        RequireText(dataset.DatasetId, "datasetId");
        RequireText(dataset.Owner, "owner");
        RequireText(dataset.RedactionReviewStatus, "redactionReviewStatus");

        if (!IsAllowedSourceClassification(dataset.SourceClassification))
        {
            Fail($"Dataset '{dataset.DatasetId}' sourceClassification '{dataset.SourceClassification}' is not allowed.");
        }

        if (!dataset.IsScaffold)
        {
            Fail($"Dataset '{dataset.DatasetId}' must be marked as scaffold and must not claim the full A9a corpus.");
        }

        if (dataset.TenantPartitions.Count == 0)
        {
            Fail($"Dataset '{dataset.DatasetId}' has zero tenant partitions.");
        }

        if (dataset.Cases.Count == 0)
        {
            Fail($"Dataset '{dataset.DatasetId}' has zero fixture cases.");
        }

        if (dataset.RegressionHistory is null)
        {
            Fail($"Dataset '{dataset.DatasetId}' is missing the top-level regression-history slot.");
        }
    }

    private static Dictionary<string, TenantScopedFixtureTenantPartition> ValidateTenantPartitions(TenantScopedEvaluationDataset dataset)
    {
        Dictionary<string, TenantScopedFixtureTenantPartition> tenants = new(StringComparer.Ordinal);
        foreach (TenantScopedFixtureTenantPartition tenant in dataset.TenantPartitions)
        {
            RequireText(tenant.TenantId, "tenantId");
            RequireText(tenant.Alias, "tenantPartition");
            RequireText(tenant.Role, "tenantRole");

            if (!tenants.TryAdd(tenant.TenantId, tenant))
            {
                Fail($"Tenant partition '{tenant.Alias}' has a duplicate tenant ID.");
            }
        }

        return tenants;
    }

    private static void ValidateCases(
        TenantScopedEvaluationDataset dataset,
        IReadOnlyDictionary<string, TenantScopedFixtureTenantPartition> tenants,
        IReadOnlySet<string> partitions)
    {
        HashSet<string> caseIds = new(StringComparer.Ordinal);
        Dictionary<string, string> unscopedResourceOwners = new(StringComparer.Ordinal);

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases)
        {
            CaseIdentity identity = CaseIdentity.From(fixtureCase);
            RequireText(fixtureCase.CaseId, "caseId", identity);
            if (!caseIds.Add(fixtureCase.CaseId))
            {
                Fail(identity, "duplicate case ID");
            }

            RequireText(fixtureCase.TenantId, "tenantId", identity);
            if (!tenants.ContainsKey(fixtureCase.TenantId))
            {
                Fail(identity, "unknown tenant reference");
            }

            ValidateMembership("label", fixtureCase.Labels, dataset.RequiredLabels, identity);
            ValidateMembership("channel", fixtureCase.WorkflowChannels, dataset.WorkflowChannels, identity);
            ValidateMembership("partition", fixtureCase.Partitions, partitions, identity);
            ValidateCaseSourceClassification(fixtureCase, identity);
            ValidateExpectedOutcome(fixtureCase, identity);
            ValidateTenantOwnedResources(fixtureCase, tenants, unscopedResourceOwners, identity);
            ValidateConfidenceFields(fixtureCase, identity);
            ValidateRiskFields(fixtureCase, identity);
        }
    }

    private static void ValidateCaseSourceClassification(TenantScopedFixtureCase fixtureCase, CaseIdentity identity)
    {
        if (!IsAllowedSourceClassification(fixtureCase.SourceClassification))
        {
            Fail(identity, "source classification is not allowed");
        }
    }

    private static void ValidateExpectedOutcome(TenantScopedFixtureCase fixtureCase, CaseIdentity identity)
    {
        TenantScopedFixtureExpectedOutcome? expectedOutcome = fixtureCase.ExpectedOutcome;
        if (expectedOutcome is null)
        {
            Fail(identity, "missing expected outcome");
        }

        RequireText(expectedOutcome.State, "expectedOutcome.state", identity);
        RequireText(expectedOutcome.ReasonCode, "expectedOutcome.reasonCode", identity);
        RequireText(expectedOutcome.RedactionState, "expectedOutcome.redactionState", identity);
        RequireText(expectedOutcome.AuditExpectation, "expectedOutcome.auditExpectation", identity);

        TenantScopedFixtureRedactionExpectation? redactionExpectation = fixtureCase.RedactionExpectation;
        if (redactionExpectation is null)
        {
            Fail(identity, "missing redaction expectation");
        }

        RequireText(redactionExpectation.Mode, "redactionExpectation.mode", identity);
        RequireNonEmpty(redactionExpectation.ForbiddenPayloadClasses, "redactionExpectation.forbiddenPayloadClasses", identity);
        RequireNonEmpty(fixtureCase.AuditExpectedFields, "auditExpectedFields", identity);

        if (fixtureCase.RegressionHistory is null)
        {
            Fail(identity, "missing regression-history slot");
        }

        if (fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal))
        {
            RequireText(fixtureCase.IdempotencyKey, "idempotencyKey", identity);
            RequireText(fixtureCase.StateTransition, "stateTransition", identity);
        }
    }

    private static void ValidateTenantOwnedResources(
        TenantScopedFixtureCase fixtureCase,
        IReadOnlyDictionary<string, TenantScopedFixtureTenantPartition> tenants,
        IDictionary<string, string> unscopedResourceOwners,
        CaseIdentity identity)
    {
        RequireNonEmpty(fixtureCase.TenantOwnedResources, "tenantOwnedResources", identity);

        foreach (TenantScopedFixtureResource resource in fixtureCase.TenantOwnedResources)
        {
            RequireText(resource.ResourceType, "resourceType", identity);
            RequireText(resource.TenantId, "resource.tenantId", identity);
            RequireText(resource.ResourceId, "resourceId", identity);

            if (!tenants.ContainsKey(resource.TenantId))
            {
                Fail(identity, "unknown tenant resource reference");
            }

            if (string.Equals(fixtureCase.TenantId, resource.TenantId, StringComparison.Ordinal)
                || fixtureCase.Labels.Contains("cross-tenant-reference", StringComparer.Ordinal)
                || fixtureCase.Labels.Contains("unauthorized-project", StringComparer.Ordinal))
            {
                // Own-tenant resources are normal; foreign resources are only allowed for negative cases.
            }
            else
            {
                Fail(identity, "foreign tenant resource outside a negative case");
            }

            if (!resource.ResourceId.Contains(':', StringComparison.Ordinal))
            {
                if (unscopedResourceOwners.TryGetValue(resource.ResourceId, out string? existingTenant)
                    && !string.Equals(existingTenant, resource.TenantId, StringComparison.Ordinal))
                {
                    Fail(identity, "duplicate unscoped resource ID");
                }

                unscopedResourceOwners[resource.ResourceId] = resource.TenantId;
            }
        }
    }

    private static void ValidateConfidenceFields(TenantScopedFixtureCase fixtureCase, CaseIdentity identity)
    {
        if (fixtureCase.ConfidenceScore.HasValue
            && (!double.IsFinite(fixtureCase.ConfidenceScore.Value)
                || fixtureCase.ConfidenceScore.Value < 0.0
                || fixtureCase.ConfidenceScore.Value > 1.0))
        {
            Fail(identity, "confidence score outside [0.0, 1.0]");
        }

        if (fixtureCase.ThresholdBand is not null
            && !TenantScopedFixtureConstants.ThresholdBands.Contains(fixtureCase.ThresholdBand, StringComparer.Ordinal))
        {
            Fail(identity, "unknown threshold band");
        }
    }

    private static void ValidateRiskFields(TenantScopedFixtureCase fixtureCase, CaseIdentity identity)
    {
        if (!fixtureCase.Labels.Contains("risky-ai-candidate", StringComparer.Ordinal))
        {
            return;
        }

        RequireText(fixtureCase.EffectSurface, "effectSurface", identity);
        RequireText(fixtureCase.RequesterAuthorityClass, "requesterAuthorityClass", identity);
        RequireText(fixtureCase.ExpectedRiskClassification, "expectedRiskClassification", identity);
    }

    private static void ValidateCoverage(TenantScopedEvaluationDataset dataset, IReadOnlySet<string> partitions)
    {
        foreach (string label in dataset.RequiredLabels)
        {
            if (!dataset.Cases.Any(fixtureCase => fixtureCase.Labels.Contains(label, StringComparer.Ordinal)))
            {
                Fail($"Dataset '{dataset.DatasetId}' label '{label}' has zero cases.");
            }
        }

        foreach (string channel in dataset.WorkflowChannels)
        {
            if (!dataset.Cases.Any(fixtureCase => fixtureCase.WorkflowChannels.Contains(channel, StringComparer.Ordinal)))
            {
                Fail($"Dataset '{dataset.DatasetId}' channel '{channel}' has zero cases.");
            }
        }

        foreach (string partition in partitions)
        {
            if (!dataset.Cases.Any(fixtureCase => fixtureCase.Partitions.Contains(partition, StringComparer.Ordinal)))
            {
                Fail($"Dataset '{dataset.DatasetId}' partition '{partition}' has zero cases.");
            }
        }

        // AC6 non-vacuity: every "own" tenant partition must own at least one case. Foreign tenants intentionally
        // own zero cases (AC3 keeps cross-tenant references as negative-only resource references, never case owners).
        foreach (TenantScopedFixtureTenantPartition tenant in dataset.TenantPartitions.Where(
            static tenant => string.Equals(tenant.Role, TenantScopedFixtureConstants.OwnTenantRole, StringComparison.Ordinal)))
        {
            if (!dataset.Cases.Any(fixtureCase => string.Equals(fixtureCase.TenantId, tenant.TenantId, StringComparison.Ordinal)))
            {
                Fail($"Dataset '{dataset.DatasetId}' own tenant partition '{tenant.Alias}' has zero owning cases.");
            }
        }
    }

    private static void ValidateExactSet(string name, IEnumerable<string> actual, IReadOnlyList<string> expected)
    {
        List<string> actualList = [.. actual];
        if (actualList.Count != expected.Count || actualList.Distinct(StringComparer.Ordinal).Count() != actualList.Count)
        {
            Fail($"Dataset field '{name}' must contain each required value exactly once.");
        }

        foreach (string expectedValue in expected)
        {
            if (!actualList.Contains(expectedValue, StringComparer.Ordinal))
            {
                Fail($"Dataset field '{name}' is missing required value '{expectedValue}'.");
            }
        }
    }

    private static void ValidateMembership(
        string fieldName,
        IReadOnlyList<string> values,
        IEnumerable<string> allowed,
        CaseIdentity identity)
    {
        RequireNonEmpty(values, fieldName, identity);
        HashSet<string> allowedSet = allowed.ToHashSet(StringComparer.Ordinal);
        foreach (string value in values)
        {
            RequireText(value, fieldName, identity);
            if (!allowedSet.Contains(value))
            {
                Fail(identity, $"unknown {fieldName}");
            }
        }
    }

    private static void RequireListPresent<T>(IReadOnlyCollection<T>? values, string fieldName)
    {
        if (values is null)
        {
            Fail($"Dataset field '{fieldName}' is required.");
        }
    }

    private static void RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Fail($"Dataset field '{fieldName}' is required.");
        }
    }

    private static void RequireText(string? value, string fieldName, CaseIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Fail(identity, $"missing {fieldName}");
        }
    }

    private static void RequireNonEmpty<T>(IReadOnlyCollection<T>? values, string fieldName, CaseIdentity identity)
    {
        if (values is null || values.Count == 0)
        {
            Fail(identity, $"missing {fieldName}");
        }
    }

    private static bool IsAllowedSourceClassification(string? sourceClassification)
        => string.Equals(sourceClassification, TenantScopedFixtureConstants.SyntheticSourceClassification, StringComparison.Ordinal)
        || string.Equals(sourceClassification, TenantScopedFixtureConstants.RedactedSourceClassification, StringComparison.Ordinal)
        || string.Equals(sourceClassification, TenantScopedFixtureConstants.ConsentedSourceClassification, StringComparison.Ordinal);

    [DoesNotReturn]
    private static void Fail(string message)
        => throw new TenantScopedFixtureValidationException(message);

    [DoesNotReturn]
    private static void Fail(CaseIdentity identity, string reason)
        => Fail($"Fixture case '{identity.CaseId}' label '{identity.Label}' channel '{identity.Channel}' partition '{identity.Partition}' failed validation rule: {reason}.");

    private readonly record struct CaseIdentity(string CaseId, string Label, string Channel, string Partition)
    {
        public static CaseIdentity From(TenantScopedFixtureCase fixtureCase)
            => new(
                string.IsNullOrWhiteSpace(fixtureCase.CaseId) ? "<missing>" : fixtureCase.CaseId,
                (fixtureCase.Labels ?? []).FirstOrDefault(static label => !string.IsNullOrWhiteSpace(label)) ?? "<missing>",
                (fixtureCase.WorkflowChannels ?? []).FirstOrDefault(static channel => !string.IsNullOrWhiteSpace(channel)) ?? "<missing>",
                (fixtureCase.Partitions ?? []).FirstOrDefault(static partition => !string.IsNullOrWhiteSpace(partition)) ?? "<missing>");
    }
}
