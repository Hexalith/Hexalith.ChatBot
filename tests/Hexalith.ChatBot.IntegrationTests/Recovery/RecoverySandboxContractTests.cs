using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Workers.Mailbox;

using CaptureMailboxMessageIntake = Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake;
using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Contract tests for the recovery sandbox Restore body and controller authorization.</summary>
public sealed class RecoverySandboxContractTests
{
    private const string TenantRef = "replay-test:recovery-validation";
    private const string AllowlistedMailboxId = "recovery-mailbox-001";

    [Fact]
    public void ScopedRestoreReturnsPriorAndCurrentSnapshots()
    {
        RecoveryScopedOutageState state = new();
        _ = state.Fault("ai-provider", DateTimeOffset.UtcNow);

        string json = JsonSerializer.Serialize(state.Restore("ai-provider", TenantRef, DateTimeOffset.UtcNow));
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        RecoverySandboxRestoreResponse.WasPreviouslyFaulted(root).ShouldBeTrue();
        RecoverySandboxRestoreResponse.IsCurrentlyFaulted(root).ShouldBeFalse();
        RecoverySandboxRestoreResponse.CrossTenantEffectDetectedBeforeRestore(root).ShouldBeFalse();
        state.IsFaulted("ai-provider").ShouldBeFalse();
    }

    [Fact]
    public void ScopedRestoreReportsAnEffectRecordedOutsideTheExpectedTenantBeforeClearingIt()
    {
        RecoveryScopedOutageState state = new();
        _ = state.Fault("ai-provider", DateTimeOffset.UtcNow);
        _ = state.RecordEffect("ai-provider", "tenant-beta", "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        string json = JsonSerializer.Serialize(state.Restore("ai-provider", TenantRef, DateTimeOffset.UtcNow));
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        RecoverySandboxRestoreResponse.CrossTenantEffectDetectedBeforeRestore(root).ShouldBeTrue();
        state.HasCrossTenantEffect("ai-provider", TenantRef).ShouldBeFalse();
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

    [Fact]
    public async Task ControlledLossCandidateRequiresAnActiveSubscriptionFault()
    {
        RecoverySubscriptionSimulatorState state = new();
        ControlledGraphMailboxMessageSource source = new(state);
        GraphMailboxNotification notification = new(
            "recovery-mailbox-001",
            RecoveryNotificationIdentity.Compose(
                "provider-message",
                RecoveryNotificationIdentity.ControlledLossLane,
                RecoveryNotificationIdentity.LossPhase),
            OpaqueProviderState: null);

        GraphMailboxFetchResult healthyResult = await source.FetchMessageAsync(
            notification,
            TestContext.Current.CancellationToken);
        healthyResult.Kind.ShouldBe(GraphMailboxFetchResultKind.RetryableFailure);
        healthyResult.ReasonCode.ShouldBe("controlled_loss_fault_not_active");

        state.Fault(DateTimeOffset.UtcNow);
        GraphMailboxFetchResult faultedResult = await source.FetchMessageAsync(
            notification,
            TestContext.Current.CancellationToken);
        faultedResult.Kind.ShouldBe(GraphMailboxFetchResultKind.Found);
    }

    /// <summary>
    /// Drives the real intake worker across the decorator that actually creates the controlled loss. Everything else
    /// in the channel — the candidate identity, its proven absence, and the RPO the gate grades — is downstream of
    /// this one rejection, and outside a hosted Tier-3 run the decorator was covered only by a source-text match on
    /// its class name. Inverting the rejection, or capturing the identity after the throw, left every suite green.
    /// </summary>
    /// <param name="rejectSubmission">Whether the sandbox decorator is in its loss-producing configuration.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ControlledLossDecoratorCapturesTheCandidateAndRejectsOnlyTheLossPhase(bool rejectSubmission)
    {
        RecoverySubscriptionSimulatorState state = new();
        state.Fault(DateTimeOffset.UtcNow);
        RecordingChatBotClient inner = new();
        CapturingRecoveryChatBotClient client = new(inner, rejectSubmission);
        GraphMailboxIntakeWorker worker = new(
            new ControlledMailboxPattern(AllowlistedMailboxId, "recovery-graph-v1"),
            new ControlledGraphMailboxMessageSource(state),
            client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification(
                AllowlistedMailboxId,
                RecoveryNotificationIdentity.Compose(
                    "provider-message",
                    RecoveryNotificationIdentity.ControlledLossLane,
                    RecoveryNotificationIdentity.LossPhase),
                OpaqueProviderState: null),
            cancellationToken: TestContext.Current.CancellationToken);

        // The identity is captured before the dependency decision either way: it is the only safe handle the drill
        // has on a candidate it is about to lose.
        RecoveryValidationEvidenceManifest.IsCanonicalUlid(client.CandidateRef).ShouldBeTrue();
        client.ObservedAtUtc.ShouldNotBeNull();
        client.ObservedAtUtc!.Value.Offset.ShouldBe(TimeSpan.Zero);

        if (rejectSubmission)
        {
            result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);

            // The exact reason code AspireRecoverySandboxOperations.RejectControlledLossCandidateAsync requires
            // before it will publish `candidate-rejected`.
            result.ReasonCode.ShouldBe("chatbot_submission_recoverable");
            inner.Submitted.ShouldBeEmpty();
        }
        else
        {
            result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
            result.IntakeId.ShouldBe(client.CandidateRef);
            inner.Submitted.ShouldHaveSingleItem().IntakeId.ShouldBe(client.CandidateRef);
        }
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

    private sealed class RecordingChatBotClient : IChatBotClient
    {
        public List<CaptureMailboxMessageIntake> Submitted { get; } = [];

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            if (command is CaptureMailboxMessageIntake intake)
            {
                Submitted.Add(intake);
            }

            return Task.FromResult(new CommandSubmissionResponse());
        }

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
