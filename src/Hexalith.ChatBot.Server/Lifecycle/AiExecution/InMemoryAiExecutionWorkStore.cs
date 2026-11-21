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
            (_, current) => AiExecutionWorkItem.MergeStarted(current, item));
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
        AiExecutionWorkItem placeholder = AiExecutionWorkItem.CancellationPlaceholder(request, key);
        _ = _items.AddOrUpdate(
            key,
            placeholder,
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
        QuarantineInvalidRows();
        IReadOnlyList<AiExecutionWorkItem> result = _items.Values
            .Where(item => item.Status is not (AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined))
            .GroupBy(static item => item.CanonicalKey, StringComparer.Ordinal)
            .Select(static group => group.OrderByDescending(static item => item.Key.StartsWith("ai-execution-v2.", StringComparison.Ordinal)).First())
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
            current.Status is AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined ||
            current.AttemptCount == int.MaxValue ||
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
            if (current.Status is AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined ||
                !string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal) ||
                current.LeaseExpiresAtUtc is null ||
                current.LeaseExpiresAtUtc <= now)
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

    public ValueTask<bool> MarkCompletionPendingAsync(
        string key,
        string owner,
        LowRiskAiAssistanceExecutionRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            now,
            current => current with
            {
                Status = AiExecutionWorkStatus.CompletionPending,
                CompletionRecord = record,
                UpdatedAtUtc = now,
            },
            cancellationToken);

    public ValueTask<bool> MarkTerminalAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            now,
            current => current with
            {
                Status = AiExecutionWorkStatus.Terminal,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
            },
            cancellationToken);

    public ValueTask<bool> ReleaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            now,
            current => current with
            {
                Status = current.CompletionRecord is not null
                    ? AiExecutionWorkStatus.CompletionPending
                    : current.CancellationId is not null
                        ? AiExecutionWorkStatus.CancellationRequested
                        : AiExecutionWorkStatus.Pending,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
            },
            cancellationToken);

    public async ValueTask RecordTerminalRelayFailureAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await UpdateOwnedAsync(
            key,
            owner,
            now,
            current => current with
            {
                TerminalSubmissionAttemptCount = checked(current.TerminalSubmissionAttemptCount + 1),
                UpdatedAtUtc = now,
            },
            cancellationToken).ConfigureAwait(false);

    public ValueTask MarkTerminalObservedAsync(string key, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (_items.TryGetValue(key, out AiExecutionWorkItem? current))
        {
            if (_items.TryUpdate(key, current with
            {
                Status = AiExecutionWorkStatus.Terminal,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
            }, current))
            {
                break;
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask MarkCancellationFailedAsync(string key, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (_items.TryGetValue(key, out AiExecutionWorkItem? current))
        {
            if (current.Execution is null && current.Status is not AiExecutionWorkStatus.Terminal)
            {
                _ = _items.TryRemove(key, out _);
                break;
            }

            AiExecutionWorkItem updated = current.Status is AiExecutionWorkStatus.Terminal
                ? current
                : current with
                {
                    Status = current.CompletionRecord is null ? AiExecutionWorkStatus.Pending : AiExecutionWorkStatus.CompletionPending,
                    CancellationId = null,
                    LeaseOwner = null,
                    LeaseExpiresAtUtc = null,
                    TerminalSubmissionAttemptCount = 0,
                    UpdatedAtUtc = now,
                };
            if (_items.TryUpdate(key, updated, current))
            {
                break;
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MarkExhaustedAsync(
        string key,
        string owner,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
        => UpdateOwnedAsync(
            key,
            owner,
            now,
            current => current with
            {
                Status = AiExecutionWorkStatus.Exhausted,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
                FailureReason = reason,
            },
            cancellationToken);

    public ValueTask<IReadOnlyList<AiExecutionWorkItem>> ListExhaustedAsync(
        string? afterKey,
        int maximumCount,
        CancellationToken cancellationToken,
        string? tenantId = null,
        Func<bool>? authorityIsCurrent = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireCurrentAuthority(authorityIsCurrent);
        IReadOnlyList<AiExecutionWorkItem> items = _items.Values
            .Where(item => item.Status is AiExecutionWorkStatus.Exhausted && (tenantId is null || item.TenantId == tenantId))
            .Where(item => afterKey is null || string.CompareOrdinal(item.Key, afterKey) > 0)
            .OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Take(maximumCount)
            .ToArray();
        return ValueTask.FromResult(items);
    }

    public ValueTask<bool> RecoverExhaustedAsync(string key, DateTimeOffset now, CancellationToken cancellationToken, string? tenantId = null, Func<bool>? authorityIsCurrent = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireCurrentAuthority(authorityIsCurrent);
        while (_items.TryGetValue(key, out AiExecutionWorkItem? current))
        {
            RequireCurrentAuthority(authorityIsCurrent);
            if (current.Status is not AiExecutionWorkStatus.Exhausted || (tenantId is not null && current.TenantId != tenantId))
            {
                return ValueTask.FromResult(false);
            }

            AiExecutionWorkItem recovered = current with
            {
                Status = current.CompletionRecord is null ? AiExecutionWorkStatus.Pending : AiExecutionWorkStatus.CompletionPending,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                AttemptCount = 0,
                TerminalSubmissionAttemptCount = 0,
                UpdatedAtUtc = now,
                FailureReason = null,
            };
            if (_items.TryUpdate(key, recovered, current))
            {
                return ValueTask.FromResult(true);
            }
        }

        return ValueTask.FromResult(false);
    }

    private static void RequireCurrentAuthority(Func<bool>? authorityIsCurrent)
    {
        if (authorityIsCurrent is not null && !authorityIsCurrent())
        {
            throw new Hexalith.ChatBot.Server.Gateway.Stages.ChatBotAuthorityLapsedException();
        }
    }

    private void QuarantineInvalidRows()
    {
        foreach ((string key, AiExecutionWorkItem item) in _items)
        {
            if (item.Status is AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Quarantined ||
                (item.HasValidPersistedIdentity() && item.AttemptCount < int.MaxValue))
            {
                continue;
            }

            _ = _items.TryUpdate(key, item with
            {
                Status = AiExecutionWorkStatus.Quarantined,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                FailureReason = item.AttemptCount == int.MaxValue ? "attempt-count-overflow" : "persisted-identity-corrupt",
            }, item);
        }
    }

    private static bool IsRunnable(AiExecutionWorkItem item, DateTimeOffset now)
        => item.Status is not (AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined) &&
            (item.Execution is not null || item.Status is AiExecutionWorkStatus.CancellationRequested) &&
            (item.LeaseExpiresAtUtc is null || item.LeaseExpiresAtUtc <= now);

    private ValueTask<bool> UpdateOwnedAsync(
        string key,
        string owner,
        DateTimeOffset now,
        Func<AiExecutionWorkItem, AiExecutionWorkItem> update,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (_items.TryGetValue(key, out AiExecutionWorkItem? current))
        {
            if (!string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal) ||
                current.LeaseExpiresAtUtc is null ||
                current.LeaseExpiresAtUtc <= now)
            {
                return ValueTask.FromResult(false);
            }

            if (_items.TryUpdate(key, update(current), current))
            {
                return ValueTask.FromResult(true);
            }
        }

        return ValueTask.FromResult(false);
    }
}
