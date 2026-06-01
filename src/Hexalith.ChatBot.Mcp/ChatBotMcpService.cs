using System.Globalization;
using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Mcp;

using AssociateEmailToProjectCommand = Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject;
using ApprovalDecision = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;
using AssociationCorrection = Hexalith.ChatBot.Contracts.Enums.AssociationCorrectionKind;
using AssociationDecision = Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind;
using CorrectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation;
using DecideAiActionApprovalCommand = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using DeferEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.DeferEmailProjectAssociation;
using ExecuteApprovedAIActionCommand = Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction;
using RejectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.RejectEmailProjectAssociation;
using RequestFailedWorkflowRetryCommand = Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry;

public sealed class ChatBotMcpService
{
    private readonly IChatBotClient _client;
    private static readonly HashSet<string> NumericArguments =
    [
        "sourceVersion",
        "expectedFailedSourceVersion",
        "expectedApprovalSourceVersion",
        "expectedProposalSourceVersion",
        "pageSize",
    ];

    private static readonly HashSet<string> StringListArguments =
    [
        "sourceEvidenceReferences",
        "affectedResourceReferences",
        "recipientReferences",
    ];

    public ChatBotMcpService(IChatBotClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<JsonElement> InvokeAsync(ChatBotMcpInvocation invocation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        try
        {
            if (!ChatBotMcpToolCatalog.TryGet(invocation.ToolName, out ChatBotMcpToolMetadata metadata))
            {
                throw McpToolDeniedException.UnknownTool(invocation.ToolName);
            }

            ToolArguments arguments = ValidateArguments(metadata, invocation.Arguments);
            return invocation.ToolName switch
            {
                "chatbot.association.status" => ChatBotMcpResultFormatter.FormatReadResult(await _client.GetAssociationRoutingStatusAsync(
                    arguments.RequiredString("associationId"),
                    arguments.OptionalString("correlationId"),
                    arguments.OptionalString("taskId"),
                    cancellationToken).ConfigureAwait(false)),
                "chatbot.association.associate" => await SubmitAsync(
                    new AssociateEmailToProjectCommand(
                        arguments.RequiredString("associationId"),
                        arguments.RequiredString("intakeId"),
                        arguments.RequiredString("projectId"),
                        AssociationDecision.Associate,
                        arguments.OptionalString("note"),
                        arguments.RequiredString("evidenceFingerprint"),
                        arguments.RequiredLong("sourceVersion"),
                        arguments.RequiredString("schemaVersion")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.association.reject" => await SubmitAsync(
                    new RejectEmailProjectAssociationCommand(
                        arguments.RequiredString("associationId"),
                        arguments.RequiredString("intakeId"),
                        AssociationDecision.Reject,
                        arguments.OptionalString("note"),
                        arguments.RequiredString("evidenceFingerprint"),
                        arguments.RequiredLong("sourceVersion"),
                        arguments.RequiredString("schemaVersion")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.association.defer" => await SubmitAsync(
                    new DeferEmailProjectAssociationCommand(
                        arguments.RequiredString("associationId"),
                        arguments.RequiredString("intakeId"),
                        AssociationDecision.Defer,
                        arguments.OptionalString("note"),
                        arguments.RequiredString("evidenceFingerprint"),
                        arguments.RequiredLong("sourceVersion"),
                        arguments.RequiredString("schemaVersion")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.association.correct" => await SubmitAsync(
                    new CorrectEmailProjectAssociationCommand(
                        arguments.RequiredString("associationId"),
                        arguments.RequiredString("intakeId"),
                        arguments.RequiredString("priorProjectId"),
                        arguments.RequiredString("targetProjectId"),
                        AssociationCorrection.ProjectReassignment,
                        arguments.OptionalString("rationale"),
                        arguments.RequiredString("predecessorAssociationId"),
                        arguments.RequiredString("evidenceFingerprint"),
                        arguments.RequiredLong("sourceVersion"),
                        arguments.RequiredString("schemaVersion")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.conversation.get" => ChatBotMcpResultFormatter.FormatReadResult(await _client.GetProjectConversationAsync(
                    arguments.RequiredString("projectId"),
                    arguments.OptionalString("cursor"),
                    arguments.OptionalInt("pageSize") ?? 25,
                    arguments.OptionalString("correlationId"),
                    arguments.OptionalString("taskId"),
                    cancellationToken).ConfigureAwait(false)),
                "chatbot.task.review" => ChatBotMcpResultFormatter.FormatReadResult(await _client.GetTaskIntentReviewAsync(
                    arguments.RequiredString("projectId"),
                    arguments.RequiredString("taskIntentId"),
                    arguments.OptionalString("correlationId"),
                    arguments.OptionalString("taskId"),
                    cancellationToken).ConfigureAwait(false)),
                "chatbot.operation.retry" => await SubmitAsync(
                    new RequestFailedWorkflowRetryCommand(
                        arguments.RequiredString("retryId"),
                        arguments.RequiredString("failedEventId"),
                        arguments.RequiredString("failedOperationClass"),
                        arguments.RequiredString("failureReasonCode"),
                        arguments.RequiredLong("expectedFailedSourceVersion"),
                        arguments.OptionalString("rationale")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.approval.decide" => await SubmitAsync(
                    new DecideAiActionApprovalCommand(
                        arguments.RequiredString("projectId"),
                        arguments.RequiredString("approvalId"),
                        arguments.RequiredString("proposalId"),
                        arguments.RequiredString("sourceMessageId"),
                        arguments.RequiredApprovalDecision("decision"),
                        arguments.RequiredLong("expectedApprovalSourceVersion"),
                        arguments.RequiredString("commandCorrelationId"),
                        arguments.RequiredString("decisionId")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.ai_action.execute" => await SubmitAsync(
                    new ExecuteApprovedAIActionCommand(
                        arguments.RequiredString("projectId"),
                        arguments.RequiredString("proposalId"),
                        arguments.RequiredString("approvalId"),
                        arguments.RequiredString("taskIntentId"),
                        arguments.RequiredString("sourceMessageId"),
                        arguments.RequiredString("requesterId"),
                        arguments.RequiredString("commandName"),
                        arguments.RequiredString("commandAllowlistVersion"),
                        arguments.RequiredLong("expectedApprovalSourceVersion"),
                        arguments.RequiredLong("expectedProposalSourceVersion"),
                        arguments.RequiredString("commandCorrelationId"),
                        arguments.RequiredString("executionId"),
                        arguments.RequiredString("transitionId"),
                        arguments.OptionalStringList("sourceEvidenceReferences"),
                        arguments.OptionalStringList("affectedResourceReferences"),
                        arguments.OptionalStringList("recipientReferences")),
                    arguments,
                    cancellationToken).ConfigureAwait(false),
                "chatbot.operation.status" => ChatBotMcpResultFormatter.FormatOperationStatus(await _client.GetOperationStatusAsync(
                    arguments.RequiredString("operationId"),
                    arguments.OptionalString("correlationId"),
                    arguments.OptionalString("taskId"),
                    cancellationToken).ConfigureAwait(false)),
                "chatbot.operation.audit" => ChatBotMcpResultFormatter.FormatReadResult(await _client.GetOperationAuditHistoryAsync(
                    arguments.RequiredString("operationId"),
                    arguments.OptionalString("correlationId"),
                    arguments.OptionalString("taskId"),
                    cancellationToken).ConfigureAwait(false)),
                _ => throw McpToolDeniedException.UnknownTool(invocation.ToolName),
            };
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            string? correlationId = TryString(invocation.Arguments, "correlationId");
            string? taskId = TryString(invocation.Arguments, "taskId");
            return ChatBotMcpResultFormatter.FormatSafeDenial(ex, correlationId, taskId);
        }
    }

    private async Task<JsonElement> SubmitAsync(
        Hexalith.ChatBot.Contracts.Commands.IChatBotCommand command,
        ToolArguments arguments,
        CancellationToken cancellationToken)
    {
        Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse response = await _client
            .SubmitAsync(
                command,
                arguments.OptionalString("correlationId"),
                arguments.OptionalString("taskId"),
                ChatBotSurfaceOrigin.Mcp,
                cancellationToken)
            .ConfigureAwait(false);

        return ChatBotMcpResultFormatter.FormatCommandAccepted(response);
    }

    private static ToolArguments ValidateArguments(ChatBotMcpToolMetadata metadata, IReadOnlyDictionary<string, object?> arguments)
    {
        foreach (string argument in arguments.Keys)
        {
            if (!metadata.AllowsArgument(argument))
            {
                throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.unsupported",
                    "The MCP tool argument is not supported for this tool.",
                    $"Use {metadata.Name} with supported argument keys only.");
            }

            EnsureArgumentShape(argument, arguments[argument]);
        }

        var validated = new ToolArguments(arguments);
        foreach (string required in metadata.RequiredArguments)
        {
            if (!validated.HasRequiredValue(required))
            {
                throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.missing",
                    "A required MCP tool argument is missing.",
                    $"Provide {required}.");
            }
        }

        return validated;
    }

    private static void EnsureArgumentShape(string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (NumericArguments.Contains(name))
        {
            _ = ToolArguments.TryReadLong(value, out _)
                ? true
                : throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-number",
                    "A numeric MCP tool argument is invalid.",
                    $"Provide {name} as an integer.");
            return;
        }

        if (StringListArguments.Contains(name))
        {
            ToolArguments.EnsureStringList(name, value);
            return;
        }

        _ = ToolArguments.TryReadString(value, out _)
            ? true
            : throw McpToolDeniedException.InvalidArgument(
                "mcp.argument.invalid-string",
                "A text MCP tool argument is invalid.",
                $"Provide {name} as text.");
    }

    private static string? TryString(IReadOnlyDictionary<string, object?> arguments, string name)
        => arguments.TryGetValue(name, out object? value) ? ToolArguments.ToStringValue(value) : null;

    private static bool IsSafeClientFailure(Exception ex)
        => ex is McpToolDeniedException or ArgumentException or Hexalith.ChatBot.Client.Generated.HexalithChatBotApiException or InvalidOperationException;

    private sealed class ToolArguments
    {
        private readonly IReadOnlyDictionary<string, object?> _arguments;

        public ToolArguments(IReadOnlyDictionary<string, object?> arguments)
        {
            _arguments = arguments;
        }

        public bool HasRequiredValue(string name)
            => _arguments.TryGetValue(name, out object? value)
                && value is not null
                && (TryReadString(value, out string? text)
                    ? !string.IsNullOrWhiteSpace(text)
                    : true);

        public string RequiredString(string name)
            => OptionalString(name) is { Length: > 0 } value
                ? value
                : throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.missing",
                    "A required MCP tool argument is missing.",
                    $"Provide {name}.");

        public string? OptionalString(string name)
        {
            if (!_arguments.TryGetValue(name, out object? value) || value is null)
            {
                return null;
            }

            return TryReadString(value, out string? text)
                ? text
                : throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-string",
                    "A text MCP tool argument is invalid.",
                    $"Provide {name} as text.");
        }

        public long RequiredLong(string name)
        {
            object? value = _arguments.TryGetValue(name, out object? raw) ? raw : null;
            return value is not null && TryReadLong(value, out long number)
                ? number
                : throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-number",
                    "A numeric MCP tool argument is invalid.",
                    $"Provide {name} as an integer.");
        }

        public int? OptionalInt(string name)
        {
            if (!_arguments.ContainsKey(name))
            {
                return null;
            }

            long value = RequiredLong(name);
            if (value is < int.MinValue or > int.MaxValue)
            {
                throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-number",
                    "A numeric MCP tool argument is invalid.",
                    $"Provide {name} within the supported range.");
            }

            return (int)value;
        }

