using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Cli;
using Hexalith.ChatBot.Mcp;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

using AssociateEmailToProjectCommand = Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject;
using ApprovalDecision = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;
using AssociationCorrection = Hexalith.ChatBot.Contracts.Enums.AssociationCorrectionKind;
using AssociationDecision = Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;
using ChatBotSurfaceOrigins = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigins;
using CorrectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation;
using DecideAiActionApprovalCommand = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using DeferEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.DeferEmailProjectAssociation;
using ExecuteApprovedAIActionCommand = Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction;
using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;
using RejectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.RejectEmailProjectAssociation;
using RequestFailedWorkflowRetryCommand = Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry;

/// <summary>
/// A semantic, surface-agnostic state-changing intent. Each arm expresses this same intent through its
/// production surface path and must submit an equivalent typed command through <see cref="IChatBotClient"/>.
/// </summary>
internal sealed record SemanticCommandIntent(
    string Key,
    IChatBotCommand ApiCommand,
    string[] CliArgs,
    string McpToolName,
    IReadOnlyDictionary<string, object?> McpArguments);

/// <summary>
/// A semantic, surface-agnostic read intent. Each arm must invoke the same client-facing read method and receive
/// equivalent metadata-only contract facts.
/// </summary>
internal sealed record SemanticReadIntent(
    string Key,
    Func<IChatBotClient, CancellationToken, Task> ApiInvokeAsync,
    string[] CliArgs,
    string McpToolName,
    IReadOnlyDictionary<string, object?> McpArguments);

internal sealed record SurfaceCommandTranslation(
    string ArmName,
    string DeclaredOrigin,
    string SubmittedOrigin,
    IChatBotCommand Command);

internal sealed record SurfaceReadTranslation(
    string ArmName,
    string ReadMethod,
    string TargetId,
    string? CorrelationId,
    string? TaskId,
    IReadOnlyList<KeyValuePair<string, string>> ContractFacts);

internal sealed record ReadInvocation(
    string Method,
    string TargetId,
    string? CorrelationId,
    string? TaskId,
    IReadOnlyList<KeyValuePair<string, string>> ContractFacts);

/// <summary>
/// A production surface driver. CLI and MCP arms execute their production adapters; the UI/API arm uses the
/// adapter-facing <see cref="IChatBotClient"/> seam directly, matching the UI service boundary without inventing
/// another translator in tests.
/// </summary>
internal interface ISurfaceArm
{
    string Name { get; }

    ChatBotSurfaceOrigin Origin { get; }

    Task<SurfaceCommandTranslation> TranslateCommandAsync(SemanticCommandIntent intent, CancellationToken cancellationToken);

    Task<SurfaceReadTranslation> InvokeReadAsync(SemanticReadIntent intent, CancellationToken cancellationToken);
}

internal sealed class UiApiSurfaceArm : ISurfaceArm
{
    public string Name => "ui-api";

    public ChatBotSurfaceOrigin Origin => ChatBotSurfaceOrigin.Ui;

    public async Task<SurfaceCommandTranslation> TranslateCommandAsync(SemanticCommandIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var client = new RecordingChatBotClient();
        _ = await client.SubmitAsync(
            intent.ApiCommand,
            SurfaceIntentCatalog.CorrelationId,
            SurfaceIntentCatalog.TaskId,
            Origin,
            cancellationToken)
            .ConfigureAwait(false);

        return client.RequireCommand(Name);
    }

    public async Task<SurfaceReadTranslation> InvokeReadAsync(SemanticReadIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var client = new RecordingChatBotClient();
        await intent.ApiInvokeAsync(client, cancellationToken).ConfigureAwait(false);
        return client.RequireRead(Name);
    }
}

internal sealed class CliSurfaceArm : ISurfaceArm
{
    public string Name => "cli";

    public ChatBotSurfaceOrigin Origin => ChatBotSurfaceOrigin.Cli;

