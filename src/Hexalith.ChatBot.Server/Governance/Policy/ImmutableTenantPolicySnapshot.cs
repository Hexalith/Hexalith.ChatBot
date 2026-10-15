using System.Collections.ObjectModel;

using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Governance.Policy;

/// <summary>An immutable synthetic policy fixture. It conveys no accepted protection or native isolation evidence.</summary>
internal sealed class ImmutableTenantPolicySnapshot
{
    /// <summary>Copies a complete synthetic snapshot and its unaccepted evidence references.</summary>
    public ImmutableTenantPolicySnapshot(
        string tenantId, string snapshotId, long policyVersion, string schemaVersion,
        DateTimeOffset effectiveAtUtc, string actorId, string provenanceReference,
        string redactionReference, string retentionReference, string holdReference,
        string exportDeleteReference, string backupReference, string encryptionReference,
        TenantPolicyChangeSet values, string? supersedesSnapshotId = null, string? rollbackSnapshotId = null)
    {
        string[] tokens = [tenantId, snapshotId, schemaVersion, actorId, provenanceReference, redactionReference,
            retentionReference, holdReference, exportDeleteReference, backupReference, encryptionReference];
        if (tokens.Any(static value => !TenantPolicySchema.IsSafePolicyToken(value)) ||
            !TenantPolicySchemaVersions.IsKnown(schemaVersion) || policyVersion < 1 || effectiveAtUtc.Offset != TimeSpan.Zero ||
            effectiveAtUtc == default ||
            (supersedesSnapshotId is not null && !TenantPolicySchema.IsSafePolicyToken(supersedesSnapshotId)) ||
            (rollbackSnapshotId is not null && !TenantPolicySchema.IsSafePolicyToken(rollbackSnapshotId)))
        {
            throw new ArgumentException("Synthetic policy metadata is invalid.");
        }

        ArgumentNullException.ThrowIfNull(values);
        if (values.Values is null || values.Values.Any(static value => value is null))
        {
            throw new ArgumentException("Synthetic policy values are invalid.", nameof(values));
        }

        TenantPolicyValue[] copied = values.Values.OrderBy(static value => value.KnobId, StringComparer.Ordinal)
            .Select(static value => value with
            {
                AiActionLowRiskAllowed = value.AiActionLowRiskAllowed is null ? null :
                    new ReadOnlyDictionary<Contracts.Enums.AiActionRiskActionClass, bool>(value.AiActionLowRiskAllowed
                        .OrderBy(static pair => pair.Key).ToDictionary(static pair => pair.Key, static pair => pair.Value)),
                StringListValue = value.StringListValue is null ? null : Array.AsReadOnly(value.StringListValue.ToArray()),
                AdminScopesValue = value.AdminScopesValue is null ? null : Array.AsReadOnly(value.AdminScopesValue.Order().ToArray()),
            }).ToArray();
        Values = new TenantPolicyChangeSet(Array.AsReadOnly(copied));
        // Sorting ensures the schema's cross-knob low/high check sees high before low.
        if (!TenantPolicySchema.Validate(Values).IsValid ||
            TenantPolicySchema.Definitions.Where(definition => definition.SchemaVersion == TenantPolicySchemaVersions.M0 ||
                definition.SchemaVersion == schemaVersion).Any(definition => !copied.Any(value => value.KnobId == definition.KnobId)) ||
            copied.Any(value => TenantPolicySchema.TryGetDefinition(value.KnobId, out TenantPolicyKnobDefinition definition) &&
                schemaVersion == TenantPolicySchemaVersions.M0 && definition.SchemaVersion != schemaVersion))
        {
            throw new ArgumentException("Synthetic policy values are invalid.", nameof(values));
        }

        TenantId = tenantId;
        SnapshotId = snapshotId;
        PolicyVersion = policyVersion;
        SchemaVersion = schemaVersion;
        EffectiveAtUtc = effectiveAtUtc;
        ActorId = actorId;
        ProvenanceReference = provenanceReference;
        RedactionReference = redactionReference;
        RetentionReference = retentionReference;
        HoldReference = holdReference;
        ExportDeleteReference = exportDeleteReference;
        BackupReference = backupReference;
        EncryptionReference = encryptionReference;
        SupersedesSnapshotId = supersedesSnapshotId;
        RollbackSnapshotId = rollbackSnapshotId;
    }

