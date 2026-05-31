using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotCognitiveLoadContractTests
{
    private static readonly string[] CanonicalOrder = ["Evidence", "Risk", "Status", "Actor", "Timestamp"];

    [Fact]
    public void CognitiveLoadContractShouldExposeCanonicalCrossSurfaceFieldOrder()
    {
        ChatBotCognitiveLoadContract.CanonicalFieldOrder.ShouldBe(CanonicalOrder, ignoreOrder: false);
        ChatBotCognitiveLoadContract.AppliesToSurfaces.ShouldBe(["candidate rows", "proposals", "queues", "audit entries"], ignoreOrder: false);
    }

    [Fact]
    public void WorkflowItemShouldAllowOnlyOnePrimaryActionAndGroupSecondaryThenDestructive()
    {
        ChatBotCognitiveLoadContract valid = new(
            SummaryText: "Pending governed note review",
            RawIdentifier: "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            FieldsInDisplayOrder: ["Evidence", "Risk", "Status", "Actor", "Timestamp"],
            Actions:
            [
                new("Approve governed operation", ChatBotWorkflowItemActionKind.Primary),
                new("Defer governed operation", ChatBotWorkflowItemActionKind.Secondary),
                new("Reject governed operation", ChatBotWorkflowItemActionKind.Destructive),
            ],
            ActiveFilterSummary: "Filter: Pending review. 2 results.",
            ResultCount: 2,
            ConsolidatedStateMessage: "Projection pending; audit metadata committed.");

        valid.IsComplete.ShouldBeTrue();
        valid.HasExactlyOnePrimaryAction.ShouldBeTrue();
        valid.HasSummaryBeforeRawIdentifier.ShouldBeTrue();
        valid.HasCanonicalFieldOrder.ShouldBeTrue();

        (valid with { Actions = [.. valid.Actions, new("Escalate", ChatBotWorkflowItemActionKind.Primary)] }).IsComplete.ShouldBeFalse();
        (valid with { FieldsInDisplayOrder = ["Risk", "Evidence", "Status", "Actor", "Timestamp"] }).IsComplete.ShouldBeFalse();
        (valid with { ActiveFilterSummary = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { ResultCount = null }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void CurrentFixtureShouldPutPlainLanguageBeforeMachineIdentifiers()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        page.IndexOf("OperationStatus_Summary", StringComparison.Ordinal).ShouldBeLessThan(page.IndexOf("OperationLabel", StringComparison.Ordinal));
        page.IndexOf("AuditHistory_MetadataOnly", StringComparison.Ordinal).ShouldBeLessThan(page.IndexOf("AuditHistoryTitle", StringComparison.Ordinal));
        page.ShouldContain("chatbot-labelled-row-list");
    }

    [Fact]
    public void ActiveFilterSummaryShouldExtendQueueLoadingContract()
    {
        ChatBotQueueLoadingContract queue = new(
            Mode: ChatBotQueueLoadingMode.Pagination,
            ActiveFilterDescription: "Pending review",
            ResultCount: 2,
            PageNumber: 1,
            PageSize: 25);

        queue.IsValidOperationalQueueContract.ShouldBeTrue();
        queue.HasVisibleActiveFilterSummary.ShouldBeTrue();
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
