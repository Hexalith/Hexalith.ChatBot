using Hexalith.ChatBot.Server;
using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

public static class PlatformReferenceTests
{
    [Fact]
    public static void ServerShouldResolveEventStoreAndTenantContractTypes()
    {
        ChatBotPlatformReferences.EventPayloadContractType.ShouldBe(typeof(IEventPayload));
        ChatBotPlatformReferences.SystemTenantId.ShouldBe("system");
    }
}
