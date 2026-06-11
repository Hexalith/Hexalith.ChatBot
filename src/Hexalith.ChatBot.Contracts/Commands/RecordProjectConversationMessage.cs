namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only project conversation append requested from a governed UI composer.
/// Tenant authority is supplied by authenticated server context.
/// </summary>
public sealed record RecordProjectConversationMessage(
    string ProjectId,
    string MessageId,
    string TextFingerprint,
    int TextLength,
    string Locale,
    long ExpectedSourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.project-conversation-message.v1") : IChatBotCommand;
