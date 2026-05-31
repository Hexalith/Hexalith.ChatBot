using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

using GeneratedApiException = Hexalith.ChatBot.Client.Generated.HexalithChatBotApiException;
using GeneratedProblemDetailsApiException = Hexalith.ChatBot.Client.Generated.HexalithChatBotApiException<Hexalith.ChatBot.Client.Generated.ProblemDetails>;

namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// Narrow M365 mailbox intake lane. Concrete Graph calls stay behind <see cref="IGraphMailboxMessageSource"/>;
/// durable writes happen only through <see cref="IChatBotClient"/>.
/// </summary>
public sealed class GraphMailboxIntakeWorker(
    ControlledMailboxPattern pattern,
    IGraphMailboxMessageSource source,
    IChatBotClient client)
{
    public const string LeastPrivilegeGraphPermission = "Mail.Read";

    public async ValueTask<MailboxIntakeWorkerResult> ProcessAsync(
        GraphMailboxNotification notification,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (!string.Equals(notification.MailboxId, pattern.MailboxId, StringComparison.Ordinal))
        {
            return MailboxIntakeWorkerResult.Recoverable("mailbox_scope_mismatch");
        }

        GraphMailboxFetchResult fetch = await source.FetchMessageAsync(notification, cancellationToken).ConfigureAwait(false);
        if (fetch.Kind != GraphMailboxFetchResultKind.Found)
        {
            return MailboxIntakeWorkerResult.Recoverable(fetch.ReasonCode);
        }

        GraphMailboxMessage message = fetch.Message!;
        if (!MatchesNotificationScope(notification, message))
        {
            return MailboxIntakeWorkerResult.Recoverable("mailbox_message_scope_mismatch");
        }

        CaptureMailboxMessageIntake command = ToCommand(message);
        try
        {
            _ = await client
                .SubmitAsync(command, correlationId, taskId: null, ChatBotSurfaceOrigin.Mailbox, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (GeneratedProblemDetailsApiException ex) when (IsRecoverableSubmissionFailure(ex.StatusCode))
        {
            return MailboxIntakeWorkerResult.Recoverable(SafeSubmissionReason(ex.Result?.Code));
        }
        catch (GeneratedApiException ex) when (IsRecoverableSubmissionFailure(ex.StatusCode))
        {
            return MailboxIntakeWorkerResult.Recoverable("chatbot_submission_recoverable");
        }

        return MailboxIntakeWorkerResult.Submitted(command.IntakeId);
    }

    private bool MatchesNotificationScope(GraphMailboxNotification notification, GraphMailboxMessage message)
        => string.Equals(message.MailboxId, pattern.MailboxId, StringComparison.Ordinal) &&
            string.Equals(message.MailboxId, notification.MailboxId, StringComparison.Ordinal) &&
            string.Equals(message.ProviderMessageId, notification.ProviderMessageId, StringComparison.Ordinal);

    private static bool IsRecoverableSubmissionFailure(int statusCode)
        => statusCode is 401 or 403 or 503;

    private static string SafeSubmissionReason(string? problemCode)
        => string.IsNullOrWhiteSpace(problemCode) ||
            problemCode.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character)
                || !(char.IsLetterOrDigit(character) || character is '_' or '-'))
            ? "chatbot_submission_recoverable"
            : problemCode;

    private CaptureMailboxMessageIntake ToCommand(GraphMailboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new CaptureMailboxMessageIntake(
            MailboxMessageIntakeId.New().Value,
            new MailboxMessageSourceIdentity(
                message.ProviderMessageId,
                message.InternetMessageId,
                message.ConversationId,
                message.ThreadId,
                message.MailboxId,
                new MailboxParticipantIdentity(message.From.Address, message.From.DisplayName),
                message.ReceivedAt.ToUniversalTime(),
                message.SentAt?.ToUniversalTime(),
                message.CreatedAt?.ToUniversalTime(),
                message.SourceTimezone,
                pattern.SourceContext,
                SourceSchemaVersion: 1),
            message.Recipients
                .Select(static recipient => new MailboxRecipientIdentity(recipient.Address, recipient.DisplayName, recipient.Kind))
                .ToArray(),
            message.Attachments
                .Select(static attachment => new MailboxAttachmentReference(
                    attachment.ProviderAttachmentId,
                    attachment.Name,
                    attachment.ContentType,
                    attachment.SizeInBytes))
                .ToArray());
    }
}
