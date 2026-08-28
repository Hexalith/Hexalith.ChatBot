using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Always-run regressions for recovery cleanup generation handoff and metadata-only diagnostics.</summary>
public sealed class AspireRecoveryCleanupStateTests
{
    private const string NextNoteRef = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
    private const string TrackedNoteRef = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public void CompleteCleanupHandoffUsesOneDetachedSnapshotAndLeavesActiveGenerationEmpty()
    {
        EventStoreRecoveryCleanupState activeState = new()
        {
            CheckpointCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            ControlTenantNoteRef = NextNoteRef,
        };
        activeState.CheckpointNoteRefs.Add(TrackedNoteRef);
        EventStoreRecoveryCleanupState expectedSnapshot = activeState;

        EventStoreRecoveryCleanupState detachedState = EventStoreRecoveryCleanupState.DetachAndReset(
            ref activeState);

        ReferenceEquals(detachedState, expectedSnapshot).ShouldBeTrue();
        detachedState.CheckpointNoteRefs.ShouldBe([TrackedNoteRef]);
        detachedState.ControlTenantNoteRef.ShouldBe(NextNoteRef);
        activeState.HasOwnedState.ShouldBeFalse();
        activeState.CheckpointCorrelationId.ShouldBeNull();
    }

    [Fact]
    public async Task PartialCleanupFailureLeavesNextEventStoreGenerationIsolated()
    {
        EventStoreRecoveryCleanupState activeState = new()
        {
            CheckpointCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
        };
        activeState.CheckpointNoteRefs.Add(TrackedNoteRef);
        EventStoreRecoveryCleanupState detachedState = EventStoreRecoveryCleanupState.DetachAndReset(
            ref activeState);
        InMemoryRecoveryReadModelStore store = new() { FailOnReadNumber = 1 };

        _ = await Should.ThrowAsync<InvalidOperationException>(() => store.TryReadEtagAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            GovernedOperationView.KeyFor(
                RecoveryValidationTopology.StorageTenantRef,
                detachedState.CheckpointNoteRefs.Single()),
            TestContext.Current.CancellationToken));

