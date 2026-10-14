using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Mcp;

public static class ChatBotMcpResultFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // The generated read DTOs annotate their enums with Newtonsoft.Json + [EnumMember], neither of which
        // System.Text.Json honors. Without this converter FormatReadResult would emit raw integer ordinals
        // (e.g. redactionState:0, lifecycleState:5) that are version-brittle and diverge from the governed
        // wire-name strings FormatOperationStatus already produces. This keeps every MCP surface enum stable.
        Converters = { new EnumMemberJsonConverterFactory() },
    };

    public static JsonElement FormatCommandAccepted(CommandSubmissionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return ToElement(new
        {
            outcome = "command-accepted",
            operationId = response.TaskId ?? response.CommandId,
            commandId = response.CommandId,
            correlationId = response.CorrelationId,
            taskId = response.TaskId,
            lifecycleState = WireName(response.LifecycleState),
            acceptedAt = response.AcceptedAt,
            completionStatus = "accepted-projection-pending",
            auditStatus = "reconciling",
            retryCount = 0,
            safeNextActions = new[] { "operation status", "operation audit" },
            terminalReason = (string?)null,
            failureReasonCode = (string?)null,
            terminalReasonCode = (string?)null,
        });
    }

    public static JsonElement FormatOperationStatus(OperationStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        bool knownOutcome = Enum.IsDefined(status.LifecycleState) &&
            Enum.IsDefined(status.CompletionStatus) && Enum.IsDefined(status.AuditStatus) &&
            KnownStatusCode(status.ReasonCode) &&
            KnownStatusCode(status.TerminalReason) &&
            KnownStatusCode(status.FailureReasonCode) &&
            KnownStatusCode(status.TerminalReasonCode) &&
            KnownStatusCode(status.WorkflowLastFailureCode);
        string completionStatus = knownOutcome ? WireName(status.CompletionStatus) : "failed";
        string? fallbackCode = knownOutcome ? null : ChatBotMessageCatalog.Resolve(null).Code;
        return ToElement(new
        {
            status.OperationId,
            status.CommandId,
            status.CorrelationId,
            lifecycleState = knownOutcome ? WireName(status.LifecycleState) : WireName(LifecycleState.Failed),
            retryCount = knownOutcome ? status.RetryCount : 0,
            completionStatus,
            auditStatus = Enum.IsDefined(status.AuditStatus) ? WireName(status.AuditStatus) : "unavailable",
            safeNextActions = knownOutcome ? status.SafeNextActions.Select(SafeAction).ToArray() : [ChatBotMessageNextActions.Dismiss],
            terminalReason = !knownOutcome ? fallbackCode : status.TerminalReason is null ? null : SafeCode(status.TerminalReason.Value),
            failureReasonCode = !knownOutcome ? fallbackCode : status.FailureReasonCode is null ? null : SafeCode(status.FailureReasonCode.Value),
            terminalReasonCode = !knownOutcome ? fallbackCode : status.TerminalReasonCode is null ? null : SafeCode(status.TerminalReasonCode.Value),
            status.AcceptedAt,
            status.LastUpdatedAt,
            partialSuccess = string.Equals(completionStatus, "accepted-projection-pending", StringComparison.Ordinal),
        });
    }

    public static JsonElement FormatSafeDenial(Exception exception, string? correlationId = null, string? taskId = null, string? safeSuggestion = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        SafeDenialMetadata raw = SafeDenialMetadata.From(exception, correlationId, taskId, safeSuggestion);
        string catalogCode = raw.Code switch
        {
            "mcp.argument.unsupported" or
            "mcp.argument.missing" or
            "mcp.argument.invalid-number" or
            "mcp.argument.invalid-string" or
            "mcp.argument.invalid-list" or
            "mcp.argument.invalid-enum" or
            "mcp.tool.unknown" => ChatBotMessageCodes.CommandContractInvalid,
            _ => raw.Code,
        };
        bool compatible = ChatBotMessageCatalog.IsKnown(catalogCode) &&
            (ChatBotMessageCatalog.Resolve(catalogCode).State is not ("waiting" or "completed") ||
                catalogCode.StartsWith("idempotency_conflict_", StringComparison.Ordinal)) &&
            (raw.Category is null or "request_denied" || Enum.GetValues<ProblemDetailsCategory>()
                .Any(category => WireName(category) == raw.Category));
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(compatible ? catalogCode : null);
        bool retryable = raw.Retryable && !entry.IsTerminal &&
            entry.NextAction == ChatBotMessageNextActions.RetryLater && raw.ClientAction == entry.NextAction;
        string action = entry.Code == ChatBotMessageCodes.GenericDenial ? ChatBotMessageNextActions.Dismiss
            : raw.ClientAction == entry.NextAction &&
                (entry.NextAction != ChatBotMessageNextActions.RetryLater || retryable)
                ? entry.NextAction : ChatBotMessageNextActions.None;
        SafeDenialMetadata denial = raw with
        {
            Category = entry.Code == ChatBotMessageCodes.GenericDenial ? "request_denied" : raw.Category ?? "request_denied",
            Code = entry.Code,
            SafeMessage = entry.Reason,
            Retryable = retryable,
            ClientAction = action,
            DetailsVisibility = "metadata-only",
            SafeSuggestion = action,
        };
        return ToElement(new
        {
            outcome = "denied",
            denial.Category,
            denial.Code,
            message = denial.SafeMessage,
            headline = entry.Headline,
            catalogVersion = entry.Version,
            denial.CorrelationId,
            denial.TaskId,
            denial.Retryable,
            clientAction = denial.ClientAction,
            detailsVisibility = denial.DetailsVisibility,
            safeSuggestion = denial.SafeSuggestion,
        });
    }

    public static JsonElement FormatReadResult(object response)
        => ToElement(response);

    private static JsonElement ToElement(object value)
        => JsonSerializer.SerializeToElement(value, JsonOptions);

    private static string WireName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = Enum.GetName(value) ?? value.ToString();
        FieldInfo? field = typeof(TEnum).GetField(name);
        return field?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? name;
    }

    private static bool KnownStatusCode(ChatBotMessageCode? value)
        => value is null || (ChatBotMessageCatalog.IsKnown(WireName(value.Value)) &&
            value != ChatBotMessageCode.Generic_denial);

    private static string SafeCode(ChatBotMessageCode value)
        => ChatBotMessageCatalog.Resolve(WireName(value)).Code;

    private static string SafeAction(ChatBotMessageNextAction value)
        => Enum.IsDefined(value) ? WireName(value) : ChatBotMessageNextActions.Dismiss;

    private sealed class EnumMemberJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(
                typeof(EnumMemberJsonConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        private static readonly Dictionary<string, TEnum> FromWire = BuildFromWire();

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? token = reader.GetString();
                if (token is not null && FromWire.TryGetValue(token, out TEnum value))
                {
                    return value;
                }

                if (token is not null && Enum.TryParse(token, ignoreCase: true, out TEnum parsed))
                {
                    return parsed;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long number))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), number);
            }

            return default;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => writer.WriteStringValue(WireName(value));

        private static Dictionary<string, TEnum> BuildFromWire()
        {
            var map = new Dictionary<string, TEnum>(StringComparer.Ordinal);
            foreach (TEnum value in Enum.GetValues<TEnum>())
            {
                map[WireName(value)] = value;
            }

            return map;
        }
    }

    private static string ReasonCodeForStatus(int statusCode)
        => statusCode switch
        {
            401 => ChatBotMessageCodes.AuthenticationDenied,
            403 or 404 => ChatBotMessageCodes.AuthorizationDenied,
            400 or 422 => ChatBotMessageCodes.CommandContractInvalid,
            409 => ChatBotMessageCodes.GenericDenial,
            408 or 429 or >= 500 => ChatBotMessageCodes.DependencyDegraded,
            _ => ChatBotMessageCodes.GenericDenial,
        };

    private static string SafeActionForReason(string reasonCode)
        => ChatBotMessageCatalog.Resolve(reasonCode).NextAction;

    private sealed record SafeDenialMetadata(
        string Category,
        string Code,
        string SafeMessage,
        string? CorrelationId,
        string? TaskId,
        bool Retryable,
        string ClientAction,
        string DetailsVisibility,
        string SafeSuggestion)
    {
        public static SafeDenialMetadata From(Exception exception, string? correlationId, string? taskId, string? safeSuggestion)
        {
            if (exception is McpToolDeniedException mcp)
            {
                return new SafeDenialMetadata(
                    mcp.Category,
                    mcp.Code,
                    mcp.SafeMessage,
                    mcp.CorrelationId ?? correlationId,
                    mcp.TaskId ?? taskId,
                    mcp.Retryable,
                    mcp.ClientAction,
                    "metadata-only",
                    mcp.SafeSuggestion);
            }

            if (exception is HexalithChatBotApiException<ProblemDetails> { Result: { } problem })
            {
                string action = WireName(problem.ClientAction);
                return new SafeDenialMetadata(
                    WireName(problem.Category),
                    string.IsNullOrWhiteSpace(problem.Code) ? "request-denied" : problem.Code,
                    string.IsNullOrWhiteSpace(problem.Message) ? "Request denied." : problem.Message,
                    problem.CorrelationId ?? correlationId,
                    problem.TaskId ?? taskId,
                    problem.Retryable,
                    action,
                    Visibility(problem.Details?.Visibility),
                    safeSuggestion ?? action);
            }

            if (exception is HexalithChatBotApiException api)
            {
                string code = ReasonCodeForStatus(api.StatusCode);
                string action = SafeActionForReason(code);
                return new SafeDenialMetadata(
                    "request_denied",
                    code,
                    "Request denied.",
                    correlationId,
                    taskId,
                    api.StatusCode is 408 or 429 or >= 500,
                    action,
                    "metadata-only",
                    safeSuggestion ?? action);
            }

            if (exception is ArgumentException or InvalidOperationException)
            {
                const string code = ChatBotMessageCodes.CommandContractInvalid;
                return new SafeDenialMetadata(
                    "validation_error",
                    code,
                    "The request could not be accepted.",
                    correlationId,
                    taskId,
                    false,
                    "correct-request",
                    "metadata-only",
                    safeSuggestion ?? "correct-request");
            }

            if (exception is HttpRequestException or TimeoutException)
            {
                return new SafeDenialMetadata(
                    "internal_error",
                    ChatBotMessageCodes.DependencyDegraded,
                    "A required dependency is temporarily degraded.",
                    correlationId,
                    taskId,
                    true,
                    ChatBotMessageNextActions.RetryLater,
                    "metadata-only",
                    ChatBotMessageNextActions.RetryLater);
            }

            return new SafeDenialMetadata(
                "request_denied",
                "request-denied",
                "Request denied.",
                correlationId,
                taskId,
                false,
                "retry-later",
                "metadata-only",
                safeSuggestion ?? "retry-later");
        }

        private static string Visibility(ProblemDetailsDetailsVisibility? visibility)
            => visibility is null ? "metadata-only" : WireName(visibility.Value).Replace('_', '-');
    }
}
