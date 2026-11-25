using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Qualifies the live mailbox grant fixtures against the ChatBot authority aggregate.</summary>
public sealed class RecoveryMailboxOwnerProvisionerTests
{
    [Fact]
    public void RecoveryMailboxGrantCommandsShouldPassOwnerValidationAndKeepProbeScope()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        (GrantChatBotAuthority operation, GrantChatBotAuthority service) = RecoveryMailboxOwnerProvisioner.CreateGrants(now);

        operation.ResourceId.ShouldBe(RecoveryMailboxOwnerProvisioner.ProbeIntakeId);
        operation.Authority.ShouldBe("operation");
        operation.ExpiresAt.ShouldBe(now.AddHours(1));
        operation.ServiceGrant.ShouldBeNull();
        service.ResourceId.ShouldBe(RecoveryValidationTopology.MailboxClientId);
        service.Authority.ShouldBe("service-grant");
        service.ExpiresAt.ShouldBe(now.AddDays(1));
        ServiceClientGrant nestedGrant = service.ServiceGrant.ShouldNotBeNull();
        nestedGrant.AllowedCommandNames.ShouldBe([nameof(CaptureMailboxMessageIntake)]);
        nestedGrant.AllowedQueryNames.ShouldBeEmpty();
        nestedGrant.ClientClass.ShouldBe(ServiceClientClass.MailboxIngestion);
        nestedGrant.Scopes.ShouldBe(["mailbox.ingest"]);
        operation.GrantId.ShouldNotBe(service.GrantId);
        operation.AuthorityKey.ShouldNotBe(service.AuthorityKey);

        foreach (GrantChatBotAuthority grant in new[] { operation, service })
        {
            CommandEnvelope envelope = new(ChatBotCommandId.New().ToString(), RecoveryValidationTopology.StorageTenantRef,
                ChatBotEventStore.DomainName, grant.AuthorityKey, nameof(GrantChatBotAuthority),
                JsonSerializer.SerializeToUtf8Bytes(grant), ChatBotCommandId.New().ToString(), null, "recovery-validator", null);

            ChatBotAuthorityGranted issued = GovernedOperationAggregate.Handle(grant, null, envelope)
                .Events.ShouldHaveSingleItem().ShouldBeOfType<ChatBotAuthorityGranted>();
            issued.PrincipalId.ShouldBe(RecoveryValidationTopology.MailboxClientId);
            issued.Operation.ShouldBe(nameof(CaptureMailboxMessageIntake));
            issued.TenantId.ShouldBe(RecoveryValidationTopology.StorageTenantRef);
            issued.Origin.ShouldBe(ChatBotSurfaceOrigin.Mailbox);
        }
    }
}
