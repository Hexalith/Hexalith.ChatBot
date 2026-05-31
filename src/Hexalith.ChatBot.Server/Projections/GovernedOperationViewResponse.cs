using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Metadata-only response exposing a projected governed-operation view read from <c>chatbot-statestore</c>.
/// Carries the derived-record shape and version stamp only — never the owning tenant id, command payload, or
/// display text — so a tenant-scoped read confirms the durable read model exists without leaking restricted
/// evidence. The owning tenant is resolved from the caller's authenticated claims, never echoed in the body.
/// </summary>
/// <param name="NoteId">The governed-note aggregate ULID.</param>
/// <param name="SchemaVersion">The read-model schema version token.</param>
/// <param name="SourceProvenance">The provenance class of the source effect (metadata-only code).</param>
/// <param name="DerivationKernelVersion">The derivation kernel version that produced this record.</param>
/// <param name="RedactionState">The redaction state of the record (metadata-only).</param>
/// <param name="RetentionClass">The retention class governing this record.</param>
/// <param name="SourceVersion">The source aggregate version stamp.</param>
/// <param name="RecordedAt">The UTC instant the note was first projected.</param>
/// <param name="LastUpdatedAt">The UTC instant this record was last written.</param>
public sealed record GovernedOperationViewResponse(
    [property: JsonPropertyName("noteId")] string NoteId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("sourceProvenance")] string SourceProvenance,
    [property: JsonPropertyName("derivationKernelVersion")] string DerivationKernelVersion,
    [property: JsonPropertyName("redactionState")] string RedactionState,
    [property: JsonPropertyName("retentionClass")] string RetentionClass,
    [property: JsonPropertyName("sourceVersion")] long SourceVersion,
    [property: JsonPropertyName("recordedAt")] DateTimeOffset RecordedAt,
    [property: JsonPropertyName("lastUpdatedAt")] DateTimeOffset LastUpdatedAt)
{
    /// <summary>Projects a stored view into its metadata-only response (dropping the owning tenant id).</summary>
    /// <param name="view">The stored, tenant-partitioned view.</param>
    /// <returns>The metadata-only response.</returns>
    public static GovernedOperationViewResponse From(GovernedOperationView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new GovernedOperationViewResponse(
            view.NoteId,
            view.SchemaVersion,
            view.SourceProvenance,
            view.DerivationKernelVersion,
            view.RedactionState,
            view.RetentionClass,
            view.SourceVersion,
            view.RecordedAt,
            view.LastUpdatedAt);
    }
}
