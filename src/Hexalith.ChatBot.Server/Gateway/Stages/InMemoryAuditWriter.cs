using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class InMemoryAuditWriter : IAuditWriter
{
    public List<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures { get; } = [];

    public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fact);
        AuthorizationFailures.Add(fact);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordPreCommitAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordPostCommitAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.CompletedTask;
    }
}
