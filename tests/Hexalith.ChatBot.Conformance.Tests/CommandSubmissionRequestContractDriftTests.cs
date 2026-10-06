using System.Text;
using System.Text.Json.Nodes;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;

using Microsoft.AspNetCore.Http;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Binds the server request adapter's restated validation rules to the OpenAPI <c>CommandSubmissionRequest</c> schema
/// (AD-18). The adapter hand-copies the property list, the <c>commandType</c> bounds and pattern, the schema version,
/// and the closed metadata enums for fast pre-admission validation; each copy must match the YAML exactly so a change
/// on either side fails here instead of silently diverging from the published contract.
/// </summary>
public static class CommandSubmissionRequestContractDriftTests
{
    private static readonly string ContractPath = Path.Combine(
        LocateRepositoryRoot(), "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void AdapterPropertyListShouldMatchTheClosedRequestSchema()
    {
        YamlMappingNode request = RequestSchema();

        Scalar(request, "additionalProperties").ShouldBe("false");
        Keys(Mapping(request, "properties")).Order(StringComparer.Ordinal)
            .ShouldBe(CommandSubmissionContractAdapter.RequestProperties.Order(StringComparer.Ordinal));
        Values(Sequence(request, "required"))
            .ShouldBe(CommandSubmissionContractAdapter.RequiredRequestProperties, ignoreOrder: false);
    }

    [Fact]
    public static void AdapterCommandTypeRulesShouldMatchTheRequestSchema()
    {
        YamlMappingNode commandType = Mapping(Mapping(RequestSchema(), "properties"), "commandType");

        Scalar(commandType, "type").ShouldBe("string");
        int.Parse(Scalar(commandType, "maxLength"), System.Globalization.CultureInfo.InvariantCulture)
            .ShouldBe(CommandSubmissionContractAdapter.CommandTypeMaxLength);
        Scalar(commandType, "pattern").ShouldBe(CommandSubmissionContractAdapter.CommandTypePatternText);
        Scalar(Mapping(commandType, "not"), "pattern")
            .ShouldBe(CommandSubmissionContractAdapter.ForbiddenCommandTypeSuffix + "$");
    }

    [Fact]
    public static void AdapterSchemaVersionShouldMatchTheRequestSchemaEnum()
    {
        YamlMappingNode version = Mapping(Mapping(RequestSchema(), "properties"), "requestSchemaVersion");

        Values(Sequence(version, "enum")).ShouldBe([CommandSubmissionContractAdapter.RequestSchemaVersion]);
    }

    [Theory]
    [InlineData("actorType")]
    [InlineData("riskClass")]
    [InlineData("thresholdBand")]
    [InlineData("origin")]
    public static void AdapterMetadataEnumsShouldMatchTheReferencedSchemaEnums(string property)
    {
        YamlMappingNode root = LoadContract();
        YamlMappingNode schemas = Mapping(Mapping(root, "components"), "schemas");
        string reference = Scalar(Mapping(Mapping(Mapping(schemas, "CommandSubmissionRequest"), "properties"), property), "$ref");
        const string prefix = "#/components/schemas/";
        reference.ShouldStartWith(prefix);
        YamlMappingNode referenced = Mapping(schemas, reference[prefix.Length..]);
        IReadOnlySet<string> adapterValues = property switch
        {
            "actorType" => CommandSubmissionContractAdapter.ActorTypes,
            "riskClass" => CommandSubmissionContractAdapter.RiskClasses,
            "thresholdBand" => CommandSubmissionContractAdapter.ThresholdBands,
            _ => CommandSubmissionContractAdapter.SurfaceOrigins,
        };

        Values(Sequence(referenced, "enum")).Order(StringComparer.Ordinal)
            .ShouldBe(adapterValues.Order(StringComparer.Ordinal));
    }

    /// <summary>Every root member the YAML schema requires, read from the schema rather than the adapter.</summary>
    public static TheoryData<string> RequiredRootProperties()
        => new(Values(Sequence(RequestSchema(), "required")));

    /// <summary>Every root member the YAML schema declares but does not require.</summary>
    public static TheoryData<string> OptionalRootProperties()
    {
        YamlMappingNode request = RequestSchema();
        string[] required = Values(Sequence(request, "required"));
        return new(Keys(Mapping(request, "properties")).Where(name => !required.Contains(name, StringComparer.Ordinal)).ToArray());
    }

    [Fact]
    public static async Task ABodyCarryingEveryDeclaredRootPropertyShouldParse()
        => (await ReadAsync(ValidBodyWithEveryDeclaredProperty()).ConfigureAwait(true)).Request.ShouldNotBeNull();

    [Theory]
    [MemberData(nameof(RequiredRootProperties))]
    public static async Task OmittingARequiredRootPropertyShouldBeRejectedBeforeAdmission(string property)
    {
        JsonObject body = ValidBodyWithEveryDeclaredProperty();
        body.Remove(property).ShouldBeTrue(property);

        (CommandSubmissionRequest? request, string? origin) = await ReadAsync(body).ConfigureAwait(true);

        request.ShouldBeNull($"The adapter accepted a body without the required '{property}' member.");
        origin.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(OptionalRootProperties))]
    public static async Task OmittingAnOptionalRootPropertyShouldStillParse(string property)
    {
        JsonObject body = ValidBodyWithEveryDeclaredProperty();
        body.Remove(property).ShouldBeTrue(property);

        (CommandSubmissionRequest? request, _) = await ReadAsync(body).ConfigureAwait(true);

        request.ShouldNotBeNull($"The adapter rejected a body without the optional '{property}' member.");
    }