    public async Task<SurfaceCommandTranslation> TranslateCommandAsync(SemanticCommandIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var client = new RecordingChatBotClient();
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exitCode = await ChatBotCliCommands.InvokeAsync(
            [.. intent.CliArgs, "--correlation-id", SurfaceIntentCatalog.CorrelationId, "--task-id", SurfaceIntentCatalog.TaskId],
            client,
            output,
            error,
            cancellationToken)
            .ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"CLI arm failed to translate '{intent.Key}'.");
        }

        return client.RequireCommand(Name);
    }

    public async Task<SurfaceReadTranslation> InvokeReadAsync(SemanticReadIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var client = new RecordingChatBotClient();
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exitCode = await ChatBotCliCommands.InvokeAsync(
            [.. intent.CliArgs, "--correlation-id", SurfaceIntentCatalog.CorrelationId, "--task-id", SurfaceIntentCatalog.TaskId],
            client,
            output,
            error,
            cancellationToken)
            .ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"CLI arm failed to read '{intent.Key}'.");
        }

        return client.RequireRead(Name);
    }
}

internal sealed class McpSurfaceArm : ISurfaceArm
{
    public string Name => "mcp";

    public ChatBotSurfaceOrigin Origin => ChatBotSurfaceOrigin.Mcp;

    public async Task<SurfaceCommandTranslation> TranslateCommandAsync(SemanticCommandIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var client = new RecordingChatBotClient();
        _ = await new ChatBotMcpService(client)
            .InvokeAsync(ChatBotMcpInvocation.Create(intent.McpToolName, WithTrace(intent.McpArguments)), cancellationToken)
            .ConfigureAwait(false);

        return client.RequireCommand(Name);
    }

    public async Task<SurfaceReadTranslation> InvokeReadAsync(SemanticReadIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var client = new RecordingChatBotClient();
        _ = await new ChatBotMcpService(client)
            .InvokeAsync(ChatBotMcpInvocation.Create(intent.McpToolName, WithTrace(intent.McpArguments)), cancellationToken)
            .ConfigureAwait(false);

        return client.RequireRead(Name);
    }

    private static IReadOnlyDictionary<string, object?> WithTrace(IReadOnlyDictionary<string, object?> arguments)
    {
        Dictionary<string, object?> traced = new(arguments, StringComparer.Ordinal)
        {
            ["correlationId"] = SurfaceIntentCatalog.CorrelationId,
            ["taskId"] = SurfaceIntentCatalog.TaskId,
        };
        return traced;
    }
}

internal static class SurfaceArms
{
    public static IReadOnlyList<ISurfaceArm> All { get; } = [new UiApiSurfaceArm(), new CliSurfaceArm(), new McpSurfaceArm()];
}

internal static class SurfaceIntentCatalog
{
    public const string AssociationId = "01HX0000000000000000000001";
    public const string IntakeId = "01HX0000000000000000000002";
    public const string ProjectId = "project-123";
    public const string EvidenceFingerprint = "ev-fingerprint";
    public const string CorrelationId = "01HX0000000000000000000003";
    public const string TaskId = "01HX0000000000000000000004";

