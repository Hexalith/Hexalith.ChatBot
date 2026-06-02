using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AdminQueueSummaryProjector
{
    private const int MaxPageSize = 100;

    public static AdminQueueSummary Create(
        string queueRef,
        IEnumerable<AdminQueueSummaryProjectionItem> items,
        AdminOperationReference auditRef,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueRef);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(auditRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        AdminQueueSummaryProjectionItem[] visibleItems = items
            .Where(item => string.Equals(item.QueueRef, queueRef, StringComparison.Ordinal))
            .ToArray();

        string safeQueueRef = SafeSummaryToken(queueRef) ?? "queue:redacted";

        IReadOnlyList<AdminQueueSummaryBucket> buckets = visibleItems
            .GroupBy(static item => new { item.Status, item.OwnerClass })
            .Select(group => new AdminQueueSummaryBucket(
                SafeSummaryToken(group.Key.Status) ?? "redacted",
                SafeSummaryToken(group.Key.OwnerClass) ?? "redacted",
                group.Count(),
                group.Max(static item => Math.Max(0, item.AgeSeconds))))
            .OrderBy(static bucket => bucket.Status, StringComparer.Ordinal)
            .ThenBy(static bucket => bucket.OwnerClass, StringComparer.Ordinal)
            .ToArray();

        IReadOnlyList<AdminQueueSummaryItemRef> itemRefs = visibleItems
            .Select(item => new
            {
                ItemRef = SafeSummaryToken(item.ItemRef),
                Status = SafeSummaryToken(item.Status) ?? "redacted",
                OwnerClass = SafeSummaryToken(item.OwnerClass) ?? "redacted",
            })
            .Where(static item => item.ItemRef is not null)
            .Select(static item => new AdminQueueSummaryItemRef(
                item.ItemRef!,
                item.Status,
                item.OwnerClass,
                [ChatBotDisabledActionReasons.InsufficientAuthority]))
            .ToArray();

        return new AdminQueueSummary(
            safeQueueRef,
            WorstHealth(visibleItems),
            buckets,
            itemRefs,
            auditRef,
            "chatbot.admin-queue-summary.v1",
            correlationId);
    }

    public static OperationalQueueSearchResult Search(
        SearchOperationalQueueItems query,
        IEnumerable<AdminQueueSummaryProjectionItem> items,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (!OperationalQueueContractValidator.IsValid(query))
        {
            return new OperationalQueueSearchResult(
                [],
                NextPageToken: null,
                PageSize: Math.Clamp(query.PageSize ?? OperationalQueueContractValidator.DefaultPageSize, 1, MaxPageSize),
                TotalCount: 0,
                StableFilterFingerprint: "invalid-filter",
                SchemaVersion: "chatbot.operational-queue.v1",
                correlationId);
        }

        int pageSize = Math.Clamp(query.PageSize ?? OperationalQueueContractValidator.DefaultPageSize, 1, MaxPageSize);
        AdminQueueSummaryProjectionItem[] orderedItems = items
            .Where(item => item.QueueFamily == query.QueueFamily)
            .Where(item => MatchesFilter(item, query.Filter))
            .OrderByDescending(item => PriorityValue(item, query.SortKey, query.SortDescending))
            .ThenByDescending(item => query.SortDescending ? item.SourceVersion : -item.SourceVersion)
            .ThenBy(item => SafeSummaryToken(item.ItemRef) ?? "redacted", StringComparer.Ordinal)
            .ToArray();

        AdminQueueSummaryProjectionItem[] pagedItems = PageAfterToken(orderedItems, query.PageToken)
            .Take(pageSize + 1)
            .ToArray();

        OperationalQueueRow[] rows = pagedItems
            .Take(pageSize)
            .Select(ToOperationalRow)
            .ToArray();

        string? nextPageToken = pagedItems.Length > pageSize
            ? SafeSummaryToken(rows.Last().ItemRef) ?? "redacted"
            : null;

        return new OperationalQueueSearchResult(
            rows,
            nextPageToken,
            pageSize,
            orderedItems.Length,
            StableFilterFingerprint: StableFilterFingerprint(query),
            SchemaVersion: "chatbot.operational-queue.v1",
            correlationId);
    }

    public static OperationalQueueItemDetail CreateSafeDetail(
        OperationalQueueRow row,
        bool hasProjectAuthority)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new OperationalQueueItemDetail(
            row,
            hasProjectAuthority ? "authorized" : "request-access",
            hasProjectAuthority ? "metadata-only-detail-available" : "restricted-detail-redacted",
            hasProjectAuthority ? [] : ["request-access", "escalate"]);
    }

    private static ChatBotHealthStatus WorstHealth(IReadOnlyList<AdminQueueSummaryProjectionItem> items)
    {
        if (items.Count == 0)
        {
            return ChatBotHealthStatus.Unknown;
        }

        if (items.Any(static item => item.Health == ChatBotHealthStatus.Failed))
        {
            return ChatBotHealthStatus.Failed;
        }

        if (items.Any(static item => item.Health == ChatBotHealthStatus.Degraded))
        {
            return ChatBotHealthStatus.Degraded;
        }

        return ChatBotHealthStatus.Healthy;
    }

    private static string? SafeSummaryToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|')
                ? value
                : null;

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static OperationalQueueRow ToOperationalRow(AdminQueueSummaryProjectionItem item)
    {
        string itemRef = SafeSummaryToken(item.ItemRef) ?? "item:redacted";
        string queueRef = SafeSummaryToken(item.QueueRef) ?? "queue:redacted";
        string state = SafeSummaryToken(item.Status) ?? "redacted";
        string ownerRole = SafeSummaryToken(item.OwnerRole) ?? "operations-admin";
        int retryCount = Math.Max(0, item.RetryCount);
        DateTimeOffset freshness = item.FreshnessTimestampUtc?.ToUniversalTime() ??
            DateTimeOffset.FromUnixTimeSeconds(Math.Max(0, item.AgeSeconds));
        var diagnostics = new OperationalQueueDiagnostics(
            CorrelationId: "correlation:" + itemRef,
            TenantRef: "tenant:current",
            MailboxRef: SafeSummaryToken(item.MailboxRef),
            WorkflowItemRef: itemRef,
            CurrentState: state,
            LastTransition: "last-transition:" + state,
            RetryCount: retryCount,
            FailureReason: SafeSummaryToken(item.FailureState),
            NextSafeAction: SafeSummaryToken(item.NextAction) ?? "review");

        return new OperationalQueueRow(
            item.QueueFamily,
            queueRef,
            itemRef,
            state,
            Math.Max(0, item.AgeSeconds),
            SafeSummaryToken(item.Risk) ?? "medium",
            Math.Clamp(item.Confidence, 0, 1),
            SafeSummaryToken(item.AssigneeRef),
            SafeSummaryToken(item.NextAction) ?? "review",
            retryCount,
            item.IsTerminal,
            item.Health,
            freshness,
            ownerRole,
            [ChatBotDisabledActionReasons.InsufficientAuthority],
            diagnostics,
            "metadata_only",
            Math.Max(0, item.SourceVersion),
            item.PriorityScore,
            SafeSummaryToken(item.PriorityExplanation) ?? "stable-order");
    }

    private static bool MatchesFilter(AdminQueueSummaryProjectionItem item, OperationalQueueFilter filter)
        => (filter.MinAgeSeconds is null || item.AgeSeconds >= filter.MinAgeSeconds) &&
            (filter.MaxAgeSeconds is null || item.AgeSeconds <= filter.MaxAgeSeconds) &&
            MatchesSafe(filter.Risk, item.Risk) &&
            (filter.MinConfidence is null || item.Confidence >= filter.MinConfidence) &&
            (filter.MaxConfidence is null || item.Confidence <= filter.MaxConfidence) &&
            MatchesSafe(filter.MailboxRef, item.MailboxRef) &&
            MatchesSafe(filter.FailureState, item.FailureState) &&
            MatchesSafe(filter.AssignedReviewerRef, item.AssigneeRef) &&
            MatchesSafe(filter.NextAction, item.NextAction) &&
            MatchesSafe(filter.ProjectRef, SafeSummaryToken(item.ProjectName));

    private static bool MatchesSafe(string? filter, string? candidate)
        => string.IsNullOrWhiteSpace(filter) ||
            string.Equals(SafeSummaryToken(filter), SafeSummaryToken(candidate), StringComparison.Ordinal);

    private static IEnumerable<AdminQueueSummaryProjectionItem> PageAfterToken(
        IReadOnlyList<AdminQueueSummaryProjectionItem> orderedItems,
        string? pageToken)
    {
        if (string.IsNullOrWhiteSpace(pageToken))
        {
            return orderedItems;
        }

        string? safePageToken = SafeSummaryToken(pageToken);
        if (safePageToken is null)
        {
            return [];
        }

        int tokenIndex = -1;
        for (int index = 0; index < orderedItems.Count; index++)
        {
            if (string.Equals(SafeSummaryToken(orderedItems[index].ItemRef), safePageToken, StringComparison.Ordinal))
            {
                tokenIndex = index;
                break;
            }
        }

        return tokenIndex < 0
            ? []
            : orderedItems.Skip(tokenIndex + 1);
    }

    private static decimal PriorityValue(AdminQueueSummaryProjectionItem item, OperationalQueueSortKey sortKey, bool descending)
    {
        decimal value = sortKey switch
        {
            OperationalQueueSortKey.Priority => item.PriorityScore,
            OperationalQueueSortKey.Age => item.AgeSeconds,
            OperationalQueueSortKey.Risk => RiskWeight(item.Risk),
            OperationalQueueSortKey.Confidence => item.Confidence,
            OperationalQueueSortKey.Freshness => item.FreshnessTimestampUtc?.ToUnixTimeSeconds() ?? 0,
            OperationalQueueSortKey.RetryCount => item.RetryCount,
            _ => item.PriorityScore,
        };

        return descending ? value : -value;
    }

    private static decimal RiskWeight(string? risk)
        => risk switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0,
        };

    private static string StableFilterFingerprint(SearchOperationalQueueItems query)
    {
        string canonical = string.Join(
            "|",
            OperationalQueueFamilies.ToWireValue(query.QueueFamily),
            query.SortKey.ToString(),
            query.SortDescending ? "true" : "false",
            (query.PageSize ?? OperationalQueueContractValidator.DefaultPageSize).ToString(System.Globalization.CultureInfo.InvariantCulture),
            SafeSummaryToken(query.Filter.Risk) ?? string.Empty,
            query.Filter.MinAgeSeconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            query.Filter.MaxAgeSeconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            query.Filter.MinConfidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            query.Filter.MaxConfidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            SafeSummaryToken(query.Filter.ProjectRef) ?? string.Empty,
            SafeSummaryToken(query.Filter.MailboxRef) ?? string.Empty,
            SafeSummaryToken(query.Filter.FailureState) ?? string.Empty,
            SafeSummaryToken(query.Filter.AssignedReviewerRef) ?? string.Empty,
            SafeSummaryToken(query.Filter.NextAction) ?? string.Empty,
            query.Filter.ChangedAfterUtc?.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            query.Filter.ChangedBeforeUtc?.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);

        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
