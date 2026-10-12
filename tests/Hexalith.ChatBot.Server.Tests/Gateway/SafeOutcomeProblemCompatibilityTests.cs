using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Checks incompatible or malformed metadata at the public problem boundary.</summary>
public sealed class SafeOutcomeProblemCompatibilityTests
{
    /// <summary>Refused responses cannot present acceptance or undefined category values.</summary>
    /// <param name="code">The otherwise catalog-backed code.</param>
    /// <param name="category">The supplied problem category.</param>
    [Theory]
    [InlineData(ChatBotMessageCodes.CommandAccepted, ProblemDetailsCategory.Authorization_denied)]
    [InlineData(ChatBotMessageCodes.AuditUnavailable, (ProblemDetailsCategory)999999)]
    public void RefusedProblemShouldDenyIncompatibleMetadata(string code, ProblemDetailsCategory category)
    {
        ProblemDetails result = new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
        {
            Code = code, Category = category, Status = 403, Retryable = true,
            ClientAction = ProblemDetailsClientAction.RetryLater,
        });
        result.Code.ShouldBe(ChatBotMessageCodes.GenericDenial);
        result.Category.ShouldBe(ProblemDetailsCategory.Authorization_denied);
        result.Message.ShouldBe("The requested action is unavailable.");
        result.Retryable.ShouldBeFalse();
        result.ClientAction.ShouldBe(ProblemDetailsClientAction.Dismiss);
        result.Status.ShouldBe(403);
    }
}
