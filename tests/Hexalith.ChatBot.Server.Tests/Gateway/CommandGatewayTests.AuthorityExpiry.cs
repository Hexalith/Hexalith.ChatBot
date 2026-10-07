using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorityExpiryDuringPreparationReleasesOnlyUndispatchedOwnershipAndAllowsFreshRetry(bool durable)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        FakeCoarseIdempotencyStateClient state = new();
        IIdempotencyStore real = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        AuthorityExpiryPreparationStore injected = new(real, () => clock.UtcNow += TimeSpan.FromSeconds(2));
        RecordingDispatcher dispatcher = new();
        CommandGateway gateway = Gateway(dispatcher, clock: clock, idempotencyStore: injected,
            requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner));
        ChatBotCommandSubmission request = Submission(TrustedAuthorityFixture.Principal(), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        ChatBotGatewayResult result = await gateway.SubmitAsync(request, TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        dispatcher.DispatchCount.ShouldBe(0);
        injected.Prepared.ShouldBeTrue();
        if (durable)
        {
            state.DomainRecords.ShouldBeEmpty();
            state.IdentityRecords.ShouldBeEmpty();
        }
        else { ((InMemoryCoarseIdempotencyStore)real).Records.ShouldBeEmpty(); }

        injected.AdvanceDuringPreparation = false;
        ChatBotGatewayResult fresh = await gateway.SubmitAsync(request, TestContext.Current.CancellationToken);
        fresh.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        if (durable)
        {
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        }
        else { ((InMemoryCoarseIdempotencyStore)real).Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull(); }
    }

    private sealed class AuthorityExpiryPreparationStore(IIdempotencyStore real, Action advance) : IIdempotencyStore
    {
        public bool AdvanceDuringPreparation { get; set; } = true;
        public bool Prepared { get; private set; }
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken token) => real.RecordAdmissionAsync(context, token);
        public async ValueTask<bool> PrepareDispatchAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
        {
            Prepared = await real.PrepareDispatchAsync(metadata, outcome, token).ConfigureAwait(false);
            await Task.Yield();
            if (Prepared && AdvanceDuringPreparation) { advance(); }
            return Prepared;
        }
        public ValueTask<bool> BindDispatchTargetAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string aggregateId, CancellationToken token)
            => real.BindDispatchTargetAsync(metadata, outcome, aggregateId, token);
        public ValueTask<bool> ConfirmSdkSubmissionAcceptedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string aggregateId, CancellationToken token)
            => real.ConfirmSdkSubmissionAcceptedAsync(metadata, outcome, aggregateId, token);
        public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => real.RecordOutcomeAsync(metadata, outcome, token);
        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken token) => real.AbortAdmissionAsync(metadata, token);
        public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => real.AbortUndispatchedAsync(metadata, outcome, token);
    }
}
