using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Shared builders and test doubles for the Story 9.1 WORM audit-chain tests.</summary>
internal static class WormAuditTestData
{
    public static readonly DateTimeOffset FixedNow = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a metadata-only post-commit envelope for chaining tests.</summary>
    public static AuditEnvelope Envelope(
        string tenantId,
        string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
        string commandName = "TestCommand",
        string resourceId = "res-1")
        => new(
            tenantId,
            ActorId: "actor-1",
            ActorType: "system",
            CommandName: commandName,
            ResourceId: resourceId,
            Decision: "allow",
            ReasonCode: "eventstore_dispatch_accepted",
            CorrelationId: correlationId,
            Timestamp: FixedNow,
            PolicySnapshotId: "chatbot.gateway.policy-snapshot.v1",
            SourceEvidenceRefs: [$"command:{resourceId}", $"correlation:{correlationId}", "phase:post-commit"],
            IdempotencyKey: null,
            StateTransition: "Proposed->Accepted",
            RedactionDecision: CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "proposed",
            Phase: AuditCommitPhase.PostCommit,
            EnvelopeSchemaVersion: "chatbot.audit-envelope.v1",
            PredecessorHash: null,
            SurfaceOrigin: "worker");

    public sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    /// <summary>A WORM store whose append always fails, exercising the fail-open-then-reconcile path.</summary>
    public sealed class FailingWormAuditStore : IWormAuditStore
    {
        public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(WormAuditAppendOutcome.Unavailable("worm_store_unavailable"));

        public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants() => [];
    }

    /// <summary>A WORM store that serves a preset, already-built chain for one tenant (e.g. a tampered chain).</summary>
    public sealed class StubWormAuditStore(string tenantRef, IReadOnlyList<WormAuditChainRecord> chain) : IWormAuditStore
    {
        public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => throw new NotSupportedException("Stub store is read-only.");

        public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId)
            => string.Equals(tenantId, tenantRef, StringComparison.Ordinal) ? chain : [];

        public IReadOnlyList<string> EnumerateTenants() => [tenantRef];
    }

    /// <summary>Builds a real chain for a tenant, then mutates the record at the given sequence to break it.</summary>
    public static async Task<IReadOnlyList<WormAuditChainRecord>> BuildTamperedChainAsync(string tenantRef, int length, int tamperAtSequence)
    {
        InMemoryWormAuditStore store = new();
        for (int i = 0; i < length; i++)
        {
            _ = await store.AppendAsync(Envelope(tenantRef, resourceId: $"r{i}"), CancellationToken.None).ConfigureAwait(false);
        }

        List<WormAuditChainRecord> chain = [.. store.EnumerateChain(tenantRef)];
        chain[tamperAtSequence] = chain[tamperAtSequence] with
        {
            Envelope = chain[tamperAtSequence].Envelope with { Outcome = "tampered" },
        };
        return chain;
    }

    /// <summary>A WORM store whose enumeration throws, exercising the fail-closed (Unknown) verification path.</summary>
    public sealed class ThrowingWormAuditStore : IWormAuditStore
    {
        public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => throw new InvalidOperationException("store down");

        public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId)
            => throw new InvalidOperationException("store down");

        public IReadOnlyList<string> EnumerateTenants() => ["tenant-alpha"];
    }

    /// <summary>An audit writer whose pre-commit write always fails, exercising the fail-closed audit-then-deliver path.</summary>
    public sealed class UnavailableAuditWriter : IAuditWriter
    {
        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Unavailable());

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Unavailable());
    }
}
