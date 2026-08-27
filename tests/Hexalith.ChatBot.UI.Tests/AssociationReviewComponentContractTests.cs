using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewComponentContractTests
{
    [Fact]
    public void M0GovernedPagesShouldRemainFrontComposerBodyContentWithoutDuplicateShellOwnership()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string program = ReadProjectFile("src/Hexalith.ChatBot.UI/Program.cs");
        string projectConversation = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");
        string projectConversationWorkspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string associationReview = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string approvalItem = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string aiPreview = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor");

        CountOccurrences(layout, "<FrontComposerShell").ShouldBe(1);
        layout.ShouldContain("AppTitle=\"Hexalith ChatBot\"");
        layout.ShouldContain("ShowAccountMenu=\"false\"");
        layout.ShouldContain("@Body");
        app.ShouldContain("css/chatbot.tokens.css");
        (app + layout).ShouldNotContain("<FluentProviders", Case.Sensitive);
        (app + layout).ShouldNotContain("StoreInitializer", Case.Sensitive);
        program.ShouldContain("AddHexalithFrontComposerQuickstart");
        program.ShouldContain("AddHexalithDomain<ChatBotUiFrontComposerMarker>");
        program.ShouldNotContain("AddHexalithEventStore", Case.Sensitive);
        program.ShouldNotContain("EventStore:BaseAddress", Case.Sensitive);
        program.ShouldContain("AddFluentUIComponents", Case.Sensitive);
        program.ShouldContain("AddChatBotUiHostDefaults", Case.Sensitive);
        program.ShouldContain("MapChatBotUiHealthEndpoints", Case.Sensitive);
        program.ShouldNotContain("AddFluxor", Case.Sensitive);

        projectConversation.ShouldContain("@page \"/projects/{ProjectId}/conversation\"");
        projectConversation.ShouldContain("ChatBotProjectConversationWorkspace");
        projectConversationWorkspace.ShouldContain("<ChatBotConversationShell");
        projectConversationWorkspace.ShouldContain("data-chatbot-responsive-fixture=\"@ResponsiveFixture\"");
        projectConversation.ShouldNotContain("FcPageLayoutMode.Constrained");

        associationReview.ShouldContain("@page \"/association-review/{AssociationId}\"");
        associationReview.ShouldContain("<ChatBotConversationShell");
        associationReview.ShouldContain("data-chatbot-responsive-fixture=\"association-review\"");
        associationReview.ShouldNotContain("FcPageLayoutMode.Constrained");

        approvalItem.ShouldContain("SubmitApprovalDecisionAsync");
        approvalItem.ShouldContain("ChatBotAiActionPreviewSections");
        approvalItem.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"");
        aiPreview.ShouldContain("data-chatbot-ai-action-preview=\"metadata-only\"");
        aiPreview.ShouldNotContain("providerPayload");
        aiPreview.ShouldNotContain("RawAttachmentContent");
    }

    [Fact]
    public void UiHostDefaultsShouldPreserveServiceDiscoveryTelemetryResilienceAndHealthProbes()
    {
        string defaults = ReadProjectFile("src/Hexalith.ChatBot.UI/Hosting/ChatBotUiHostDefaultsExtensions.cs");
        string project = ReadProjectFile("src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj");

        defaults.ShouldContain("AddServiceDiscovery");
        defaults.ShouldContain("AddStandardResilienceHandler");
        defaults.ShouldContain("DisableForUnsafeHttpMethods");
        defaults.ShouldContain("resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30)");
        defaults.ShouldContain("resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60)");
        defaults.ShouldContain("resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60)");
        defaults.ShouldContain("AddOpenTelemetry");
        defaults.ShouldContain("UseOtlpExporter");
        defaults.ShouldContain("MapGet(\"/health\"");
        defaults.ShouldContain("MapGet(\"/alive\"");
        project.ShouldContain("Microsoft.Extensions.Http.Resilience");
        project.ShouldContain("Microsoft.Extensions.ServiceDiscovery");
        project.ShouldContain("OpenTelemetry.Extensions.Hosting");
        project.ShouldNotContain("Hexalith.ChatBot.ServiceDefaults");
    }

    // AC3 (single, non-duplicated FrontComposer ownership) + AC6 (unique landmarks): the migrated M0
    // pages must stay pure body content. A page that re-introduces its own <main>, banner landmark,
    // skip link, or a nested <FrontComposerShell> would silently duplicate the shell-owned landmarks
    // the FrontComposer shell already renders. The static fixtures only approximate the rendered DOM,
    // so this guards the real production source against that regression.
    [Fact]
    public void MigratedM0PagesMustNotDuplicateShellLandmarksOwnedByFrontComposerShell()
    {
        string[] pageFiles =
        [
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor",
        ];

        foreach (string pageFile in pageFiles)
        {
            string page = ReadProjectFile(pageFile);

            // Only MainLayout owns the shell; pages render through <ChatBotConversationShell> as body content.
            page.ShouldContain("<ChatBotConversationShell");
            page.ShouldNotContain("<FrontComposerShell", Case.Sensitive);

            // The FrontComposer shell renders the banner, skip link, and main region; pages must not re-emit them.
            page.ShouldNotContain("role=\"banner\"", Case.Sensitive);
            page.ShouldNotContain("<main", Case.Sensitive);
            page.ShouldNotContain("chatbot-skip-link", Case.Sensitive);

            // Pages must not reintroduce an app-owned provider/store-initializer tree.
            page.ShouldNotContain("<FluentProviders", Case.Sensitive);
            page.ShouldNotContain("StoreInitializer", Case.Sensitive);
        }

        // The shared governed inner shell exposes unique, labelled region/complementary landmarks and never
        // re-declares a <main> or banner that would collide with the FrontComposer shell landmarks.
        string conversationShell = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor");
        conversationShell.ShouldContain("role=\"region\"");
        conversationShell.ShouldContain("role=\"complementary\"");
        conversationShell.ShouldContain("aria-label=");
        conversationShell.ShouldNotContain("<main", Case.Sensitive);
        conversationShell.ShouldNotContain("role=\"banner\"", Case.Sensitive);
        conversationShell.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
    }

    [Fact]
    public void AssociationReviewPageShouldUseGovernedPrimitivesAndKeepActionsDiscoverable()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string row = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor");
        string comparison = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor");

        page.ShouldContain("ChatBotConversationShell");
        page.ShouldContain("<FluentCard", Case.Sensitive);
        page.ShouldContain("<FluentStack", Case.Sensitive);
        page.ShouldContain("<FluentText", Case.Sensitive);
        page.ShouldContain("ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotStatusBanner");
        page.ShouldContain("ChatBotBlockedState");
        page.ShouldContain("ChatBotAssociationEvidenceComparison");
        actions.ShouldContain("ChatBotGovernedAction");
        actions.ShouldContain("<FluentLabel", Case.Sensitive);
        actions.ShouldContain("<FluentTextArea", Case.Sensitive);
        actions.ShouldContain("ValueChanged=\"UpdateNote\"");
        actions.ShouldContain("ValueChanged=\"UpdateCorrectionRationale\"");
        actions.ShouldNotContain("<textarea", Case.Sensitive);
        actions.ShouldNotContain("<label", Case.Sensitive);
        actions.ShouldContain("association-correction-submit");
        actions.ShouldContain("AssociationReviewCorrectionRationale");
        actions.ShouldContain("RecoverySafeNextActionCorrection");
        actions.ShouldContain("already-decided");
        actions.ShouldContain("already-corrected");
        actions.ShouldContain("evidence-expired");
        actions.ShouldContain("not-authorized");
        actions.ShouldContain("projection-pending");
        row.ShouldContain("<FluentButton", Case.Sensitive);
        row.ShouldContain("Type=\"ButtonType.Button\"");
        row.ShouldContain("role=\"radio\"");
        row.ShouldContain("aria-checked=\"@IsSelectedText\"");
        row.ShouldContain("data-chatbot-association-candidate=\"@Candidate.ProjectId\"");
        row.ShouldContain("data-chatbot-selected=\"@IsSelectedText\"");
        row.ShouldNotContain("<button", Case.Sensitive);
        row.ShouldContain("ChatBotEvidenceChip");
        row.ShouldContain("AssociationReviewEvidenceRestricted");
        comparison.ShouldContain("<FluentCard", Case.Sensitive);
        comparison.ShouldContain("<FluentStack", Case.Sensitive);
        comparison.ShouldContain("<FluentText", Case.Sensitive);
        comparison.ShouldContain("data-chatbot-association-comparison=\"true\"");
        comparison.ShouldContain("<article");
        comparison.ShouldContain("<code");

        // The <dl> dump was replaced by a Fluent key-value block. The previous assertion here checked for
        // "<dl" and passed only because those characters appear in a code comment describing the removal -
        // while Story13DefinitionListMigrationTests forbids the element in this very file. Assert the
        // replacement structure and the element's absence instead.
        comparison.ShouldContain("CodeRow(", Case.Sensitive);
        StripComments(comparison).ShouldNotContain("<dl");
    }

    [Fact]
    public void AssociationReviewActionsShouldPreserveValidationBannerAndDisabledReasonCatalog()
    {
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string policy = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewActionPolicy.cs");

        actions.ShouldContain("<ChatBotStatusBanner", Case.Sensitive);
        actions.ShouldContain("StableId=\"association-review-validation\"");
        actions.ShouldContain("StateFamily=\"@ChatBotFeedbackStateFamily.ValidationError\"");
        // The banner renders id="@StableId", so this reference resolves to a real element; it is composed at
        // runtime by DecisionNoteDescribedBy rather than hardcoded.
        actions.ShouldContain("DecisionNoteDescribedBy", Case.Sensitive);
        actions.ShouldContain("association-review-validation association-decision-note-counter");
        banner.ShouldContain("id=\"@StableId\"");
        // ARIA state attributes must resolve to explicit "true"/"false" strings, not a .NET bool (which Blazor
        // would drop entirely when false). See DecisionNoteInvalidText/CorrectionRationaleInvalidText.
        actions.ShouldContain("aria-invalid=\"@DecisionNoteInvalidText\"");
        actions.ShouldContain("aria-invalid=\"@CorrectionRationaleInvalidText\"");
        actions.ShouldContain("DecisionNoteInvalidText", Case.Sensitive);
        actions.ShouldContain("CorrectionRationaleInvalidText", Case.Sensitive);
        actions.ShouldNotContain("aria-invalid=\"@(", Case.Sensitive);
        actions.ShouldContain("ValueChanged=\"UpdateNote\"");
        actions.ShouldContain("ValueChanged=\"UpdateCorrectionRationale\"");

        foreach (string validationCode in new[]
        {
            "candidate-required",
            "correction-invalid-lifecycle",
            "correction-target-required",
            "stale-evidence",
            "association-review-note-too-long",
        })
        {
            actions.ShouldContain(validationCode, Case.Sensitive);
        }

        foreach (string disabledReasonCode in new[]
        {
            "candidate-required",
            "evidence-expired",
            "not-authorized",
            "projection-pending",
            "already-decided",
            "already-corrected",
            "audit-unavailable",
            "corrected-context-stale",
            "correction-delayed",
            "correction-invalid-lifecycle",
            "correction-target-required",
            "policy-blocked",
            "projection-invalidation-unavailable",
            "stale-evidence",
            "target-unauthorized",
            "terminal-state",
        })
        {
            actions.ShouldContain(disabledReasonCode, Case.Sensitive);
        }

        policy.ShouldContain("DisabledReasonPriority", Case.Sensitive);
        policy.ShouldContain("ResolveDecisionDisabledReasonCode", Case.Sensitive);
        policy.ShouldContain("ResolveCorrectionDisabledReasonCode", Case.Sensitive);
        actions.ShouldContain("DisabledReasonText", Case.Sensitive);
    }

    [Fact]
    public void AssociationReviewCssShouldCoverResponsiveForcedColorsAndReducedMotionWithoutRawColors()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-association-candidate");
        css.ShouldContain("@media (max-width: 48rem)");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldNotContain("#");
        css.ShouldNotContain("rgb(");
        css.ShouldNotContain("hsl(");
    }

    [Fact]
    public void ProjectConversationPageShouldUseGovernedPrimitivesAndLabelSystemDecisions()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");
        string item = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor");
        string decision = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor");
        string participant = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor");
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");
        string whyPanel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor");

        page.ShouldContain("ChatBotProjectConversationWorkspace");
        workspace.ShouldContain("ChatBotConversationShell");
        workspace.ShouldContain("ChatBotProjectContextHeader");
        workspace.ShouldContain("ChatBotStatusBanner");
        workspace.ShouldContain("ChatBotBlockedState");
        workspace.ShouldContain("ChatBotWhyProjectPanel");
        workspace.ShouldContain("OpenProjectAssociationWhyPanelAction");
        page.ShouldContain("@page \"/projects/{ProjectId}/conversation\"");
        stream.ShouldContain("ChatBotEmailConversationItem");
        stream.ShouldContain("ChatBotDecisionConversationItem");
        stream.ShouldContain("OnWhyThisProjectRequested");
        stream.ShouldContain("ChatBotParticipantConversationItem");
        stream.ShouldContain("ChatBotAttachmentConversationItem");
        item.ShouldContain("ChatBotActorBadge");
        item.ShouldContain("ProjectConversationSystemDecision");
        item.ShouldContain("ChatBotEvidenceChip");
        item.ShouldContain("WhyProjectOpenAction");
        item.ShouldContain("SourceProviderMessageId");
        item.ShouldContain("InternetMessageId");
        item.ShouldContain("SourceReceivedAtUtc");
        item.ShouldContain("SourceProvenanceDisplayToken");
        item.ShouldContain("ThresholdBandLabel");
        item.ShouldNotContain("SourceContext");
        item.ShouldNotContain("providerPayload");
        decision.ShouldContain("ChatBotActorBadge");
        decision.ShouldContain("ChatBotEvidenceChip");
        decision.ShouldContain("WhyProjectOpenAction");
        decision.ShouldContain("ProjectConversationDecisionItemAccessible");
        decision.ShouldContain("DecisionKindLabel");
        decision.ShouldContain("CorrectionKindLabel");
        decision.ShouldContain("EvidenceReferenceSummary");
        decision.ShouldContain("SupersedesAssociationId");
        decision.ShouldContain("PropagationProgress");
        decision.ShouldContain("DecisionNoteRedactionState");
        decision.ShouldContain("CorrectionRationaleRedactionState");
        decision.ShouldContain("DecisionUnavailableReason");
        decision.ShouldNotContain("DecisionNote\"");
        decision.ShouldNotContain("CorrectionRationale\"");
        decision.ShouldNotContain("SourceContext");
        decision.ShouldNotContain("providerPayload");
        participant.ShouldContain("ChatBotActorBadge");
        participant.ShouldContain("ChatBotEvidenceChip");
        participant.ShouldContain("ParticipantAllowedReviewActions");
        participant.ShouldContain("WhyUnavailable");
        participant.ShouldNotContain("AddressEvidence");
        participant.ShouldNotContain("ProviderDisplayName");
        attachment.ShouldContain("ChatBotActorBadge");
        attachment.ShouldContain("ChatBotEvidenceChip");
        attachment.ShouldContain("AttachmentStatusLabel");
        attachment.ShouldContain("WhyUnavailable");
        attachment.ShouldContain("RedactedMetadataValue");
        attachment.ShouldContain("SourceProviderAttachmentId");
        attachment.ShouldContain("AttachmentDisplayName");
        attachment.ShouldNotContain("SourceContext");
        attachment.ShouldNotContain("providerPayload");
        attachment.ShouldNotContain("RawAttachmentContent");
        whyPanel.ShouldContain("data-chatbot-why-project-panel");
        whyPanel.ShouldContain("WhyProjectPanelAccessible");
        whyPanel.ShouldContain("<FluentButton", Case.Sensitive);
        whyPanel.ShouldContain("Type=\"ButtonType.Button\"", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"CloseAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"OpenSupersedingCorrectionAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("EvidenceState(evidence)");
        whyPanel.ShouldContain("WhyProjectEvidenceRedactedExplanation");
        whyPanel.ShouldContain("SupersedingCorrection");
        whyPanel.ShouldNotContain("<button", Case.Sensitive);
        whyPanel.ShouldNotContain("DecisionNote\"");
        whyPanel.ShouldNotContain("CorrectionRationale\"");
        whyPanel.ShouldNotContain("SourceContext");
        whyPanel.ShouldNotContain("providerPayload");
    }

    [Fact]
    public void ApprovalGovernedActionSurfacesShouldUseFluentPrimitivesAndPreserveGovernedMarkers()
    {
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string taskIntent = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor");
        string whyPanel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor");

        approval.ShouldContain("<FluentButton", Case.Sensitive);
        approval.ShouldContain("Type=\"ButtonType.Button\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"ApproveAsync\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"RejectAsync\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"RequestRevisionAsync\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"CancelAsync\"", Case.Sensitive);
        approval.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"");
        approval.ShouldContain("aria-describedby=\"@(!CanApprove ? ApproveReasonId : null)\"");
        approval.ShouldContain("BlockApproveAsync");
        approval.ShouldContain("DecisionLiveRegion = \"assertive\"");
        approval.ShouldNotContain("<button", Case.Sensitive);

        taskIntent.ShouldContain("<FluentButton", Case.Sensitive);
        taskIntent.ShouldContain("<FluentLabel", Case.Sensitive);
        taskIntent.ShouldContain("<FluentTextInput", Case.Sensitive);
        taskIntent.ShouldContain("role=\"toolbar\"");
        taskIntent.ShouldContain("aria-label=\"Task intent actions\"");
        taskIntent.ShouldContain("aria-disabled=\"@AriaDisabled(transition)\"");
        taskIntent.ShouldContain("aria-describedby=\"@DisabledReasonReferenceId(transition)\"");
        taskIntent.ShouldContain("ValueChanged=\"OnPredecessorChanged\"");
        taskIntent.ShouldContain("predecessor_task_intent_required");
        taskIntent.ShouldContain("TaskIntentTransitionSelectionModel");
        taskIntent.ShouldNotContain("<button", Case.Sensitive);
        taskIntent.ShouldNotContain("<input", Case.Sensitive);
        taskIntent.ShouldNotContain("<label", Case.Sensitive);

        whyPanel.ShouldContain("<FluentButton", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"CloseAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"OpenSupersedingCorrectionAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("data-chatbot-correction-link");
        whyPanel.ShouldContain("data-chatbot-why-project-panel=\"metadata-only\"");
        whyPanel.ShouldContain("role=\"complementary\"");
        whyPanel.ShouldNotContain("<button", Case.Sensitive);
    }

    [Fact]
    public void ProjectConversationCssShouldCoverResponsiveForcedColorsAndReducedMotionWithoutRawColors()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-project-conversation");
        css.ShouldContain(".chatbot-email-conversation-item");
        css.ShouldContain(".chatbot-decision-conversation-item");
        css.ShouldContain(".chatbot-participant-conversation-item");
        css.ShouldContain(".chatbot-attachment-conversation-item");
        css.ShouldContain(".chatbot-why-project-panel");
        css.ShouldContain("@media (max-width: 48rem)");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldNotContain("#");
        css.ShouldNotContain("rgb(");
        css.ShouldNotContain("hsl(");
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

    private static int CountOccurrences(string value, string marker)
    {
        int count = 0;
        int startIndex = 0;
        while ((startIndex = value.IndexOf(marker, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += marker.Length;
        }

        return count;
    }

    /// <summary>
    /// The suppression rule was verified only at the service layer. These assert it at the two components that
    /// actually render evidence: a restricted item shows the localized placeholder, and neither the evidence
    /// kind/reference nor a reference-derived id is emitted.
    /// </summary>
    [Fact]
    public void EvidenceRenderersSuppressRestrictedEvidenceAndNeverDeriveIdsFromTheReference()
    {
        foreach (string path in new[]
        {
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor",
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor",
        })
        {
            string source = StripComments(ReadProjectFile(path));

            // Kind/Reference render only on the Available branch.
            source.ShouldContain("evidence.State is ChatBotEvidenceState.Available");
            source.ShouldContain("AssociationReviewEvidenceRestricted");

            // The chip must not advertise an open affordance for evidence it cannot open.
            source.ShouldContain("CanOpenEvidence=\"@(evidence.State is ChatBotEvidenceState.Available)\"");
            source.ShouldNotContain("CanOpenEvidence=\"true\"");

            // Ids are derived from the project id and position, never from the free-text reference - the chip
            // renders its reason id precisely when the evidence is restricted.
            source.ShouldContain("EvidenceStableId(");
            source.ShouldNotContain("{evidence.Reference}");
        }
    }

    /// <summary>
    /// The candidate row is a role="radio"; nesting another interactive control inside it is invalid markup
    /// and breaks the radio's accessible name.
    /// </summary>
    [Fact]
    public void EvidenceChipsAreNotNestedInsideTheCandidateRadio()
    {
        string row = StripComments(ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor"));

        int closeButton = row.IndexOf("</FluentButton>", StringComparison.Ordinal);
        int chip = row.IndexOf("<ChatBotEvidenceChip", StringComparison.Ordinal);

        closeButton.ShouldBeGreaterThan(-1);
        chip.ShouldBeGreaterThan(closeButton, "evidence chips must render after the radio button closes");
    }

    /// <summary>
    /// Removes Razor and C# comments so a source-text assertion cannot be satisfied by prose describing the
    /// very markup it is meant to prove exists.
    /// </summary>
    private static string StripComments(string source)
    {
        string withoutRazor = System.Text.RegularExpressions.Regex.Replace(
            source,
            @"@\*.*?\*@",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        string withoutBlock = System.Text.RegularExpressions.Regex.Replace(
            withoutRazor,
            @"/\*.*?\*/",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        return System.Text.RegularExpressions.Regex.Replace(
            withoutBlock,
            @"^\s*//.*$",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Multiline);
    }
}
