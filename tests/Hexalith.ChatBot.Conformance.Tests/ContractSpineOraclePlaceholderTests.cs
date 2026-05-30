using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

public static class ContractSpineOraclePlaceholderTests
{
    [Fact]
    public static void StoryTwelveConformancePlaceholderShouldExist()
    {
        string fixture = Path.Combine(RepositoryRoot(), "tests", "fixtures", "story-1-2-contract-spine-oracle.placeholder.json");

        File.Exists(fixture).ShouldBeTrue();
        File.ReadAllText(fixture).ShouldContain("\"story\": \"1.2\"");
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
