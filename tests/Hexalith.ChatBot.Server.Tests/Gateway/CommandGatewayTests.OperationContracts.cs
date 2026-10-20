using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Tests.Operations;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Fact]
    public async Task NewCommandIdShouldNotAliasHistoricalTaskBasedOperationBeforeDispatch()
    {
        InMemoryOperationStatusStore status = new();
        CommandSubmissionResponse historical = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FBA", OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            AcceptedAt = FixedClock.FixedUtcNow, LifecycleState = LifecycleState.Proposed,
        };
        await status.UpsertAsync(OperationStatusRecord.Accepted(BoundTenant, historical, false, historical.AcceptedAt), TestContext.Current.CancellationToken);
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore idempotency = new(new FixedClock());
        CommandGateway gateway = Gateway(dispatcher, operationStatusStore: status, idempotencyStore: idempotency, auditWriter: audit);
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "new-resource")), TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().Status.ShouldBe(409);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        OperationStatusRecord stored = (await status.TryGetAsync(BoundTenant, historical.OperationId, TestContext.Current.CancellationToken))!;
        stored.CommandId.ShouldBe(historical.CommandId);
        stored.OperationId.ShouldBe(historical.OperationId);
        stored.AcceptedAt.ShouldBe(historical.AcceptedAt);
    }

    [Fact]
    public async Task SeparateCommandsSharingTaskIdShouldHaveIndependentLifetimeOperationStatus()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryOperationStatusStore status = new();
        CommandGateway gateway = Gateway(dispatcher, operationStatusStore: status);
        ChatBotCommandSubmission first = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource-first"));
        ChatBotCommandSubmission second = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource-second"),
            commandId: "01ARZ3NDEKTSV4RRFFQ69G5FBA");
        first.TaskId.ShouldBe(second.TaskId);
        ChatBotGatewayResult one = await gateway.SubmitAsync(first, TestContext.Current.CancellationToken);
        ChatBotGatewayResult two = await gateway.SubmitAsync(second, TestContext.Current.CancellationToken);
        one.IsAccepted.ShouldBeTrue();
        two.IsAccepted.ShouldBeTrue();
        one.Accepted!.OperationId.ShouldBe(first.Request.CommandId);
        two.Accepted!.OperationId.ShouldBe(second.Request.CommandId);
        one.Accepted.OperationId.ShouldNotBe(two.Accepted.OperationId);
        foreach (ChatBotGatewayResult result in new[] { one, two })
        {
            OperationStatusRecord record = (await status.TryGetAsync(BoundTenant, result.Accepted!.OperationId, TestContext.Current.CancellationToken))!;
            record.CommandId.ShouldBe(result.Accepted.CommandId);
            record.CommittedRevision.ShouldBeNull();
            record.ExecutionState.ShouldBeNull();
            record.CompletionStatus.ShouldBe(OperationStatusRecord.AcceptedProjectionPending);
            record.Origin.ShouldBeNull();
            record.ResourceIds.ShouldBeNull();
            record.CreatedAt.ShouldBeNull();
            record.EquivalenceVersion.ShouldBeNull();
        }
        dispatcher.DispatchCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DuplicateDuringInitialStatusWriteShouldPreserveReceiptAndSingleDispatchEvenIfInitialStatusFails(bool failInitialWrite, bool advancingClock)
    {
        RecordingDispatcher dispatcher = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        DelayedInitialOperationStatusStore status = new(failInitialWrite);
        CommandGateway gateway = Gateway(dispatcher, clock: clock, idempotencyStore: idempotency, operationStatusStore: status);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));
        Task<ChatBotGatewayResult> initial = gateway.SubmitAsync(submission, TestContext.Current.CancellationToken).AsTask();
        await status.FirstWriteEntered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        CommandSubmissionResponse receipt = idempotency.Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        string originalReceipt = JsonSerializer.Serialize(receipt);
        if (advancingClock) { clock.UtcNow = clock.UtcNow.AddSeconds(1); }
        try
        {
            ChatBotGatewayResult racingReplay = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
            racingReplay.IsAccepted.ShouldBeTrue();
            AssertPreparedOutcome(racingReplay.Accepted!, receipt);
        }
        finally
        {
            status.ReleaseFirstWrite();
        }
        if (failInitialWrite)
        {
            await Should.ThrowAsync<InvalidOperationException>(() => initial);
        }
        else
        {
            ChatBotGatewayResult original = await initial.ConfigureAwait(true);
            original.IsAccepted.ShouldBeTrue();
            AssertPreparedOutcome(original.Accepted!, receipt);
        }
        ChatBotGatewayResult recovered = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        recovered.IsAccepted.ShouldBeTrue();
        AssertPreparedOutcome(recovered.Accepted!, receipt);
        JsonSerializer.Serialize(idempotency.Records.ShouldHaveSingleItem().PriorOutcome).ShouldBe(originalReceipt);
        dispatcher.DispatchCount.ShouldBe(1);
        OperationStatusRecord stored = (await status.TryGetAsync(BoundTenant, receipt.OperationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        stored.OperationId.ShouldBe(receipt.CommandId);
        stored.CorrelationId.ShouldBe(receipt.CorrelationId);
        stored.AcceptedAt.ShouldBe(receipt.AcceptedAt);
        stored.PriorOutcome.ShouldNotBeNull();
        AssertPreparedOutcome(stored.PriorOutcome, receipt);
        stored.Origin.ShouldBeNull();
        stored.ResourceIds.ShouldBeNull();
        stored.CreatedAt.ShouldBeNull();
        stored.EquivalenceVersion.ShouldBeNull();
        stored.CanonicalRevision.ShouldBeNull();
        stored.AuditStatus.ShouldBe(failInitialWrite ? OperationStatusRecord.AuditReconciling : OperationStatusRecord.AuditCommitted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CollisionLookupFailureShouldReturnUnavailableWithoutReservationAndAllowLaterRetry(bool cancelled)
    {
        bool fail = true;
        IOperationStatusStore status = TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(new InMemoryOperationStatusStore(), method =>
        {
            if (fail && method == "TryGetAsync")
            {
                if (cancelled) { throw new OperationCanceledException("Synthetic dependency read cancellation."); }
                throw new IOException("Synthetic status lookup failure.");
            }
        });
        InMemoryCoarseIdempotencyStore idempotency = new(new FixedClock());
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: idempotency, operationStatusStore: status, auditWriter: audit);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));
        ChatBotGatewayResult unavailable = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        unavailable.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        fail = false;
        (await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        idempotency.Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task CallerCancellationDuringCollisionLookupShouldPropagateWithoutReservationAuditOrDispatch()
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        TaskCompletionSource lookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IOperationStatusStore status = TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(new InMemoryOperationStatusStore(), method =>
        {
            if (method == "TryGetAsync")
            {
                lookupEntered.TrySetResult();
                caller.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue();
                caller.Token.ThrowIfCancellationRequested();
            }
        });
        InMemoryCoarseIdempotencyStore idempotency = new(new FixedClock());
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: idempotency, operationStatusStore: status, auditWriter: audit);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));
        Task<ChatBotGatewayResult> pending = gateway.SubmitAsync(submission, caller.Token).AsTask();
        await lookupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        caller.Cancel();
        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() => pending);
        exception.CancellationToken.ShouldBe(caller.Token);
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldBeEmpty();
        dispatcher.DispatchCount.ShouldBe(0);
    }

    [Fact]
    public async Task CallerCancellationDuringFailedCollisionLookupShouldPropagateWithoutReservationAuditOrDispatch()
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        IOperationStatusStore status = TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(new InMemoryOperationStatusStore(), method =>
        {
            if (method == "TryGetAsync")
            {
                caller.Cancel();
                throw new IOException("Synthetic status lookup failure after caller cancellation.");
            }
        });
        InMemoryCoarseIdempotencyStore idempotency = new(new FixedClock());
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: idempotency, operationStatusStore: status, auditWriter: audit);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() => gateway.SubmitAsync(submission, caller.Token).AsTask());
        exception.CancellationToken.ShouldBe(caller.Token);
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldBeEmpty();
        dispatcher.DispatchCount.ShouldBe(0);
    }

    [Fact]
    public async Task CanonicalGuardShouldPreserveAuthorizationDenialAuditAndCounterPrecedence()
    {
        FixedClock clock = new();
        RecordingAuditWriter audit = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        RecordingDispatcher dispatcher = new();
        CommandGateway gateway = Gateway(dispatcher, authorizationStage: new DenyingAuthorizationStage(ChatBotAuthorizationReasonCodes.AuthorizationDenied),
            auditWriter: audit, idempotencyStore: idempotency, authorizationFailureCounter: counter);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));
        submission.Request.Concurrency = new() { ExpectedRevision = 0 };
        ChatBotGatewayResult result = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        result.Problem.ShouldNotBeNull().Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task CanonicalLiveRequestShouldFailClosedBeforeReservationOrDispatchDespiteSyntheticEvidence()
    {
        // Folding a synthetic operation must not enable a live canonical path or create an approval flag.
        CommandOperationContractTests.Received().Revision.ShouldBe(0);
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotency = new(new FixedClock());
        InMemoryOperationStatusStore status = new();
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: idempotency, operationStatusStore: status);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));
        submission.Request.Concurrency = new CommandOperationConcurrency { ExpectedRevision = 0, DecisionSlotId = "decision:alpha" };
        ChatBotGatewayResult result = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        result.Problem.Retryable.ShouldBeFalse();
        result.Problem.ClientAction.ShouldBe(ProblemDetailsClientAction.None);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.RecordCount.ShouldBe(0);
        (await status.TryGetAsync(BoundTenant, submission.Request.CommandId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }
}
