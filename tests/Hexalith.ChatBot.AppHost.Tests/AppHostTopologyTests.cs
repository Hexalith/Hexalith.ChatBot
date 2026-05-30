using Shouldly;

namespace Hexalith.ChatBot.AppHost.Tests;

public static class AppHostTopologyTests
{
    [Fact]
    public static void AppHostShouldFailFastWhenDaprAccessControlIsMissing()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));

        source.ShouldContain("ResolveDaprConfigPath");
        source.ShouldContain("throw new FileNotFoundException");
        source.ShouldContain("accesscontrol.yaml");
    }

    [Fact]
    public static void AppHostShouldWireKeycloakWithHealthyWaitFor()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string realm = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "KeycloakRealms", "hexalith-realm.json"));

        source.ShouldContain("AddKeycloak");
        source.ShouldContain("WaitFor(keycloak)");
        source.ShouldContain("\"hexalith-chatbot\"");
        source.ShouldContain("\"hexalith-eventstore\"");
        source.ShouldContain("\"hexalith-tenants\"");
        realm.ShouldContain("\"clientId\": \"hexalith-chatbot\"");
        realm.ShouldContain("\"clientId\": \"hexalith-eventstore\"");
        realm.ShouldContain("\"clientId\": \"hexalith-tenants\"");
    }

    [Fact]
    public static void DaprAccessControlShouldBeDenyByDefault()
    {
        string policy = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "DaprComponents", "accesscontrol.yaml"));

        policy.ShouldContain("defaultAction: deny");
        policy.ShouldNotContain("defaultAction: allow");
        policy.ShouldContain("appId: chatbot");
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
