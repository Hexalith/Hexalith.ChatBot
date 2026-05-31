using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static partial class SharedContractTypeTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();

    [Fact]
    public static void ChatBotCommandMarkerShouldBeBehaviorFree()
    {
        typeof(IChatBotCommand).IsInterface.ShouldBeTrue();
        typeof(IChatBotCommand).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ShouldBeEmpty();
    }

    [Fact]
    public static void ContractEnumsShouldExposeStableWireNames()
    {
        AssertEnumWireNames<LifecycleState>([
            "Received",
            "Proposed",
            "Associated",
            "Rejected",
            "Deferred",
            "NeedsReview",
            "Failed",
            "Skipped",
            "Corrected",
            "Correcting",
            "Correction-delayed",
        ]);
        AssertEnumWireNames<ChatBotHealthStatus>(["healthy", "degraded", "failed", "unknown"]);
        AssertEnumWireNames<RiskClass>(["none", "low", "medium", "high", "blocked"]);
        AssertEnumWireNames<ActorType>(["human", "ai", "service", "system"]);
        AssertEnumWireNames<ThresholdBand>(["below", "within", "above", "critical"]);
        AssertEnumWireNames<ParticipantResolutionStatus>(["resolved", "unresolved", "rejected", "quarantined", "blocked"]);
        AssertEnumWireNames<ParticipantReviewAction>(["link", "create-pending", "reject", "quarantine"]);
        AssertEnumWireNames<ParticipantResolutionBlockedReason>([
            "not-found",
            "ambiguous-match",
            "restricted-party",
            "erased-party",
            "tenant-mismatch",
            "directory-degraded",
            "directory-unavailable",
            "invalid-evidence",
            "unauthorized-actor",
            "unresolved-participant",
        ]);
    }

    [Fact]
    public static void IdentityHelpersShouldValidateUlidsAndRejectInvalidValues()
    {
        const string validUlid = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

        ChatBotIdentity.IsValidUlid(validUlid).ShouldBeTrue();
        ChatBotIdentity.IsValidUlid(Guid.NewGuid().ToString()).ShouldBeFalse();
        ChatBotIdentity.IsValidUlid("/tmp/sensitive-correlation").ShouldBeFalse();
        ChatBotCommandId.TryParse(validUlid, out ChatBotCommandId commandId).ShouldBeTrue();
        ChatBotCorrelationId.TryParse(validUlid, out ChatBotCorrelationId correlationId).ShouldBeTrue();
        ChatBotTaskId.TryParse(validUlid, out ChatBotTaskId taskId).ShouldBeTrue();
        ParticipantResolutionId.TryParse(validUlid, out ParticipantResolutionId resolutionId).ShouldBeTrue();

        commandId.Value.ShouldBe(validUlid);
        correlationId.Value.ShouldBe(validUlid);
        taskId.Value.ShouldBe(validUlid);
        resolutionId.Value.ShouldBe(validUlid);
    }

    [Fact]
    public static void IdentityHelperSourceShouldUseUlidTryParseAndNeverGuidTryParse()
    {
        string[] identitySources = Directory.GetFiles(Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "Identities"), "*.cs", SearchOption.AllDirectories);
        identitySources.ShouldNotBeEmpty();

        string combined = string.Join('\n', identitySources.Select(File.ReadAllText));
        combined.ShouldContain("Ulid.TryParse");
        combined.ShouldNotContain("Guid.TryParse");
    }

    [Fact]
    public static void HandWrittenCommandNamesShouldBeImperativeWithoutCommandSuffix()
    {
        string[] commandTypeNames = typeof(IChatBotCommand).Assembly
            .GetTypes()
            .Where(static type => typeof(IChatBotCommand).IsAssignableFrom(type) && type.IsClass)
            .Select(static type => type.Name)
            .ToArray();

        commandTypeNames.ShouldAllBe(static name => !name.EndsWith("Command", StringComparison.Ordinal));
        commandTypeNames.ShouldAllBe(static name => ImperativeNamePattern().IsMatch(name));
    }

    [Fact]
    public static void ContractSpineShouldRejectCommandTypeSuffix()
    {
        string contract = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml"));

        contract.ShouldContain("Command$");
    }

    [Fact]
    public static void HandWrittenEventAndRejectionNamesShouldFollowContractNamingRules()
    {
        string eventsPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "Events");
        string[] eventTypeNames = Directory.Exists(eventsPath) ? PublicTypeNames(eventsPath) : [];

        foreach (string eventTypeName in eventTypeNames.Where(static name => !name.StartsWith('I')))
        {
            eventTypeName.ShouldNotEndWith("Event");
            if (eventTypeName.EndsWith("Rejection", StringComparison.Ordinal))
            {
                TargetReasonRejectionPattern().IsMatch(eventTypeName).ShouldBeTrue(eventTypeName);
            }
            else
            {
                PastTenseEventPattern().IsMatch(eventTypeName).ShouldBeTrue(eventTypeName);
            }
        }
    }

    private static void AssertEnumWireNames<TEnum>(string[] expected)
        where TEnum : struct, Enum
    {
        string[] actual = typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(static field => field.GetCustomAttribute<EnumMemberAttribute>()?.Value)
            .Select(static value => value.ShouldNotBeNull())
            .ToArray();

        actual.ShouldBe(expected, ignoreOrder: false);
    }

    private static string[] PublicTypeNames(string path)
        => Directory
            .GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .SelectMany(static file => PublicTypePattern().Matches(File.ReadAllText(file)).Select(static match => match.Groups["name"].Value))
            .ToArray();

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    [GeneratedRegex(@"\bpublic\s+(?:sealed\s+|readonly\s+|partial\s+|static\s+)*(?:record\s+struct|record|class|struct|interface|enum)\s+(?<name>[A-Za-z][A-Za-z0-9_]*)")]
    private static partial Regex PublicTypePattern();

    [GeneratedRegex("^(Submit|Start|Stop|Cancel|Approve|Reject|Record|Capture|Create|Update|Delete|Archive|Assign|Resolve|Request|Configure|Score|Set)[A-Za-z0-9]*$")]
    private static partial Regex ImperativeNamePattern();

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*(ed|en|nt|lt)$")]
    private static partial Regex PastTenseEventPattern();

    [GeneratedRegex("^[A-Z][A-Za-z0-9]+[A-Z][A-Za-z0-9]+Rejection$")]
    private static partial Regex TargetReasonRejectionPattern();
}
