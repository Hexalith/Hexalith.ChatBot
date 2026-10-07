using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Outbound;

using Shouldly;

using ScoreMailboxMessageAssociation = Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false, "preparation", false)]
    [InlineData(true, "preparation", false)]
    [InlineData(true, "preparation", true)]
    [InlineData(false, "prepared-return", false)]
    [InlineData(true, "prepared-return", false)]
    [InlineData(true, "prepared-return", true)]
    [InlineData(false, "planning", false)]
    [InlineData(true, "planning", false)]
    [InlineData(true, "planning", true)]
    public async Task CancellationBeforeExternalWriteMustReleaseExactPreparedOwnership(bool durable, string boundary, bool cleanupUnavailable)
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        IIdempotencyStore real = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        CancellationPreparationStore injected = new(real, caller, state, boundary != "planning", cleanupUnavailable, boundary == "preparation");
        CancelingScoringOrchestrator planning = new(caller, state, cleanupUnavailable);
        DurableRecoveryEventStoreClient platform = new();
        AcceptedCommandDispatcher dispatcher = new(platform, null!, planning, clock);
        ChatBotCommandSubmission request = Submission(Principal(BoundTenant), boundary == "planning"
            ? AssociationScoringCommand("cancellation-kernel") : new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        await Should.ThrowAsync<OperationCanceledException>(() => Gateway(dispatcher, idempotencyStore: injected)
            .SubmitAsync(request, caller.Token).AsTask());
        caller.IsCancellationRequested.ShouldBeTrue();
        injected.CleanupTokenWasIndependent.ShouldBeTrue();
        platform.SubmissionCount.ShouldBe(0);
        planning.Attempts.ShouldBe(boundary == "planning" ? 1 : 0);
        if (durable && cleanupUnavailable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Aborted);
        }
        else if (durable)
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)real).Records.ShouldBeEmpty();
        }

        state.RejectDeletes = 0;
        IIdempotencyStore restored = durable ? new DaprCoarseIdempotencyStore(state, new FixedClock()) : real;
        RecordingDispatcher nextDispatcher = new();
        ChatBotGatewayResult retry = await Gateway(nextDispatcher, idempotencyStore: restored)
            .SubmitAsync(request, TestContext.Current.CancellationToken);
        retry.IsAccepted.ShouldBeTrue();
        nextDispatcher.DispatchCount.ShouldBe(1);
        if (durable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        }
    }

    [Theory]
    [InlineData(false, "eventstore")]
    [InlineData(true, "eventstore")]
    [InlineData(false, "conversation")]
    [InlineData(true, "conversation")]
    [InlineData(false, "mailbox")]
    [InlineData(true, "mailbox")]
    public async Task CancellationAfterAnyAttemptedExternalWriteMustRetainDispatchFence(bool durable, string boundary)
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        DurableRecoveryEventStoreClient platform = new() { CancelOnSubmission = boundary == "eventstore" ? caller : null };
        CancelingExternalWriters writers = new(caller);
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock, conversationWriter: writers, outboundMailboxSender: writers);
        IIdempotencyStore store = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : new InMemoryCoarseIdempotencyStore(clock);
        object command = boundary switch
        {
            "conversation" => ApprovedExecutionCommand(),
            "mailbox" => OutboundSendCommand("send-canceled"),
            _ => new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"),
        };
        ChatBotCommandSubmission request = Submission(Principal(BoundTenant,
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
            new Claim(OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-send"),
            new Claim(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "authenticated-user-send"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxIdClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxOwnerClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.OwnMailboxMailSendClaim, "true")), command);
        if (boundary == "mailbox")
        {
            ChatBotGatewayResult denied = await Gateway(dispatcher, idempotencyStore: store).SubmitAsync(request, caller.Token);
            denied.IsAccepted.ShouldBeFalse();
            platform.SubmissionCount.ShouldBe(0);
            writers.Attempts.ShouldBe(0);
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
            if (!durable) { ((InMemoryCoarseIdempotencyStore)store).RecordCount.ShouldBe(0); }
            return;
        }
        await Should.ThrowAsync<OperationCanceledException>(() => Gateway(dispatcher, idempotencyStore: store).SubmitAsync(request, caller.Token).AsTask());
        platform.SubmissionCount.ShouldBe(boundary == "eventstore" ? 1 : 0);
        writers.Attempts.ShouldBe(boundary == "eventstore" ? 0 : 1);
        RecordingDispatcher retryDispatcher = new();
        IIdempotencyStore restored = durable ? new DaprCoarseIdempotencyStore(state, new FixedClock(), eventStore: platform) : store;
        ChatBotGatewayResult retry = await Gateway(retryDispatcher, idempotencyStore: restored).SubmitAsync(request, TestContext.Current.CancellationToken);
        retry.IsAccepted.ShouldBeFalse();
        retry.Problem!.Status.ShouldBe(503);
        retryDispatcher.DispatchCount.ShouldBe(0);
        if (durable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
    }

    private sealed class CancellationPreparationStore(IIdempotencyStore real, CancellationTokenSource caller,
        FakeCoarseIdempotencyStateClient state, bool cancelPreparation, bool cleanupUnavailable, bool throwPreparation) : IIdempotencyStore
    {
        public bool CleanupTokenWasIndependent { get; private set; }
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken token) => real.RecordAdmissionAsync(context, token);
        public async ValueTask<bool> PrepareDispatchAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
        {
            bool prepared = await real.PrepareDispatchAsync(metadata, outcome, token).ConfigureAwait(false);
            if (prepared && cancelPreparation)
            {
                state.RejectDeletes = cleanupUnavailable ? 3 : 0;
                caller.Cancel();
                if (throwPreparation)
                {
                    token.ThrowIfCancellationRequested();
                }
            }
            return prepared;
        }
        public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => real.RecordOutcomeAsync(metadata, outcome, token);
        public ValueTask<bool> BindDispatchTargetAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string aggregateId, CancellationToken token)
            => real.BindDispatchTargetAsync(metadata, outcome, aggregateId, token);
        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken token) => real.AbortAdmissionAsync(metadata, token);
        public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
        {
            CleanupTokenWasIndependent = token.CanBeCanceled && !token.IsCancellationRequested && caller.IsCancellationRequested && token != caller.Token;
            return real.AbortUndispatchedAsync(metadata, outcome, token);
        }
    }

    private sealed class CancelingScoringOrchestrator(CancellationTokenSource caller, FakeCoarseIdempotencyStateClient state,
        bool cleanupUnavailable) : IAssociationScoringOrchestrator
    {
        public int Attempts { get; private set; }
        public ValueTask<ScoreMailboxMessageAssociation> ScoreAsync(ScoreMailboxMessageAssociation command, ChatBotGatewayContext context, CancellationToken token)
        {
            Attempts++;
            context.ExternalEffectAttempted.ShouldBeFalse();
            state.RejectDeletes = cleanupUnavailable ? 3 : 0;
            caller.Cancel();
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult(command);
        }
    }

    private sealed class CancelingExternalWriters(CancellationTokenSource caller) : IConversationWriter, IOutboundMailboxSender
    {
        public int Attempts { get; private set; }
        public ValueTask<ConversationAppendResult> PrepareAppendConversationMessageAsync(ApprovedAiConversationAppendRequest request, CancellationToken token)
        {
            Attempts++;
            caller.Cancel();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not propagated.");
        }
        public ValueTask<OutboundMailboxSendResult> SendAsync(OutboundMailboxSendRequest request, CancellationToken token = default)
        {
            Attempts++;
            caller.Cancel();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not propagated.");
        }
    }
}
