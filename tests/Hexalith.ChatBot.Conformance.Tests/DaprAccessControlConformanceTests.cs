using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

public static class DaprAccessControlConformanceTests
{
    [Fact]
    public static void ChatBotAccessControlMustBeDenyByDefaultAndNotCopyFoldersAllowPolicy()
    {
        string policy = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.yaml"));

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
