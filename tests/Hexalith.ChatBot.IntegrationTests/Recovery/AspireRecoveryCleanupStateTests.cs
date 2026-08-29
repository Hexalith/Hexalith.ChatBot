using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Always-run concrete regressions for recovery cleanup generation handoff.</summary>
public sealed class AspireRecoveryCleanupStateTests
{
    private const string Id1 = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string Id2 = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
    private const string Id3 = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string Id4 = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string Id5 = "01ARZ3NDEKTSV4RRFFQ69G5FB0";
    private const string Id6 = "01ARZ3NDEKTSV4RRFFQ69G5FB1";
    private const string Id7 = "01ARZ3NDEKTSV4RRFFQ69G5FB2";
    private const string Id8 = "01ARZ3NDEKTSV4RRFFQ69G5FB3";
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(3);

    [Fact]
    public async Task EventStorePreCommitFailureIsCleanedAndRetired()
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        List<string> diagnostics = [];
        RecoverySandboxOperationsTestSeam seam = SandboxSeam(
            submit: (_, _, _, _, _, _) => throw new InvalidOperationException("injected submission failure"),
            diagnostic: (value, _) =>
            {
                diagnostics.Add(value);
                return ValueTask.CompletedTask;
            });
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireRecoverySandboxOperations operations = SandboxOperations(seam, store, durable);

        await Should.ThrowAsync<InvalidOperationException>(() => operations.SeedCommittedOperationAsync(
            RecoveryValidationTopology.LogicalTenantRef,
            Id8,
            TestContext.Current.CancellationToken).AsTask());
        string unconfirmed = operations.ActiveEventStoreCleanupState.CheckpointNoteRefs.ShouldHaveSingleItem();

        bool complete = await operations.CleanupEventStoreScenarioAsync(TestContext.Current.CancellationToken);

