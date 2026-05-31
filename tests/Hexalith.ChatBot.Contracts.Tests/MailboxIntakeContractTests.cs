using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class MailboxIntakeContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void MailboxIntakeIdShouldBeUlidOnly()
    {
        const string validUlid = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

        MailboxMessageIntakeId.TryParse(validUlid, out MailboxMessageIntakeId intakeId).ShouldBeTrue();
        intakeId.Value.ShouldBe(validUlid);
        MailboxMessageIntakeId.TryParse(Guid.NewGuid().ToString(), out _).ShouldBeFalse();
    }

    [Fact]
    public static void MailboxIntakeCommandShouldSerializeCamelCaseWithUtcTimestamps()
    {
        CaptureMailboxMessageIntake command = ValidCommand();

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"intakeId\"");
        json.ShouldContain("\"providerMessageId\"");
        json.ShouldContain("\"receivedAt\":\"2026-05-30T10:15:00+00:00\"");
        json.ShouldNotContain("IntakeId", Case.Sensitive);
        json.ShouldNotContain("Guid");
    }

    [Fact]
    public static void OpenApiShouldDeclareMailboxIntakeRequiredFields()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode command = Mapping(schemas, nameof(CaptureMailboxMessageIntake));

        Sequence(command, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["intakeId", "source", "recipients", "attachments"], ignoreOrder: false);

        YamlMappingNode source = Mapping(schemas, nameof(MailboxMessageSourceIdentity));
        string[] sourceRequired = Sequence(source, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ToArray();
        foreach (string expected in new[]
                 {
                     "providerMessageId",
                     "internetMessageId",
                     "conversationId",
                     "mailboxId",
                     "sender",
                     "receivedAt",
                     "sourceContext",
                     "sourceSchemaVersion",
                 })
        {
            sourceRequired.ShouldContain(expected);
        }
    }

    private static CaptureMailboxMessageIntake ValidCommand()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            new MailboxMessageSourceIdentity(
                "graph-message-001",
                "<message-001@example.test>",
                "graph-conversation-001",
                "graph-thread-001",
                "controlled-mailbox-001",
                new MailboxParticipantIdentity("sender@example.test", "Sender"),
                new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.Zero),
                null,
                "W. Europe Standard Time",
                "graph-message-v1",
                1),
            [new MailboxRecipientIdentity("project@example.test", "Project", "to")],
            [new MailboxAttachmentReference("attachment-001", "evidence.pdf", "application/pdf", 1024)]);

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
