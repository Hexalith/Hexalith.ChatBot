using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

public sealed class IdempotencyStateStoreIntegrationTests
{
    [Fact]
    public async Task EquivalentRepeatsShouldLeaveSingleCoarseRecordAndSingleDispatchInStateStoreLane()
    {
        FixedClock clock = new();
        InMemoryCoarseIdempotencyStore store = new(clock);
        RecordingDispatcher dispatcher = new();
        // RecoverySandbox already links and exposes these shared test fixtures to this assembly.
        // Synthetic owner/admission evidence isolates coarse persistence and replay mechanics.
        // This fixture does not qualify production owner authority or atomic execution.
        ChatBotRequestAuthorizer authorizer = new(
            new ChatBotAuthorityCatalog(),
            new SyntheticOwnerAuthorityProvider(clock),
            clock,
            new ServiceClientGrantProjectionCache(clock));
        CommandGateway gateway = new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            store,
            new RecordingAuditWriter(),
            new RecordingReplayIntentQueue(),
            new RecordingOperatorAlertSink(),
            new InMemoryOperationStatusStore(),
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new ChatBotSpineCommandAllowlist(),
            requestAuthorizer: authorizer,
            admissionEvidence: new SyntheticLegacyAdmissionEvidenceValidator());
        ChatBotCommandSubmission submission = Submission();

        ChatBotGatewayResult single = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        single.IsAccepted.ShouldBeTrue($"The first submission was denied with '{single.Problem?.Code}'.");
        single.Accepted.ShouldNotBeNull();
        CoarseIdempotencyRecord storedAfterSingle = store.Records.Single();
        ChatBotGatewayResult repeat = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        repeat.IsAccepted.ShouldBeTrue($"The repeated submission was denied with '{repeat.Problem?.Code}'.");
        repeat.Accepted.ShouldNotBeNull();
        CoarseIdempotencyRecord storedAfterRepeat = store.Records.Single();

        repeat.Accepted.CommandId.ShouldBe(single.Accepted.CommandId);
        dispatcher.DispatchCount.ShouldBe(1);
        storedAfterRepeat.ShouldBe(storedAfterSingle);
    }

    private static ChatBotCommandSubmission Submission()
        => new(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("sub", "actor-alpha"), new Claim("eventstore:tenant", "tenant-alpha")],
                    "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = nameof(RecordGovernedNote),
                Command = new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX");

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 30, 18, 30, 0, TimeSpan.Zero);
    }

    private sealed class RecordingDispatcher : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            DispatchCount++;
            return ValueTask.FromResult(new ChatBotDispatchResult(new DateTimeOffset(2026, 5, 30, 18, 30, 1, TimeSpan.Zero)));
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Success);

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Success);
    }

    private sealed class RecordingReplayIntentQueue : IAuditReplayIntentQueue
    {
        public ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class RecordingOperatorAlertSink : IOperatorAlertSink
    {
        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
