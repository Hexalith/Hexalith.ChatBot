using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Workers.Mailbox;

using Shouldly;

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
        string sourceTimezone = "UTC")
        => new(
            mailboxId,
            providerMessageId,
            "<message-001@example.test>",
            "graph-conversation-001",
            "graph-thread-001",
            new GraphMailboxParticipant("sender@example.test", "Sender"),
            [new GraphMailboxRecipient("project@example.test", "Project", "to")],
            receivedAt ?? new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.Zero),
            sentAt ?? new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.Zero),
            createdAt ?? new DateTimeOffset(2026, 5, 30, 10, 5, 0, TimeSpan.Zero),
            sourceTimezone,
            [new GraphMailboxAttachment("attachment-001", "evidence.pdf", "application/pdf", 1024)]);

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
    }

    private sealed record Submission(
        IChatBotCommand Command,
        string? CorrelationId,
        string? TaskId,
        ChatBotSurfaceOrigin Origin);
}
