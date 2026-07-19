using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.Tests;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Proves the accessibility and focus floor is encoded as UI-owned contracts.
/// </summary>
public sealed class ChatBotAccessibilityFocusContractTests
{
    private static readonly Epic10SurfaceContract[] Epic10SurfaceContracts =
    [
        new(
            "Project Workspace",
            "src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor",
            [
                "<ChatBotConversationShell",
                "data-chatbot-responsive-fixture=\"project-workspace\"",
                "ProjectWorkspaceStateNoProjectSelected",
                "ShowProjectSwitchSuccess",
                "ProjectWorkspacePickerIntro",
                "ChatBotStatusBanner",
            ]),
        new(
            "Project Conversation",
            "src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor",
            [
                "<ChatBotProjectConversationWorkspace",
                "project-conversation-title",
                "ProjectConversationTitle",
            ]),
        new(
            "S1 conversation stream and governed composer",
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor",
            [
                "<ChatBotConversationStream",
                "<ChatBotGovernedComposer",
                "ProjectConversationStream",
                "ProjectConversationComposer",
                "SubmitProjectConversationComposerAction",
            ]),
        new(
            "Governed composer focus and shortcut floor",
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor",
            [
                "project-conversation-composer-error",
                "<FluentButton",
                "<FluentLabel",
                "<FluentTextArea",
                "tabindex=\"-1\"",
                "FocusAsync",
                "@onkeydown:stopPropagation=\"true\"",
                "ProjectConversationComposerValidationRequired",
                "ProjectConversationComposerAccepted",
            ]),
        new(
            "S2 association review",
            "src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor",
            [
                "data-chatbot-responsive-fixture=\"association-review\"",
                "<FluentCard",
                "<FluentStack",
                "<FluentText",
                "ChatBotAssociationCandidateRow",
                "ChatBotAssociationReviewActions",
                "ChatBotAssociationEvidenceComparison",
                "ChatBotBlockedState",
            ]),
        new(
            "S3 AI approval",
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor",
            [
                "ApprovalEventAccessible",
                "aria-describedby",
                "WhyUnavailable",
                "ChatBotAiActionPreviewSections",
                "ApprovalDisabledReasonLabel",
            ]),
        new(
            "S8/S10 operational queues",
            "src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor",
            [
                "data-chatbot-operational-queue=\"true\"",
                "role=\"table\"",
                "role=\"row\"",
                "tabindex=\"0\"",
                "GovernedOperationsQueueOpenDetailAccessible",
                "GovernedOperationsQueueDetailUnavailable",
            ]),
        new(
            "Operational dashboards",
            "src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor",
            [
                "data-chatbot-responsive-fixture=\"operational-dashboards\"",
                "OperationalDashboardsFreshnessLabel",
                "ChatBotStatusBanner",
                // Story 13.5: the per-view data-viz is a FluentDataGrid + FluentCard KPI tiles (grid table/row
                // semantics) with the machine tokens on sibling per-row markers — replacing the role="table"/row/
                // tabindex="0" hand-rolled markup.
                "<FluentDataGrid",
                "<FluentCard",
                "data-chatbot-dashboard-view",
            ]),
        new(
            "S9 audit investigation",
            "src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor",
            [
                "data-chatbot-responsive-fixture=\"audit-investigation-s9\"",
                "ComplianceAuditSafeMetadataLabel",
                "ComplianceAuditOperateDenied",
                "aria-disabled=\"true\"",
                "data-compliance-operate-denied=\"true\"",
                "OpaqueEscalationTarget",
            ]),
        new(
            "Streaming stop primitive readiness",
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor",
            [
                "data-chatbot-streaming",
                "StopResponseAccessible",
                "StopResponseAnnouncement",
                "FocusReturnTargetId",
                "HexalithChatBot.focusElementById",
            ]),
    ];

