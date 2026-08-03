using System.Text.Json;

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
        // RecordAsync self-drains after the poll interval; a hosted StartAsync loop is not required for this unit test.
        using RecoveryScopeObservationMonitor monitor = new(TimeSpan.FromMilliseconds(10));
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

        string restoreJson = JsonSerializer.Serialize(state.Restore(dependency, DateTimeOffset.UtcNow));
        using JsonDocument restoreDocument = JsonDocument.Parse(restoreJson);
        RecoverySandboxRestoreResponse.WasPreviouslyFaulted(restoreDocument.RootElement).ShouldBeTrue();
        RecoverySandboxRestoreResponse.IsCurrentlyFaulted(restoreDocument.RootElement).ShouldBeFalse();

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
}
