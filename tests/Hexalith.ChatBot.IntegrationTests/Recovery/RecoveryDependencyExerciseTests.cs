using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Focused contract tests for the four non-resource scoped dependency exercises.</summary>
public sealed class RecoveryDependencyExerciseTests
{
    [Theory]
    [InlineData("ai-provider", "operation")]
    [InlineData("command-execution", "operation")]
    [InlineData("audit-store", "command-surface")]
    [InlineData("attachment-processing", "workflow-item")]
    public async Task FaultIsObservedByTheConcreteSeamAndRecoveryIsIdempotent(
        string dependency,
        string expectedScope)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RecoveryScopedOutageState state = new();
        using RecoveryScopeObservationMonitor monitor = new();
        await monitor.StartAsync(cancellationToken);
        try
        {
            RecoveryDependencyExercise exercise = new(
                state,
                new RecoveryAiAssistanceProvider(state),
                new RecoveryEventStoreGatewayClient(state),
                new RecoveryAuditWriter(state),
                new RecoveryAttachmentContentSource(state),
                new RecoveryFolderStore(),
                new RecoveryTenantAiPolicySnapshotProvider(),
                new InMemoryProjectConversationProjectionStore(),
                monitor);
            _ = state.Fault(dependency, DateTimeOffset.UtcNow);

            (RecoveryDependencyExerciseResult fault, RecoveryScopeObservation? scope) = await exercise.ProcessAsync(
                dependency,
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                cancellationToken);

            fault.FaultObserved.ShouldBeTrue();
            fault.EffectCount.ShouldBe(0);
            fault.UnauthorizedMutationDetected.ShouldBeFalse();
            scope.ShouldNotBeNull();
            scope.ObservedScope.ShouldBe(expectedScope);
            scope.ScopeRecordedAtUtc.ShouldBeGreaterThanOrEqualTo(scope.DependencyFailureObservedAtUtc);

            _ = state.Restore(dependency, DateTimeOffset.UtcNow);
            (RecoveryDependencyExerciseResult first, _) = await exercise.ProcessAsync(
                dependency,
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                cancellationToken);
            (RecoveryDependencyExerciseResult replay, _) = await exercise.ProcessAsync(
                dependency,
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                cancellationToken);

            first.FaultObserved.ShouldBeFalse();
            replay.EffectCount.ShouldBe(1);
            replay.SilentDataLossDetected.ShouldBeFalse();
            replay.DuplicateSideEffectDetected.ShouldBeFalse();
            replay.CrossTenantLeakageDetected.ShouldBeFalse();
        }
        finally
        {
            await monitor.StopAsync(cancellationToken);
        }
    }
}
