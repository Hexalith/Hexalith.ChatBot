namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Tenant-partitioned read model projected from <c>GovernedNoteRecorded</c> into <c>chatbot-statestore</c>.
/// Carries the derived-record shape (provenance/derivation/redaction/retention/schema) and a source-version
/// stamp that makes projection writes idempotent and order-tolerant (last-writer-wins by source version).
/// Metadata-only: identifiers and stable codes, never command payload or display text.
/// </summary>
/// <param name="TenantId">The owning tenant; every state-store key is partitioned by it.</param>
/// <param name="NoteId">The governed note aggregate ULID.</param>
/// <param name="SchemaVersion">The read-model schema version token.</param>
/// <param name="SourceProvenance">The provenance class of the source effect (metadata-only code).</param>
/// <param name="DerivationKernelVersion">The derivation kernel version that produced this record.</param>
/// <param name="RedactionState">The redaction state of the record (metadata-only).</param>
/// <param name="RetentionClass">The retention class governing this record.</param>
/// <param name="SourceVersion">The source aggregate version stamp used for order-tolerant projection.</param>
/// <param name="RecordedAt">The UTC instant the note was first projected.</param>
/// <param name="LastUpdatedAt">The UTC instant this record was last written.</param>
public sealed record GovernedOperationView(
    string TenantId,
    string NoteId,
    string SchemaVersion,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    DateTimeOffset RecordedAt,
    DateTimeOffset LastUpdatedAt)
{
    /// <summary>The read-model schema version token.</summary>
    public const string CurrentSchemaVersion = "chatbot.governed-operation-view.v1";

    /// <summary>The derivation kernel version token.</summary>
    public const string CurrentDerivationKernelVersion = "chatbot.derivation-kernel.v1";

    /// <summary>Provenance class for a governed-command-sourced record (metadata-only).</summary>
    public const string GovernedCommandProvenance = "governed-command";

    /// <summary>Redaction state for a metadata-only derived record.</summary>
    public const string MetadataOnlyRedactionState = "metadata_only";

    /// <summary>Retention class for governed operational records.</summary>
    public const string GovernedOperationalRetentionClass = "governed-operational";

    /// <summary>
    /// Builds the tenant-partitioned state-store key for a governed operation view so that M1's second
    /// tenant is additive and no key is shared across tenants.
    /// </summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="noteId">The governed note aggregate id.</param>
    /// <returns>The partitioned key.</returns>
    public static string KeyFor(string tenantId, string noteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(noteId);
        return $"{tenantId}:governed-operation:{noteId}";
    }
}
