using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Records denial facts and refuses every precommit or postcommit effect.</summary>
/// <param name="failure">An optional real audit-backend failure to propagate.</param>
/// <param name="duringWrite">An optional effect that runs inside the write, after the fact is attempted.</param>
internal sealed class BoundaryAuditWriter(Exception? failure = null, Action? duringWrite = null) : IAuditWriter
{
    /// <summary>The denial facts attempted by the handler.</summary>
    public List<ChatBotAuthorizationFailureAuditFact> Facts { get; } = [];

    /// <inheritdoc />
    /// <remarks>Like the production in-memory writer, the write ignores the token, so an abort row observes only the handler.</remarks>
    public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
    {
        Facts.Add(fact);
        duringWrite?.Invoke();
        return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
    }

    /// <inheritdoc />
    public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Boundary denial must not run precommit audit.");

    /// <inheritdoc />
    public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Boundary denial must not run postcommit audit.");
}
