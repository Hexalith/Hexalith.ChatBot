using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Status;

public sealed class OperationStatusStoreRegistrationTests
{
    [Fact]
    public void DaprStateStoreSwapShouldReplaceVolatileOperationStatusStore()
    {
        ServiceCollection services = new();

        _ = services.AddChatBotCommandGateway();
        _ = services.AddChatBotDaprStateStores();

        ServiceDescriptor statusStore = services
            .Where(static descriptor => descriptor.ServiceType == typeof(IOperationStatusStore))
            .ShouldHaveSingleItem();
        statusStore.ImplementationType.ShouldBe(typeof(DaprOperationStatusStore));
    }

    [Fact]
    public void OperationStatusDaprKeyShouldBeTenantPartitioned()
    {
        string key = DaprOperationStatusStore.KeyFor("tenant-alpha", "operation-123");

        key.ShouldBe("tenant-alpha:operation-status:operation-123");
        key.ShouldStartWith("tenant-alpha:");
        key.ShouldNotBe(GovernedOperationView.KeyFor("tenant-alpha", "operation-123"));
    }
}
