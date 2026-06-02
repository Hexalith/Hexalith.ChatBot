using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

namespace Hexalith.ChatBot.Server.Audit;

internal static class AuditEnvelopeFactory
{
    private const string EnvelopeSchemaVersion = "chatbot.audit-envelope.v1";
    private const string NoPayloadPolicySnapshotId = "chatbot.gateway.policy-snapshot.v1";

    public static AuditEnvelope PreCommit(
        ChatBotGatewayContext context,
        LifecycleTransitionDefinition transition,
        DateTimeOffset timestamp)
        => Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "allow",
            reasonCode: "pre_commit_gate",
            stateTransition: transition.ToString(),
            outcome: "gate_passed");

    public static AuditEnvelope PostCommit(
        ChatBotGatewayContext context,
        ChatBotDispatchResult dispatchResult,
        LifecycleTransitionDefinition transition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(dispatchResult);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "allow",
            reasonCode: "eventstore_dispatch_accepted",
            stateTransition: transition.ToString(),
            outcome: "proposed",
            resourceId: dispatchResult.ResourceId);
    }

    public static AuditEnvelope DuplicateMailboxIntakeSuppressed(ChatBotGatewayContext context, DateTimeOffset timestamp)
        => Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "suppress",
            reasonCode: "duplicate_provider_message",
            stateTransition: "Received->Skipped",
            outcome: "duplicate_suppressed");

    public static AuditEnvelope RejectedLifecycleTransition(
        ChatBotGatewayContext context,
        LifecycleTransitionValidation transition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "reject",
            reasonCode: transition.ReasonCode,
            stateTransition: transition.Transition.ToString(),
            outcome: "rejected");
    }

    private static AuditEnvelope Create(
        ChatBotGatewayContext context,
        DateTimeOffset timestamp,
        AuditCommitPhase phase,
        string decision,
        string reasonCode,
        string stateTransition,
        string outcome,
        string? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        string commandName = CommandName(context);

        // The post-commit envelope references the durable aggregate identity (the dispatched NoteId) when the
        // dispatcher resolved one; pre-commit / rejection envelopes have no aggregate yet and fall back to the
        // command id. The value is still a safe, metadata-only ULID token (no payload).
        string auditedResourceId = AuditMetadata.IsSafeStableIdentifier(resourceId)
            ? resourceId!
            : context.Submission.Request.CommandId;

        return new AuditEnvelope(
            context.TenantBinding.TenantId,
            context.Actor.ActorId,
            ActorType(context),
            commandName,
            auditedResourceId,
            decision,
            reasonCode,
            context.Submission.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            SourceEvidenceRefs(context, phase),
            AuditMetadata.SafeOptionalToken(IdempotencyKey(context)),
            stateTransition,
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            outcome,
            phase,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(context.Submission.Origin));
    }

    private static string CommandName(ChatBotGatewayContext context)
    {
        string? runtimeTypeName = context.Submission.Request.Command?.GetType().Name;
        if (!string.IsNullOrWhiteSpace(runtimeTypeName) &&
            !string.Equals(runtimeTypeName, "JsonElement", StringComparison.Ordinal))
        {
            return AuditMetadata.SafeCommandName(runtimeTypeName);
        }

        return AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
    }

    private static string ActorType(ChatBotGatewayContext context)
    {
        string? actorType = context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, ParticipantAuthorizationStage.ActorTypeClaim, StringComparison.Ordinal))?
            .Value;

        actorType ??= context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, "actor_type", StringComparison.Ordinal))?
            .Value;

        return AuditMetadata.SafeActorType(actorType ?? context.Actor.ActorType);
    }

    private static IReadOnlyList<string> SourceEvidenceRefs(ChatBotGatewayContext context, AuditCommitPhase phase)
    {
        List<string> refs =
        [
            $"command:{context.Submission.Request.CommandId}",
            $"correlation:{context.Submission.CorrelationId}",
            $"phase:{PhaseName(phase)}",
        ];

        refs.AddRange(AssociationDecisionEvidenceRefs(context));
        refs.AddRange(AssociationCorrectionEvidenceRefs(context));
        refs.AddRange(AiActionClassificationEvidenceRefs(context));
        refs.AddRange(LowRiskAiAssistanceEvidenceRefs(context));
        refs.AddRange(ApprovalDecisionEvidenceRefs(context));
        refs.AddRange(ApprovedAiActionExecutionEvidenceRefs(context));
        refs.AddRange(OutboundDraftEvidenceRefs(context));
        refs.AddRange(OutboundApprovalEvidenceRefs(context));
        refs.AddRange(OutboundSendEvidenceRefs(context));
        refs.AddRange(MailboxIntakeEvidenceRefs(context));
        refs.AddRange(ServiceClientGrantEvidenceRefs(context));
        return refs;
    }

    private static string PhaseName(AuditCommitPhase phase)
        => phase switch
        {
            AuditCommitPhase.PreCommit => "pre-commit",
            AuditCommitPhase.PostCommit => "post-commit",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported audit phase."),
        };

    private static string? IdempotencyKey(ChatBotGatewayContext context)
        => context.Idempotency?.CoarseKeyHash;

    private static IEnumerable<string> AssociationDecisionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (commandType is not (nameof(AssociateEmailToProject)
            or nameof(RejectEmailProjectAssociation)
            or nameof(DeferEmailProjectAssociation)
            or nameof(MarkEmailAssociationNeedsReview)))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "decisionKind", out string? decisionKind))
        {
            yield return $"decision-kind:{AuditMetadata.SafeOptionalToken(decisionKind)}";
        }

        if (TryReadString(element, "candidateEvidenceFingerprint", out string? fingerprint))
        {
            yield return $"evidence-fingerprint:{AuditMetadata.SafeOptionalToken(fingerprint)}";
        }

        if (TryReadString(element, "associationId", out string? associationId))
        {
            yield return $"association:{AuditMetadata.SafeOptionalToken(associationId)}";
        }
    }

    private static IEnumerable<string> AssociationCorrectionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "correctionKind", out string? correctionKind))
        {
            yield return $"correction-kind:{AuditMetadata.SafeOptionalToken(correctionKind)}";
        }

        if (TryReadString(element, "candidateEvidenceFingerprint", out string? fingerprint))
        {
            yield return $"evidence-fingerprint:{AuditMetadata.SafeOptionalToken(fingerprint)}";
        }

        if (TryReadString(element, "associationId", out string? associationId))
        {
            yield return $"association:{AuditMetadata.SafeOptionalToken(associationId)}";
        }

        if (TryReadString(element, "predecessorAssociationId", out string? predecessorAssociationId))
        {
            yield return $"predecessor-association:{AuditMetadata.SafeOptionalToken(predecessorAssociationId)}";
        }

        if (TryReadString(element, "priorProjectId", out string? priorProjectId))
        {
            yield return $"prior-project:{AuditMetadata.SafeOptionalToken(priorProjectId)}";
        }

        if (TryReadString(element, "targetProjectId", out string? targetProjectId))
        {
            yield return $"corrected-project:{AuditMetadata.SafeOptionalToken(targetProjectId)}";
        }

        if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
        {
            long propagationSourceVersion = sourceVersion + 1;
            yield return $"correction-source-version:{propagationSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            if (TryReadString(element, "associationId", out string? correctionAssociationId))
            {
                yield return $"correction-id:{AuditMetadata.SafeOptionalToken($"{correctionAssociationId}:correction:{propagationSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}")}";
            }
        }
    }

    private static IEnumerable<string> AiActionClassificationEvidenceRefs(ChatBotGatewayContext context)
    {
        if (context.RiskClassification?.Record is not { } classification)
        {
            yield break;
        }

        yield return $"classifier:{AuditMetadata.SafeOptionalToken(classification.ClassifierVersion)}";
        yield return $"risk-class:{AuditMetadata.SafeOptionalToken(RiskClassToken(classification.RiskClass))}";
        yield return $"reason:{AuditMetadata.SafeOptionalToken(classification.ReasonCode)}";

        if (!string.IsNullOrWhiteSpace(classification.PolicySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(classification.PolicySnapshotId)}";
        }

        foreach (string actionClass in classification.RiskActionClasses.Select(RiskActionClassToken))
        {
            yield return $"risk-action:{AuditMetadata.SafeOptionalToken(actionClass)}";
        }
    }

    private static IEnumerable<string> LowRiskAiAssistanceEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (context.ApprovalResult is { } approval)
        {
            yield return $"low-risk-policy-decision:{AuditMetadata.SafeOptionalToken(approval.Kind.ToString())}";
            yield return $"low-risk-policy-reason:{AuditMetadata.SafeOptionalToken(approval.ReasonCode)}";
            if (!string.IsNullOrWhiteSpace(approval.PolicySnapshotId))
            {
                yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(approval.PolicySnapshotId)}";
            }
        }

        if (TryReadString(element, "contextPackageId", out string? contextPackageId))
        {
            yield return $"context-package:{AuditMetadata.SafeOptionalToken(contextPackageId)}";
        }

        if (TryReadString(element, "contextPackageVersion", out string? contextPackageVersion))
        {
            yield return $"context-package-version:{AuditMetadata.SafeOptionalToken(contextPackageVersion)}";
        }

        if (TryReadString(element, "executionId", out string? executionId))
        {
            yield return $"execution:{AuditMetadata.SafeOptionalToken(executionId)}";
        }

        if (TryReadString(element, "proposalId", out string? proposalId))
        {
            yield return $"proposal:{AuditMetadata.SafeOptionalToken(proposalId)}";
        }
    }

    private static IEnumerable<string> ApprovalDecisionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(DecideAiActionApproval), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "proposalId", out string? proposalId))
        {
            yield return $"proposal:{AuditMetadata.SafeOptionalToken(proposalId)}";
        }

        if (TryReadString(element, "decision", out string? decision))
        {
            yield return $"approval-decision:{AuditMetadata.SafeOptionalToken(decision)}";
        }

        if (context.ApprovalResult is { } approval)
        {
            yield return $"approval-authority:{AuditMetadata.SafeOptionalToken(approval.ReasonCode)}";
        }
    }

    private static IEnumerable<string> ApprovedAiActionExecutionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "executionId", out string? executionId))
        {
            yield return $"execution:{AuditMetadata.SafeOptionalToken(executionId)}";
        }

        if (TryReadString(element, "proposalId", out string? proposalId))
        {
            yield return $"proposal:{AuditMetadata.SafeOptionalToken(proposalId)}";
        }

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "commandName", out string? commandName))
        {
            yield return $"approved-ai-command:{AuditMetadata.SafeOptionalToken(commandName)}";
        }

        if (TryReadString(element, "commandAllowlistVersion", out string? version))
        {
            yield return $"ai-action-command-allowlist:{AuditMetadata.SafeOptionalToken(version)}";
        }
    }

    private static IEnumerable<string> OutboundDraftEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CreateOutboundDraft), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "draftId", out string? draftId))
        {
            yield return $"outbound-draft:{AuditMetadata.SafeOptionalToken(draftId)}";
        }

        yield return "sender-authority:draft-only";

        if (TryReadString(element, "requesterId", out string? requesterId))
        {
            yield return $"requester:{AuditMetadata.SafeOptionalToken(requesterId)}";
        }

        if (TryReadString(element, "projectId", out string? projectId))
        {
            yield return $"project:{AuditMetadata.SafeOptionalToken(projectId)}";
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(policySnapshotId)}";
        }

        foreach (string safeRef in SafeRefArray(element, "contextRefs"))
        {
            yield return safeRef;
        }

        foreach (string safeRef in SafeRefArray(element, "recipientRefs"))
        {
            yield return safeRef;
        }

        if (TryReadString(element, "sourceConversationId", out string? sourceConversationId))
        {
            yield return $"conversation:{AuditMetadata.SafeOptionalToken(sourceConversationId)}";
        }

        if (TryReadString(element, "sourceMessageId", out string? sourceMessageId))
        {
            yield return $"source-message:{AuditMetadata.SafeOptionalToken(sourceMessageId)}";
        }
    }

    private static IEnumerable<string> OutboundApprovalEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (commandType is not (nameof(RequestOutboundSendApproval) or nameof(DecideOutboundApproval)))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "draftId", out string? draftId))
        {
            yield return $"outbound-draft:{AuditMetadata.SafeOptionalToken(draftId)}";
        }

        if (TryReadString(element, "requesterId", out string? requesterId))
        {
            yield return $"requester:{AuditMetadata.SafeOptionalToken(requesterId)}";
        }

        if (TryReadString(element, "projectId", out string? projectId))
        {
            yield return $"project:{AuditMetadata.SafeOptionalToken(projectId)}";
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(policySnapshotId)}";
        }

        if (TryReadString(element, "decision", out string? decision))
        {
            yield return $"approval-decision:{AuditMetadata.SafeOptionalToken(decision)}";
        }

        if (TryReadString(element, "senderAuthorityClass", out string? authorityClass))
        {
            yield return $"sender-authority:{AuditMetadata.SafeOptionalToken(authorityClass!.Replace(" ", "-", StringComparison.Ordinal))}";
        }

        foreach (string safeRef in SafeRefArray(element, "contextRefs"))
        {
            yield return safeRef;
        }

        foreach (string safeRef in SafeRefArray(element, "recipientRefs"))
        {
            yield return safeRef;
        }
    }

    private static IEnumerable<string> OutboundSendEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "sendId", out string? sendId))
        {
            yield return $"outbound-send:{AuditMetadata.SafeOptionalToken(sendId)}";
        }

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "draftId", out string? draftId))
        {
            yield return $"outbound-draft:{AuditMetadata.SafeOptionalToken(draftId)}";
        }

        if (TryReadString(element, "senderAuthorityClass", out string? authorityClass))
        {
            yield return $"sender-authority:{AuditMetadata.SafeOptionalToken(authorityClass!.Replace(" ", "-", StringComparison.Ordinal))}";
        }

        if (TryReadString(element, "requesterId", out string? requesterId))
        {
            yield return $"requester:{AuditMetadata.SafeOptionalToken(requesterId)}";
        }

        if (TryReadString(element, "sendActorId", out string? sendActorId))
        {
            yield return $"send-actor:{AuditMetadata.SafeOptionalToken(sendActorId)}";
        }

        if (TryReadString(element, "projectId", out string? projectId))
        {
            yield return $"project:{AuditMetadata.SafeOptionalToken(projectId)}";
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(policySnapshotId)}";
        }

        if (TryReadString(element, "adapterMode", out string? adapterMode))
        {
            yield return $"adapter-mode:{AuditMetadata.SafeOptionalToken(adapterMode)}";
        }

        foreach (string safeRef in SafeRefArray(element, "contextRefs"))
        {
            yield return safeRef;
        }

        foreach (string safeRef in SafeRefArray(element, "recipientRefs"))
        {
            yield return safeRef;
        }
    }

    private static IEnumerable<string> MailboxIntakeEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CaptureMailboxMessageIntake), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (element.TryGetProperty("source", out JsonElement source))
        {
            if (TryReadString(source, "mailboxId", out string? mailboxId))
            {
                yield return $"mailbox:{AuditMetadata.SafeOptionalToken(mailboxId)}";
            }

            if (TryReadString(source, "providerMessageId", out string? providerMessageId))
            {
                yield return $"provider-message:{AuditMetadata.SafeOptionalToken(providerMessageId)}";
            }
        }

        if (!element.TryGetProperty("authenticity", out JsonElement authenticity) ||
            authenticity.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (authenticity.TryGetProperty("authenticationResults", out JsonElement authenticationResults))
        {
            foreach ((string property, string prefix) in new[]
                     {
                         ("spf", "auth-spf"),
                         ("dkim", "auth-dkim"),
                         ("dmarc", "auth-dmarc"),
                         ("compositeAuthentication", "auth-compauth"),
                     })
            {
                if (TryReadString(authenticationResults, property, out string? verdict))
                {
                    yield return $"{prefix}:{AuditMetadata.SafeOptionalToken(verdict)}";
                }
            }

            if (TryReadString(authenticationResults, "compositeAuthenticationReason", out string? reason))
            {
                yield return $"auth-compauth-reason:{AuditMetadata.SafeOptionalToken(reason)}";
            }
        }

        if (authenticity.TryGetProperty("headerInspection", out JsonElement headerInspection))
        {
            foreach (string discrepancy in SafeRefArray(headerInspection, "discrepancies"))
            {
                yield return $"header-discrepancy:{discrepancy}";
            }

            foreach (string headerName in SelectedHeaderNames(headerInspection, "receivedHeaders"))
            {
                yield return $"selected-header:{headerName}";
            }

            foreach (string headerName in SelectedHeaderNames(headerInspection, "authenticationResultsHeaders"))
            {
                yield return $"selected-header:{headerName}";
            }
        }
    }

    private static IEnumerable<string> SelectedHeaderNames(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            if (TryReadString(item, "name", out string? value))
            {
                yield return AuditMetadata.SafeOptionalToken(value)!;
            }
        }
    }

    private static IEnumerable<string> ServiceClientGrantEvidenceRefs(ChatBotGatewayContext context)
    {
        if (context.ServiceClientGrantEvidence is not { } evidence)
        {
            yield break;
        }

        yield return $"service-client:{AuditMetadata.SafeOptionalToken(evidence.ServiceClientId)}";
        yield return $"actor-type:{AuditMetadata.SafeOptionalToken(context.Actor.ActorType)}";
        yield return $"grant:{AuditMetadata.SafeOptionalToken(evidence.GrantId)}";
        yield return $"grant-scope:{AuditMetadata.SafeOptionalToken(string.Join('|', evidence.Scopes))}";
        yield return $"grant-expiry:{AuditMetadata.SafeOptionalToken(evidence.ExpiresAt.UtcDateTime.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture))}";
        yield return $"command-set:{AuditMetadata.SafeOptionalToken(evidence.CommandSetVersion)}";
        yield return $"service-surface:{AuditMetadata.SafeOptionalToken(ChatBotSurfaceOrigins.ToWireValue(evidence.SurfaceOrigin))}";
        yield return $"service-client-class:{AuditMetadata.SafeOptionalToken(ServiceClientClasses.ToWireValue(evidence.ClientClass))}";

        if (!string.IsNullOrWhiteSpace(evidence.DelegatedUserId))
        {
            yield return $"delegated-user:{AuditMetadata.SafeOptionalToken(evidence.DelegatedUserId)}";
        }

        if (!string.IsNullOrWhiteSpace(evidence.OAuthGrantEvidenceFingerprint))
        {
            yield return $"oauth-evidence:{AuditMetadata.SafeOptionalToken(evidence.OAuthGrantEvidenceFingerprint)}";
        }
    }

    private static string RiskClassToken(Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass riskClass)
        => riskClass switch
        {
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass.LowRisk => "low-risk",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass.ApprovalRequired => "approval-required",
            _ => "approval-required",
        };

    private static string RiskActionClassToken(Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass actionClass)
        => actionClass switch
        {
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.ModifiesState => "modifies-state",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.ExposesFiles => "exposes-files",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.SendsExternal => "sends-external",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.CreatesTasks => "creates-tasks",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.InvokesTools => "invokes-tools",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.ActsOnBehalf => "acts-on-behalf",
            _ => "unknown",
        };

    private static bool TryReadString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadInt64(JsonElement element, string propertyName, out long value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt64(out value);
    }

    private static IEnumerable<string> SafeRefArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return AuditMetadata.SafeOptionalToken(value)!;
            }
        }
    }
}
