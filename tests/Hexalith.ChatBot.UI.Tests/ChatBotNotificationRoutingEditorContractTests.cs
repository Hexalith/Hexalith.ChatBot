using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotNotificationRoutingEditorContractTests
{
    [Fact]
    public void RoutingEditorContractShouldCoverMatrixBoundedSelectorsValidationAndPhoneFallback()
    {
        ChatBotNotificationRoutingEditorContract contract = ChatBotNotificationRoutingEditorContract.CreateDefault();

        contract.IsComplete.ShouldBeTrue();
        contract.SelectorsAreBounded.ShouldBeTrue();
        contract.Validation.FocusTargetId.ShouldBe(contract.Validation.SummaryId);
        contract.Validation.RequiresMessageAssociation.ShouldBeTrue();
        contract.Recovery.ValidationSummaryPlacement.ShouldBe("before-fields");
        contract.Recovery.SaveConflictCause.ShouldBe(ChatBotSaveConflictCause.StaleData);
        contract.RoutingMatrix.Count.ShouldBe(6);
        contract.StateClassTokens.Count.ShouldBe(6);
        contract.ChannelTokens.ShouldContain("operator-alert");
        contract.RecipientRoleTokens.ShouldContain("policy-admin");
        contract.SmallScreenFallback.IsComplete.ShouldBeTrue();
        contract.SmallScreenFallback.ReachableExplanation.ShouldNotContain("tooltip", Case.Insensitive);
        contract.DisabledSaveAction.ReferencesReachableReason.ShouldBeTrue();
        contract.ContainsRestrictedText.ShouldBeFalse();
    }

    [Fact]
    public void RoutingEditorContractShouldRejectUnboundedSelectorRows()
    {
        ChatBotNotificationRoutingEditorContract baseline = ChatBotNotificationRoutingEditorContract.CreateDefault();
        ChatBotNotificationRoutingEditorContract tampered = baseline with
        {
            RoutingMatrix =
            [
                new ChatBotNotificationRoutingMatrixRow("review-needed", "see-only", "rogue-role", "in-app"),
            ],
        };

        tampered.SelectorsAreBounded.ShouldBeFalse();
        tampered.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void RoutingEditorComponentShouldUseLocalizedTextBoundedSelectorsAndNoRestrictedMarkers()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor");

        component.ShouldContain("ChatBotUiTextKey.NotificationRoutingTitle");
        component.ShouldContain("<FluentLabel", Case.Sensitive);
        component.ShouldContain("<FluentSelect", Case.Sensitive);
        component.ShouldContain("<FluentOption", Case.Sensitive);
        component.ShouldContain("<FluentTextInput", Case.Sensitive);
        component.ShouldContain("notification-routing-validation-summary");
        component.ShouldContain("data-notification-routing-matrix=\"true\"");
        component.ShouldContain("data-routing-role-select");
        component.ShouldContain("data-routing-channel-select");
        component.ShouldContain("aria-invalid=\"true\"");
        component.ShouldContain("aria-describedby");
        component.ShouldContain("data-validation-placement=\"@Recovery.ValidationSummaryPlacement\"");
        component.ShouldContain("data-small-screen-fallback");
        component.ShouldContain("RecipientRoleTokens");
        component.ShouldContain("ChannelTokens");
        component.ShouldContain("notification-routing-save");

        component.ShouldNotContain("<input", Case.Sensitive);
        component.ShouldNotContain("<select", Case.Sensitive);
        component.ShouldNotContain("<option", Case.Sensitive);
        component.ShouldNotContain("projectName", Case.Insensitive);
        component.ShouldNotContain("providerPayload", Case.Insensitive);
        component.ShouldNotContain("rawClaims", Case.Insensitive);
        component.ShouldNotContain("mailboxSubject", Case.Insensitive);
        component.ShouldNotContain("recipientAddress", Case.Insensitive);
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }
}
