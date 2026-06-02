using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationSourceEmailView(
    string TenantId,
    string IntakeId,
    string SourceMailboxId,
    string SourceProviderMessageId,
    string? InternetMessageId,
    string SourceConversationId,
    string? SourceThreadId,
    DateTimeOffset SourceReceivedAtUtc,
    DateTimeOffset? SourceSentAtUtc,
    DateTimeOffset? SourceCreatedAtUtc,
    string? SourceTimezone,
    string SourceProvenanceDisplayToken,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    long SourceVersion,
    string CorrelationId,
    MailboxAuthenticityMetadata? Authenticity = null,
    MailboxDelegatedSenderSnapshot? DelegatedSender = null,
    MailboxExternalSenderPosture? ExternalSender = null)
{
    public const string CurrentSchemaVersion = "chatbot.project-conversation-source-email.v1";

    public static string KeyFor(string tenantId, string intakeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);
        return $"{tenantId}:project-conversation-source-email:{intakeId}";
    }

    public static ProjectConversationSourceEmailView FromIntake(
        string tenantId,
        MailboxMessageIntakeCaptured captured,
        long sourceVersion,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(captured);

        return new ProjectConversationSourceEmailView(
            tenantId,
            captured.IntakeId,
            captured.MailboxId,
            captured.ProviderMessageId,
            string.IsNullOrWhiteSpace(captured.InternetMessageId) ? null : captured.InternetMessageId,
            captured.ConversationId,
            captured.ThreadId,
            captured.ReceivedAtUtc.ToUniversalTime(),
            captured.SentAtUtc?.ToUniversalTime(),
            captured.CreatedAtUtc?.ToUniversalTime(),
            captured.SourceTimezone,
            DisplayTokenFor(captured.SourceProvenance),
            captured.SourceProvenance,
            captured.RedactionState,
            captured.RetentionClass,
            CurrentSchemaVersion,
            sourceVersion,
            correlationId,
            captured.Authenticity,
            captured.DelegatedSender,
            captured.ExternalSender);
    }

    public static bool ShouldReplace(ProjectConversationSourceEmailView existing, ProjectConversationSourceEmailView incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.SourceVersion >= existing.SourceVersion;
    }

    private static string DisplayTokenFor(string sourceProvenance)
        => string.Equals(sourceProvenance, AssociationCandidateView.MailboxSourceProvenance, StringComparison.Ordinal)
            ? "Microsoft 365 mailbox"
            : "source-provenance-unavailable";
}
