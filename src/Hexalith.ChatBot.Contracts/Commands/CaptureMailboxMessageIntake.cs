namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Captures metadata-only source identity for a controlled mailbox message. Tenant authority is supplied
/// by the authenticated gateway context, not by this command body.
/// </summary>
/// <param name="IntakeId">ChatBot-owned ULID for the mailbox intake aggregate.</param>
/// <param name="Source">Provider source identity and timestamp context.</param>
/// <param name="Recipients">Provider recipients preserved as source identity metadata.</param>
/// <param name="Attachments">Provider attachment references only; body content is out of scope.</param>
public sealed record CaptureMailboxMessageIntake(
    string IntakeId,
    MailboxMessageSourceIdentity Source,
    IReadOnlyList<MailboxRecipientIdentity> Recipients,
    IReadOnlyList<MailboxAttachmentReference> Attachments) : IChatBotCommand;
