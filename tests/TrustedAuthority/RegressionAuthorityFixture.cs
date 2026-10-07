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
    private static readonly AsyncLocal<ClaimsPrincipal?> Evidence = new();
    /// <summary>The persona whose owner records are configured for the current test request.</summary>
    public static ClaimsPrincipal? EvidencePrincipal => Evidence.Value;
    /// <summary>Sets owner fixture metadata for a real request without changing its authenticated input.</summary>
    public static ClaimsPrincipal SetEvidence(ClaimsPrincipal principal)
    {
        Evidence.Value = principal;
        return principal;
    }

    /// <summary>Supplies synthetic owner-proven scopes to lower-level tests that isolate the downstream policy.</summary>
    public static ClaimsPrincipal Principal(ClaimsPrincipal principal)
    {
        SetEvidence(principal);
        if (!principal.Identities.Any(static identity => identity.IsAuthenticated))
        {
            return principal;
        }

        string subject = principal.FindFirstValue("sub") ?? "actor-alpha";
        string tenant = principal.FindFirstValue("eventstore:tenant") ?? principal.FindFirstValue("tenant") ?? "tenant-alpha";
        string actor = principal.FindFirstValue(ParticipantAuthorizationStage.ActorTypeClaim) ?? "human";
        string? client = principal.FindFirstValue(ClaimsServiceClientGrantResolver.ServiceClientIdClaim);
        actor = actor == "user" ? "human" : actor;
        ChatBotRequestContext context = new(subject, tenant, actor, client, ChatBotSurfaceOrigin.Api, principal);
        AdminRole[] roles = principal.FindAll(ParticipantAuthorizationStage.TenantRoleClaim).Select(static claim => AdminRoles.TryFromWireValue(claim.Value, out AdminRole role) ? (AdminRole?)role : null).Where(static role => role.HasValue).Select(static role => role!.Value).ToArray();
        string[] scopes = roles.SelectMany(AdminScopes.ScopesForRole).Select(AdminScopes.ToWireValue).Concat(roles.Contains(AdminRole.TenantAdmin) ? ["tenant"] : []).ToArray();
        string[] projects = principal.FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim).Select(static claim => claim.Value).Where(static value => value != "*").ToArray();
        ChatBotAuthorityPrincipal snapshot = new(context, roles.Contains(AdminRole.TenantAdmin) ? "tenant" : scopes.FirstOrDefault(), projects, scopes, roles);
        // Preserve test persona labels so the test-owned provider can map them to separate exact current grants.
        ClaimsIdentity fixtureLabels = new("synthetic-owner-labels");
        foreach (Claim claim in principal.FindAll(ParticipantAuthorizationStage.TenantRoleClaim))
        {
            fixtureLabels.AddClaim(claim);
        }

        snapshot.AddIdentity(fixtureLabels);
        return SetEvidence(snapshot);
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
