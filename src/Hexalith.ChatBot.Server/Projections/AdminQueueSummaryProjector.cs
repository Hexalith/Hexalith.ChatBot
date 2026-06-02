using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AdminQueueSummaryProjector
{
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
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
