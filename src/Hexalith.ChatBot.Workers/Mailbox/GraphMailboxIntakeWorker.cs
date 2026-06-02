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
public sealed class GraphMailboxIntakeWorker
{
    public const string LeastPrivilegeGraphPermission = "Mail.Read";
    private const string LegacyTenantId = "tenant-default";

    private readonly string _tenantId;
    private readonly IMailboxConfigurationProvider _configurationProvider;
    private readonly IGraphMailboxMessageSource _source;
    private readonly IChatBotClient _client;

    public GraphMailboxIntakeWorker(
        ControlledMailboxPattern pattern,
        IGraphMailboxMessageSource source,
        IChatBotClient client)
        : this(LegacyTenantId, new StaticMailboxConfigurationProvider(RequirePattern(pattern)), source, client)
    {
    }

    public GraphMailboxIntakeWorker(
        string tenantId,
        IMailboxConfigurationProvider configurationProvider,
        IGraphMailboxMessageSource source,
        IChatBotClient client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(configurationProvider);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(client);

        _tenantId = tenantId;
        _configurationProvider = configurationProvider;
        _source = source;
        _client = client;
    }

    public async ValueTask<MailboxIntakeWorkerResult> ProcessAsync(
        GraphMailboxNotification notification,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ControlledMailboxPattern? pattern = await _configurationProvider
            .ResolvePatternAsync(_tenantId, notification.MailboxId, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            return MailboxIntakeWorkerResult.Recoverable("mailbox_scope_mismatch");
        }

        // FR74: a mailbox source disabled under the two-person admin path blocks all future intake before any
        // Graph fetch or CaptureMailboxMessageIntake submission. Existing captured records are untouched.
        if (pattern.ControlState == MailboxSourceControlState.Disabled)
        {
            return MailboxIntakeWorkerResult.Recoverable("mailbox_source_disabled");
        }

        // FR74 (Story 7.13): a mailbox source quarantined under the two-person admin path routes new intake to a
        // contained-for-review outcome before any Graph fetch or CaptureMailboxMessageIntake submission, so no
        // restricted content (body, addresses, attachments) is read. Existing captured records are untouched.
        if (pattern.ControlState == MailboxSourceControlState.Quarantined)
        {
            return MailboxIntakeWorkerResult.Recoverable("mailbox_source_quarantined");
        }

        GraphMailboxFetchResult fetch = await _source.FetchMessageAsync(notification, cancellationToken).ConfigureAwait(false);
        if (fetch.Kind != GraphMailboxFetchResultKind.Found)
        {
            return MailboxIntakeWorkerResult.Recoverable(fetch.ReasonCode);
        }

        GraphMailboxMessage message = fetch.Message!;
        if (!MatchesNotificationScope(pattern, notification, message))
        {
            return MailboxIntakeWorkerResult.Recoverable("mailbox_message_scope_mismatch");
        }

        CaptureMailboxMessageIntake command = ToCommand(pattern, message);
        try
        {
            _ = await _client
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

    private static bool MatchesNotificationScope(ControlledMailboxPattern pattern, GraphMailboxNotification notification, GraphMailboxMessage message)
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

    private static CaptureMailboxMessageIntake ToCommand(ControlledMailboxPattern pattern, GraphMailboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        MailboxParticipantIdentity providerFrom = new(message.From.Address, message.From.DisplayName);
        MailboxParticipantIdentity? providerSender = message.Sender is null
            ? null
            : new MailboxParticipantIdentity(message.Sender.Address, message.Sender.DisplayName);
        MailboxParticipantIdentity authoritySender = IsDifferentParticipant(providerSender, providerFrom)
            ? providerSender!
            : providerFrom;
        MailboxAuthenticityMetadata authenticity = BuildAuthenticityMetadata(message);

        return new CaptureMailboxMessageIntake(
            MailboxMessageIntakeId.New().Value,
            new MailboxMessageSourceIdentity(
                message.ProviderMessageId,
                message.InternetMessageId,
                message.ConversationId,
                message.ThreadId,
                message.MailboxId,
                authoritySender,
                message.ReceivedAt.ToUniversalTime(),
                message.SentAt?.ToUniversalTime(),
                message.CreatedAt?.ToUniversalTime(),
                message.SourceTimezone,
                pattern.SourceContext,
                SourceSchemaVersion: 1,
                DelegatedSender: BuildDelegatedSender(message, providerFrom, providerSender, authenticity.HeaderInspection.Discrepancies),
                ExternalSender: new MailboxExternalSenderPosture(
                    ExternalSender: true,
                    MailboxPartyResolutionState.Unavailable,
                    ResolvedPartyRef: null,
                    ["external-sender:true", "party-resolution:unavailable"])),
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
            authenticity);
    }

    private static ControlledMailboxPattern RequirePattern(ControlledMailboxPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        return pattern;
    }

    private sealed class StaticMailboxConfigurationProvider(ControlledMailboxPattern pattern) : IMailboxConfigurationProvider
    {
        public ValueTask<ControlledMailboxPattern?> ResolvePatternAsync(
            string tenantId,
            string notificationMailboxId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(
                string.Equals(notificationMailboxId, pattern.MailboxId, StringComparison.Ordinal)
                    ? pattern
                    : null);
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
                discrepancies),
            new MailboxAuthenticityStrictnessPolicySnapshot(
                MailboxAuthenticityStrictness.Strict,
                "policy-unavailable",
                "policy-unavailable"));
    }

    private static MailboxDelegatedSenderSnapshot BuildDelegatedSender(
        GraphMailboxMessage message,
        MailboxParticipantIdentity providerFrom,
        MailboxParticipantIdentity? providerSender,
        IReadOnlyList<MailboxHeaderDiscrepancyKind> headerDiscrepancies)
    {
        bool hasProviderDelegation = IsDifferentParticipant(providerSender, providerFrom);
        MailboxDelegatedSenderState state = hasProviderDelegation
            ? HasHeaderProviderConflict(message, providerFrom, providerSender!) ? MailboxDelegatedSenderState.Ambiguous : MailboxDelegatedSenderState.Delegated
            : providerSender is null ? MailboxDelegatedSenderState.NotSupplied : MailboxDelegatedSenderState.NotDelegated;
        List<string> evidenceRefs = ["provider:from"];
        if (providerSender is not null)
        {
            evidenceRefs.Add("provider:sender");
        }

        evidenceRefs.AddRange(SelectedHeaderEvidenceRefs(message, "From"));
        evidenceRefs.AddRange(SelectedHeaderEvidenceRefs(message, "Sender"));
        evidenceRefs.AddRange(SelectedHeaderEvidenceRefs(message, "Reply-To"));
        evidenceRefs.AddRange(SelectedHeaderEvidenceRefs(message, "X-Original-Sender"));

        return new MailboxDelegatedSenderSnapshot(
            state,
            hasProviderDelegation ? providerSender : null,
            hasProviderDelegation ? providerFrom : null,
            evidenceRefs.Distinct(StringComparer.Ordinal).ToArray(),
            headerDiscrepancies);
    }

    private static bool IsDifferentParticipant(MailboxParticipantIdentity? left, MailboxParticipantIdentity right)
        => left is not null &&
            !string.Equals(left.Address, right.Address, StringComparison.OrdinalIgnoreCase);

    private static bool HasHeaderProviderConflict(
        GraphMailboxMessage message,
        MailboxParticipantIdentity providerFrom,
        MailboxParticipantIdentity providerSender)
    {
        HeaderAddress headerFrom = HeaderAddressValue(message, "From");
        HeaderAddress headerSender = HeaderAddressValue(message, "Sender");
        return (headerFrom.Address is not null && !string.Equals(headerFrom.Address, providerFrom.Address, StringComparison.OrdinalIgnoreCase)) ||
            (headerSender.Address is not null && !string.Equals(headerSender.Address, providerSender.Address, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> SelectedHeaderEvidenceRefs(GraphMailboxMessage message, string name)
    {
        int ordinal = 0;
        foreach (GraphMailboxInternetMessageHeader header in message.InternetMessageHeaders)
        {
            if (string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"header:{CanonicalHeaderName(name)}:{ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                ordinal++;
            }
        }
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
            "Reply-To" => "Reply-To",
            "X-Original-Sender" => "X-Original-Sender",
            "Sender" => "Sender",
            "From" => "From",
            _ => name,
        };

    private sealed record HeaderAddress(string? Address, MailboxHeaderValueState State);
}
