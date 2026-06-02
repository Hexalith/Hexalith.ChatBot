using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Workers.Mailbox;

using Shouldly;

using ContractCaptureMailboxMessageIntake = Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake;
using ContractMailboxAuthenticationVerdictKind = Hexalith.ChatBot.Contracts.Enums.MailboxAuthenticationVerdictKind;
using ContractMailboxAuthenticityMetadata = Hexalith.ChatBot.Contracts.Commands.MailboxAuthenticityMetadata;
using ContractMailboxAuthenticityStrictness = Hexalith.ChatBot.Contracts.Enums.MailboxAuthenticityStrictness;
using ContractMailboxDelegatedSenderState = Hexalith.ChatBot.Contracts.Enums.MailboxDelegatedSenderState;
using ContractMailboxHeaderDiscrepancyKind = Hexalith.ChatBot.Contracts.Enums.MailboxHeaderDiscrepancyKind;
using ContractMailboxHeaderValueState = Hexalith.ChatBot.Contracts.Enums.MailboxHeaderValueState;
using ContractMailboxPartyResolutionState = Hexalith.ChatBot.Contracts.Enums.MailboxPartyResolutionState;

namespace Hexalith.ChatBot.Workers.Tests.Mailbox;

public sealed class GraphMailboxIntakeWorkerTests
{
    [Fact]
    public async Task CreatedNotificationShouldSubmitTypedMailboxCommandWithMailboxOrigin()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        client.Submissions.Count.ShouldBe(1);
        client.Submissions[0].Origin.ShouldBe(ChatBotSurfaceOrigin.Mailbox);
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake command =
            client.Submissions[0].Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        command.Source.ProviderMessageId.ShouldBe("graph-message-001");
        command.Source.InternetMessageId.ShouldBe("<message-001@example.test>");
        command.Source.ConversationId.ShouldBe("graph-conversation-001");
        command.Source.MailboxId.ShouldBe("controlled-mailbox-001");
        command.Source.ReceivedAt.Offset.ShouldBe(TimeSpan.Zero);
        command.Source.SourceContext.ShouldBe("graph-message-v1");
        command.Recipients.Single().Kind.ShouldBe("to");
        command.Attachments.Single().ProviderAttachmentId.ShouldBe("attachment-001");
    }

    [Fact]
    public async Task DuplicateNotificationShouldSubmitSameProviderIdentityForGatewayIdempotency()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);
        GraphMailboxNotification notification = new("controlled-mailbox-001", "graph-message-001", null);

        _ = await worker.ProcessAsync(notification, cancellationToken: TestContext.Current.CancellationToken);
        _ = await worker.ProcessAsync(notification, cancellationToken: TestContext.Current.CancellationToken);

        client.Submissions.Count.ShouldBe(2);
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake first =
            client.Submissions[0].Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake second =
            client.Submissions[1].Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        first.IntakeId.ShouldNotBe(second.IntakeId);
        first.Source.MailboxId.ShouldBe(second.Source.MailboxId);
        first.Source.ProviderMessageId.ShouldBe(second.Source.ProviderMessageId);
    }

    [Fact]
    public async Task ProviderLocalTimestampsShouldSubmitUtcWhilePreservingSourceTimezoneContext()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            receivedAt: new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.FromHours(2)),
            sentAt: new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.FromHours(2)),
            createdAt: new DateTimeOffset(2026, 5, 30, 10, 5, 0, TimeSpan.FromHours(2)),
            sourceTimezone: "W. Europe Standard Time");
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake command =
            client.Submissions.Single().Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        command.Source.ReceivedAt.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 15, 0, TimeSpan.Zero));
        command.Source.SentAt.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 10, 0, TimeSpan.Zero));
        command.Source.CreatedAt.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 5, 0, TimeSpan.Zero));
        command.Source.SourceTimezone.ShouldBe("W. Europe Standard Time");
        command.Source.SourceContext.ShouldBe("graph-message-v1");
    }

    [Fact]
    public async Task OpaqueProviderStateShouldNotBeForwardedIntoCommandOrResult()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-secret-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        string commandText = client.Submissions.Single().Command.ToString() ?? string.Empty;
        commandText.ShouldNotContain("opaque-secret-delta-token", Case.Sensitive);
        result.ToString().ShouldNotContain("opaque-secret-delta-token", Case.Sensitive);
    }

    [Fact]
    public async Task AuthenticationResultsHeadersShouldMapToMetadataOnlyVerdictsAndDiscrepancies()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            sender: new GraphMailboxParticipant("delegate@example.test", "Delegate"),
            replyTo: [new GraphMailboxParticipant("reply@example.test", "Reply")],
            headers:
            [
                new GraphMailboxInternetMessageHeader("Received", "from mx1.example.test by mx2.example.test"),
                new GraphMailboxInternetMessageHeader("authentication-results", "spf=pass smtp.mailfrom=sender.example; dkim=fail header.d=example.test; dmarc=bestguesspass action=none header.from=example.test; compauth=pass reason=109"),
                new GraphMailboxInternetMessageHeader("Authentication-Results", "spf=softfail smtp.mailfrom=other.example"),
                new GraphMailboxInternetMessageHeader("From", "Sender <sender@example.test>"),
                new GraphMailboxInternetMessageHeader("Sender", "Delegate <delegate@example.test>"),
                new GraphMailboxInternetMessageHeader("Reply-To", "Reply <reply@example.test>"),
                new GraphMailboxInternetMessageHeader("X-Original-Sender", "sender@example.test"),
                new GraphMailboxInternetMessageHeader("Subject", "must not be forwarded"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        ContractMailboxAuthenticityMetadata authenticity = command.Authenticity.ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(ContractMailboxAuthenticationVerdictKind.Ambiguous);
        authenticity.AuthenticationResults.Dkim.ShouldBe(ContractMailboxAuthenticationVerdictKind.Fail);
        authenticity.AuthenticationResults.Dmarc.ShouldBe(ContractMailboxAuthenticationVerdictKind.BestGuessPass);
        authenticity.AuthenticationResults.CompositeAuthentication.ShouldBe(ContractMailboxAuthenticationVerdictKind.Pass);
        authenticity.AuthenticationResults.CompositeAuthenticationReason.ShouldBe("109");
        authenticity.HeaderInspection.AuthenticationResultsHeaders.Select(static header => header.Ordinal).ShouldBe([0, 1], ignoreOrder: false);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.MultipleAuthenticationResults);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromSenderMismatch);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromReplyToMismatch);

        string commandText = command.ToString() ?? string.Empty;
        commandText.ShouldNotContain("Subject", Case.Insensitive);
        commandText.ShouldNotContain("must not be forwarded", Case.Insensitive);
        commandText.ShouldNotContain("smtp.mailfrom", Case.Insensitive);
    }

    [Fact]
    public async Task ProviderSenderDifferentFromFromShouldRecordDelegateAuthorityAndPrincipalFor()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            sender: new GraphMailboxParticipant("delegate@example.test", "Delegate"),
            headers:
            [
                new GraphMailboxInternetMessageHeader("From", "Sender <sender@example.test>"),
                new GraphMailboxInternetMessageHeader("Sender", "Header Delegate <delegate@example.test>"),
                new GraphMailboxInternetMessageHeader("Reply-To", "Sender <sender@example.test>"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        command.Source.Sender.Address.ShouldBe("delegate@example.test");
        command.Source.DelegatedSender.ShouldNotBeNull().State.ShouldBe(ContractMailboxDelegatedSenderState.Delegated);
        command.Source.DelegatedSender.Delegate.ShouldNotBeNull().Address.ShouldBe("delegate@example.test");
        command.Source.DelegatedSender.PrincipalFor.ShouldNotBeNull().Address.ShouldBe("sender@example.test");
        command.Source.DelegatedSender.EvidenceRefs.ShouldContain("provider:sender");
        command.Source.DelegatedSender.EvidenceRefs.ShouldContain("provider:from");
        command.Source.ExternalSender.ShouldNotBeNull().ExternalSender.ShouldBeTrue();
        command.Source.ExternalSender.PartyResolutionState.ShouldBe(ContractMailboxPartyResolutionState.Unavailable);
        command.Authenticity.ShouldNotBeNull().StrictnessPolicy.ShouldNotBeNull().Strictness.ShouldBe(ContractMailboxAuthenticityStrictness.Strict);
    }

    [Fact]
    public async Task HeaderProviderConflictShouldKeepProviderAuthorityAndRecordAmbiguousDelegation()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            sender: new GraphMailboxParticipant("delegate@example.test", "Delegate"),
            headers:
            [
                new GraphMailboxInternetMessageHeader("From", "Forged <forged@example.test>"),
                new GraphMailboxInternetMessageHeader("Sender", "Other <other@example.test>"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        _ = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        command.Source.Sender.Address.ShouldBe("delegate@example.test");
        command.Source.DelegatedSender.ShouldNotBeNull().State.ShouldBe(ContractMailboxDelegatedSenderState.Ambiguous);
        command.Source.DelegatedSender.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromSenderMismatch);
    }

    [Fact]
    public async Task RepeatedAuthenticationResultsShouldFillMissingVerdictsFromLaterHeaders()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            headers:
            [
                new GraphMailboxInternetMessageHeader("Authentication-Results", "spf=pass smtp.mailfrom=sender.example"),
                new GraphMailboxInternetMessageHeader("Authentication-Results", "dkim=pass header.d=example.test; dmarc=fail action=oreject header.from=example.test; compauth=fail reason=001"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractMailboxAuthenticityMetadata authenticity = client.Submissions.Single()
            .Command
            .ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(ContractMailboxAuthenticationVerdictKind.Pass);
        authenticity.AuthenticationResults.Dkim.ShouldBe(ContractMailboxAuthenticationVerdictKind.Pass);
        authenticity.AuthenticationResults.Dmarc.ShouldBe(ContractMailboxAuthenticationVerdictKind.Fail);
        authenticity.AuthenticationResults.CompositeAuthentication.ShouldBe(ContractMailboxAuthenticationVerdictKind.Fail);
        authenticity.AuthenticationResults.CompositeAuthenticationReason.ShouldBe("001");
        authenticity.HeaderInspection.AuthenticationResultsHeaders.Select(static header => header.Ordinal).ShouldBe([0, 1], ignoreOrder: false);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.MultipleAuthenticationResults);
    }

    [Fact]
    public async Task MissingAndMalformedHeadersShouldSubmitRecoverableMetadata()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            headers:
            [
                new GraphMailboxInternetMessageHeader("From", "not an address"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractMailboxAuthenticityMetadata authenticity = client.Submissions.Single()
            .Command
            .ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(ContractMailboxAuthenticationVerdictKind.NotSupplied);
        authenticity.HeaderInspection.From.ShouldBe(ContractMailboxHeaderValueState.Malformed);
        authenticity.HeaderInspection.Sender.ShouldBe(ContractMailboxHeaderValueState.NotSupplied);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.MalformedFrom);
    }

    [Theory]
    [InlineData("graph_throttled")]
    [InlineData("graph_subscription_expired")]
    [InlineData("graph_token_expired")]
    [InlineData("graph_partial_access")]
    public async Task RetryableGraphFailuresShouldRemainScopedRecoverableMailboxDegradation(string reasonCode)
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.RetryableFailure(reasonCode)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe(reasonCode);
        result.OperationClass.ShouldBe("message-intake");
        result.RetryCount.ShouldBe(1);
        result.MaxAttempts.ShouldBeGreaterThan(1);
        result.NextRetryAt.ShouldNotBeNull();
        result.SafeNextAction.ShouldBe("retry-later");
        client.Submissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task RevokedCredentialShouldRecoverWithoutSubmittingCommand()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.PermissionRevoked()), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("graph_permission_revoked");
        result.NextRetryAt.ShouldBeNull();
        result.OwnerRole.ShouldBe("tenant-admin");
        client.Submissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchedMessageWithDifferentProviderIdentityShouldFailClosedWithoutSubmit()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage mismatched = Message(providerMessageId: "foreign-message-002");
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(mismatched)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_message_scope_mismatch");
        client.Submissions.ShouldBeEmpty();
        result.ToString().ShouldNotContain("foreign-message-002", Case.Sensitive);
    }

    [Fact]
    public async Task FetchedMessageFromDifferentMailboxShouldFailClosedWithoutSubmit()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage mismatched = Message(mailboxId: "foreign-mailbox");
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(mismatched)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_message_scope_mismatch");
        client.Submissions.ShouldBeEmpty();
        result.ToString().ShouldNotContain("foreign-mailbox", Case.Sensitive);
    }

    [Theory]
    [InlineData(403, "authorization_denied")]
    [InlineData(503, "audit_unavailable")]
    public async Task RecoverableGatewaySubmissionFailuresShouldReturnSafeWorkerResult(int statusCode, string code)
    {
        RejectingChatBotClient client = new(statusCode, code);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-secret-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe(code);
        result.IntakeId.ShouldBeNull();
        result.ToString().ShouldNotContain("opaque-secret-delta-token", Case.Sensitive);
    }

    [Fact]
    public async Task ForeignMailboxShouldFailClosedBeforeGraphFetch()
    {
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), source, client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("foreign-mailbox", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_scope_mismatch");
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();
    }

    [Fact]
    public static void WorkerShouldDocumentLeastPrivilegeGraphPermission()
    {
        GraphMailboxIntakeWorker.LeastPrivilegeGraphPermission.ShouldBe("Mail.Read");
    }

    private static ControlledMailboxPattern Pattern()
        => new("controlled-mailbox-001", "graph-message-v1");

    private static GraphMailboxMessage Message(
        string mailboxId = "controlled-mailbox-001",
        string providerMessageId = "graph-message-001",
        DateTimeOffset? receivedAt = null,
        DateTimeOffset? sentAt = null,
        DateTimeOffset? createdAt = null,
        string sourceTimezone = "UTC",
        GraphMailboxParticipant? sender = null,
        IReadOnlyList<GraphMailboxParticipant>? replyTo = null,
        IReadOnlyList<GraphMailboxInternetMessageHeader>? headers = null)
        => new(
            mailboxId,
            providerMessageId,
            "<message-001@example.test>",
            "graph-conversation-001",
            "graph-thread-001",
            new GraphMailboxParticipant("sender@example.test", "Sender"),
            sender,
            replyTo ?? [],
            [new GraphMailboxRecipient("project@example.test", "Project", "to")],
            receivedAt ?? new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.Zero),
            sentAt ?? new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.Zero),
            createdAt ?? new DateTimeOffset(2026, 5, 30, 10, 5, 0, TimeSpan.Zero),
            sourceTimezone,
            [new GraphMailboxAttachment("attachment-001", "evidence.pdf", "application/pdf", 1024)],
            headers ?? []);

    private sealed class FakeGraphSource(GraphMailboxFetchResult result) : IGraphMailboxMessageSource
    {
        public int FetchCount { get; private set; }

        public ValueTask<GraphMailboxFetchResult> FetchMessageAsync(GraphMailboxNotification notification, CancellationToken cancellationToken)
        {
            FetchCount++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingChatBotClient : IChatBotClient
    {
        public List<Submission> Submissions { get; } = [];

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            Submissions.Add(new Submission(command, correlationId, taskId, origin));
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CorrelationId = correlationId ?? "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TaskId = taskId,
                LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Proposed,
                AcceptedAt = DateTimeOffset.UtcNow,
            });
        }

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RejectingChatBotClient(int statusCode, string code) : IChatBotClient
    {
        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
            => throw new HexalithChatBotApiException<ProblemDetails>(
                "Metadata-only failure.",
                statusCode,
                null,
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                new ProblemDetails
                {
                    Code = code,
                    Status = statusCode,
                    CorrelationId = correlationId ?? "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
                },
                innerException: null);

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed record Submission(
        IChatBotCommand Command,
        string? CorrelationId,
        string? TaskId,
        ChatBotSurfaceOrigin Origin);
}
