using Dapr.Client;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class DaprCoarseIdempotencyStore(DaprClient client, ISystemClock clock) : IIdempotencyStore
{
    private const string StateStoreName = "chatbot-statestore";

    private static readonly StateOptions StateOptions = new()
    {
        Consistency = ConsistencyMode.Strong,
        Concurrency = ConcurrencyMode.FirstWrite,
    };

    public async ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        DateTimeOffset now = clock.UtcNow;
        CoarseIdempotencyRecord proposed = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, now);
        CoarseIdempotencyMetadata metadata = Metadata(proposed);
        context.SetIdempotency(metadata);

        (CoarseIdempotencyRecord? existing, string etag) = await client
            .GetStateAndETagAsync<CoarseIdempotencyRecord>(
                StateStoreName,
                proposed.CoarseKeyHash,
                ConsistencyMode.Strong,
                metadata: null,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && existing.ExpiresAt <= now)
        {
            _ = await client
                .TryDeleteStateAsync(
                    StateStoreName,
                    proposed.CoarseKeyHash,
                    etag ?? string.Empty,
                    StateOptions,
                    metadata: null,
                    cancellationToken)
                .ConfigureAwait(false);
            existing = null;
            etag = string.Empty;
        }

        if (existing is null)
        {
            bool saved = await client
                .TrySaveStateAsync(
                    StateStoreName,
                    proposed.CoarseKeyHash,
                    proposed,
                    etag ?? string.Empty,
                    StateOptions,
                    metadata: null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (saved)
            {
                return CoarseIdempotencyDecision.Proceed(metadata);
            }

            (existing, _) = await client
                .GetStateAndETagAsync<CoarseIdempotencyRecord>(
                    StateStoreName,
                    proposed.CoarseKeyHash,
                    ConsistencyMode.Strong,
                    metadata: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (existing is null ||
            !string.Equals(existing.CanonicalEquivalenceHash, proposed.CanonicalEquivalenceHash, StringComparison.Ordinal) ||
            existing.PriorOutcome is null)
        {
            return CoarseIdempotencyDecision.Conflict(metadata);
        }

        return CoarseIdempotencyDecision.ReplayPriorOutcome(Metadata(existing), existing.PriorOutcome);
    }

    public async ValueTask RecordOutcomeAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(outcome);

        (CoarseIdempotencyRecord? existing, string etag) = await client
            .GetStateAndETagAsync<CoarseIdempotencyRecord>(
                StateStoreName,
                metadata.CoarseKeyHash,
                ConsistencyMode.Strong,
                metadata: null,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        CoarseIdempotencyRecord updated = existing with { PriorOutcome = Clone(outcome) };
        _ = await client
            .TrySaveStateAsync(
                StateStoreName,
                metadata.CoarseKeyHash,
                updated,
                etag,
                StateOptions,
                metadata: null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        (CoarseIdempotencyRecord? _, string etag) = await client
            .GetStateAndETagAsync<CoarseIdempotencyRecord>(
                StateStoreName,
                metadata.CoarseKeyHash,
                ConsistencyMode.Strong,
                metadata: null,
                cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(etag))
        {
            _ = await client
                .TryDeleteStateAsync(StateStoreName, metadata.CoarseKeyHash, etag, StateOptions, metadata: null, cancellationToken)
                .ConfigureAwait(false);
        }
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
}
