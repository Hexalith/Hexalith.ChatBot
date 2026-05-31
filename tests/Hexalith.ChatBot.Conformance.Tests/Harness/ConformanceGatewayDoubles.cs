using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

// Recording in-process doubles promoted from the private doubles in
// tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs (they are file-private there, so the
// differential-conformance harness owns its own copies). These back the admission-event-sequence oracle: the
// audit writer captures the ordered .Envelopes, the dispatcher counts dispatches. The internal production
// stores (InMemoryCoarseIdempotencyStore / InMemoryOperationStatusStore) are consumed directly via IVT — never
// duplicated — so their behavior cannot diverge from production.

/// <summary>A deterministic clock so per-run timestamps are stable and excluded from the differential.</summary>
internal sealed class FixedConformanceClock : ISystemClock
{
    public static DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow => FixedUtcNow;
}

/// <summary>Counts dispatches so the harness can prove exactly one durable effect on a replay.</summary>
internal sealed class RecordingDispatcher : ICommandDispatcher
{
    public int DispatchCount { get; private set; }

    public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        DispatchCount++;
        return ValueTask.FromResult(new ChatBotDispatchResult(FixedConformanceClock.FixedUtcNow.AddSeconds(1)));
    }
}

/// <summary>Captures the ordered admission audit-envelope sequence (the event sequence the oracle compares).</summary>
internal sealed class RecordingAuditWriter : IAuditWriter
{
    public List<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures { get; } = [];

    public List<AuditEnvelope> Envelopes { get; } = [];

    public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
    {
        AuthorizationFailures.Add(fact);
        return ValueTask.CompletedTask;
    }

    public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        Envelopes.Add(envelope);
        return ValueTask.FromResult(AuditWriteResult.Success);
    }

    public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        Envelopes.Add(envelope);
        return ValueTask.FromResult(AuditWriteResult.Success);
    }
}

/// <summary>No-op replay queue: the admitted parity paths never enqueue a replay intent.</summary>
internal sealed class NoOpReplayIntentQueue : IAuditReplayIntentQueue
{
    public ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

/// <summary>No-op operator alert sink: the admitted parity paths never alert.</summary>
internal sealed class NoOpOperatorAlertSink : IOperatorAlertSink
{
    public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
