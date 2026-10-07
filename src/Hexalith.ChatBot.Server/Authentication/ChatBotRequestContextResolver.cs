using System.Security.Claims;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>Binds only authenticated evidence and rejects ambiguous or malformed evidence.</summary>
internal sealed class ChatBotRequestContextResolver(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Resolves the authenticated transport context for SDK reads.</summary>
    public ChatBotRequestContext? ResolveCurrent()
    {
        HttpContext? http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return null;
        }

        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigins.FromWireValueOrDefault(http.Request.Headers["X-Hexalith-Surface-Origin"].ToString());
        return TryResolve(http.User, origin, out ChatBotRequestContext? context, out _) && context?.TenantId is not null ? context : null;
    }

    /// <summary>Binds a command relay only through the authenticated EventStore process workload.</summary>
    public ChatBotRequestContext? ResolveCommand(CommandEnvelope command)
    {
        if (!IsEventStoreRelay("domain-service:process"))
        {
            return HasEventStoreWorkloadIdentity() ? null : ResolveCurrent();
        }

        if (!TryRelayExtension(command, "surfaceOrigin", out string? origin) ||
            !TryRelayExtension(command, "actorType", out string? actor) ||
            !TryRelayExtension(command, "serviceClientId", out string? client))
        {
            return null;
        }
        return ResolveRelay(command.UserId, command.TenantId, origin, actor, client);
    }

    /// <summary>Binds an authenticated EventStore query relay; the existing envelope defaults to API provenance.</summary>
    public ChatBotRequestContext? ResolveQuery(QueryEnvelope query)
    {
        if (!IsEventStoreRelay("domain-service:query"))
        {
            return HasEventStoreWorkloadIdentity() ? null : ResolveCurrent();
        }

        string? origin = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(query.Payload);
            JsonProperty[] declarations = document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().Where(static property => string.Equals(property.Name, "surfaceOrigin", StringComparison.OrdinalIgnoreCase)).ToArray()
                : [];
            if (declarations.Length > 1 || declarations.Any(static property => property.Name != "surfaceOrigin" || property.Value.ValueKind != JsonValueKind.String))
            {
                return null;
            }

            origin = declarations.Length == 1 ? declarations[0].Value.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }

        return ResolveRelay(query.UserId, query.TenantId, origin, null, null);
    }

    private static bool TryRelayExtension(CommandEnvelope command, string name, out string? value)
    {
        KeyValuePair<string, string>[] declarations = command.Extensions?.Where(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)).ToArray() ?? [];
        value = declarations.Length == 1 ? declarations[0].Value : null;
        return declarations.Length == 0 || (declarations.Length == 1 && declarations[0].Key == name && value is not null);
    }

    private bool HasEventStoreWorkloadIdentity()
        => httpContextAccessor.HttpContext?.User.Identities.Any(static identity => identity.IsAuthenticated && identity.AuthenticationType == "EventStoreWorkload") == true;

    private bool IsEventStoreRelay(string operation)
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
        if (principal is null || !principal.Identities.Any(static identity => identity.IsAuthenticated && identity.AuthenticationType == "EventStoreWorkload"))
        {
            return false;
        }

        ClaimsPrincipal authenticated = new(principal.Identities.Where(static identity => identity.IsAuthenticated));
        return TrySingle(authenticated, [ClaimTypes.NameIdentifier, "sub"], true, out string? subject) && subject == "workload:eventstore" &&
            TrySingle(authenticated, ["eventstore:workload"], true, out string? workload) && workload == "eventstore" &&
            authenticated.Identities.Where(static identity => identity.AuthenticationType == "EventStoreWorkload")
                .SelectMany(static identity => identity.Claims).Any(claim => claim.Type == "eventstore:operation" && claim.Value == operation);
    }

    private static ChatBotRequestContext? ResolveRelay(string subject, string tenant, string? declaredOrigin, string? actor, string? client)
    {
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigins.FromWireValueOrDefault(declaredOrigin);
        if (declaredOrigin is not null && ChatBotSurfaceOrigins.ToWireValue(origin) != declaredOrigin)
        {
            return null;
        }

        // The pinned relay contract has no trusted actor-class field for opaque subjects.
        // Do not infer human authority from caller-shaped extensions or the OAuth application's id.
        if (!subject.StartsWith("service-account-", StringComparison.Ordinal) || actor is not (null or "service"))
        {
            return null;
        }

        string serviceAccount = subject["service-account-".Length..];
        if (client is not null && client != serviceAccount)
        {
            return null;
        }

        List<Claim> claims = [new("sub", subject), new("eventstore:tenant", tenant),
            new(ParticipantAuthorizationStage.ActorTypeClaim, "service"), new(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, serviceAccount)];

        return TryResolve(new(new ClaimsIdentity(claims, "eventstore-relay")), origin, out ChatBotRequestContext? context, out _) && context?.TenantId is not null ? context : null;
    }

    /// <summary>Creates a snapshot from authenticated identities without trusting supplemental identities.</summary>
    public static bool TryResolve(ClaimsPrincipal principal, ChatBotSurfaceOrigin origin, out ChatBotRequestContext? context, out string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(principal);
        context = null;
        reasonCode = ChatBotAuthorizationReasonCodes.AuthenticationDenied;
        ClaimsIdentity[] identities = principal.Identities.Where(static identity => identity.IsAuthenticated).ToArray();
        if (identities.Length == 0)
        {
            return false;
        }

        ClaimsPrincipal authenticated = new(identities.Select(static identity => identity.Clone()));
        if (!TrySingle(authenticated, ["sub", ClaimTypes.NameIdentifier], true, out string? subject) ||
            !TrySingle(authenticated, [ParticipantAuthorizationStage.ActorTypeClaim, "actor_type"], false, out string? actor) ||
            !TrySingle(authenticated, [ClaimsServiceClientGrantResolver.ServiceClientIdClaim], false, out string? serviceClient))
        {
            return false;
        }

        if (authenticated.Claims.Any(static claim =>
            (string.Equals(claim.Type, "eventstore:tenant", StringComparison.OrdinalIgnoreCase) || string.Equals(claim.Type, "tenant", StringComparison.OrdinalIgnoreCase)) && claim.Type is not ("eventstore:tenant" or "tenant")))
        {
            return false;
        }

        if (!TrySingle(authenticated, ["eventstore:tenant", "tenant"], false, out string? tenant))
        {
            // Authentication remains bound; tenant binding denies unresolved scope and queues mailbox intake.
            tenant = null;
        }

        Claim[] preferredUsernames = authenticated.Claims.Where(static claim => string.Equals(claim.Type, "preferred_username", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (preferredUsernames.Any(static claim => claim.Type != "preferred_username"))
        {
            return false;
        }

        string[] serviceAccounts = preferredUsernames
            .Select(static claim => claim.Value)
            .Where(static value => value.StartsWith("service-account-", StringComparison.Ordinal))
            .Select(static value => value["service-account-".Length..]).ToArray();
        if (serviceAccounts.Any(static value => !AuditMetadata.IsSafeStableIdentifier(value)) ||
            serviceAccounts.Distinct(StringComparer.Ordinal).Count() > 1 ||
            (serviceClient is not null && serviceAccounts.Any(value => !string.Equals(value, serviceClient, StringComparison.Ordinal))))
        {
            return false;
        }

        serviceClient ??= serviceAccounts.FirstOrDefault();
        actor ??= serviceClient is null ? "human" : "service";
        actor = actor == "user" ? "human" : actor;
        if (actor is not ("human" or "service" or "ai") ||
            (serviceClient is not null && actor == "human") ||
            (actor is "service" or "ai" && serviceClient is null) || !Enum.IsDefined(origin))
        {
            return false;
        }

        // azp/client_id identify the OAuth application on human tokens, not a machine subject.
        context = new ChatBotRequestContext(subject!, tenant, actor, serviceClient, origin, authenticated);
        reasonCode = string.Empty;
        return true;
    }

    private static bool TrySingle(ClaimsPrincipal principal, string[] types, bool required, out string? value)
    {
        Claim[] claims = principal.Claims.Where(claim => types.Contains(claim.Type, StringComparer.OrdinalIgnoreCase)).ToArray();
        value = null;
        if (claims.Any(claim => !types.Contains(claim.Type, StringComparer.Ordinal)))
        {
            return false;
        }

        string[] values = claims.Select(static claim => claim.Value).ToArray();
        if (values.Length == 0)
        {
            return !required;
        }

        if (values.Any(static candidate => !AuditMetadata.IsSafeStableIdentifier(candidate)) || values.Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return false;
        }

        value = values[0];
        return true;
    }
}
