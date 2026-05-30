using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

public sealed class IdempotencyStateStoreIntegrationTests
{
    [Fact]
    public async Task EquivalentRepeatsShouldLeaveSingleCoarseRecordAndSingleDispatchInStateStoreLane()
    {
        FixedClock clock = new();
        InMemoryCoarseIdempotencyStore store = new(clock);
        RecordingDispatcher dispatcher = new();
        CommandGateway gateway = new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            store,
            new RecordingAuditWriter(),
            new RecordingReplayIntentQueue(),
            new RecordingOperatorAlertSink(),
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher);
        ChatBotCommandSubmission submission = Submission();

        ChatBotGatewayResult single = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        CoarseIdempotencyRecord storedAfterSingle = store.Records.Single();
        ChatBotGatewayResult repeat = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        CoarseIdempotencyRecord storedAfterRepeat = store.Records.Single();

        single.IsAccepted.ShouldBeTrue();
        repeat.IsAccepted.ShouldBeTrue();
        repeat.Accepted.ShouldNotBeNull();
        repeat.Accepted.CommandId.ShouldBe(single.Accepted!.CommandId);
        dispatcher.DispatchCount.ShouldBe(1);
        storedAfterRepeat.ShouldBe(storedAfterSingle);
    }

    private static ChatBotCommandSubmission Submission()
        => new(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("sub", "actor-alpha"), new Claim("eventstore:tenant", "tenant-alpha")],
                    "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = nameof(TenantScopedCommand),
                Command = new TenantScopedCommand("tenant-alpha", "allowed-resource"),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX");

    private sealed record TenantScopedCommand(string TenantId, string ResourceName) : IChatBotCommand;

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 30, 18, 30, 0, TimeSpan.Zero);
    }

    private sealed class RecordingDispatcher : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            DispatchCount++;
            return ValueTask.FromResult(new ChatBotDispatchResult(new DateTimeOffset(2026, 5, 30, 18, 30, 1, TimeSpan.Zero)));
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Success);

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Success);
    }

    private sealed class RecordingReplayIntentQueue : IAuditReplayIntentQueue
    {
        public ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class RecordingOperatorAlertSink : IOperatorAlertSink
    {
        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
