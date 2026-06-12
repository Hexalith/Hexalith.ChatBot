using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Status;

public sealed class OperationStatusStoreRegistrationTests
{
    [Fact]
    public void DaprStateStoreSwapShouldReplaceVolatileOperationStatusStoreWithSdkReadModelStore()
    {
        ServiceCollection services = new();

        _ = services.AddChatBotCommandGateway();
        _ = services.AddChatBotDaprStateStores();

        ServiceDescriptor statusStore = services
            .Where(static descriptor => descriptor.ServiceType == typeof(IOperationStatusStore))
            .ShouldHaveSingleItem();
        statusStore.ImplementationType.ShouldBe(typeof(ReadModelOperationStatusStore));
    }

    [Fact]
    public void OperationStatusReadModelKeyShouldBeTenantPartitioned()
    {
        string key = ReadModelOperationStatusStore.KeyFor("tenant-alpha", "operation-123");

        key.ShouldBe("tenant-alpha:operation-status:operation-123");
        key.ShouldStartWith("tenant-alpha:");
        key.ShouldNotBe(GovernedOperationView.KeyFor("tenant-alpha", "operation-123"));
    }
}
