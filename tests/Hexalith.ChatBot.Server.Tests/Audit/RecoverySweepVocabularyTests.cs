using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Behavioural coverage for the two closed sweep vocabularies.
/// <para>
/// These were previously guarded only by source-text inspection in the architecture suite: the "identity runs last"
/// contract was asserted by checking that <c>Identity</c> appeared <i>after</i> <c>AttachmentProcessing</c> in the
/// file, which a seventh dependency appended after it would satisfy while breaking the contract; and nothing anywhere
/// read <see cref="ScopedOutageDependencies.SweepOrder"/> at runtime, although this assembly already has the access.
/// </para>
/// </summary>
public sealed class RecoverySweepVocabularyTests
{
    [Fact]
    public void ScopedOutageSweepRunsIdentityLastAndCarriesNoDuplicates()
    {
        // Identity last is a safety contract, not a convenience: the identity outage breaks the token acquisition every
        // other scenario's independent control depends on, so running it earlier poisons the rest of the sweep.
        ScopedOutageDependencies.SweepOrder[^1].ShouldBe(ScopedOutageDependencies.Identity);
        ScopedOutageDependencies.SweepOrder.Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(ScopedOutageDependencies.SweepOrder.Count);
        ScopedOutageDependencies.All.Count.ShouldBe(ScopedOutageDependencies.SweepOrder.Count);
        ScopedOutageDependencies.All.ShouldBe(ScopedOutageDependencies.SweepOrder, ignoreOrder: true);
    }

    [Fact]
    public void ContinuitySweepIsOrderedDistinctAndClosed()
    {
        // The sibling vocabulary rejects a duplicate; this one built `All` from a bare HashSet, so a duplicated token
        // would silently dedupe — running one destructive drill twice while the closed set stayed one short, which the
        // evidence gate then reports as `continuity:incomplete_scenario_set` on every run thereafter.
        ContinuityDrillScenarios.SweepOrder[0].ShouldBe(ContinuityDrillScenarios.EventStoreOutage);
        ContinuityDrillScenarios.SweepOrder[^1].ShouldBe(ContinuityDrillScenarios.M365SubscriptionFailure);
        ContinuityDrillScenarios.All.Count.ShouldBe(ContinuityDrillScenarios.SweepOrder.Count);
        ContinuityDrillScenarios.Contains("eventstore-outage").ShouldBeTrue();
        ContinuityDrillScenarios.Contains("not-a-scenario").ShouldBeFalse();
        ContinuityDrillScenarios.Contains(null).ShouldBeFalse();
    }

    [Fact]
    public void StorageTenantDerivationRejectsASuffixThatStillCarriesTheSeparator()
    {
        // `:` is a legal identifier character — it has to be, for the prefix itself — so stripping exactly one prefix
        // and stopping left a physical tenant still containing the character this method exists to remove.
        ReplayTenantPolicy.StorageTenantFor("replay-test:recovery-validation").ShouldBe("recovery-validation");
        ReplayTenantPolicy.StorageTenantFor("replay-test:replay-test:x").ShouldBeNull();
        ReplayTenantPolicy.StorageTenantFor("replay-test:a:b").ShouldBeNull();
        ReplayTenantPolicy.StorageTenantFor("replay-test::").ShouldBeNull();
        ReplayTenantPolicy.StorageTenantFor("replay-test:").ShouldBeNull();
        ReplayTenantPolicy.StorageTenantFor("tenant-alpha").ShouldBeNull();
        ReplayTenantPolicy.StorageTenantFor(null).ShouldBeNull();
    }

    [Fact]
    public void DatasetVolumeOverflowIsReportedRatherThanWrappingIntoAMatch()
    {
        // An unchecked sum wrapped, so counts past int.MaxValue could land on the configured expectation and validate a
        // descriptor whose declared population is nonsense.
        RecoveryValidationDatasetDescriptor overflowing = new(
            DatasetRef: "recovery-baseline",
            Version: "v1",
            ProjectionSchemaVersion: "chatbot.project-conversation-source-email.v1",
            ValidationPartitionRef: "recovery-partition-v1",
            SourceRecordCount: int.MaxValue,
            WormAuditRecordCount: int.MaxValue,
            GovernedCommandCount: 1,
            ApprovalCount: 1,
            PolicySnapshotCount: 1,
            AttachmentMetadataCount: 5,
            UsesIsolatedValidationStore: true);

        overflowing
            .Validate("recovery-baseline", "v1", 6, "chatbot.project-conversation-source-email.v1", "recovery-partition-v1")
            .ShouldNotBeNull()
            .ShouldContain("overflow", Case.Insensitive);
    }
}
