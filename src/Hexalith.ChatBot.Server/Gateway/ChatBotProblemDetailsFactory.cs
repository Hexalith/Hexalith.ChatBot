using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

using Hexalith.ChatBot.Server.Gateway.Redaction;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class ChatBotProblemDetailsFactory(
    IUserFacingRedactionStage redactionStage,
    IUserFacingMessageTelemetry telemetry) : IChatBotProblemDetailsFactory
{
    public ProblemDetails CreateAuthorizationProblem(string reasonCode, string correlationId, string? taskId)
    {
        string catalogCode = AuthorizationCatalogCode(reasonCode);
        if (!IsKnownAuthorizationReason(reasonCode))
        {
            telemetry.RecordUncategorizedMessage(ChatBotMessageCatalogVersion.Current, catalogCode);
        }

        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(catalogCode);
        int status = string.Equals(catalogCode, ChatBotMessageCodes.AuthenticationDenied, StringComparison.Ordinal)
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status403Forbidden;

        return redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.AuthorizationDenied,
            Title = entry.Headline,
            Status = status,
            Category = status == StatusCodes.Status401Unauthorized
                ? ProblemDetailsCategory.Authentication_failure
                : ProblemDetailsCategory.Authorization_denied,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = IsTransientAuthorizationReason(reasonCode),
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateAuditUnavailable(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AuditUnavailable);

        return redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.AuditUnavailable,
            Title = entry.Headline,
            Status = StatusCodes.Status503ServiceUnavailable,
            Category = ProblemDetailsCategory.Internal_error,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = true,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateDispatchUnavailable(string correlationId, string? taskId)
    {
        // A dispatch outage (EventStore gateway unreachable / non-2xx) or an unreadable command payload is a
        // fail-closed, transient internal error: no durable state was written. Reuse the catalog-backed,
        // redacted audit-unavailable copy (RetryLater) so no raw exception text ever reaches the caller (NFR40).
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AuditUnavailable);

        return redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.DispatchUnavailable,
            Title = entry.Headline,
            Status = StatusCodes.Status503ServiceUnavailable,
            Category = ProblemDetailsCategory.Internal_error,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = true,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateIdempotencyConflict(string correlationId, string? taskId, string? catalogCode = null)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(
            string.IsNullOrWhiteSpace(catalogCode)
                ? ChatBotMessageCodes.IdempotencyConflictCommandExecution
                : catalogCode);

        return redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.IdempotencyConflict,
            Title = entry.Headline,
            Status = StatusCodes.Status409Conflict,
            Category = ProblemDetailsCategory.Conflict,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateInvalidLifecycleTransition(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.InvalidLifecycleTransition);

        return redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.InvalidLifecycleTransition,
            Title = entry.Headline,
            Status = StatusCodes.Status409Conflict,
            Category = ProblemDetailsCategory.Conflict,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateCommandNotAllowlisted(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotRefusalReasonCodes.CatalogEntryFor(ChatBotRefusalReasonCodes.CommandNotAllowlisted);

        return redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.CommandNotAllowlisted,
            Title = entry.Headline,
            Status = StatusCodes.Status403Forbidden,
            Category = ProblemDetailsCategory.Authorization_denied,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    private static string AuthorizationCatalogCode(string reasonCode)
        => reasonCode switch
        {
            ChatBotAuthorizationReasonCodes.AuthenticationDenied => ChatBotMessageCodes.AuthenticationDenied,
            ChatBotAuthorizationReasonCodes.UnresolvedParticipant => ChatBotMessageCodes.UnresolvedParticipant,
            ChatBotAuthorizationReasonCodes.UnauthorizedParticipant => ChatBotMessageCodes.UnauthorizedParticipant,
            ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded => ChatBotMessageCodes.ParticipantDirectoryDegraded,
            ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized => ChatBotMessageCodes.UnauthorizedThresholdUpdate,
            ChatBotAuthorizationReasonCodes.AssociationCorrectionTargetUnauthorized => ChatBotMessageCodes.AssociationCorrectionTargetUnauthorizedSuppressed,
            ChatBotAuthorizationReasonCodes.AssociationCorrectionProjectionUnavailable => ChatBotMessageCodes.AssociationCorrectionProjectionUnavailable,
            ChatBotAuthorizationReasonCodes.AssociationCorrectionWorkflowUnavailable => ChatBotMessageCodes.AssociationCorrectionWorkflowUnavailable,
            ChatBotMessageCodes.AssociationCorrectionAuditUnavailable => ChatBotMessageCodes.AssociationCorrectionAuditUnavailable,
            ChatBotMessageCodes.DependencyDegraded => ChatBotMessageCodes.DependencyDegraded,
            ChatBotAuthorizationReasonCodes.CommandNotAllowlisted => ChatBotRefusalReasonCodes.CatalogCodeFor(ChatBotRefusalReasonCodes.CommandNotAllowlisted),
            ChatBotAuthorizationReasonCodes.AiActorRateLimited => ChatBotMessageCodes.AiActorRateLimited,
            ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited => ChatBotMessageCodes.CommandCapabilityRateLimited,
            _ => ChatBotMessageCodes.AuthorizationDenied,
        };

    private static bool IsTransientAuthorizationReason(string reasonCode)
        => reasonCode is ChatBotAuthorizationReasonCodes.AiActorRateLimited
            or ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited;

    private static bool IsKnownAuthorizationReason(string reasonCode)
        => reasonCode is
            ChatBotAuthorizationReasonCodes.AuthenticationDenied or
            ChatBotAuthorizationReasonCodes.TenantMissing or
            ChatBotAuthorizationReasonCodes.TenantMismatch or
            ChatBotAuthorizationReasonCodes.AuthorizationDenied or
            ChatBotAuthorizationReasonCodes.SafeNotFound or
            ChatBotAuthorizationReasonCodes.UnresolvedParticipant or
            ChatBotAuthorizationReasonCodes.UnauthorizedParticipant or
            ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded or
            ChatBotAuthorizationReasonCodes.CommandNotAllowlisted or
            ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized or
            ChatBotAuthorizationReasonCodes.AssociationCorrectionTargetUnauthorized or
            ChatBotAuthorizationReasonCodes.AssociationCorrectionProjectionUnavailable or
            ChatBotAuthorizationReasonCodes.AssociationCorrectionWorkflowUnavailable or
            ChatBotMessageCodes.AssociationCorrectionAuditUnavailable or
            ChatBotMessageCodes.DependencyDegraded or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantAmbiguous or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped or
            ChatBotAuthorizationReasonCodes.ServiceClientGrantTenantMismatch or
            ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface or
            ChatBotAuthorizationReasonCodes.AiActorRateLimited or
            ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited;

    private static ProblemDetailsClientAction ClientAction(string action)
        => action switch
        {
            ChatBotMessageNextActions.Authenticate => ProblemDetailsClientAction.Authenticate,
            ChatBotMessageNextActions.CorrectRequest => ProblemDetailsClientAction.CorrectRequest,
            ChatBotMessageNextActions.RetryLater => ProblemDetailsClientAction.RetryLater,
            ChatBotMessageNextActions.RequestAccess => ProblemDetailsClientAction.RequestAccess,
            ChatBotMessageNextActions.Escalate => ProblemDetailsClientAction.Escalate,
            ChatBotMessageNextActions.Dismiss => ProblemDetailsClientAction.Dismiss,
            _ => ProblemDetailsClientAction.None,
        };
}
