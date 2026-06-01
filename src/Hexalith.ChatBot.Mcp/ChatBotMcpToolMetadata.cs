namespace Hexalith.ChatBot.Mcp;

public sealed record ChatBotMcpToolMetadata(
    string Name,
    string ContractName,
    bool StateChanging,
    IReadOnlyList<string> RequiredArguments,
    IReadOnlyList<string> OptionalArguments,
    string Description)
{
    public const string ExposureMarker = "mcp-exposed";

    public IReadOnlyList<string> Tags { get; } = [ExposureMarker];

    public bool AllowsArgument(string argument)
        => RequiredArguments.Contains(argument, StringComparer.Ordinal)
            || OptionalArguments.Contains(argument, StringComparer.Ordinal);
}

public static class ChatBotMcpToolCatalog
{
    private static readonly string[] CommonOptionalArguments = ["correlationId", "taskId", "tenant"];
    private static readonly ChatBotMcpToolMetadata[] RegisteredTools =
    [
        Read(
            "chatbot.association.status",
            "GetAssociationRoutingStatus",
            ["associationId"],
            "mcp-exposed read of association routing status through IChatBotClient."),
        Write(
            "chatbot.association.associate",
            "AssociateEmailToProject",
            ["associationId", "intakeId", "projectId", "evidenceFingerprint", "sourceVersion", "schemaVersion"],
            "mcp-exposed association decision command.",
            ["note"]),
        Write(
            "chatbot.association.reject",
            "RejectEmailProjectAssociation",
            ["associationId", "intakeId", "evidenceFingerprint", "sourceVersion", "schemaVersion"],
            "mcp-exposed association rejection command.",
            ["note"]),
        Write(
            "chatbot.association.defer",
            "DeferEmailProjectAssociation",
            ["associationId", "intakeId", "evidenceFingerprint", "sourceVersion", "schemaVersion"],
            "mcp-exposed association deferral command.",
            ["note"]),
        Write(
            "chatbot.association.correct",
            "CorrectEmailProjectAssociation",
            ["associationId", "intakeId", "priorProjectId", "targetProjectId", "predecessorAssociationId", "evidenceFingerprint", "sourceVersion", "schemaVersion"],
            "mcp-exposed association correction command.",
            ["rationale"]),
        Read(
            "chatbot.conversation.get",
            "GetProjectConversation",
            ["projectId"],
            "mcp-exposed project conversation read through IChatBotClient.",
            ["cursor", "pageSize"]),
        Read(
            "chatbot.task.review",
            "GetTaskIntentReview",
            ["projectId", "taskIntentId"],
            "mcp-exposed task review read through IChatBotClient."),
        Write(
            "chatbot.operation.retry",
            "RequestFailedWorkflowRetry",
            ["retryId", "failedEventId", "failedOperationClass", "failureReasonCode", "expectedFailedSourceVersion"],
            "mcp-exposed failed operation retry command.",
            ["rationale"]),
        Write(
            "chatbot.approval.decide",
            "DecideAiActionApproval",
            ["projectId", "approvalId", "proposalId", "sourceMessageId", "decision", "expectedApprovalSourceVersion", "commandCorrelationId", "decisionId"],
            "mcp-exposed AI action approval decision command."),
        Write(
            "chatbot.ai_action.execute",
            "ExecuteApprovedAIAction",
            ["projectId", "proposalId", "approvalId", "taskIntentId", "sourceMessageId", "requesterId", "commandName", "commandAllowlistVersion", "expectedApprovalSourceVersion", "expectedProposalSourceVersion", "commandCorrelationId", "executionId", "transitionId"],
            "mcp-exposed approved AI action execution command.",
            ["sourceEvidenceReferences", "affectedResourceReferences", "recipientReferences"]),
        Read(
            "chatbot.operation.status",
            "GetOperationStatus",
            ["operationId"],
            "mcp-exposed operation status read through IChatBotClient."),
        Read(
            "chatbot.operation.audit",
            "GetOperationAuditHistory",
            ["operationId"],
            "mcp-exposed operation audit read through IChatBotClient."),
    ];

    public static IReadOnlyList<ChatBotMcpToolMetadata> Tools => RegisteredTools;

    public static bool TryGet(string toolName, out ChatBotMcpToolMetadata metadata)
    {
        metadata = RegisteredTools.FirstOrDefault(tool => string.Equals(tool.Name, toolName, StringComparison.Ordinal))!;
        return metadata is not null;
    }

    public static string NearestToolName(string toolName)
        => RegisteredTools
            .OrderBy(tool => EditDistance(toolName, tool.Name))
            .ThenBy(tool => tool.Name, StringComparer.Ordinal)
            .First()
            .Name;

    private static ChatBotMcpToolMetadata Read(
        string name,
        string contractName,
        string[] requiredArguments,
        string description,
        string[]? extraOptionalArguments = null)
        => new(
            name,
            contractName,
            StateChanging: false,
            requiredArguments,
            [.. CommonOptionalArguments, .. (extraOptionalArguments ?? [])],
            description);

    private static ChatBotMcpToolMetadata Write(
        string name,
        string contractName,
        string[] requiredArguments,
        string description,
        string[]? extraOptionalArguments = null)
        => new(
            name,
            contractName,
            StateChanging: true,
            requiredArguments,
            [.. CommonOptionalArguments, .. (extraOptionalArguments ?? [])],
            description);

    private static int EditDistance(string left, string right)
    {
        int[,] distance = new int[left.Length + 1, right.Length + 1];
        for (int i = 0; i <= left.Length; i++)
        {
            distance[i, 0] = i;
        }

        for (int j = 0; j <= right.Length; j++)
        {
            distance[0, j] = j;
        }

        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[left.Length, right.Length];
    }
}
