using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ProblemDetailsContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void ProblemDetailsShouldExposeRfcAndHexalithMetadataFields()
    {
        YamlMappingNode schema = Schema("ProblemDetails");
        string[] properties = Mapping(schema, "properties").Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value.ShouldNotBeNull()).ToArray();

        foreach (string property in new[]
        {
            "type",
            "title",
            "status",
            "detail",
            "instance",
            "category",
            "code",
            "message",
            "correlationId",
            "taskId",
            "retryable",
            "clientAction",
            "details",
        })
        {
            properties.ShouldContain(property);
        }
    }

    [Fact]
    public static void ProblemDetailsDetailsShouldExposeVisibilityOnlyAsRequiredNestedMetadata()
    {
        YamlMappingNode schema = Schema("ProblemDetailsDetails");
        string[] required = Sequence(schema, "required").Children.OfType<YamlScalarNode>().Select(static value => value.Value.ShouldNotBeNull()).ToArray();
        string[] properties = Mapping(schema, "properties").Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value.ShouldNotBeNull()).ToArray();

        required.ShouldContain("visibility");
        properties.ShouldBe(["visibility"], ignoreOrder: false);
        Scalar(schema, "additionalProperties").ShouldBe("false");
    }

    [Fact]
    public static void ProblemExamplesShouldRemainSyntheticAndMetadataOnly()
    {
        string text = File.ReadAllText(ContractPath);

        text.ShouldContain("synthetic");
        text.ShouldNotContain("restricted project", Case.Insensitive);
        text.ShouldNotContain("candidate evidence", Case.Insensitive);
        text.ShouldNotContain("audit detail", Case.Insensitive);
        text.ShouldNotContain("payload", Case.Insensitive);
        text.ShouldNotContain("secret", Case.Insensitive);
        text.ShouldNotContain("/home/", Case.Insensitive);
        text.ShouldNotContain("C:\\", Case.Insensitive);
    }

    private static YamlMappingNode Schema(string name)
        => Mapping(Mapping(LoadContract(), "components"), "schemas").Children[new YamlScalarNode(name)].ShouldBeOfType<YamlMappingNode>();

    private static YamlMappingNode LoadContract()
    {
        using StringReader reader = new(File.ReadAllText(ContractPath));
        YamlStream stream = new();
        stream.Load(reader);
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Mapping(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlMappingNode>();
    }

    private static YamlSequenceNode Sequence(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlSequenceNode>();
    }

    private static string Scalar(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlScalarNode>().Value.ShouldNotBeNull();
    }

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
