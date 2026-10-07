using System.Globalization;
using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Synthetic owner records for existing unit personas. Raw labels are fixture inputs only.</summary>
internal static class RegressionAuthorityFixture
{
    private static readonly AsyncLocal<IReadOnlyDictionary<(string Subject, string Tenant), ClaimsPrincipal>?> Evidence = new();

    /// <summary>Looks up an exact synthetic owner persona by the bound requester and tenant.</summary>
    public static ClaimsPrincipal? EvidenceFor(string subject, string tenant)
        => Evidence.Value?.GetValueOrDefault((subject, tenant));

    /// <summary>Registers a copied test owner persona under its exact bound identity.</summary>
    public static ClaimsPrincipal SetEvidence(ClaimsPrincipal principal)
    {
        if (ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _) && context?.TenantId is { } tenant)
        {
            Dictionary<(string, string), ClaimsPrincipal> records = Evidence.Value is { } existing ? new(existing) : [];
            records[(context.SubjectId, tenant)] = new(principal.Identities.Select(static identity => identity.Clone()));
            Evidence.Value = records;
        }

        return principal;
    }

    /// <summary>Retains raw authenticated gateway input and registers separate exact owner fixture records.</summary>
    public static ClaimsPrincipal Principal(ClaimsPrincipal principal, bool bindTenant = false)
    {
        ClaimsPrincipal prepared = bindTenant ? PreparePolicyIdentity(principal) : principal;
        if (bindTenant)
        {
            string personaKey = string.Join("|", prepared.Claims.OrderBy(static claim => claim.Type, StringComparer.Ordinal).ThenBy(static claim => claim.Value, StringComparer.Ordinal).Select(static claim => $"{claim.Type}:{claim.Value}"));
            string subject = "synthetic-stage-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(personaKey)))[..20];
            foreach (ClaimsIdentity identity in prepared.Identities)
            {
                foreach (Claim claim in identity.Claims.Where(static claim => claim.Type is "sub" or ClaimTypes.NameIdentifier).ToArray())
                {
                    identity.RemoveClaim(claim);
                    identity.AddClaim(new(claim.Type, subject));
                }
            }
        }

        return SetEvidence(prepared);
    }

    /// <summary>Constructs an isolated downstream policy fixture with one exact hypothetical owner scope.</summary>
    public static ClaimsPrincipal PolicyPrincipal(ClaimsPrincipal principal, AdminScope scope)
    {
        ClaimsPrincipal prepared = PreparePolicyIdentity(principal);
        if (!ChatBotRequestContextResolver.TryResolve(prepared, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _))
        {
            return prepared;
        }

        AdminRole[] roles = prepared.FindAll(ParticipantAuthorizationStage.TenantRoleClaim)
            .Select(static claim => AdminRoles.TryFromWireValue(claim.Value, out AdminRole role) ? (AdminRole?)role : null)
            .Where(static role => role.HasValue).Select(static role => role!.Value).Distinct().ToArray();
        bool granted = !context!.IsMachine && roles.Length == 1 && AdminScopes.ScopesForRole(roles[0]).Contains(scope);
        string[] projects = prepared.FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim).Select(static claim => claim.Value).Where(static project => project != "*").ToArray();
        return new ChatBotAuthorityPrincipal(context, granted ? AdminScopes.ToWireValue(scope) : null, projects,
            granted ? [AdminScopes.ToWireValue(scope)] : [], granted ? roles : []);
    }

    private static ClaimsPrincipal PreparePolicyIdentity(ClaimsPrincipal principal)
    {
        ClaimsPrincipal prepared = new(principal.Identities.Select(static identity => identity.Clone()));
        ClaimsIdentity? identity = prepared.Identities.FirstOrDefault(static value => value.IsAuthenticated);
        if (identity is null)
        {
            return prepared;
        }

        if (!prepared.Claims.Any(static claim => claim.Type is "tenant" or "eventstore:tenant"))
        {
            identity.AddClaim(new("eventstore:tenant", "tenant-alpha"));
        }

        if (prepared.Claims.Any(static claim => (claim.Type is "actor_type" or ParticipantAuthorizationStage.ActorTypeClaim) && claim.Value is "service" or "ai") &&
            !prepared.HasClaim(static claim => claim.Type == ClaimsServiceClientGrantResolver.ServiceClientIdClaim) &&
            !prepared.Claims.Any(static claim => claim.Type == "preferred_username" && claim.Value.StartsWith("service-account-", StringComparison.Ordinal)))
        {
            identity.AddClaim(new(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "synthetic-client"));
        }

        return prepared;
    }

    /// <summary>Authorizes an isolated downstream stage using current synthetic owner evidence and independent machine grants.</summary>
    public static ChatBotRequestAuthorizer StageAuthorizer()
    {
        ISystemClock clock = new SystemClock();
        return TrustedAuthorityFixture.Authorizer(clock, new RegressionOwnerAuthorityProvider(clock, independentMachineGrants: true));
    }

    /// <summary>The current synthetic authority gate used by legacy direct gateway fixtures.</summary>
    public static ChatBotRequestAuthorizer Authorizer(ISystemClock clock)
        => TrustedAuthorityFixture.Authorizer(clock, new RegressionOwnerAuthorityProvider(clock));
    /// <summary>Registers synthetic owner records only in explicitly configured test hosts.</summary>
    public static void AddOwners(IServiceCollection services)
        => services.AddScoped<IChatBotOwnerAuthorityProvider>(static provider => new RegressionOwnerAuthorityProvider(provider.GetRequiredService<ISystemClock>()));

    /// <summary>Reads synthetic grant fixture records, independent of the production token resolver.</summary>
    public static ServiceClientGrant? ReadServiceGrant(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        string? Single(string type)
        {
            string[] values = principal.FindAll(type).Select(static claim => claim.Value).Distinct(StringComparer.Ordinal).ToArray();
            return values.Length == 1 ? values[0] : null;
        }

        string? client = Single(ClaimsServiceClientGrantResolver.ServiceClientIdClaim);
        string? grantId = Single(ClaimsServiceClientGrantResolver.GrantIdClaim);
        string? tenant = Single(ClaimsServiceClientGrantResolver.GrantTenantClaim);
        string? version = Single(ClaimsServiceClientGrantResolver.CommandSetVersionClaim);
        if (client is null || grantId is null || tenant is null || version is null ||
            !ServiceClientClasses.TryFromWireValue(Single(ClaimsServiceClientGrantResolver.ServiceClientClassClaim), out ServiceClientClass clientClass) ||
            !DateTimeOffset.TryParse(Single(ClaimsServiceClientGrantResolver.GrantExpiryClaim), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset expiry))
        {
            return null;
        }

        return new(grantId, tenant, client, clientClass,
            principal.FindAll(ClaimsServiceClientGrantResolver.GrantCommandClaim).Select(static claim => claim.Value).ToArray(),
            principal.FindAll(ClaimsServiceClientGrantResolver.GrantQueryClaim).Select(static claim => claim.Value).ToArray(),
            ChatBotSurfaceOrigins.FromWireValueOrDefault(Single(ClaimsServiceClientGrantResolver.GrantSurfaceClaim)), expiry,
            principal.HasClaim(ClaimsServiceClientGrantResolver.GrantRevokedClaim, "true"),
            principal.FindAll(ClaimsServiceClientGrantResolver.GrantScopeClaim).Select(static claim => claim.Value).ToArray(), version,
            Single(ClaimsServiceClientGrantResolver.DelegatedUserIdClaim), Single(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim));
    }
}