        activeState.HasOwnedState.ShouldBeFalse();
        activeState.CheckpointNoteRefs.Add(NextNoteRef);
        detachedState.CheckpointNoteRefs.ShouldBe([TrackedNoteRef]);
        activeState.CheckpointNoteRefs.ShouldBe([NextNoteRef]);
    }

    [Fact]
    public async Task UnconfirmedPreCommitCheckpointRemainsOwnedUntilDetachedCleanupHandlesItsAbsence()
    {
        EventStoreRecoveryCleanupState activeState = new()
        {
            CheckpointCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
        };
        activeState.CheckpointNoteRefs.Add(TrackedNoteRef);

        EventStoreRecoveryCleanupState detachedState = EventStoreRecoveryCleanupState.DetachAndReset(
            ref activeState);
        InMemoryRecoveryReadModelStore store = new();
        (bool present, _) = await store.TryReadEtagAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            GovernedOperationView.KeyFor(
                RecoveryValidationTopology.StorageTenantRef,
                detachedState.CheckpointNoteRefs.Single()),
            TestContext.Current.CancellationToken);

        detachedState.HasOwnedState.ShouldBeTrue();
        detachedState.CheckpointCommittedAtUtc.ShouldBeEmpty();
        present.ShouldBeFalse();
        activeState.HasOwnedState.ShouldBeFalse();
    }

    [Fact]
    public void SubscriptionAndScopedObservationFlagsResetWithTheirReferences()
    {
        SubscriptionRecoveryCleanupState activeSubscription = new()
        {
            SubscriptionCheckpointIntakeRef = TrackedNoteRef,
            ControlledRejectedCandidateRef = NextNoteRef,
            SubscriptionFaultLeftStateUnchanged = true,
            ControlledFaultLeftStateUnchanged = true,
        };
        ScopedOutageRecoveryCleanupState activeScopedOutage = new()
        {
            GraphDuplicateProbeIntakeRef = TrackedNoteRef,
            GraphFaultLeftStateUnchanged = true,
            IdentityFaultLeftStateUnchanged = true,
        };
        activeScopedOutage.ControlOperationRefs.Add(NextNoteRef);

        SubscriptionRecoveryCleanupState detachedSubscription =
            SubscriptionRecoveryCleanupState.DetachAndReset(ref activeSubscription);
        ScopedOutageRecoveryCleanupState detachedScopedOutage =
            ScopedOutageRecoveryCleanupState.DetachAndReset(ref activeScopedOutage);

        detachedSubscription.SubscriptionFaultLeftStateUnchanged.ShouldBeTrue();
        detachedSubscription.ControlledFaultLeftStateUnchanged.ShouldBeTrue();
        detachedSubscription.SubscriptionCheckpointIntakeRef.ShouldBe(TrackedNoteRef);
        detachedSubscription.ControlledRejectedCandidateRef.ShouldBe(NextNoteRef);
        activeSubscription.SubscriptionFaultLeftStateUnchanged.ShouldBeFalse();
        activeSubscription.ControlledFaultLeftStateUnchanged.ShouldBeFalse();
        activeSubscription.SubscriptionCheckpointIntakeRef.ShouldBeNull();
        activeSubscription.ControlledRejectedCandidateRef.ShouldBeNull();

        detachedScopedOutage.GraphFaultLeftStateUnchanged.ShouldBeTrue();
        detachedScopedOutage.IdentityFaultLeftStateUnchanged.ShouldBeTrue();
        detachedScopedOutage.GraphDuplicateProbeIntakeRef.ShouldBe(TrackedNoteRef);
        detachedScopedOutage.ControlOperationRefs.ShouldBe([NextNoteRef]);
        activeScopedOutage.GraphFaultLeftStateUnchanged.ShouldBeFalse();
        activeScopedOutage.IdentityFaultLeftStateUnchanged.ShouldBeFalse();
        activeScopedOutage.GraphDuplicateProbeIntakeRef.ShouldBeNull();
        activeScopedOutage.ControlOperationRefs.ShouldBeEmpty();
    }

    [Fact]
    public void DetachedDiagnosticInputsStayStableWhenTheNextScenarioStarts()
    {
        EventStoreRecoveryCleanupState activeState = new()
        {
            CheckpointCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
        };
        activeState.CheckpointNoteRefs.Add(TrackedNoteRef);
        EventStoreRecoveryCleanupState detachedState = EventStoreRecoveryCleanupState.DetachAndReset(
            ref activeState);
        activeState.CheckpointCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        activeState.CheckpointNoteRefs.Add(NextNoteRef);

        string diagnostic = AspireRecoverySandboxOperations.FormatIncompleteCleanupDiagnostic(
        [
            detachedState.CheckpointNoteRefs.Count == 1
                ? "checkpoint-note-not-present"
                : "checkpoint-note-presence-faulted",
            "checkpoint-note-not-present",
        ]);

        diagnostic.ShouldBe("RECOVERY_CLEANUP_INCOMPLETE checks=checkpoint-note-not-present");
        diagnostic.ShouldNotContain(TrackedNoteRef);
        diagnostic.ShouldNotContain(NextNoteRef);
        detachedState.CheckpointNoteRefs.ShouldBe([TrackedNoteRef]);
        activeState.CheckpointNoteRefs.ShouldBe([NextNoteRef]);
    }

    [Fact]
    public void CleanupDiagnosticRejectsUnstableInputs()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            AspireRecoverySandboxOperations.FormatIncompleteCleanupDiagnostic([TrackedNoteRef]));

        exception.Message.ShouldBe("Continuity cleanup received an unsupported diagnostic code.");
        exception.Message.ShouldNotContain(TrackedNoteRef);
    }

    [Fact]
    public async Task CallerCancellationPropagatesWithoutRestoringDetachedGeneration()
    {
        EventStoreRecoveryCleanupState activeState = new()
        {
            CheckpointCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
        };
        activeState.CheckpointNoteRefs.Add(TrackedNoteRef);
        EventStoreRecoveryCleanupState detachedState = EventStoreRecoveryCleanupState.DetachAndReset(
            ref activeState);
        InMemoryRecoveryReadModelStore store = new();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => store.TryReadEtagAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            GovernedOperationView.KeyFor(
                RecoveryValidationTopology.StorageTenantRef,
                detachedState.CheckpointNoteRefs.Single()),
            cancellation.Token));

        activeState.HasOwnedState.ShouldBeFalse();
        detachedState.CheckpointNoteRefs.ShouldBe([TrackedNoteRef]);
    }
}
