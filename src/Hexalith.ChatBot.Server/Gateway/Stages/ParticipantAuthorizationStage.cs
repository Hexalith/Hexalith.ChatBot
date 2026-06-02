using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.Outbound;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ParticipantAuthorizationStage(
    IAssociationCorrectionDependencyReadiness? correctionDependencyReadiness = null,
    IServiceClientGrantValidator? serviceClientGrantValidator = null) : IAuthorizationStage
{
    private readonly IAssociationCorrectionDependencyReadiness _correctionDependencyReadiness =
        correctionDependencyReadiness ?? new StaticAssociationCorrectionDependencyReadiness(AssociationCorrectionDependencyReadinessStatus.Ready);
    private readonly IServiceClientGrantValidator _serviceClientGrantValidator =
        serviceClientGrantValidator ?? new PassThroughServiceClientGrantValidator();
    public const string ParticipantAuthorityClaim = "chatbot:participant-authority";
    public const string UnresolvedValue = "unresolved";
    public const string EmailOnlyValue = "email-only";
    public const string UnauthorizedValue = "unauthorized";
    public const string DirectoryDegradedValue = "directory-degraded";
    public const string ActorTypeClaim = "chatbot:actor-type";
    public const string TenantRoleClaim = "chatbot:tenant-role";
    public const string ProjectOwnerClaim = "chatbot:project-owner";
    public const string HumanActorValue = "human";
    public const string ServiceActorValue = "service";
    public const string AiActorValue = "ai";
    public const string TenantAdminValue = "tenant-admin";

    public async ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        cancellationToken.ThrowIfCancellationRequested();

        ChatBotAuthorizationResult grantResult = await _serviceClientGrantValidator
            .ValidateAsync(submission, actor, tenantBinding, cancellationToken)
            .ConfigureAwait(false);
        if (!grantResult.IsAllowed)
        {
            return grantResult;
        }

        string[] authorities = actor.Principal
            .FindAll(ParticipantAuthorityClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (authorities.Contains(DirectoryDegradedValue, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded);
        }

        if (authorities.Contains(UnresolvedValue, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.UnresolvedParticipant);
        }

        if (authorities.Contains(EmailOnlyValue, StringComparer.Ordinal) ||
            authorities.Contains(UnauthorizedValue, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.UnauthorizedParticipant);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SetAssociationConfidenceThresholds), StringComparison.Ordinal) &&
            !IsTenantAdminHuman(actor.Principal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal) &&
            !_correctionDependencyReadiness.IsProjectionInvalidationReady)
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AssociationCorrectionProjectionUnavailable);
        }

        if (string.Equals(submission.Request.CommandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal) &&
            !CanCorrectAssociation(actor.Principal, submission.Request.Command))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AssociationCorrectionTargetUnauthorized);
        }

        if ((string.Equals(submission.Request.CommandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal)) &&
            !CanReadProject(actor.Principal, submission.Request.Command))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(CreateOutboundDraft), StringComparison.Ordinal))
        {
            CreateOutboundDraft? command = ReadCreateOutboundDraft(submission.Request.Command);
            if (command is null)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            if (!HasTrustedOutboundDraftOrigin(command, actor, grantResult.ServiceClientGrantEvidence))
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            var classification = OutboundDraftAuthorityEvaluator.Classify(command, actor.Principal, tenantBinding.TenantId);
            if (classification.DenialReason is not null ||
                command.SenderAuthorityClass is not SenderAuthorityClass.DraftOnly)
            {
                return ChatBotAuthorizationResult.Denied(OutboundDraftAuthorityEvaluator.SafeDenialReason(command, actor.Principal, classification));
            }
        }

        return ChatBotAuthorizationResult.Allowed(grantResult.ServiceClientGrantEvidence);
    }

    private static CreateOutboundDraft? ReadCreateOutboundDraft(object? command)
    {
        if (command is CreateOutboundDraft typed)
        {
            return typed;
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<CreateOutboundDraft>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            : null;
    }

    private static bool HasTrustedOutboundDraftOrigin(
        CreateOutboundDraft command,
        ChatBotAuthenticatedActor actor,
        ServiceClientGrantEvidence? serviceClientGrantEvidence)
    {
        if (!string.Equals(command.SourceActorId, actor.ActorId, StringComparison.Ordinal))
        {
            return false;
        }

        bool isServiceOrAiActor =
            string.Equals(actor.ActorType, ServiceActorValue, StringComparison.Ordinal) ||
            string.Equals(actor.ActorType, AiActorValue, StringComparison.Ordinal);
        if (!isServiceOrAiActor)
        {
            return true;
        }

        return serviceClientGrantEvidence is not null &&
            !string.IsNullOrWhiteSpace(serviceClientGrantEvidence.DelegatedUserId) &&
            string.Equals(command.RequesterId, serviceClientGrantEvidence.DelegatedUserId, StringComparison.Ordinal);
    }

    private static bool IsTenantAdminHuman(ClaimsPrincipal principal)
        => principal.HasClaim(ActorTypeClaim, HumanActorValue) &&
            principal.HasClaim(TenantRoleClaim, TenantAdminValue);

    private static bool CanCorrectAssociation(ClaimsPrincipal principal, object? command)
    {
        if (!principal.HasClaim(ActorTypeClaim, HumanActorValue))
        {
            return false;
        }

        (string? PriorProjectId, string? TargetProjectId) projects = CorrectionProjects(command);
        if (string.IsNullOrWhiteSpace(projects.PriorProjectId) ||
            string.IsNullOrWhiteSpace(projects.TargetProjectId))
        {
            return false;
        }

        string[] ownedProjects = principal
            .FindAll(ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return ownedProjects.Contains("*", StringComparer.Ordinal) ||
            (ownedProjects.Contains(projects.PriorProjectId, StringComparer.Ordinal) &&
                ownedProjects.Contains(projects.TargetProjectId, StringComparer.Ordinal));
    }

    private static (string? PriorProjectId, string? TargetProjectId) CorrectionProjects(object? command)
    {
        if (command is CorrectEmailProjectAssociation typed)
        {
            return (typed.PriorProjectId, typed.TargetProjectId);
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (element.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        string? priorProjectId = element.TryGetProperty("priorProjectId", out JsonElement priorProject) &&
            priorProject.ValueKind == JsonValueKind.String
                ? priorProject.GetString()
                : null;
        string? targetProjectId = element.TryGetProperty("targetProjectId", out JsonElement targetProject) &&
            targetProject.ValueKind == JsonValueKind.String
                ? targetProject.GetString()
                : null;
        return (priorProjectId, targetProjectId);
    }

    private static bool CanReadProject(ClaimsPrincipal principal, object? command)
    {
        string? projectId = CommandProjectId(command);
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return false;
        }

        string[] projectClaims = principal
            .FindAll(ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return projectClaims.Contains("*", StringComparer.Ordinal) ||
            projectClaims.Contains(projectId, StringComparer.Ordinal);
    }

    private static string? CommandProjectId(object? command)
    {
        if (command is ExecuteLowRiskAIAssistance typed)
        {
            return typed.ProjectId;
        }

        if (command is ExecuteApprovedAIAction approved)
        {
            return approved.ProjectId;
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("projectId", out JsonElement projectId) &&
            projectId.ValueKind == JsonValueKind.String
                ? projectId.GetString()
                : null;
    }

    private sealed class PassThroughServiceClientGrantValidator : IServiceClientGrantValidator
    {
        public ValueTask<ChatBotAuthorizationResult> ValidateAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ChatBotAuthorizationResult.Allowed());
    }
}
