using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Metadata-only notification that a <c>GovernedNoteRecorded</c> event was published to <c>chatbot.events</c>.
/// Carries only the identifiers and version stamp the projection needs; never the command payload or any
/// display text. The <see cref="SourceVersion"/> drives order-tolerant, idempotent projection.
/// </summary>
/// <param name="TenantId">The owning tenant (state-store partition).</param>
/// <param name="NoteId">The governed note aggregate ULID.</param>
/// <param name="MessageId">The originating command/message ULID (idempotency evidence).</param>
/// <param name="SourceVersion">The aggregate version of the published event.</param>
/// <param name="RecordedAt">The UTC instant the event was persisted.</param>
/// <param name="CorrelationId">The correlation id carried through the spine.</param>
public sealed record GovernedNoteRecordedNotification(
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("noteId")] string NoteId,
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("sourceVersion")] long SourceVersion,
    [property: JsonPropertyName("recordedAt")] DateTimeOffset RecordedAt,
    [property: JsonPropertyName("correlationId")] string CorrelationId);