    public static IReadOnlyList<SemanticCommandIntent> StateChangingIntents { get; } =
    [
        Command(
            "association.associate",
            new AssociateEmailToProjectCommand(AssociationId, IntakeId, ProjectId, AssociationDecision.Associate, null, EvidenceFingerprint, 7, "chatbot.association-decision.v1"),
            ["association", "associate", "--association-id", AssociationId, "--intake-id", IntakeId, "--project-id", ProjectId, "--evidence-fingerprint", EvidenceFingerprint, "--source-version", "7"],
            "chatbot.association.associate",
            Args(("associationId", AssociationId), ("intakeId", IntakeId), ("projectId", ProjectId), ("evidenceFingerprint", EvidenceFingerprint), ("sourceVersion", 7L), ("schemaVersion", "chatbot.association-decision.v1"))),
        Command(
            "association.reject",
            new RejectEmailProjectAssociationCommand(AssociationId, IntakeId, AssociationDecision.Reject, null, EvidenceFingerprint, 7, "chatbot.association-decision.v1"),
            ["association", "reject", "--association-id", AssociationId, "--intake-id", IntakeId, "--evidence-fingerprint", EvidenceFingerprint, "--source-version", "7"],
            "chatbot.association.reject",
            Args(("associationId", AssociationId), ("intakeId", IntakeId), ("evidenceFingerprint", EvidenceFingerprint), ("sourceVersion", 7L), ("schemaVersion", "chatbot.association-decision.v1"))),
        Command(
            "association.defer",
            new DeferEmailProjectAssociationCommand(AssociationId, IntakeId, AssociationDecision.Defer, null, EvidenceFingerprint, 7, "chatbot.association-decision.v1"),
            ["association", "defer", "--association-id", AssociationId, "--intake-id", IntakeId, "--evidence-fingerprint", EvidenceFingerprint, "--source-version", "7"],
            "chatbot.association.defer",
            Args(("associationId", AssociationId), ("intakeId", IntakeId), ("evidenceFingerprint", EvidenceFingerprint), ("sourceVersion", 7L), ("schemaVersion", "chatbot.association-decision.v1"))),
        Command(
            "association.correct",
            new CorrectEmailProjectAssociationCommand(AssociationId, IntakeId, "project-old", ProjectId, AssociationCorrection.ProjectReassignment, null, "assoc-previous", EvidenceFingerprint, 7, "chatbot.association-correction.v1"),
            ["association", "correct", "--association-id", AssociationId, "--intake-id", IntakeId, "--prior-project-id", "project-old", "--target-project-id", ProjectId, "--predecessor-association-id", "assoc-previous", "--evidence-fingerprint", EvidenceFingerprint, "--source-version", "7"],
            "chatbot.association.correct",
            Args(("associationId", AssociationId), ("intakeId", IntakeId), ("priorProjectId", "project-old"), ("targetProjectId", ProjectId), ("predecessorAssociationId", "assoc-previous"), ("evidenceFingerprint", EvidenceFingerprint), ("sourceVersion", 7L), ("schemaVersion", "chatbot.association-correction.v1"))),
        Command(
            "operation.retry",
            new RequestFailedWorkflowRetryCommand("retry-1", "failed-event-1", "projection", "dependency_degraded", 9, null),
            ["operation", "retry", "--retry-id", "retry-1", "--failed-event-id", "failed-event-1", "--failed-operation-class", "projection", "--failure-reason-code", "dependency_degraded", "--expected-failed-source-version", "9"],
            "chatbot.operation.retry",
            Args(("retryId", "retry-1"), ("failedEventId", "failed-event-1"), ("failedOperationClass", "projection"), ("failureReasonCode", "dependency_degraded"), ("expectedFailedSourceVersion", 9L))),
        Command(
            "approval.decide",
            new DecideAiActionApprovalCommand(ProjectId, "approval-1", "proposal-1", "message-1", ApprovalDecision.Approve, 3, CorrelationId, "decision-1"),
            ["approval", "decide", "--project-id", ProjectId, "--approval-id", "approval-1", "--proposal-id", "proposal-1", "--source-message-id", "message-1", "--decision", "approve", "--expected-approval-source-version", "3", "--command-correlation-id", CorrelationId, "--decision-id", "decision-1"],
            "chatbot.approval.decide",
            Args(("projectId", ProjectId), ("approvalId", "approval-1"), ("proposalId", "proposal-1"), ("sourceMessageId", "message-1"), ("decision", "approve"), ("expectedApprovalSourceVersion", 3L), ("commandCorrelationId", CorrelationId), ("decisionId", "decision-1"))),
        Command(
            "ai_action.execute",
            new ExecuteApprovedAIActionCommand(ProjectId, "proposal-1", "approval-1", "task-intent-1", "message-1", "requester-1", "SendProjectReply", "allowlist-v1", 3, 4, CorrelationId, "execution-1", "transition-1", ["evidence-1"], ["resource-1"], ["recipient-1"]),
            ["ai-action", "execute", "--project-id", ProjectId, "--proposal-id", "proposal-1", "--approval-id", "approval-1", "--task-intent-id", "task-intent-1", "--source-message-id", "message-1", "--requester-id", "requester-1", "--command-name", "SendProjectReply", "--command-allowlist-version", "allowlist-v1", "--expected-approval-source-version", "3", "--expected-proposal-source-version", "4", "--command-correlation-id", CorrelationId, "--execution-id", "execution-1", "--transition-id", "transition-1", "--source-evidence", "evidence-1", "--affected-resource", "resource-1", "--recipient", "recipient-1"],
            "chatbot.ai_action.execute",
            Args(("projectId", ProjectId), ("proposalId", "proposal-1"), ("approvalId", "approval-1"), ("taskIntentId", "task-intent-1"), ("sourceMessageId", "message-1"), ("requesterId", "requester-1"), ("commandName", "SendProjectReply"), ("commandAllowlistVersion", "allowlist-v1"), ("expectedApprovalSourceVersion", 3L), ("expectedProposalSourceVersion", 4L), ("commandCorrelationId", CorrelationId), ("executionId", "execution-1"), ("transitionId", "transition-1"), ("sourceEvidenceReferences", new[] { "evidence-1" }), ("affectedResourceReferences", new[] { "resource-1" }), ("recipientReferences", new[] { "recipient-1" }))),
    ];

