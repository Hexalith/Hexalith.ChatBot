using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 8.2 AC3: the operation-class dimension is a finite, bounded, low-cardinality token set. These tests lock
/// the closed taxonomy so no free-form or high-cardinality value can silently become a dimension, and so the seven
/// stable tokens stay in lockstep with the literals already flowing on the operation-status/intake/retry paths.
/// </summary>
public sealed class ChatBotOperationClassesTests
{
    [Fact]
    public void AllContainsExactlyTheSevenStableOperationClassTokens()
        => ChatBotOperationClasses.All.ShouldBe(
            new[]
            {
                ChatBotOperationClasses.MessageIntake,
                ChatBotOperationClasses.Association,
                ChatBotOperationClasses.Approval,
                ChatBotOperationClasses.CommandExecution,
                ChatBotOperationClasses.Retry,
                ChatBotOperationClasses.DuplicateHandling,
                ChatBotOperationClasses.AuditProjectionLag,
            },
            ignoreOrder: true);

    [Fact]
    public void OperationClassTokensUseTheStableDottedLiteralsSharedWithTheRestOfTheTaxonomy()
    {
        ChatBotOperationClasses.MessageIntake.ShouldBe("message-intake");
        ChatBotOperationClasses.Association.ShouldBe("association");
        ChatBotOperationClasses.Approval.ShouldBe("approval");
        ChatBotOperationClasses.CommandExecution.ShouldBe("command-execution");
        ChatBotOperationClasses.Retry.ShouldBe("retry");
        ChatBotOperationClasses.DuplicateHandling.ShouldBe("duplicate-handling");
        ChatBotOperationClasses.AuditProjectionLag.ShouldBe("audit-projection-lag");
    }

    [Theory]
    [InlineData(ChatBotOperationClasses.MessageIntake)]
    [InlineData(ChatBotOperationClasses.Association)]
    [InlineData(ChatBotOperationClasses.Approval)]
    [InlineData(ChatBotOperationClasses.CommandExecution)]
    [InlineData(ChatBotOperationClasses.Retry)]
    [InlineData(ChatBotOperationClasses.DuplicateHandling)]
    [InlineData(ChatBotOperationClasses.AuditProjectionLag)]
    public void IsKnownAcceptsEveryTokenInTheClosedSet(string operationClass)
        => ChatBotOperationClasses.IsKnown(operationClass).ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("Message-Intake")]
    [InlineData("project:acme/secret")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    public void IsKnownRejectsAnyValueOutsideTheClosedSet(string candidate)
        => ChatBotOperationClasses.IsKnown(candidate).ShouldBeFalse();
}
