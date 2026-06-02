using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotTenantPolicyEditorContractTests
{
    [Fact]
    public void TenantPolicyEditorContractShouldCoverS5ValidationRecoveryAndPhoneFallback()
    {
        ChatBotTenantPolicyEditorContract contract = ChatBotTenantPolicyEditorContract.CreateDefault();

        contract.IsComplete.ShouldBeTrue();
        contract.Validation.FocusTargetId.ShouldBe(contract.Validation.SummaryId);
        contract.Validation.RequiresMessageAssociation.ShouldBeTrue();
        contract.Recovery.ValidationSummaryPlacement.ShouldBe("before-fields");
        contract.Recovery.SaveConflictCause.ShouldBe(ChatBotSaveConflictCause.StaleData);
        contract.SmallScreenFallback.IsComplete.ShouldBeTrue();
        contract.SmallScreenFallback.ReachableExplanation.ShouldNotContain("tooltip", Case.Insensitive);
        contract.DisabledSaveAction.ReferencesReachableReason.ShouldBeTrue();
        contract.ContainsRestrictedText.ShouldBeFalse();
    }

    [Fact]
    public void TenantPolicyEditorComponentShouldUseLocalizedTextAndAriaFieldContracts()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor");

        component.ShouldContain("ChatBotUiTextKey");
        component.ShouldContain("tenant-policy-validation-summary");
        component.ShouldContain("aria-invalid=\"true\"");
        component.ShouldContain("aria-describedby");
        component.ShouldContain("data-validation-placement=\"@Recovery.ValidationSummaryPlacement\"");
        component.ShouldContain("data-small-screen-fallback");
        component.ShouldNotContain("projectName", Case.Insensitive);
        component.ShouldNotContain("providerPayload", Case.Insensitive);
        component.ShouldNotContain("rawClaims", Case.Insensitive);
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
}
