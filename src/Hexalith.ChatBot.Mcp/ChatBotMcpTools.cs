using System.ComponentModel;
using System.Text.Json;

using ModelContextProtocol.Server;

namespace Hexalith.ChatBot.Mcp;

[McpServerToolType]
public sealed class ChatBotMcpTools
{
    private readonly ChatBotMcpService _service;

    public ChatBotMcpTools(ChatBotMcpService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [McpServerTool(Name = "chatbot.association.status", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: read association routing status through the governed ChatBot client facade.")]
    public Task<JsonElement> AssociationStatusAsync(string associationId, string? correlationId = null, string? taskId = null, string? tenant = null, CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.association.status", Args(
            ("associationId", associationId),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.association.associate", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit an AssociateEmailToProject command through the governed ChatBot client facade.")]
    public Task<JsonElement> AssociateAsync(
        string associationId,
        string intakeId,
        string projectId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? note = null,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.association.associate", Args(
            ("associationId", associationId),
            ("intakeId", intakeId),
            ("projectId", projectId),
            ("evidenceFingerprint", evidenceFingerprint),
            ("sourceVersion", sourceVersion),
            ("schemaVersion", schemaVersion),
            ("note", note),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.association.reject", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit a RejectEmailProjectAssociation command through the governed ChatBot client facade.")]
    public Task<JsonElement> RejectAsync(
        string associationId,
        string intakeId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? note = null,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.association.reject", Args(
            ("associationId", associationId),
            ("intakeId", intakeId),
            ("evidenceFingerprint", evidenceFingerprint),
            ("sourceVersion", sourceVersion),
            ("schemaVersion", schemaVersion),
            ("note", note),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.association.defer", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit a DeferEmailProjectAssociation command through the governed ChatBot client facade.")]
    public Task<JsonElement> DeferAsync(
        string associationId,
        string intakeId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? note = null,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.association.defer", Args(
            ("associationId", associationId),
            ("intakeId", intakeId),
            ("evidenceFingerprint", evidenceFingerprint),
            ("sourceVersion", sourceVersion),
            ("schemaVersion", schemaVersion),
            ("note", note),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.association.correct", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit a CorrectEmailProjectAssociation command through the governed ChatBot client facade.")]
    public Task<JsonElement> CorrectAsync(
        string associationId,
        string intakeId,
        string priorProjectId,
        string targetProjectId,
        string predecessorAssociationId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? rationale = null,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.association.correct", Args(
            ("associationId", associationId),
            ("intakeId", intakeId),
            ("priorProjectId", priorProjectId),
            ("targetProjectId", targetProjectId),
            ("predecessorAssociationId", predecessorAssociationId),
            ("evidenceFingerprint", evidenceFingerprint),
            ("sourceVersion", sourceVersion),
            ("schemaVersion", schemaVersion),
            ("rationale", rationale),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.conversation.get", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: read project conversation state through the governed ChatBot client facade.")]
    public Task<JsonElement> ConversationGetAsync(string projectId, string? cursor = null, int? pageSize = null, string? correlationId = null, string? taskId = null, string? tenant = null, CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.conversation.get", Args(
            ("projectId", projectId),
            ("cursor", cursor),
            ("pageSize", pageSize),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.task.review", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: read task intent review through the governed ChatBot client facade.")]
    public Task<JsonElement> TaskReviewAsync(string projectId, string taskIntentId, string? correlationId = null, string? taskId = null, string? tenant = null, CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.task.review", Args(
            ("projectId", projectId),
            ("taskIntentId", taskIntentId),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.operation.retry", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit a RequestFailedWorkflowRetry command through the governed ChatBot client facade.")]
    public Task<JsonElement> OperationRetryAsync(
        string retryId,
        string failedEventId,
        string failedOperationClass,
        string failureReasonCode,
        long expectedFailedSourceVersion,
        string? rationale = null,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.operation.retry", Args(
            ("retryId", retryId),
            ("failedEventId", failedEventId),
            ("failedOperationClass", failedOperationClass),
            ("failureReasonCode", failureReasonCode),
            ("expectedFailedSourceVersion", expectedFailedSourceVersion),
            ("rationale", rationale),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.approval.decide", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit a DecideAiActionApproval command through the governed ChatBot client facade.")]
    public Task<JsonElement> ApprovalDecideAsync(
        string projectId,
        string approvalId,
        string proposalId,
        string sourceMessageId,
        string decision,
        long expectedApprovalSourceVersion,
        string commandCorrelationId,
        string decisionId,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.approval.decide", Args(
            ("projectId", projectId),
            ("approvalId", approvalId),
            ("proposalId", proposalId),
            ("sourceMessageId", sourceMessageId),
            ("decision", decision),
            ("expectedApprovalSourceVersion", expectedApprovalSourceVersion),
            ("commandCorrelationId", commandCorrelationId),
            ("decisionId", decisionId),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.ai_action.execute", Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: submit an ExecuteApprovedAIAction command through the governed ChatBot client facade.")]
    public Task<JsonElement> AiActionExecuteAsync(
        string projectId,
        string proposalId,
        string approvalId,
        string taskIntentId,
        string sourceMessageId,
        string requesterId,
        string commandName,
        string commandAllowlistVersion,
        long expectedApprovalSourceVersion,
        long expectedProposalSourceVersion,
        string commandCorrelationId,
        string executionId,
        string transitionId,
        string[]? sourceEvidenceReferences = null,
        string[]? affectedResourceReferences = null,
        string[]? recipientReferences = null,
        string? correlationId = null,
        string? taskId = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.ai_action.execute", Args(
            ("projectId", projectId),
            ("proposalId", proposalId),
            ("approvalId", approvalId),
            ("taskIntentId", taskIntentId),
            ("sourceMessageId", sourceMessageId),
            ("requesterId", requesterId),
            ("commandName", commandName),
            ("commandAllowlistVersion", commandAllowlistVersion),
            ("expectedApprovalSourceVersion", expectedApprovalSourceVersion),
            ("expectedProposalSourceVersion", expectedProposalSourceVersion),
            ("commandCorrelationId", commandCorrelationId),
            ("executionId", executionId),
            ("transitionId", transitionId),
            ("sourceEvidenceReferences", sourceEvidenceReferences),
            ("affectedResourceReferences", affectedResourceReferences),
            ("recipientReferences", recipientReferences),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.operation.status", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: read operation status through the governed ChatBot client facade.")]
    public Task<JsonElement> OperationStatusAsync(string operationId, string? correlationId = null, string? taskId = null, string? tenant = null, CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.operation.status", Args(
            ("operationId", operationId),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    [McpServerTool(Name = "chatbot.operation.audit", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("mcp-exposed: read operation audit through the governed ChatBot client facade.")]
    public Task<JsonElement> OperationAuditAsync(string operationId, string? correlationId = null, string? taskId = null, string? tenant = null, CancellationToken cancellationToken = default)
        => InvokeAsync("chatbot.operation.audit", Args(
            ("operationId", operationId),
            ("correlationId", correlationId),
            ("taskId", taskId),
            ("tenant", tenant)), cancellationToken);

    private Task<JsonElement> InvokeAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
        => _service.InvokeAsync(ChatBotMcpInvocation.Create(toolName, arguments), cancellationToken);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] values)
        => values
            .Where(static pair => pair.Value is not null)
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
}
