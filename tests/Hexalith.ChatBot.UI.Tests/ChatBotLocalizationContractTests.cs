using System.Globalization;
using System.Resources;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.State.GovernedOperations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotLocalizationContractTests
{
    [Fact]
    public void SupportedCultureContractShouldBeEnglishAndFrenchOnlyWithEnglishDefault()
    {
        ChatBotSupportedCultures.DefaultCultureName.ShouldBe("en");
        ChatBotSupportedCultures.SupportedCultureNames.ShouldBe(["en", "fr"], ignoreOrder: false);

        var options = ChatBotSupportedCultures.CreateRequestLocalizationOptions();

        options.DefaultRequestCulture.Culture.Name.ShouldBe("en");
        options.SupportedCultures!.Select(static culture => culture.Name).ShouldBe(["en", "fr"], ignoreOrder: false);
        options.SupportedUICultures!.Select(static culture => culture.Name).ShouldBe(["en", "fr"], ignoreOrder: false);
    }

    [Fact]
    public void SharedResourcesShouldHaveCompleteEnglishAndFrenchCoverage()
    {
        ResourceManager manager = SharedResource.ResourceManager;

        foreach (string key in ChatBotUiTextKey.All)
        {
            manager.GetString(key, CultureInfo.GetCultureInfo("en")).ShouldNotBeNullOrWhiteSpace($"Missing English resource {key}.");
            manager.GetString(key, CultureInfo.GetCultureInfo("fr")).ShouldNotBeNullOrWhiteSpace($"Missing French resource {key}.");
        }
    }

    [Fact]
    public void ActualStringLocalizerPathShouldResolveResourcesAndFailMissingKeys()
    {
        ServiceProvider provider = CreateProvider();
        ChatBotUiTextLocalizer text = provider.GetRequiredService<ChatBotUiTextLocalizer>();
        IStringLocalizer<SharedResource> raw = provider.GetRequiredService<IStringLocalizer<SharedResource>>();

        using (UseCulture("fr"))
        {
            text[ChatBotUiTextKey.RecordGovernedNote].ShouldBe("Enregistrer la note gouvernée");
            raw[ChatBotUiTextKey.RecordGovernedNote].ResourceNotFound.ShouldBeFalse();
            raw["Missing_ChatBot_Key"].ResourceNotFound.ShouldBeTrue();
            Should.Throw<InvalidOperationException>(() => text["Missing_ChatBot_Key"]);
        }
    }

    [Fact]
    public void GovernedTextShouldLocalizeDisplayLabelsWithoutChangingStableSlots()
    {
        using (UseCulture("en"))
        {
            ChatBotGovernedUiText.GetActorCategoryLabel(ChatBotActorCategory.HumanUser).ShouldBe("Human user");
            ChatBotGovernedUiText.GetFeedbackKindLabel(ChatBotFeedbackKind.Warning).ShouldBe("Warning");
        }

        using (UseCulture("fr"))
        {
            ChatBotGovernedUiText.GetActorCategoryLabel(ChatBotActorCategory.HumanUser).ShouldBe("utilisateur humain");
            ChatBotGovernedUiText.GetFeedbackKindLabel(ChatBotFeedbackKind.Warning).ShouldBe("Avertissement");
            ChatBotGovernedUiText.GetFeedbackKindSlot(ChatBotFeedbackKind.Warning).ShouldBe("warning");
            ChatBotGovernedUiText.GetActorCategoryIconText(ChatBotActorCategory.HumanUser).ShouldBe("HU");
        }
    }

    [Fact]
    public void InteractionGuardrailsShouldResolveThroughEnglishAndFrenchResources()
    {
        foreach (ChatBotInteractionGuardrail guardrail in Enum.GetValues<ChatBotInteractionGuardrail>())
        {
            ChatBotUiTextKey.All.ShouldContain(ChatBotGovernedUiText.GetInteractionGuardrailResourceKey(guardrail));
        }

        using (UseCulture("en"))
        {
            ChatBotGovernedUiText.GetInteractionGuardrailLabel(ChatBotInteractionGuardrail.NoHoverOnlyCriticalActions)
                .ShouldBe("No hover-only critical actions");
        }

        using (UseCulture("fr"))
        {
            ChatBotGovernedUiText.GetInteractionGuardrailLabel(ChatBotInteractionGuardrail.NoHoverOnlyCriticalActions)
                .ShouldBe("Aucune action critique uniquement au survol");
            ChatBotGovernedUiText.GetInteractionGuardrailLabel(ChatBotInteractionGuardrail.NoCliMcpAdminAuthorizationBypassAffordance)
                .ShouldBe("Aucune option de contournement d'autorisation CLI/MCP/admin");
        }
    }

    [Fact]
    public void PhraseLevelAccessibleLabelsShouldDifferByCulture()
    {
        ChatBotUiTextLocalizer text = CreateProvider().GetRequiredService<ChatBotUiTextLocalizer>();

        using (UseCulture("en"))
        {
            text.ActorBadgeAccessibleLabel(ChatBotActorCategory.HumanUser, "Jerome").ShouldBe("Human user actor: Jerome");
            text.RiskAccessibleLabel(ChatBotRiskActionClass.ToolInvoking, "Requires approval.").ShouldBe("Risk: Tool-invoking. Policy reason: Requires approval.");
            text.EvidenceAccessibleLabel(ChatBotEvidenceState.Redacted, "Supporting file", "Policy redacted.").ShouldBe("Evidence redacted: Supporting file. Policy redacted.");
            text.ParticipantStatusLabel("Resolved").ShouldBe("Resolved");
            text.ParticipantBlockedReasonLabel("DirectoryUnavailable").ShouldBe("Directory unavailable");
            text.ParticipantReviewActionLabel("CreatePending").ShouldBe("Create pending participant");
            text.AttachmentStatusLabel("Pending").ShouldBe("Pending");
            text.DecisionKindLabel("Associate").ShouldBe("Confirmed association");
            text.DecisionKindLabel("associate").ShouldBe("Confirmed association");
            text.CorrectionKindLabel("ProjectReassignment").ShouldBe("Project reassignment");
            text.CorrectionKindLabel("project-reassignment").ShouldBe("Project reassignment");
            text.RedactionStateLabel("Redacted").ShouldBe("Redacted");
            text.RedactionStateLabel("redacted").ShouldBe("Redacted");
            text.ApprovalEventKindLabel("request").ShouldBe("Approval requested");
            text.ApprovalStatusLabel("revision-requested").ShouldBe("Revision requested");
            text.ApprovalDecisionKindLabel("request-revision").ShouldBe("Requested revision");
            text.ApprovalEvidenceFreshnessLabel("expired").ShouldBe("Expired");
            text.ApprovalDisabledReasonLabel("evidence-expired").ShouldBe("Evidence expired");
            text.FailureCatalogHeadline("refusal_blocked_action").ShouldBe("Refused action");
            text.FailureCatalogReason("authorization_denied").ShouldBe("The operation is not available to this caller.");
            text.FailureCatalogHeadline("recoverable_mailbox_degradation").ShouldBe("Mailbox recovery pending");
            text.FailureBlockedReasonLabel("retry-exhausted").ShouldBe("Retry exhausted");
            text.FailureBlockedReasonLabel("already-decided").ShouldBe("Already decided");
            text.ClassificationKindLabel("actionable").ShouldBe("Actionable");
            text.DetectedActionKindLabel("request-decision").ShouldBe("Request decision");
            text.ReviewHistoryActionLabel("classification-projected").ShouldBe("Classification projected");
            text.ReviewHistoryDecisionLabel("email", "actionable").ShouldBe("Actionable");
            text[ChatBotUiTextKey.AiSummaryLabel].ShouldBe("AI summary");
            text[ChatBotUiTextKey.AiActionPreviewTitle].ShouldBe("AI action preview");
            text[ChatBotUiTextKey.AiActionPreviewOutboundTitle].ShouldBe("Outbound communication");
            text[ChatBotUiTextKey.AiActionPreviewMetadataOnlyReason].ShouldBe("Only governed metadata is shown; restricted detail uses stable reason codes.");
            text[ChatBotUiTextKey.AttachmentRedactedDisplayName].ShouldBe("Redacted attachment");
        }

        using (UseCulture("fr"))
        {
            text.ActorBadgeAccessibleLabel(ChatBotActorCategory.HumanUser, "Jerome").ShouldBe("Acteur utilisateur humain : Jerome");
            text.RiskAccessibleLabel(ChatBotRiskActionClass.ToolInvoking, "Approbation requise.").ShouldBe("Risque : Invoque un outil. Raison de stratégie : Approbation requise.");
            text.EvidenceAccessibleLabel(ChatBotEvidenceState.Redacted, "Fichier justificatif", "Masqué par stratégie.").ShouldBe("Preuve masquée : Fichier justificatif. Masqué par stratégie.");
            text.ParticipantStatusLabel("Resolved").ShouldBe("Résolu");
            text.ParticipantBlockedReasonLabel("DirectoryUnavailable").ShouldBe("Annuaire indisponible");
            text.ParticipantReviewActionLabel("CreatePending").ShouldBe("Créer un participant en attente");
            text.AttachmentStatusLabel("Pending").ShouldBe("En attente");
            text.DecisionKindLabel("Associate").ShouldBe("Association confirmée");
            text.DecisionKindLabel("associate").ShouldBe("Association confirmée");
            text.CorrectionKindLabel("ProjectReassignment").ShouldBe("Réaffectation de projet");
            text.CorrectionKindLabel("project-reassignment").ShouldBe("Réaffectation de projet");
            text.RedactionStateLabel("Redacted").ShouldBe("Masqué");
            text.RedactionStateLabel("redacted").ShouldBe("Masqué");
            text.ApprovalEventKindLabel("request").ShouldBe("Approbation demandée");
            text.ApprovalStatusLabel("revision-requested").ShouldBe("Révision demandée");
            text.ApprovalDecisionKindLabel("request-revision").ShouldBe("Révision demandée");
            text.ApprovalEvidenceFreshnessLabel("expired").ShouldBe("Expirée");
            text.ApprovalDisabledReasonLabel("evidence-expired").ShouldBe("Preuve expirée");
            text.FailureCatalogHeadline("refusal_blocked_action").ShouldBe("Action refusée");
            text.FailureCatalogReason("authorization_denied").ShouldBe("L'opération n'est pas disponible pour cet appelant.");
            text.FailureCatalogHeadline("recoverable_mailbox_degradation").ShouldBe("Récupération boîte aux lettres en attente");
            text.FailureBlockedReasonLabel("retry-exhausted").ShouldBe("Tentatives épuisées");
            text.FailureBlockedReasonLabel("already-decided").ShouldBe("Déjà décidé");
            text.ClassificationKindLabel("actionable").ShouldBe("Action requise");
            text.DetectedActionKindLabel("request-decision").ShouldBe("Demander une décision");
            text.ReviewHistoryActionLabel("classification-projected").ShouldBe("Classification projetée");
            text.ReviewHistoryDecisionLabel("email", "actionable").ShouldBe("Action requise");
            text[ChatBotUiTextKey.AiSummaryLabel].ShouldBe("Résumé IA");
            text[ChatBotUiTextKey.AiActionPreviewTitle].ShouldBe("Aperçu de l'action IA");
            text[ChatBotUiTextKey.AiActionPreviewOutboundTitle].ShouldBe("Communication sortante");
            text[ChatBotUiTextKey.AiActionPreviewMetadataOnlyReason].ShouldBe("Seules les métadonnées gouvernées sont affichées ; les détails restreints utilisent des codes de raison stables.");
            text[ChatBotUiTextKey.AttachmentRedactedDisplayName].ShouldBe("Pièce jointe masquée");
        }
    }

    [Fact]
    public void SafetyCriticalDefaultPrimitiveTextShouldResolveThroughFrenchResources()
    {
        ChatBotUiTextLocalizer text = CreateProvider().GetRequiredService<ChatBotUiTextLocalizer>();

        using (UseCulture("fr"))
        {
            text[ChatBotUiTextKey.GovernedCommandPath].ShouldBe("Chemin de commande gouvernée");
            text[ChatBotUiTextKey.RiskPolicyReasonDefault].ShouldBe("La stratégie exige une revue.");
            text[ChatBotUiTextKey.BlockedReasonTextDefault].ShouldBe("Cette opération est bloquée par la stratégie.");
            text[ChatBotUiTextKey.SafeNextActionDefault].ShouldBe("Revoyez l'action sûre suivante pour cet état.");
            text[ChatBotUiTextKey.DisabledReasonDefault].ShouldBe("Cette action n'est pas disponible dans l'état gouverné actuel.");
        }
    }

    [Fact]
    public void DisplayFormattingShouldUseCurrentCultureWhileIdentifiersStayInvariant()
    {
        ChatBotCultureFormatter formatter = CreateProvider().GetRequiredService<ChatBotCultureFormatter>();
        DateTimeOffset value = new(2026, 5, 31, 14, 45, 0, TimeSpan.Zero);

        using (UseCulture("en-US"))
        {
            formatter.FormatNumber(1234.5m).ShouldContain("1,234");
            formatter.FormatConfidence(0.875).ShouldBe("88%");
            formatter.FormatItemCount(2).ShouldBe("2 items");
            formatter.FormatDateTime(value).ShouldContain("2026");
        }

        using (UseCulture("fr-FR"))
        {
            formatter.FormatNumber(1234.5m).ShouldContain("1\u202f234");
            formatter.FormatConfidence(0.875).ShouldBe("88 %");
            formatter.FormatItemCount(2).ShouldBe("2 éléments");
            formatter.FormatConfidenceBand(ThresholdBand.Critical).ShouldBe("Critique");
            formatter.FormatActorLabel(ActorType.Human).ShouldBe("Humain");
        }

        ChatBotCultureFormatter.FormatInvariantIdentifier(1234.5m).ShouldBe("1234.5");
        ChatBotCultureFormatter.IdentifierEquals("Committed", "committed").ShouldBeFalse();
    }

    [Fact]
    public void StableMachineIdentifiersShouldRemainByteStableUnderFrenchCulture()
    {
        OperationOutcome outcome = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "Accepted",
            "AcceptedProjectionPending",
            "Committed",
            ["Retry", "inspect audit metadata", "defer"],
            ["post-commit - allow/proposed - audit:Committed - origin:Ui"]);

        using (UseCulture("fr"))
        {
            outcome.OperationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
            outcome.CommandId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
            outcome.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            outcome.LifecycleState.ShouldBe("Accepted");
            outcome.CompletionStatus.ShouldBe("AcceptedProjectionPending");
            outcome.AuditStatus.ShouldBe("Committed");
            outcome.SafeNextActions.ShouldBe(["Retry", "inspect audit metadata", "defer"], ignoreOrder: false);
            outcome.AuditHistory.Single().ShouldContain("origin:Ui");
        }
    }

    [Fact]
    public void ComponentsShouldUseTypedPhraseLocalizerForAccessibleNames()
    {
        string actor = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor");
        string evidence = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor");
        string risk = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor");
        string blocked = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor");
        string status = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string action = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor");
        string participant = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor");
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");
        string decision = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor");
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string statusSummary = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor");
        string classification = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor");
        string reviewHistory = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor");

        actor.ShouldContain("UiText.ActorBadgeAccessibleLabel");
        actor.ShouldContain("UiText.ActorBadgeResolveAccessibleLabel");
        evidence.ShouldContain("UiText.EvidenceAccessibleLabel");
        risk.ShouldContain("UiText.RiskAccessibleLabel");
        blocked.ShouldContain("UiText.BlockedStateAccessibleLabel");
        status.ShouldContain("UiText.StatusAccessibleLabel");
        risk.ShouldContain("ChatBotUiTextKey.RiskPolicyReasonDefault");
        blocked.ShouldContain("ChatBotUiTextKey.BlockedReasonTextDefault");
        blocked.ShouldContain("ChatBotUiTextKey.SafeNextActionDefault");
        action.ShouldContain("ChatBotUiTextKey.DisabledReasonDefault");
        participant.ShouldContain("UiText.ParticipantStatusLabel");
        participant.ShouldContain("UiText.ParticipantBlockedReasonLabel");
        participant.ShouldContain("UiText.ParticipantReviewActionLabel");
        attachment.ShouldContain("UiText.AttachmentStatusLabel");
        attachment.ShouldContain("ProjectConversationAttachmentItemAccessible");
        decision.ShouldContain("UiText.DecisionKindLabel");
        decision.ShouldContain("UiText.CorrectionKindLabel");
        decision.ShouldContain("UiText.RedactionStateLabel");
        decision.ShouldContain("ProjectConversationDecisionItemAccessible");
        approval.ShouldContain("UiText.ApprovalEventKindLabel");
        approval.ShouldContain("UiText.ApprovalStatusLabel");
        approval.ShouldContain("UiText.ApprovalDecisionKindLabel");
        approval.ShouldContain("UiText.ApprovalEvidenceFreshnessLabel");
        approval.ShouldContain("ApprovalEventAccessible");
        statusSummary.ShouldContain("UiText.StatusSummaryFacetLabel");
        statusSummary.ShouldContain("UiText.StatusSummaryHealthLabel");
        statusSummary.ShouldContain("UiText.StatusSummaryNextActionLabel");
        statusSummary.ShouldContain("StatusSummaryPartialSuccess");
        classification.ShouldContain("UiText.ClassificationKindLabel");
        classification.ShouldContain("UiText.DetectedActionKindLabel");
        classification.ShouldContain("DetectedIntent.SourceEvidenceIds");
        classification.ShouldContain("DetectedIntent.MessageCode");
        reviewHistory.ShouldContain("ReviewHistoryAccessible");
        reviewHistory.ShouldContain("UiText.ReviewHistoryActionLabel");
        reviewHistory.ShouldContain("UiText.ReviewHistoryDecisionLabel");
        statusSummary.ShouldContain("aria-live");
        statusSummary.ShouldContain("ChatBotAnnouncementDeduplicationState");
        statusSummary.ShouldContain("OncePerStableOperationKey");
        statusSummary.ShouldContain("data-chatbot-live-announced");
        statusSummary.ShouldContain("accepted-projection-pending");

        actor.ShouldNotContain(" actor: ");
        actor.ShouldNotContain("IsResolved && !string.IsNullOrWhiteSpace(DisplayLabel)");
        evidence.ShouldNotContain("}: {Text}");
        risk.ShouldNotContain("Policy reason:");
        blocked.ShouldNotContain("Next action: {SafeNextAction}");
        participant.ShouldNotContain("? \"unknown\"");
        participant.ShouldNotContain("string.Join(\", \", Item.ParticipantAllowedReviewActions)");
        approval.ShouldNotContain("Done");
        statusSummary.ShouldNotContain("Done");
        statusSummary.ShouldNotContain("executed");
        statusSummary.ShouldNotContain("CommandPayload");
        statusSummary.ShouldNotContain("AuditEnvelope");
    }

    [Fact]
    public void AiActionPreviewComponentShouldUseLocalizedMetadataOnlySections()
    {
        string preview = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor");

        preview.ShouldContain("AiActionPreviewAccessible");
        preview.ShouldContain("AiActionPreviewOutboundTitle");
        preview.ShouldContain("AiActionPreviewFileAccessTitle");
        preview.ShouldContain("AiActionPreviewCommandTitle");
        preview.ShouldContain("AiActionPreviewGeneratedChangesTitle");
        preview.ShouldContain("AiActionPreviewMetadataOnlyReason");
        preview.ShouldContain("data-chatbot-ai-action-preview");
        preview.ShouldContain("aria-disabled");
        preview.ShouldContain("reason.Equals(\"not-yet-produced\", StringComparison.OrdinalIgnoreCase)");
        preview.ShouldNotContain("raw prompt", Case.Insensitive);
        preview.ShouldNotContain("provider payload", Case.Insensitive);
        preview.ShouldNotContain("file contents", Case.Insensitive);
    }

    [Fact]
    public void AiActionPreviewCssShouldCoverReachableFocusForcedColorsAndReducedMotion()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-ai-action-preview__reason:focus");
        css.ShouldContain(".chatbot-ai-action-preview__section:focus");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain(".chatbot-ai-action-preview__reason,");
        css.ShouldContain(".chatbot-ai-action-preview__section,");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain(".chatbot-ai-action-preview,");
    }

    [Fact]
    public void PackagePinsShouldRemainUnchangedForLocalizationFoundation()
    {
        string packages = ReadProjectFile("Directory.Packages.props");
        string uiProject = ReadProjectFile("src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj");

        packages.ShouldContain("Include=\"Microsoft.FluentUI.AspNetCore.Components\" Version=\"5.0.0-rc.3-26138.1\"");
        packages.ShouldContain("Include=\"Fluxor\" Version=\"6.9.0\"");
        packages.ShouldContain("Include=\"Microsoft.Playwright\" Version=\"1.60.0\"");
        packages.ShouldContain("Include=\"xunit.v3\" Version=\"3.2.2\"");
        packages.ShouldContain("Include=\"bunit\" Version=\"2.7.2\"");
        uiProject.ShouldNotContain("Version=");
        uiProject.ShouldNotContain("IViewLocalizer");
        uiProject.ShouldNotContain("IHtmlLocalizer");
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddScoped<ChatBotUiTextLocalizer>();
        services.AddScoped<ChatBotCultureFormatter>();
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
