namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Publishes a metadata-only "project-conversation projection changed" signal (Story 10.6b reuse transport) so
/// subscribed UI clients re-query the typed server read state. The signal is advisory and non-authoritative: it
/// carries only the projection type and tenant, never response text, raw provider chunks, prompts, or any content.
/// The <see cref="NoOpProjectConversationChangePublisher"/> default keeps unit tests and non-DAPR hosts deterministic;
/// the DAPR-backed implementation is registered only when projection-change notifications are enabled.
/// </summary>
internal interface IProjectConversationChangePublisher
{
    /// <summary>
    /// Signals that the project-conversation read model changed for the given tenant. Implementations must be
    /// fail-open: a missed signal only means clients fall back to their existing typed re-query, never a projection
    /// failure.
    /// </summary>
    /// <param name="tenantId">The tenant whose project-conversation projection changed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task PublishProjectConversationChangedAsync(string tenantId, CancellationToken cancellationToken = default);
}

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
