using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Verifies canonical intake read-model keys and fail-closed sustained-absence behavior.</summary>
public sealed class RecoveryIntakeReadModelProbeTests
{
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TenantId = "recovery-validation";

    [Fact]
    public void KeysPreserveCanonicalShapesAndOrder()
    {
        RecoveryIntakeReadModelProbe.KeysFor(TenantId, IntakeId).ShouldBe(
        [
            $"{TenantId}:project-conversation-source-email:{IntakeId}",
            $"{TenantId}:project-conversation-attachments:{IntakeId}",
            $"{TenantId}:project-conversation:{IntakeId}:attachments",
        ]);
    }

    [Theory]
    [InlineData("", IntakeId)]
    [InlineData(TenantId, "")]
    public void KeysPreserveFactoryIdentifierValidation(string tenantId, string intakeId)
    {
        _ = Should.Throw<ArgumentException>(() => RecoveryIntakeReadModelProbe.KeysFor(tenantId, intakeId));
    }

    [Fact]
    public async Task OneShotAbsenceChecksEveryCanonicalKey()
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryIntakeReadModelProbe probe = new(store);

        bool absent = await probe.AreAbsentAsync(TenantId, IntakeId, TestContext.Current.CancellationToken);

        absent.ShouldBeTrue();
        store.Reads.ShouldBe(3);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task OneShotPresenceAtAnyCanonicalKeyReturnsFalseImmediately(int presentKeyIndex, int expectedReads)
    {
        InMemoryRecoveryReadModelStore store = new();
        IReadOnlyList<string> keys = RecoveryIntakeReadModelProbe.KeysFor(TenantId, IntakeId);
        await store.SaveAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            keys[presentKeyIndex],
            new object(),
            TestContext.Current.CancellationToken);
        RecoveryIntakeReadModelProbe probe = new(store);

        bool absent = await probe.AreAbsentAsync(TenantId, IntakeId, TestContext.Current.CancellationToken);

        absent.ShouldBeFalse();
        store.Reads.ShouldBe(expectedReads);
    }

    [Fact]
    public async Task OneShotStorageFailurePropagates()
    {
        InMemoryRecoveryReadModelStore store = new() { RejectReads = true };
        RecoveryIntakeReadModelProbe probe = new(store);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => probe.AreAbsentAsync(TenantId, IntakeId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OneShotCallerCancellationPropagates()
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryIntakeReadModelProbe probe = new(store);
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => probe.AreAbsentAsync(TenantId, IntakeId, cancellation.Token));
    }

    [Fact]
    public void DefaultConstructionUsesFiveHundredMillisecondPolling()
    {
        RecoveryIntakeReadModelProbe probe = new(new InMemoryRecoveryReadModelStore());

        probe.PollInterval.ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task SustainedAbsencePerformsFinalBoundaryRead()
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryIntakeReadModelProbe probe = new(store, TimeSpan.Zero);

        bool absent = await probe.RemainsAbsentAsync(
            TenantId,
            IntakeId,
            TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        absent.ShouldBeTrue();
        store.Reads.ShouldBe(6);
    }

    [Fact]
    public async Task SustainedAbsenceReturnsFalseForPresenceAtFinalBoundary()
    {
        InMemoryRecoveryReadModelStore store = new();
        string key = RecoveryIntakeReadModelProbe.KeysFor(TenantId, IntakeId)[0];
        RecoveryIntakeReadModelProbe probe = new(store, TimeSpan.Zero);

        // A zero-length window closes after the first three-key sweep, so read attempt 4 is the closing
        // boundary read. Injecting presence from the store's own read seam removes any wall-clock race.
        store.OnReadAttempt = attempt => SaveOnReadAttempt(store, key, attempt, 4);

        bool absent = await probe.RemainsAbsentAsync(
            TenantId,
            IntakeId,
            TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        absent.ShouldBeFalse();
        store.Reads.ShouldBe(4);
    }

    [Fact]
    public async Task SustainedAbsenceReturnsFalseForIntermediatePresenceEvenAfterLaterRemoval()
    {
        InMemoryRecoveryReadModelStore store = new();
        string key = RecoveryIntakeReadModelProbe.KeysFor(TenantId, IntakeId)[0];
        RecoveryIntakeReadModelProbe probe = new(store, TimeSpan.Zero);

        // A 200 ms window cannot close after a single in-memory sweep, so read attempt 4 is an intermediate
        // poll rather than the closing boundary read.
        store.OnReadAttempt = attempt => SaveOnReadAttempt(store, key, attempt, 4);

        bool absent = await probe.RemainsAbsentAsync(
            TenantId,
            IntakeId,
            TimeSpan.FromMilliseconds(200),
            TestContext.Current.CancellationToken);

        absent.ShouldBeFalse();
        store.Reads.ShouldBe(4);

        // The intermediate observation is the verdict: erasing the key afterwards does not retroactively
        // make the elapsed window clean, and a fresh check reports the new state instead.
        store.OnReadAttempt = null;
        (bool present, string etag) = await store.TryReadEtagAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            key,
            TestContext.Current.CancellationToken);
        present.ShouldBeTrue();
        bool erased = await store.TryEraseAsync(
            ChatBotReadModelStoreNames.StateStoreName,
            key,
            etag,
            TestContext.Current.CancellationToken);
        erased.ShouldBeTrue();
        (await probe.AreAbsentAsync(TenantId, IntakeId, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task InjectedReadFailureFailsOnlyTheTargetedAttempt()
    {
        InMemoryRecoveryReadModelStore store = new() { FailOnReadNumber = 2 };
        RecoveryIntakeReadModelProbe probe = new(store);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => probe.AreAbsentAsync(TenantId, IntakeId, TestContext.Current.CancellationToken));
        store.Reads.ShouldBe(1);

        bool absent = await probe.AreAbsentAsync(TenantId, IntakeId, TestContext.Current.CancellationToken);

        absent.ShouldBeTrue();
        store.Reads.ShouldBe(4);
    }

    [Fact]
    public async Task SustainedAbsenceClosingStorageFailurePropagates()
    {
        InMemoryRecoveryReadModelStore store = new() { FailOnReadNumber = 4 };
        RecoveryIntakeReadModelProbe probe = new(store, TimeSpan.Zero);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => probe.RemainsAbsentAsync(
            TenantId,
            IntakeId,
            TimeSpan.Zero,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SustainedAbsenceCallerCancellationPropagates()
    {
        InMemoryRecoveryReadModelStore store = new();
        RecoveryIntakeReadModelProbe probe = new(store, TimeSpan.FromMilliseconds(100));
        using CancellationTokenSource cancellation = new();
        Task<bool> observation = probe.RemainsAbsentAsync(
            TenantId,
            IntakeId,
            TimeSpan.FromSeconds(1),
            cancellation.Token);
        store.Reads.ShouldBe(3);
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => observation);
    }

    /// <summary>Persists <paramref name="key"/> from inside the store's read seam on one exact attempt.</summary>
    private static void SaveOnReadAttempt(
        InMemoryRecoveryReadModelStore store,
        string key,
        int attempt,
        int saveAtAttempt)
    {
        if (attempt != saveAtAttempt)
        {
            return;
        }

        // The in-memory store persists synchronously, so this completes before the attempt observes storage.
        store.SaveAsync(ChatBotReadModelStoreNames.StateStoreName, key, new object(), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
}
