namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Default no-op publisher: emits no signal. Used by direct unit construction of the projection store and by hosts
/// that have not enabled the projection-change notification transport.
/// </summary>
internal sealed class NoOpProjectConversationChangePublisher : IProjectConversationChangePublisher
{
    /// <summary>Shared instance for the projection store's optional-constructor default.</summary>
    public static readonly NoOpProjectConversationChangePublisher Instance = new();

    /// <inheritdoc/>
    public Task PublishProjectConversationChangedAsync(string tenantId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
