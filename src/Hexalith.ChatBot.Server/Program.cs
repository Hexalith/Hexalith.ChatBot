using System.Security.Claims;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();
_ = builder.Services.AddChatBotCommandGateway();

// JWT bearer auth is wired only when the topology supplies an Authority/SigningKey (the live Aspire Keycloak
// realm). The in-process WebApplicationFactory tests inject a test principal directly and configure neither, so
// no authentication middleware is added there and the injected principal is preserved.
bool jwtAuthentication = ChatBotJwtAuthentication.IsConfigured(builder.Configuration);
_ = builder.Services.AddChatBotJwtAuthentication(builder.Configuration);

// Gate the durable DAPR-backed read-model store on a sidecar being present: the live topology sets
// ChatBot:UseDaprStateStores=true so the projection lands in chatbot-statestore; in-process tests keep the
// in-memory default (no sidecar).
if (string.Equals(builder.Configuration["ChatBot:UseDaprStateStores"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.AddChatBotDaprStateStores();
}

WebApplication app = builder.Build();

if (jwtAuthentication)
{
    _ = app.UseAuthentication();
}

_ = app.UseChatBotCorrelation();

// DAPR pub/sub delivery: UseCloudEvents unwraps the CloudEvent so the projection subscriber binds the
// EventStore-stamped envelope as the request body; MapSubscribeHandler exposes the declarative subscription
// registry. Harmless for the in-process tests (a plain application/json POST passes through unchanged).
_ = app.UseCloudEvents();
_ = app.MapSubscribeHandler();
_ = app.MapDefaultEndpoints();
_ = app.MapGet("/health/chatbot", () => Results.Ok(new ChatBotHealth(
    ChatBotClientDescriptor.Default.ModuleName,
    ChatBotClientDescriptor.Default.DaprAppId,
    "healthy")));
_ = app.MapPost(
    "/api/v1/commands",
    async (
        CommandSubmissionWireRequest wireRequest,
        HttpContext httpContext,
        CommandGateway gateway,
        CancellationToken cancellationToken) =>
    {
        var request = wireRequest.ToGeneratedRequest();
        request.CommandId = NormalizeCommandId(request.CommandId);
        ChatBotCorrelationContext correlationContext = httpContext.ResolveCorrelationContext(request.CommandId);
        ChatBotSurfaceOrigin origin = ResolveSurfaceOrigin(wireRequest, httpContext);
        ChatBotGatewayResult result = await gateway
            .SubmitAsync(
                new ChatBotCommandSubmission(
                    httpContext.User,
                    request,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId,
                    origin),
                cancellationToken)
            .ConfigureAwait(false);

        return CommandGatewayHttpResults.ToHttpResult(result);
    });
_ = app.MapChatBotDomainServiceEndpoints();

// The EventStore publishes chatbot events to "{tenantId}.chatbot.events" on the chatbot-pubsub component; the
// subscription topic is configurable so the M0 single-tenant topic is set by the topology without baking a
// tenant into code. Defaults keep the in-process tests independent of any sidecar.
_ = app.MapGovernedOperationProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapMailboxIntakeProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapAssociationProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapParticipantResolutionProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapAiOutcomeProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapGet(
    "/api/v1/associations/{associationId}/routing-status",
    async (
        string associationId,
        HttpContext httpContext,
        IAssociationProjectionStore projectionStore,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!AssociationWorkflowId.TryParse(associationId, out AssociationWorkflowId parsedAssociationId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryResolveTenant(httpContext.User, out string? tenantId, out string reasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ReadDenialReason(reasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        AssociationCandidateView? view = await projectionStore
            .GetAsync(tenantId!, parsedAssociationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return view is null
            ? CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)))
            : Results.Ok(BuildAssociationRoutingStatus(view, correlationContext.CorrelationId));
    });
_ = app.MapGet(
    "/api/v1/projects/{projectId}/conversation",
    async (
        string projectId,
        string? cursor,
        int? pageSize,
        HttpContext httpContext,
        IProjectConversationProjectionStore projectionStore,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!AuditMetadata.IsSafeStableIdentifier(projectId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryResolveTenant(httpContext.User, out string? tenantId, out string reasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ReadDenialReason(reasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryAuthorizeProjectRead(httpContext.User, projectId, out bool hasProjectScopeClaims))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!ProjectConversationCursor.TryRead(cursor, tenantId!, projectId, out _, out _))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        ProjectConversationPage page = await projectionStore
            .ReadPageAsync(tenantId!, projectId, cursor, Math.Clamp(pageSize ?? 25, 1, 100), cancellationToken)
            .ConfigureAwait(false);

        if (page.Items.Count == 0)
        {
            return hasProjectScopeClaims
                ? Results.Ok(BuildProjectConversationResponse(projectId, page, correlationContext.CorrelationId))
                : CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                    problemDetailsFactory.CreateAuthorizationProblem(
                        ChatBotAuthorizationReasonCodes.SafeNotFound,
                        correlationContext.CorrelationId,
                        correlationContext.TaskId)));
        }

        return Results.Ok(BuildProjectConversationResponse(projectId, page, correlationContext.CorrelationId));
    });
_ = app.MapGet(
    "/api/v1/operations/{operationId}",
    async (
        string operationId,
        HttpContext httpContext,
        IOperationStatusStore statusStore,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!ChatBotIdentity.IsValidUlid(operationId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryResolveTenant(httpContext.User, out string? tenantId, out string reasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ReadDenialReason(reasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(tenantId!, operationId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return OperationStatusHttpResults.Ok(record);
    });
_ = app.MapGet(
    "/api/v1/operations/{operationId}/audit-history",
    async (
        string operationId,
        HttpContext httpContext,
        IOperationStatusStore statusStore,
        IAuditHistoryReader auditHistoryReader,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        // Tenant-scoped, redacted, metadata-only read of the operation's post-commit audit envelope summary
        // (Story 1.9 M3). A bad ULID, an unresolved tenant, and a cross-tenant/unknown operation all collapse to
        // the identical safe-not-found, reusing the Story 1.8 operation-status tenant-binding so the read never
        // confirms existence across the tenant boundary.
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!ChatBotIdentity.IsValidUlid(operationId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryResolveTenant(httpContext.User, out string? tenantId, out string reasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ReadDenialReason(reasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(tenantId!, operationId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        IReadOnlyList<AuditEnvelope> postCommitEnvelopes = auditHistoryReader.GetPostCommitEnvelopes(tenantId!, record.CommandId);
        return OperationAuditHistoryHttpResults.Ok(record.OperationId, record.AuditStatus, postCommitEnvelopes);
    });
_ = app.MapGet(
    "/api/v1/governed-operations/{noteId}",
    async (
        string noteId,
        HttpContext httpContext,
        IGovernedOperationProjectionStore projectionStore,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        // Tenant-scoped, metadata-only read of the durable projected read model in chatbot-statestore. A bad
        // ULID, an unresolved tenant, and a cross-tenant/unknown note all collapse to the identical
        // safe-not-found so the read never confirms existence across the tenant boundary (Story 1.3 floor).
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!ChatBotIdentity.IsValidUlid(noteId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryResolveTenant(httpContext.User, out string? tenantId, out string reasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ReadDenialReason(reasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        GovernedOperationView? view = await projectionStore
            .GetAsync(tenantId!, noteId, cancellationToken)
            .ConfigureAwait(false);

        return view is null
            ? CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)))
            : Results.Ok(GovernedOperationViewResponse.From(view));
    });

app.Run();

static string NormalizeCommandId(string? value)
    => ChatBotCommandId.TryParse(value, out ChatBotCommandId commandId)
        ? commandId.Value
        : ChatBotCommandId.New().Value;

static ProjectConversationResponse BuildProjectConversationResponse(
    string projectId,
    ProjectConversationPage page,
    string requestCorrelationId)
{
    ProjectConversationReadStatus status = ProjectConversationReadStatus.Empty;
    LifecycleState state = LifecycleState.Proposed;
    string? safeNextAction = "none";
    if (page.Items.Count > 0)
    {
        ProjectConversationItemView latest = page.Items
            .OrderByDescending(static item => item.OccurredAt)
            .ThenByDescending(static item => item.SourceVersion)
            .First();
        state = latest.LifecycleState;
        status = latest.LifecycleState switch
        {
            LifecycleState.Correcting or LifecycleState.CorrectionDelayed => ProjectConversationReadStatus.Blocked,
            LifecycleState.Failed => ProjectConversationReadStatus.Degraded,
            LifecycleState.Corrected when latest.SafeNextAction is not null => ProjectConversationReadStatus.Stale,
            _ => ProjectConversationReadStatus.Current,
        };
        safeNextAction = latest.SafeNextAction ?? (status == ProjectConversationReadStatus.Current ? "none" : "review-status");
    }

    return new ProjectConversationResponse(
        projectId,
        page.Items.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item.ProjectDisplayName))?.ProjectDisplayName,
        null,
        status,
        state,
        page.Items.Select(ToContractItem).ToArray(),
        new ProjectConversationCursorPage(page.NextCursor, page.HasMore, page.PageSize),
        AssociationCandidateView.MailboxSourceProvenance,
        "metadata_only",
        "collaboration_input",
        "chatbot.project-conversation-response.v1",
        requestCorrelationId,
        safeNextAction);
}

static ProjectConversationItem ToContractItem(ProjectConversationItemView item)
    => new(
        item.ItemId,
        item.Kind,
        item.ActorKind,
        item.ActorLabel,
        item.OccurredAt,
        item.LifecycleState,
        item.ThresholdBand,
        item.ConfidenceScore,
        item.AssociationId,
        item.SourceMailboxId,
        item.SourceProviderMessageId,
        item.InternetMessageId,
        item.SourceConversationId,
        item.SourceThreadId,
        item.SourceReceivedAtUtc,
        item.SourceSentAtUtc,
        item.SourceCreatedAtUtc,
        item.SourceTimezone,
        item.SourceProvenanceDisplayToken,
        item.SourceProvenance,
        item.RedactionState,
        item.RetentionClass,
        item.SchemaVersion,
        item.SourceVersion,
        item.CorrelationId,
        item.ProjectId,
        item.ProjectDisplayName,
        item.DecisionLabel,
        item.SafeNextAction,
        item.ParticipantResolutionId,
        item.SourceParticipantId,
        item.PartyId,
        item.ParticipantStatus,
        item.ParticipantBlockedReason,
        item.ParticipantDisplayKind,
        item.ParticipantEvidenceReference,
        item.ParticipantEvidenceFingerprint,
        item.ParticipantAllowedReviewActions,
        item.ParticipantRedactionState,
        item.SourceProviderAttachmentId,
        item.AttachmentDisplayName,
        item.AttachmentContentType,
        item.AttachmentSizeInBytes,
        item.AttachmentCaptureStatus,
        item.AttachmentStorageStatus,
        item.AttachmentScanStatus,
        item.AttachmentFolderId,
        item.AttachmentFileId,
        item.AttachmentDuplicateState,
        item.AttachmentRetryState,
        item.AttachmentAiContextEligibility,
        item.AttachmentAllowedActions,
        item.AttachmentRedactionState,
        item.DecisionKind,
        item.DecisionActorId,
        item.DecisionActorType,
        item.DecidedAtUtc,
        item.DecisionNoteRedactionState,
        item.SurfaceOrigin,
        item.PolicySnapshotVersion,
        item.EvidenceReferenceSummary,
        item.CorrectionKind,
        item.PriorProjectId,
        item.CorrectedProjectId,
        item.PredecessorAssociationId,
        item.SupersedesAssociationId,
        item.SupersededByAssociationId,
        item.CorrectionRationaleRedactionState,
        item.CorrectionActorId,
        item.CorrectionActorType,
        item.CorrectedAtUtc,
        item.DownstreamImpactStatus,
        item.CorrectionId,
        item.WorkflowInstanceId,
        item.RequiredStoreKeys,
        item.CompletedStoreKeys,
        item.FailedStoreKeys,
        item.PropagationProgressNumerator,
        item.PropagationProgressDenominator,
        item.PropagationStartedAtUtc,
        item.PropagationCompletedAtUtc,
        item.PropagationEstimatedCompletionAtUtc,
        item.PropagationStatus,
        item.IsCorrectedContextStale,
        item.ResponsibleOwnerRole,
        item.ApprovalId,
        item.ApprovalEventKind,
        item.ApprovalStatus,
        item.ApprovalDecisionKind,
        item.ApprovalRequesterId,
        item.ApprovalRequesterActorType,
        item.ApprovalRequestedAtUtc,
        item.ApprovalDecisionActorId,
        item.ApprovalDecisionActorType,
        item.ApprovalDecidedAtUtc,
        item.ApprovalOutcomeAtUtc,
        item.ApprovalProposalId,
        item.ApprovalSourceMessageId,
        item.ApprovalSourceConversationItemId,
        item.ApprovalCommandName,
        item.ApprovalCommandAllowlistVersion,
        item.ApprovalRiskClass,
        item.ApprovalRiskActionClasses,
        item.ApprovalPolicySnapshotId,
        item.ApprovalPolicySnapshotVisibility,
        item.ApprovalEvidenceReferences,
        item.ApprovalEvidenceFreshnessStates,
        item.ApprovalAffectedResourceReferences,
        item.ApprovalRecipientReferences,
        item.ApprovalSenderAuthorityClass,
        item.ApprovalExpectedPostStateRedactionState,
        item.ApprovalActionSummaryRedactionState,
        item.ApprovalDecisionRationaleRedactionState,
        item.ApprovalAuthorityResult,
        item.ApprovalDisabledReason,
        item.ApprovalAuditOperationId,
        item.ApprovalAuditStatus,
        item.ApprovalCommandOutcomeStatus,
        item.ApprovalProjectedOutcomeItemId,
        item.ApprovalFailureCode,
        item.ApprovalRetryability,
        item.SupersedesApprovalId,
        item.SupersededByApprovalId,
        item.FailureStateKind,
        item.FailureStatus,
        item.MessageCatalogCode,
        item.MessageCatalogVersion,
        item.MessageDetailVisibility,
        item.FailureCategory,
        item.FailureScope,
        item.FailureReasonCode,
        item.BlockedReason,
        item.Retryable,
        item.RetryCount,
        item.MaxRetryCount,
        item.NextRetryAtUtc,
        item.LastRetryAtUtc,
        item.RetryOperationId,
        item.SupersedesWorkflowInstanceId,
        item.SupersededByWorkflowInstanceId,
        item.TaskId,
        item.OperationId,
        item.AuditOperationId,
        item.AuditStatus,
        item.ClientAction,
        item.DuplicateSafetyState,
        item.DuplicateSuppressionId,
        item.DependencyName,
        item.DegradedUntilUtc,
        item.EscalationTargetRole,
        item.ReprocessCreatedWorkflowInstanceId,
        item.AiOutcomeKind,
        item.AiOutcomeStatus,
        item.AiActorId,
        item.AiActorType,
        item.AiProposalId,
        item.AiRequestId,
        item.AiRequesterId,
        item.AiSourceConversationItemId,
        item.AiSourceMessageId,
        item.AiOperationId,
        item.AiCorrelationId,
        item.AiRiskClass,
        item.AiRiskActionClasses,
        item.AiPolicySnapshotId,
        item.AiPolicySnapshotVisibility,
        item.AiContextPackageId,
        item.AiContextPackageVersion,
        item.AiContextRedactionState,
        item.AiAuthorizedContextReferences,
        item.AiExcludedContextReasons,
        item.AiGeneratedSummaryRedactionState,
        item.AiGeneratedContentVisibility,
        item.AiCommandName,
        item.AiCommandAllowlistVersion,
        item.AiApprovalId,
        item.AiApprovalStatus,
        item.AiExecutionStatus,
        item.AiExecutionOutcomeCode,
        item.AiAuditOperationId,
        item.AiAuditStatus,
        item.AiFailureCode,
        item.AiRetryability,
        item.AiSafeNextAction,
        item.SupersedesAiOutcomeId,
        item.SupersededByAiOutcomeId,
        item.BuildStatusSummary(),
        item.BuildClassification(),
        item.BuildDetectedIntent(),
        item.BuildAiSummaryProvenance(),
        item.BuildReviewHistory());

static AssociationRoutingStatus BuildAssociationRoutingStatus(AssociationCandidateView view, string requestCorrelationId)
{
    string[] disabledReasons = BuildAssociationDisabledReasons(view);
    string[] nextActions = BuildAssociationNextActionCodes(view);

    return new AssociationRoutingStatus(
        view.AssociationId,
        view.IntakeId,
        view.SourceMailboxId,
        view.SourceConversationId,
        view.SourceThreadId,
        view.LifecycleState,
        view.Outcome,
        view.ThresholdBand,
        view.ConfidenceScore,
        BuildAssociationReasonCodes(view),
        view.Candidates,
        view.Exclusions,
        view.ThresholdPolicyVersion,
        BuildAssociationEvidenceRefs(view),
        view.DerivationKernelVersion,
        view.DetectedAt,
        view.SourceProvenance,
        view.RedactionState,
        view.RetentionClass,
        "chatbot.association-routing-status.v1",
        view.SourceVersion,
        string.IsNullOrWhiteSpace(view.CorrelationId) ? requestCorrelationId : view.CorrelationId,
        disabledReasons,
        nextActions,
        view.DecisionKind,
        view.DecidedAt,
        view.DecisionActorId,
        view.DecisionActorType,
        view.DecisionNoteRedactionState,
        view.CorrectedProjectId,
        view.PriorProjectId,
        view.PredecessorAssociationId,
        view.SupersedesAssociationId,
        view.SupersededByAssociationId,
        view.CorrectionId,
        SupersedingCorrectionLinkFor(view),
        !string.IsNullOrWhiteSpace(view.SupersededByAssociationId) || !string.IsNullOrWhiteSpace(view.CorrectionId),
        view.CorrectionKind,
        view.CorrectedAt,
        view.CorrectionActorId,
        view.CorrectionActorType,
        view.CorrectionRationaleRedactionState,
        view.DownstreamImpactStatus,
        view.PropagationStatus,
        view.PropagationProgressNumerator,
        view.PropagationProgressDenominator,
        view.PropagationEstimatedCompletionAtUtc,
        view.IsCorrectedContextStale,
        view.ResponsibleOwnerRole,
        view.SafeNextAction,
        view.WorkflowInstanceId,
        view.RequiredStoreKeys,
        view.CompletedStoreKeys,
        view.FailedStoreKeys);
}

static IReadOnlyList<AssociationReasonCode> BuildAssociationReasonCodes(AssociationCandidateView view)
{
    AssociationReasonCode[] fromCandidates = view.Candidates
        .SelectMany(static candidate => candidate.ReasonCodes)
        .ToArray();
    AssociationReasonCode[] fromExclusions = view.Exclusions
        .Select(static exclusion => exclusion.ReasonCode)
        .ToArray();

    return fromCandidates
        .Concat(fromExclusions)
        .DefaultIfEmpty(AssociationReasonCode.NoAuthorizedCandidate)
        .Distinct()
        .ToArray();
}

static IReadOnlyList<AssociationEvidenceReference> BuildAssociationEvidenceRefs(AssociationCandidateView view)
{
    Dictionary<string, AssociationConfidenceInput> confidenceByReference = view.Candidates
        .SelectMany(static candidate => candidate.ConfidenceInputs)
        .GroupBy(static input => input.EvidenceReference, StringComparer.Ordinal)
        .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

    AssociationEvidenceReference[] candidateRefs = view.Candidates
        .SelectMany(static candidate => candidate.EvidenceRefs)
        .Select(reference => EnrichAssociationEvidence(reference, confidenceByReference))
        .ToArray();
    AssociationEvidenceReference[] exclusionRefs = view.Exclusions
        .Select(static exclusion => new AssociationEvidenceReference(
            exclusion.EvidenceReference,
            exclusion.EvidenceFingerprint,
            exclusion.State.ToString(),
            VisibilityState: "redacted",
            RedactionState: "redacted",
            FreshnessState: exclusion.State is AssociationExclusionState.Stale ? "stale" : "unavailable"))
        .ToArray();

    return candidateRefs
        .Concat(exclusionRefs)
        .GroupBy(static evidence => evidence.EvidenceReference, StringComparer.Ordinal)
        .Select(static group => group.First())
        .ToArray();
}

static AssociationEvidenceReference EnrichAssociationEvidence(
    AssociationEvidenceReference reference,
    IReadOnlyDictionary<string, AssociationConfidenceInput> confidenceByReference)
{
    if (!confidenceByReference.TryGetValue(reference.EvidenceReference, out AssociationConfidenceInput? input))
    {
        return reference with
        {
            MatchedValueDisplayToken = reference.MatchedValueDisplayToken ?? SafeEvidenceDisplayToken(reference.EvidenceReference),
            VisibilityState = reference.VisibilityState ?? "available",
            RedactionState = reference.RedactionState ?? "metadata_only",
            FreshnessState = reference.FreshnessState ?? "fresh",
        };
    }

    return reference with
    {
        SignalClass = reference.SignalClass ?? SignalClassWireValue(input.SignalClass),
        MatchedValueDisplayToken = reference.MatchedValueDisplayToken ?? SafeEvidenceDisplayToken(reference.EvidenceReference),
        VisibilityState = reference.VisibilityState ?? "available",
        RedactionState = reference.RedactionState ?? "metadata_only",
        FreshnessState = reference.FreshnessState ?? "fresh",
        ConfidenceContribution = reference.ConfidenceContribution ?? input.Weight,
    };
}

static string? SupersedingCorrectionLinkFor(AssociationCandidateView view)
    => string.IsNullOrWhiteSpace(view.SupersededByAssociationId)
        ? null
        : $"association:{view.SupersededByAssociationId}";

static string SafeEvidenceDisplayToken(string evidenceReference)
{
    int separator = evidenceReference.IndexOf(':', StringComparison.Ordinal);
    return separator <= 0 ? "evidence-reference" : $"{evidenceReference[..separator]}:metadata";
}

static string SignalClassWireValue(AssociationSignalClass signalClass)
    => signalClass switch
    {
        AssociationSignalClass.ExplicitProjectIdentifier => "explicit-project-identifier",
        AssociationSignalClass.MailboxRoutingRule => "mailbox-routing-rule",
        AssociationSignalClass.ConversationThreadIdentifier => "conversation-thread-identifier",
        AssociationSignalClass.HumanSelection => "human-selection",
        AssociationSignalClass.Correction => "correction",
        _ => signalClass.ToString(),
    };

static string[] BuildAssociationDisabledReasons(AssociationCandidateView view)
{
    List<string> reasons = [];

    if (view.Candidates.Count == 0)
    {
        reasons.Add("candidate-required");
    }

    if (view.LifecycleState is LifecycleState.Rejected or LifecycleState.Failed or LifecycleState.Skipped)
    {
        reasons.Add("terminal-state");
    }

    if (view.LifecycleState is LifecycleState.Correcting or LifecycleState.CorrectionDelayed ||
        string.Equals(view.PropagationStatus, Hexalith.ChatBot.Server.Association.CorrectionPropagationStatuses.Pending, StringComparison.Ordinal) ||
        string.Equals(view.PropagationStatus, Hexalith.ChatBot.Server.Association.CorrectionPropagationStatuses.Correcting, StringComparison.Ordinal) ||
        view.IsCorrectedContextStale)
    {
        reasons.Add("projection-pending");
        reasons.Add("corrected-context-stale");
    }

    if (view.LifecycleState is LifecycleState.CorrectionDelayed ||
        string.Equals(view.PropagationStatus, "delayed", StringComparison.Ordinal))
    {
        reasons.Add("correction-delayed");
    }

    if (view.Exclusions.Any(static exclusion => exclusion.State is AssociationExclusionState.Unauthorized))
    {
        reasons.Add("not-authorized");
    }

    return reasons.Distinct(StringComparer.Ordinal).ToArray();
}

static string[] BuildAssociationNextActionCodes(AssociationCandidateView view)
    => view switch
    {
        { LifecycleState: LifecycleState.Correcting } => [ChatBotMessageCodes.AssociationCorrectionPropagationPending],
        { LifecycleState: LifecycleState.CorrectionDelayed } => [ChatBotMessageCodes.AssociationCorrectionPropagationDelayed],
        { Candidates.Count: 0, LifecycleState: LifecycleState.Failed } => [ChatBotMessageCodes.AssociationScorerFailedClosed],
        { Candidates.Count: 0 } => [ChatBotMessageCodes.AssociationContextUnavailable],
        { Exclusions.Count: > 0 } => [ChatBotMessageCodes.AssociationCandidateSuppressed],
        { DownstreamImpactStatus: "complete" } => [ChatBotMessageCodes.AssociationCorrectionPropagationComplete],
        _ => [ChatBotMessageCodes.AssociationAmbiguousRouted],
    };

// Surface origin is captured once here at the adapter boundary (FR85 / S7): the request body field
// takes precedence, then the X-Hexalith-Surface-Origin header, and an absent/unknown declaration
// collapses to the safe default. From this point it is immutable on ChatBotCommandSubmission.
static ChatBotSurfaceOrigin ResolveSurfaceOrigin(CommandSubmissionWireRequest wireRequest, HttpContext httpContext)
{
    string? declared = wireRequest.Origin;
    if (string.IsNullOrWhiteSpace(declared)
        && httpContext.Request.Headers.TryGetValue("X-Hexalith-Surface-Origin", out Microsoft.Extensions.Primitives.StringValues header)
        && header.Count == 1)
    {
        declared = header[0];
    }

    return ChatBotSurfaceOrigins.FromWireValueOrDefault(declared);
}

static bool TryResolveTenant(ClaimsPrincipal principal, out string? tenantId, out string reasonCode)
{
    tenantId = null;
    reasonCode = ChatBotAuthorizationReasonCodes.AuthenticationDenied;

    if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
    {
        return false;
    }

    string? actorId = principal.FindFirstValue("sub");
    if (!AuditMetadata.IsSafeStableIdentifier(actorId))
    {
        return false;
    }

    string[] tenantClaims = ["eventstore:tenant", "tenant"];
    string[] tenants = tenantClaims
        .SelectMany(principal.FindAll)
        .Select(static claim => claim.Value)
        .Where(AuditMetadata.IsSafeStableIdentifier)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    if (tenants.Length != 1)
    {
        reasonCode = ChatBotAuthorizationReasonCodes.TenantMissing;
        return false;
    }

    tenantId = tenants[0];
    reasonCode = string.Empty;
    return true;
}

static bool TryAuthorizeProjectRead(ClaimsPrincipal principal, string projectId, out bool hasProjectScopeClaims)
{
    hasProjectScopeClaims = false;
    string[] projectClaims = principal
        .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
        .Select(static claim => claim.Value)
        .Where(static value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    if (projectClaims.Length == 0)
    {
        return true;
    }

    hasProjectScopeClaims = true;
    if (projectClaims.Any(static value => !string.Equals(value, "*", StringComparison.Ordinal) && !AuditMetadata.IsSafeStableIdentifier(value)))
    {
        return false;
    }

    return projectClaims.Contains("*", StringComparer.Ordinal) ||
        projectClaims.Contains(projectId, StringComparer.Ordinal);
}

// Read-surface defense-in-depth (AC3): an authenticated-but-unresolved tenant on a READ collapses to
// safe-not-found, so a foreign, unknown, missing-, ambiguous-, stale-, or unsafe-tenant read is
// indistinguishable from a not-found. Today the message catalog already renders both TenantMissing and
// SafeNotFound through the same Authorization_denied entry, so this mapping is behaviour-preserving; it pins
// the read-boundary invariant explicitly so a future catalog change that gave TenantMissing its own surface
// text could not start distinguishing the unresolved-tenant case. Unauthenticated reads keep
// AuthenticationDenied (401) so the caller still learns it must authenticate.
static string ReadDenialReason(string reasonCode)
    => string.Equals(reasonCode, ChatBotAuthorizationReasonCodes.AuthenticationDenied, StringComparison.Ordinal)
        ? reasonCode
        : ChatBotAuthorizationReasonCodes.SafeNotFound;

public sealed record ChatBotHealth(string ModuleName, string DaprAppId, string Status);

public partial class Program;
