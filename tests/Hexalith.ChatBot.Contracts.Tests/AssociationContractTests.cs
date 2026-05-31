using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class AssociationContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void AssociationWorkflowIdShouldBeUlidOnly()
    {
        AssociationWorkflowId.TryParse("01ARZ3NDEKTSV4RRFFQ69G5FAV", out AssociationWorkflowId id).ShouldBeTrue();
        id.Value.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        AssociationWorkflowId.TryParse(Guid.NewGuid().ToString(), out _).ShouldBeFalse();
    }

    [Fact]
    public static void AssociationResultShouldSerializeCamelCaseWithoutRawPii()
    {
        AssociationScoringResult result = new(
            0.9,
            AssociationThresholdBand.Auto,
            AssociationScoringOutcome.AutoAssociated,
            [AssociationReasonCode.ExplicitProjectIdentifierMatched],
            "association-deterministic.kernel.m0.v1",
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            "controlled-mailbox-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "conversation-001",
            "thread-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "metadata_only",
            "collaboration_input",
            "chatbot.association-scoring-result.v1");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"confidenceScore\"");
        json.ShouldContain("\"kernelVersion\"");
        json.ShouldNotContain("ConfidenceScore", Case.Sensitive);
        json.ShouldNotContain("sender@example.test", Case.Insensitive);
        json.ShouldNotContain("Project Alpha", Case.Sensitive);
    }

    [Fact]
    public static void AssociationReasonCodesShouldHaveStableWireTokens()
    {
        WireValue(AssociationReasonCode.ExplicitProjectIdentifierMatched).ShouldBe("explicit-project-identifier-matched");
        WireValue(AssociationReasonCode.UnauthorizedCandidateSuppressed).ShouldBe("unauthorized-candidate-suppressed");
        WireValue(AssociationThresholdBand.FailClosed).ShouldBe("fail-closed");
    }

    [Fact]
    public static void OpenApiShouldDeclareAssociationRequiredFieldsAndEnums()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode command = Mapping(schemas, nameof(ScoreMailboxMessageAssociation));

        Sequence(command, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(
                [
                    "associationId",
                    "intakeId",
                    "sourceMailboxId",
                    "sourceConversationId",
                    "deterministicSignals",
                    "thresholdPolicy",
                    "candidates",
                    "exclusions",
                    "result",
                    "scoringKernelVersion",
                ],
                ignoreOrder: false);

        Sequence(Mapping(schemas, nameof(AssociationThresholdBand)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["auto", "ambiguous", "fail-closed"], ignoreOrder: false);
        Sequence(Mapping(schemas, nameof(AssociationReasonCode)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("authorization-evidence-unavailable");
    }

    private static string WireValue<T>(T enumValue)
        where T : struct, Enum
    {
        MemberInfo member = typeof(T).GetMember(enumValue.ToString()).Single();
        string? wireValue = member.GetCustomAttribute<EnumMemberAttribute>()?.Value;
        wireValue.ShouldNotBeNull();
        return wireValue;
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

    private static YamlSequenceNode Sequence(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlSequenceNode>();
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
