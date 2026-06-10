using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;

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

        string completionStatus = WireName(status.CompletionStatus);
        return ToElement(new
        {
            status.OperationId,
            status.CommandId,
            status.CorrelationId,
            lifecycleState = WireName(status.LifecycleState),
            status.RetryCount,
            completionStatus,
            auditStatus = WireName(status.AuditStatus),
            safeNextActions = status.SafeNextActions.Select(WireName).ToArray(),
            terminalReason = status.TerminalReason is null ? null : WireName(status.TerminalReason.Value),
            failureReasonCode = status.FailureReasonCode is null ? null : WireName(status.FailureReasonCode.Value),
            terminalReasonCode = status.TerminalReasonCode is null ? null : WireName(status.TerminalReasonCode.Value),
            status.AcceptedAt,
            status.LastUpdatedAt,
            partialSuccess = string.Equals(completionStatus, "accepted-projection-pending", StringComparison.Ordinal),
        });
    }

    public static JsonElement FormatSafeDenial(Exception exception, string? correlationId = null, string? taskId = null, string? safeSuggestion = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        SafeDenialMetadata denial = SafeDenialMetadata.From(exception, correlationId, taskId, safeSuggestion);
        return ToElement(new
        {
            outcome = "denied",
            denial.Category,
            denial.Code,
            message = denial.SafeMessage,
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
            401 => "authentication-denied",
            403 => "authorization-denied",
            404 => "safe-not-found",
            409 => "validation-error",
            422 => "validation-error",
            _ => "request-denied",
        };

    private static string SafeActionForReason(string reasonCode)
        => reasonCode switch
        {
            "authentication-denied" => "authenticate",
            "authorization-denied" => "request-access",
            "safe-not-found" => "verify-identifier",
            "validation-error" => "correct-request",
            _ => "retry-later",
        };

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

            if (exception is HexalithChatBotApiException<ProblemDetails> problemException)
            {
                ProblemDetails problem = problemException.Result;
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
                const string code = "validation-error";
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
