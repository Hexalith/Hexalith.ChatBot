using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.Admin;
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
    public const string TenantAdminValue = AdminRoles.TenantAdmin;

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
            !AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitTenantPolicyChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidTenantPolicyChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveTenantPolicyChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidTenantPolicyApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitMailboxConfigurationChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxConfigurationChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(RecordMailboxProviderConnection), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxProviderConnection(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(AssignTenantAdminRole), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidAdminAssignment(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ExecuteAdminQueueOperation), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Operate) ||
                !IsValidAdminQueueOperation(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
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
                string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(RequestOutboundSendApproval), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(DecideOutboundApproval), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal)) &&
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

        if (string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal))
        {
            ExecuteApprovedOutboundDraft? command = ReadExecuteApprovedOutboundDraft(submission.Request.Command);
            if (command is null)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            if (!string.Equals(command.SendActorId, actor.ActorId, StringComparison.Ordinal))
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            var classification = OutboundSendAuthorityEvaluator.Classify(command, actor.Principal, tenantBinding.TenantId, grantResult.ServiceClientGrantEvidence);
            if (classification.DenialReason is not null ||
                classification.AuthorityClass != command.SenderAuthorityClass)
            {
                return ChatBotAuthorizationResult.Denied(OutboundSendAuthorityEvaluator.SafeDenialReason(command, actor.Principal, classification));
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

    private static ExecuteApprovedOutboundDraft? ReadExecuteApprovedOutboundDraft(object? command)
    {
        if (command is ExecuteApprovedOutboundDraft typed)
        {
            return typed;
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<ExecuteApprovedOutboundDraft>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            : null;
    }

    private static bool IsValidAdminAssignment(object? command)
    {
        AssignTenantAdminRole? assignment = ReadAssignTenantAdminRole(command);
        return assignment is not null &&
            AdminRoles.All.Contains(assignment.Role) &&
            IsSafeAdminToken(assignment.AssignmentId) &&
            IsSafeAdminToken(assignment.TargetActorId) &&
            IsSafeAdminToken(assignment.ReasonCode) &&
            IsSafeAdminToken(assignment.PolicySnapshotId) &&
            assignment.SourceVersion >= 0;
    }

    private static bool IsValidTenantPolicyChange(object? command)
    {
        SubmitTenantPolicyChange? change = ReadSubmitTenantPolicyChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            IsSafeAdminToken(change.PolicyChangeId) &&
            IsSafeAdminToken(change.SourcePolicySnapshotId) &&
            IsSafeAdminToken(change.ProposedPolicySnapshotId) &&
            IsSafeAdminToken(change.ReasonCode) &&
            IsSafeAdminToken(change.RequesterRef) &&
            TenantPolicySchemaVersions.IsKnown(change.SchemaVersion) &&
            IsSafeAdminToken(change.CorrelationId) &&
            IsSafeAdminToken(change.OldValueFingerprint) &&
            IsSafeAdminToken(change.NewValueFingerprint) &&
            IsValidChangedKnobs(change.ChangedKnobIds, change.ChangeSet) &&
            TenantPolicySchema.Validate(change.ChangeSet).IsValid;
    }

    private static bool IsValidTenantPolicyApproval(object? command)
    {
        ApproveTenantPolicyChange? approval = ReadApproveTenantPolicyChange(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.PolicyChangeId) &&
            IsSafeAdminToken(approval.PendingPolicySnapshotId) &&
            IsSafeAdminToken(approval.ActivatedPolicySnapshotId) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            TenantPolicySchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.ChangedKnobIds is { Count: > 0 } &&
            approval.ChangedKnobIds.All(static knob => TenantPolicySchema.TryGetDefinition(knob, out _));
    }

    private static bool IsValidMailboxConfigurationChange(object? command)
    {
        SubmitMailboxConfigurationChange? change = ReadSubmitMailboxConfigurationChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            IsSafeAdminToken(change.ConfigurationChangeId) &&
            IsSafeAdminToken(change.SourceConfigurationSnapshotId) &&
            IsSafeAdminToken(change.ProposedConfigurationSnapshotId) &&
            IsSafeAdminToken(change.ReasonCode) &&
            IsSafeAdminToken(change.RequesterRef) &&
            MailboxConfigurationSchemaVersions.IsKnown(change.SchemaVersion) &&
            IsSafeAdminToken(change.CorrelationId) &&
            MailboxConfigurationSchema.IsSafeFingerprint(change.OldConfigurationFingerprint) &&
            MailboxConfigurationSchema.IsSafeFingerprint(change.NewConfigurationFingerprint) &&
            MailboxConfigurationSchema.Validate(change.ChangeSet).IsValid;
    }

    private static bool IsValidMailboxProviderConnection(object? command)
    {
        RecordMailboxProviderConnection? connection = ReadRecordMailboxProviderConnection(command);
        return connection is not null &&
            connection.SourceVersion >= 0 &&
            IsSafeAdminToken(connection.ProviderConnectionChangeId) &&
            IsSafeAdminToken(connection.ProviderConnectionRef) &&
            connection.ProviderKind is not MailboxProviderKind.Unknown &&
            Enum.IsDefined(connection.ProviderKind) &&
            MailboxConfigurationSchema.IsSafeFingerprint(connection.CredentialFingerprint) &&
            IsSafeAdminToken(connection.PermissionEvidenceRef) &&
            connection.Freshness is not MailboxPermissionFreshnessState.Unknown &&
            Enum.IsDefined(connection.Freshness) &&
            IsSafeAdminToken(connection.ReasonCode) &&
            IsSafeAdminToken(connection.RequesterRef) &&
            MailboxConfigurationSchemaVersions.IsKnown(connection.SchemaVersion) &&
            IsSafeAdminToken(connection.CorrelationId) &&
            IsSafeAdminToken(connection.PolicySnapshotId);
    }

    private static bool IsValidChangedKnobs(IReadOnlyList<string>? changedKnobIds, TenantPolicyChangeSet? changeSet)
    {
        if (changedKnobIds is not { Count: > 0 } || changeSet?.Values is not { Count: > 0 })
        {
            return false;
        }

        HashSet<string> changed = changedKnobIds.ToHashSet(StringComparer.Ordinal);
        HashSet<string> values = changeSet.Values.Select(static value => value.KnobId).ToHashSet(StringComparer.Ordinal);
        return changed.SetEquals(values) &&
            changed.All(static knob => TenantPolicySchema.TryGetDefinition(knob, out _));
    }

    private static SubmitTenantPolicyChange? ReadSubmitTenantPolicyChange(object? command)
    {
        if (command is SubmitTenantPolicyChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitTenantPolicyChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveTenantPolicyChange? ReadApproveTenantPolicyChange(object? command)
    {
        if (command is ApproveTenantPolicyChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveTenantPolicyChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitMailboxConfigurationChange? ReadSubmitMailboxConfigurationChange(object? command)
    {
        if (command is SubmitMailboxConfigurationChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitMailboxConfigurationChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static RecordMailboxProviderConnection? ReadRecordMailboxProviderConnection(object? command)
    {
        if (command is RecordMailboxProviderConnection typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<RecordMailboxProviderConnection>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static AssignTenantAdminRole? ReadAssignTenantAdminRole(object? command)
    {
        if (command is AssignTenantAdminRole typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<AssignTenantAdminRole>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidAdminQueueOperation(object? command)
    {
        ExecuteAdminQueueOperation? operation = ReadExecuteAdminQueueOperation(command);
        return operation is not null &&
            AdminQueueOperations.All.Contains(operation.Operation) &&
            operation.ScopeUsed == AdminScope.Operate &&
            IsSafeAdminToken(operation.OperationId) &&
            IsSafeAdminToken(operation.QueueRef) &&
            IsAllowedAdminQueueReason(operation.ReasonCode) &&
            IsSafeAdminToken(operation.PolicySnapshotId) &&
            IsSafeAdminToken(operation.RedactionState) &&
            operation.SourceVersion >= 0 &&
            operation.ItemCount > 0 &&
            operation.ItemRefs is not null &&
            operation.ItemRefs.All(IsSafeAdminToken) &&
            (operation.ItemRefs.Count == 0 || operation.ItemRefs.Count == operation.ItemCount);
    }

    private static ExecuteAdminQueueOperation? ReadExecuteAdminQueueOperation(object? command)
    {
        if (command is ExecuteAdminQueueOperation typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ExecuteAdminQueueOperation>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsSafeAdminToken(string? value)
        => AuditMetadata.SafeOptionalToken(value) is not null;

    private static bool IsAllowedAdminQueueReason(string? reasonCode)
        => reasonCode is ChatBotDisabledActionReasons.DependencyDegraded
            or ChatBotDisabledActionReasons.InsufficientAuthority
            or ChatBotDisabledActionReasons.PolicyBlocked
            or ChatBotAuthorizationReasonCodes.AuthorizationDenied;

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
