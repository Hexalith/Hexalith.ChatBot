namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Metadata-only provenance and load profile for the deterministic Story 12.15 recovery baseline. Counts cover the
/// immutable source, WORM, governed decision, policy, and attachment metadata required by the live assertions.
/// </summary>
internal sealed record RecoveryValidationDatasetDescriptor(
    string DatasetRef,
    string Version,
    string ProjectionSchemaVersion,
    string ValidationPartitionRef,
    int SourceRecordCount,
    int WormAuditRecordCount,
    int GovernedCommandCount,
    int ApprovalCount,
    int PolicySnapshotCount,
    int AttachmentMetadataCount,
    bool UsesIsolatedValidationStore)
{
    /// <summary>Gets the deterministic total population represented by this descriptor.</summary>
    public int TotalVolume => SourceRecordCount + WormAuditRecordCount + GovernedCommandCount + ApprovalCount + PolicySnapshotCount + AttachmentMetadataCount;

    /// <summary>
    /// Validates the populated descriptor against the exact configured provenance, including the configured isolated
    /// partition.
    /// <para>
    /// The partition is compared, not merely shape-checked: <see cref="UsesIsolatedValidationStore"/> is a boolean the
    /// descriptor asserts about itself, so without comparing <see cref="ValidationPartitionRef"/> against the
    /// configured <c>ValidationPartitionRef</c> a descriptor naming the shared read-model partition validates clean
    /// simply by claiming it is isolated.
    /// </para>
    /// </summary>
    public string? Validate(
        string expectedDatasetRef,
        string expectedVersion,
        int expectedVolume,
        string expectedProjectionSchemaVersion,
        string expectedValidationPartitionRef)
    {
        if (!AuditMetadata.IsSafeStableIdentifier(DatasetRef) || !string.Equals(DatasetRef, expectedDatasetRef, StringComparison.Ordinal))
        {
            return "Dataset reference does not match the configured dataset.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(Version) || !string.Equals(Version, expectedVersion, StringComparison.Ordinal))
        {
            return "Dataset version does not match the configured version.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(ProjectionSchemaVersion) ||
            !string.Equals(ProjectionSchemaVersion, expectedProjectionSchemaVersion, StringComparison.Ordinal))
        {
            return "Projection schema version does not match the configured schema.";
        }

        if (!AuditMetadata.IsSafeStableIdentifier(ValidationPartitionRef) ||
            !UsesIsolatedValidationStore ||
            !string.Equals(ValidationPartitionRef, expectedValidationPartitionRef, StringComparison.Ordinal))
        {
            return "Dataset must target the configured, separately isolated validation-store partition.";
        }

        if (SourceRecordCount <= 0)
        {
            return $"{nameof(SourceRecordCount)} must be positive.";
        }

        if (WormAuditRecordCount <= 0)
        {
            return $"{nameof(WormAuditRecordCount)} must be positive.";
        }

        if (GovernedCommandCount <= 0)
        {
            return $"{nameof(GovernedCommandCount)} must be positive.";
        }

        if (ApprovalCount <= 0)
        {
            return $"{nameof(ApprovalCount)} must be positive.";
        }

        if (PolicySnapshotCount <= 0)
        {
            return $"{nameof(PolicySnapshotCount)} must be positive.";
        }

        if (AttachmentMetadataCount <= 0)
        {
            return $"{nameof(AttachmentMetadataCount)} must be positive.";
        }

        return TotalVolume == expectedVolume
            ? null
            : $"Dataset volume {TotalVolume} does not match configured volume {expectedVolume}.";
    }
}
