using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Cli;

public static class ChatBotCliOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string FormatCommandAccepted(CommandSubmissionResponse response, bool json)
    {
        ArgumentNullException.ThrowIfNull(response);

        object shape = new
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
        };

        return json ? ToJson(shape) : Lines(
            "outcome: command-accepted",
            $"operation-id: {response.TaskId ?? response.CommandId}",
            $"command-id: {response.CommandId}",
            $"correlation-id: {response.CorrelationId}",
            $"lifecycle-state: {WireName(response.LifecycleState)}",
            "completion-status: accepted-projection-pending",
            "audit-status: reconciling",
            "retry-count: 0",
            "safe-next-action: operation status",
            "safe-next-action: operation audit");
    }

    public static string FormatOperationStatus(OperationStatus status, bool json)
    {
        ArgumentNullException.ThrowIfNull(status);

        bool knownOutcome = Enum.IsDefined(status.LifecycleState) &&
            Enum.IsDefined(status.CompletionStatus) && Enum.IsDefined(status.AuditStatus) &&
            KnownStatusCode(status.ReasonCode) &&
            KnownStatusCode(status.TerminalReason) &&
            KnownStatusCode(status.FailureReasonCode) &&
            KnownStatusCode(status.TerminalReasonCode) &&
            KnownStatusCode(status.WorkflowLastFailureCode);
        string completion = knownOutcome ? WireName(status.CompletionStatus) : "failed";
        string lifecycle = knownOutcome ? WireName(status.LifecycleState) : WireName(LifecycleState.Failed);
        string[] actions = knownOutcome
            ? status.SafeNextActions.Select(SafeAction).ToArray()
            : [ChatBotMessageNextActions.Dismiss];
        string? terminalReason = knownOutcome
            ? status.TerminalReason is null ? null : SafeCode(status.TerminalReason.Value)
            : ChatBotMessageCatalog.Resolve(null).Code;
        string? failureReasonCode = knownOutcome
            ? status.FailureReasonCode is null ? null : SafeCode(status.FailureReasonCode.Value)
            : ChatBotMessageCodes.GenericDenial;
        string? terminalReasonCode = knownOutcome
            ? status.TerminalReasonCode is null ? null : SafeCode(status.TerminalReasonCode.Value)
            : ChatBotMessageCodes.GenericDenial;
        object shape = new
        {
            operationId = status.OperationId,
            commandId = status.CommandId,
            correlationId = status.CorrelationId,
            lifecycleState = lifecycle,
            retryCount = status.RetryCount,
            completionStatus = completion,
            auditStatus = Enum.IsDefined(status.AuditStatus) ? WireName(status.AuditStatus) : "unavailable",
            safeNextActions = actions,
            terminalReason,
            failureReasonCode,
            terminalReasonCode,
            acceptedAt = status.AcceptedAt,
            lastUpdatedAt = status.LastUpdatedAt,
            partialSuccess = completion == "accepted-projection-pending",
        };

        if (json)
        {
            return ToJson(shape);
        }

        List<string> lines =
        [
            $"operation-id: {status.OperationId}",
            $"command-id: {status.CommandId}",
            $"correlation-id: {status.CorrelationId}",
            $"lifecycle-state: {lifecycle}",
            $"completion-status: {completion}",
            $"audit-status: {(Enum.IsDefined(status.AuditStatus) ? WireName(status.AuditStatus) : "unavailable")}",
            $"retry-count: {status.RetryCount}",
        ];

        if (completion == "accepted-projection-pending")
        {
            lines.Add("partial-success: accepted by backend; projection reconciliation is pending");
        }

        foreach (string action in actions)
        {
            lines.Add($"safe-next-action: {action}");
        }

        if (terminalReason is not null)
        {
            lines.Add($"terminal-reason: {terminalReason}");
        }

        if (failureReasonCode is not null)
        {
            lines.Add($"failure-reason-code: {failureReasonCode}");
        }

        if (terminalReasonCode is not null)
        {
            lines.Add($"terminal-reason-code: {terminalReasonCode}");
        }

        return Lines(lines);
    }

    public static string FormatOperationAudit(OperationAuditHistory history, bool json)
    {
        ArgumentNullException.ThrowIfNull(history);

        object shape = new
        {
            operationId = history.OperationId,
            auditStatus = WireName(history.AuditStatus),
            entries = history.Entries.Select(static entry => new
            {
                phase = WireName(entry.Phase),
                entry.Decision,
                entry.ReasonCode,
                entry.Outcome,
                entry.StateTransition,
                redactionDecision = WireName(entry.RedactionDecision),
            }).ToArray(),
        };

        return json ? ToJson(shape) : Lines(
            $"operation-id: {history.OperationId}",
            $"audit-status: {WireName(history.AuditStatus)}",
            $"entry-count: {history.Entries.Count}");
    }

    public static string FormatAssociationStatus(AssociationRoutingStatus status, bool json)
    {
        ArgumentNullException.ThrowIfNull(status);

        object shape = new
        {
            associationId = status.AssociationId,
            intakeId = status.IntakeId,
            lifecycleState = WireName(status.LifecycleState),
            outcome = WireName(status.Outcome),
            thresholdBand = WireName(status.ThresholdBand),
            status.ConfidenceScore,
            correlationId = status.CorrelationId,
            redactionState = WireName(status.RedactionState),
            safeNextAction = status.SafeNextAction,
            reasonCodes = status.ReasonCodes.Select(WireName).ToArray(),
            candidates = status.Candidates
                .OrderBy(static candidate => candidate.Rank)
                .Select(static candidate => new
                {
                    candidate.ProjectId,
                    candidate.DisplayName,
                    candidate.ConfidenceScore,
                    candidate.Rank,
                    reasonCodes = candidate.ReasonCodes.Select(WireName).ToArray(),
                    evidenceRefs = candidate.EvidenceRefs.Select(static evidence => new
                    {
                        evidence.EvidenceReference,
                        evidence.EvidenceFingerprint,
                        evidence.EvidenceKind,
                        redactionState = evidence.RedactionState is null ? null : WireName(evidence.RedactionState.Value),
                    }).ToArray(),
                }).ToArray(),
        };

        if (json)
        {
            return ToJson(shape);
        }

        List<string> lines =
        [
            $"association-id: {status.AssociationId}",
            $"intake-id: {status.IntakeId}",
            $"lifecycle-state: {WireName(status.LifecycleState)}",
            $"outcome: {WireName(status.Outcome)}",
            $"threshold-band: {WireName(status.ThresholdBand)}",
            $"confidence-score: {status.ConfidenceScore}",
            $"correlation-id: {status.CorrelationId}",
            $"redaction-state: {WireName(status.RedactionState)}",
        ];

        if (!string.IsNullOrWhiteSpace(status.SafeNextAction))
        {
            lines.Add($"safe-next-action: {status.SafeNextAction}");
        }

        foreach (AssociationCandidate candidate in status.Candidates.OrderBy(static candidate => candidate.Rank))
        {
            lines.Add(
                $"candidate: rank={candidate.Rank} project-id={candidate.ProjectId} display-name={SafeValue(candidate.DisplayName)} confidence={candidate.ConfidenceScore} required-evidence-complete={candidate.RequiredEvidenceComplete.ToString().ToLowerInvariant()}");
            lines.Add($"candidate-reason-codes: rank={candidate.Rank} values={JoinWire(candidate.ReasonCodes)}");

            foreach (AssociationEvidenceReference evidence in candidate.EvidenceRefs)
            {
                lines.Add(
                    $"candidate-evidence: rank={candidate.Rank} reference={evidence.EvidenceReference} fingerprint={evidence.EvidenceFingerprint} kind={evidence.EvidenceKind} redaction-state={WireNameOrNone(evidence.RedactionState)} visibility-state={WireNameOrNone(evidence.VisibilityState)} freshness-state={WireNameOrNone(evidence.FreshnessState)}");
            }
        }

        lines.Add($"reason-codes: {JoinWire(status.ReasonCodes)}");
        lines.Add($"next-action-reason-codes: {JoinWire(status.NextActionReasonCodes)}");
        lines.Add($"disabled-action-reason-codes: {JoinStrings(status.DisabledActionReasonCodes)}");

        return Lines(lines);
    }

    public static string FormatProjectConversation(ProjectConversationResponse response, bool json)
    {
        ArgumentNullException.ThrowIfNull(response);

        object shape = new
        {
            projectId = response.ProjectId,
            status = WireName(response.Status),
            conversationState = WireName(response.ConversationState),
            correlationId = response.CorrelationId,
            redactionState = WireName(response.RedactionState),
            safeNextAction = response.SafeNextAction,
            itemCount = response.Items.Count,
        };

        return json ? ToJson(shape) : Lines(
            $"project-id: {response.ProjectId}",
            $"status: {WireName(response.Status)}",
            $"conversation-state: {WireName(response.ConversationState)}",
            $"correlation-id: {response.CorrelationId}",
            $"redaction-state: {WireName(response.RedactionState)}",
            $"item-count: {response.Items.Count}",
            $"safe-next-action: {response.SafeNextAction ?? "none"}");
    }

    public static string FormatTaskIntentReview(TaskIntentReview review, bool json)
    {
        ArgumentNullException.ThrowIfNull(review);

        object shape = new
        {
            projectId = review.ProjectId,
            taskIntentId = review.TaskIntentId,
            review.Available,
            review.ReasonCode,
            currentState = review.CurrentState is null ? null : WireName(review.CurrentState.Value),
            review.SourceVersion,
            correlationId = review.CorrelationId,
            redactionState = WireName(review.RedactionState),
            availableTransitions = review.AvailableTransitions.Select(static transition => new
            {
                transition.Transition,
                transition.Enabled,
                transition.DisabledReasonCode,
            }).ToArray(),
        };

        return json ? ToJson(shape) : Lines(
            $"project-id: {review.ProjectId}",
            $"task-intent-id: {review.TaskIntentId}",
            $"available: {review.Available.ToString().ToLowerInvariant()}",
            $"reason-code: {review.ReasonCode}",
            $"current-state: {(review.CurrentState is null ? "none" : WireName(review.CurrentState.Value))}",
            $"correlation-id: {review.CorrelationId}",
            $"redaction-state: {WireName(review.RedactionState)}");
    }

    public static string FormatSafeDenial(Exception exception, bool json)
    {
        ArgumentNullException.ThrowIfNull(exception);

        SafeDenialMetadata raw = SafeDenialMetadata.From(exception);
        bool compatible = raw.Code != ChatBotMessageCodes.CommandAccepted &&
            (raw.Category is null or "request_denied" || Enum.GetValues<ProblemDetailsCategory>()
                .Any(category => WireName(category) == raw.Category));
        ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(compatible ? raw.Code : null);
        bool retryable = raw.Retryable && !entry.IsTerminal &&
            entry.NextAction == ChatBotMessageNextActions.RetryLater && raw.SafeNextAction == entry.NextAction;
        string action = entry.Code == ChatBotMessageCodes.GenericDenial ? ChatBotMessageNextActions.Dismiss
            : raw.SafeNextAction == entry.NextAction &&
                (entry.NextAction != ChatBotMessageNextActions.RetryLater || retryable)
                ? entry.NextAction : ChatBotMessageNextActions.None;
        SafeDenialMetadata denial = raw with
        {
            Category = entry.Code == ChatBotMessageCodes.GenericDenial ? "authorization_denied" : raw.Category,
            Code = entry.Code,
            Retryable = retryable,
            SafeNextAction = action,
            RedactionState = "metadata-only",
        };

        object shape = new
        {
            outcome = "denied",
            reasonCode = denial.Code,
            code = denial.Code,
            headline = entry.Headline,
            message = entry.Reason,
            catalogVersion = entry.Version,
            denial.Category,
            denial.StatusCode,
            denial.CorrelationId,
            denial.TaskId,
            denial.Retryable,
            denial.RedactionState,
            safeNextActions = new[] { denial.SafeNextAction },
        };

        if (json)
        {
            return ToJson(shape);
        }

        List<string> lines =
        [
            "outcome: denied",
            $"category: {denial.Category ?? "request_denied"}",
            $"reason-code: {denial.Code}",
            $"headline: {entry.Headline}",
            $"message: {entry.Reason}",
            $"catalog-version: {entry.Version}",
            $"redaction-state: {denial.RedactionState}",
            $"retryable: {denial.Retryable.ToString().ToLowerInvariant()}",
            $"safe-next-action: {denial.SafeNextAction}",
        ];

        if (!string.IsNullOrWhiteSpace(denial.CorrelationId))
        {
            lines.Add($"correlation-id: {denial.CorrelationId}");
        }

        if (!string.IsNullOrWhiteSpace(denial.TaskId))
        {
            lines.Add($"task-id: {denial.TaskId}");
        }

        return Lines(lines);
    }

    private static string ToJson(object value)
        => JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine;

    private static string Lines(params string[] lines)
        => Lines((IEnumerable<string>)lines);

    private static string Lines(IEnumerable<string> lines)
        => string.Join(Environment.NewLine, lines) + Environment.NewLine;

    private static string SafeValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "none" : value;

    private static string JoinStrings(IEnumerable<string> values)
    {
        string joined = string.Join(",", values.Where(static value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(joined) ? "none" : joined;
    }

    private static string JoinWire<TEnum>(IEnumerable<TEnum> values)
        where TEnum : struct, Enum
    {
        string joined = string.Join(",", values.Select(WireName));
        return string.IsNullOrWhiteSpace(joined) ? "none" : joined;
    }

    private static string WireNameOrNone<TEnum>(TEnum? value)
        where TEnum : struct, Enum
        => value is null ? "none" : WireName(value.Value);

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

    private sealed record SafeDenialMetadata(
        string Code,
        string? Category,
        int? StatusCode,
        string? CorrelationId,
        string? TaskId,
        bool Retryable,
        string RedactionState,
        string SafeNextAction)
    {
        public static SafeDenialMetadata From(Exception exception)
        {
            if (exception is HexalithChatBotApiException<ProblemDetails> { Result: { } problem })
            {
                return new SafeDenialMetadata(
                    SafeValue(problem.Code),
                    WireName(problem.Category),
                    problem.Status,
                    problem.CorrelationId,
                    problem.TaskId,
                    problem.Retryable,
                    Visibility(problem.Details?.Visibility),
                    WireName(problem.ClientAction));
            }

            if (exception is HexalithChatBotApiException api)
            {
                string code = ReasonCodeForStatus(api.StatusCode);
                return new SafeDenialMetadata(
                    code,
                    null,
                    api.StatusCode,
                    null,
                    null,
                    RetryableForStatus(api.StatusCode),
                    "metadata-only",
                    SafeActionForReason(code));
            }

            if (exception is ArgumentException)
            {
                const string code = ChatBotMessageCodes.CommandContractInvalid;
                return new SafeDenialMetadata(
                    code,
                    "validation_error",
                    null,
                    null,
                    null,
                    false,
                    "metadata-only",
                    SafeActionForReason(code));
            }

            return new SafeDenialMetadata(
                ChatBotMessageCodes.GenericDenial,
                "request_denied",
                null,
                null,
                null,
                false,
                "metadata-only",
                ChatBotMessageNextActions.Dismiss);
        }

        private static string Visibility(ProblemDetailsDetailsVisibility? visibility)
            => visibility is null ? "metadata-only" : WireName(visibility.Value).Replace('_', '-');

        private static bool RetryableForStatus(int statusCode)
            => statusCode is 408 or 429 or >= 500;
    }
}
