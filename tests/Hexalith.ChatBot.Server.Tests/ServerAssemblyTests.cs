using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

public static class ServerAssemblyTests
{
    [Fact]
    public static void ServerAssemblyShouldBeAvailableForAppHost()
    {
        typeof(Program).Assembly.GetName().Name.ShouldBe("Hexalith.ChatBot.Server");
    }
}
