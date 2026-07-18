using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Conformance.Tests;

public static class DaprAccessControlConformanceTests
{
    [Fact]
    public static void ChatBotAccessControlMustBeDenyByDefaultAndNotCopyFoldersAllowPolicy()
    {
        string policyPath = Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.yaml");
        using StreamReader reader = File.OpenText(policyPath);
        YamlStream yaml = new();
        yaml.Load(reader);

        YamlMappingNode root = (YamlMappingNode)yaml.Documents.Single().RootNode;
        YamlMappingNode accessControl = Mapping(Mapping(root, "spec"), "accessControl");
        Scalar(accessControl, "defaultAction").ShouldBe("deny");

        YamlSequenceNode policies = Sequence(accessControl, "policies");
        YamlMappingNode eventStore = policies.Children.Cast<YamlMappingNode>()
            .Single(policy => Scalar(policy, "appId") == "eventstore");
        Scalar(eventStore, "defaultAction").ShouldBe("deny");
        YamlSequenceNode operations = Sequence(eventStore, "operations");
        operations.Children.Count.ShouldBe(1);
        YamlMappingNode operation = (YamlMappingNode)operations.Children.Single();
        Scalar(operation, "name").ShouldBe("/process");
        Scalar(operation, "action").ShouldBe("allow");
        Sequence(operation, "httpVerb").Children.Cast<YamlScalarNode>()
            .Select(static verb => verb.Value)
            .ShouldBe(["POST"]);

        YamlMappingNode chatBot = policies.Children.Cast<YamlMappingNode>()
            .Single(policy => Scalar(policy, "appId") == "chatbot");
        Scalar(chatBot, "defaultAction").ShouldBe("deny");
        Sequence(chatBot, "operations").Children.ShouldBeEmpty();
    }

    private static YamlMappingNode Mapping(YamlMappingNode parent, string key)
        => (YamlMappingNode)parent.Children[new YamlScalarNode(key)];

    private static YamlSequenceNode Sequence(YamlMappingNode parent, string key)
        => (YamlSequenceNode)parent.Children[new YamlScalarNode(key)];

    private static string? Scalar(YamlMappingNode parent, string key)
        => ((YamlScalarNode)parent.Children[new YamlScalarNode(key)]).Value;

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
