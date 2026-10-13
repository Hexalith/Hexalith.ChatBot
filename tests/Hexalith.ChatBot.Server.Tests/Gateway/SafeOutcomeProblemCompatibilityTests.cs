using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Checks incompatible or malformed metadata at the public problem boundary.</summary>
public sealed class SafeOutcomeProblemCompatibilityTests
{
    /// <summary>Unknown failure codes retain a safe status class without exposing a caller-supplied category.</summary>
    /// <param name="status">The incoming HTTP status.</param>
    /// <param name="expectedStatus">The safe public status.</param>
    /// <param name="expectedCategory">The safe public category.</param>
    [Theory]
    [InlineData(401, 401, ProblemDetailsCategory.Authentication_failure)]
    [InlineData(404, 403, ProblemDetailsCategory.Authorization_denied)]
    [InlineData(503, 503, ProblemDetailsCategory.Internal_error)]
    public void UnknownProblemShouldPreserveSafeStatusClass(
        int status, int expectedStatus, ProblemDetailsCategory expectedCategory)
    {
        ProblemDetails result = new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
        {
            Code = "private_resource_secret", Status = status,
            Category = ProblemDetailsCategory.Authorization_denied,
            Retryable = true, ClientAction = ProblemDetailsClientAction.RetryLater,
        });

        result.Code.ShouldBe(ChatBotMessageCodes.GenericDenial);
        result.Status.ShouldBe(expectedStatus);
        result.Category.ShouldBe(expectedCategory);
        result.Retryable.ShouldBeFalse();
        result.ClientAction.ShouldBe(ProblemDetailsClientAction.Dismiss);
        result.Message.ShouldBe("The requested action is unavailable.");
        result.Message.ShouldNotContain("private_resource_secret");
    }

    /// <summary>Refused responses cannot present acceptance or undefined category values.</summary>
    /// <param name="code">The otherwise catalog-backed code.</param>
    /// <param name="category">The supplied problem category.</param>
    [Theory]
    [InlineData(ChatBotMessageCodes.CommandAccepted, ProblemDetailsCategory.Authorization_denied)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionAccepted, ProblemDetailsCategory.Authorization_denied)]
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
