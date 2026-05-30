using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class InMemoryAuditWriter : IAuditWriter
{
    private readonly Lock _gate = new();
    private readonly List<ChatBotAuthorizationFailureAuditFact> _authorizationFailures = [];
    private readonly List<AuditEnvelope> _envelopes = [];

    public IReadOnlyList<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures
    {
        get
        {
            lock (_gate)
            {
                return [.. _authorizationFailures];
            }
        }
    }

    public IReadOnlyList<AuditEnvelope> Envelopes
    {
        get
        {
            lock (_gate)
            {
                return [.. _envelopes];
            }
        }
    }

    public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fact);
        lock (_gate)
        {
            _authorizationFailures.Add(fact);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        lock (_gate)
        {
            _envelopes.Add(envelope);
        }

        return ValueTask.FromResult(AuditWriteResult.Success);
    }

    public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        lock (_gate)
        {
            _envelopes.Add(envelope);
        }

        return ValueTask.FromResult(AuditWriteResult.Success);
    }
}
