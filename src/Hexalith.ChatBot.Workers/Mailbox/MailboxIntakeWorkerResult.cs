namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed record MailboxIntakeWorkerResult(MailboxIntakeWorkerResultKind Kind, string ReasonCode, string? IntakeId)
{
    public static MailboxIntakeWorkerResult Submitted(string intakeId)
        => new(MailboxIntakeWorkerResultKind.Submitted, "submitted", intakeId);

    public static MailboxIntakeWorkerResult Recoverable(string reasonCode)
        => new(MailboxIntakeWorkerResultKind.Recoverable, reasonCode, null);
}
