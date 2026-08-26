using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed class InMemoryAiExecutionWorkStore : IAiExecutionWorkStore
{
    private readonly ConcurrentDictionary<string, AiExecutionWorkItem> _items = new(StringComparer.Ordinal);

    public ValueTask UpsertStartedAsync(AiExecutionWorkItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();
        _ = _items.AddOrUpdate(
            item.Key,
            item,
            (_, current) => current.Status is AiExecutionWorkStatus.Terminal ? current : current);
        return ValueTask.CompletedTask;
    }

    public ValueTask MarkCancellationRequestedAsync(
        AiResponseGenerationCancellationRequested request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        string key = AiExecutionWorkItem.KeyFor(
            request.TenantId,
            request.ProjectId,
            request.ConversationId,
            request.ResponseId,
            request.GenerationId);
        if (!_items.ContainsKey(key))
        {
            // A replay can contain a cancellation for a legacy Started row that predates the reconstructable
            // durable-work payload. Keep projecting the governed request, but never invent provider work.
            return ValueTask.CompletedTask;
        }

        _ = _items.AddOrUpdate(
            key,
            _ => throw new InvalidOperationException($"AI execution work '{key}' disappeared while cancellation was recorded."),
            (_, current) => current.Status is AiExecutionWorkStatus.Terminal
                ? current
                : current with
                {
                    Status = AiExecutionWorkStatus.CancellationRequested,
                    CancellationId = request.CancellationId,
                    CorrelationId = request.CorrelationId,
                    UpdatedAtUtc = request.RequestedAtUtc,
                });
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<AiExecutionWorkItem>> ListRunnableAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<AiExecutionWorkItem> result = _items.Values
            .Where(item => IsRunnable(item, now))
            .OrderBy(static item => item.UpdatedAtUtc)
            .ThenBy(static item => item.Key, StringComparer.Ordinal)
            .Take(maximumCount)
            .ToArray();
        return ValueTask.FromResult(result);
    }

    public ValueTask<AiExecutionWorkItem?> TryClaimAsync(
        string key,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryGetValue(key, out AiExecutionWorkItem? current) ||
            current.Status is AiExecutionWorkStatus.Terminal ||
            (current.LeaseExpiresAtUtc > now && !string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal)))
        {
            return ValueTask.FromResult<AiExecutionWorkItem?>(null);
        }

        AiExecutionWorkItem claimed = current with
        {
            Status = current.Status is AiExecutionWorkStatus.CancellationRequested or AiExecutionWorkStatus.CompletionPending
                ? current.Status
                : AiExecutionWorkStatus.Executing,
            LeaseOwner = owner,
            LeaseExpiresAtUtc = now.Add(leaseDuration),
            AttemptCount = checked(current.AttemptCount + 1),
            UpdatedAtUtc = now,
        };
        return _items.TryUpdate(key, claimed, current)
            ? ValueTask.FromResult<AiExecutionWorkItem?>(claimed)
            : ValueTask.FromResult<AiExecutionWorkItem?>(null);
    }

    public ValueTask<AiExecutionWorkItem?> TryRenewLeaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (_items.TryGetValue(key, out AiExecutionWorkItem? current))
        {
            if (current.Status is AiExecutionWorkStatus.Terminal ||
                !string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal))
            {
                return ValueTask.FromResult<AiExecutionWorkItem?>(null);
            }

            AiExecutionWorkItem renewed = current with
            {
                LeaseExpiresAtUtc = now.Add(leaseDuration),
                UpdatedAtUtc = now,
            };
            if (_items.TryUpdate(key, renewed, current))
            {
                return ValueTask.FromResult<AiExecutionWorkItem?>(renewed);
            }
        }

        return ValueTask.FromResult<AiExecutionWorkItem?>(null);
    }

    public ValueTask MarkCompletionPendingAsync(
        string key,
        string owner,
        LowRiskAiAssistanceExecutionRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            current => current with
            {
                Status = AiExecutionWorkStatus.CompletionPending,
                CompletionRecord = record,
                UpdatedAtUtc = now,
            },
            cancellationToken);

    public ValueTask MarkTerminalAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            current => current with
            {
                Status = AiExecutionWorkStatus.Terminal,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
            },
            cancellationToken);

    public ValueTask ReleaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            current => current with
            {
                Status = current.CompletionRecord is not null
                    ? AiExecutionWorkStatus.CompletionPending
                    : current.CancellationId is not null
                        ? AiExecutionWorkStatus.CancellationRequested
                        : AiExecutionWorkStatus.Pending,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                TerminalSubmissionAttemptCount = checked(current.TerminalSubmissionAttemptCount + 1),
                UpdatedAtUtc = now,
            },
            cancellationToken);

    private static bool IsRunnable(AiExecutionWorkItem item, DateTimeOffset now)
        => item.Status is not AiExecutionWorkStatus.Terminal &&
            (item.LeaseExpiresAtUtc is null || item.LeaseExpiresAtUtc <= now);

    private ValueTask UpdateOwnedAsync(
        string key,
        string owner,
        Func<AiExecutionWorkItem, AiExecutionWorkItem> update,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (_items.TryGetValue(key, out AiExecutionWorkItem? current))
        {
            if (!string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal))
            {
                return ValueTask.CompletedTask;
            }

            if (_items.TryUpdate(key, update(current), current))
            {
                return ValueTask.CompletedTask;
            }
        }

        return ValueTask.CompletedTask;
    }
}
