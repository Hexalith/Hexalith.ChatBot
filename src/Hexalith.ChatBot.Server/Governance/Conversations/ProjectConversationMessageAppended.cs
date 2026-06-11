using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Conversations;

public sealed record ProjectConversationMessageAppended(
    string TenantId,
    string ProjectId,
    string MessageId,
    string ActorId,
    string TextFingerprint,
    int TextLength,
    string Locale,
    DateTimeOffset AppendedAtUtc,
    string CorrelationId,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    long SourceVersion,
    string SafeNextAction) : IEventPayload;
