using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Testing.Fixtures;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 1.13 conformance checks for the tenant-scoped fixture scaffold and sandbox harness.
/// </summary>
public sealed class TenantScopedFixtureHarnessTests
{
    [Fact]
    public void ConformanceAssemblyShouldLoadTheEmbeddedTenantScopedManifest()
    {
        TenantScopedEvaluationDataset dataset = TenantScopedFixtureHarness.LoadDataset();

        dataset.DatasetId.ShouldBe("story-1-13-tenant-scoped-evaluation-scaffold");
        dataset.IsScaffold.ShouldBeTrue();
        dataset.TenantPartitions.Select(static tenant => tenant.TenantId)
            .ShouldBe(["tenant-alpha", "tenant-beta"]);
    }

    [Fact]
    public async Task CommandExecutionFixtureShouldRunThroughExistingGatewaySandbox()
    {
        TenantScopedEvaluationDataset dataset = TenantScopedFixtureHarness.LoadDataset();
        TenantScopedFixtureCase fixtureCase = TenantScopedFixtureHarness.CommandExecutionCases(dataset).Single();

        ArmOutcome outcome = await TenantScopedFixtureHarness
            .RunCommandExecutionFixtureAsync(fixtureCase, TestContext.Current.CancellationToken);

        outcome.DomainOutcomeIdentity.ShouldBe("GovernedNoteRecorded");
        outcome.DispatchCount.ShouldBe(1);
        outcome.CoarseIdempotencyRecordCount.ShouldBe(1);
        outcome.DurableView.ShouldNotBeNull();
        outcome.DurableView.NoteId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FB3");

        // The fixture's declared expected outcome / state transition must DRIVE the sandbox assertion, not merely
        // be asserted non-empty — otherwise the manifest's expectedOutcome is decorative for the only executable lane.
        fixtureCase.ExpectedOutcome.ShouldNotBeNull();
        outcome.AcceptedLifecycleState.ToLowerInvariant().ShouldBe(fixtureCase.ExpectedOutcome!.State.ToLowerInvariant());
        fixtureCase.IdempotencyKey.ShouldNotBeNullOrWhiteSpace();
        fixtureCase.StateTransition.ShouldNotBeNullOrWhiteSpace();
        fixtureCase.StateTransition!.Split("->")[^1].ToLowerInvariant().ShouldBe(outcome.AcceptedLifecycleState.ToLowerInvariant());
    }

    [Fact]
    public async Task CommandExecutionFixtureForAnUnboundTenantShouldFailClosed()
    {
        TenantScopedEvaluationDataset dataset = TenantScopedFixtureHarness.LoadDataset();
        TenantScopedFixtureCase boundCase = TenantScopedFixtureHarness.CommandExecutionCases(dataset).Single();
        TenantScopedFixtureCase unboundCase = boundCase with { TenantId = CrossTenantLeakageCorpus.ForeignTenant };

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => TenantScopedFixtureHarness.RunCommandExecutionFixtureAsync(unboundCase, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain(unboundCase.CaseId);
        exception.Message.ShouldNotContain(CrossTenantLeakageCorpus.ForeignTenant);
    }

    [Fact]
    public void TenantScopedCaseSurfaceShouldNotLeakUndeclaredTenantSentinels()
    {
        TenantScopedEvaluationDataset dataset = TenantScopedFixtureHarness.LoadDataset();

        foreach (TenantScopedFixtureCase fixtureCase in dataset.Cases)
        {
            string scope = TenantScopedFixtureHarness.SerializeCaseScope(fixtureCase);

            // Scan the tenant/resource-bearing surface, excluding ONLY the tenant tokens this case legitimately
            // declares (own tenant + declared resource tenants for adversarial negative cases). A foreign tenant or
            // foreign resource-id sentinel that is not declared would still trip the shared scanner — so this scan
            // can actually fail (unlike a projection that strips every tenant-bearing field).
            IReadOnlyList<LeakageSentinel> sentinels = CrossTenantLeakageCorpus.SentinelsExcluding(
                [.. TenantScopedFixtureHarness.DeclaredTenants(fixtureCase)]);

            CrossTenantLeakageScanner.Scan(fixtureCase.CaseId, "fixture-case-scope", scope, sentinels);
        }
    }

    [Fact]
    public void DeliberatelyLeakingForeignTenantArtifactShouldBeCaughtByTheSharedScanner()
    {
        string leakingArtifact = $"{{\"caseId\":\"case-cross-tenant-reference-001\",\"marker\":\"{CrossTenantLeakageCorpus.ForeignTenant}\"}}";

        CrossTenantLeakageException exception = Should.Throw<CrossTenantLeakageException>(() =>
            CrossTenantLeakageScanner.ScanAll("case-cross-tenant-reference-001", "negative-control", leakingArtifact));

        exception.Persona.ShouldBe("case-cross-tenant-reference-001");
        exception.ChannelLabel.ShouldBe("negative-control");
        exception.SentinelChannel.ShouldBe("tenant");
        exception.Message.ShouldNotContain("marker");
    }
}
