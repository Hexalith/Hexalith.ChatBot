using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class AiExecutionCoordinatorTests
{
    [ThreadStatic]
    private static bool _insideCancellationProjectionCall;

    [Fact]
    public void ProductionStartupShouldRequireDurableAiExecutionStoreWhileDevelopmentRetainsCompatibility()
    {
        InvalidOperationException failure = Should.Throw<InvalidOperationException>(
            () => AiExecutionStoreConfiguration.RequireDurableStore(isProduction: true, useDaprStateStores: false));

        failure.Message.ShouldContain("ChatBot:UseDaprStateStores=true");
        Should.NotThrow(
            () => AiExecutionStoreConfiguration.RequireDurableStore(isProduction: true, useDaprStateStores: true));
        Should.NotThrow(
            () => AiExecutionStoreConfiguration.RequireDurableStore(isProduction: false, useDaprStateStores: false));
    }

    private static readonly DateTimeOffset Now = new(2026, 8, 26, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PersistedStartedShouldOwnProviderInvocationAndTerminalSubmission()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway);

        await coordinator.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, Started(1), TestContext.Current.CancellationToken);
        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(1);
        SubmitCommandRequest submitted = gateway.Submitted.ShouldHaveSingleItem();
        submitted.AggregateId.ShouldBe("conversation-alpha");
        submitted.CommandType.ShouldBe("CompleteLowRiskAiAssistance");
        ChatBotIdentity.IsValidUlid(submitted.MessageId).ShouldBeTrue();
        submitted.MessageId.ShouldNotContain(":");
    }

    [Fact]
    public async Task TerminalRelayMessageIdShouldBeGatewayValidAndStableAcrossRestart()
    {
        InMemoryAiExecutionWorkStore store = new();
        LowRiskAiAssistanceExecutionStarted started = Started(1);
        RecordingGateway firstGateway = new();
        using (AiExecutionCoordinator first = Coordinator(
                   store,
                   new RecordingProvider(),
                   firstGateway,
                   new MutableClock(Now)))
        {
            await first.RecordStartedAsync(
                "tenant-alpha",
                "conversation-alpha",
                9,
                started,
                TestContext.Current.CancellationToken);
            await first.StartAsync(TestContext.Current.CancellationToken);
            await WaitUntilAsync(() => firstGateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
            await first.StopAsync(TestContext.Current.CancellationToken);
        }

        RecordingGateway restartedGateway = new();
        using (AiExecutionCoordinator restarted = Coordinator(
                   store,
                   new RecordingProvider(),
                   restartedGateway,
                   new MutableClock(Now.AddMinutes(3))))
        {
            await restarted.StartAsync(TestContext.Current.CancellationToken);
            await WaitUntilAsync(() => restartedGateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
            await restarted.StopAsync(TestContext.Current.CancellationToken);
        }

        string firstMessageId = firstGateway.Submitted.ShouldHaveSingleItem().MessageId;
        string restartedMessageId = restartedGateway.Submitted.ShouldHaveSingleItem().MessageId;
        ChatBotIdentity.IsValidUlid(firstMessageId).ShouldBeTrue();
        firstMessageId.ShouldBe(restartedMessageId);
        firstMessageId.ShouldAllBe(static character => char.IsAsciiLetterOrDigit(character));
    }

    [Fact]
    public async Task CancellationRelayMessageIdShouldBeGatewayValidAndBoundToCancellationIdentity()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new(blockUntilCancelled: true);
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway);
        await coordinator.RecordStartedAsync(
            "tenant-alpha",
            "conversation-alpha",
            9,
            Started(1),
            TestContext.Current.CancellationToken);
        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => provider.ActiveCount == 1, TestContext.Current.CancellationToken);

        await coordinator.RecordCancellationRequestedAsync(Cancellation(1), TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        SubmitCommandRequest submitted = gateway.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe(nameof(CompleteAiResponseGenerationCancellation));
        ChatBotIdentity.IsValidUlid(submitted.MessageId).ShouldBeTrue();
        submitted.MessageId.ShouldBe(AiExecutionCoordinator.TerminalMessageId(
            Item(Started(1)),
            nameof(CompleteAiResponseGenerationCancellation),
            "ai-cancellation-completion:cancellation-001"));
    }

    [Fact]
    public async Task TwoReplicasShouldClaimTheSamePersistedWorkOnlyOnce()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator replicaOne = Coordinator(store, provider, gateway);
        using AiExecutionCoordinator replicaTwo = Coordinator(store, provider, gateway);
        await replicaOne.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, Started(1), TestContext.Current.CancellationToken);

        await Task.WhenAll(
            replicaOne.StartAsync(TestContext.Current.CancellationToken),
            replicaTwo.StartAsync(TestContext.Current.CancellationToken));
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await Task.WhenAll(
            replicaOne.StopAsync(TestContext.Current.CancellationToken),
            replicaTwo.StopAsync(TestContext.Current.CancellationToken));

        provider.ExecuteCount.ShouldBe(1);
        gateway.Submitted.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ExpiredLeaseShouldBeRecoveredByAnotherReplicaWithoutLosingWork()
    {
        InMemoryAiExecutionWorkStore store = new();
        LowRiskAiAssistanceExecutionStarted started = Started(1);
        await store.UpsertStartedAsync(Item(started), TestContext.Current.CancellationToken);
        AiExecutionWorkItem? abandoned = await store.TryClaimAsync(
            Item(started).Key,
            "abandoned-replica",
            Now,
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);
        abandoned.ShouldNotBeNull();

        MutableClock clock = new(Now.AddSeconds(2));
        RecordingProvider provider = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator recovered = Coordinator(store, provider, gateway, clock);
        await recovered.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await recovered.StopAsync(TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(1);
        gateway.Submitted.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CancellationShouldReachTheOwningReplicaAndSubmitOnlyConfirmation()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new(blockUntilCancelled: true);
        RecordingGateway gateway = new();
        using AiExecutionCoordinator ownerReplica = Coordinator(store, provider, gateway);
        using AiExecutionCoordinator cancellationReplica = Coordinator(store, provider, gateway);
        await ownerReplica.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, Started(1), TestContext.Current.CancellationToken);
        await ownerReplica.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => provider.ActiveCount == 1, TestContext.Current.CancellationToken);

        await cancellationReplica.RecordCancellationRequestedAsync(
            Cancellation(1),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await ownerReplica.StopAsync(TestContext.Current.CancellationToken);

        provider.ObservedCancellation.ShouldBeTrue();
        gateway.Submitted.ShouldHaveSingleItem().CommandType.ShouldBe("CompleteAiResponseGenerationCancellation");
        gateway.Submitted.ShouldHaveSingleItem().Payload.GetProperty("Confirmed").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task OwningReplicaCancellationProjectionShouldReturnBeforeTerminalRelayCompletes()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new(blockUntilCancelled: true);
        BlockingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway);
        await coordinator.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, Started(1), TestContext.Current.CancellationToken);
        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => provider.ActiveCount == 1, TestContext.Current.CancellationToken);

        await coordinator.RecordCancellationRequestedAsync(Cancellation(1), TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await gateway.SubmissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        gateway.Release();
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        provider.ObservedCancellation.ShouldBeTrue();
        gateway.Submitted.ShouldHaveSingleItem().CommandType.ShouldBe("CompleteAiResponseGenerationCancellation");
    }

    [Fact]
    public async Task OwningReplicaCancellationProjectionShouldNotRunProviderCancellationCallbacksInline()
    {
        InMemoryAiExecutionWorkStore store = new();
        InlineCancellationCallbackProvider provider = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway);
        await coordinator.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, Started(1), TestContext.Current.CancellationToken);
        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await provider.Started.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        ValueTask request;
        _insideCancellationProjectionCall = true;
        try
        {
            request = coordinator.RecordCancellationRequestedAsync(Cancellation(1), TestContext.Current.CancellationToken);
        }
        finally
        {
            _insideCancellationProjectionCall = false;
        }

        await request.AsTask().WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await provider.CallbackObserved.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        provider.CallbackObservedInline.ShouldBeFalse();
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        gateway.Submitted.ShouldHaveSingleItem().CommandType.ShouldBe("CompleteAiResponseGenerationCancellation");
    }

    [Fact]
    public async Task RecoveredCancellationWithoutProviderObservationShouldSubmitFailedOutcome()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator recoveredReplica = Coordinator(store, provider, gateway);
        await recoveredReplica.RecordStartedAsync(
            "tenant-alpha",
            "conversation-alpha",
            9,
            Started(1),
            TestContext.Current.CancellationToken);
        await recoveredReplica.RecordCancellationRequestedAsync(Cancellation(1), TestContext.Current.CancellationToken);

        await recoveredReplica.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await recoveredReplica.StopAsync(TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(0);
        SubmitCommandRequest submitted = gateway.Submitted.ShouldHaveSingleItem();
        submitted.CommandType.ShouldBe("CompleteAiResponseGenerationCancellation");
        submitted.Payload.GetProperty("Confirmed").GetBoolean().ShouldBeFalse();
        submitted.Payload.GetProperty("FailureReasonCode").GetString().ShouldBe("provider-cancellation-unobserved");
    }

    [Fact]
    public async Task TerminalSubmissionShouldRetryWithinBoundWithoutRepeatingProvider()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new();
        RecordingGateway gateway = new(failuresBeforeSuccess: 2);
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway);
        await coordinator.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, Started(1), TestContext.Current.CancellationToken);

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.AttemptCount == 3, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(1);
        gateway.AttemptCount.ShouldBe(3);
        gateway.Submitted.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CoordinatorShouldBoundProviderConcurrencyAtFour()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingProvider provider = new(blockUntilReleased: true);
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway);
        for (int index = 1; index <= 8; index++)
        {
            await coordinator.RecordStartedAsync(
                "tenant-alpha",
                "conversation-alpha",
                8 + index,
                Started(index),
                TestContext.Current.CancellationToken);
        }

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => provider.ActiveCount == 4, TestContext.Current.CancellationToken);
        provider.MaximumActiveCount.ShouldBe(4);
        provider.Release();
        await WaitUntilAsync(() => gateway.Submitted.Count == 8, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBe(8);
        provider.MaximumActiveCount.ShouldBe(4);
    }

    [Fact]
    public async Task ProductionReadModelStoreShouldPreserveOutboxAndLeaseAcrossInstances()
    {
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore first = new(durable);
        ReadModelAiExecutionWorkStore restartedReplica = new(durable);
        LowRiskAiAssistanceExecutionStarted started = Started(1);
        AiExecutionWorkItem item = Item(started);
        await first.UpsertStartedAsync(item, TestContext.Current.CancellationToken);

        AiExecutionWorkItem? claimed = await first.TryClaimAsync(
            item.Key,
            "replica-one",
            Now,
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);
        claimed.ShouldNotBeNull();
        (await restartedReplica.TryClaimAsync(
            item.Key,
            "replica-two",
            Now,
            TimeSpan.FromMinutes(2),
            TestContext.Current.CancellationToken)).ShouldBeNull();
        await first.MarkCompletionPendingAsync(
            item.Key,
            "replica-one",
            CompletionRecord(item.Execution),
            Now,
            TestContext.Current.CancellationToken);

        AiExecutionWorkItem recovered = (await restartedReplica.TryClaimAsync(
            item.Key,
            "replica-two",
            Now.AddSeconds(2),
            TimeSpan.FromMinutes(2),
            TestContext.Current.CancellationToken)).ShouldNotBeNull();
        recovered.Status.ShouldBe(AiExecutionWorkStatus.CompletionPending);
        recovered.CompletionRecord.ShouldNotBeNull();
        await restartedReplica.MarkTerminalAsync(
            item.Key,
            "replica-two",
            Now.AddSeconds(2),
            TestContext.Current.CancellationToken);
        (await first.ListRunnableAsync(Now.AddMinutes(5), 10, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ExpiredOwnerShouldBeFencedFromRenewalAndEveryOwnedMutation()
    {
        InMemoryAiExecutionWorkStore store = new();
        AiExecutionWorkItem item = Item(Started(1));
        await store.UpsertStartedAsync(item, TestContext.Current.CancellationToken);
        (await store.TryClaimAsync(item.Key, "old-owner", Now, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken))
            .ShouldNotBeNull();

        (await store.TryRenewLeaseAsync(
            item.Key,
            "old-owner",
            Now.AddSeconds(2),
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken)).ShouldBeNull();
        (await store.MarkCompletionPendingAsync(
            item.Key,
            "old-owner",
            CompletionRecord(item.Execution),
            Now.AddSeconds(2),
            TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.ReleaseAsync(
            item.Key,
            "old-owner",
            Now.AddSeconds(2),
            TestContext.Current.CancellationToken)).ShouldBeFalse();

        (await store.TryClaimAsync(
            item.Key,
            "new-owner",
            Now.AddSeconds(2),
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Fact]
    public async Task LiveLeaseTheftWhileProviderIsActiveShouldCancelAndFenceOldOwnerWithoutTerminalRelay()
    {
        InMemoryAiExecutionWorkStore store = new();
        MutableClock clock = new(Now);
        RecordingProvider provider = new(blockUntilCancelled: true);
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, provider, gateway, clock);
        LowRiskAiAssistanceExecutionStarted started = Started(1);
        AiExecutionWorkItem item = Item(started);
        await coordinator.RecordStartedAsync("tenant-alpha", "conversation-alpha", 9, started, TestContext.Current.CancellationToken);
        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => provider.ActiveCount == 1, TestContext.Current.CancellationToken);

        clock.UtcNow = Now.AddMinutes(3);
        (await store.TryClaimAsync(
            item.Key,
            "replica-that-stole-expired-live-lease",
            clock.UtcNow,
            TimeSpan.FromMinutes(2),
            TestContext.Current.CancellationToken)).ShouldNotBeNull();
        await WaitUntilAsync(() => provider.ObservedCancellation, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        gateway.Submitted.ShouldBeEmpty();
        (await store.MarkCompletionPendingAsync(
            item.Key,
            "stale-owner",
            CompletionRecord(item.Execution),
            clock.UtcNow,
            TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public void WorkIdentityEncodingShouldNotAliasDelimiterBearingSegments()
    {
        string first = AiExecutionWorkItem.KeyFor("tenant:a", "project", "conversation", "response", "generation");
        string second = AiExecutionWorkItem.KeyFor("tenant", "a:project", "conversation", "response", "generation");

        first.ShouldNotBe(second);
        first.ShouldStartWith("ai-execution-v2.");
    }

    [Fact]
    public async Task DurableRecoveryIndexShouldMergeConcurrentWritersAcrossManySpillPages()
    {
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore[] replicas = Enumerable.Range(0, 8)
            .Select(_ => new ReadModelAiExecutionWorkStore(durable))
            .ToArray();
        AiExecutionWorkItem[] items = Enumerable.Range(1, 320)
            .Select(index => Item(Started(index)))
            .ToArray();

        await Task.WhenAll(items.Select((item, index) => replicas[index % replicas.Length]
            .UpsertStartedAsync(item, TestContext.Current.CancellationToken)
            .AsTask()));

        IReadOnlyList<AiExecutionWorkItem> recovered = await replicas[0]
            .ListRunnableAsync(Now, items.Length, TestContext.Current.CancellationToken);
        recovered.Select(static item => item.Key).Distinct(StringComparer.Ordinal).Count().ShouldBe(items.Length);
    }

    [Fact]
    public async Task LegacyAndV2EquivalentRowsShouldBeSuppressedBeforeProviderInvocation()
    {
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore store = new(durable);
        AiExecutionWorkItem current = Item(Started(1));
        AiExecutionWorkItem legacy = current with
        {
            Key = AiExecutionWorkItem.LegacyKeyFor(
                current.TenantId,
                current.ProjectId,
                current.ConversationId,
                current.ResponseId,
                current.GenerationId),
        };
        await durable.SaveAsync("chatbot-state", legacy.Key, legacy, TestContext.Current.CancellationToken);
        await durable.SaveAsync(
            "chatbot-state",
            "chatbot:ai-execution:index:v1",
            new AiExecutionWorkIndex([legacy.Key]),
            TestContext.Current.CancellationToken);
        await store.UpsertStartedAsync(current, TestContext.Current.CancellationToken);

        IReadOnlyList<AiExecutionWorkItem> runnable = await store.ListRunnableAsync(
            Now,
            10,
            TestContext.Current.CancellationToken);

        AiExecutionWorkItem selected = runnable.ShouldHaveSingleItem();
        selected.Key.ShouldBe(current.Key);
    }

    [Fact]
    public async Task CorruptIdentityAndAttemptOverflowShouldBeQuarantinedWithoutExecution()
    {
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore store = new(durable);
        AiExecutionWorkItem corrupt = Item(Started(1));
        AiExecutionWorkItem overflow = Item(Started(2));
        await store.UpsertStartedAsync(corrupt, TestContext.Current.CancellationToken);
        await store.UpsertStartedAsync(overflow, TestContext.Current.CancellationToken);
        await durable.SaveAsync(
            "chatbot-state",
            corrupt.Key,
            corrupt with { Execution = Execution(3) },
            TestContext.Current.CancellationToken);
        await durable.SaveAsync(
            "chatbot-state",
            overflow.Key,
            overflow with { AttemptCount = int.MaxValue },
            TestContext.Current.CancellationToken);

        (await store.ListRunnableAsync(Now, 10, TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await durable.GetAsync<AiExecutionWorkItem>("chatbot-state", corrupt.Key, TestContext.Current.CancellationToken))
            .Value!.Status.ShouldBe(AiExecutionWorkStatus.Quarantined);
        (await durable.GetAsync<AiExecutionWorkItem>("chatbot-state", overflow.Key, TestContext.Current.CancellationToken))
            .Value!.Status.ShouldBe(AiExecutionWorkStatus.Quarantined);
    }

    [Fact]
    public async Task FullBucketShouldSpillAndRotateBeyondFourPagesWithoutLosingWork()
    {
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore store = new(durable, maximumIndexPageSize: 1, maximumIndexPages: 8);
        AiExecutionWorkItem[] items = Enumerable.Range(1, 6).Select(index => Item(Started(index))).ToArray();

        foreach (AiExecutionWorkItem item in items)
        {
            await store.UpsertStartedAsync(item, TestContext.Current.CancellationToken);
        }

        IReadOnlyList<AiExecutionWorkItem> recovered = await store.ListRunnableAsync(
            Now,
            items.Length,
            TestContext.Current.CancellationToken);
        recovered.Select(static item => item.Key).ShouldBe(items.Select(static item => item.Key), ignoreOrder: true);
    }

    [Fact]
    public async Task ExhaustedCursorTraversalShouldReachEveryRowBeyondOneHundredAndAllowRecovery()
    {
        InMemoryAiExecutionWorkStore store = new();
        for (int index = 1; index <= 125; index++)
        {
            AiExecutionWorkItem item = Item(Started(index));
            await store.UpsertStartedAsync(item, TestContext.Current.CancellationToken);
            (await store.TryClaimAsync(item.Key, "exhaustion-owner", Now, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken))
                .ShouldNotBeNull();
            (await store.MarkExhaustedAsync(
                item.Key,
                "exhaustion-owner",
                Now,
                "forced-test-exhaustion",
                TestContext.Current.CancellationToken)).ShouldBeTrue();
        }

        List<AiExecutionWorkItem> traversed = [];
        string? afterKey = null;
        while (true)
        {
            IReadOnlyList<AiExecutionWorkItem> page = await store.ListExhaustedAsync(
                afterKey,
                17,
                TestContext.Current.CancellationToken);
            traversed.AddRange(page);
            if (page.Count < 17)
            {
                break;
            }

            afterKey = page[^1].Key;
        }

        traversed.Count.ShouldBe(125);
        traversed.Select(static item => item.Key).Distinct(StringComparer.Ordinal).Count().ShouldBe(125);
        (await store.RecoverExhaustedAsync(traversed[110].Key, Now.AddMinutes(2), TestContext.Current.CancellationToken))
            .ShouldBeTrue();
        (await store.ListRunnableAsync(Now.AddMinutes(2), 200, TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem().Key.ShouldBe(traversed[110].Key);
    }

    [Fact]
    public async Task AuthenticatedExhaustedOperationsShouldTraverseProtectedCursorBeyondOneHundredAndRecoverRow()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(
                services => services.AddSingleton<IStartupFilter, OperatorPrincipalStartupFilter>()));
        IAiExecutionWorkStore store = factory.Services.GetRequiredService<IAiExecutionWorkStore>();
        for (int index = 1; index <= 105; index++)
        {
            AiExecutionWorkItem item = Item(Started(index));
            await store.UpsertStartedAsync(item, TestContext.Current.CancellationToken);
            (await store.TryClaimAsync(item.Key, "operator-test-owner", Now, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken))
                .ShouldNotBeNull();
            (await store.MarkExhaustedAsync(
                item.Key,
                "operator-test-owner",
                Now,
                "test-exhaustion",
                TestContext.Current.CancellationToken)).ShouldBeTrue();
        }

        using HttpClient client = factory.CreateClient();
        List<string> keys = [];
        string? cursor = null;
        do
        {
            string requestUri = "/api/v1/operations/ai-executions/exhausted?pageSize=13"
                + (cursor is null ? string.Empty : $"&cursor={Uri.EscapeDataString(cursor)}");
            using HttpResponseMessage response = await client.GetAsync(requestUri, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            keys.AddRange(payload.RootElement.GetProperty("items").EnumerateArray().Select(static row => row.GetProperty("key").GetString()!));
            cursor = payload.RootElement.GetProperty("nextCursor").ValueKind is JsonValueKind.Null
                ? null
                : payload.RootElement.GetProperty("nextCursor").GetString();
        }
        while (cursor is not null);

        keys.Count.ShouldBe(105);
        using HttpResponseMessage recovery = await client.PostAsJsonAsync(
            "/api/v1/operations/ai-executions/exhausted/recover",
            new { key = keys[100] },
            TestContext.Current.CancellationToken);
        recovery.EnsureSuccessStatusCode();
        using JsonDocument recoveryPayload = JsonDocument.Parse(
            await recovery.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        recoveryPayload.RootElement.GetProperty("status").GetString().ShouldBe("recovered");
        recoveryPayload.RootElement.GetProperty("key").GetString().ShouldBe(keys[100]);
        // The hosted coordinator is intentionally live in this API test and may claim the recovered row before this
        // assertion. The stable postcondition is that recovery atomically removes it from the exhausted operator set;
        // the store-level test above separately proves the recovered Pending row is runnable.
        (await store.ListExhaustedAsync(null, 200, TestContext.Current.CancellationToken))
            .ShouldNotContain(item => string.Equals(item.Key, keys[100], StringComparison.Ordinal));
    }

    [Fact]
    public async Task TokenIgnoringProviderShouldBeRetiredAtItsSlotDeadlineAndAllowRefill()
    {
        InMemoryAiExecutionWorkStore store = new();
        TokenIgnoringBurstProvider provider = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(
            store,
            provider,
            gateway,
            providerExecutionDeadline: TimeSpan.FromMilliseconds(75));
        for (int index = 1; index <= 5; index++)
        {
            await coordinator.RecordStartedAsync(
                "tenant-alpha",
                "conversation-alpha",
                index,
                Started(index),
                TestContext.Current.CancellationToken);
        }

        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count > 0, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        provider.ExecuteCount.ShouldBeGreaterThan(4);
        gateway.Submitted.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task TransportAcceptanceShouldRemainRecoverableUntilPersistedTerminalTruthIsObserved()
    {
        InMemoryAiExecutionWorkStore store = new();
        RecordingGateway gateway = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, new RecordingProvider(), gateway);
        LowRiskAiAssistanceExecutionStarted started = Started(1);
        await coordinator.RecordStartedAsync(
            "tenant-alpha",
            "conversation-alpha",
            9,
            started,
            TestContext.Current.CancellationToken);
        await coordinator.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => gateway.Submitted.Count == 1, TestContext.Current.CancellationToken);
        await coordinator.StopAsync(TestContext.Current.CancellationToken);

        (await store.ListRunnableAsync(Now.AddMinutes(5), 10, TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();
        await coordinator.RecordTerminalObservedAsync(
            "tenant-alpha",
            "conversation-alpha",
            "project-alpha",
            started.ProposalId,
            started.ExecutionId,
            TestContext.Current.CancellationToken);
        (await store.ListRunnableAsync(Now.AddMinutes(5), 10, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task PersistedCancellationFailureShouldResetCancellationBudgetAndRestoreRunnableWork()
    {
        InMemoryAiExecutionWorkStore store = new();
        using AiExecutionCoordinator coordinator = Coordinator(store, new RecordingProvider(), new RecordingGateway());
        LowRiskAiAssistanceExecutionStarted started = Started(1);
        await coordinator.RecordStartedAsync(
            "tenant-alpha",
            "conversation-alpha",
            9,
            started,
            TestContext.Current.CancellationToken);
        await coordinator.RecordCancellationRequestedAsync(Cancellation(1), TestContext.Current.CancellationToken);

        await coordinator.RecordCancellationFailedAsync(
            new AiResponseGenerationCancellationFailed(
                "tenant-alpha",
                "project-alpha",
                "conversation-alpha",
                started.ProposalId,
                started.ExecutionId,
                "cancellation-001",
                started.CorrelationId,
                Now.AddSeconds(2),
                "provider-cancellation-unobserved",
                "metadata_only",
                "chatbot.ai-response-cancel.v1",
                "retry-stop"),
            TestContext.Current.CancellationToken);

        AiExecutionWorkItem runnable = (await store.ListRunnableAsync(Now.AddSeconds(2), 10, TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();
        runnable.Status.ShouldBe(AiExecutionWorkStatus.Pending);
        runnable.CancellationId.ShouldBeNull();
        runnable.TerminalSubmissionAttemptCount.ShouldBe(0);
    }

    private static AiExecutionCoordinator Coordinator(
        IAiExecutionWorkStore store,
        IAiAssistanceProvider provider,
        IEventStoreGatewayClient gateway,
        ISystemClock? clock = null,
        TimeSpan? providerExecutionDeadline = null)
        => new(
            store,
            provider,
            gateway,
            clock ?? new MutableClock(Now),
            NullLogger<AiExecutionCoordinator>.Instance,
            providerExecutionDeadline: providerExecutionDeadline);

    private static AiExecutionWorkItem Item(LowRiskAiAssistanceExecutionStarted started)
        => new(
            AiExecutionWorkItem.KeyFor("tenant-alpha", started.ProjectId, "conversation-alpha", started.ProposalId, started.ExecutionId),
            "tenant-alpha",
            started.ProjectId,
            "conversation-alpha",
            started.ProposalId,
            started.ExecutionId,
            9,
            started.Execution!,
            AiExecutionWorkStatus.Pending,
            started.CorrelationId,
            Now);

    private static LowRiskAiAssistanceExecutionStarted Started(int index)
    {
        ExecuteLowRiskAIAssistance execution = Execution(index);
        return new LowRiskAiAssistanceExecutionStarted(
            execution.ExecutionId,
            execution.ProposalId,
            execution.ProjectId,
            execution.TaskIntentId,
            execution.SourceMessageId,
            execution.RequesterId,
            "summarize-visible-context",
            execution.ContextPackageId,
            execution.ContextPackageVersion,
            execution.PolicySnapshotId!,
            "low_risk_tuple",
            execution.ExpectedProposalSourceVersion,
            execution.CorrelationId,
            Now,
            TenantId: "tenant-alpha",
            ConversationId: "conversation-alpha",
            SourceEvidenceReferences: execution.SourceEvidenceReferences,
            AuthorizedContextReferences: execution.AuthorizedContextReferences,
            ExcludedContextReasons: execution.ExcludedContextReasons,
            Execution: execution);
    }

    private static ExecuteLowRiskAIAssistance Execution(int index)
        => new(
            "project-alpha",
            $"response-{index:000}",
            $"task-intent-{index:000}",
            $"message-{index:000}",
            "requester-alpha",
            LowRiskAiAssistanceKind.SummarizeVisibleContext,
            $"context-package-{index:000}",
            "v1",
            "metadata_only",
            "collaboration_input",
            "disabled",
            [$"evidence-{index:000}"],
            [$"evidence-{index:000}"],
            ["redacted"],
            8,
            "policy-snapshot-alpha",
            $"correlation-{index:000}",
            $"generation-{index:000}",
            $"transition-{index:000}",
            RiskClassification: null,
            ExecutionRecord: null);

    private static AiResponseGenerationCancellationRequested Cancellation(int index)
        => new(
            "tenant-alpha",
            "project-alpha",
            "conversation-alpha",
            $"response-{index:000}",
            $"generation-{index:000}",
            "actor-alpha",
            9,
            $"correlation-{index:000}",
            $"cancellation-{index:000}",
            Now.AddSeconds(1),
            "metadata_only",
            "chatbot.ai-response-cancel.v1",
            10,
            "wait-for-executor");

    private static LowRiskAiAssistanceExecutionRecord CompletionRecord(ExecuteLowRiskAIAssistance execution)
        => new(
            execution.ExecutionId,
            execution.ProposalId,
            "summarize-visible-context",
            "success",
            "test-provider",
            "test-model-v1",
            Now,
            execution.SourceEvidenceReferences,
            execution.ContextPackageId,
            execution.ContextPackageVersion,
            execution.ContextPackageRedactionState,
            execution.PolicySnapshotId!,
            "low_risk_tuple",
            $"audit:{execution.ExecutionId}",
            "available",
            execution.CorrelationId,
            "metadata_only",
            "metadata_only",
            "none");

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("The AI execution coordinator did not reach the expected state.");
            }

            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class OperatorPrincipalStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (context, continuation) =>
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator-alpha")], "test"));
                    await continuation().ConfigureAwait(false);
                });
                next(app);
            };
    }

    private sealed class RecordingProvider(
        bool blockUntilCancelled = false,
        bool blockUntilReleased = false) : IAiAssistanceProvider
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCount;
        private int _executeCount;
        private int _maximumActiveCount;
        private int _observedCancellation;

        public int ActiveCount => Volatile.Read(ref _activeCount);

        public int ExecuteCount => Volatile.Read(ref _executeCount);

        public int MaximumActiveCount => Volatile.Read(ref _maximumActiveCount);

        public bool ObservedCancellation => Volatile.Read(ref _observedCancellation) == 1;

        public void Release() => _release.TrySetResult();

        public async ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
            AiAssistanceProviderRequest request,
            CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref _executeCount);
            int active = Interlocked.Increment(ref _activeCount);
            while (active > Volatile.Read(ref _maximumActiveCount))
            {
                _ = Interlocked.CompareExchange(ref _maximumActiveCount, active, Volatile.Read(ref _maximumActiveCount));
            }

            try
            {
                if (blockUntilCancelled)
                {
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _ = Interlocked.Exchange(ref _observedCancellation, 1);
                        throw;
                    }
                }
                else if (blockUntilReleased)
                {
                    await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }

                return new LowRiskAiAssistanceExecutionRecord(
                    request.ExecutionId,
                    request.ProposalId,
                    request.AssistanceKind,
                    "success",
                    "test-provider",
                    "test-model-v1",
                    Now,
                    request.SourceEvidenceReferences,
                    request.ContextPackageId,
                    request.ContextPackageVersion,
                    request.ContextRedactionState,
                    request.PolicySnapshotId,
                    request.PolicyReasonCode,
                    request.AuditOperationId,
                    "available",
                    request.CorrelationId,
                    "metadata_only",
                    "metadata_only",
                    "none");
            }
            finally
            {
                _ = Interlocked.Decrement(ref _activeCount);
            }
        }
    }

    private sealed class InlineCancellationCallbackProvider : IAiAssistanceProvider
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CallbackObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool CallbackObservedInline { get; private set; }

        public async ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
            AiAssistanceProviderRequest request,
            CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                CallbackObservedInline = _insideCancellationProjectionCall;
                CallbackObserved.TrySetResult();
            });
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The cancellation-only provider cannot complete naturally.");
        }
    }

    private sealed class RecordingGateway(int failuresBeforeSuccess = 0) : IEventStoreGatewayClient
    {
        private readonly ConcurrentQueue<SubmitCommandRequest> _submitted = new();
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public IReadOnlyList<SubmitCommandRequest> Submitted => _submitted.ToArray();

        public Task<SubmitCommandResponse> SubmitCommandAsync(
            SubmitCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int attempt = Interlocked.Increment(ref _attemptCount);
            if (attempt <= failuresBeforeSuccess)
            {
                throw new InvalidOperationException("transient event store failure");
            }

            _submitted.Enqueue(request);
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(
            StreamReadRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TokenIgnoringBurstProvider : IAiAssistanceProvider
    {
        private readonly TaskCompletionSource<LowRiskAiAssistanceExecutionRecord> _never =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _executeCount;

        public int ExecuteCount => Volatile.Read(ref _executeCount);

        public ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
            AiAssistanceProviderRequest request,
            CancellationToken cancellationToken)
        {
            int attempt = Interlocked.Increment(ref _executeCount);
            return attempt <= 4
                ? new ValueTask<LowRiskAiAssistanceExecutionRecord>(_never.Task)
                : ValueTask.FromResult(new LowRiskAiAssistanceExecutionRecord(
                    request.ExecutionId,
                    request.ProposalId,
                    request.AssistanceKind,
                    "success",
                    "test-provider",
                    "test-model-v1",
                    Now,
                    request.SourceEvidenceReferences,
                    request.ContextPackageId,
                    request.ContextPackageVersion,
                    request.ContextRedactionState,
                    request.PolicySnapshotId,
                    request.PolicyReasonCode,
                    request.AuditOperationId,
                    "available",
                    request.CorrelationId,
                    "metadata_only",
                    "metadata_only",
                    "none"));
        }
    }

    private sealed class BlockingGateway : IEventStoreGatewayClient
    {
        private readonly ConcurrentQueue<SubmitCommandRequest> _submitted = new();
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SubmissionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<SubmitCommandRequest> Submitted => _submitted.ToArray();

        public void Release() => _release.TrySetResult();

        public async Task<SubmitCommandResponse> SubmitCommandAsync(
            SubmitCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            SubmissionStarted.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            _submitted.Enqueue(request);
            return new SubmitCommandResponse(request.CorrelationId ?? request.MessageId);
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(
            StreamReadRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class DurableReadModelStore : IReadModelStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _versions = new(StringComparer.Ordinal);

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                return Task.FromResult(_values.TryGetValue(key, out object? value)
                    ? new ReadModelEntry<TValue>((TValue)value, _versions[key].ToString(System.Globalization.CultureInfo.InvariantCulture))
                    : new ReadModelEntry<TValue>(null, null));
            }
        }

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _values[key] = value;
                _versions[key] = _versions.GetValueOrDefault(key) + 1;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                string current = _versions.TryGetValue(key, out long version)
                    ? version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
                if (!string.Equals(current, etag, StringComparison.Ordinal))
                {
                    return Task.FromResult(false);
                }

                _values[key] = value;
                _versions[key] = version + 1;
                return Task.FromResult(true);
            }
        }
    }
}
