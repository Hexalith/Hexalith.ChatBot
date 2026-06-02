using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using System.Text.RegularExpressions;

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
                .ToArray(),
            BuildAuthenticityMetadata(message));
    }

    private static MailboxAuthenticityMetadata BuildAuthenticityMetadata(GraphMailboxMessage message)
    {
        IReadOnlyList<MailboxSelectedHeaderSnapshot> received = SelectedHeaders(message, "Received");
        IReadOnlyList<MailboxSelectedHeaderSnapshot> authenticationResults = SelectedHeaders(message, "Authentication-Results");
        HeaderAddress from = HeaderAddressValue(message, "From");
        HeaderAddress sender = HeaderAddressValue(message, "Sender");
        HeaderAddress replyTo = HeaderAddressValue(message, "Reply-To");
        HeaderAddress originalSender = HeaderAddressValue(message, "X-Original-Sender");
        IReadOnlyList<MailboxHeaderDiscrepancyKind> discrepancies = Discrepancies(authenticationResults, from, sender, replyTo, originalSender);

        return new MailboxAuthenticityMetadata(
            AuthenticationResults(message, authenticationResults),
            new MailboxHeaderInspectionSnapshot(
                received,
                authenticationResults,
                from.State,
                replyTo.State,
                sender.State,
                originalSender.State,
                discrepancies));
    }

    private static MailboxAuthenticationResultSnapshot AuthenticationResults(
        GraphMailboxMessage message,
        IReadOnlyList<MailboxSelectedHeaderSnapshot> authenticationResults)
    {
        string?[] values = message.InternetMessageHeaders
            .Where(static header => string.Equals(header.Name, "Authentication-Results", StringComparison.OrdinalIgnoreCase))
            .Select(static header => header.Value)
            .ToArray();
        if (values.Length == 0)
        {
            return new MailboxAuthenticationResultSnapshot(
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                null,
                authenticationResults);
        }

        return new MailboxAuthenticationResultSnapshot(
            Verdict(values, "spf"),
            Verdict(values, "dkim"),
            Verdict(values, "dmarc"),
            Verdict(values, "compauth"),
            SafeReason(values),
            authenticationResults);
    }

    private static MailboxAuthenticationVerdictKind Verdict(
        IReadOnlyList<string?> values,
        string key)
    {
        bool sawMalformedHeader = false;
        MailboxAuthenticationVerdictKind? selected = null;
        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                sawMalformedHeader = true;
                continue;
            }

            MailboxAuthenticationVerdictKind? parsed = TryVerdict(value, key);
            if (parsed is null)
            {
                continue;
            }

            if (selected is not null && selected.Value != parsed.Value)
            {
                return MailboxAuthenticationVerdictKind.Ambiguous;
            }

            selected = parsed;
        }

        return selected ?? (sawMalformedHeader ? MailboxAuthenticationVerdictKind.Malformed : MailboxAuthenticationVerdictKind.NotSupplied);
    }

    private static MailboxAuthenticationVerdictKind? TryVerdict(string value, string key)
    {
        Match match = Regex.Match(
            value,
            $@"(?:^|[;\s]){Regex.Escape(key)}\s*=\s*(?<value>[A-Za-z0-9_-]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups["value"].Value.ToLowerInvariant() switch
        {
            "pass" => MailboxAuthenticationVerdictKind.Pass,
            "fail" => MailboxAuthenticationVerdictKind.Fail,
            "softfail" => MailboxAuthenticationVerdictKind.SoftFail,
            "neutral" => MailboxAuthenticationVerdictKind.Neutral,
            "none" => MailboxAuthenticationVerdictKind.None,
            "temperror" => MailboxAuthenticationVerdictKind.TempError,
            "permerror" => MailboxAuthenticationVerdictKind.PermError,
            "bestguesspass" => MailboxAuthenticationVerdictKind.BestGuessPass,
            _ => MailboxAuthenticationVerdictKind.Unknown,
        };
    }

    private static string? SafeReason(IReadOnlyList<string?> values)
    {
        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            Match match = Regex.Match(
                value,
                @"(?:^|[;\s])reason\s*=\s*(?<value>[A-Za-z0-9_.:-]+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                return match.Groups["value"].Value;
            }
        }

        return null;
    }

    private static IReadOnlyList<MailboxSelectedHeaderSnapshot> SelectedHeaders(GraphMailboxMessage message, string name)
    {
        List<MailboxSelectedHeaderSnapshot> selected = [];
        foreach (GraphMailboxInternetMessageHeader header in message.InternetMessageHeaders)
        {
            if (!string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            selected.Add(new MailboxSelectedHeaderSnapshot(
                CanonicalHeaderName(name),
                selected.Count,
                string.IsNullOrWhiteSpace(header.Value) ? MailboxHeaderValueState.Malformed : MailboxHeaderValueState.Supplied));
        }

        return selected;
    }

    private static HeaderAddress HeaderAddressValue(GraphMailboxMessage message, string name)
    {
        GraphMailboxInternetMessageHeader? header = message.InternetMessageHeaders
            .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
        if (header is null)
        {
            return new HeaderAddress(null, MailboxHeaderValueState.NotSupplied);
        }

        string? address = ExtractAddress(header.Value);
        return address is null
            ? new HeaderAddress(null, MailboxHeaderValueState.Malformed)
            : new HeaderAddress(address, MailboxHeaderValueState.Supplied);
    }

    private static string? ExtractAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Match bracketed = Regex.Match(value, "<(?<address>[^<>\\s]+@[^<>\\s]+)>", RegexOptions.CultureInvariant);
        if (bracketed.Success)
        {
            return bracketed.Groups["address"].Value.ToLowerInvariant();
        }

        string trimmed = value.Trim();
        return Regex.IsMatch(trimmed, "^[^@\\s<>]+@[^@\\s<>]+$", RegexOptions.CultureInvariant)
            ? trimmed.ToLowerInvariant()
            : null;
    }

    private static IReadOnlyList<MailboxHeaderDiscrepancyKind> Discrepancies(
        IReadOnlyList<MailboxSelectedHeaderSnapshot> authenticationResults,
        HeaderAddress from,
        HeaderAddress sender,
        HeaderAddress replyTo,
        HeaderAddress originalSender)
    {
        List<MailboxHeaderDiscrepancyKind> discrepancies = [];
        if (authenticationResults.Count > 1)
        {
            discrepancies.Add(MailboxHeaderDiscrepancyKind.MultipleAuthenticationResults);
        }

        AddMalformed(discrepancies, from, MailboxHeaderDiscrepancyKind.MalformedFrom);
        AddMalformed(discrepancies, sender, MailboxHeaderDiscrepancyKind.MalformedSender);
        AddMalformed(discrepancies, replyTo, MailboxHeaderDiscrepancyKind.MalformedReplyTo);
        AddMalformed(discrepancies, originalSender, MailboxHeaderDiscrepancyKind.MalformedXOriginalSender);
        AddMismatch(discrepancies, from, sender, MailboxHeaderDiscrepancyKind.FromSenderMismatch);
        AddMismatch(discrepancies, from, replyTo, MailboxHeaderDiscrepancyKind.FromReplyToMismatch);
        AddMismatch(discrepancies, sender, replyTo, MailboxHeaderDiscrepancyKind.SenderReplyToMismatch);
        AddMismatch(discrepancies, from, originalSender, MailboxHeaderDiscrepancyKind.FromXOriginalSenderMismatch);
        return discrepancies;
    }

    private static void AddMalformed(List<MailboxHeaderDiscrepancyKind> discrepancies, HeaderAddress header, MailboxHeaderDiscrepancyKind kind)
    {
        if (header.State == MailboxHeaderValueState.Malformed)
        {
            discrepancies.Add(kind);
        }
    }

    private static void AddMismatch(
        List<MailboxHeaderDiscrepancyKind> discrepancies,
        HeaderAddress first,
        HeaderAddress second,
        MailboxHeaderDiscrepancyKind kind)
    {
        if (first.Address is not null &&
            second.Address is not null &&
            !string.Equals(first.Address, second.Address, StringComparison.OrdinalIgnoreCase))
        {
            discrepancies.Add(kind);
        }
    }

    private static string CanonicalHeaderName(string name)
        => name switch
        {
            "Authentication-Results" => "Authentication-Results",
            "Received" => "Received",
            _ => name,
        };

    private sealed record HeaderAddress(string? Address, MailboxHeaderValueState State);
}
