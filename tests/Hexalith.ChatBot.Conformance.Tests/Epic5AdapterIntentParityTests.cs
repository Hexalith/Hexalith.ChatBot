using System.Collections;
using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Mcp;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 5.4 real-adapter parity catalog. These tests drive UI/API, CLI, and MCP through adapter-facing
/// production paths and compare typed commands/read contract facts rather than presentation strings.
/// </summary>
public static class Epic5AdapterIntentParityTests
{
    [Fact]
    public static void SurfaceArmCatalogShouldContainExactlyTheRequiredStory54ProductionSurfaces()
    {
        SurfaceArms.All.Select(static arm => arm.Name).ShouldBe(["ui-api", "cli", "mcp"]);
        SurfaceArms.All.Select(static arm => arm.Origin.ToString()).ShouldBe(["Ui", "Cli", "Mcp"]);
    }

    [Fact]
    public static void IntentCatalogShouldCoverTheAvailableEpic5StateChangingAdapterSet()
    {
        string[] expectedKeys =
        [
            "association.associate",
            "association.reject",
            "association.defer",
            "association.correct",
            "operation.retry",
            "approval.decide",
            "ai_action.execute",
        ];

        SurfaceIntentCatalog.StateChangingIntents.Select(static intent => intent.Key).ShouldBe(expectedKeys);
        SurfaceIntentCatalog.StateChangingIntents.Select(static intent => intent.McpToolName).ShouldBe(
            ChatBotMcpToolCatalog.Tools
                .Where(static tool => tool.StateChanging)
                .Select(static tool => tool.Name)
                .ToArray());
    }

    [Fact]
    public static void IntentCatalogShouldCoverTheStory54ReadAdapterSet()
    {
        SurfaceIntentCatalog.ReadIntents.Select(static intent => intent.Key).ShouldBe(
        [
            "association.status",
            "operation.status",
            "operation.audit",
        ]);
    }

    [Fact]
    public static async Task StateChangingIntentsShouldSubmitEquivalentTypedCommandsWithOnlyOriginDifferent()
    {
        foreach (SemanticCommandIntent intent in SurfaceIntentCatalog.StateChangingIntents)
        {
            SurfaceCommandTranslation[] translations = await TranslateAllAsync(intent);
            IReadOnlyList<KeyValuePair<string, string>> baseline = CommandFacts(translations[0].Command);

            translations.Select(static item => item.SubmittedOrigin).ShouldBe(["ui", "cli", "mcp"], intent.Key);
            foreach (SurfaceCommandTranslation translation in translations)
            {
                translation.Command.GetType().ShouldBe(translations[0].Command.GetType(), intent.Key);
                CommandFacts(translation.Command).ShouldBe(baseline, intent.Key);
            }
        }
    }

    [Fact]
    public static async Task ReadIntentsShouldReturnEquivalentClientContractFacts()
    {
        foreach (SemanticReadIntent intent in SurfaceIntentCatalog.ReadIntents)
        {
            SurfaceReadTranslation[] reads = await ReadAllAsync(intent);
            SurfaceReadTranslation baseline = reads[0];

            foreach (SurfaceReadTranslation read in reads)
            {
                read.ReadMethod.ShouldBe(baseline.ReadMethod, intent.Key);
                read.TargetId.ShouldBe(baseline.TargetId, intent.Key);
                read.CorrelationId.ShouldBe(SurfaceIntentCatalog.CorrelationId, intent.Key);
                read.TaskId.ShouldBe(SurfaceIntentCatalog.TaskId, intent.Key);
                read.ContractFacts.ShouldBe(baseline.ContractFacts, intent.Key);
            }
        }
    }

    [Fact]
    public static async Task CapturedAdapterOutcomesShouldNotContainRestrictedLeakageSentinels()
    {
        object[] captured =
        [
            .. await TranslateAllAsync(SurfaceIntentCatalog.StateChangingIntents[0]),
            .. await ReadAllAsync(SurfaceIntentCatalog.ReadIntents[0]),
        ];

        string serialized = JsonSerializer.Serialize(captured, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (string sentinel in RestrictedLeakageSentinels())
        {
            serialized.ShouldNotContain(sentinel, Case.Insensitive);
        }
    }

    [Fact]
    public static async Task AddingOrRemovingARequiredSurfaceCannotSilentlyReduceCoverage()
    {
        SemanticCommandIntent commandIntent = SurfaceIntentCatalog.GatewayCommandIntent;
        SurfaceCommandTranslation[] commandTranslations = await TranslateAllAsync(commandIntent);
        SurfaceReadTranslation[] readTranslations = await ReadAllAsync(SurfaceIntentCatalog.ReadIntents[0]);

        commandTranslations.Select(static item => item.ArmName).ShouldBe(["ui-api", "cli", "mcp"]);
        readTranslations.Select(static item => item.ArmName).ShouldBe(["ui-api", "cli", "mcp"]);
        commandTranslations.Length.ShouldBe(3);
        readTranslations.Length.ShouldBe(3);
        SurfaceIntentCatalog.StateChangingIntents.Count.ShouldBe(7);
        SurfaceIntentCatalog.ReadIntents.Count.ShouldBe(3);
    }

    private static async Task<SurfaceCommandTranslation[]> TranslateAllAsync(SemanticCommandIntent intent)
    {
        List<SurfaceCommandTranslation> translations = [];
        foreach (ISurfaceArm arm in SurfaceArms.All)
        {
            translations.Add(await arm.TranslateCommandAsync(intent, TestContext.Current.CancellationToken).ConfigureAwait(false));
        }

        return [.. translations];
    }

    private static async Task<SurfaceReadTranslation[]> ReadAllAsync(SemanticReadIntent intent)
    {
        List<SurfaceReadTranslation> translations = [];
        foreach (ISurfaceArm arm in SurfaceArms.All)
        {
            translations.Add(await arm.InvokeReadAsync(intent, TestContext.Current.CancellationToken).ConfigureAwait(false));
        }

        return [.. translations];
    }

    private static IReadOnlyList<KeyValuePair<string, string>> CommandFacts(IChatBotCommand command)
    {
        List<KeyValuePair<string, string>> facts =
        [
            new("type", command.GetType().Name),
        ];

        foreach (PropertyInfo property in command.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            facts.Add(new KeyValuePair<string, string>(property.Name, CanonicalValue(property.GetValue(command))));
        }

        return facts;
    }

    private static string CanonicalValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is string text)
        {
            return text;
        }

        if (value is IEnumerable enumerable)
        {
            return string.Join("|", enumerable.Cast<object?>().Select(CanonicalValue));
        }

        return value.ToString() ?? string.Empty;
    }

    private static string[] RestrictedLeakageSentinels()
        =>
        [
            "restricted project",
            "candidate secret",
            "file metadata",
            "command payload",
            "bearer-token",
            "raw-claim",
            "provider-payload",
            "stack trace",
            "audit internals",
        ];
}
