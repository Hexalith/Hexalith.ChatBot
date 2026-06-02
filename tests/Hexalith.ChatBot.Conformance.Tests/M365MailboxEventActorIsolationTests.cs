using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Workers.Mailbox;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

public sealed class M365MailboxEventActorIsolationTests
{
    [Fact]
    public async Task ForeignMailboxNotificationShouldFailClosedWithoutFetchSubmitOrLeakage()
    {
        RecordingGraphSource source = new();
        RecordingClient client = new();
        GraphMailboxIntakeWorker worker = new(
            new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1"),
            source,
            client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("foreign-mailbox-tenant-beta", "foreign-message-tenant-beta", "opaque-provider-state"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_scope_mismatch");
        source.FetchCount.ShouldBe(0);
        client.SubmitCount.ShouldBe(0);
        result.ToString().ShouldNotContain("foreign-message-tenant-beta", Case.Sensitive);
        result.ToString().ShouldNotContain("opaque-provider-state", Case.Sensitive);
    }

    [Fact]
    public async Task ForeignFetchedMailboxMessageShouldFailClosedWithoutSubmitOrLeakage()
    {
        RecordingGraphSource source = new(GraphMailboxFetchResult.Found(new GraphMailboxMessage(
            "foreign-mailbox-tenant-beta",
            "foreign-message-tenant-beta",
            "<foreign-message@example.test>",
            "foreign-conversation",
            null,
            new GraphMailboxParticipant("foreign-sender@example.test", null),
            null,
            [],
            [new GraphMailboxRecipient("foreign-project@example.test", null, "to")],
            DateTimeOffset.UtcNow,
            null,
            null,
            "UTC",
            [],
            [new GraphMailboxInternetMessageHeader("Authentication-Results", "spf=pass smtp.mailfrom=foreign.example")])));
        RecordingClient client = new();
        GraphMailboxIntakeWorker worker = new(
            new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1"),
            source,
            client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-provider-state"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_message_scope_mismatch");
        source.FetchCount.ShouldBe(1);
        client.SubmitCount.ShouldBe(0);
        result.ToString().ShouldNotContain("foreign-mailbox-tenant-beta", Case.Sensitive);
        result.ToString().ShouldNotContain("foreign-message-tenant-beta", Case.Sensitive);
        result.ToString().ShouldNotContain("foreign.example", Case.Sensitive);
        result.ToString().ShouldNotContain("opaque-provider-state", Case.Sensitive);
    }

    private sealed class RecordingGraphSource(GraphMailboxFetchResult? result = null) : IGraphMailboxMessageSource
    {
        public int FetchCount { get; private set; }

        public ValueTask<GraphMailboxFetchResult> FetchMessageAsync(GraphMailboxNotification notification, CancellationToken cancellationToken)
        {
            FetchCount++;
            return result is null
                ? throw new InvalidOperationException("Foreign mailbox should not be fetched.")
                : ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingClient : IChatBotClient
    {
        public int SubmitCount { get; private set; }

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            SubmitCount++;
            throw new InvalidOperationException("Foreign mailbox should not submit.");
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
}
