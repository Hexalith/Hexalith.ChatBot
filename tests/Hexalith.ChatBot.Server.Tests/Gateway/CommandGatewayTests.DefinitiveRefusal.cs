using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.EventStore.Client.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false, "domain-rejection")]
    [InlineData(true, "domain-rejection")]
    [InlineData(false, "backpressure")]
    [InlineData(true, "backpressure")]
    public async Task DefinitiveEventStoreRefusalShouldReleaseOwnershipSoTheSameIdRetryDispatchesAgain(bool durable, string refusal)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new() { SubmissionFailure = EventStoreRefusal(refusal) };
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock);
        IIdempotencyStore store = durable
            ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)
            : new InMemoryCoarseIdempotencyStore(clock);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));

        ChatBotGatewayResult refused = await Gateway(dispatcher, clock: clock, idempotencyStore: store)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);

        refused.IsAccepted.ShouldBeFalse();
        ProblemDetails problem = refused.Problem.ShouldNotBeNull();
        problem.Status.ShouldBe(503);
        problem.Type.ShouldBe(ChatBotProblemTypes.DispatchUnavailable);
        problem.Retryable.ShouldBeTrue();
        platform.SubmissionCount.ShouldBe(1);
        if (durable)
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldBeEmpty();
        }

        // The refusal is authoritative non-commit proof, so the same caller ID dispatches again with the same
        // message ID (after every process-local service is replaced, for the durable store). EventStore's actor
        // deduplicates that message ID and replays its cached domain rejection, so the retry is refused again and
        // released again. Back-pressure is refused before EventStore admits the command, so the retry can succeed.
        if (refusal == "backpressure")
        {
            platform.SubmissionFailure = null;
        }

        IIdempotencyStore replacement = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : store;
        ChatBotGatewayResult retry = await Gateway(dispatcher, clock: clock, idempotencyStore: replacement)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);

        platform.SubmissionCount.ShouldBe(2);
        platform.SubmittedMessageIds.ShouldBe([submission.Request.CommandId, submission.Request.CommandId]);
        if (refusal == "backpressure")
        {
            retry.IsAccepted.ShouldBeTrue();
            retry.Accepted.ShouldNotBeNull().CommandId.ShouldBe(submission.Request.CommandId);
            if (durable)
            {
                state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
                state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            }
            else
            {
                ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            }

            return;
        }

        retry.IsAccepted.ShouldBeFalse();
        ProblemDetails retryProblem = retry.Problem.ShouldNotBeNull();
        retryProblem.Status.ShouldBe(503);
        retryProblem.Type.ShouldBe(ChatBotProblemTypes.DispatchUnavailable);
        retryProblem.Retryable.ShouldBeTrue();
        if (durable)
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefinitiveEventStoreRefusalShouldUnblockAnEquivalentSpecializedRequestWithAnotherCallerId(bool durable)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new() { SubmissionFailure = EventStoreRefusal("domain-rejection") };
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock);
        IIdempotencyStore store = durable
            ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)
            : new InMemoryCoarseIdempotencyStore(clock);
        ClaimsPrincipal principal = Principal(BoundTenant);
        ChatBotCommandSubmission original = Submission(principal, RetryCommand(), origin: ChatBotSurfaceOrigin.Ui);
        ChatBotCommandSubmission equivalent = Submission(principal, RetryCommand(), origin: ChatBotSurfaceOrigin.Ui,
            commandId: "01ARZ3NDEKTSV4RRFFQ69G5FBB");

        ChatBotGatewayResult refused = await Gateway(dispatcher, clock: clock, idempotencyStore: store,
                commandAllowlist: new ChatBotSpineCommandAllowlist())
            .SubmitAsync(original, TestContext.Current.CancellationToken);
        refused.IsAccepted.ShouldBeFalse();
        refused.Problem.ShouldNotBeNull().Type.ShouldBe(ChatBotProblemTypes.DispatchUnavailable);
        platform.SubmissionCount.ShouldBe(1);

        // Without the release, the equivalent specialized request would meet a Dispatching domain reservation and
        // stay recovery-pending (503) forever.
        platform.SubmissionFailure = null;
        IIdempotencyStore replacement = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : store;
        ChatBotGatewayResult admitted = await Gateway(dispatcher, clock: clock, idempotencyStore: replacement,
                commandAllowlist: new ChatBotSpineCommandAllowlist())
            .SubmitAsync(equivalent, TestContext.Current.CancellationToken);

        admitted.IsAccepted.ShouldBeTrue();
        CommandSubmissionResponse accepted = admitted.Accepted.ShouldNotBeNull();
        accepted.CommandId.ShouldBe(equivalent.Request.CommandId);
        accepted.PriorOutcome.ShouldBeNull();
        platform.SubmissionCount.ShouldBe(2);
        platform.SubmittedMessageIds[^1].ShouldBe(equivalent.Request.CommandId);
    }

    [Theory]
    [InlineData(false, "gateway-unreachable")]
    [InlineData(true, "gateway-unreachable")]
    [InlineData(false, "outcome-unknown")]
    [InlineData(true, "outcome-unknown")]
    [InlineData(false, "untyped-rejection")]
    [InlineData(true, "untyped-rejection")]
    [InlineData(false, "identity-conflict")]
    [InlineData(true, "identity-conflict")]
    [InlineData(false, "writer-then-rejection")]
    [InlineData(true, "writer-then-rejection")]
    public async Task UnprovenEventStoreFailureShouldRetainDispatchOwnershipWithoutRedispatch(bool durable, string failure)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new() { SubmissionFailure = EventStoreRefusal(failure) };
        UncertainExternalWriters writers = new() { AcknowledgeWrites = true };
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock,
            conversationWriter: writers, outboundMailboxSender: writers);
        IIdempotencyStore store = durable
            ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)
            : new InMemoryCoarseIdempotencyStore(clock);
        object command = failure == "writer-then-rejection"
            ? OutboundSendCommand("send-refused-after-write")
            : new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant,
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
            new Claim(OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-send"),
            new Claim(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "authenticated-user-send"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxIdClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxOwnerClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.OwnMailboxMailSendClaim, "true")), command);

        ChatBotGatewayResult first = await Gateway(dispatcher, clock: clock, idempotencyStore: store)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeFalse();
        first.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        platform.SubmissionCount.ShouldBe(1);
        writers.Attempts.ShouldBe(failure == "writer-then-rejection" ? 1 : 0);

        // An uncertain or unproven failure can never redispatch: even well past the lease and replay window, a
        // retry through replaced services stays recovery-pending.
        clock.UtcNow += TimeSpan.FromHours(26);
        RecordingDispatcher replacementDispatcher = new();
        IIdempotencyStore replacement = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : store;
        ChatBotGatewayResult retry = await Gateway(replacementDispatcher, clock: clock, idempotencyStore: replacement)
            .SubmitAsync(submission, TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        retry.IsAccepted.ShouldBeFalse();
        retry.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        retry.Problem.Retryable.ShouldBeTrue();
        replacementDispatcher.DispatchCount.ShouldBe(0);
        if (durable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldBeNull();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
    }

    [Theory]
    [InlineData(false, "not-submitted")]
    [InlineData(true, "not-submitted")]
    [InlineData(false, "refused")]
    [InlineData(true, "refused")]
    public async Task ProvenNonCommitFailureShouldReleaseOwnershipWithAnIndependentTokenWhenTheCallerCancels(bool durable, string boundary)
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new()
        {
            CancelOnSubmission = boundary == "refused" ? caller : null,
            SubmissionFailure = boundary == "refused" ? EventStoreRefusal("domain-rejection") : null,
        };
        ICommandDispatcher dispatcher = boundary == "refused"
            ? new AcceptedCommandDispatcher(platform, null!, null!, clock)
            : new CancelingNotSubmittedDispatcher(caller);
        IIdempotencyStore real = durable
            ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)
            : new InMemoryCoarseIdempotencyStore(clock);
        CallerTokenObservingStore observed = new(real, caller);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));

        ChatBotGatewayResult failure = await Gateway(dispatcher, clock: clock, idempotencyStore: observed)
            .SubmitAsync(submission, caller.Token);

        caller.IsCancellationRequested.ShouldBeTrue();
        failure.IsAccepted.ShouldBeFalse();
        failure.Problem.ShouldNotBeNull().Type.ShouldBe(ChatBotProblemTypes.DispatchUnavailable);
        observed.UndispatchedAborts.ShouldBe(1);
        observed.CleanupTokenWasIndependent.ShouldBeTrue();
        if (durable)
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)real).Records.ShouldBeEmpty();
        }

        platform.SubmissionFailure = null;
        RecordingDispatcher next = new();
        IIdempotencyStore restored = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : real;
        ChatBotGatewayResult retry = await Gateway(next, clock: clock, idempotencyStore: restored)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        retry.IsAccepted.ShouldBeTrue();
        next.DispatchCount.ShouldBe(1);
    }

    private static EventStoreGatewayException EventStoreRefusal(string kind)
        => kind switch
        {
            // Mirrors EventStore's DomainCommandRejectedExceptionHandler problem shape.
            "domain-rejection" or "writer-then-rejection" => new EventStoreGatewayException(
                422,
                "Governed Note Rejected",
                type: "https://hexalith.io/problems/domain-rejections/governed-note-rejected",
                extensions: new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["rejectionType"] = JsonSerializer.SerializeToElement("Hexalith.ChatBot.Server.GovernedNoteRejected"),
                },
                reasonCode: "governed-note-rejected"),

            // Mirrors EventStore's BackpressureExceptionHandler problem shape.
            "backpressure" => new EventStoreGatewayException(
                429,
                "Too Many Requests",
                type: "https://hexalith.io/problems/backpressure-exceeded",
                retryAfter: "5"),

            // Transport faults are translated by the gateway client into a 503 with no proof of non-commit.
            "gateway-unreachable" => new EventStoreGatewayException(
                503,
                "EventStore gateway unavailable",
                reason: "gateway-unreachable",
                innerException: new HttpRequestException("Injected sidecar outage.")),

            // The original mutation outcome is explicitly unknown to EventStore.
            "outcome-unknown" => new EventStoreGatewayException(
                409,
                "Conflict",
                type: "https://hexalith.io/problems/idempotency-admission-failure",
                code: "idempotency_outcome_unknown",
                retryable: true,
                clientAction: "poll_status_then_retry"),

            // A rejection-shaped status without the typed rejection evidence does not prove non-commit.
            "untyped-rejection" => new EventStoreGatewayException(
                422,
                "Unprocessable Entity",
                type: "https://hexalith.io/problems/domain-rejections/unknown"),

            // Another command already owns this message ID; this submission's own outcome is not proven.
            "identity-conflict" => new EventStoreGatewayException(
                409,
                "Conflict",
                type: "https://hexalith.io/problems/command-identity-conflict"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private sealed class CancelingNotSubmittedDispatcher(CancellationTokenSource caller) : ICommandDispatcher
    {
        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            // The caller disconnects while planning fails before any external write was attempted.
            caller.Cancel();
            context.ExternalEffectAttempted.ShouldBeFalse();
            throw new CommandNotSubmittedException(new InvalidOperationException("Injected planning failure."));
        }
    }

    /// <summary>Models a production state client that honours the caller's cancellation on cleanup.</summary>
    private sealed class CallerTokenObservingStore(IIdempotencyStore real, CancellationTokenSource caller) : IIdempotencyStore
    {
        public int UndispatchedAborts { get; private set; }

        public bool CleanupTokenWasIndependent { get; private set; }

        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken token)
            => real.RecordAdmissionAsync(context, token);

        public ValueTask<bool> PrepareDispatchAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
            => real.PrepareDispatchAsync(metadata, outcome, token);

        public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
            => real.RecordOutcomeAsync(metadata, outcome, token);

        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken token)
            => real.AbortAdmissionAsync(metadata, token);

        public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
        {
            UndispatchedAborts++;
            CleanupTokenWasIndependent = token.CanBeCanceled && !token.IsCancellationRequested && token != caller.Token;
            token.ThrowIfCancellationRequested();
            return real.AbortUndispatchedAsync(metadata, outcome, token);
        }
    }
}