    /// <summary>
    /// Builds a contract-valid body containing every root property the YAML declares; enum members take their first
    /// published value. A newly declared member without a sample here fails the build of this body.
    /// </summary>
    private static JsonObject ValidBodyWithEveryDeclaredProperty()
    {
        YamlMappingNode root = LoadContract();
        YamlMappingNode schemas = Mapping(Mapping(root, "components"), "schemas");
        YamlMappingNode properties = Mapping(Mapping(schemas, "CommandSubmissionRequest"), "properties");
        JsonObject body = [];
        foreach (string name in Keys(properties))
        {
            body[name] = name switch
            {
                "commandId" => "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                "commandType" => "RecordGovernedNote",
                "command" => new JsonObject { ["noteId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAW" },
                _ => FirstEnumValue(schemas, Mapping(properties, name), name),
            };
        }

        return body;
    }

    private static string FirstEnumValue(YamlMappingNode schemas, YamlMappingNode property, string name)
    {
        YamlMappingNode schema = property.Children.ContainsKey(new YamlScalarNode("$ref"))
            ? Mapping(schemas, Scalar(property, "$ref")["#/components/schemas/".Length..])
            : property;
        schema.Children.ContainsKey(new YamlScalarNode("enum"))
            .ShouldBeTrue($"Add a contract-valid sample for the newly declared '{name}' request member.");
        return Values(Sequence(schema, "enum"))[0];
    }

    private static async Task<(CommandSubmissionRequest? Request, string? Origin)> ReadAsync(JsonObject body)
    {
        DefaultHttpContext context = new();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body.ToJsonString()));
        return await CommandSubmissionContractAdapter.ReadAsync(context, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static YamlMappingNode RequestSchema()
        => Mapping(Mapping(Mapping(LoadContract(), "components"), "schemas"), "CommandSubmissionRequest");

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

    private static string[] Keys(YamlMappingNode node)
        => node.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value.ShouldNotBeNull()).ToArray();

    private static string[] Values(YamlSequenceNode node)
        => node.Children.OfType<YamlScalarNode>().Select(static item => item.Value.ShouldNotBeNull()).ToArray();

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
