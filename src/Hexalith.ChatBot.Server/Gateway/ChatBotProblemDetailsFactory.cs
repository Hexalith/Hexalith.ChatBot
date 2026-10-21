using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

using Hexalith.ChatBot.Server.Gateway.Redaction;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class ChatBotProblemDetailsFactory : IChatBotProblemDetailsFactory
{
    private readonly IUserFacingRedactionStage _redactionStage;
    private readonly IUserFacingMessageTelemetry _telemetry;

    public ChatBotProblemDetailsFactory(
        IUserFacingRedactionStage redactionStage,
        IUserFacingMessageTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(redactionStage);
        _redactionStage = redactionStage;
        ArgumentNullException.ThrowIfNull(telemetry);
        _telemetry = telemetry;
    }

    public ProblemDetails CreateValidationProblem(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.CommandContractInvalid);

        return _redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.ValidationFailure,
            Title = entry.Headline,
            Status = StatusCodes.Status400BadRequest,
            Category = ProblemDetailsCategory.Validation_error,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    /// <summary>Returns a terminal failure without retaining the failed request as a proposal or retry.</summary>
    public ProblemDetails CreateClassifierIndeterminate(string correlationId, string? taskId)
        => CreateAdmissionFailure(ChatBotMessageCodes.ClassifierIndeterminate, ChatBotProblemTypes.ClassifierIndeterminate,
            StatusCodes.Status422UnprocessableEntity, ProblemDetailsCategory.Validation_error, correlationId, taskId);

    /// <summary>Requires a distinct governed action proposal without persisting one on this command's behalf.</summary>
    public ProblemDetails CreateApprovalRequired(string correlationId, string? taskId)
        => CreateAdmissionFailure(ChatBotMessageCodes.ApprovalRequired, ChatBotProblemTypes.ApprovalRequired,
            StatusCodes.Status409Conflict, ProblemDetailsCategory.Conflict, correlationId, taskId);

    private ProblemDetails CreateAdmissionFailure(string code, string type, int status, ProblemDetailsCategory category, string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(code);
        return _redactionStage.Apply(new ProblemDetails
        {
            Type = type, Title = entry.Headline, Status = status, Category = category,
            Code = entry.Code, Message = entry.Reason, CorrelationId = correlationId, TaskId = taskId,
            Retryable = false, ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateAuthorizationProblem(string reasonCode, string correlationId, string? taskId)
    {
        bool known = IsKnownAuthorizationReason(reasonCode);
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(
            known ? AuthorizationCatalogCode(reasonCode) : null);
        if (!known)
        {
            _telemetry.RecordUncategorizedMessage(entry.Version, entry.Code);
        }
        string catalogCode = entry.Code;
        int status = string.Equals(catalogCode, ChatBotMessageCodes.AuthenticationDenied, StringComparison.Ordinal)
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status403Forbidden;

        return _redactionStage.Apply(new ProblemDetails
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
            Retryable = known && !entry.IsTerminal && entry.NextAction == ChatBotMessageNextActions.RetryLater,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateAuditUnavailable(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AuditUnavailable);

        return _redactionStage.Apply(new ProblemDetails
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

        return _redactionStage.Apply(new ProblemDetails
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

    public ProblemDetails CreateCanonicalExecutionUnavailable(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        return _redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.CanonicalExecutionUnavailable,
            Title = entry.Headline,
            Status = StatusCodes.Status503ServiceUnavailable,
            Category = ProblemDetailsCategory.Internal_error,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = false,
            ClientAction = ClientAction(entry.NextAction),
        });
    }

    public ProblemDetails CreateDependencyUnavailable(string correlationId, string? taskId)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.DependencyDegraded);
        return _redactionStage.Apply(new ProblemDetails
        {
            Type = ChatBotProblemTypes.DependencyUnavailable,
            Title = entry.Headline,
            Status = StatusCodes.Status503ServiceUnavailable,
            Category = ProblemDetailsCategory.Internal_error,
            Code = entry.Code,
            Message = entry.Reason,
            CorrelationId = correlationId,
            TaskId = taskId,
            Retryable = true,
            ClientAction = ProblemDetailsClientAction.RetryLater,
        });
    }

    public ProblemDetails CreateIdempotencyConflict(string correlationId, string? taskId, string? catalogCode = null)
    {
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(
            string.IsNullOrWhiteSpace(catalogCode)
                ? ChatBotMessageCodes.IdempotencyConflictCommandExecution
                : catalogCode);

        return _redactionStage.Apply(new ProblemDetails
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

        return _redactionStage.Apply(new ProblemDetails
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

        return _redactionStage.Apply(new ProblemDetails
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
            ChatBotRefusalReasonCodes.ApprovalStateInvalid => ChatBotRefusalReasonCodes.CatalogCodeFor(ChatBotRefusalReasonCodes.ApprovalStateInvalid),
            ChatBotRefusalReasonCodes.ApprovedCommandScopeExceeded => ChatBotMessageCodes.RefusalBlockedAction,
            ChatBotRefusalReasonCodes.EvidenceExpired => ChatBotRefusalReasonCodes.CatalogCodeFor(ChatBotRefusalReasonCodes.EvidenceExpired),
            ChatBotMessageCodes.AssociationCorrectionAuditUnavailable => ChatBotMessageCodes.AssociationCorrectionAuditUnavailable,
            ChatBotMessageCodes.DependencyDegraded => ChatBotMessageCodes.DependencyDegraded,
            ChatBotAuthorizationReasonCodes.CommandNotAllowlisted => ChatBotRefusalReasonCodes.CatalogCodeFor(ChatBotRefusalReasonCodes.CommandNotAllowlisted),
            ChatBotAuthorizationReasonCodes.ServiceClientRateLimited => ChatBotMessageCodes.ServiceClientRateLimited,
            ChatBotAuthorizationReasonCodes.AiActorRateLimited => ChatBotMessageCodes.AiActorRateLimited,
            ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited => ChatBotMessageCodes.CommandCapabilityRateLimited,
            ChatBotDisabledActionReasons.PolicyBlocked => ChatBotMessageCodes.RefusalBlockedAction,
            _ when ChatBotMessageCatalog.IsKnown(reasonCode) => reasonCode,
            _ => ChatBotMessageCodes.AuthorizationDenied,
        };

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
            ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized or
            ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized or
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
            ChatBotAuthorizationReasonCodes.ServiceClientDisabled or
            ChatBotAuthorizationReasonCodes.ServiceClientQuarantined or
            ChatBotAuthorizationReasonCodes.ServiceClientRateLimited or
            ChatBotAuthorizationReasonCodes.AiActorDisabled or
            ChatBotAuthorizationReasonCodes.AiActorQuarantined or
            ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled or
            ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined or
            ChatBotAuthorizationReasonCodes.AiActorRateLimited or
            ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited or
            ChatBotDisabledActionReasons.InsufficientAuthority or
            ChatBotDisabledActionReasons.PolicyBlocked or
            ChatBotRefusalReasonCodes.ApprovalStateInvalid or
            ChatBotRefusalReasonCodes.ApprovedCommandScopeExceeded or
            ChatBotRefusalReasonCodes.CommandNotAllowlisted or
            ChatBotMessageCodes.ProjectAiContextPackageUnavailable or
            ChatBotRefusalReasonCodes.EvidenceExpired;

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
