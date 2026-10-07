using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Explicit synthetic owner evidence, confined to test assemblies.</summary>
internal sealed class SyntheticOwnerAuthorityProvider(ISystemClock clock) : IChatBotOwnerAuthorityProvider
{
    /// <summary>Restricts synthetic scope, independently of caller roles or flags.</summary>
    public Func<ChatBotOwnerAuthorityRequest, bool> Allows { get; set; } = static _ => true;
    /// <summary>Transforms owner evidence for adverse cases.</summary>
    public Func<ChatBotOwnerAuthorityEvidence, ChatBotOwnerAuthorityEvidence> Transform { get; set; } = static evidence => evidence;
    /// <summary>Records metadata-only owner requests.</summary>
    public List<ChatBotOwnerAuthorityRequest> Requests { get; } = [];
    /// <inheritdoc/>
    public ValueTask<ChatBotOwnerAuthorityEvidence?> GetAuthorityAsync(ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        DateTimeOffset now = clock.UtcNow;
        ServiceClientGrant? grant = request.Authority == "service-grant"
            ? new("synthetic-grant-v1", request.TenantId, request.ResourceId, ServiceClientClass.BackgroundWorker,
                new ChatBotAuthorityCatalog().Find(request.Operation, false) is null ? [] : [request.Operation],
                new ChatBotAuthorityCatalog().Find(request.Operation, true) is null ? [] : [request.Operation], request.Origin, now.AddMinutes(5), false, ["exact-operation"], "v1")
            : null;
        return ValueTask.FromResult<ChatBotOwnerAuthorityEvidence?>(Transform(new(request, "synthetic-evidence", "v1", now, now, now.AddMinutes(5), Allows(request), false, grant)));
    }
}
