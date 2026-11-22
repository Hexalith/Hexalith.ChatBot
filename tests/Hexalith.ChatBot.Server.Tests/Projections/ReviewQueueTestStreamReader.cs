using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.Server.Tests.Projections;

internal sealed class ReviewQueueTestStreamReader(Func<AggregateIdentity, AuthoritativeEventStream?> read) : IAuthoritativeEventStreamReader
{
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthoritativeStreamReadResult(read(identity), null));
}
