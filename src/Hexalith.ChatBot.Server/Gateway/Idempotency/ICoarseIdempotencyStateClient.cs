namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal interface ICoarseIdempotencyStateClient
{
    Task<(CoarseIdempotencyRecord? Record, string Etag)> ReadDomainAsync(string key, CancellationToken cancellationToken);

    Task<(CoarseCommandIdentityRecord? Record, string Etag)> ReadIdentityAsync(string key, CancellationToken cancellationToken);

    Task<bool> TrySaveDomainAsync(string key, CoarseIdempotencyRecord record, string etag, CancellationToken cancellationToken);

    Task<bool> TrySaveIdentityAsync(string key, CoarseCommandIdentityRecord record, string etag, CancellationToken cancellationToken);

    Task<bool> TryDeleteAsync(string key, string etag, CancellationToken cancellationToken);
}