    private static readonly Epic10SurfaceContract[] Epic12MigratedSurfaceContracts =
    [
        new("Story 12.2 governed composer", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor", ["<FluentButton", "<FluentLabel", "<FluentTextArea", "aria-describedby=\"project-conversation-composer-help project-conversation-composer-status\"", "aria-invalid=", "@onkeydown:stopPropagation=\"true\"", "FocusAsync"]),
        new("Story 12.3 conversation stream", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor", ["<FluentCard", "<FluentStack", "<FluentText", "data-chatbot-conversation-stream=\"metadata-only\"", "aria-labelledby=\"@TitleId\"", "<ol"]),
        new("Story 12.4 association review actions", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor", ["<FluentLabel", "<FluentTextArea", "aria-label=\"@UiText[ChatBotUiTextKey.AssociationReviewDecisionNote]\"", "aria-describedby=\"association-review-validation\"", "aria-invalid=\"@DecisionNoteInvalidText\"", "ChatBotGovernedAction"]),
        new("Story 12.4 association candidate row", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor", ["<FluentButton", "role=\"radio\"", "aria-checked=\"@IsSelectedText\"", "aria-label=\"@AccessibleLabel\"", "data-chatbot-association-candidate=\"@Candidate.ProjectId\""]),
        new("Story 12.5 approval decision actions", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor", ["<FluentButton", "aria-describedby=\"@ApproveReasonId\"", "DecisionLiveRegion", "BlockApproveAsync", "data-chatbot-approval-evidence-freshness"]),
        new("Story 12.5 why-this-project panel", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor", ["<FluentButton", "role=\"complementary\"", "data-chatbot-why-project-panel=\"metadata-only\"", "data-chatbot-correction-link", "data-chatbot-evidence-visibility"]),
        new("Story 12.5 task intent review", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor", ["<FluentButton", "<FluentLabel", "<FluentTextInput", "role=\"toolbar\"", "aria-describedby=\"@DisabledReasonReferenceId(transition)\"", "role=\"status\""]),
        new("Story 12.6 escalation editor", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor", ["<FluentNumberInput", "<FluentSelect", "<FluentOption", "<FluentTextInput", "<FluentLabel", "aria-label=", "role=\"complementary\""]),
        new("Story 12.6 notification routing editor", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor", ["<FluentSelect", "<FluentOption", "<FluentTextInput", "<FluentLabel", "aria-label=", "role=\"complementary\""]),
        new("Story 12.6 tenant policy editor", "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor", ["<FluentTextInput", "<FluentLabel", "aria-label=\"@UiText[ChatBotUiTextKey.CommandLabel]\"", "data-mailbox-status-row=\"permission-freshness\"", "role=\"complementary\""]),
        // Story 13.6 migrated the filter form to a FluentGrid of label-above-input fields, so the accessible name now
        // comes from the Fluent v5 native Label (not a separate <FluentLabel> + redundant aria-label); markers retargeted.
        new("Story 12.7 compliance audit investigation", "src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor", ["<FluentGrid", "<FluentTextInput", "<FluentNumberInput", "<FluentButton", "Label=\"@UiText[ChatBotUiTextKey.ComplianceAuditFilterTenant]\"", "data-compliance-operate-denied=\"true\"", "data-compliance-projection-pending=\"true\"", "compliance-phone-fallback"]),
        // Story 13.7 retarget: the governed-operations MainContent sibling sections now group in a single
        // FluentAccordion (one item per section, expanded by default). The queue/data markers and the
        // operation-outcome-title focus-landing target (asserted by the busy-region contract below) are
        // preserved on the regrouped sections, so the marker set is strengthened, not loosened.
        new("Story 12.7 governed operations", "src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor", ["<FluentButton", "<ChatBotGovernedAction", "data-chatbot-operational-queue=\"true\"", "role=\"table\"", "role=\"row\"", "data-chatbot-queue-family", "<FluentAccordion", "ExpandMode=\"AccordionExpandMode.Multi\"", "Expanded=\"true\"", "id=\"operation-outcome-title\""]),
        // Story 13.5 retarget: the operational-dashboards data-viz moved from hand-rolled role="table"/role="row"
        // markup to a FluentDataGrid + non-color FluentBadge status cues; the data-chatbot-* machine tokens are
        // preserved on the sibling per-row markers, so the marker set is retargeted (grid shape), not loosened.
        new("Story 12.7 operational dashboards", "src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor", ["<ChatBotGovernedAction", "data-chatbot-dashboard-view", "data-chatbot-freshness", "data-chatbot-slo-metric", "<FluentDataGrid", "<FluentBadge"]),
    ];

    private static readonly string[] RequiredFloorContracts =
    [
        "Keyboard operation",
        "Repeated landmark naming",
        "Visible-order focus sequence",
        "Focus return",
        "Disabled-action explanation",
        "Busy-region focus preservation",
        "Validation error association",
        "Off-surface redaction equivalence",
    ];

    [Fact]
    public void AccessibilityFloorShouldEnumerateRequiredGovernedUiContracts()
    {
        ChatBotAccessibilityFloorContract.RequiredContracts
            .Select(static contract => contract.Name)
            .ShouldBe(RequiredFloorContracts, ignoreOrder: false);

        foreach (ChatBotAccessibilityRequirement requirement in ChatBotAccessibilityFloorContract.RequiredContracts)
        {
            requirement.IsComplete.ShouldBeTrue();
            requirement.RequiredBehavior.ShouldNotContain("server", Case.Insensitive);
            requirement.RequiredBehavior.ShouldNotContain("DAPR", Case.Insensitive);
            requirement.RequiredBehavior.ShouldNotContain("MCP", Case.Insensitive);
        }
    }

    [Fact]
    public void KeyboardAndVisibleOrderContractsShouldRequireSkipMainHeadingAndReachableActions()
    {
        ChatBotKeyboardOperationContract keyboard = ChatBotKeyboardOperationContract.CreateGovernedSurface(
            SurfaceName: "Governed operations",
            RequiredKeyboardPaths:
            [
                "Skip link reaches main content.",
                "Primary governed action is keyboard reachable.",
                "Disabled governed action reason is keyboard reachable.",
            ]);

        keyboard.IsComplete.ShouldBeTrue();
        keyboard.RequiresVisibleFocus.ShouldBeTrue();
        keyboard.RequiresNoHoverOnlyCriticalActions.ShouldBeTrue();
        keyboard.RequiresShortcutGovernance.ShouldBeTrue();

        (keyboard with { RequiredKeyboardPaths = [] }).IsComplete.ShouldBeFalse();
        (keyboard with { RequiredKeyboardPaths = null! }).IsComplete.ShouldBeFalse();

        ChatBotFocusSequenceContract focusSequence = new(
            SkipLinkTargetId: "chatbot-main-content",
            MainRegionId: "chatbot-main-content",
            SurfaceHeadingSelector: "h1",
            OrderedLandmarkNames:
            [
                "Project context",
                "Governed command path",
                "Governed operation review context",
                "Operation status summary",
            ]);

        focusSequence.IsComplete.ShouldBeTrue();
        (focusSequence with { SurfaceHeadingSelector = string.Empty }).IsComplete.ShouldBeFalse();
        (focusSequence with { OrderedLandmarkNames = ["Project context", ""] }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void LandmarkContractsShouldRejectDuplicateRepeatedNames()
    {
        ChatBotLandmarkContract main = new("region", "Governed command path", IsRepeatedWithinSurface: true);
        ChatBotLandmarkContract complementary = new("complementary", "Governed operation review context", IsRepeatedWithinSurface: true);
        ChatBotLandmarkContract duplicate = new("region", "Governed command path", IsRepeatedWithinSurface: true);

        main.IsComplete.ShouldBeTrue();
        complementary.IsComplete.ShouldBeTrue();

        ChatBotLandmarkContract.HasUniqueAccessibleNames([main, complementary]).ShouldBeTrue();
        ChatBotLandmarkContract.HasUniqueAccessibleNames([main, duplicate]).ShouldBeFalse();
        ChatBotLandmarkContract.HasUniqueAccessibleNames(null!).ShouldBeFalse();
        (main with { AccessibleName = string.Empty }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void DisabledActionContractShouldRequireDiscoverableReasonAndSuppressedActivation()
    {
        ChatBotDisabledActionContract disabledAction = ChatBotDisabledActionContract.CreateGovernedAction(
            actionName: "Retry quarantined operation",
            disabledReasonId: "retry-disabled-reason",
            disabledReasonLabel: "Why unavailable? Quarantine review is required before retry.");

        disabledAction.IsComplete.ShouldBeTrue();
        disabledAction.UsesAriaDisabled.ShouldBeTrue();
        disabledAction.KeepsKeyboardFocusOrder.ShouldBeTrue();
        disabledAction.ReferencesReachableReason.ShouldBeTrue();
        disabledAction.SuppressesActivationWhenDisabled.ShouldBeTrue();
        disabledAction.UsesTooltipOnlyReason.ShouldBeFalse();

        (disabledAction with { UsesAriaDisabled = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { KeepsKeyboardFocusOrder = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { ReferencesReachableReason = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { SuppressesActivationWhenDisabled = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { UsesTooltipOnlyReason = true }).IsComplete.ShouldBeFalse();
        (disabledAction with { DisabledReasonLabel = string.Empty }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void OverlayFocusPolicyShouldCoverContainmentEscapeReturnAndComplementaryPanels()
    {
        ChatBotOverlayPolicy.RequiresFocusContainment(ChatBotOverlayKind.ModalDialog).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresFocusContainment(ChatBotOverlayKind.ModalSheet).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresFocusContainment(ChatBotOverlayKind.EvidenceDrawer).ShouldBeFalse();
        ChatBotOverlayPolicy.AllowsEscapeCloseWhenTopmost(ChatBotOverlayKind.Popover).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresFocusReturn(ChatBotOverlayKind.ReviewPanel).ShouldBeTrue();
        ChatBotOverlayPolicy.IsComplementaryRegion(ChatBotOverlayKind.ReviewPanel).ShouldBeTrue();

        ChatBotFocusReturnContract modalDialog = ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ModalDialog);
        ChatBotFocusReturnContract reviewPanel = ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ReviewPanel);

        modalDialog.IsComplete.ShouldBeTrue();
        modalDialog.ContainsFocusWhenModal.ShouldBeTrue();
        reviewPanel.IsComplete.ShouldBeTrue();
        reviewPanel.UsesComplementaryRegionWhenNonModal.ShouldBeTrue();
        (modalDialog with { ReturnsFocusToInvoker = false }).IsComplete.ShouldBeFalse();
        (modalDialog with { ContainsFocusWhenModal = false }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void BusyAndValidationContractsShouldRequireStableFocusAndErrorAssociations()
    {
        ChatBotBusyRegionContract busy = new(
            RegionId: "operation-status-region",
            AccessibleLabel: "Operation status summary",
            BusyStateElementId: "operation-status-region",
            FocusPreservationTargetId: "record-governed-note",
            LoadedContentLandingTargetId: "operation-outcome-title",
            ClearsAriaBusyOnSameRegion: true,
            AnnouncesHistoricalContent: false);

        busy.IsComplete.ShouldBeTrue();
        (busy with { ClearsAriaBusyOnSameRegion = false }).IsComplete.ShouldBeFalse();
        (busy with { AnnouncesHistoricalContent = true }).IsComplete.ShouldBeFalse();
        (busy with { FocusPreservationTargetId = string.Empty, LoadedContentLandingTargetId = string.Empty }).IsComplete.ShouldBeFalse();

        ChatBotValidationErrorContract validation = new(
            SummaryId: "approval-errors",
            SummaryLabel: "Approval validation summary",
            FocusTargetId: "approval-errors",
            AffectedFieldIds: ["approval-rationale", "approval-decision"],
            FieldMessageIds: new Dictionary<string, string>
            {
                ["approval-rationale"] = "approval-rationale-message",
                ["approval-decision"] = "approval-decision-message",
            },
            SafeNextAction: "Review the highlighted fields before submitting.");

        validation.IsComplete.ShouldBeTrue();
        validation.RequiresInvalidFields.ShouldBeTrue();
        validation.RequiresMessageAssociation.ShouldBeTrue();
        (validation with { FieldMessageIds = new Dictionary<string, string>() }).IsComplete.ShouldBeFalse();
        (validation with { AffectedFieldIds = null! }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void ShellAndPageShouldExposeStableFocusEntryAndUniqueRegionSemantics()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");
        string routes = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Routes.razor");
        string shell = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        layout.ShouldContain("<FrontComposerShell");
        layout.ShouldContain("AppTitle=\"Hexalith ChatBot\"");
        layout.ShouldNotContain("chatbot-main-content");
        routes.ShouldContain("FocusOnNavigate");
        routes.ShouldContain("Selector=\"h1\"");

        shell.ShouldContain("ResolvedMainLabel");
        shell.ShouldContain("ResolvedComplementaryLabel");
        shell.ShouldContain("role=\"complementary\"");
        page.ShouldContain("MainLabel=\"@UiText[ChatBotUiTextKey.GovernedCommandPath]\"");
        page.ShouldContain("ComplementaryLabel=\"@UiText[ChatBotUiTextKey.GovernedOperationReviewContext]\"");
        page.ShouldContain("<ComplementaryPanel>");
    }

    [Fact]
    public void ConversationStreamComponentsShouldUseFluentPrimitivesAndPreserveGovernedReadProjectionMarkers()
    {
        (string Path, string[] FluentMarkers, string[] ContractMarkers)[] contracts =
        [
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor",
                ["<FluentButton"],
                ["role=\"radio\"", "aria-checked=\"@IsSelectedText\"", "data-chatbot-association-candidate=\"@Candidate.ProjectId\"", "ChatBotEvidenceChip"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor",
                ["<FluentStack", "<FluentLabel", "<FluentTextArea"],
                ["ChatBotGovernedAction", "association-review-validation", "association-correction-submit", "projection-invalidation-unavailable"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["data-chatbot-association-comparison=\"true\"", "<article", "<dl", "ChatBotEvidenceChip"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor",
                ["<FluentStack"],
                ["aria-label=\"@ResolvedShellLabel\"", "role=\"region\"", "role=\"complementary\""]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["data-chatbot-conversation-stream=\"metadata-only\"", "<ol", "<li", "ChatBotBlockedState"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["data-chatbot-conversation-item-kind", "data-chatbot-conversation-item-id", "tabindex=\"0\"", "<time", "ChatBotEvidenceChip", "ChatBotConversationItemReviewHistory"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["ParticipantUnavailableReason", "ParticipantEvidenceFingerprintLabel", "ParticipantAllowedReviewActionsLabel", "tabindex=\"0\""]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["AttachmentRedactedDisplayName", "AttachmentAllowedActionsLabel", "AttachmentAiEligibilityLabel", "tabindex=\"0\""]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["DecisionNoteStateLabel", "CorrectionRationaleStateLabel", "PropagationProgressLabel", "ChatBotEvidenceChip"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["FailureCatalogHeadline", "FailureTerminalRuleReason", "FailureAuditUnavailableReason", "ClientActionLabel"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["data-chatbot-ai-content=\"source-evidence\"", "data-chatbot-ai-content=\"ai-summary\"", "AiOutcomeMetadataOnlyReason", "ChatBotAiActionPreviewSections"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor",
                ["<FluentCard", "<FluentStack", "<FluentText", "<FluentButton"],
                ["ApprovalDisabledReasonLabel", "ChatBotAiActionPreviewSections", "chatbot-approval-conversation-item__actions", "OnClick=\"ApproveAsync\""]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["StatusSummaryPartialSuccess", "data-chatbot-announcement-key", "role=\"@LiveRegionRole(facet)\"", "aria-live=\"@LiveRegionMode(facet)\""]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor",
                ["<FluentCard", "<FluentStack", "<FluentText", "<FluentBadge"],
                ["data-chatbot-classification", "ClassificationMessageCodeLabel", "DetectedIntentActionKindLabel", "DecisionUnavailableReason"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor",
                ["<FluentCard", "<FluentStack", "<FluentText"],
                ["Entries.OrderBy(static value => value.ReviewedAtUtc)", "ReviewHistoryReasonCodeLabel", "<time"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor",
                ["<FluentBadge", "<FluentButton", "<FluentText"],
                ["data-chatbot-actor-category", "aria-label=\"@AccessibleName\"", "OnUnresolvedAction"]),
            (
                "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor",
                ["<FluentBadge", "<FluentButton", "<FluentText"],
                ["data-chatbot-evidence-state", "data-chatbot-off-surface-kind", "aria-disabled=\"@AriaDisabled\"", "aria-describedby=\"@ReasonElementId\"", "ActivateAsync"])
        ];

        foreach ((string path, string[] fluentMarkers, string[] contractMarkers) in contracts)
        {
            string source = ReadProjectFile(path);

            foreach (string marker in fluentMarkers)
            {
                source.ShouldContain(marker, Case.Sensitive);
            }

            foreach (string marker in contractMarkers)
            {
                source.ShouldContain(marker, Case.Sensitive);
            }
        }
    }

    [Fact]
    public void Epic10SurfacesShouldRemainMappedToAccessibilityFocusContracts()
    {
        Epic10SurfaceContracts.Select(static contract => contract.SurfaceName).ShouldBe(
        [
            "Project Workspace",
            "Project Conversation",
            "S1 conversation stream and governed composer",
            "Governed composer focus and shortcut floor",
            "S2 association review",
            "S3 AI approval",
            "S8/S10 operational queues",
            "Operational dashboards",
            "S9 audit investigation",
            "Streaming stop primitive readiness",
        ], ignoreOrder: false);

        foreach (Epic10SurfaceContract contract in Epic10SurfaceContracts)
        {
            string source = ReadProjectFile(contract.SourcePath);
            foreach (string marker in contract.RequiredMarkers)
            {
                source.ShouldContain(marker);
            }

            source.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
            source.ShouldNotContain("<FluentProviders", Case.Sensitive);
            source.ShouldNotContain("StoreInitializer", Case.Sensitive);
            source.ShouldNotContain("data-chatbot-owned-provider", Case.Sensitive);
            source.ShouldNotContain("data-chatbot-owned-store-initializer", Case.Sensitive);
        }
    }

    [Fact]
    public void Epic12MigratedSurfacesShouldRemainMappedToFluentAccessibilityContracts()
    {
        Epic12MigratedSurfaceContracts.Select(static contract => contract.SurfaceName).ShouldBe(
        [
            "Story 12.2 governed composer",
            "Story 12.3 conversation stream",
            "Story 12.4 association review actions",
            "Story 12.4 association candidate row",
            "Story 12.5 approval decision actions",
            "Story 12.5 why-this-project panel",
            "Story 12.5 task intent review",
            "Story 12.6 escalation editor",
            "Story 12.6 notification routing editor",
            "Story 12.6 tenant policy editor",
            "Story 12.7 compliance audit investigation",
            "Story 12.7 governed operations",
            "Story 12.7 operational dashboards",
        ], ignoreOrder: false);

        foreach (Epic10SurfaceContract contract in Epic12MigratedSurfaceContracts)
        {
            string source = ReadProjectFile(contract.SourcePath);
            foreach (string marker in contract.RequiredMarkers)
            {
                source.ShouldContain(marker, Case.Sensitive);
            }

            source.ShouldNotContain("<button", Case.Sensitive);
            source.ShouldNotContain("<input", Case.Sensitive);
            source.ShouldNotContain("<select", Case.Sensitive);
            source.ShouldNotContain("<textarea", Case.Sensitive);
            source.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
            source.ShouldNotContain("<FluentProviders", Case.Sensitive);
            source.ShouldNotContain("StoreInitializer", Case.Sensitive);
        }
    }

    [Fact]
    public void PackagePinsShouldMatchApprovedSharedCatalogForAccessibilityFloor()
    {
        PackageCatalogTestHelper.AssertUiFoundationPins();
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

    private sealed record Epic10SurfaceContract(
        string SurfaceName,
        string SourcePath,
        IReadOnlyList<string> RequiredMarkers);
}
