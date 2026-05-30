using System.Text.Json;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Conformance.Tests;

public static class ContractSpineOracleTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");
    private static readonly string OraclePath = Path.Combine(RepositoryRoot, "tests", "fixtures", "story-1-2-contract-spine-oracle.json");

    [Fact]
    public static void StoryTwelveOracleShouldTrackCurrentCommandSubmissionContract()
    {
        using JsonDocument oracle = JsonDocument.Parse(File.ReadAllText(OraclePath));
        YamlMappingNode root = LoadContract();
        JsonElement operationOracle = oracle.RootElement.GetProperty("operation");

        oracle.RootElement.GetProperty("story").GetString().ShouldBe("1.2");
        operationOracle.GetProperty("path").GetString().ShouldBe("/api/v1/commands");
        operationOracle.GetProperty("method").GetString().ShouldBe("post");
        operationOracle.GetProperty("successStatus").GetInt32().ShouldBe(202);

        YamlMappingNode operation = Mapping(Mapping(Mapping(root, "paths"), operationOracle.GetProperty("path").GetString().ShouldNotBeNull()), "post");
        Scalar(operation, "operationId").ShouldBe(operationOracle.GetProperty("operationId").GetString());

        YamlMappingNode extension = Mapping(operation, "x-hexalith-command-submission");
        JsonElement adapterInputShape = oracle.RootElement.GetProperty("adapterInputShape");
        Scalar(extension, "adapterContract").ShouldBe(adapterInputShape.GetProperty("adapterContract").GetString());
        Scalar(extension, "commandMarker").ShouldBe(adapterInputShape.GetProperty("commandMarker").GetString());

        YamlMappingNode requestSchema = Mapping(Mapping(Mapping(root, "components"), "schemas"), adapterInputShape.GetProperty("requestSchema").GetString().ShouldNotBeNull());
        string[] requestRequired = Sequence(requestSchema, "required").Children.OfType<YamlScalarNode>().Select(static node => node.Value.ShouldNotBeNull()).ToArray();
        string[] requestProperties = Mapping(requestSchema, "properties").Children.Keys.OfType<YamlScalarNode>().Select(static node => node.Value.ShouldNotBeNull()).ToArray();

        string[] oracleRequired = adapterInputShape.GetProperty("requiredFields").EnumerateArray().Select(static field => field.GetString().ShouldNotBeNull()).ToArray();
        requestRequired.ShouldBe(oracleRequired, ignoreOrder: false);

        foreach (string forbidden in adapterInputShape.GetProperty("forbiddenAuthorityFields").EnumerateArray().Select(static field => field.GetString().ShouldNotBeNull()))
        {
            requestProperties.ShouldNotContain(forbidden);
        }
    }

    [Fact]
    public static void StoryTwelveOracleShouldTrackMetadataOnlyFailureCategories()
    {
        using JsonDocument oracle = JsonDocument.Parse(File.ReadAllText(OraclePath));
        YamlMappingNode root = LoadContract();
        YamlMappingNode operation = Mapping(Mapping(Mapping(root, "paths"), "/api/v1/commands"), "post");

        string[] oracleCategories = oracle.RootElement.GetProperty("metadataOnlyFailureCategories")
            .EnumerateArray()
            .Select(static category => category.GetString().ShouldNotBeNull())
            .ToArray();

        string[] extensionCategories = Sequence(operation, "x-hexalith-canonical-error-categories")
            .Children
            .OfType<YamlScalarNode>()
            .Select(static category => category.Value.ShouldNotBeNull())
            .ToArray();

        string[] problemCategories = Sequence(Mapping(Mapping(Mapping(Mapping(root, "components"), "schemas"), "ProblemDetails"), "properties", "category"), "enum")
            .Children
            .OfType<YamlScalarNode>()
            .Select(static category => category.Value.ShouldNotBeNull())
            .ToArray();

        extensionCategories.ShouldBe(oracleCategories, ignoreOrder: false);
        problemCategories.ShouldBe(oracleCategories, ignoreOrder: false);
    }

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

    private static YamlMappingNode Mapping(YamlMappingNode node, string firstKey, string secondKey)
        => Mapping(Mapping(node, firstKey), secondKey);

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