    /// <summary>Gets the fixture classification; this cannot become accepted A6 evidence.</summary>
    public string EvidenceKind { get; } = "synthetic-unaccepted";
    /// <summary>Gets the bound synthetic tenant.</summary>
    public string TenantId { get; }
    /// <summary>Gets the immutable snapshot identity.</summary>
    public string SnapshotId { get; }
    /// <summary>Gets the immutable policy version.</summary>
    public long PolicyVersion { get; }
    /// <summary>Gets the closed schema version.</summary>
    public string SchemaVersion { get; }
    /// <summary>Gets the prospective UTC effective time.</summary>
    public DateTimeOffset EffectiveAtUtc { get; }
    /// <summary>Gets the synthetic actor reference.</summary>
    public string ActorId { get; }
    /// <summary>Gets synthetic provenance, without implying acceptance.</summary>
    public string ProvenanceReference { get; }
    /// <summary>Gets the unaccepted synthetic redaction reference.</summary>
    public string RedactionReference { get; }
    /// <summary>Gets the unaccepted synthetic retention reference.</summary>
    public string RetentionReference { get; }
    /// <summary>Gets the unaccepted synthetic legal-hold reference.</summary>
    public string HoldReference { get; }
    /// <summary>Gets the unaccepted synthetic export/deletion reference.</summary>
    public string ExportDeleteReference { get; }
    /// <summary>Gets the unaccepted synthetic backup reference.</summary>
    public string BackupReference { get; }
    /// <summary>Gets the unaccepted synthetic encryption reference.</summary>
    public string EncryptionReference { get; }
    /// <summary>Gets the previous snapshot identity without rewriting it.</summary>
    public string? SupersedesSnapshotId { get; }
    /// <summary>Gets the historical source of a prospective rollback.</summary>
    public string? RollbackSnapshotId { get; }
    /// <summary>Gets defensively copied values and nested collections.</summary>
    public TenantPolicyChangeSet Values { get; }

    /// <summary>Compares exact immutable contents after canonical copying.</summary>
    public bool HasSameContents(ImmutableTenantPolicySnapshot other)
        => (TenantId, SnapshotId, PolicyVersion, SchemaVersion, EffectiveAtUtc, ActorId, ProvenanceReference,
            RedactionReference, RetentionReference, HoldReference, ExportDeleteReference, BackupReference,
            EncryptionReference, SupersedesSnapshotId, RollbackSnapshotId) ==
            (other.TenantId, other.SnapshotId, other.PolicyVersion, other.SchemaVersion, other.EffectiveAtUtc, other.ActorId, other.ProvenanceReference,
            other.RedactionReference, other.RetentionReference, other.HoldReference, other.ExportDeleteReference, other.BackupReference,
            other.EncryptionReference, other.SupersedesSnapshotId, other.RollbackSnapshotId) && HasSameValues(other);

    /// <summary>Compares values structurally without serializing protected material into diagnostics.</summary>
    public bool HasSameValues(ImmutableTenantPolicySnapshot other)
        => Values.Values.Count == other.Values.Values.Count && Values.Values.Zip(other.Values.Values).All(static pair =>
            pair.First with { AiActionLowRiskAllowed = null, StringListValue = null, AdminScopesValue = null } ==
                pair.Second with { AiActionLowRiskAllowed = null, StringListValue = null, AdminScopesValue = null } &&
            SameSequence(pair.First.AiActionLowRiskAllowed, pair.Second.AiActionLowRiskAllowed) &&
            SameSequence(pair.First.StringListValue, pair.Second.StringListValue) &&
            SameSequence(pair.First.AdminScopesValue, pair.Second.AdminScopesValue));

    private static bool SameSequence<T>(IEnumerable<T>? left, IEnumerable<T>? right)
        => left is null ? right is null : right is not null && left.SequenceEqual(right);
}
