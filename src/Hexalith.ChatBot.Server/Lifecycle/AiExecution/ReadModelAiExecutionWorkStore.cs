using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed class ReadModelAiExecutionWorkStore : IAiExecutionWorkStore
{
    private const string IndexKey = "chatbot:ai-execution:index:v1";
    private const string IndexPagePrefix = "chatbot:ai-execution:index-page:v2:";
    private const int DefaultMaximumIndexPageSize = 64;
    private const int DefaultMaximumIndexPages = 4096;
    private readonly IReadModelStore _store;
    private readonly int _maximumIndexPageSize;
    private readonly int _maximumIndexPages;

    public ReadModelAiExecutionWorkStore(
        IReadModelStore store,
        int maximumIndexPageSize = DefaultMaximumIndexPageSize,
        int maximumIndexPages = DefaultMaximumIndexPages)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _maximumIndexPageSize = maximumIndexPageSize > 0
            ? maximumIndexPageSize
            : throw new ArgumentOutOfRangeException(nameof(maximumIndexPageSize));
        _maximumIndexPages = maximumIndexPages > 0 && maximumIndexPages <= DefaultMaximumIndexPages
            ? maximumIndexPages
            : throw new ArgumentOutOfRangeException(nameof(maximumIndexPages));
    }

    public async ValueTask UpsertStartedAsync(AiExecutionWorkItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        _ = await UpdateAsync(
            item.Key,
            current => current is null ? item : current,
            item.CorrelationId,
            cancellationToken).ConfigureAwait(false);
        bool indexed = await TryAddToSpillPageAsync(item, cancellationToken).ConfigureAwait(false);
        if (!indexed)
        {
            await QuarantineAsync(item.Key, item.CorrelationId, "recovery-index-capacity-exhausted", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async ValueTask MarkCancellationRequestedAsync(
        AiResponseGenerationCancellationRequested request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string key = AiExecutionWorkItem.KeyFor(
            request.TenantId,
            request.ProjectId,
            request.ConversationId,
            request.ResponseId,
            request.GenerationId);
        if (await GetAsync(key, cancellationToken).ConfigureAwait(false) is null)
        {
            // Legacy Started rows do not contain enough context to reconstruct safe provider work. Their governed
            // cancellation still projects, but the coordinator must not fabricate a durable work item.
            return;
        }

        _ = await UpdateAsync(
            key,
            current => current is null
                ? throw new InvalidOperationException($"AI execution work '{key}' disappeared while cancellation was recorded.")
                : current.Status is AiExecutionWorkStatus.Terminal
                    ? current
                    : current with
                    {
                        Status = AiExecutionWorkStatus.CancellationRequested,
                        CancellationId = request.CancellationId,
                        CorrelationId = request.CorrelationId,
                        UpdatedAtUtc = request.RequestedAtUtc,
                    },
            request.CorrelationId,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<AiExecutionWorkItem>> ListRunnableAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        AiExecutionWorkIndex index = (await _store
            .GetAsync<AiExecutionWorkIndex>(ChatBotReadModelStoreNames.StateStoreName, IndexKey, cancellationToken)
            .ConfigureAwait(false)).Value ?? new AiExecutionWorkIndex([]);
        List<AiExecutionWorkItem> items = [];
        foreach (string indexEntry in index.Keys)
        {
            IReadOnlyList<string> workKeys;
            if (indexEntry.StartsWith(IndexPagePrefix, StringComparison.Ordinal))
            {
                try
                {
                    workKeys = (await _store
                        .GetAsync<AiExecutionWorkIndex>(ChatBotReadModelStoreNames.StateStoreName, indexEntry, cancellationToken)
                        .ConfigureAwait(false)).Value?.Keys ?? [];
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // A poisoned page is isolated. Other pages remain recoverable; a replay/repair can compact this
                    // deterministic page without preventing unrelated generations from running.
                    continue;
                }
            }
            else
            {
                // v1 migration: the old root stored work keys directly. Read them in place, while every new write is
                // placed in a bounded v2 page. Equivalent v1/v2 Started rows still deduplicate at the work identity.
                workKeys = [indexEntry];
            }

            foreach (string key in workKeys)
            {
                AiExecutionWorkItem? item;
                try
                {
                    item = await GetAsync(key, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                if (item is not null && !item.HasValidPersistedIdentity())
                {
                    await QuarantineAsync(key, item.CorrelationId, "persisted-identity-corrupt", cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                if (item is not null && item.AttemptCount == int.MaxValue)
                {
                    await QuarantineAsync(key, item.CorrelationId, "attempt-count-overflow", cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                if (item is not null &&
                    item.Status is not (AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined) &&
                    (item.LeaseExpiresAtUtc is null || item.LeaseExpiresAtUtc <= now))
                {
                    items.Add(item);
                }
            }
        }

        return items
            .GroupBy(static item => item.CanonicalKey, StringComparer.Ordinal)
            .Select(static group => group
                .OrderByDescending(static item => item.Key.StartsWith("ai-execution-v2.", StringComparison.Ordinal))
                .First())
            .OrderBy(static item => item.UpdatedAtUtc)
            .ThenBy(static item => item.Key, StringComparer.Ordinal)
            .Take(maximumCount)
            .ToArray();
    }

    public async ValueTask<AiExecutionWorkItem?> TryClaimAsync(
        string key,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        DateTimeOffset expires = now.Add(leaseDuration);
        AiExecutionWorkItem claimed = await UpdateAsync(
            key,
            current => current is null ||
                current.Status is AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined ||
                current.AttemptCount == int.MaxValue ||
                (current.LeaseExpiresAtUtc > now && !string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal))
                    ? current ?? throw new InvalidOperationException($"AI execution work '{key}' was not found.")
                    : current with
                    {
                        Status = current.Status is AiExecutionWorkStatus.CancellationRequested or AiExecutionWorkStatus.CompletionPending
                            ? current.Status
                            : AiExecutionWorkStatus.Executing,
                        LeaseOwner = owner,
                        LeaseExpiresAtUtc = expires,
                        AttemptCount = checked(current.AttemptCount + 1),
                        UpdatedAtUtc = now,
                    },
            owner,
            cancellationToken).ConfigureAwait(false);
        return string.Equals(claimed.LeaseOwner, owner, StringComparison.Ordinal) && claimed.LeaseExpiresAtUtc == expires
            ? claimed
            : null;
    }

    public async ValueTask<AiExecutionWorkItem?> TryRenewLeaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        DateTimeOffset expires = now.Add(leaseDuration);
        AiExecutionWorkItem renewed = await UpdateAsync(
            key,
            current => current is not null &&
                current.Status is not (AiExecutionWorkStatus.Terminal or AiExecutionWorkStatus.Exhausted or AiExecutionWorkStatus.Quarantined) &&
                string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal) &&
                current.LeaseExpiresAtUtc is not null &&
                current.LeaseExpiresAtUtc > now
                    ? current with { LeaseExpiresAtUtc = expires, UpdatedAtUtc = now }
                    : current ?? throw new InvalidOperationException($"AI execution work '{key}' was not found."),
            owner,
            cancellationToken).ConfigureAwait(false);
        return string.Equals(renewed.LeaseOwner, owner, StringComparison.Ordinal) && renewed.LeaseExpiresAtUtc == expires
            ? renewed
            : null;
    }

    public async ValueTask<bool> MarkCompletionPendingAsync(
        string key,
        string owner,
        LowRiskAiAssistanceExecutionRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await UpdateOwnedAsync(
            key,
            owner,
            now,
            current => current with
            {
                Status = AiExecutionWorkStatus.CompletionPending,
                CompletionRecord = record,
                UpdatedAtUtc = now,
            },
            cancellationToken).ConfigureAwait(false);

    public async ValueTask<bool> MarkTerminalAsync(
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
                Status = AiExecutionWorkStatus.Terminal,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
            },
            cancellationToken).ConfigureAwait(false);

    public async ValueTask<bool> ReleaseAsync(
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
            cancellationToken).ConfigureAwait(false);

    public async ValueTask MarkTerminalObservedAsync(string key, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await GetAsync(key, cancellationToken).ConfigureAwait(false) is null)
        {
            return;
        }

        _ = await UpdateAsync(
            key,
            current => current is null
                ? throw new InvalidOperationException($"AI execution work '{key}' was not found.")
                : current with
                {
                    Status = AiExecutionWorkStatus.Terminal,
                    LeaseOwner = null,
                    LeaseExpiresAtUtc = null,
                    UpdatedAtUtc = now,
                },
            "terminal-observed",
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask MarkCancellationFailedAsync(string key, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await GetAsync(key, cancellationToken).ConfigureAwait(false) is null)
        {
            return;
        }

        _ = await UpdateAsync(
            key,
            current => current is null
                ? throw new InvalidOperationException($"AI execution work '{key}' was not found.")
                : current.Status is AiExecutionWorkStatus.Terminal
                    ? current
                    : current with
                    {
                        Status = current.CompletionRecord is null ? AiExecutionWorkStatus.Pending : AiExecutionWorkStatus.CompletionPending,
                        CancellationId = null,
                        LeaseOwner = null,
                        LeaseExpiresAtUtc = null,
                        TerminalSubmissionAttemptCount = 0,
                        UpdatedAtUtc = now,
                    },
            "cancellation-failed",
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<bool> MarkExhaustedAsync(
        string key,
        string owner,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
        => await UpdateOwnedAsync(
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
            cancellationToken).ConfigureAwait(false);

    public async ValueTask<IReadOnlyList<AiExecutionWorkItem>> ListExhaustedAsync(
        string? afterKey,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);
        IReadOnlyList<AiExecutionWorkItem> indexed = await ListIndexedAsync(cancellationToken).ConfigureAwait(false);
        return indexed
            .Where(static item => item.Status is AiExecutionWorkStatus.Exhausted)
            .Where(item => afterKey is null || string.CompareOrdinal(item.Key, afterKey) > 0)
            .OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Take(maximumCount)
            .ToArray();
    }

    public async ValueTask<bool> RecoverExhaustedAsync(
        string key,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        bool recovered = false;
        _ = await UpdateAsync(
            key,
            current =>
            {
                recovered = current is not null && current.Status is AiExecutionWorkStatus.Exhausted;
                return recovered
                    ? current! with
                    {
                        Status = current.CompletionRecord is null ? AiExecutionWorkStatus.Pending : AiExecutionWorkStatus.CompletionPending,
                        LeaseOwner = null,
                        LeaseExpiresAtUtc = null,
                        AttemptCount = 0,
                        TerminalSubmissionAttemptCount = 0,
                        UpdatedAtUtc = now,
                        FailureReason = null,
                    }
                    : current ?? throw new InvalidOperationException($"AI execution work '{key}' was not found.");
            },
            "operator-recovery",
            cancellationToken).ConfigureAwait(false);
        return recovered;
    }

    private async Task<AiExecutionWorkItem?> GetAsync(string key, CancellationToken cancellationToken)
        => (await _store
            .GetAsync<AiExecutionWorkItem>(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
            .ConfigureAwait(false)).Value;

    private static AiExecutionWorkIndex AddIndexEntry(AiExecutionWorkIndex? current, string entry, int maximumCount)
    {
        string[] keys = (current?.Keys ?? [])
            .Append(entry)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (keys.Length > maximumCount)
        {
            throw new InvalidOperationException("The bounded AI execution recovery index exhausted its spill capacity.");
        }

        return new AiExecutionWorkIndex(keys);
    }

    private string IndexPageKey(string workKey, int probe)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(workKey));
        int initialBucket = ((digest[0] << 8) | digest[1]) % _maximumIndexPages;
        int bucket = (initialBucket + probe) % _maximumIndexPages;
        return $"{IndexPagePrefix}{bucket:X3}";
    }

    private async Task<bool> TryAddToSpillPageAsync(AiExecutionWorkItem item, CancellationToken cancellationToken)
    {
        for (int probe = 0; probe < _maximumIndexPages; probe++)
        {
            string pageKey = IndexPageKey(item.Key, probe);
            try
            {
                _ = await ReadModelWritePolicy.UpdateAsync<AiExecutionWorkIndex>(
                    _store,
                    ChatBotReadModelStoreNames.StateStoreName,
                    pageKey,
                    current => AddIndexEntry(current, item.Key, _maximumIndexPageSize),
                    new ReadModelWriteContext(nameof(AiExecutionWorkIndex), nameof(ReadModelAiExecutionWorkStore), item.CorrelationId),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                _ = await ReadModelWritePolicy.UpdateAsync<AiExecutionWorkIndex>(
                    _store,
                    ChatBotReadModelStoreNames.StateStoreName,
                    IndexKey,
                    current => AddIndexEntry(current, pageKey, _maximumIndexPages),
                    new ReadModelWriteContext(nameof(AiExecutionWorkIndex), nameof(ReadModelAiExecutionWorkStore), item.CorrelationId),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (InvalidOperationException exception) when (
                string.Equals(exception.Message, "The bounded AI execution recovery index exhausted its spill capacity.", StringComparison.Ordinal))
            {
                // This deterministic page filled between the read and CAS. Probe the next bounded page.
            }
        }

        return false;
    }

    private async Task<IReadOnlyList<AiExecutionWorkItem>> ListIndexedAsync(CancellationToken cancellationToken)
    {
        AiExecutionWorkIndex index = (await _store
            .GetAsync<AiExecutionWorkIndex>(ChatBotReadModelStoreNames.StateStoreName, IndexKey, cancellationToken)
            .ConfigureAwait(false)).Value ?? new AiExecutionWorkIndex([]);
        List<AiExecutionWorkItem> items = [];
        foreach (string indexEntry in index.Keys)
        {
            IReadOnlyList<string> workKeys = indexEntry.StartsWith(IndexPagePrefix, StringComparison.Ordinal)
                ? (await _store.GetAsync<AiExecutionWorkIndex>(ChatBotReadModelStoreNames.StateStoreName, indexEntry, cancellationToken)
                    .ConfigureAwait(false)).Value?.Keys ?? []
                : [indexEntry];
            foreach (string workKey in workKeys)
            {
                AiExecutionWorkItem? item = await GetAsync(workKey, cancellationToken).ConfigureAwait(false);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
        }

        return items;
    }

    private async Task QuarantineAsync(
        string key,
        string correlationId,
        string reason,
        CancellationToken cancellationToken)
    {
        _ = await UpdateAsync(
            key,
            current => current is null
                ? throw new InvalidOperationException($"AI execution work '{key}' was not found.")
                : current with
                {
                    Status = AiExecutionWorkStatus.Quarantined,
                    LeaseOwner = null,
                    LeaseExpiresAtUtc = null,
                    FailureReason = reason,
                },
            correlationId,
            cancellationToken).ConfigureAwait(false);
    }

    private Task<AiExecutionWorkItem> UpdateAsync(
        string key,
        Func<AiExecutionWorkItem?, AiExecutionWorkItem> update,
        string correlationId,
        CancellationToken cancellationToken)
        => ReadModelWritePolicy.UpdateAsync(
            _store,
            ChatBotReadModelStoreNames.StateStoreName,
            key,
            update,
            new ReadModelWriteContext(nameof(AiExecutionWorkItem), nameof(ReadModelAiExecutionWorkStore), correlationId),
            cancellationToken: cancellationToken);

    private async Task<bool> UpdateOwnedAsync(
        string key,
        string owner,
        DateTimeOffset now,
        Func<AiExecutionWorkItem, AiExecutionWorkItem> update,
        CancellationToken cancellationToken)
    {
        bool updated = false;
        _ = await UpdateAsync(
            key,
            current =>
            {
                updated = current is not null &&
                    string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal) &&
                    current.LeaseExpiresAtUtc is not null &&
                    current.LeaseExpiresAtUtc > now;
                return updated
                    ? update(current!)
                    : current ?? throw new InvalidOperationException($"AI execution work '{key}' was not found.");
            },
            owner,
            cancellationToken).ConfigureAwait(false);
        return updated;
    }
}
