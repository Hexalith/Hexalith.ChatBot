using System.Text.RegularExpressions;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests.Fitness;

/// <summary>Guards closed admission mappings and the single command spine.</summary>
public sealed class AdmissionProfileFitnessTests
{
    /// <summary>Verifies every Concrete Command Should Have Exactly One Closed Profile.</summary>
    [Fact]
    public void EveryConcreteCommandShouldHaveExactlyOneClosedProfile()
    {
        ChatBotCommandAdmissionCatalog catalog = new();
        string[] contracts = typeof(IChatBotCommand).Assembly.GetTypes()
            .Where(type => typeof(IChatBotCommand).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            .Select(type => type.Name).Order(StringComparer.Ordinal).ToArray();
        catalog.Commands.Keys.Order(StringComparer.Ordinal).ShouldBe(contracts);
        Enum.GetValues<ChatBotAdmissionProfile>().Length.ShouldBe(10);
        catalog.Commands.Values.ShouldAllBe(value => Enum.IsDefined(value));
        ChatBotCommandAdmissionCatalog.StableConcreteContracts.Keys.Order(StringComparer.Ordinal).ShouldBe(ChatBotCommandAdmissionCatalog.StableCommands.Keys.Order(StringComparer.Ordinal));
        foreach (KeyValuePair<string, IReadOnlyList<string>> stable in ChatBotCommandAdmissionCatalog.StableConcreteContracts)
        {
            stable.Value.Distinct(StringComparer.Ordinal).Count().ShouldBe(stable.Value.Count);
            foreach (string concrete in stable.Value) { catalog.Find(concrete).ShouldBe(ChatBotCommandAdmissionCatalog.StableCommands[stable.Key]); }
        }
        // These existing DTOs lack a stable product row. Freeze this exact debt until the PRD maps or denies it;
        // Story 1.8 must resolve it before atomic participation can enable writes.
        string[] legacyUnmapped =
        [
            nameof(DecideOutboundApproval), nameof(MarkAiActionProposalInvalidatedByCorrection), nameof(RecordGovernedNote),
            nameof(RecordProjectConversationMessage), nameof(RequestComplianceEscalation), nameof(RequestComplianceInvestigation),
            nameof(RequestOutboundSendApproval), nameof(SubmitConsentLawfulBasisRecord), nameof(SubmitDataClassInventoryChange),
            nameof(SubmitEscalationPolicyChange),
        ];
        catalog.Commands.Keys.Except(ChatBotCommandAdmissionCatalog.StableConcreteContracts.Values.SelectMany(value => value), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ShouldBe(legacyUnmapped.Order(StringComparer.Ordinal));
        // The only contracts with two revision dimensions must reach their exact approval validators.
        typeof(IChatBotCommand).Assembly.GetTypes()
            .Where(type => typeof(IChatBotCommand).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            .Where(type => type.GetProperties().Count(property => property.Name.StartsWith("Expected", StringComparison.Ordinal) &&
                (property.Name.EndsWith("Version", StringComparison.Ordinal) || property.Name.EndsWith("Revision", StringComparison.Ordinal))) > 1)
            .Select(type => type.Name).Order(StringComparer.Ordinal)
            .ShouldBe(new[] { nameof(ExecuteApprovedAIAction), nameof(ExecuteApprovedOutboundDraft) }.Order(StringComparer.Ordinal));
        catalog.Find("unmapped").ShouldBeNull();
        catalog.Find("RetryCommandExecution").ShouldBeNull();
        catalog.Find(nameof(ExecuteLowRiskAIAssistance)).ShouldBe(ChatBotAdmissionProfile.AiRead);
        catalog.Find(nameof(ExecuteApprovedAIAction)).ShouldBe(ChatBotAdmissionProfile.AiEffect);
    }

    /// <summary>Exemptions remove only declared profile stages, and every stage remains applicable somewhere.</summary>
    [Fact]
    public void CommandStageApplicabilityShouldRemainBoundedAndNonVacuous()
    {
        ChatBotCommandAdmissionCatalog catalog = new();
        foreach (KeyValuePair<string, IReadOnlyList<string>> exemption in ChatBotCommandAdmissionCatalog.NotApplicableStages)
        {
            ChatBotAdmissionProfile profile = catalog.Find(exemption.Key).ShouldNotBeNull();
            exemption.Value.ShouldNotBeEmpty();
            exemption.Value.Distinct(StringComparer.Ordinal).Count().ShouldBe(exemption.Value.Count);
            exemption.Value.ShouldAllBe(stage => ChatBotCommandAdmissionCatalog.RequiredStages(profile).Contains(stage));
            ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(exemption.Key)
                .ShouldBe(ChatBotCommandAdmissionCatalog.RequiredStages(profile).Except(exemption.Value, StringComparer.Ordinal));
        }
        foreach (ChatBotAdmissionProfile profile in catalog.Commands.Values.Distinct())
        {
            foreach (string stage in ChatBotCommandAdmissionCatalog.RequiredStages(profile))
            {
                catalog.Commands.Where(row => row.Value == profile)
                    .ShouldContain(row => ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(row.Key).Contains(stage));
            }
        }
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(CorrectEmailProjectAssociation)).ShouldContain("correction-impact-manifest");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(ProposeAIAction)).ShouldNotContain("human-approval-lifetime-drift");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(CreateOutboundDraft)).ShouldNotContain("provider-evidence");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(ExecuteApprovedOutboundDraft)).ShouldContain("provider-evidence");
    }

    /// <summary>Verifies stable Product Map Should Remain Exhaustive And Unique.</summary>
    [Fact]
    public void StableProductMapShouldRemainExhaustiveAndUnique()
    {
        string addendum = File.ReadAllText(Path.Combine(Root(), "_bmad-output", "planning-artifacts", "prds", "prd-Hexalith.ChatBot-2026-05-28", "addendum.md"));
        string table = addendum.Split("#### Command-to-profile map", StringSplitOptions.None)[1]
            .Split("#### Transition and atomicity rules", StringSplitOptions.None)[0];
        string[] expected = table.Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal))
            .SelectMany(line => Regex.Matches(line.Split('|')[2], "`([^`]+)`").Select(match => match.Groups[1].Value)).ToArray();
        foreach (string row in table.Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal)))
        {
            string wireProfile = row.Split('|')[1].Trim().Trim('`');
            foreach (Match command in Regex.Matches(row.Split('|')[2], "`([^`]+)`"))
            {
                ChatBotCommandAdmissionCatalog.ProfileId(ChatBotCommandAdmissionCatalog.StableCommands[command.Groups[1].Value]).ShouldBe(wireProfile);
            }
        }
        expected.Distinct(StringComparer.Ordinal).Count().ShouldBe(expected.Length);
        ChatBotCommandAdmissionCatalog.StableCommands.Keys.Order(StringComparer.Ordinal).ShouldBe(expected.Order(StringComparer.Ordinal));
    }

    /// <summary>Verifies invalid Catalog Rows Should Fail At Construction.</summary>
    [Fact]
    public void InvalidCatalogRowsShouldFailAtConstruction()
    {
        KeyValuePair<string, ChatBotAdmissionProfile>[] rows = new ChatBotCommandAdmissionCatalog().Commands.ToArray();
        Should.Throw<InvalidOperationException>(() => new ChatBotCommandAdmissionCatalog(rows.Append(rows[0])));
        Should.Throw<InvalidOperationException>(() => new ChatBotCommandAdmissionCatalog(rows.Skip(1)));
        Should.Throw<InvalidOperationException>(() => new ChatBotCommandAdmissionCatalog(rows.Append(new("Unknown", ChatBotAdmissionProfile.Workflow))));
        Should.Throw<InvalidOperationException>(() => new ChatBotCommandAdmissionCatalog(rows.Skip(1).Append(new(rows[0].Key, (ChatBotAdmissionProfile)900))));
    }

    /// <summary>Verifies negative Fixtures Should Reject Competing Spines Stages Dispatch And Selectors.</summary>
    [Theory]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "internal class SecondaryAdmissionPipeline {}")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "internal partial class SecondaryAdmissionPipeline {}")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "internal record SecondaryAdmissionPipeline;")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "internal readonly record struct SecondaryAdmissionPipeline;")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "public partial class ChatBotDomainServiceAdmissionStage {}")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "public readonly struct ChatBotDomainServiceAdmissionStage {}")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "public sealed record ChatBotDomainServiceAdmissionStage;")]
    [InlineData("src/Hexalith.ChatBot.UI/Adapter.cs", "RiskClassification = new AiActionRiskClassificationRecord(...);")]
    [InlineData("src/Hexalith.ChatBot.UI/Adapter.cs", "AiActionRiskClassificationRecord record = new(...);")]
    [InlineData("src/Hexalith.ChatBot.UI/Composer.razor", "@code { private AiActionRiskClassificationRecord _record = new(...); }")]
    [InlineData("src/Hexalith.ChatBot.UI/Composer.razor", "@code { private object Classify() => new AiActionRiskClassificationRecord(...); }")]
    [InlineData("src/Hexalith.ChatBot.UI/Composer.razor", "@code { private Task Admit() => pipeline.AdmitAsync(command); }")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "public interface IChatBotAdmissionEvidenceValidator {}")]
    [InlineData("src/Hexalith.ChatBot.Server/Other.cs", "await processor.ProcessAsync(command);")]
    [InlineData("src/Hexalith.ChatBot.Cli/Adapter.cs", "await authorization.AuthorizeAsync(command);")]
    [InlineData("src/Hexalith.ChatBot.Mcp/Adapter.cs", "await risk.ClassifyAsync(command);")]
    [InlineData("src/Hexalith.ChatBot.Contracts/Command.cs", "public string AdmissionProfile { get; init; }")]
    public void NegativeFixturesShouldRejectCompetingSpinesStagesDispatchAndSelectors(string path, string source)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(source);
        Violates(path, source).ShouldBeTrue();
    }

    /// <summary>Verifies current Sources Should Obey The Same Guards As The Negative Fixtures.</summary>
    [Fact]
    public void CurrentSourcesShouldObeyTheSameGuardsAsTheNegativeFixtures()
    {
        string root = Root();
        string[] violations = Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor")
            .Where(file => !file.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal) && !file.Replace('\\', '/').Contains("/bin/", StringComparison.Ordinal))
            .Where(file => Violates(Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText(file))).ToArray();
        violations.ShouldBeEmpty();
        Violates("src/Hexalith.ChatBot.Cli/Adapter.cs", "return client.SubmitAsync(command);").ShouldBeFalse();
    }

    private static bool Violates(string path, string source)
    {
        if (!path.EndsWith("Gateway/ChatBotCommandAdmissionPipeline.cs", StringComparison.Ordinal) &&
            Regex.IsMatch(source, @"(?:class|record|struct)\s+(?:(?:class|struct)\s+)?\w*AdmissionPipeline\b")) { return true; }
        if (Regex.IsMatch(source, @"public\s+(?:(?:sealed|abstract|partial|readonly|ref)\s+)*(?:class|interface|record|struct)\s+(?:(?:class|struct)\s+)?(?:IChatBotAdmissionEvidenceValidator|IChatBotAdmissionEvidenceSource|ChatBotDomainServiceAdmissionStage)\b")) { return true; }
        if (Regex.IsMatch(source, @"\.\s*ProcessAsync\s*\(")) { return true; }
        bool adapter = path.Contains("/Adapters/", StringComparison.Ordinal) || path.Contains(".Cli/", StringComparison.Ordinal) || path.Contains(".Mcp/", StringComparison.Ordinal) || path.Contains(".UI/", StringComparison.Ordinal) || path.Contains(".Workers/", StringComparison.Ordinal);
        if (adapter && (Regex.IsMatch(source, @"\bnew\s+(?:[\w]+\.)*AiActionRiskClassificationRecord\s*\(") ||
            Regex.IsMatch(source, @"\bAiActionRiskClassificationRecord\??\s+\w+\s*=\s*new\s*\("))) { return true; }
        if (adapter && Regex.IsMatch(source, @"\.\s*(?:AuthorizeAsync|ClassifyAsync|RecordPreCommitAsync|AdmitAsync)\s*\(")) { return true; }
        return path.Contains(".Contracts/", StringComparison.Ordinal) && Regex.IsMatch(source, @"public\s+\w+\??\s+AdmissionProfile\b");
    }

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx"))) { directory = directory.Parent; }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }
}
