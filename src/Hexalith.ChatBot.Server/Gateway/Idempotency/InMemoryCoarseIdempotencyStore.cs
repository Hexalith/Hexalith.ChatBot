using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class InMemoryCoarseIdempotencyStore(ISystemClock clock) : IIdempotencyStore
{
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
        CoarseIdempotencyRecord proposed = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, clock.UtcNow);
        CoarseIdempotencyMetadata metadata = Metadata(proposed);
        context.SetIdempotency(metadata);
        PendingRecord? replay = null;
        lock (_sync)
        {
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

        CommandSubmissionResponse outcome = replay.Record.PriorOutcome ?? await replay.PriorOutcome.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                }
                else
                {
                    _identities.Add(replayIdentityKey, new IdentityReservation(proposed.CallerFingerprint!, replay));
                }
            }
        }

        return CoarseIdempotencyDecision.ReplayPriorOutcome(metadata, Clone(outcome));
    }

    public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(outcome);
        lock (_sync)
        {
            if (_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending))
            {
                CommandSubmissionResponse stored = Clone(outcome);
                pending.Record = pending.Record with { PriorOutcome = stored };
                pending.PriorOutcome.TrySetResult(Clone(stored));
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        lock (_sync)
        {
            if (_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending) && pending.Record.PriorOutcome is null)
            {
                _records.Remove(metadata.CoarseKeyHash);
                if (pending.Record.IdentityKeyHash is { } key &&
                    _identities.TryGetValue(key, out IdentityReservation? identity) && ReferenceEquals(identity.Domain, pending))
                {
                    _identities.Remove(key);
                }

                pending.PriorOutcome.TrySetCanceled(cancellationToken);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static CoarseIdempotencyMetadata Metadata(CoarseIdempotencyRecord record)
        => new(record.OperationClass, record.CoarseKeyHash, record.CanonicalEquivalenceHash, record.ExpiresAt, record.IdentityKeyHash, record.CreatedAt);

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

        public TaskCompletionSource<CommandSubmissionResponse> PriorOutcome { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record IdentityReservation(string CallerFingerprint, PendingRecord Domain);
}