    public static IReadOnlyList<SemanticReadIntent> ReadIntents { get; } =
    [
        new(
            "association.status",
            (client, cancellationToken) => client.GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, cancellationToken),
            ["association", "status", "--association-id", AssociationId],
            "chatbot.association.status",
            Args(("associationId", AssociationId))),
        new(
            "operation.status",
            (client, cancellationToken) => client.GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, cancellationToken),
            ["operation", "status", "--operation-id", AssociationId],
            "chatbot.operation.status",
            Args(("operationId", AssociationId))),
        new(
            "operation.audit",
            (client, cancellationToken) => client.GetOperationAuditHistoryAsync(AssociationId, CorrelationId, TaskId, cancellationToken),
            ["operation", "audit", "--operation-id", AssociationId],
            "chatbot.operation.audit",
            Args(("operationId", AssociationId))),
    ];

    public static SemanticCommandIntent GatewayCommandIntent
        => StateChangingIntents.Single(static intent => string.Equals(intent.Key, "operation.retry", StringComparison.Ordinal));

    private static SemanticCommandIntent Command(
        string key,
        IChatBotCommand apiCommand,
        string[] cliArgs,
        string mcpToolName,
        IReadOnlyDictionary<string, object?> mcpArguments)
        => new(key, apiCommand, cliArgs, mcpToolName, mcpArguments);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] values)
        => values.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
}

internal sealed class RecordingChatBotClient : IChatBotClient
{
    private IChatBotCommand? _submittedCommand;
    private ChatBotSurfaceOrigin? _submittedOrigin;
    private ReadInvocation? _read;

