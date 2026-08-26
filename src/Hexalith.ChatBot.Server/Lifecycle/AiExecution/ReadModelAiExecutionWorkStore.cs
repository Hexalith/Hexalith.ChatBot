using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed class ReadModelAiExecutionWorkStore(IReadModelStore store) : IAiExecutionWorkStore
{
    private const string IndexKey = "chatbot:ai-execution:index:v1";
    private readonly IReadModelStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public async ValueTask UpsertStartedAsync(AiExecutionWorkItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        _ = await UpdateAsync(
            item.Key,
            current => current is null ? item : current,
            item.CorrelationId,
            cancellationToken).ConfigureAwait(false);
        _ = await ReadModelWritePolicy.UpdateAsync<AiExecutionWorkIndex>(
            _store,
            ChatBotReadModelStoreNames.StateStoreName,
            IndexKey,
            current => new AiExecutionWorkIndex((current?.Keys ?? [])
                .Concat([item.Key])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray()),
            new ReadModelWriteContext(nameof(AiExecutionWorkIndex), nameof(ReadModelAiExecutionWorkStore), item.CorrelationId),
            cancellationToken: cancellationToken).ConfigureAwait(false);
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
        foreach (string key in index.Keys)
        {
            AiExecutionWorkItem? item = await GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (item is not null &&
                item.Status is not AiExecutionWorkStatus.Terminal &&
                (item.LeaseExpiresAtUtc is null || item.LeaseExpiresAtUtc <= now))
            {
                items.Add(item);
            }
        }

        return items
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
                current.Status is AiExecutionWorkStatus.Terminal ||
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
                current.Status is not AiExecutionWorkStatus.Terminal &&
                string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal)
                    ? current with { LeaseExpiresAtUtc = expires, UpdatedAtUtc = now }
                    : current ?? throw new InvalidOperationException($"AI execution work '{key}' was not found."),
            owner,
            cancellationToken).ConfigureAwait(false);
        return string.Equals(renewed.LeaseOwner, owner, StringComparison.Ordinal) && renewed.LeaseExpiresAtUtc == expires
            ? renewed
            : null;
    }

    public async ValueTask MarkCompletionPendingAsync(
        string key,
        string owner,
        LowRiskAiAssistanceExecutionRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => _ = await UpdateOwnedAsync(
            key,
            owner,
            current => current with
            {
                Status = AiExecutionWorkStatus.CompletionPending,
                CompletionRecord = record,
                UpdatedAtUtc = now,
            },
            cancellationToken).ConfigureAwait(false);

    public async ValueTask MarkTerminalAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => _ = await UpdateOwnedAsync(
            key,
            owner,
            current => current with
            {
                Status = AiExecutionWorkStatus.Terminal,
                LeaseOwner = null,
                LeaseExpiresAtUtc = null,
                UpdatedAtUtc = now,
            },
            cancellationToken).ConfigureAwait(false);

    public async ValueTask ReleaseAsync(
        string key,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => _ = await UpdateOwnedAsync(
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
            cancellationToken).ConfigureAwait(false);

    private async Task<AiExecutionWorkItem?> GetAsync(string key, CancellationToken cancellationToken)
        => (await _store
            .GetAsync<AiExecutionWorkItem>(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
            .ConfigureAwait(false)).Value;

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

    private Task<AiExecutionWorkItem> UpdateOwnedAsync(
        string key,
        string owner,
        Func<AiExecutionWorkItem, AiExecutionWorkItem> update,
        CancellationToken cancellationToken)
        => UpdateAsync(
            key,
            current => current is not null && string.Equals(current.LeaseOwner, owner, StringComparison.Ordinal)
                ? update(current)
                : current ?? throw new InvalidOperationException($"AI execution work '{key}' was not found."),
            owner,
            cancellationToken);
}
