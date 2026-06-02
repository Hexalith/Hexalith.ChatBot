using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotEscalationPolicyEditorContractTests
{
    [Fact]
    public void EscalationEditorContractShouldCoverMatrixBoundedSelectorsValidationAndPhoneFallback()
    {
        ChatBotEscalationPolicyEditorContract contract = ChatBotEscalationPolicyEditorContract.CreateDefault();

        contract.IsComplete.ShouldBeTrue();
        contract.SelectorsAreBounded.ShouldBeTrue();
        contract.Validation.FocusTargetId.ShouldBe(contract.Validation.SummaryId);
        contract.Validation.RequiresMessageAssociation.ShouldBeTrue();
        contract.Recovery.ValidationSummaryPlacement.ShouldBe("before-fields");
        contract.Recovery.SaveConflictCause.ShouldBe(ChatBotSaveConflictCause.StaleData);
        contract.EscalationMatrix.Count.ShouldBe(5);
        // State classes are restricted to the five escalatable classes (retry excluded).
        contract.StateClassTokens.Count.ShouldBe(5);
        contract.StateClassTokens.ShouldNotContain("retry");
        contract.SeverityTokens.ShouldBe(["low", "medium", "high"]);
        contract.ChannelTokens.ShouldContain("operator-alert");
        contract.EscalationTargetRoleTokens.ShouldContain("policy-admin");
        contract.EscalationMatrix.ShouldAllBe(static row => row.AgeThresholdSeconds >= 0);
        contract.SmallScreenFallback.IsComplete.ShouldBeTrue();
        contract.SmallScreenFallback.ReachableExplanation.ShouldNotContain("tooltip", Case.Insensitive);
        contract.DisabledSaveAction.ReferencesReachableReason.ShouldBeTrue();
        contract.ContainsRestrictedText.ShouldBeFalse();
    }

    [Fact]
    public void EscalationEditorContractShouldRejectUnboundedSelectorRows()
    {
        ChatBotEscalationPolicyEditorContract baseline = ChatBotEscalationPolicyEditorContract.CreateDefault();
        ChatBotEscalationPolicyEditorContract tampered = baseline with
        {
            EscalationMatrix =
            [
                new ChatBotEscalationPolicyMatrixRow("review-needed", "see-only", 3600, "rogue-severity", "operations-admin", "in-app"),
            ],
        };

        tampered.SelectorsAreBounded.ShouldBeFalse();
        tampered.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void EscalationEditorContractShouldRejectNegativeAgeThreshold()
    {
        ChatBotEscalationPolicyEditorContract baseline = ChatBotEscalationPolicyEditorContract.CreateDefault();
        ChatBotEscalationPolicyEditorContract tampered = baseline with
        {
            EscalationMatrix =
            [
                new ChatBotEscalationPolicyMatrixRow("failure", "operate", -1, "high", "operations-admin", "operator-alert"),
            ],
        };

        tampered.SelectorsAreBounded.ShouldBeFalse();
    }

    [Fact]
    public void EscalationEditorComponentShouldUseLocalizedTextBoundedSelectorsAndNoRestrictedMarkers()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor");

        component.ShouldContain("ChatBotUiTextKey.EscalationPolicyTitle");
        component.ShouldContain("escalation-policy-validation-summary");
        component.ShouldContain("data-escalation-policy-matrix=\"true\"");
        component.ShouldContain("data-escalation-age-input");
        component.ShouldContain("data-escalation-severity-select");
        component.ShouldContain("data-escalation-role-select");
        component.ShouldContain("data-escalation-channel-select");
        component.ShouldContain("aria-invalid=\"true\"");
        component.ShouldContain("aria-describedby");
        component.ShouldContain("data-validation-placement=\"@Recovery.ValidationSummaryPlacement\"");
        component.ShouldContain("data-small-screen-fallback");
        component.ShouldContain("SeverityTokens");
        component.ShouldContain("EscalationTargetRoleTokens");
        component.ShouldContain("ChannelTokens");
        component.ShouldContain("escalation-policy-save");

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
