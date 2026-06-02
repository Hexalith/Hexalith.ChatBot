using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotApprovalQueuePriorityContractTests
{
    [Fact]
    public void ApprovalQueuePriorityContractShouldCoverGroupingPriorityAndPhoneFallback()
    {
        ChatBotApprovalQueuePriorityContract contract = ChatBotApprovalQueuePriorityContract.CreateDefault();

        contract.IsComplete.ShouldBeTrue();
        contract.GroupsAreBounded.ShouldBeTrue();
        contract.PrioritizedHighestFirst.ShouldBeTrue();
        contract.Validation.FocusTargetId.ShouldBe(contract.Validation.SummaryId);
        contract.Validation.RequiresMessageAssociation.ShouldBeTrue();
        contract.Recovery.ValidationSummaryPlacement.ShouldBe("before-fields");
        contract.Recovery.SaveConflictCause.ShouldBe(ChatBotSaveConflictCause.StaleData);
        contract.Groups.Count.ShouldBe(3);
        contract.Groups.ShouldAllBe(static row => row.GroupKey.StartsWith("sha256:"));
        contract.Groups.ShouldAllBe(static row => row.ItemCount >= 1);
        // One primary batch action per group carries the per-item count; partial-authority is a reachable disabled reason.
        contract.DisabledBatchAction.ReferencesReachableReason.ShouldBeTrue();
        contract.DisabledBatchAction.UsesTooltipOnlyReason.ShouldBeFalse();
        contract.SmallScreenFallback.IsComplete.ShouldBeTrue();
        contract.SmallScreenFallback.ReachableExplanation.ShouldNotContain("tooltip", Case.Insensitive);
        contract.ContainsRestrictedText.ShouldBeFalse();
    }

    [Fact]
    public void ApprovalQueuePriorityContractShouldRejectUnboundedGroupRows()
    {
        ChatBotApprovalQueuePriorityContract baseline = ChatBotApprovalQueuePriorityContract.CreateDefault();

        // GetHashCode-style / non-sha256 group key is rejected.
        (baseline with
        {
            Groups = [new ChatBotApprovalPriorityGroupRow("12345", "requester:r", "command:c", "project:p", "High", "risk:high|authority:send-on-behalf|age:60s", 2)],
        }).GroupsAreBounded.ShouldBeFalse();

        // Per-item count below one is rejected.
        (baseline with
        {
            Groups = [new ChatBotApprovalPriorityGroupRow("sha256:aa", "requester:r", "command:c", "project:p", "High", "risk:high|authority:send-on-behalf|age:60s", 0)],
        }).GroupsAreBounded.ShouldBeFalse();

        // A priority explanation carrying spaces (free-form text) is rejected — explanations must be safe tokens.
        (baseline with
        {
            Groups = [new ChatBotApprovalPriorityGroupRow("sha256:aa", "requester:r", "command:c", "project:p", "High", "risk high authority send", 2)],
        }).GroupsAreBounded.ShouldBeFalse();
    }

    [Fact]
    public void ApprovalQueuePriorityContractShouldRejectRowsNotOrderedHighestFirst()
    {
        ChatBotApprovalQueuePriorityContract baseline = ChatBotApprovalQueuePriorityContract.CreateDefault();
        ChatBotApprovalQueuePriorityContract misordered = baseline with
        {
            Groups =
            [
                new ChatBotApprovalPriorityGroupRow("sha256:aa", "requester:r", "command:c", "project:p", "Low", "risk:low|authority:draft-only|age:10s", 1),
                new ChatBotApprovalPriorityGroupRow("sha256:bb", "requester:r2", "command:c2", "project:p2", "Critical", "risk:blocked|authority:send-on-behalf|age:7200s", 3),
            ],
        };

        misordered.PrioritizedHighestFirst.ShouldBeFalse();
        misordered.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void ApprovalQueuePriorityComponentShouldUseLocalizedTextGroupingAndNoRestrictedMarkers()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor");

        component.ShouldContain("ChatBotUiTextKey.ApprovalQueuePriorityTitle");
        component.ShouldContain("data-approval-queue-priority=\"true\"");
        component.ShouldContain("data-approval-priority-table=\"true\"");
        component.ShouldContain("data-approval-group-row");
        component.ShouldContain("data-approval-group-item-count");
        component.ShouldContain("data-approval-priority-explanation");
        component.ShouldContain("data-approval-partial-authority-reason=\"true\"");
        component.ShouldContain("ChatBotUiTextKey.ApprovalQueuePriorityBatchApproveAction");
        component.ShouldContain("ChatBotUiTextKey.ApprovalQueuePriorityPartialAuthorityReason");
        component.ShouldContain("data-small-screen-fallback");
        component.ShouldContain("ChatBotGovernedActionState.DisabledWithReason");

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
