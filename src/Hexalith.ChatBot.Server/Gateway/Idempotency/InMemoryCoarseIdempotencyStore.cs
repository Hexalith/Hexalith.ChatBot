using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class InMemoryCoarseIdempotencyStore(ISystemClock clock) : IIdempotencyStore
{
    private static readonly TimeSpan DuplicateWaitLimit = TimeSpan.FromSeconds(1);
    private readonly Lock _sync = new();
    private readonly Dictionary<string, PendingRecord> _records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IdentityReservation> _identities = new(StringComparer.Ordinal);

    public int RecordCount
    {
        get { lock (_sync) { return _records.Count; } }
    }

    public IReadOnlyCollection<CoarseIdempotencyRecord> Records
    {
        get { lock (_sync) { return _records.Values.Select(static pending => pending.Record).ToArray(); } }
    }

    public async ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        CoarseIdempotencyRecord proposed = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, clock.UtcNow) with
        {
            ReservationId = Guid.NewGuid().ToString("N"),
            DispatchState = CoarseDispatchState.Reserved,
            ReservationLeaseExpiresAt = clock.UtcNow.Add(DaprCoarseIdempotencyStore.ReservationLease),
        };
        CoarseIdempotencyMetadata metadata = Metadata(proposed);
        context.SetIdempotency(metadata);
        PendingRecord? replay = null;
        lock (_sync)
        {
            // Only a bounded Reserved lease proves abandonment. Dispatching ownership
            // remains fenced even after its lease expires because an external write may exist.
            if (proposed.IdentityKeyHash is { } proposedIdentity &&
                _identities.TryGetValue(proposedIdentity, out IdentityReservation? expiredIdentity))
            {
                ReleaseExpiredReservation(expiredIdentity.Domain);
            }
            if (_records.TryGetValue(proposed.CoarseKeyHash, out PendingRecord? expiredDomain))
            {
                ReleaseExpiredReservation(expiredDomain);
            }
            if (proposed.IdentityKeyHash is { } identityKey && _identities.TryGetValue(identityKey, out IdentityReservation? owner))
            {
                if (!string.Equals(owner.CallerFingerprint, proposed.CallerFingerprint, StringComparison.Ordinal))
                {
                    return CoarseIdempotencyDecision.Conflict(metadata);
                }

                replay = owner.Domain;
            }
            else
            {
                if (_records.TryGetValue(proposed.CoarseKeyHash, out PendingRecord? domain) &&
                    domain.Record.PriorOutcome is not null && domain.Record.ExpiresAt <= clock.UtcNow)
                {
                    _records.Remove(proposed.CoarseKeyHash);
                    domain = null;
                }

                if (domain is not null)
                {
                    if (!string.Equals(domain.Record.CanonicalEquivalenceHash, proposed.CanonicalEquivalenceHash, StringComparison.Ordinal))
                    {
                        return CoarseIdempotencyDecision.Conflict(metadata);
                    }

                    replay = domain;
                }
                else
                {
                    PendingRecord pending = new(proposed);
                    _records.Add(proposed.CoarseKeyHash, pending);
                    if (proposed.IdentityKeyHash is { } key)
                    {
                        _identities.Add(key, new IdentityReservation(proposed.CallerFingerprint!, pending));
                    }

                    return CoarseIdempotencyDecision.Proceed(metadata);
                }
            }
        }

        CommandSubmissionResponse? outcome = await ReadReplayOutcomeAsync(replay, cancellationToken).ConfigureAwait(false);
        if (outcome is null)
        {
            return CoarseIdempotencyDecision.RecoveryPending(metadata);
        }
        if (proposed.IdentityKeyHash is { } replayIdentityKey)
        {
            lock (_sync)
            {
                if (_identities.TryGetValue(replayIdentityKey, out IdentityReservation? owner))
                {
                    if (!string.Equals(owner.CallerFingerprint, proposed.CallerFingerprint, StringComparison.Ordinal))
                    {
                        return CoarseIdempotencyDecision.Conflict(metadata);
                    }

                    // A concurrent claim may bind this caller to a newer domain after expiry.
                    // Its canonical outcome wins even if this request already captured an old one.
                    replay = owner.Domain;
                }
                else
                {
                    _identities.Add(replayIdentityKey, new IdentityReservation(proposed.CallerFingerprint!, replay));
                }
            }
        }

        outcome = await ReadReplayOutcomeAsync(replay, cancellationToken).ConfigureAwait(false);
        return outcome is null
            ? CoarseIdempotencyDecision.RecoveryPending(metadata)
            : CoarseIdempotencyDecision.ReplayPriorOutcome(metadata, Clone(outcome));
    }

    private static async ValueTask<CommandSubmissionResponse?> ReadReplayOutcomeAsync(PendingRecord replay, CancellationToken cancellationToken)
    {
        try
        {
            return replay.Record.PriorOutcome ?? await replay.PriorOutcome.Task
                .WaitAsync(DuplicateWaitLimit, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    public ValueTask<bool> PrepareDispatchAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending) ||
                pending.Record.ReservationId != metadata.ReservationId ||
                pending.Record.DispatchState != CoarseDispatchState.Reserved ||
                pending.Record.ReservationLeaseExpiresAt is not { } lease || lease <= clock.UtcNow)
            {
                return ValueTask.FromResult(false);
            }

            pending.Record = pending.Record with { DispatchState = CoarseDispatchState.Dispatching, PreparedOutcome = Clone(outcome) };
            return ValueTask.FromResult(true);
        }
    }

    /// <summary>Retains the actual planned target only on the matching prepared dispatch owner.</summary>
    public ValueTask<bool> BindDispatchTargetAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse preparedOutcome,
        string aggregateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(aggregateId) ||
                !_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending) ||
                pending.Record.ReservationId != metadata.ReservationId || pending.Record.IdentityKeyHash != metadata.IdentityKeyHash ||
                pending.Record.OperationClass != metadata.OperationClass || pending.Record.CanonicalEquivalenceHash != metadata.CanonicalEquivalenceHash ||
                pending.Record.PriorOutcome is not null || pending.Record.DispatchState != CoarseDispatchState.Dispatching ||
                pending.Record.PreparedOutcome is not { } prepared || !SameOutcome(prepared, preparedOutcome) ||
                pending.Record.PreparedAggregateId is { } target && !string.Equals(target, aggregateId, StringComparison.Ordinal))
            {
                return ValueTask.FromResult(false);
            }

            pending.Record = pending.Record with { PreparedAggregateId = aggregateId };
            return ValueTask.FromResult(true);
        }
    }

    /// <summary>Retains SDK acceptance on the same exact prepared owner and planned aggregate.</summary>
    public ValueTask<bool> ConfirmSdkSubmissionAcceptedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse preparedOutcome,
        string aggregateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(aggregateId) ||
                !_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending) ||
                pending.Record.ReservationId != metadata.ReservationId || pending.Record.IdentityKeyHash != metadata.IdentityKeyHash ||
                pending.Record.OperationClass != metadata.OperationClass || pending.Record.CanonicalEquivalenceHash != metadata.CanonicalEquivalenceHash ||
                pending.Record.CreatedAt != metadata.CreatedAt ||
                pending.Record.PriorOutcome is not null || pending.Record.DispatchState != CoarseDispatchState.Dispatching ||
                pending.Record.PreparedOutcome is not { } prepared || !SameOutcome(prepared, preparedOutcome) ||
                !string.Equals(pending.Record.PreparedAggregateId, aggregateId, StringComparison.Ordinal))
            {
                return ValueTask.FromResult(false);
            }

            pending.Record = pending.Record with { SdkSubmissionAccepted = true };
            return ValueTask.FromResult(true);
        }
    }

    public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(outcome);
        lock (_sync)
        {
            if (_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending) && pending.Record.ReservationId == metadata.ReservationId)
            {
                CommandSubmissionResponse stored = Clone(outcome);
                pending.Record = pending.Record with { PriorOutcome = stored };
                pending.PriorOutcome.TrySetResult(Clone(stored));
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
        => AbortCore(metadata, null);

    /// <summary>
    /// Releases only the exact prepared response after dispatch proves nothing committed (no external write was
    /// attempted, or EventStore definitively refused the only one).
    /// </summary>
    public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse preparedOutcome,
        CancellationToken cancellationToken) => AbortCore(metadata, preparedOutcome);

    private ValueTask AbortCore(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse? undispatchedOutcome)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        lock (_sync)
        {
            if (_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending) && pending.Record.PriorOutcome is null &&
                !pending.Record.SdkSubmissionAccepted &&
                pending.Record.ReservationId == metadata.ReservationId &&
                (pending.Record.DispatchState == CoarseDispatchState.Reserved ||
                 pending.Record.DispatchState == CoarseDispatchState.Dispatching && undispatchedOutcome is not null &&
                 pending.Record.PreparedOutcome is { } prepared && SameOutcome(prepared, undispatchedOutcome)))
            {
                ReleaseReservation(pending);
            }
        }

        return ValueTask.CompletedTask;
    }

    private void ReleaseExpiredReservation(PendingRecord pending)
    {
        if (pending.Record.PriorOutcome is null && pending.Record.DispatchState == CoarseDispatchState.Reserved &&
            pending.Record.ReservationLeaseExpiresAt is { } lease && lease <= clock.UtcNow)
        {
            ReleaseReservation(pending);
        }
    }

    private void ReleaseReservation(PendingRecord pending)
    {
        pending.Record = pending.Record with { DispatchState = CoarseDispatchState.Aborted };
        _records.Remove(pending.Record.CoarseKeyHash);
        foreach (string key in _identities.Where(entry => ReferenceEquals(entry.Value.Domain, pending)).Select(static entry => entry.Key).ToArray())
        {
            _identities.Remove(key);
        }
        pending.PriorOutcome.TrySetResult(null);
    }

    private static bool SameOutcome(CommandSubmissionResponse actual, CommandSubmissionResponse expected)
        => actual.CommandId == expected.CommandId && actual.CorrelationId == expected.CorrelationId &&
           actual.TaskId == expected.TaskId && actual.OperationId == expected.OperationId &&
           actual.LifecycleState == expected.LifecycleState && actual.AcceptedAt == expected.AcceptedAt &&
           actual.ReasonCode == expected.ReasonCode && actual.RetryEligible == expected.RetryEligible;

    private static CoarseIdempotencyMetadata Metadata(CoarseIdempotencyRecord record)
        => new(record.OperationClass, record.CoarseKeyHash, record.CanonicalEquivalenceHash, record.ExpiresAt, record.IdentityKeyHash, record.CreatedAt, record.ReservationId);

    private static CommandSubmissionResponse Clone(CommandSubmissionResponse outcome)
        => new()
        {
            CommandId = outcome.CommandId,
            CorrelationId = outcome.CorrelationId,
            TaskId = outcome.TaskId,
            OperationId = outcome.OperationId,
            LifecycleState = outcome.LifecycleState,
            AcceptedAt = outcome.AcceptedAt,
            ReasonCode = outcome.ReasonCode,
            RetryEligible = outcome.RetryEligible,
        };

    private sealed class PendingRecord(CoarseIdempotencyRecord record)
    {
        public CoarseIdempotencyRecord Record { get; set; } = record;

        public TaskCompletionSource<CommandSubmissionResponse?> PriorOutcome { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record IdentityReservation(string CallerFingerprint, PendingRecord Domain);
}