        public IReadOnlyList<string> OptionalStringList(string name)
        {
            if (!_arguments.TryGetValue(name, out object? value) || value is null)
            {
                return [];
            }

            return value switch
            {
                string text when string.IsNullOrWhiteSpace(text) => [],
                string text => [text],
                string[] array => array.Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
                IEnumerable<string> values => values.Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
                JsonElement { ValueKind: JsonValueKind.Array } element => element
                    .EnumerateArray()
                    .Select(static element => TryReadString(element, out string? text) ? text : null)
                    .Where(static item => !string.IsNullOrWhiteSpace(item))
                    .Select(static item => item!)
                    .ToArray(),
                _ => throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-list",
                    "A list MCP tool argument is invalid.",
                    $"Provide {name} as a string list."),
            };
        }

        public ApprovalDecision RequiredApprovalDecision(string name)
            => RequiredString(name) switch
            {
                "approve" => ApprovalDecision.Approve,
                "reject" => ApprovalDecision.Reject,
                "request-revision" => ApprovalDecision.RequestRevision,
                "cancel" => ApprovalDecision.Cancel,
                _ => throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-enum",
                    "An MCP tool enum argument is invalid.",
                    "Use approve, reject, request-revision, or cancel."),
            };

        public static string? ToStringValue(object? value)
            => TryReadString(value, out string? text) ? text : null;

        public static bool TryReadString(object? value, out string? text)
        {
            text = value switch
            {
                null => null,
                string stringValue => string.IsNullOrWhiteSpace(stringValue) ? null : stringValue,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                JsonElement { ValueKind: JsonValueKind.Null } => null,
                JsonElement { ValueKind: JsonValueKind.Undefined } => null,
                _ => null,
            };

            return value is null
                || value is string
                || value is JsonElement { ValueKind: JsonValueKind.String or JsonValueKind.Null or JsonValueKind.Undefined };
        }

        public static bool TryReadLong(object? value, out long number)
        {
            switch (value)
            {
                case long longValue:
                    number = longValue;
                    return true;
                case int intValue:
                    number = intValue;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetInt64(out long jsonValue):
                    number = jsonValue;
                    return true;
                case string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long stringValue):
                    number = stringValue;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.String } element
                    when long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long jsonStringValue):
                    number = jsonStringValue;
                    return true;
                default:
                    number = default;
                    return false;
            }
        }

        public static void EnsureStringList(string name, object value)
        {
            bool valid = value switch
            {
                string => true,
                string[] => true,
                IEnumerable<string> => true,
                JsonElement { ValueKind: JsonValueKind.Array } element => element
                    .EnumerateArray()
                    .All(static item => TryReadString(item, out string? text) && !string.IsNullOrWhiteSpace(text)),
                _ => throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-list",
                    "A list MCP tool argument is invalid.",
                    $"Provide {name} as a string list."),
            };

            if (!valid)
            {
                throw McpToolDeniedException.InvalidArgument(
                    "mcp.argument.invalid-list",
                    "A list MCP tool argument is invalid.",
                    $"Provide {name} as a string list.");
            }
        }
    }
}
