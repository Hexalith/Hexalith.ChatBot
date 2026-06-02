using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
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
    public static void MailboxIntakeCommandShouldSerializeAuthenticityMetadataOnly()
    {
        CaptureMailboxMessageIntake command = ValidCommand() with
        {
            Authenticity = AuthenticityMetadata(),
        };

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"spf\":\"pass\"");
        json.ShouldContain("\"dkim\":\"fail\"");
        json.ShouldContain("\"compositeAuthentication\":\"bestguesspass\"");
        json.ShouldContain("\"discrepancies\":[\"from-sender-mismatch\"]");
        json.ShouldContain("\"valueState\":\"supplied\"");
        json.ShouldNotContain("Authentication-Results: spf=pass", Case.Insensitive);
        json.ShouldNotContain("raw provider payload", Case.Insensitive);
        json.ShouldNotContain("body", Case.Insensitive);
    }

    [Fact]
    public static void MailboxIntakeCommandShouldSerializeDelegatedAndExternalPostureAsFiniteMetadata()
    {
        CaptureMailboxMessageIntake command = ValidCommand() with
        {
            Source = ValidCommand().Source with
            {
                Sender = new MailboxParticipantIdentity("delegate@example.test", "Delegate"),
                DelegatedSender = new MailboxDelegatedSenderSnapshot(
                    MailboxDelegatedSenderState.Delegated,
                    new MailboxParticipantIdentity("delegate@example.test", "Delegate"),
                    new MailboxParticipantIdentity("principal@example.test", "Principal"),
                    ["provider:sender", "provider:from"],
                    []),
                ExternalSender = new MailboxExternalSenderPosture(
                    ExternalSender: true,
                    MailboxPartyResolutionState.Unresolved,
                    ResolvedPartyRef: null,
                    ["external-sender:true", "party-resolution:unresolved"]),
            },
            Authenticity = AuthenticityMetadata() with
            {
                StrictnessPolicy = new MailboxAuthenticityStrictnessPolicySnapshot(
                    MailboxAuthenticityStrictness.Strict,
                    "policy-unavailable",
                    "policy-unavailable"),
            },
        };

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"delegatedSender\"");
        json.ShouldContain("\"state\":\"delegated\"");
        json.ShouldContain("\"principalFor\"");
        json.ShouldContain("\"externalSender\":true");
        json.ShouldContain("\"partyResolutionState\":\"unresolved\"");
        json.ShouldContain("\"strictness\":\"strict\"");
        json.ShouldNotContain("Delegated", Case.Sensitive);
        json.ShouldNotContain("raw provider payload", Case.Insensitive);
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

        command.Children[new YamlScalarNode("properties")]
            .ShouldBeOfType<YamlMappingNode>()
            .Children
            .ShouldContainKey(new YamlScalarNode("authenticity"));

        YamlMappingNode authenticity = Mapping(schemas, nameof(MailboxAuthenticityMetadata));
        Sequence(authenticity, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["authenticationResults", "headerInspection"], ignoreOrder: false);

        Mapping(schemas, nameof(MailboxDelegatedSenderSnapshot));
        Mapping(schemas, nameof(MailboxExternalSenderPosture));
        Mapping(schemas, nameof(MailboxAuthenticityStrictnessPolicySnapshot));
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

    private static MailboxAuthenticityMetadata AuthenticityMetadata()
        => new(
            new MailboxAuthenticationResultSnapshot(
                MailboxAuthenticationVerdictKind.Pass,
                MailboxAuthenticationVerdictKind.Fail,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.BestGuessPass,
                "109",
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Supplied)]),
            new MailboxHeaderInspectionSnapshot(
                [new MailboxSelectedHeaderSnapshot("Received", 0, MailboxHeaderValueState.Supplied)],
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Supplied)],
                MailboxHeaderValueState.Supplied,
                MailboxHeaderValueState.NotSupplied,
                MailboxHeaderValueState.Supplied,
                MailboxHeaderValueState.NotSupplied,
                [MailboxHeaderDiscrepancyKind.FromSenderMismatch]));

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
