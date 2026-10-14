using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Checks incompatible or malformed metadata at the public problem boundary.</summary>
public sealed class SafeOutcomeProblemCompatibilityTests
{
    /// <summary>Every admitted operation class retains specific safe conflict guidance.</summary>
    [Fact]
    public void EveryIdempotencyOperationClassShouldHaveCatalogBackedConflict()
    {
        foreach (CoarseIdempotencyOperationClass operation in CoarseIdempotencyOperationClass.All)
        {
            ChatBotMessageCatalog.IsKnown(operation.ConflictCode).ShouldBeTrue(operation.Code);
            ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(operation.ConflictCode);
            ProblemDetails result = new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
            {
                Code = operation.ConflictCode, Type = ChatBotProblemTypes.IdempotencyConflict, Status = 409,
                Category = ProblemDetailsCategory.Conflict, ClientAction = ProblemDetailsClientAction.None,
            });
            result.Code.ShouldBe(operation.ConflictCode);
            result.Title.ShouldBe(entry.Headline);
            result.Message.ShouldBe(entry.Reason);
            result.Type.ShouldBe(ChatBotProblemTypes.IdempotencyConflict);
            result.ClientAction.ShouldBe(ProblemDetailsClientAction.None);
        }
    }

    /// <summary>Unknown failure codes retain a safe status class without exposing a caller-supplied category.</summary>
    /// <param name="status">The incoming HTTP status.</param>
    /// <param name="expectedStatus">The safe public status.</param>
    /// <param name="expectedCategory">The safe public category.</param>
    [Theory]
    [InlineData(401, 401, ProblemDetailsCategory.Authentication_failure)]
    [InlineData(404, 403, ProblemDetailsCategory.Authorization_denied)]
    [InlineData(409, 409, ProblemDetailsCategory.Conflict)]
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
        result.Type.ShouldBe(ChatBotProblemTypes.GenericDenial);
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

    /// <summary>Generic denial cannot retain a specific conflict cause from either a code or a supplied type.</summary>
    /// <param name="code">An unknown, generic, or incompatible outcome.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("restricted-conflict-secret")]
    [InlineData(ChatBotMessageCodes.GenericDenial)]
    [InlineData(ChatBotMessageCodes.AssociationDecisionAccepted)]
    public void GenericConflictDenialShouldWithholdSpecificProblemType(string? code)
    {
        ProblemDetails result = new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
        {
            Code = code!, Type = ChatBotProblemTypes.IdempotencyConflict, Status = 409,
            Category = ProblemDetailsCategory.Conflict, Retryable = true,
            ClientAction = ProblemDetailsClientAction.Dismiss,
        });

        result.Type.ShouldBe(ChatBotProblemTypes.GenericDenial);
        result.Status.ShouldBe(409);
        result.Category.ShouldBe(ProblemDetailsCategory.Conflict);
        result.Code.ShouldBe(ChatBotMessageCodes.GenericDenial);
        result.Title.ShouldBe(ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.GenericDenial).Headline);
        result.Message.ShouldBe(ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.GenericDenial).Reason);
        result.Retryable.ShouldBeFalse();
        result.ClientAction.ShouldBe(ProblemDetailsClientAction.Dismiss);
        result.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        result.SchemaVersion.ShouldBe(ChatBotMessageCatalogVersion.Current);
    }

    /// <summary>A recognized conflict still carries its catalog-backed cause and existing type.</summary>
    /// <param name="code">The established idempotency conflict code.</param>
    [Theory]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictCommandExecution)]
    [InlineData(ChatBotMessageCodes.IdempotencyConflictMessageIntake)]
    public void KnownConflictShouldKeepSpecificProblemType(string code)
    {
        ProblemDetails result = new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
        {
            Code = code, Type = ChatBotProblemTypes.IdempotencyConflict, Status = 409,
            Category = ProblemDetailsCategory.Conflict, ClientAction = ProblemDetailsClientAction.None,
        });

        result.Type.ShouldBe(ChatBotProblemTypes.IdempotencyConflict);
        result.Status.ShouldBe(409);
        result.Category.ShouldBe(ProblemDetailsCategory.Conflict);
        result.Code.ShouldBe(code);
        result.Message.ShouldBe(ChatBotMessageCatalog.Resolve(code).Reason);
        result.Retryable.ShouldBeFalse();
        result.ClientAction.ShouldBe(ProblemDetailsClientAction.None);
    }

    /// <summary>Explicit and resolved generic denial ignore incompatible caller actions.</summary>
    /// <param name="explicitGeneric">Whether the caller supplies the generic catalog code.</param>
    /// <param name="action">The incompatible caller action.</param>
    [Theory]
    [InlineData(true, ProblemDetailsClientAction.None)]
    [InlineData(true, ProblemDetailsClientAction.Authenticate)]
    [InlineData(true, ProblemDetailsClientAction.RetryLater)]
    [InlineData(true, (ProblemDetailsClientAction)999999)]
    [InlineData(false, ProblemDetailsClientAction.None)]
    [InlineData(false, ProblemDetailsClientAction.Authenticate)]
    [InlineData(false, ProblemDetailsClientAction.RetryLater)]
    [InlineData(false, (ProblemDetailsClientAction)999999)]
    public void GenericDenialShouldAlwaysDismiss(bool explicitGeneric, ProblemDetailsClientAction action)
    {
        ProblemDetails result = new CoarseUserFacingRedactionStage().Apply(new ProblemDetails
        {
            Code = explicitGeneric ? ChatBotMessageCodes.GenericDenial : "unknown-private-code",
            Type = ChatBotProblemTypes.AuthorizationDenied, Status = 403,
            Category = ProblemDetailsCategory.Authorization_denied,
            Retryable = true, ClientAction = action,
        });

        result.Code.ShouldBe(ChatBotMessageCodes.GenericDenial);
        result.Type.ShouldBe(ChatBotProblemTypes.GenericDenial);
        result.Retryable.ShouldBeFalse();
        result.ClientAction.ShouldBe(ProblemDetailsClientAction.Dismiss);
    }
}
