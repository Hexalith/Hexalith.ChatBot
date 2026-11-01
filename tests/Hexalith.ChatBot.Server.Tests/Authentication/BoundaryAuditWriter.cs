using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Records denial facts and refuses every precommit or postcommit effect.</summary>
/// <param name="failure">An optional real audit-backend failure to propagate.</param>
internal sealed class BoundaryAuditWriter(Exception? failure = null) : IAuditWriter
{
    /// <summary>The denial facts attempted by the handler.</summary>
    public List<ChatBotAuthorizationFailureAuditFact> Facts { get; } = [];

    /// <inheritdoc />
    public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Facts.Add(fact);
        return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
    }

    /// <inheritdoc />
    public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Boundary denial must not run precommit audit.");

    /// <inheritdoc />
    public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Boundary denial must not run postcommit audit.");
}
