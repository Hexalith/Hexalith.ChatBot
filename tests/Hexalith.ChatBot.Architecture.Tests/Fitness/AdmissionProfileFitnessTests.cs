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
        // AI-profile classification and structured approval are selected by command name. Freeze both sets so a new
        // AI contract cannot silently skip classification or pass approval lifetime/drift on an opaque reference.
        catalog.Commands.Where(row => row.Value is ChatBotAdmissionProfile.AiRead or ChatBotAdmissionProfile.AiEffect)
            .Where(row => !ChatBotCommandAdmissionCatalog.RequiresClassification(row.Key))
            .Select(row => row.Key).Order(StringComparer.Ordinal)
            .ShouldBe(new[] { nameof(DecideAiActionApproval), nameof(MarkAiActionProposalInvalidatedByCorrection) }.Order(StringComparer.Ordinal));
        catalog.Commands.Where(row => row.Value == ChatBotAdmissionProfile.AiEffect)
            .Where(row => ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(row.Key).Contains("approval-lifetime-drift"))
            .Select(row => row.Key)
            .ShouldBe(new[] { nameof(ExecuteApprovedAIAction) });
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
        // Freeze the approved lifecycle exemptions: adding or changing one requires an explicit product decision.
        Dictionary<string, string[]> expected = new(StringComparer.Ordinal)
        {
            [nameof(AssociateEmailToProject)] = ["correction-impact-manifest"],
            [nameof(CaptureTaskIntent)] = ["correction-impact-manifest"],
            [nameof(DeferEmailProjectAssociation)] = ["correction-impact-manifest"],
            [nameof(MarkEmailAssociationNeedsReview)] = ["correction-impact-manifest"],
            [nameof(MarkTaskIntentDisposition)] = ["correction-impact-manifest"],
            [nameof(RejectEmailProjectAssociation)] = ["correction-impact-manifest"],
            [nameof(ResolveMailboxMessageParticipants)] = ["correction-impact-manifest"],
            [nameof(ScoreMailboxMessageAssociation)] = ["correction-impact-manifest"],
            [nameof(ProposeAIAction)] = ["human-only-non-self-approval", "approval-lifetime-drift"],
            [nameof(DecideAiActionApproval)] = ["approval-lifetime-drift"],
            [nameof(MarkAiActionProposalInvalidatedByCorrection)] = ["human-only-non-self-approval", "approval-lifetime-drift"],
            [nameof(CreateOutboundDraft)] = ["human-approval-lifetime-drift", "provider-evidence", "no-blind-retry"],
            [nameof(RequestOutboundSendApproval)] = ["human-approval-lifetime-drift", "provider-evidence", "no-blind-retry"],
            [nameof(DecideOutboundApproval)] = ["human-approval-lifetime-drift", "provider-evidence", "no-blind-retry"],
        };
        ChatBotCommandAdmissionCatalog.NotApplicableStages.Keys.Order(StringComparer.Ordinal).ShouldBe(expected.Keys.Order(StringComparer.Ordinal));
        foreach (KeyValuePair<string, IReadOnlyList<string>> exemption in ChatBotCommandAdmissionCatalog.NotApplicableStages)
        {
            ChatBotAdmissionProfile profile = catalog.Find(exemption.Key).ShouldNotBeNull();
            exemption.Value.ShouldNotBeEmpty();
            exemption.Value.ShouldBe(expected[exemption.Key], ignoreOrder: true);
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
        foreach (string command in new[] { nameof(ProposeAIAction), nameof(MarkAiActionProposalInvalidatedByCorrection) })
        {
            ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(command).ShouldNotContain("human-only-non-self-approval");
            ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(command).ShouldNotContain("approval-lifetime-drift");
        }
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(DecideAiActionApproval)).ShouldContain("human-only-non-self-approval");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(DecideAiActionApproval)).ShouldNotContain("approval-lifetime-drift");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(ExecuteApprovedAIAction)).ShouldContain("human-only-non-self-approval");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(ExecuteApprovedAIAction)).ShouldContain("approval-lifetime-drift");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(CreateOutboundDraft)).ShouldNotContain("provider-evidence");
        ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(nameof(ExecuteApprovedOutboundDraft)).ShouldContain("provider-evidence");
    }

    /// <summary>
    /// Each profile's owner proofs stay locked to the addendum's "Additional mandatory stages" column, plus the
    /// Story 1.7 matrix's Project-authority requirement for ai-read-v1.
    /// </summary>
    [Fact]
    public void ProfileRequiredStagesShouldMatchTheLockedAddendumColumnAndStoryMatrix()
    {
        // Locked list: changing a profile's stages must change their source and this pin together. Every stage comes
        // from the addendum column except ai-read-v1's project-authority, which the frozen Story 1.7 I/O matrix
        // requires ("current policy/context/Project authority"); do not drop it to match the addendum row alone.
        // AI risk classification is the pipeline's classifier stage, not an owner proof, so it is not listed here.
        Dictionary<ChatBotAdmissionProfile, string[]> expected = new()
        {
            [ChatBotAdmissionProfile.MailboxIntake] = ["provider-identity-deduplication", "authenticity-state", "a6-retention", "attachment-quarantine"],
            [ChatBotAdmissionProfile.Workflow] = ["family-state", "evidence-freshness", "project-authority", "correction-impact-manifest"],
            [ChatBotAdmissionProfile.GovernedChat] = ["conversation-authority", "origin-instruction-boundary", "attempt-identity-revision", "stream-race-guard"],
            [ChatBotAdmissionProfile.AiRead] = ["origin-instruction-boundary", "policy-context-scope", "project-authority"],
            [ChatBotAdmissionProfile.AiEffect] = ["origin-instruction-boundary", "human-only-non-self-approval", "approval-lifetime-drift", "allowlist-effect-target"],
            [ChatBotAdmissionProfile.HumanEffect] = ["frozen-content-recipients", "sender-authority", "human-approval-lifetime-drift", "provider-evidence", "no-blind-retry"],
            [ChatBotAdmissionProfile.CommandExecution] = ["exposed-command-effect", "caller-authority", "prior-attempt-state", "target-idempotency-concurrency", "uncertain-effect-denial"],
            [ChatBotAdmissionProfile.Governance] = ["closed-role-scope", "separation-of-duty", "independent-approval", "safer-state"],
            [ChatBotAdmissionProfile.DataRights] = ["request-authority", "tenantowner-compliance-approval", "a6-policy", "hold-precedence", "owner-command-acknowledgement"],
            [ChatBotAdmissionProfile.ProjectionDelivery] = ["source-watermark-or-recipient-redaction-routing"],
        };
        expected.Keys.Order().ShouldBe(Enum.GetValues<ChatBotAdmissionProfile>().Order());
        foreach (KeyValuePair<ChatBotAdmissionProfile, string[]> profile in expected)
        {
            ChatBotCommandAdmissionCatalog.RequiredStages(profile.Key).ShouldBe(profile.Value, ignoreOrder: true);
        }
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
        Should.Throw<InvalidOperationException>(() => new ChatBotCommandAdmissionCatalog(rows.Skip(1).Append(new("Unknown", rows[0].Value))));
        ChatBotAdmissionProfile differentProfile = Enum.GetValues<ChatBotAdmissionProfile>().First(profile => profile != rows[0].Value);
        Should.Throw<InvalidOperationException>(() => new ChatBotCommandAdmissionCatalog(rows.Skip(1).Append(new(rows[0].Key, differentProfile))));
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
    [InlineData("src/Hexalith.ChatBot.Client/Adapter.cs", "RiskClassification = new AiActionRiskClassificationRecord(...);")]
    [InlineData("src/Hexalith.ChatBot.Client/Adapter.cs", "await pipeline.ClassifyAsync(command);")]
    [InlineData("src/Hexalith.ChatBot.UI/Adapter.cs", "RiskClassification = new AiActionRiskClassificationRecord(...);")]
    [InlineData("src/Hexalith.ChatBot.UI/Adapter.cs", "AiActionRiskClassificationRecord record = new(...);")]
    [InlineData("src/Hexalith.ChatBot.UI/Adapter.cs", "AiActionRiskClassificationRecord Classify(Request request) => new(...);")]
    [InlineData("src/Hexalith.ChatBot.UI/Composer.razor", "@code { private AiActionRiskClassificationRecord Classify(Request request) => new(...); }")]
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
        bool adapter = path.Contains(".Client/", StringComparison.Ordinal) || path.Contains("/Adapters/", StringComparison.Ordinal) || path.Contains(".Cli/", StringComparison.Ordinal) || path.Contains(".Mcp/", StringComparison.Ordinal) || path.Contains(".UI/", StringComparison.Ordinal) || path.Contains(".Workers/", StringComparison.Ordinal);
        if (adapter && (Regex.IsMatch(source, @"\bnew\s+(?:[\w]+\.)*AiActionRiskClassificationRecord\s*\(") ||
            Regex.IsMatch(source, @"\bAiActionRiskClassificationRecord\??\s+\w+\s*(?:=\s*|\([^)]*\)\s*=>\s*)new\s*\("))) { return true; }
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