        complete.ShouldBeFalse();
        diagnostics.ShouldBe(["RECOVERY_CLEANUP_INCOMPLETE checks=checkpoint-note-not-present"]);
        await AssertAbsentAsync(store, GovernedKey(RecoveryValidationTopology.StorageTenantRef, unconfirmed));
        AssertEventStoreEmpty(operations.ActiveEventStoreCleanupState);
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("negative")]
    public async Task EventStoreCleanupCoversCompleteAndAuxiliaryNegativeOutcomes(string scenario)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        List<string> diagnostics = [];
        RecoverySandboxOperationsTestSeam seam = SandboxSeam(
            submit: async (note, _, _, _, token, cancellationToken) =>
            {
                string tenant = string.Equals(token, "control", StringComparison.Ordinal)
                    ? RecoveryValidationTopology.ControlTenantRef
                    : RecoveryValidationTopology.StorageTenantRef;
                await store.SaveAsync(
                    ChatBotReadModelStoreNames.StateStoreName,
                    GovernedKey(tenant, note),
                    Governed(tenant, note),
                    cancellationToken).ConfigureAwait(false);
                handler.AddGovernedNote(tenant, note);
            },
            diagnostic: (value, _) =>
            {
                diagnostics.Add(value);
                return ValueTask.CompletedTask;
            });
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireRecoverySandboxOperations operations = SandboxOperations(seam, store, durable);
        _ = await operations.SeedCommittedOperationAsync(
            RecoveryValidationTopology.LogicalTenantRef,
            Id8,
            TestContext.Current.CancellationToken);
        List<string> checkpoints = [.. operations.ActiveEventStoreCleanupState.CheckpointNoteRefs];
        string control = operations.ActiveEventStoreCleanupState.ControlTenantNoteRef!;
        string fault = Id7;
        if (string.Equals(scenario, "complete", StringComparison.Ordinal))
        {
            operations.ActiveEventStoreCleanupState.FaultProbeNoteRef = fault;
        }

        if (string.Equals(scenario, "negative", StringComparison.Ordinal))
        {
            await EraseIfPresentAsync(store, GovernedKey(RecoveryValidationTopology.ControlTenantRef, control));
        }

        bool complete = await operations.CleanupEventStoreScenarioAsync(TestContext.Current.CancellationToken);

        complete.ShouldBe(string.Equals(scenario, "complete", StringComparison.Ordinal));
        if (string.Equals(scenario, "negative", StringComparison.Ordinal))
        {
            diagnostics.ShouldBe([
                "RECOVERY_CLEANUP_INCOMPLETE checks=control-note-not-present",
            ]);
        }
        else
        {
            diagnostics.ShouldBeEmpty();
        }

        foreach (string note in checkpoints)
        {
            await AssertAbsentAsync(store, GovernedKey(RecoveryValidationTopology.StorageTenantRef, note));
        }

        await AssertAbsentAsync(store, GovernedKey(RecoveryValidationTopology.ControlTenantRef, control));
        await AssertAbsentAsync(store, GovernedKey(RecoveryValidationTopology.StorageTenantRef, fault));
        AssertEventStoreEmpty(operations.ActiveEventStoreCleanupState);

        if (string.Equals(scenario, "negative", StringComparison.Ordinal))
        {
            AspireRecoverySandboxOperations faultPresent = SandboxOperations(seam, store, durable);
            faultPresent.ActiveEventStoreCleanupState.CheckpointCorrelationId = Id8;
            faultPresent.ActiveEventStoreCleanupState.FaultProbeNoteRef = fault;
            await store.SaveAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedKey(RecoveryValidationTopology.StorageTenantRef, fault),
                Governed(RecoveryValidationTopology.StorageTenantRef, fault),
                TestContext.Current.CancellationToken);
            (await faultPresent.CleanupEventStoreScenarioAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
            diagnostics[^1].ShouldBe("RECOVERY_CLEANUP_INCOMPLETE checks=fault-probe-projection-present");
            await AssertAbsentAsync(store, GovernedKey(RecoveryValidationTopology.StorageTenantRef, fault));
            AssertEventStoreEmpty(faultPresent.ActiveEventStoreCleanupState);

            AspireRecoverySandboxOperations reappearing = SandboxOperations(seam, store, durable);
            reappearing.ActiveEventStoreCleanupState.CheckpointCorrelationId = Id8;
            reappearing.ActiveEventStoreCleanupState.CheckpointNoteRefs.Add(Id6);
            reappearing.ActiveEventStoreCleanupState.FaultProbeNoteRef = fault;
            string checkpointKey = GovernedKey(RecoveryValidationTopology.StorageTenantRef, Id6);
            string faultKey = GovernedKey(RecoveryValidationTopology.StorageTenantRef, fault);
            await store.SaveAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                checkpointKey,
                Governed(RecoveryValidationTopology.StorageTenantRef, Id6),
                TestContext.Current.CancellationToken);
            bool checkpointErased = false;
            bool faultEraseRead = false;
            store.OnEraseKey = key =>
            {
                if (string.Equals(key, checkpointKey, StringComparison.Ordinal))
                {
                    checkpointErased = true;
                }
            };
            store.OnReadKey = key =>
            {
                if (!checkpointErased || !string.Equals(key, faultKey, StringComparison.Ordinal))
                {
                    return;
                }

                if (!faultEraseRead)
                {
                    faultEraseRead = true;
                    return;
                }

                store.SaveAsync(
                    ChatBotReadModelStoreNames.StateStoreName,
                    faultKey,
                    Governed(RecoveryValidationTopology.StorageTenantRef, fault)).GetAwaiter().GetResult();
            };
            (await reappearing.CleanupEventStoreScenarioAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
            diagnostics[^1].ShouldBe("RECOVERY_CLEANUP_INCOMPLETE checks=erased-projection-still-present");
            await AssertAbsentAsync(store, checkpointKey);
            await AssertPresentAsync(store, faultKey);
            AssertEventStoreEmpty(reappearing.ActiveEventStoreCleanupState);
        }
    }

    [Theory]
    [InlineData("exception")]
    [InlineData("cancellation")]
    public async Task EventStoreCleanupDetachesBeforeFirstAwaitAndPropagatesCancellation(string scenario)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        using EventStoreDurableStateProbe durable = Durable(handler);
        if (string.Equals(scenario, "exception", StringComparison.Ordinal))
        {
            RecoverySandboxOperationsTestSeam exceptionSeam = SandboxSeam(
                availability: _ => throw new InvalidOperationException("injected availability failure"));
            AspireRecoverySandboxOperations exceptionOperations = SandboxOperations(exceptionSeam, store, durable);
            SeedEventStoreState(exceptionOperations.ActiveEventStoreCleanupState);

            await Should.ThrowAsync<InvalidOperationException>(() => exceptionOperations
                .CleanupEventStoreScenarioAsync(TestContext.Current.CancellationToken).AsTask());
            AssertEventStoreEmpty(exceptionOperations.ActiveEventStoreCleanupState);
            return;
        }

        RecoverySandboxOperationsTestSeam alreadyCancelledSeam = SandboxSeam();
        AspireRecoverySandboxOperations alreadyCancelled = SandboxOperations(alreadyCancelledSeam, store, durable);
        SeedEventStoreState(alreadyCancelled.ActiveEventStoreCleanupState);
        using (CancellationTokenSource cancellation = new())
        {
            await cancellation.CancelAsync();
            await Should.ThrowAsync<OperationCanceledException>(() => alreadyCancelled
                .CleanupEventStoreScenarioAsync(cancellation.Token).AsTask());
        }

        AssertEventStoreEmpty(alreadyCancelled.ActiveEventStoreCleanupState);

        TaskCompletionSource writerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RecoverySandboxOperationsTestSeam blockedWriterSeam = SandboxSeam(
            diagnostic: async (_, cancellationToken) =>
            {
                writerEntered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            });
        AspireRecoverySandboxOperations blockedWriter = SandboxOperations(blockedWriterSeam, store, durable);
        SeedEventStoreState(blockedWriter.ActiveEventStoreCleanupState);
        using CancellationTokenSource blockedCancellation = new();
        Task cleanup = blockedWriter.CleanupEventStoreScenarioAsync(blockedCancellation.Token).AsTask();
        await writerEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        await blockedCancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => cleanup);
        AssertEventStoreEmpty(blockedWriter.ActiveEventStoreCleanupState);
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("witnesses")]
    [InlineData("reconcile")]
    public async Task SubscriptionProducersRetainEveryValidIdentityBeforeLaterFailure(string scenario)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        Queue<string> responses = new();
        RecoverySandboxOperationsTestSeam seam = SandboxSeam(responses: responses);
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireRecoverySandboxOperations operations = SandboxOperations(seam, store, durable);

        if (string.Equals(scenario, "validation", StringComparison.Ordinal))
        {
            responses.Enqueue(Process(Id1, Id1, submitted: false));
            await Should.ThrowAsync<InvalidOperationException>(() => operations
                .CheckpointSubscriptionCommittedBoundAsync(RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken)
                .AsTask());
            responses.Enqueue(Process(Id2, Id3));
            await Should.ThrowAsync<InvalidOperationException>(() => operations
                .CheckpointSubscriptionCommittedBoundAsync(RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken)
                .AsTask());
            int durableReadsBeforeMalformed = handler.Requests.Count;
            responses.Enqueue(Process(Id4, " "));
            await Should.ThrowAsync<InvalidOperationException>(() => operations
                .CheckpointSubscriptionCommittedBoundAsync(RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken)
                .AsTask());
            handler.Requests.Count.ShouldBe(durableReadsBeforeMalformed);

            await SaveSentinelsAsync(store, operations.ActiveSubscriptionCleanupState, identityBranch: false);
            responses.Enqueue(Process(Id5, Id5, submitted: false, candidateObservedAtUtc: "2026-08-01T00:00:00+01:00"));
            await Should.ThrowAsync<InvalidOperationException>(() => operations
                .RejectControlledLossCandidateAsync(RecoveryValidationTopology.LogicalTenantRef, TestContext.Current.CancellationToken)
                .AsTask());
            operations.ActiveSubscriptionCleanupState.StorageIntakeRefs.SetEquals([Id1, Id2, Id3, Id4, Id5]).ShouldBeTrue();
            operations.ActiveSubscriptionCleanupState.StorageDurableAbsenceRefs.ShouldContain(Id5);
            return;
        }

        if (string.Equals(scenario, "witnesses", StringComparison.Ordinal))
        {
            handler.AddMailboxIntake(RecoveryValidationTopology.StorageTenantRef, Id1);
            handler.AddMailboxIntake(RecoveryValidationTopology.StorageTenantRef, Id2);
            responses.Enqueue(Process(Id1, Id1));
            responses.Enqueue(Process(Id2, Id2));
            _ = await operations.WitnessControlledLossCommitAsync(
                RecoveryValidationTopology.LogicalTenantRef, preFault: true, TestContext.Current.CancellationToken);
            _ = await operations.WitnessControlledLossCommitAsync(
                RecoveryValidationTopology.LogicalTenantRef, preFault: false, TestContext.Current.CancellationToken);
            responses.Enqueue(Process(Id3, Id3));
            await Should.ThrowAsync<TimeoutException>(() => operations.WitnessControlledLossCommitAsync(
                RecoveryValidationTopology.LogicalTenantRef, preFault: true, TestContext.Current.CancellationToken).AsTask());
            operations.ActiveSubscriptionCleanupState.ControlledPreFaultIntakeRef.ShouldBe(Id1);
            operations.ActiveSubscriptionCleanupState.ControlledPostRecoveryIntakeRef.ShouldBe(Id2);
            operations.ActiveSubscriptionCleanupState.StorageIntakeRefs.SetEquals([Id1, Id2, Id3]).ShouldBeTrue();
            operations.ActiveSubscriptionCleanupState.ControlTenantAbsenceRefs.SetEquals([Id1, Id2, Id3]).ShouldBeTrue();
            return;
        }

        await SaveSentinelsAsync(store, operations.ActiveSubscriptionCleanupState, identityBranch: false);
        handler.AddMailboxIntake(RecoveryValidationTopology.StorageTenantRef, Id1);
        responses.Enqueue(Process(Id1, Id1));
        responses.Enqueue(Process(Id2, Id2));
        _ = await operations.ReconcileSubscriptionAsync(
            RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken);
        responses.Enqueue(Process(Id3, Id3, submitted: false, candidateObservedAtUtc: "2026-08-01T00:00:00Z"));
        _ = await operations.RejectControlledLossCandidateAsync(
            RecoveryValidationTopology.LogicalTenantRef, TestContext.Current.CancellationToken);
        operations.ActiveSubscriptionCleanupState.ReconciledIntakeRef.ShouldBe(Id1);
        operations.ActiveSubscriptionCleanupState.DuplicateProbeIntakeRef.ShouldBe(Id2);
        operations.ActiveSubscriptionCleanupState.ControlledRejectedCandidateRef.ShouldBe(Id3);
        operations.ActiveSubscriptionCleanupState.StorageIntakeRefs.SetEquals([Id1, Id2, Id3]).ShouldBeTrue();
        operations.ActiveSubscriptionCleanupState.StorageDurableAbsenceRefs.SetEquals([Id2, Id3]).ShouldBeTrue();
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("incomplete")]
    public async Task SubscriptionCleanupErasesIndependentTargetsAndChecksBothDurableTenants(string scenario)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        int restores = 0;
        RecoverySandboxOperationsTestSeam seam = SandboxSeam(
            response: (_, action, _, _, _, _) =>
            {
                if (string.Equals(action, "restore", StringComparison.Ordinal))
                {
                    restores++;
                }

                return ValueTask.FromResult(Json(RestoreBody()));
            });
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireRecoverySandboxOperations operations = SandboxOperations(seam, store, durable);
        List<(string Tenant, string Intake)> oldTargets = await SeedSubscriptionCleanupAsync(store, operations.ActiveSubscriptionCleanupState);
        string recreatedKey = IntakeKeys(RecoveryValidationTopology.StorageTenantRef, Id1)[0];
        string successor = Id8;
        if (string.Equals(scenario, "incomplete", StringComparison.Ordinal))
        {
            handler.AddMailboxIntake(RecoveryValidationTopology.StorageTenantRef, Id6);
            handler.AddMailboxIntake(RecoveryValidationTopology.ControlTenantRef, Id7);
            store.OnEraseKey = key =>
            {
                if (!string.Equals(key, recreatedKey, StringComparison.Ordinal))
                {
                    return;
                }

                store.SaveAsync(ChatBotReadModelStoreNames.StateStoreName, recreatedKey, Sentinel(RecoveryValidationTopology.StorageTenantRef, Id1))
                    .GetAwaiter().GetResult();
                operations.ActiveSubscriptionCleanupState.StorageIntakeRefs.Add(successor);
                foreach (string successorKey in IntakeKeys(RecoveryValidationTopology.StorageTenantRef, successor))
                {
                    store.SaveAsync(ChatBotReadModelStoreNames.StateStoreName, successorKey, Sentinel(RecoveryValidationTopology.StorageTenantRef, successor))
                        .GetAwaiter().GetResult();
                }
            };
        }

        bool complete = await operations.CleanupSubscriptionScenarioAsync(
            RecoveryValidationTopology.LogicalTenantRef, TestContext.Current.CancellationToken);

        complete.ShouldBe(string.Equals(scenario, "complete", StringComparison.Ordinal));
        restores.ShouldBe(1);
        foreach (string key in oldTargets.SelectMany(static target => IntakeKeys(target.Tenant, target.Intake)).Distinct(StringComparer.Ordinal))
        {
            if (string.Equals(scenario, "incomplete", StringComparison.Ordinal) && string.Equals(key, recreatedKey, StringComparison.Ordinal))
            {
                await AssertPresentAsync(store, key);
            }
            else
            {
                await AssertAbsentAsync(store, key);
            }
        }

        HashSet<(string TenantRef, string AggregateRef)> expectedDurable =
        [
            (RecoveryValidationTopology.StorageTenantRef, Id6),
            (RecoveryValidationTopology.ControlTenantRef, Id7),
        ];
        handler.Requests.ToHashSet().SetEquals(expectedDurable).ShouldBeTrue();
        if (string.Equals(scenario, "incomplete", StringComparison.Ordinal))
        {
            operations.ActiveSubscriptionCleanupState.StorageIntakeRefs.ShouldBe([successor]);
            foreach (string key in IntakeKeys(RecoveryValidationTopology.StorageTenantRef, successor))
            {
                await AssertPresentAsync(store, key);
            }
        }
        else
        {
            AssertSubscriptionEmpty(operations.ActiveSubscriptionCleanupState);
        }
    }

    [Fact]
    public async Task SubscriptionCleanupRestoresAfterEraseFailureAndRetiresOnAllExceptions()
    {
        await AssertSubscriptionExceptionalCleanupAsync("erase");
        await AssertSubscriptionExceptionalCleanupAsync("cancel");
        await AssertSubscriptionExceptionalCleanupAsync("restore");
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("awaited")]
    [InlineData("complete")]
    public async Task ScopedGraphProducerRetainsEveryValidIdentityBeforeLaterFailure(string scenario)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        Queue<string> responses = new();
        ScopedOutageOperationsTestSeam seam = ScopedSeam(responses);
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireScopedOutageOperations operations = ScopedOperations(seam, store, durable);

        if (string.Equals(scenario, "validation", StringComparison.Ordinal))
        {
            responses.Enqueue(Process(Id1, Id1, submitted: false));
            await Should.ThrowAsync<InvalidOperationException>(() => operations.VerifyRecoveryAsync(
                ScopedOutageDependencies.Graph, RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken).AsTask());
            responses.Enqueue(Process(Id2, Id3));
            await Should.ThrowAsync<InvalidOperationException>(() => operations.VerifyRecoveryAsync(
                ScopedOutageDependencies.Graph, RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken).AsTask());
            int durableReadsBeforeMalformed = handler.Requests.Count;
            responses.Enqueue(Process(Id4, "\t"));
            await Should.ThrowAsync<InvalidOperationException>(() => operations.VerifyRecoveryAsync(
                ScopedOutageDependencies.Graph, RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken).AsTask());
            handler.Requests.Count.ShouldBe(durableReadsBeforeMalformed);
            operations.ActiveCleanupState.GraphIntakeRefs.SetEquals([Id1, Id2, Id3, Id4]).ShouldBeTrue();
            return;
        }

        await SaveScopedGraphSentinelsAsync(store, operations.ActiveCleanupState);
        operations.ActiveCleanupState.GraphFaultLeftStateUnchanged = true;
        if (string.Equals(scenario, "awaited", StringComparison.Ordinal))
        {
            responses.Enqueue(Process(Id1, Id1));
            await Should.ThrowAsync<TimeoutException>(() => operations.VerifyRecoveryAsync(
                ScopedOutageDependencies.Graph, RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken).AsTask());
            operations.ActiveCleanupState.GraphIntakeRefs.ShouldContain(Id1);
            return;
        }

        handler.AddMailboxIntake(RecoveryValidationTopology.StorageTenantRef, Id1);
        responses.Enqueue(Process(Id1, Id1));
        responses.Enqueue(Process(Id2, Id2));
        _ = await operations.VerifyRecoveryAsync(
            ScopedOutageDependencies.Graph, RecoveryValidationTopology.LogicalTenantRef, Id8, TestContext.Current.CancellationToken);
        operations.ActiveCleanupState.GraphRecoveredIntakeRef.ShouldBe(Id1);
        operations.ActiveCleanupState.GraphDuplicateProbeIntakeRef.ShouldBe(Id2);
        operations.ActiveCleanupState.GraphDurableAbsenceRefs.ShouldBe([Id2]);
    }

    [Theory]
    [InlineData(ScopedOutageDependencies.Graph)]
    [InlineData(ScopedOutageDependencies.Identity)]
    public async Task ScopedCleanupExecutesGraphAndIdentityWithIsolatedSuccessors(string dependency)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        int restores = 0;
        int tokenChecks = 0;
        ScopedOutageOperationsTestSeam seam = new()
        {
            SendSubscriptionAsync = (_, action, _, _, _) => ValueTask.FromResult(Json(
                string.Equals(action, "status", StringComparison.Ordinal) ? "{\"faulted\":false}" : RestoreBody())),
            RestoreAsync = (_, _, _) =>
            {
                restores++;
                return ValueTask.FromResult(true);
            },
            TryAcquireRecoveryTokenOnceAsync = _ =>
            {
                tokenChecks++;
                return ValueTask.FromResult(tokenChecks > 1);
            },
            IsIdentityAvailableAsync = _ => ValueTask.FromResult(true),
            AcquireControlAccessTokenAsync = _ => ValueTask.FromResult("control"),
        };
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireScopedOutageOperations operations = ScopedOperations(seam, store, durable);
        List<string> oldKeys = await SeedScopedCleanupAsync(store, operations.ActiveCleanupState, dependency);

        bool complete = await operations.CleanupAsync(
            dependency, RecoveryValidationTopology.LogicalTenantRef, TestContext.Current.CancellationToken);

        complete.ShouldBeTrue();
        restores.ShouldBe(1);
        foreach (string key in oldKeys)
        {
            await AssertAbsentAsync(store, key);
        }

        AssertScopedEmpty(operations.ActiveCleanupState);

        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            handler.Requests.ToHashSet().SetEquals([
                (RecoveryValidationTopology.StorageTenantRef, Id6),
            ]).ShouldBeTrue();

            InMemoryRecoveryReadModelStore incompleteStore = new();
            RecoveryDurableStateMessageHandler incompleteHandler = new();
            incompleteHandler.AddMailboxIntake(RecoveryValidationTopology.StorageTenantRef, Id6);
            using EventStoreDurableStateProbe incompleteDurable = Durable(incompleteHandler);
            AspireScopedOutageOperations incomplete = ScopedOperations(seam, incompleteStore, incompleteDurable);
            List<string> incompleteOldKeys = await SeedScopedCleanupAsync(
                incompleteStore, incomplete.ActiveCleanupState, ScopedOutageDependencies.Graph);
            string recreated = incompleteOldKeys[0];
            incompleteStore.OnEraseKey = key =>
            {
                if (!string.Equals(key, recreated, StringComparison.Ordinal))
                {
                    return;
                }

                incompleteStore.SaveAsync(
                    ChatBotReadModelStoreNames.StateStoreName,
                    recreated,
                    Sentinel(RecoveryValidationTopology.StorageTenantRef, Id1)).GetAwaiter().GetResult();
                incomplete.ActiveCleanupState.GraphIntakeRefs.Add(Id8);
                foreach (string successorKey in IntakeKeys(RecoveryValidationTopology.StorageTenantRef, Id8))
                {
                    incompleteStore.SaveAsync(
                        ChatBotReadModelStoreNames.StateStoreName,
                        successorKey,
                        Sentinel(RecoveryValidationTopology.StorageTenantRef, Id8)).GetAwaiter().GetResult();
                }
            };

            bool incompleteResult = await incomplete.CleanupAsync(
                ScopedOutageDependencies.Graph,
                RecoveryValidationTopology.LogicalTenantRef,
                TestContext.Current.CancellationToken);
            incompleteResult.ShouldBeFalse();
            incompleteHandler.Requests.ToHashSet().SetEquals([
                (RecoveryValidationTopology.StorageTenantRef, Id6),
            ]).ShouldBeTrue();
            foreach (string key in incompleteOldKeys)
            {
                if (string.Equals(key, recreated, StringComparison.Ordinal))
                {
                    await AssertPresentAsync(incompleteStore, key);
                }
                else
                {
                    await AssertAbsentAsync(incompleteStore, key);
                }
            }

            incomplete.ActiveCleanupState.GraphIntakeRefs.ShouldBe([Id8]);
            foreach (string successorKey in IntakeKeys(RecoveryValidationTopology.StorageTenantRef, Id8))
            {
                await AssertPresentAsync(incompleteStore, successorKey);
            }
        }
        else
        {
            InMemoryRecoveryReadModelStore incompleteStore = new();
            RecoveryDurableStateMessageHandler incompleteHandler = new();
            using EventStoreDurableStateProbe incompleteDurable = Durable(incompleteHandler);
            int identityTokenChecks = 0;
            ScopedOutageOperationsTestSeam incompleteSeam = new()
            {
                RestoreAsync = (_, _, _) => ValueTask.FromResult(true),
                TryAcquireRecoveryTokenOnceAsync = _ => ValueTask.FromResult(++identityTokenChecks > 1),
                IsIdentityAvailableAsync = _ => ValueTask.FromResult(true),
                AcquireControlAccessTokenAsync = _ => ValueTask.FromResult("control"),
            };
            AspireScopedOutageOperations incomplete = ScopedOperations(incompleteSeam, incompleteStore, incompleteDurable);
            List<string> incompleteOldKeys = await SeedScopedCleanupAsync(
                incompleteStore, incomplete.ActiveCleanupState, ScopedOutageDependencies.Identity);
            string recreated = incompleteOldKeys[0];
            incompleteStore.OnEraseKey = key =>
            {
                if (!string.Equals(key, recreated, StringComparison.Ordinal))
                {
                    return;
                }

                incompleteStore.SaveAsync(
                    ChatBotReadModelStoreNames.StateStoreName,
                    recreated,
                    Sentinel(RecoveryValidationTopology.StorageTenantRef, Id4)).GetAwaiter().GetResult();
                incomplete.ActiveCleanupState.IdentityAffectedSentinel =
                    Sentinel(RecoveryValidationTopology.StorageTenantRef, Id8);
                foreach (string successorKey in IntakeKeys(RecoveryValidationTopology.StorageTenantRef, Id8))
                {
                    incompleteStore.SaveAsync(
                        ChatBotReadModelStoreNames.StateStoreName,
                        successorKey,
                        Sentinel(RecoveryValidationTopology.StorageTenantRef, Id8)).GetAwaiter().GetResult();
                }
            };

            bool incompleteResult = await incomplete.CleanupAsync(
                ScopedOutageDependencies.Identity,
                RecoveryValidationTopology.LogicalTenantRef,
                TestContext.Current.CancellationToken);
            incompleteResult.ShouldBeFalse();
            foreach (string key in incompleteOldKeys)
            {
                if (string.Equals(key, recreated, StringComparison.Ordinal))
                {
                    await AssertPresentAsync(incompleteStore, key);
                }
                else
                {
                    await AssertAbsentAsync(incompleteStore, key);
                }
            }

            incomplete.ActiveCleanupState.IdentityAffectedSentinel!.IntakeId.ShouldBe(Id8);
            foreach (string successorKey in IntakeKeys(RecoveryValidationTopology.StorageTenantRef, Id8))
            {
                await AssertPresentAsync(incompleteStore, successorKey);
            }
        }
    }

    [Fact]
    public async Task ScopedCleanupRestoresAfterFirstEraseFailureAndPropagatesCancellation()
    {
        await AssertScopedExceptionalCleanupAsync(ScopedOutageDependencies.Graph, cancel: false);
        await AssertScopedExceptionalCleanupAsync(ScopedOutageDependencies.Graph, cancel: true);
        await AssertScopedExceptionalCleanupAsync(ScopedOutageDependencies.Identity, cancel: false);
        await AssertScopedExceptionalCleanupAsync(ScopedOutageDependencies.Identity, cancel: true);
    }

    [Fact]
    public void StateHandoffAndDiagnosticAllowlistRetireAllFlagsAndIdentifiers()
    {
        SubscriptionRecoveryCleanupState subscription = new()
        {
            SubscriptionCheckpointIntakeRef = Id1,
            ReconciledIntakeRef = Id2,
            DuplicateProbeIntakeRef = Id3,
            ControlledPreFaultIntakeRef = Id4,
            ControlledRejectedCandidateRef = Id5,
            ControlledPostRecoveryIntakeRef = Id6,
            SubscriptionFaultLeftStateUnchanged = true,
            ControlledFaultLeftStateUnchanged = true,
        };
        subscription.StorageIntakeRefs.UnionWith([Id1, Id2]);
        subscription.StorageDurableAbsenceRefs.Add(Id3);
        subscription.ControlTenantAbsenceRefs.Add(Id4);
        SubscriptionRecoveryCleanupState detachedSubscription = SubscriptionRecoveryCleanupState.DetachAndReset(ref subscription);
        detachedSubscription.SubscriptionFaultLeftStateUnchanged.ShouldBeTrue();
        detachedSubscription.ControlledFaultLeftStateUnchanged.ShouldBeTrue();
        AssertSubscriptionEmpty(subscription);

        ScopedOutageRecoveryCleanupState scoped = new()
        {
            GraphRecoveredIntakeRef = Id1,
            GraphDuplicateProbeIntakeRef = Id2,
            GraphFaultLeftStateUnchanged = true,
            IdentityFaultLeftStateUnchanged = true,
        };
        scoped.GraphIntakeRefs.Add(Id1);
        scoped.GraphDurableAbsenceRefs.Add(Id2);
        scoped.ControlOperationRefs.Add(Id3);
        ScopedOutageRecoveryCleanupState detachedScoped = ScopedOutageRecoveryCleanupState.DetachAndReset(ref scoped);
        detachedScoped.GraphFaultLeftStateUnchanged.ShouldBeTrue();
        detachedScoped.IdentityFaultLeftStateUnchanged.ShouldBeTrue();
        AssertScopedEmpty(scoped);

        AspireRecoverySandboxOperations.FormatIncompleteCleanupDiagnostic([
            "checkpoint-note-not-present",
            "checkpoint-note-not-present",
            "erased-projection-still-present",
        ]).ShouldBe("RECOVERY_CLEANUP_INCOMPLETE checks=checkpoint-note-not-present,erased-projection-still-present");
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            AspireRecoverySandboxOperations.FormatIncompleteCleanupDiagnostic([Id1]));
        exception.Message.ShouldBe("Continuity cleanup received an unsupported diagnostic code.");
        exception.Message.ShouldNotContain(Id1);
    }

    private static RecoverySandboxOperationsTestSeam SandboxSeam(
        Func<string, string, string, string, string, CancellationToken, Task>? submit = null,
        Func<string, CancellationToken, ValueTask>? diagnostic = null,
        Func<CancellationToken, ValueTask<bool>>? availability = null,
        Queue<string>? responses = null,
        Func<string, string, bool, CancellationToken, string?, string, ValueTask<JsonDocument>>? response = null)
        => new()
        {
            AcquireUserAccessTokenAsync = _ => ValueTask.FromResult("user"),
            AcquireControlAccessTokenAsync = _ => ValueTask.FromResult("control"),
            SubmitUntilAcceptedAsync = submit ?? ((_, _, _, _, _, _) => Task.CompletedTask),
            IsEventStoreEndpointAvailableAsync = availability ?? (cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(true);
            }),
            WriteDiagnosticAsync = diagnostic ?? ((_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.CompletedTask;
            }),
            SendSandboxControlAsync = response ?? ((_, _, _, cancellationToken, _, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(Json(responses?.Dequeue() ?? RestoreBody()));
            }),
        };

    private static ScopedOutageOperationsTestSeam ScopedSeam(Queue<string> responses)
        => new()
        {
            SendSubscriptionAsync = (_, _, _, cancellationToken, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(Json(responses.Dequeue()));
            },
            RestoreAsync = (_, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(true);
            },
            TryAcquireRecoveryTokenOnceAsync = _ => ValueTask.FromResult(true),
            IsIdentityAvailableAsync = _ => ValueTask.FromResult(true),
            AcquireControlAccessTokenAsync = _ => ValueTask.FromResult("control"),
        };

    private static AspireRecoverySandboxOperations SandboxOperations(
        RecoverySandboxOperationsTestSeam seam,
        InMemoryRecoveryReadModelStore store,
        EventStoreDurableStateProbe durable)
        => new(seam, store, store, durable, Poll, Window, Window);

    private static AspireScopedOutageOperations ScopedOperations(
        ScopedOutageOperationsTestSeam seam,
        InMemoryRecoveryReadModelStore store,
        EventStoreDurableStateProbe durable)
        => new(seam, "control", store, store, durable, Poll, Window);

    private static EventStoreDurableStateProbe Durable(RecoveryDurableStateMessageHandler handler)
        => new(new Uri("http://eventstore.test"), handler, Window, Poll);

    private static async Task AssertSubscriptionExceptionalCleanupAsync(string scenario)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        int restores = 0;
        RecoverySandboxOperationsTestSeam seam = SandboxSeam(response: (_, action, _, _, _, _) =>
        {
            restores++;
            if (string.Equals(scenario, "restore", StringComparison.Ordinal) && string.Equals(action, "restore", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("injected restore failure");
            }

            return ValueTask.FromResult(Json(RestoreBody()));
        });
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireRecoverySandboxOperations operations = SandboxOperations(seam, store, durable);
        List<(string Tenant, string Intake)> targets = await SeedSubscriptionCleanupAsync(store, operations.ActiveSubscriptionCleanupState)
            .ConfigureAwait(false);
        string firstKey = IntakeKeys(targets[0].Tenant, targets[0].Intake)[0];
        using CancellationTokenSource cancellation = new();
        if (string.Equals(scenario, "erase", StringComparison.Ordinal))
        {
            store.FailOnEraseKey = firstKey;
        }
        else if (string.Equals(scenario, "cancel", StringComparison.Ordinal))
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }

        if (string.Equals(scenario, "cancel", StringComparison.Ordinal))
        {
            await Should.ThrowAsync<OperationCanceledException>(() => operations.CleanupSubscriptionScenarioAsync(
                RecoveryValidationTopology.LogicalTenantRef, cancellation.Token).AsTask()).ConfigureAwait(false);
        }
        else
        {
            await Should.ThrowAsync<InvalidOperationException>(() => operations.CleanupSubscriptionScenarioAsync(
                RecoveryValidationTopology.LogicalTenantRef, cancellation.Token).AsTask()).ConfigureAwait(false);
        }

        restores.ShouldBe(1);
        AssertSubscriptionEmpty(operations.ActiveSubscriptionCleanupState);
    }

    private static async Task AssertScopedExceptionalCleanupAsync(string dependency, bool cancel)
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryDurableStateMessageHandler handler = new();
        int restores = 0;
        ScopedOutageOperationsTestSeam seam = new()
        {
            SendSubscriptionAsync = (_, _, _, _, _) => ValueTask.FromResult(Json("{\"faulted\":false}")),
            RestoreAsync = (_, _, _) =>
            {
                restores++;
                return ValueTask.FromResult(true);
            },
            TryAcquireRecoveryTokenOnceAsync = _ => ValueTask.FromResult(false),
            IsIdentityAvailableAsync = _ => ValueTask.FromResult(true),
            AcquireControlAccessTokenAsync = _ => ValueTask.FromResult("control"),
        };
        using EventStoreDurableStateProbe durable = Durable(handler);
        AspireScopedOutageOperations operations = ScopedOperations(seam, store, durable);
        List<string> oldKeys = await SeedScopedCleanupAsync(store, operations.ActiveCleanupState, dependency)
            .ConfigureAwait(false);
        using CancellationTokenSource cancellation = new();
        if (cancel)
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }
        else
        {
            store.FailOnEraseKey = oldKeys[0];
        }

        if (cancel)
        {
            await Should.ThrowAsync<OperationCanceledException>(() => operations.CleanupAsync(
                dependency, RecoveryValidationTopology.LogicalTenantRef, cancellation.Token).AsTask()).ConfigureAwait(false);
        }
        else
        {
            await Should.ThrowAsync<InvalidOperationException>(() => operations.CleanupAsync(
                dependency, RecoveryValidationTopology.LogicalTenantRef, cancellation.Token).AsTask()).ConfigureAwait(false);
        }

        restores.ShouldBe(1);
        AssertScopedEmpty(operations.ActiveCleanupState);
    }

    private static async Task<List<(string Tenant, string Intake)>> SeedSubscriptionCleanupAsync(
        InMemoryRecoveryReadModelStore store,
        SubscriptionRecoveryCleanupState state)
    {
        state.AffectedTenantSentinel = Sentinel(RecoveryValidationTopology.StorageTenantRef, Id1);
        state.ControlTenantSentinel = Sentinel(RecoveryValidationTopology.ControlTenantRef, Id2);
        state.SubscriptionCheckpointIntakeRef = Id3;
        state.ReconciledIntakeRef = Id4;
        state.DuplicateProbeIntakeRef = Id6;
        state.ControlledPreFaultIntakeRef = Id3;
        state.ControlledRejectedCandidateRef = Id6;
        state.ControlledPostRecoveryIntakeRef = Id4;
        state.SubscriptionFaultLeftStateUnchanged = true;
        state.ControlledFaultLeftStateUnchanged = true;
        state.StorageIntakeRefs.UnionWith([Id3, Id4, Id6]);
        state.StorageDurableAbsenceRefs.Add(Id6);
        state.ControlTenantAbsenceRefs.Add(Id7);
        List<(string Tenant, string Intake)> targets =
        [
            (RecoveryValidationTopology.StorageTenantRef, Id1),
            (RecoveryValidationTopology.ControlTenantRef, Id2),
            (RecoveryValidationTopology.StorageTenantRef, Id3),
            (RecoveryValidationTopology.StorageTenantRef, Id4),
            (RecoveryValidationTopology.StorageTenantRef, Id6),
            (RecoveryValidationTopology.ControlTenantRef, Id7),
        ];
        foreach ((string tenant, string intake) in targets)
        {
            await SaveIntakeKeysAsync(store, tenant, intake).ConfigureAwait(false);
        }

        return targets;
    }

    private static async Task<List<string>> SeedScopedCleanupAsync(
        InMemoryRecoveryReadModelStore store,
        ScopedOutageRecoveryCleanupState state,
        string dependency)
    {
        List<(string Tenant, string Intake)> targets = [];
        if (string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal))
        {
            state.GraphAffectedSentinel = Sentinel(RecoveryValidationTopology.StorageTenantRef, Id1);
            state.GraphControlSentinel = Sentinel(RecoveryValidationTopology.ControlTenantRef, Id2);
            state.GraphRecoveredIntakeRef = Id3;
            state.GraphDuplicateProbeIntakeRef = Id6;
            state.GraphFaultLeftStateUnchanged = true;
            state.GraphIntakeRefs.UnionWith([Id3, Id6]);
            state.GraphDurableAbsenceRefs.Add(Id6);
            targets.Add((RecoveryValidationTopology.StorageTenantRef, Id1));
            targets.Add((RecoveryValidationTopology.ControlTenantRef, Id2));
            targets.Add((RecoveryValidationTopology.StorageTenantRef, Id3));
            targets.Add((RecoveryValidationTopology.StorageTenantRef, Id6));
        }
        else
        {
            state.IdentityAffectedSentinel = Sentinel(RecoveryValidationTopology.StorageTenantRef, Id4);
            state.IdentityControlSentinel = Sentinel(RecoveryValidationTopology.ControlTenantRef, Id5);
            state.IdentityFaultLeftStateUnchanged = true;
            targets.Add((RecoveryValidationTopology.StorageTenantRef, Id4));
            targets.Add((RecoveryValidationTopology.ControlTenantRef, Id5));
        }

        state.ControlOperationRefs.Add(Id7);
        List<string> keys = [.. targets.SelectMany(static target => IntakeKeys(target.Tenant, target.Intake))];
        keys.Add(GovernedKey(RecoveryValidationTopology.ControlTenantRef, Id7));
        foreach ((string tenant, string intake) in targets)
        {
            await SaveIntakeKeysAsync(store, tenant, intake).ConfigureAwait(false);
        }

        await store.SaveAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            GovernedKey(RecoveryValidationTopology.ControlTenantRef, Id7),
            Governed(RecoveryValidationTopology.ControlTenantRef, Id7)).ConfigureAwait(false);
        return keys;
    }

    private static async Task SaveSentinelsAsync(
        InMemoryRecoveryReadModelStore store,
        SubscriptionRecoveryCleanupState state,
        bool identityBranch)
    {
        _ = identityBranch;
        state.AffectedTenantSentinel = Sentinel(RecoveryValidationTopology.StorageTenantRef, Id7);
        state.ControlTenantSentinel = Sentinel(RecoveryValidationTopology.ControlTenantRef, Id8);
        await store.SaveAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            IntakeKeys(state.AffectedTenantSentinel.TenantId, state.AffectedTenantSentinel.IntakeId)[0],
            state.AffectedTenantSentinel).ConfigureAwait(false);
        await store.SaveAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            IntakeKeys(state.ControlTenantSentinel.TenantId, state.ControlTenantSentinel.IntakeId)[0],
            state.ControlTenantSentinel).ConfigureAwait(false);
    }

    private static async Task SaveScopedGraphSentinelsAsync(
        InMemoryRecoveryReadModelStore store,
        ScopedOutageRecoveryCleanupState state)
    {
        state.GraphAffectedSentinel = Sentinel(RecoveryValidationTopology.StorageTenantRef, Id7);
        state.GraphControlSentinel = Sentinel(RecoveryValidationTopology.ControlTenantRef, Id8);
        await store.SaveAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            IntakeKeys(state.GraphAffectedSentinel.TenantId, state.GraphAffectedSentinel.IntakeId)[0],
            state.GraphAffectedSentinel).ConfigureAwait(false);
        await store.SaveAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            IntakeKeys(state.GraphControlSentinel.TenantId, state.GraphControlSentinel.IntakeId)[0],
            state.GraphControlSentinel).ConfigureAwait(false);
    }

    private static async Task SaveIntakeKeysAsync(InMemoryRecoveryReadModelStore store, string tenant, string intake)
    {
        foreach (string key in IntakeKeys(tenant, intake))
        {
            await store.SaveAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                key,
                Sentinel(tenant, intake)).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<string> IntakeKeys(string tenant, string intake)
        =>
        [
            $"{tenant}:project-conversation-source-email:{intake}",
            $"{tenant}:project-conversation-attachments:{intake}",
            $"{tenant}:project-conversation:{intake}:attachments",
        ];

    private static string GovernedKey(string tenant, string note)
        => $"{tenant}:governed-operation:{note}";

    private static GovernedOperationView Governed(string tenant, string note)
        => new(
            tenant,
            note,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            GovernedOperationView.MetadataOnlyRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            1,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

    private static ProjectConversationSourceEmailView Sentinel(string tenant, string intake)
        => new(
            tenant,
            intake,
            "recovery-sentinel-mailbox",
            "recovery-sentinel-message",
            InternetMessageId: null,
            "recovery-sentinel-conversation",
            SourceThreadId: null,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            SourceSentAtUtc: null,
            SourceCreatedAtUtc: null,
            "UTC",
            "Microsoft 365 mailbox",
            "m365-mailbox",
            "metadata-only",
            "standard",
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            1,
            Id1);

    private static string Process(
        string? intake,
        string? candidate,
        bool submitted = true,
        string candidateObservedAtUtc = "2026-08-01T00:00:00Z")
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["submitted"] = submitted,
            ["kind"] = submitted ? "accepted" : "rejected",
            ["reasonCode"] = submitted ? "none" : "chatbot_submission_recoverable",
            ["intakeId"] = intake,
            ["candidateRef"] = candidate,
            ["observedAtUtc"] = "2026-08-01T00:00:00Z",
            ["candidateObservedAtUtc"] = candidateObservedAtUtc,
        });

    private static string RestoreBody()
        => "{\"prior\":{\"faulted\":false},\"current\":{\"faulted\":false}}";

    private static JsonDocument Json(string json) => JsonDocument.Parse(json);

    private static void SeedEventStoreState(EventStoreRecoveryCleanupState state)
    {
        state.CheckpointCorrelationId = Id8;
        state.CheckpointNoteRefs.Add(Id1);
    }

    private static void AssertEventStoreEmpty(EventStoreRecoveryCleanupState state)
    {
        state.HasOwnedState.ShouldBeFalse();
        state.CheckpointNoteRefs.ShouldBeEmpty();
        state.CheckpointCommittedAtUtc.ShouldBeEmpty();
        state.CheckpointCorrelationId.ShouldBeNull();
        state.ControlTenantNoteRef.ShouldBeNull();
        state.FaultProbeNoteRef.ShouldBeNull();
    }

    private static void AssertSubscriptionEmpty(SubscriptionRecoveryCleanupState state)
    {
        state.HasOwnedState.ShouldBeFalse();
        state.AffectedTenantSentinel.ShouldBeNull();
        state.ControlTenantSentinel.ShouldBeNull();
        state.SubscriptionCheckpointIntakeRef.ShouldBeNull();
        state.ReconciledIntakeRef.ShouldBeNull();
        state.DuplicateProbeIntakeRef.ShouldBeNull();
        state.ControlledPreFaultIntakeRef.ShouldBeNull();
        state.ControlledRejectedCandidateRef.ShouldBeNull();
        state.ControlledPostRecoveryIntakeRef.ShouldBeNull();
        state.SubscriptionFaultLeftStateUnchanged.ShouldBeFalse();
        state.ControlledFaultLeftStateUnchanged.ShouldBeFalse();
        state.StorageIntakeRefs.ShouldBeEmpty();
        state.StorageDurableAbsenceRefs.ShouldBeEmpty();
        state.ControlTenantAbsenceRefs.ShouldBeEmpty();
    }

    private static void AssertScopedEmpty(ScopedOutageRecoveryCleanupState state)
    {
        state.HasOwnedState.ShouldBeFalse();
        state.GraphAffectedSentinel.ShouldBeNull();
        state.GraphControlSentinel.ShouldBeNull();
        state.GraphRecoveredIntakeRef.ShouldBeNull();
        state.GraphDuplicateProbeIntakeRef.ShouldBeNull();
        state.GraphFaultLeftStateUnchanged.ShouldBeFalse();
        state.IdentityAffectedSentinel.ShouldBeNull();
        state.IdentityControlSentinel.ShouldBeNull();
        state.IdentityFaultLeftStateUnchanged.ShouldBeFalse();
        state.GraphIntakeRefs.ShouldBeEmpty();
        state.GraphDurableAbsenceRefs.ShouldBeEmpty();
        state.ControlOperationRefs.ShouldBeEmpty();
    }

    private static async Task AssertAbsentAsync(InMemoryRecoveryReadModelStore store, string key)
    {
        (bool present, _) = await store.TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key)
            .ConfigureAwait(false);
        present.ShouldBeFalse($"Expected old-generation key '{key}' to be absent.");
    }

    private static async Task AssertPresentAsync(InMemoryRecoveryReadModelStore store, string key)
    {
        (bool present, _) = await store.TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key)
            .ConfigureAwait(false);
        present.ShouldBeTrue($"Expected named recreated/successor key '{key}' to remain present.");
    }

    private static async Task EraseIfPresentAsync(InMemoryRecoveryReadModelStore store, string key)
    {
        (bool present, string etag) = await store.TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key)
            .ConfigureAwait(false);
        if (present)
        {
            _ = await store.TryEraseAsync(ChatBotReadModelStoreNames.StateStoreName, key, etag).ConfigureAwait(false);
        }
    }
}
