using System.Reflection;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Fact]
    public async Task InMemoryConcurrentIdenticalCallerClaimAfterExpiryMustReplayWinningDomainOutcome()
    {
        MutableClock clock = new(FixedClock.FixedUtcNow);
        InMemoryCoarseIdempotencyStore store = new(clock);
        ChatBotGatewayContext original = DirectContext(AssociationDecisionCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision first = await store.RecordAdmissionAsync(original, TestContext.Current.CancellationToken);
        ChatBotGatewayContext sameCaller = DirectContext(AssociationDecisionCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FBB");
        Task<CoarseIdempotencyDecision> waiting = store.RecordAdmissionAsync(sameCaller, TestContext.Current.CancellationToken).AsTask();
        waiting.IsCompleted.ShouldBeFalse();
        CommandSubmissionResponse oldOutcome = PreparedOutcome(original, clock.UtcNow);
        CommandSubmissionResponse currentOutcome;
        CoarseIdempotencyDecision currentReplay;
        Lock gate = (Lock)typeof(InMemoryCoarseIdempotencyStore).GetField("_sync", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store)!;
        // Hold the real final-claim lock while waking the older replay continuation,
        // then expire its domain and establish the identical caller's new winner.
        lock (gate)
        {
            ReadCompleted(store.RecordOutcomeAsync(first.Metadata, oldOutcome, TestContext.Current.CancellationToken));
            clock.UtcNow = clock.UtcNow.AddHours(25);
            ValueTask<CoarseIdempotencyDecision> admission = store.RecordAdmissionAsync(sameCaller, TestContext.Current.CancellationToken);
            admission.IsCompletedSuccessfully.ShouldBeTrue();
            CoarseIdempotencyDecision current = ReadCompleted(admission);
            current.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
            currentOutcome = PreparedOutcome(sameCaller, clock.UtcNow);
            ReadCompleted(store.RecordOutcomeAsync(current.Metadata, currentOutcome, TestContext.Current.CancellationToken));
            currentReplay = ReadCompleted(store.RecordAdmissionAsync(sameCaller, TestContext.Current.CancellationToken));
        }
        CoarseIdempotencyDecision oldReplay = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        oldReplay.Kind.ShouldBe(CoarseIdempotencyDecisionKind.ReplayPriorOutcome);
        currentReplay.Kind.ShouldBe(CoarseIdempotencyDecisionKind.ReplayPriorOutcome);
        AssertPreparedOutcome(oldReplay.PriorOutcome!, currentOutcome);
        AssertPreparedOutcome(currentReplay.PriorOutcome!, currentOutcome);
        oldReplay.PriorOutcome!.CommandId.ShouldNotBe(oldOutcome.CommandId);
        store.Records.ShouldHaveSingleItem().PriorOutcome!.CommandId.ShouldBe(currentOutcome.CommandId);
    }
    private static T ReadCompleted<T>(ValueTask<T> operation)
    {
        // The controlled lock cannot be held across an await. Assert synchronous
        // completion first so GetResult here never blocks a test continuation.
        operation.IsCompletedSuccessfully.ShouldBeTrue();
        return operation.GetAwaiter().GetResult();
    }

    private static void ReadCompleted(ValueTask operation)
    {
        operation.IsCompletedSuccessfully.ShouldBeTrue();
        operation.GetAwaiter().GetResult();
    }
}
