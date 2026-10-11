using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Streams;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Authorization;

public static class ChatBotAuthorityGrantAggregateTests
{
    private const string TenantId = "tenant-alpha";

    [Fact]
    public static void ExactGrantCanBeRevokedAndReissuedWithoutRevivingOldIssuance()
    {
        GrantChatBotAuthority first = Grant("grant-001");
        DomainResult issued = GovernedOperationAggregate.Handle(first, null, Envelope(first, first.AuthorityKey));
        ChatBotAuthorityGranted firstEvent = issued.Events.ShouldHaveSingleItem().ShouldBeOfType<ChatBotAuthorityGranted>();
        GovernedOperationState state = new();
        state.Apply(firstEvent);

        RevokeChatBotAuthority revoke = new(first.AuthorityKey, first.GrantId);
        DomainResult revoked = GovernedOperationAggregate.Handle(revoke, state, Envelope(revoke, first.AuthorityKey));
        state.Apply(revoked.Events.ShouldHaveSingleItem().ShouldBeOfType<ChatBotAuthorityRevoked>());
        state.AuthorityGrantRevoked.ShouldBeTrue();
        GovernedOperationAggregate.Handle(first, state, Envelope(first, first.AuthorityKey)).IsRejection.ShouldBeTrue();

        GrantChatBotAuthority second = Grant("grant-002");
        DomainResult reissued = GovernedOperationAggregate.Handle(second, state, Envelope(second, second.AuthorityKey));
        state.Apply(reissued.Events.ShouldHaveSingleItem().ShouldBeOfType<ChatBotAuthorityGranted>());
        state.AuthorityGrantRevoked.ShouldBeFalse();
        GovernedOperationAggregate.Handle(revoke, state, Envelope(revoke, first.AuthorityKey)).IsRejection.ShouldBeTrue();
        state.CurrentAuthorityGrant!.GrantId.ShouldBe("grant-002");
    }

    [Fact]
    public static void MismatchedStreamAndMachineScopeCannotIssueAuthority()
    {
        GrantChatBotAuthority human = Grant("grant-001");
        GovernedOperationAggregate.Handle(human, null, Envelope(human, "other-stream")).IsRejection.ShouldBeTrue();

        GrantChatBotAuthority invalidMachine = human with
        {
            ActorClass = "service",
            Authority = "service-grant",
            AuthorityKey = ChatBotAuthorityGrantKey.For(TenantId, human.PrincipalId, human.ResourceId,
                human.Operation, "service-grant", "service", human.Origin),
        };
        GovernedOperationAggregate.Handle(invalidMachine, null, Envelope(invalidMachine, invalidMachine.AuthorityKey)).IsRejection.ShouldBeTrue();
    }

    [Fact]
    public static void SourceFoldUsesOnlyCurrentExactIssuance()
    {
        GrantChatBotAuthority grant = Grant("grant-001");
        ChatBotAuthorityGranted issued = GovernedOperationAggregate.Handle(grant, null, Envelope(grant, grant.AuthorityKey))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ChatBotAuthorityGranted>();
        ChatBotOwnerAuthorityRequest request = new("ChatBot", grant.PrincipalId, TenantId, grant.ResourceId,
            grant.Operation, grant.Authority, grant.ActorClass, grant.Origin, true);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        StreamReadEvent grantEvent = Event(1, issued);
        AuthoritativeEventStream source = new(new AggregateIdentity(TenantId, "chatbot", grant.AuthorityKey),
            1, now, [grantEvent], "observation-001");

        ChatBotGrantSourceEvidence.FromStream(request, source, now)!.IsAllowed.ShouldBeTrue();
        ChatBotGrantSourceEvidence.FromStream(request with { Operation = nameof(RevokeChatBotAuthority) }, source, now).ShouldBeNull();
        AuthoritativeEventStream revoked = source with
        {
            Head = 2,
            Events = [grantEvent, Event(2, new ChatBotAuthorityRevoked(grant.GrantId))],
        };
        ChatBotGrantSourceEvidence.FromStream(request, revoked, now).ShouldBeNull();
    }

    private static GrantChatBotAuthority Grant(string grantId)
    {
        string key = ChatBotAuthorityGrantKey.For(TenantId, "actor-alpha", TenantId, nameof(RecordGovernedNote), "operation", "human", ChatBotSurfaceOrigin.Api);
        return new(key, grantId, "actor-alpha", TenantId, nameof(RecordGovernedNote), "operation", "human", ChatBotSurfaceOrigin.Api, DateTimeOffset.UtcNow.AddHours(1));
    }

    private static CommandEnvelope Envelope(IChatBotCommand command, string aggregateId)
        => new("01ARZ3NDEKTSV4RRFFQ69G5FAV", TenantId, "chatbot", aggregateId, command.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(command), "01ARZ3NDEKTSV4RRFFQ69G5FAW", null, "actor-alpha", null);

    private static StreamReadEvent Event(long sequence, object payload)
        => new(sequence, payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload), "json", 1,
            $"message-{sequence}", null, null, DateTimeOffset.UtcNow, "actor-alpha");
}
