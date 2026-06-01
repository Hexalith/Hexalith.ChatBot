using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal static class MailboxIntakeProjectionTranslator
{
    public static readonly string IntakeCapturedEventType = typeof(MailboxMessageIntakeCaptured).FullName!;

    public static MailboxIntakeProjectionNotification? TryCreateNotification(PublishedMailboxIntakeEvent? published)
    {
        if (published is null ||
            !string.Equals(published.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal) ||
            !string.Equals(published.EventTypeName, IntakeCapturedEventType, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.IntakeId) ||
            string.IsNullOrWhiteSpace(published.ProviderMessageId) ||
            string.IsNullOrWhiteSpace(published.ConversationId) ||
            string.IsNullOrWhiteSpace(published.MailboxId) ||
            published.Sender is null ||
            string.IsNullOrWhiteSpace(published.SourceContext) ||
            string.IsNullOrWhiteSpace(published.SourceProvenance) ||
            string.IsNullOrWhiteSpace(published.DerivationKernelVersion) ||
            string.IsNullOrWhiteSpace(published.RedactionState) ||
            string.IsNullOrWhiteSpace(published.RetentionClass) ||
            published.ReceivedAtUtc == default ||
            published.SchemaVersion <= 0 ||
            published.SequenceNumber <= 0)
        {
            return null;
        }

        MailboxMessageIntakeCaptured captured = new(
            published.IntakeId,
            published.ProviderMessageId,
            published.InternetMessageId ?? string.Empty,
            published.ConversationId,
            published.ThreadId,
            published.MailboxId,
            published.Sender,
            published.Recipients ?? [],
            published.ReceivedAtUtc,
            published.SentAtUtc,
            published.CreatedAtUtc,
            published.AttachmentReferences ?? [],
            published.SourceTimezone,
            published.SourceContext,
            published.SourceProvenance,
            published.DerivationKernelVersion,
            published.RedactionState,
            published.RetentionClass,
            published.SchemaVersion);

        return new MailboxIntakeProjectionNotification(
            published.TenantId,
            captured,
            published.SequenceNumber,
            published.CorrelationId ?? string.Empty);
    }
}

internal sealed record MailboxIntakeProjectionNotification(
    string TenantId,
    MailboxMessageIntakeCaptured Captured,
    long SourceVersion,
    string CorrelationId);
