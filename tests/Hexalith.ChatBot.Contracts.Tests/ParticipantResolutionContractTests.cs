using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ParticipantResolutionContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void ParticipantResolutionIdShouldBeUlidOnly()
    {
        const string validUlid = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

        ParticipantResolutionId.TryParse(validUlid, out ParticipantResolutionId resolutionId).ShouldBeTrue();
        resolutionId.Value.ShouldBe(validUlid);
        ParticipantResolutionId.TryParse(Guid.NewGuid().ToString(), out _).ShouldBeFalse();
    }

    [Fact]
    public static void ResolutionCommandShouldSerializeCamelCaseAndKeepPartyIdStable()
    {
        ResolveMailboxMessageParticipants command = ValidCommand();

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"resolutionId\"");
        json.ShouldContain("\"partyId\":\"tenant-alpha:parties:party-001\"");
        json.ShouldContain("\"addressEvidence\":\"sender@example.test\"");
        json.ShouldNotContain("ResolutionId", Case.Sensitive);
        json.ShouldNotContain("Guid", Case.Sensitive);
    }

    [Fact]
    public static void OpenApiShouldDeclareResolutionRequiredFieldsAndEnums()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode command = Mapping(schemas, nameof(ResolveMailboxMessageParticipants));

        Sequence(command, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(
                ["resolutionId", "intakeId", "sourceMailboxId", "sourceParticipants", "resolvedParticipants", "unresolvedParticipants", "resolutionKernelVersion"],
                ignoreOrder: false);

        Sequence(Mapping(schemas, nameof(ParticipantResolutionStatus)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("resolved");
        Sequence(Mapping(schemas, nameof(ParticipantResolutionBlockedReason)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("directory-unavailable");
        Sequence(Mapping(schemas, nameof(ParticipantReviewAction)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["link", "create-pending", "reject", "quarantine"], ignoreOrder: false);
    }

    private static ResolveMailboxMessageParticipants ValidCommand()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            [new MailboxParticipantSourceReference("01ARZ3NDEKTSV4RRFFQ69G5FAZ", "sender", "mailbox:intake:sender", "evidence-sha256", "sender@example.test", "Sender")],
            [new ResolvedMailboxParticipantReference("01ARZ3NDEKTSV4RRFFQ69G5FAZ", "tenant-alpha:parties:party-001", "tenant-alpha", "mailbox:intake:sender", "evidence-sha256", ParticipantResolutionStatus.Resolved)],
            [],
            "participant-resolution.kernel.v1");

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
