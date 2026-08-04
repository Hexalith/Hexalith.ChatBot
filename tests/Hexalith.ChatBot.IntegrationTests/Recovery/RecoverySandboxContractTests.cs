using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.RecoverySandbox;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Contract tests for the recovery sandbox Restore body and controller authorization.</summary>
public sealed class RecoverySandboxContractTests
{
    private const string TenantRef = "replay-test:recovery-validation";

    [Fact]
    public void ScopedRestoreReturnsPriorAndCurrentSnapshots()
    {
        RecoveryScopedOutageState state = new();
        _ = state.Fault("ai-provider", DateTimeOffset.UtcNow);

        string json = JsonSerializer.Serialize(state.Restore("ai-provider", DateTimeOffset.UtcNow));
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        RecoverySandboxRestoreResponse.WasPreviouslyFaulted(root).ShouldBeTrue();
        RecoverySandboxRestoreResponse.IsCurrentlyFaulted(root).ShouldBeFalse();
        state.IsFaulted("ai-provider").ShouldBeFalse();
    }

    [Fact]
    public void SubscriptionRestoreReturnsPriorAndCurrentSnapshots()
    {
        RecoverySubscriptionSimulatorState state = new();
        state.Fault(DateTimeOffset.UtcNow);

        string json = JsonSerializer.Serialize(state.Restore(DateTimeOffset.UtcNow));
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        RecoverySandboxRestoreResponse.WasPreviouslyFaulted(root).ShouldBeTrue();
        RecoverySandboxRestoreResponse.IsCurrentlyFaulted(root).ShouldBeFalse();
        state.IsFaulted().ShouldBeFalse();
    }

    [Fact]
    public void RecordEffectCountsSameCorrelationEmissions()
    {
        RecoveryScopedOutageState state = new();
        state.RecordEffect("command-execution", TenantRef, "01ARZ3NDEKTSV4RRFFQ69G5FAW").ShouldBe(1);
        state.RecordEffect("command-execution", TenantRef, "01ARZ3NDEKTSV4RRFFQ69G5FAW").ShouldBe(2);
        state.EffectCount("command-execution", TenantRef).ShouldBe(2);
        state.CorrelationEffectCount("command-execution", TenantRef, "01ARZ3NDEKTSV4RRFFQ69G5FAW").ShouldBe(2);
    }

    [Fact]
    public void AuthorizationRejectsWrongSecretAndWrongTenant()
    {
        const string secret = "tier3-recovery-controller-secret";
        RecoverySandboxAuthorization.Authorized(TenantRef, TenantRef, secret, secret).ShouldBeTrue();
        RecoverySandboxAuthorization.Authorized(TenantRef, TenantRef, secret, "wrong-secret").ShouldBeFalse();
        RecoverySandboxAuthorization.Authorized("replay-test:other", TenantRef, secret, secret).ShouldBeFalse();
        RecoverySandboxAuthorization.Authorized(TenantRef, TenantRef, secret, presentedSecret: null).ShouldBeFalse();
        RecoverySandboxAuthorization.Authorized(TenantRef, TenantRef, secret, " ").ShouldBeFalse();
    }

    [Fact]
    public void NotificationIdentitySeparatesCheckpointFromRecoveryButKeepsRecoveryReplayStable()
    {
        string checkpoint = RecoveryNotificationIdentity.Compose(
            "provider-message",
            "continuity",
            RecoveryNotificationIdentity.CheckpointPhase);
        string recovery = RecoveryNotificationIdentity.Compose(
            "provider-message",
            "continuity",
            RecoveryNotificationIdentity.RecoveryPhase);

        checkpoint.ShouldNotBe(recovery);
        RecoveryNotificationIdentity.Compose(
            "provider-message",
            "continuity",
            RecoveryNotificationIdentity.RecoveryPhase).ShouldBe(recovery);
    }

    [Theory]
    [InlineData("unknown", "recovery")]
    [InlineData("graph", "unknown")]
    public void NotificationIdentityRejectsOpenEndedLaneOrPhase(string lane, string phase)
        => Should.Throw<InvalidOperationException>(() => RecoveryNotificationIdentity.Compose(
            "provider-message",
            lane,
            phase));

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public void RecoveryTokenStatusClassificationIsFailClosed(HttpStatusCode statusCode, bool retryable)
        => RecoveryAccessTokenProvider.IsRetryableStatus(statusCode).ShouldBe(retryable);

    [Fact]
    public void ScopeStampValidationAcceptsOnlyStrictlyOrderedNonDefaultBounds()
    {
        DateTimeOffset observed = new(2026, 8, 4, 10, 0, 0, TimeSpan.Zero);
        AspireScopedOutageOperations.RequireNonDegenerateScopeStamps(
            observed,
            observed.AddMilliseconds(1),
            "Graph");

        _ = Should.Throw<InvalidOperationException>(() => AspireScopedOutageOperations
            .RequireNonDegenerateScopeStamps(default, observed, "Graph"));
        _ = Should.Throw<InvalidOperationException>(() => AspireScopedOutageOperations
            .RequireNonDegenerateScopeStamps(observed, observed, "Graph"));
        _ = Should.Throw<InvalidOperationException>(() => AspireScopedOutageOperations
            .RequireNonDegenerateScopeStamps(observed, observed.AddMilliseconds(-1), "Graph"));
    }
}
