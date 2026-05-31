using System.Reflection;
using System.Runtime.Serialization;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Testing.Fixtures;

using Shouldly;

namespace Hexalith.ChatBot.Testing.Tests.Fixtures;

public sealed class TenantScopedFixtureManifestTests
{
    [Fact]
    public void EmbeddedManifestShouldLoadAndValidate()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        dataset.DatasetId.ShouldBe("story-1-13-tenant-scoped-evaluation-scaffold");
        dataset.IsScaffold.ShouldBeTrue();
        dataset.RequiredLabels.ShouldBe(TenantScopedFixtureConstants.RequiredLabels);
        dataset.WorkflowChannels.ShouldBe(TenantScopedFixtureConstants.RequiredWorkflowChannels);
        dataset.Partitions.Select(static partition => partition.Name).ShouldBe(TenantScopedFixtureConstants.RequiredPartitions);
        dataset.RegressionHistory.ShouldNotBeNull();
    }

    [Fact]
    public void MissingManifestResourceShouldFailClosed()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            TenantScopedFixtureManifestLoader.LoadFromEmbeddedResource(
                typeof(TenantScopedFixtureManifestTests).Assembly,
                "missing-story-1-13-resource.json"));

        exception.Message.ShouldContain("missing-story-1-13-resource.json");
        exception.Message.ShouldContain("cannot run");
    }

    [Fact]
    public void ManifestShouldCoverEveryRequiredLabelChannelAndPartition()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        foreach (string label in TenantScopedFixtureConstants.RequiredLabels)
        {
            dataset.Cases.Any(fixtureCase => fixtureCase.Labels.Contains(label, StringComparer.Ordinal))
                .ShouldBeTrue($"label '{label}' has zero cases");
        }

        foreach (string channel in TenantScopedFixtureConstants.RequiredWorkflowChannels)
        {
            dataset.Cases.Any(fixtureCase => fixtureCase.WorkflowChannels.Contains(channel, StringComparer.Ordinal))
                .ShouldBeTrue($"channel '{channel}' has zero cases");
        }

        foreach (string partition in TenantScopedFixtureConstants.RequiredPartitions)
        {
            dataset.Cases.Any(fixtureCase => fixtureCase.Partitions.Contains(partition, StringComparer.Ordinal))
                .ShouldBeTrue($"partition '{partition}' has zero cases");
        }
    }

    [Fact]
    public void EveryCaseShouldDeclareExpectedOutcomeRedactionAuditAndRegressionSlots()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases)
        {
            fixtureCase.ExpectedOutcome.ShouldNotBeNull(fixtureCase.CaseId);
            fixtureCase.ExpectedOutcome.RedactionState.ShouldNotBeNullOrWhiteSpace(fixtureCase.CaseId);
            fixtureCase.ExpectedOutcome.AuditExpectation.ShouldNotBeNullOrWhiteSpace(fixtureCase.CaseId);
            fixtureCase.RedactionExpectation.ShouldNotBeNull(fixtureCase.CaseId);
            fixtureCase.RedactionExpectation.ForbiddenPayloadClasses.ShouldNotBeEmpty(fixtureCase.CaseId);
            fixtureCase.AuditExpectedFields.ShouldNotBeEmpty(fixtureCase.CaseId);
            fixtureCase.RegressionHistory.ShouldNotBeNull(fixtureCase.CaseId);
        }
    }

    [Fact]
    public void EveryTenantOwnedResourceShouldBeTenantScopedAndKnown()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();
        HashSet<string> tenantIds = dataset.TenantPartitions
            .Select(static partition => partition.TenantId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases)
        {
            fixtureCase.TenantOwnedResources.ShouldNotBeEmpty(fixtureCase.CaseId);
            foreach (TenantScopedFixtureResource resource in fixtureCase.TenantOwnedResources)
            {
                tenantIds.ShouldContain(resource.TenantId);
                resource.ResourceId.ShouldContain(resource.TenantId, Case.Sensitive, fixtureCase.CaseId);
            }
        }
    }

    [Fact]
    public void CommandExecutionCasesShouldDeclareIdempotencyAndStateTransitionFacts()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases.Where(static fixtureCase =>
            fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal)))
        {
            fixtureCase.IdempotencyKey.ShouldNotBeNullOrWhiteSpace(fixtureCase.CaseId);
            fixtureCase.StateTransition.ShouldNotBeNullOrWhiteSpace(fixtureCase.CaseId);
        }
    }

    [Fact]
    public void ConfidenceThresholdAndRiskClassifierFieldsShouldBeReservedAndValidated()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases.Where(static fixtureCase => fixtureCase.ConfidenceScore.HasValue))
        {
            fixtureCase.ConfidenceScore.GetValueOrDefault().ShouldBeInRange(0.0, 1.0, fixtureCase.CaseId);
            fixtureCase.ThresholdBand.ShouldNotBeNullOrWhiteSpace(fixtureCase.CaseId);
            TenantScopedFixtureConstants.ThresholdBands.ShouldContain(fixtureCase.ThresholdBand);
        }

        TenantScopedFixtureCase riskyCase = dataset.Cases.Single(static fixtureCase =>
            fixtureCase.Labels.Contains("risky-ai-candidate", StringComparer.Ordinal));
        riskyCase.EffectSurface.ShouldNotBeNullOrWhiteSpace();
        riskyCase.RequesterAuthorityClass.ShouldNotBeNullOrWhiteSpace();
        riskyCase.ExpectedRiskClassification.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ScaffoldShouldNotClaimTheFullA9aCorpus()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        dataset.IsScaffold.ShouldBeTrue();
        dataset.Cases.Count.ShouldBeLessThan(500);
    }

    [Fact]
    public void ThresholdBandConstantsShouldMatchTheContractEnumWireValues()
    {
        // Computed independently from the canonical contract enum (NOT via the constant under test) so the reserved
        // thresholdBand vocabulary provably aligns with Hexalith.ChatBot.Contracts.Enums.ThresholdBand (Story 1.13 AC7).
        string[] contractBands = [.. Enum.GetValues<ThresholdBand>().Select(static band =>
            typeof(ThresholdBand).GetField(band.ToString())!.GetCustomAttribute<EnumMemberAttribute>()!.Value!)];

        TenantScopedFixtureConstants.ThresholdBands.ShouldBe(contractBands);
        contractBands.ShouldBe(["below", "within", "above", "critical"]);
    }

    [Fact]
    public void EveryThresholdBandInTheManifestShouldBeAContractEnumValue()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases.Where(static fixtureCase => fixtureCase.ThresholdBand is not null))
        {
            TenantScopedFixtureConstants.ThresholdBands.ShouldContain(fixtureCase.ThresholdBand!, fixtureCase.CaseId);
        }
    }

    [Fact]
    public void ReservedExtensionFieldsShouldRoundTripFromTheManifest()
    {
        TenantScopedEvaluationDataset dataset = LoadDataset();

        TenantScopedFixtureCase deterministic = dataset.Cases.Single(static fixtureCase => fixtureCase.CaseId == "case-deterministic-match-001");
        deterministic.KernelVersion.ShouldBe("fixture-scaffold-v1");
        deterministic.EvidenceRefs.ShouldNotBeNull();
        deterministic.EvidenceRefs!.ShouldNotBeEmpty();

        TenantScopedFixtureCase unauthorized = dataset.Cases.Single(static fixtureCase => fixtureCase.CaseId == "case-unauthorized-project-001");
        unauthorized.PolicySnapshotId.ShouldBe("policy:tenant-alpha:scaffold-001");
    }

    [Theory]
    [MemberData(nameof(InvalidDatasets))]
    public void InvalidManifestsShouldBeRejectedWithMetadataOnlyDiagnostics(
        string scenario,
        TenantScopedEvaluationDataset dataset,
        string expectedDiagnostic)
    {
        TenantScopedFixtureValidationException exception = Should.Throw<TenantScopedFixtureValidationException>(() =>
            TenantScopedFixtureValidator.Validate(dataset));

        exception.Message.Contains(expectedDiagnostic, StringComparison.Ordinal).ShouldBeTrue(scenario);

        // Metadata-only contract: NONE of the manifest's declared forbidden payload-class tokens may appear in a
        // diagnostic message (the validator must name only case ID / label / channel / partition / rule).
        foreach (string forbiddenToken in ForbiddenPayloadClassTokens())
        {
            exception.Message.Contains(forbiddenToken, StringComparison.Ordinal).ShouldBeFalse($"{scenario}:{forbiddenToken}");
        }
    }

    private static IEnumerable<string> ForbiddenPayloadClassTokens()
        => LoadDataset().Cases
            .Where(static fixtureCase => fixtureCase.RedactionExpectation is not null)
            .SelectMany(static fixtureCase => fixtureCase.RedactionExpectation!.ForbiddenPayloadClasses)
            .Distinct(StringComparer.Ordinal);

    public static IEnumerable<object[]> InvalidDatasets()
    {
        TenantScopedEvaluationDataset valid = LoadDataset();
        TenantScopedFixtureCase first = valid.Cases[0];
        TenantScopedFixtureCase second = valid.Cases[1];

        yield return ["blank tenant", valid with { Cases = Replace(valid, first, first with { TenantId = string.Empty }) }, "missing tenantId"];
        yield return ["empty labels", valid with { Cases = Replace(valid, first, first with { Labels = [] }) }, "missing label"];
        yield return ["empty channels", valid with { Cases = Replace(valid, first, first with { WorkflowChannels = [] }) }, "missing channel"];
        yield return ["empty tenant partitions", valid with { TenantPartitions = [] }, "zero tenant partitions"];
        yield return ["duplicate case IDs", valid with { Cases = Replace(valid, second, second with { CaseId = first.CaseId }) }, "duplicate case ID"];
        yield return [
            "duplicate unscoped resource IDs",
            valid with
            {
                Cases =
                [
                    first with
                    {
                        TenantOwnedResources =
                        [
                            new TenantScopedFixtureResource("project", "tenant-alpha", "shared-unscoped-id"),
                        ],
                    },
                    second with
                    {
                        TenantId = "tenant-beta",
                        TenantOwnedResources =
                        [
                            new TenantScopedFixtureResource("project", "tenant-beta", "shared-unscoped-id"),
                        ],
                    },
                    .. valid.Cases.Skip(2),
                ],
            },
            "duplicate unscoped resource ID"];
        yield return [
            "unknown tenant",
            valid with { Cases = Replace(valid, first, first with { TenantId = "tenant-unknown" }) },
            "unknown tenant reference"];
        yield return [
            "unknown resource tenant",
            valid with
            {
                Cases = Replace(
                    valid,
                    first,
                    first with
                    {
                        TenantOwnedResources =
                        [
                            new TenantScopedFixtureResource("project", "tenant-unknown", "tenant-unknown:project:001"),
                        ],
                    }),
            },
            "unknown tenant resource reference"];
        yield return [
            "missing expected outcome",
            valid with { Cases = Replace(valid, first, first with { ExpectedOutcome = null }) },
            "missing expected outcome"];
        yield return [
            "missing expected redaction state",
            valid with
            {
                Cases = Replace(
                    valid,
                    first,
                    first with
                    {
                        ExpectedOutcome = first.ExpectedOutcome! with { RedactionState = string.Empty },
                    }),
            },
            "missing expectedOutcome.redactionState"];
        yield return [
            "missing expected audit expectation",
            valid with
            {
                Cases = Replace(
                    valid,
                    first,
                    first with
                    {
                        ExpectedOutcome = first.ExpectedOutcome! with { AuditExpectation = string.Empty },
                    }),
            },
            "missing expectedOutcome.auditExpectation"];
        yield return [
            "missing redaction expectation",
            valid with { Cases = Replace(valid, first, first with { RedactionExpectation = null }) },
            "missing redaction expectation"];
        yield return [
            "missing audit expected fields",
            valid with { Cases = Replace(valid, first, first with { AuditExpectedFields = [] }) },
            "missing auditExpectedFields"];
        yield return [
            "missing regression history slot",
            valid with { Cases = Replace(valid, first, first with { RegressionHistory = null! }) },
            "missing regression-history slot"];
        yield return [
            "missing command idempotency fact",
            valid with
            {
                Cases = Replace(
                    valid,
                    valid.Cases.Single(static fixtureCase => fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal)),
                    valid.Cases.Single(static fixtureCase => fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal)) with
                    {
                        IdempotencyKey = string.Empty,
                    }),
            },
            "missing idempotencyKey"];
        yield return [
            "missing command state transition fact",
            valid with
            {
                Cases = Replace(
                    valid,
                    valid.Cases.Single(static fixtureCase => fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal)),
                    valid.Cases.Single(static fixtureCase => fixtureCase.WorkflowChannels.Contains("command-execution", StringComparer.Ordinal)) with
                    {
                        StateTransition = string.Empty,
                    }),
            },
            "missing stateTransition"];
        yield return [
            "production classification",
            valid with { Cases = Replace(valid, first, first with { SourceClassification = "production" }) },
            "source classification is not allowed"];
        yield return [
            "bad confidence",
            valid with { Cases = Replace(valid, first, first with { ConfidenceScore = 1.5 }) },
            "confidence score outside"];
        yield return [
            "bad threshold",
            valid with { Cases = Replace(valid, first, first with { ThresholdBand = "unknown" }) },
            "unknown threshold band"];

        // Missing (null) required arrays must fail closed with a controlled metadata-only message, not a raw
        // ArgumentNullException/NullReferenceException from LINQ internals.
        yield return [
            "null case labels",
            valid with { Cases = Replace(valid, first, first with { Labels = null! }) },
            "missing label"];
        yield return ["null dataset required labels", valid with { RequiredLabels = null! }, "requiredLabels' is required"];
        yield return ["null dataset cases", valid with { Cases = null! }, "cases' is required"];

        // Validator-bypassing negative controls for coverage rules (these are NOT exercised by the embedded fixture,
        // so without them ValidateCoverage / the ForbiddenPayloadClasses non-empty rule would have no failing test).
        yield return [
            "label with zero cases",
            valid with { Cases = [.. valid.Cases.Where(static fixtureCase => !fixtureCase.Labels.Contains("inbound-authenticity-anomaly", StringComparer.Ordinal))] },
            "label 'inbound-authenticity-anomaly' has zero cases"];
        yield return [
            "empty forbidden payload classes",
            valid with { Cases = Replace(valid, first, first with { RedactionExpectation = first.RedactionExpectation! with { ForbiddenPayloadClasses = [] } }) },
            "missing redactionExpectation.forbiddenPayloadClasses"];
        yield return [
            "own tenant partition with zero cases",
            valid with { TenantPartitions = [.. valid.TenantPartitions, new TenantScopedFixtureTenantPartition("tenant-gamma", "gamma", "own")] },
            "own tenant partition 'gamma' has zero owning cases"];
    }

    private static TenantScopedEvaluationDataset LoadDataset()
        => TenantScopedFixtureManifestLoader.LoadFromEmbeddedResource(Assembly.GetExecutingAssembly());

    private static IReadOnlyList<TenantScopedFixtureCase> Replace(
        TenantScopedEvaluationDataset dataset,
        TenantScopedFixtureCase original,
        TenantScopedFixtureCase replacement)
        => dataset.Cases.Select(fixtureCase => ReferenceEquals(fixtureCase, original) ? replacement : fixtureCase).ToArray();
}
