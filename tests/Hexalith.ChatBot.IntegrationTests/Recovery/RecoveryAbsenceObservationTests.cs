using System.Diagnostics;
using System.Globalization;

using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Verifies that absence requires actual initial and final persisted-store observations.</summary>
public sealed class RecoveryAbsenceObservationTests
{
    private const string Note = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    /// <summary>Even the smallest positive window performs an initial read and closes the final-delay race.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TinyWindowObservesInitialAndFinalState(int appearanceRead)
    {
        InMemoryRecoveryReadModelStore store = new();
        int reads = 0;
        string tenant = RecoveryValidationTopology.StorageTenantRef;
        store.OnReadKey = key =>
        {
            reads++;
            if (reads == appearanceRead)
            {
                DateTimeOffset now = DateTimeOffset.Parse("2026-08-01T00:00:00Z", CultureInfo.InvariantCulture);
                store.SaveAsync(ChatBotReadModelStoreNames.StateStoreName, key, new GovernedOperationView(
                    tenant, Note, GovernedOperationView.CurrentSchemaVersion,
                    GovernedOperationView.GovernedCommandProvenance, GovernedOperationView.CurrentDerivationKernelVersion,
                    GovernedOperationView.MetadataOnlyRedactionState, GovernedOperationView.GovernedOperationalRetentionClass,
                    1, now, now)).GetAwaiter().GetResult();
            }
        };
        using EventStoreDurableStateProbe durable = Durable();
        AspireRecoverySandboxOperations operations = Operations(store, durable);

        bool absent = await operations.RemainsGovernedOperationProjectionAbsentAsync(
            tenant, Note, TestContext.Current.CancellationToken);

        absent.ShouldBe(appearanceRead == 0);
        reads.ShouldBe(appearanceRead == 1 ? 1 : 2);
    }

    /// <summary>A failed or canceled read cannot become positive absence evidence.</summary>
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task ObservationFailuresPropagate(bool cancel, int failureRead)
    {
        using CancellationTokenSource caller = new();
        Exception failure = cancel ? new OperationCanceledException(caller.Token) : new HttpRequestException();
        int reads = 0;
        InMemoryRecoveryReadModelStore store = new()
        {
            OnReadKey = _ =>
            {
                if (++reads == failureRead)
                {
                    if (cancel)
                    {
                        caller.Cancel();
                    }

                    throw failure;
                }
            },
        };
        using EventStoreDurableStateProbe durable = Durable();

        Exception? thrown = await Record.ExceptionAsync(() => Operations(store, durable)
            .RemainsGovernedOperationProjectionAbsentAsync(RecoveryValidationTopology.StorageTenantRef, Note, caller.Token));

        thrown.ShouldBeSameAs(failure);
        reads.ShouldBe(failureRead);
    }

    /// <summary>Polling continues throughout an absence window and stops on intermediate persisted presence.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SustainedWindowChecksIntermediateState(bool appears)
    {
        InMemoryRecoveryReadModelStore store = new();
        int reads = 0;
        string tenant = RecoveryValidationTopology.StorageTenantRef;
        store.OnReadKey = key =>
        {
            if (++reads == 2 && appears)
            {
                DateTimeOffset now = DateTimeOffset.Parse("2026-08-01T00:00:00Z", CultureInfo.InvariantCulture);
                store.SaveAsync(ChatBotReadModelStoreNames.StateStoreName, key, new GovernedOperationView(
                    tenant, Note, GovernedOperationView.CurrentSchemaVersion,
                    GovernedOperationView.GovernedCommandProvenance, GovernedOperationView.CurrentDerivationKernelVersion,
                    GovernedOperationView.MetadataOnlyRedactionState, GovernedOperationView.GovernedOperationalRetentionClass,
                    1, now, now)).GetAwaiter().GetResult();
            }
        };
        using EventStoreDurableStateProbe durable = Durable();
        TimeSpan window = TimeSpan.FromMilliseconds(30);
        AspireRecoverySandboxOperations operations = new(new RecoverySandboxOperationsTestSeam(), store, store, durable,
            TimeSpan.FromMilliseconds(1), window, TimeSpan.FromMilliseconds(10));
        Stopwatch timer = Stopwatch.StartNew();

        bool absent = await operations.RemainsGovernedOperationProjectionAbsentAsync(
            tenant, Note, TestContext.Current.CancellationToken);

        absent.ShouldBe(!appears);
        if (appears)
        {
            reads.ShouldBe(2);
        }
        else
        {
            reads.ShouldBeGreaterThan(2);
            timer.Elapsed.ShouldBeGreaterThanOrEqualTo(window);
        }
    }

    /// <summary>Actual caller cancellation during the first polling delay propagates without a final success.</summary>
    [Fact]
    public async Task CancellationDuringDelayPropagates()
    {
        using CancellationTokenSource caller = new();
        int reads = 0;
        InMemoryRecoveryReadModelStore store = new()
        {
            OnReadKey = _ =>
            {
                reads++;
                caller.Cancel();
            },
        };
        using EventStoreDurableStateProbe durable = Durable();

        OperationCanceledException thrown = await Should.ThrowAsync<OperationCanceledException>(() => Operations(store, durable)
            .RemainsGovernedOperationProjectionAbsentAsync(RecoveryValidationTopology.StorageTenantRef, Note, caller.Token));

        thrown.CancellationToken.ShouldBe(caller.Token);
        reads.ShouldBe(1);
    }

    private static AspireRecoverySandboxOperations Operations(
        InMemoryRecoveryReadModelStore store, EventStoreDurableStateProbe durable)
        => new(new RecoverySandboxOperationsTestSeam(), store, store, durable,
            TimeSpan.FromMilliseconds(1), TimeSpan.FromTicks(1), TimeSpan.FromMilliseconds(10));

    private static EventStoreDurableStateProbe Durable()
        => new(new Uri("http://eventstore.test"), new RecoveryDurableStateMessageHandler(),
            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1));
}
