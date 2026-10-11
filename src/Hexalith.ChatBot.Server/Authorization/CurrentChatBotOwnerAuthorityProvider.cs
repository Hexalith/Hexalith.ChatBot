using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Contracts.Queries;
using System.Text.Json;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Obtains current ChatBot grants from a stable, complete owner stream.</summary>
internal sealed class CurrentChatBotOwnerAuthorityProvider(
    ChatBotBoundOwnerStreamGateway gateway,
    ISystemClock clock,
    ChatBotAuthorityCatalog catalog) : IChatBotOwnerAuthorityProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<ChatBotOwnerAuthorityEvidence?> GetAuthorityAsync(
        ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        if (!AuditMetadata.IsSafeStableIdentifier(request.TenantId) ||
            !AuditMetadata.IsSafeStableIdentifier(request.PrincipalId) ||
            !AuditMetadata.IsSafeStableIdentifier(request.ResourceId) ||
            !AuditMetadata.IsSafeStableIdentifier(request.Operation) ||
            !AuditMetadata.IsSafeStableIdentifier(request.Authority) ||
            request.ActorClass is not ("human" or "service" or "ai") ||
            !Enum.IsDefined(request.Origin))
        {
            return null;
        }

        if (request.Owner == "Tenants")
        {
            return await ReadTenantOwnerAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (request.Owner == "Parties")
        {
            return await ReadPartyIdentityAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (request.Owner == "Projects")
        {
            return await ReadProjectGrantAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (request.Owner != "ChatBot" ||
            catalog.Find(request.Operation, false) is null && catalog.Find(request.Operation, true) is null)
        {
            return null;
        }

        try
        {
            string key = ChatBotAuthorityGrantKey.For(request.TenantId, request.PrincipalId,
                request.ResourceId, request.Operation, request.Authority, request.ActorClass, request.Origin);
            var identity = new AggregateIdentity(request.TenantId, "chatbot", key);
            var reader = new AuthoritativeEventStreamReader(gateway, TimeProvider.System, TimeSpan.FromSeconds(10));
            AuthoritativeStreamReadResult result = await reader.ReadAsync(identity, cancellationToken).ConfigureAwait(false);
            return ChatBotGrantSourceEvidence.FromStream(request, result.Stream, clock.UtcNow);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private async ValueTask<ChatBotOwnerAuthorityEvidence?> ReadTenantOwnerAsync(
        ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        ChatBotAuthorityRequirement? row = catalog.Find(request.Operation, false) ?? catalog.Find(request.Operation, true);
        if (request.ActorClass != "human" || request.Authority != "TenantOwner" ||
            request.ResourceId != request.TenantId ||
            row is null || row.AdminScope is null && request.Operation is not
                ("GrantChatBotAuthority" or "RevokeChatBotAuthority"))
        {
            return null;
        }

        try
        {
            var payload = new TenantOwnerRequest(request.TenantId, request.PrincipalId, request.Operation,
                request.ActorClass, request.Origin.ToString());
            var query = new SubmitQueryRequest("system", "tenants", request.TenantId,
                "Hexalith.Tenants.Contracts.Queries.ResolveTenantOwnerAuthority",
                Payload: JsonSerializer.SerializeToElement(payload, JsonOptions));
            SubmitQueryResponse response = await gateway.ReadOwnerAuthorityAsync(query, request.PrincipalId,
                cancellationToken).ConfigureAwait(false);
            TenantOwnerResult? result = response.Payload.Deserialize<TenantOwnerResult>(JsonOptions);
            DateTimeOffset now = clock.UtcNow;
            if (result is null || !result.Allowed || result.TenantId != request.TenantId ||
                result.PrincipalId != request.PrincipalId || result.Operation != request.Operation ||
                result.ActorClass != request.ActorClass || result.Origin != request.Origin.ToString() ||
                result.SourceVersion <= 0 || string.IsNullOrWhiteSpace(result.ObservationId) ||
                result.ObservedAt > now || now - result.ObservedAt > TimeSpan.FromSeconds(60))
            {
                return null;
            }

            return new(request, result.ObservationId, result.SourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                result.ObservedAt, result.ObservedAt, result.ObservedAt.AddSeconds(60), true, false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private async ValueTask<ChatBotOwnerAuthorityEvidence?> ReadPartyIdentityAsync(
        ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        if (request.Authority != "identity" ||
            catalog.Find(request.Operation, false) is null && catalog.Find(request.Operation, true) is null)
        {
            return null;
        }

        try
        {
            var payload = new PartyIdentityRequest(request.TenantId, request.PrincipalId,
                request.ResourceId, request.Operation, request.ActorClass, request.Origin.ToString());
            var query = new SubmitQueryRequest(request.TenantId, "party", request.ResourceId,
                "Hexalith.Parties.Contracts.Queries.ResolveChatBotPartyAuthority",
                Payload: JsonSerializer.SerializeToElement(payload, JsonOptions));
            SubmitQueryResponse response = await gateway.ReadOwnerAuthorityAsync(query, request.PrincipalId,
                cancellationToken).ConfigureAwait(false);
            PartyIdentityResult? result = response.Payload.Deserialize<PartyIdentityResult>(JsonOptions);
            DateTimeOffset now = clock.UtcNow;
            if (result is null || !result.Allowed || result.TenantId != request.TenantId ||
                result.PrincipalId != request.PrincipalId || result.PartyId != request.ResourceId ||
                result.Operation != request.Operation || result.ActorClass != request.ActorClass ||
                result.Origin != request.Origin.ToString() || result.SourceVersion <= 0 ||
                string.IsNullOrWhiteSpace(result.ObservationId) || result.ObservedAt > now ||
                now - result.ObservedAt > TimeSpan.FromSeconds(60))
            {
                return null;
            }

            return new(request, result.ObservationId,
                result.SourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                result.ObservedAt, result.ObservedAt, result.ObservedAt.AddSeconds(60), true, false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private async ValueTask<ChatBotOwnerAuthorityEvidence?> ReadProjectGrantAsync(
        ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        if (request.Authority != "project" ||
            catalog.Find(request.Operation, false) is null && catalog.Find(request.Operation, true) is null)
        {
            return null;
        }

        try
        {
            var payload = new ProjectGrantRequest(request.TenantId, request.PrincipalId,
                request.ResourceId, request.Operation, request.ActorClass, request.Origin.ToString());
            var query = new SubmitQueryRequest(request.TenantId, "projects", request.ResourceId,
                "Hexalith.Projects.Contracts.Queries.ResolveProjectChatBotAuthority",
                Payload: JsonSerializer.SerializeToElement(payload, JsonOptions));
            SubmitQueryResponse response = await gateway.ReadOwnerAuthorityAsync(query, request.PrincipalId,
                cancellationToken).ConfigureAwait(false);
            ProjectGrantResult? result = response.Payload.Deserialize<ProjectGrantResult>(JsonOptions);
            DateTimeOffset now = clock.UtcNow;
            if (result is null || !result.Allowed || result.TenantId != request.TenantId ||
                result.PrincipalId != request.PrincipalId || result.ProjectId != request.ResourceId ||
                result.Operation != request.Operation || result.ActorClass != request.ActorClass ||
                result.Origin != request.Origin.ToString() || result.SourceVersion <= 0 ||
                string.IsNullOrWhiteSpace(result.ObservationId) ||
                !AuditMetadata.IsSafeStableIdentifier(result.GrantId) ||
                result.ObservedAt > now || now - result.ObservedAt > TimeSpan.FromSeconds(60) ||
                result.ExpiresAt is not { } expiresAt || expiresAt <= now)
            {
                return null;
            }

            DateTimeOffset boundedExpiry = expiresAt < result.ObservedAt.AddSeconds(60)
                ? expiresAt : result.ObservedAt.AddSeconds(60);
            return new(request, result.ObservationId,
                result.SourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                result.ObservedAt, result.ObservedAt, boundedExpiry, true, false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private sealed record TenantOwnerRequest(string TenantId, string PrincipalId, string Operation,
        string ActorClass, string Origin);

    private sealed record TenantOwnerResult(string TenantId, string PrincipalId, string Operation,
        string ActorClass, string Origin, bool Allowed, long SourceVersion, string ObservationId,
        DateTimeOffset ObservedAt);

    private sealed record PartyIdentityRequest(string TenantId, string PrincipalId, string PartyId,
        string Operation, string ActorClass, string Origin);

    private sealed record PartyIdentityResult(string TenantId, string PrincipalId, string PartyId,
        string Operation, string ActorClass, string Origin, bool Allowed, long SourceVersion,
        string ObservationId, DateTimeOffset ObservedAt);

    private sealed record ProjectGrantRequest(string TenantId, string PrincipalId, string ProjectId,
        string Operation, string ActorClass, string Origin);

    private sealed record ProjectGrantResult(string TenantId, string PrincipalId, string ProjectId,
        string Operation, string ActorClass, string Origin, bool Allowed, string? GrantId,
        long SourceVersion, string ObservationId, DateTimeOffset ObservedAt,
        DateTimeOffset? ExpiresAt);
}
