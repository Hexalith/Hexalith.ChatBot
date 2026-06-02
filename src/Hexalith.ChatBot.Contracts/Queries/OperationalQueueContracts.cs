using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record SearchOperationalQueueItems(
    OperationalQueueFamily QueueFamily,
    int? PageSize,
    string? PageToken,
    OperationalQueueSortKey SortKey,
    bool SortDescending,
    OperationalQueueFilter Filter);

public sealed record GetOperationalQueueItemDetail(
    OperationalQueueFamily QueueFamily,
    string QueueRef,
    string ItemRef,
    long SourceVersion);

public sealed record OperationalQueueFilter(
    int? MinAgeSeconds = null,
    int? MaxAgeSeconds = null,
    string? Risk = null,
    decimal? MinConfidence = null,
    decimal? MaxConfidence = null,
    string? ProjectRef = null,
    string? MailboxRef = null,
    string? FailureState = null,
    string? AssignedReviewerRef = null,
    string? NextAction = null,
    DateTimeOffset? ChangedAfterUtc = null,
    DateTimeOffset? ChangedBeforeUtc = null);

public sealed record OperationalQueueSearchResult(
    IReadOnlyList<OperationalQueueRow> Rows,
    string? NextPageToken,
    int PageSize,
    int TotalCount,
    string StableFilterFingerprint,
    string SchemaVersion,
    string CorrelationId);

public sealed record OperationalQueueRow(
    OperationalQueueFamily QueueFamily,
    string QueueRef,
    string ItemRef,
    string State,
    int AgeSeconds,
    string Risk,
    decimal Confidence,
    string? AssigneeRef,
    string NextAction,
    int RetryCount,
    bool IsTerminal,
    ChatBotHealthStatus Health,
    DateTimeOffset FreshnessTimestampUtc,
    string OwnerRole,
    IReadOnlyList<string> DisabledActionReasonCodes,
    OperationalQueueDiagnostics Diagnostics,
    string RedactionState,
    long SourceVersion,
    decimal PriorityScore,
    string PriorityExplanation);

public sealed record OperationalQueueDiagnostics(
    string CorrelationId,
    string TenantRef,
    string? MailboxRef,
    string WorkflowItemRef,
    string CurrentState,
    string LastTransition,
    int RetryCount,
    string? FailureReason,
    string NextSafeAction);

public sealed record OperationalQueueItemDetail(
    OperationalQueueRow Summary,
    string DetailAccessState,
    string SafeDetailStatus,
    IReadOnlyList<string> EscalationActions);

public static class OperationalQueueContractValidator
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 100;

    public static IReadOnlyList<string> Validate(SearchOperationalQueueItems query)
    {
        ArgumentNullException.ThrowIfNull(query);

        List<string> errors = [];
        if (!OperationalQueueFamilies.All.Contains(query.QueueFamily))
        {
            errors.Add("queue_family_invalid");
        }

        int pageSize = query.PageSize ?? DefaultPageSize;
        if (pageSize is < 1 or > MaxPageSize)
        {
            errors.Add("page_size_invalid");
        }

        if (!IsSafeToken(query.PageToken))
        {
            errors.Add("page_token_invalid");
        }

        if (!Enum.IsDefined(query.SortKey))
        {
            errors.Add("sort_key_invalid");
        }

        ValidateFilter(query.Filter, errors);
        return errors;
    }

    public static IReadOnlyList<string> Validate(GetOperationalQueueItemDetail query)
    {
        ArgumentNullException.ThrowIfNull(query);

        List<string> errors = [];
        if (!OperationalQueueFamilies.All.Contains(query.QueueFamily))
        {
            errors.Add("queue_family_invalid");
        }

        if (!IsRequiredSafeToken(query.QueueRef))
        {
            errors.Add("queue_ref_invalid");
        }

        if (!IsRequiredSafeToken(query.ItemRef))
        {
            errors.Add("item_ref_invalid");
        }

        if (query.SourceVersion < 0)
        {
            errors.Add("source_version_invalid");
        }

        return errors;
    }

    public static bool IsValid(SearchOperationalQueueItems query)
        => Validate(query).Count == 0;

    public static bool IsValid(GetOperationalQueueItemDetail query)
        => Validate(query).Count == 0;

    public static bool IsFreshnessUtc(DateTimeOffset value)
        => value.Offset == TimeSpan.Zero;

    public static bool IsRequiredSafeToken(string? value)
        => !string.IsNullOrWhiteSpace(value) && IsSafeToken(value);

    public static bool IsSafeToken(string? value)
        => value is null ||
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|');

    private static void ValidateFilter(OperationalQueueFilter? filter, List<string> errors)
    {
        if (filter is null)
        {
            errors.Add("filter_required");
            return;
        }

        if (filter.MinAgeSeconds is < 0 || filter.MaxAgeSeconds is < 0 ||
            (filter.MinAgeSeconds is { } minAge && filter.MaxAgeSeconds is { } maxAge && minAge > maxAge))
        {
            errors.Add("age_filter_invalid");
        }

        if (filter.MinConfidence is < 0 or > 1 || filter.MaxConfidence is < 0 or > 1 ||
            (filter.MinConfidence is { } minConfidence && filter.MaxConfidence is { } maxConfidence && minConfidence > maxConfidence))
        {
            errors.Add("confidence_filter_invalid");
        }

        ValidateSafeOptional(filter.Risk, "risk_filter_invalid", errors);
        ValidateSafeOptional(filter.ProjectRef, "project_filter_invalid", errors);
        ValidateSafeOptional(filter.MailboxRef, "mailbox_filter_invalid", errors);
        ValidateSafeOptional(filter.FailureState, "failure_state_filter_invalid", errors);
        ValidateSafeOptional(filter.AssignedReviewerRef, "assigned_reviewer_filter_invalid", errors);
        ValidateSafeOptional(filter.NextAction, "next_action_filter_invalid", errors);

        if (filter.ChangedAfterUtc is { } after && !IsFreshnessUtc(after))
        {
            errors.Add("changed_after_not_utc");
        }

        if (filter.ChangedBeforeUtc is { } before && !IsFreshnessUtc(before))
        {
            errors.Add("changed_before_not_utc");
        }

        if (filter.ChangedAfterUtc is { } changedAfter &&
            filter.ChangedBeforeUtc is { } changedBefore &&
            changedAfter > changedBefore)
        {
            errors.Add("changed_bounds_invalid");
        }
    }

    private static void ValidateSafeOptional(string? value, string code, List<string> errors)
    {
        if (!IsSafeToken(value))
        {
            errors.Add(code);
        }
    }

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