    public Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _submittedCommand = command ?? throw new ArgumentNullException(nameof(command));
        _submittedOrigin = origin;
        return Task.FromResult(new CommandSubmissionResponse
        {
            CommandId = "cmd-1",
            CorrelationId = correlationId ?? "corr-1",
            TaskId = taskId ?? "op-1",
            LifecycleState = LifecycleState.Received,
            AcceptedAt = DateTimeOffset.UnixEpoch,
        });
    }

    public Task<OperationStatus> GetOperationStatusAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OperationStatus status = OperationStatus();
        _read = new ReadInvocation(
            nameof(GetOperationStatusAsync),
            operationId,
            correlationId,
            taskId,
            [
                new("operationId", status.OperationId),
                new("commandId", status.CommandId),
                new("correlationId", status.CorrelationId),
                new("lifecycleState", status.LifecycleState.ToString()),
                new("completionStatus", status.CompletionStatus.ToString()),
                new("auditStatus", status.AuditStatus.ToString()),
                new("retryCount", status.RetryCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("safeNextActions", string.Join("|", status.SafeNextActions)),
                new("operationClass", status.OperationClass),
                new("redaction", "metadata-only"),
            ]);
        return Task.FromResult(status);
    }

    public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OperationAuditHistory history = OperationAudit();
        _read = new ReadInvocation(
            nameof(GetOperationAuditHistoryAsync),
            operationId,
            correlationId,
            taskId,
            [
                new("operationId", history.OperationId),
                new("auditStatus", history.AuditStatus.ToString()),
                new("entries.count", history.Entries.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("entries[0].phase", history.Entries.First().Phase.ToString()),
                new("entries[0].decision", history.Entries.First().Decision),
                new("entries[0].reasonCode", history.Entries.First().ReasonCode),
                new("entries[0].outcome", history.Entries.First().Outcome),
                new("entries[0].stateTransition", history.Entries.First().StateTransition),
                new("entries[0].redactionDecision", history.Entries.First().RedactionDecision.ToString()),
                new("entries[0].correlationId", history.Entries.First().CorrelationId),
            ]);
        return Task.FromResult(history);
    }

    public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
        string associationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AssociationRoutingStatus status = AssociationStatus();
        AssociationCandidate candidate = status.Candidates.Single();
        AssociationEvidenceReference evidence = candidate.EvidenceRefs.Single();
        _read = new ReadInvocation(
            nameof(GetAssociationRoutingStatusAsync),
            associationId,
            correlationId,
            taskId,
            [
                new("associationId", status.AssociationId),
                new("intakeId", status.IntakeId),
                new("lifecycleState", status.LifecycleState.ToString()),
                new("outcome", status.Outcome.ToString()),
                new("redactionState", status.RedactionState.ToString()),
                new("sourceVersion", status.SourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("correlationId", status.CorrelationId),
                new("candidate.rank", candidate.Rank.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("candidate.projectId", candidate.ProjectId),
                new("candidate.evidenceFingerprint", evidence.EvidenceFingerprint),
                new("candidate.evidenceRedactionState", evidence.RedactionState?.ToString() ?? string.Empty),
                new("nextActionReasonCodes", string.Join("|", status.NextActionReasonCodes)),
            ]);
        return Task.FromResult(status);
    }

    public Task<ProjectConversationResponse> GetProjectConversationAsync(
        string projectId,
        string? cursor = null,
        int pageSize = 25,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Project conversation reads are outside the Story 5.4 conformance catalog.");

    public Task<TaskIntentReview> GetTaskIntentReviewAsync(
        string projectId,
        string taskIntentId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Task review reads are outside the Story 5.4 conformance catalog.");

    public SurfaceCommandTranslation RequireCommand(string armName)
    {
        if (_submittedCommand is null || _submittedOrigin is null)
        {
            throw new InvalidOperationException($"Arm '{armName}' did not submit a command.");
        }

        return new SurfaceCommandTranslation(
            armName,
            ChatBotSurfaceOrigins.ToWireValue(_submittedOrigin.Value),
            ChatBotSurfaceOrigins.ToWireValue(_submittedOrigin.Value),
            _submittedCommand);
    }

    public SurfaceReadTranslation RequireRead(string armName)
    {
        if (_read is null)
        {
            throw new InvalidOperationException($"Arm '{armName}' did not invoke a read.");
        }

        return new SurfaceReadTranslation(
            armName,
            _read.Method,
            _read.TargetId,
            _read.CorrelationId,
            _read.TaskId,
            _read.ContractFacts);
    }

    private static AssociationRoutingStatus AssociationStatus()
        => new()
        {
            AssociationId = SurfaceIntentCatalog.AssociationId,
            IntakeId = SurfaceIntentCatalog.IntakeId,
            SourceMailboxId = "mailbox-1",
            SourceConversationId = "conversation-1",
            LifecycleState = LifecycleState.NeedsReview,
            Outcome = AssociationScoringOutcome.CandidatesGenerated,
            ThresholdBand = AssociationThresholdBand.Ambiguous,
            ConfidenceScore = 0.87,
            ThresholdPolicyVersion = "policy-v1",
            KernelVersion = "association-kernel-v1",
            DetectedAt = DateTimeOffset.UnixEpoch,
            SourceProvenance = AssociationRoutingStatusSourceProvenance.M365MailboxIntake,
            RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
            RetentionClass = AssociationRoutingStatusRetentionClass.Collaboration_input,
            SchemaVersion = "chatbot.association-routing-status.v1",
            SourceVersion = 7,
            CorrelationId = SurfaceIntentCatalog.CorrelationId,
            ReasonCodes = [AssociationReasonCode.ExplicitProjectIdentifierMatched],
            NextActionReasonCodes = [ChatBotMessageCode.Association_ambiguous_routed],
            Candidates =
            [
                new AssociationCandidate
                {
                    ProjectId = SurfaceIntentCatalog.ProjectId,
                    DisplayName = "Safe Project",
                    ConfidenceScore = 0.87,
                    Rank = 1,
                    ReasonCodes = [AssociationReasonCode.ExplicitProjectIdentifierMatched],
                    EvidenceRefs =
                    [
                        new AssociationEvidenceReference
                        {
                            EvidenceReference = "evidence-ref-1",
                            EvidenceFingerprint = "evidence-fingerprint-1",
                            EvidenceKind = "mailbox-subject",
                            RedactionState = AssociationEvidenceReferenceRedactionState.Metadata_only,
                            VisibilityState = AssociationEvidenceReferenceVisibilityState.Available,
                            FreshnessState = AssociationEvidenceReferenceFreshnessState.Fresh,
                        },
                    ],
                    RequiredEvidenceComplete = true,
                },
            ],
            EvidenceRefs =
            [
                new AssociationEvidenceReference
                {
                    EvidenceReference = "evidence-ref-1",
                    EvidenceFingerprint = "evidence-fingerprint-1",
                    EvidenceKind = "mailbox-subject",
                    RedactionState = AssociationEvidenceReferenceRedactionState.Metadata_only,
                    VisibilityState = AssociationEvidenceReferenceVisibilityState.Available,
                    FreshnessState = AssociationEvidenceReferenceFreshnessState.Fresh,
                },
            ],
        };

    private static OperationStatus OperationStatus()
        => new()
        {
            OperationId = SurfaceIntentCatalog.AssociationId,
            CommandId = "cmd-1",
            CorrelationId = SurfaceIntentCatalog.CorrelationId,
            LifecycleState = LifecycleState.Received,
            RetryCount = 1,
            CompletionStatus = OperationCompletionStatus.AcceptedProjectionPending,
            AuditStatus = OperationAuditStatus.Reconciling,
            SafeNextActions = [ChatBotMessageNextAction.RetryLater],
            AcceptedAt = DateTimeOffset.UnixEpoch,
            LastUpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            OperationClass = "command-execution",
            MaxAttempts = 5,
            OwnerRole = "reviewer",
        };

    private static OperationAuditHistory OperationAudit()
        => new()
        {
            OperationId = SurfaceIntentCatalog.AssociationId,
            AuditStatus = OperationAuditStatus.Committed,
            Entries =
            [
                new AuditHistoryEntry
                {
                    Phase = AuditHistoryPhase.PostCommit,
                    Decision = "allow",
                    ReasonCode = "committed",
                    Outcome = "accepted",
                    StateTransition = "Received->Proposed",
                    RedactionDecision = AuditHistoryEntryRedactionDecision.Metadata_only,
                    SurfaceOrigin = SurfaceOrigin.Api,
                    ResourceId = "resource-1",
                    CorrelationId = SurfaceIntentCatalog.CorrelationId,
                },
            ],
        };
}
