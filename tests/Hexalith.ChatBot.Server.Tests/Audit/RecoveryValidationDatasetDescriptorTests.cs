using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Story 12.15 Task 2 deterministic baseline-dataset contract tests.</summary>
public sealed class RecoveryValidationDatasetDescriptorTests
{
    private const string Partition = "recovery-partition-v1";

    [Fact]
    public void VersionedNonEmptyIsolatedDatasetMatchesConfiguredProvenance()
    {
        RecoveryValidationDatasetDescriptor descriptor = ValidDescriptor();

        descriptor.Validate("recovery-baseline", "v1", 6, "project-conversation-v1", Partition).ShouldBeNull();
    }

    [Fact]
    public void EmptyOrMismatchedDatasetFailsClosed()
    {
        (ValidDescriptor() with { SourceRecordCount = 0 })
            .Validate("recovery-baseline", "v1", 5, "project-conversation-v1", Partition).ShouldNotBeNull()
            .ShouldContain(nameof(RecoveryValidationDatasetDescriptor.SourceRecordCount));

        ValidDescriptor()
            .Validate("recovery-baseline", "v2", 6, "project-conversation-v1", Partition).ShouldNotBeNull()
            .ShouldContain("version");

        ValidDescriptor()
            .Validate("recovery-baseline", "v1", 7, "project-conversation-v1", Partition).ShouldNotBeNull()
            .ShouldContain("volume");
    }

    [Fact]
    public void ADatasetThatMerelyClaimsIsolationDoesNotSatisfyTheConfiguredPartition()
    {
        // UsesIsolatedValidationStore is a boolean the descriptor asserts about itself. Without comparing the
        // partition, a descriptor naming the shared read-model partition validates clean simply by claiming isolation.
        (ValidDescriptor() with { ValidationPartitionRef = "chatbot-readmodels" })
            .Validate("recovery-baseline", "v1", 6, "project-conversation-v1", Partition).ShouldNotBeNull()
            .ShouldContain("partition");

        (ValidDescriptor() with { UsesIsolatedValidationStore = false })
            .Validate("recovery-baseline", "v1", 6, "project-conversation-v1", Partition).ShouldNotBeNull()
            .ShouldContain("partition");
    }

    private static RecoveryValidationDatasetDescriptor ValidDescriptor()
        => new(
            "recovery-baseline",
            "v1",
            "project-conversation-v1",
            "recovery-partition-v1",
            SourceRecordCount: 1,
            WormAuditRecordCount: 1,
            GovernedCommandCount: 1,
            ApprovalCount: 1,
            PolicySnapshotCount: 1,
            AttachmentMetadataCount: 1,
            UsesIsolatedValidationStore: true);
}
