using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ProjectConversationContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void ProjectConversationDtoShouldSerializeMetadataOnlyWireTokens()
    {
        ProjectConversationResponse response = new(
            "project-001",
            "Authorized Project",
            null,
            ProjectConversationReadStatus.Current,
            LifecycleState.Associated,
            [
                new ProjectConversationItem(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    ProjectConversationItemKind.EmailDerived,
                    ProjectConversationActorKind.Mailbox,
                    "Mailbox event",
                    new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                    LifecycleState.Associated,
                    AssociationThresholdBand.Auto,
                    0.91,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    "controlled-mailbox-001",
                    "conversation-001",
                    "thread-001",
                    "m365-mailbox-intake",
                    "metadata_only",
                    "collaboration_input",
                    "chatbot.project-conversation-item.v1",
                    4,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    ProjectId: "project-001",
                    ProjectDisplayName: "Authorized Project"),
            ],
            new ProjectConversationCursorPage("opaque-cursor", true, 25),
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-response.v1",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            "none");

        string json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"status\":\"current\"");
        json.ShouldContain("\"kind\":\"email-derived\"");
        json.ShouldContain("\"actorKind\":\"mailbox\"");
        json.ShouldContain("\"thresholdBand\":\"auto\"");
        json.ShouldNotContain("EmailDerived", Case.Sensitive);
        json.ShouldNotContain("MailboxMessageBody", Case.Sensitive);
        json.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public static void ProjectConversationOpenApiShouldDeclareCursorPaginationAndMetadataOnlyFields()
    {
        YamlMappingNode root = LoadContract();
        YamlMappingNode operation = Mapping(Mapping(Mapping(root, "paths"), "/api/v1/projects/{projectId}/conversation"), "get");
        Scalar(operation, "operationId").ShouldBe("GetProjectConversation");
        Mapping(operation, "responses").Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("200");

        YamlMappingNode schemas = Mapping(Mapping(root, "components"), "schemas");
        Sequence(Mapping(schemas, "ProjectConversationReadStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["current", "empty", "stale", "degraded", "blocked"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ProjectConversationActorKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("system-decision");
        Sequence(Mapping(schemas, "ProjectConversationResponse"), "required").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("page");
    }

    [Fact]
    public static void ProjectConversationEnumsShouldHaveStableWireTokens()
    {
        WireValue(ProjectConversationReadStatus.Blocked).ShouldBe("blocked");
        WireValue(ProjectConversationItemKind.SystemDecision).ShouldBe("system-decision");
        WireValue(ProjectConversationActorKind.Mailbox).ShouldBe("mailbox");
    }

    private static string WireValue<T>(T value)
        where T : struct, Enum
        => typeof(T)
            .GetField(value.ToString())
            ?.GetCustomAttribute<EnumMemberAttribute>()
            ?.Value
            ?? value.ToString();

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
