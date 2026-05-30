using System.Collections.Concurrent;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class InMemoryCoarseIdempotencyStore(ISystemClock clock) : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, PendingRecord> _records = new(StringComparer.Ordinal);

    public int RecordCount => _records.Count;

    public IReadOnlyCollection<CoarseIdempotencyRecord> Records
        => _records.Values.Select(static record => record.Record).ToArray();

    public async ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        while (true)
        {
            DateTimeOffset now = clock.UtcNow;
            CoarseIdempotencyRecord proposed = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, now);
            CoarseIdempotencyMetadata metadata = Metadata(proposed);
            PendingRecord pending = new(proposed);
            PendingRecord existing = _records.GetOrAdd(proposed.CoarseKeyHash, pending);
            context.SetIdempotency(metadata);

            if (ReferenceEquals(existing, pending))
            {
                return CoarseIdempotencyDecision.Proceed(metadata);
            }

            if (existing.Record.ExpiresAt <= now &&
                _records.TryRemove(existing.Record.CoarseKeyHash, out _))
            {
                continue;
            }

            if (!string.Equals(existing.Record.CanonicalEquivalenceHash, proposed.CanonicalEquivalenceHash, StringComparison.Ordinal))
            {
                return CoarseIdempotencyDecision.Conflict(metadata);
            }

            CommandSubmissionResponse outcome = existing.Record.PriorOutcome ?? await existing
                .PriorOutcome
                .Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return CoarseIdempotencyDecision.ReplayPriorOutcome(Metadata(existing.Record), outcome);
        }
    }

    public ValueTask RecordOutcomeAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(outcome);

        if (_records.TryGetValue(metadata.CoarseKeyHash, out PendingRecord? pending))
        {
            pending.Record = pending.Record with { PriorOutcome = Clone(outcome) };
            pending.PriorOutcome.TrySetResult(Clone(outcome));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (_records.TryRemove(metadata.CoarseKeyHash, out PendingRecord? pending))
        {
            pending.PriorOutcome.TrySetCanceled(cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    private static CoarseIdempotencyMetadata Metadata(CoarseIdempotencyRecord record)
        => new(record.OperationClass, record.CoarseKeyHash, record.CanonicalEquivalenceHash, record.ExpiresAt);

    private static CommandSubmissionResponse Clone(CommandSubmissionResponse outcome)
        => new()
        {
            CommandId = outcome.CommandId,
            CorrelationId = outcome.CorrelationId,
            TaskId = outcome.TaskId,
            LifecycleState = outcome.LifecycleState,
            AcceptedAt = outcome.AcceptedAt,
        };

    private sealed class PendingRecord(CoarseIdempotencyRecord record)
    {
        public CoarseIdempotencyRecord Record { get; set; } = record;

        public TaskCompletionSource<CommandSubmissionResponse> PriorOutcome { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
