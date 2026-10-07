using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>Guards each reconciliation state boundary with the retained request authority.</summary>
internal sealed class AuthorityBoundCoarseIdempotencyStateClient(ICoarseIdempotencyStateClient inner, Func<bool> isCurrent) : ICoarseIdempotencyStateClient
{
    /// <inheritdoc/>
    public async Task<(CoarseIdempotencyRecord? Record, string Etag)> ReadDomainAsync(string key, CancellationToken cancellationToken)
    {
        RequireCurrent();
        var result = await inner.ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        RequireCurrent();
        return result;
    }

    /// <inheritdoc/>
    public async Task<(CoarseCommandIdentityRecord? Record, string Etag)> ReadIdentityAsync(string key, CancellationToken cancellationToken)
    {
        RequireCurrent();
        var result = await inner.ReadIdentityAsync(key, cancellationToken).ConfigureAwait(false);
        RequireCurrent();
        return result;
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveDomainAsync(string key, CoarseIdempotencyRecord record, string etag, CancellationToken cancellationToken)
    {
        RequireCurrent();
        return inner.TrySaveDomainAsync(key, record, etag, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveIdentityAsync(string key, CoarseCommandIdentityRecord record, string etag, CancellationToken cancellationToken)
    {
        RequireCurrent();
        return inner.TrySaveIdentityAsync(key, record, etag, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> TryDeleteAsync(string key, string etag, CancellationToken cancellationToken)
    {
        RequireCurrent();
        return inner.TryDeleteAsync(key, etag, cancellationToken);
    }

    private void RequireCurrent()
    {
        if (!isCurrent()) { throw new ChatBotAuthorityLapsedException(); }
    }
}
