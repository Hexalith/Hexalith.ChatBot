using System.Globalization;
using System.Resources;

using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotOffSurfaceRedactionContractTests
{
    private const string RestrictedSourceText = "restricted-file.txt";
    private const string RedactionNotice = "This export is redacted; full detail requires escalation.";
    private const string EscalationGuidance = "Request escalation to view full detail.";

    [Fact]
    public void OffSurfaceAffordanceKindsShouldCoverCurrentAndFutureGovernedActions()
    {
        Enum.GetNames<ChatBotOffSurfaceAffordanceKind>().ShouldBe(
            [
                "Export",
                "CopyToClipboard",
                "DownloadTranscript",
                "ReadAloud",
                "CopyShareHandoff",
                "AuditCopy",
                "EvidenceCopy",
            ],
            ignoreOrder: false);
    }

    [Fact]
    public void RedactedOffSurfaceAffordanceShouldRequireSafeArtifactAndAccessibleMessages()
    {
        ChatBotOffSurfaceAffordanceContract valid = RedactedAffordance();

        valid.IsComplete.ShouldBeTrue();
        valid.IsSafeForOffSurfaceUse.ShouldBeTrue();
        valid.UsesVisualPayloadOffSurface.ShouldBeTrue();
        valid.RequiresEscalationForFullDetail.ShouldBeTrue();
        valid.ContainsRestrictedSourceText.ShouldBeFalse();

        (valid with { OffSurfaceText = "This export is redacted; full detail requires escalation." }).IsComplete.ShouldBeFalse();
        (valid with { OffSurfaceText = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { AccessibleName = $"Copy {RestrictedSourceText}" }).IsComplete.ShouldBeFalse();
        (valid with { AccessibleDescription = $"Copies {RestrictedSourceText}" }).IsComplete.ShouldBeFalse();
        (valid with { AccessibleDescription = "Copies metadata only." }).IsComplete.ShouldBeFalse();
        (valid with { RedactionNotice = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { EscalationGuidance = string.Empty }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void UnauthorizedEvidenceAffordanceShouldRemainNonOpenableAndMetadataOnly()
    {
        ChatBotOffSurfaceAffordanceContract unauthorized = RedactedAffordance() with
        {
            Kind = ChatBotOffSurfaceAffordanceKind.EvidenceCopy,
            RedactionState = ChatBotOffSurfaceRedactionState.Unauthorized,
            DisabledReason = "Evidence is restricted for this viewer.",
            VisualText = "Evidence restricted",
            OffSurfaceText = "Evidence restricted. This export is redacted; full detail requires escalation.",
            AccessibleName = "Copy restricted evidence metadata",
            AccessibleDescription = "Evidence copy is restricted. This export is redacted; full detail requires escalation.",
        };

        unauthorized.IsComplete.ShouldBeTrue();
        unauthorized.CanOpenSourceDetail.ShouldBeFalse();
        unauthorized.OffSurfaceText.ShouldNotContain(RestrictedSourceText, Case.Insensitive);

        (unauthorized with { DisabledReason = string.Empty }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void LocalizationShouldResolveOffSurfaceRecoveryAndCognitiveLoadMicrocopy()
    {
        ResourceManager manager = SharedResource.ResourceManager;
        string[] keys =
        [
            ChatBotUiTextKey.OffSurfaceRedactedNotice,
            ChatBotUiTextKey.OffSurfaceEscalationGuidance,
            ChatBotUiTextKey.OffSurfaceUnavailableReason,
            ChatBotUiTextKey.RecoveryDuplicateSafeRetry,
            ChatBotUiTextKey.ActiveFilterSummaryTemplate,
            ChatBotUiTextKey.RecoverySafeNextActionAssociationReview,
            ChatBotUiTextKey.RecoverySafeNextActionAiActionReview,
            ChatBotUiTextKey.RecoverySafeNextActionQueueRetry,
            ChatBotUiTextKey.RecoverySafeNextActionCorrection,
            ChatBotUiTextKey.RecoverySafeNextActionTenantConfiguration,
        ];

        foreach (string key in keys)
        {
            manager.GetString(key, CultureInfo.GetCultureInfo("en")).ShouldNotBeNullOrWhiteSpace();
            manager.GetString(key, CultureInfo.GetCultureInfo("fr")).ShouldNotBeNullOrWhiteSpace();
            ChatBotUiTextKey.All.ShouldContain(key);
        }

        ChatBotUiTextLocalizer text = CreateProvider().GetRequiredService<ChatBotUiTextLocalizer>();

        using (UseCulture("en"))
        {
            text.OffSurfaceRedactedNotice().ShouldBe(RedactionNotice);
            text.OffSurfaceEscalationGuidance().ShouldBe(EscalationGuidance);
            text.RecoveryDuplicateSafeRetry().ShouldBe("Retry is duplicate-safe and will not create a second command.");
            text.ActiveFilterSummary("Pending review", 2).ShouldBe("Filter: Pending review. 2 results.");
        }

        using (UseCulture("fr"))
        {
            text.OffSurfaceRedactedNotice().ShouldBe("Cette exportation est masquée ; le détail complet nécessite une escalade.");
            text.RecoveryDuplicateSafeRetry().ShouldBe("La nouvelle tentative est sûre contre les doublons et ne créera pas de deuxième commande.");
            text.RecoverySafeNextAction(ChatBotRecoveryFlow.QueueRetry).ShouldBe("Réessayez uniquement quand la copie de sûreté anti-doublon reste visible.");
            text.ActiveFilterSummary("Revue en attente", 2).ShouldBe("Filtre : Revue en attente. 2 résultats.");
        }
    }

    [Fact]
    public void CurrentEvidencePrimitiveShouldAdvertiseOffSurfaceContractWithoutOpeningRedactedEvidence()
    {
        string evidence = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor");

        evidence.ShouldContain("OffSurfaceAffordance");
        evidence.ShouldContain("data-chatbot-off-surface-kind");
        evidence.ShouldContain("SafeOffSurfaceAffordance");
        evidence.ShouldContain("IsSafeForOffSurfaceUse");
        evidence.ShouldContain("IsOffSurfaceStateCompatible");
        evidence.ShouldContain("CanOpenEvidence");
        evidence.ShouldContain("IsUnavailable");
        evidence.ShouldContain("State is ChatBotEvidenceState.Unavailable or ChatBotEvidenceState.Redacted or ChatBotEvidenceState.Unauthorized");
    }

    private static ChatBotOffSurfaceAffordanceContract RedactedAffordance()
        => new(
            Kind: ChatBotOffSurfaceAffordanceKind.AuditCopy,
            VisualText: "Audit metadata only",
            OffSurfaceText: "Audit metadata only. audit:Committed origin:Ui correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW. This export is redacted; full detail requires escalation.",
            RedactionState: ChatBotOffSurfaceRedactionState.Redacted,
            AccessibleName: "Copy redacted audit metadata",
            AccessibleDescription: "Copies metadata only. This export is redacted; full detail requires escalation.",
            DisabledReason: "Full detail is restricted by policy.",
            EscalationGuidance: EscalationGuidance,
            RedactionNotice: RedactionNotice,
            RestrictedSourceTextMarkers: [RestrictedSourceText, "Secret Project"]);

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddScoped<ChatBotUiTextLocalizer>();
        return services.BuildServiceProvider();
    }

    private static IDisposable UseCulture(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        return new CultureRestore(previousCulture, previousUiCulture);
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }

    private sealed class CultureRestore(CultureInfo culture, CultureInfo uiCulture) : IDisposable
    {
        public void Dispose()
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }
}
