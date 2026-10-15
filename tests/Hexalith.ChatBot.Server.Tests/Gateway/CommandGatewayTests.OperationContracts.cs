using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
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
        CommandGateway gateway = Gateway(dispatcher, operationStatusStore: status);
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "new-resource")), TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().Status.ShouldBe(409);
        dispatcher.DispatchCount.ShouldBe(0);
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateDuringInitialStatusWriteShouldPreserveReceiptAndSingleDispatchEvenIfInitialStatusFails(bool failInitialWrite)
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotency = new(new FixedClock());
        DelayedInitialOperationStatusStore status = new(failInitialWrite);
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: idempotency, operationStatusStore: status);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"));
        Task<ChatBotGatewayResult> initial = gateway.SubmitAsync(submission, TestContext.Current.CancellationToken).AsTask();
        await status.FirstWriteEntered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        CommandSubmissionResponse receipt = idempotency.Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        string originalReceipt = JsonSerializer.Serialize(receipt);
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
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.RecordCount.ShouldBe(0);
        (await status.TryGetAsync(BoundTenant, submission.Request.CommandId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }
}
