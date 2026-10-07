using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Maps legacy test persona labels to explicit synthetic current owner records; never registered by product code.</summary>
internal sealed class RegressionOwnerAuthorityProvider(ISystemClock clock, ClaimsPrincipal? persona = null, bool independentMachineGrants = false) : IChatBotOwnerAuthorityProvider
{
    /// <inheritdoc/>
    public ValueTask<ChatBotOwnerAuthorityEvidence?> GetAuthorityAsync(ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        ClaimsPrincipal? fixture = persona ?? RegressionAuthorityFixture.EvidenceFor(request.PrincipalId, request.TenantId);
        bool bound = fixture is not null && Hexalith.ChatBot.Server.Authentication.ChatBotRequestContextResolver.TryResolve(fixture, request.Origin, out Hexalith.ChatBot.Server.Authentication.ChatBotRequestContext? context, out _) &&
            context?.SubjectId == request.PrincipalId && context.TenantId == request.TenantId && context.ActorClass == request.ActorClass;
        bool allowed = bound && request.Owner switch
        {
            "Parties" when fixture?.FindFirst(ParticipantAuthorizationStage.ParticipantAuthorityClaim)?.Value is "unresolved" or "email-only" or "unauthorized" or "directory-degraded" => false,
            "Projects" => fixture?.FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim).Any(claim => claim.Value == request.ResourceId) == true,
            "ChatBot" when request.Authority.StartsWith("admin:", StringComparison.Ordinal) => HasScope(fixture, request.Authority["admin:".Length..]),
            "Tenants" => HasScope(fixture, "tenant") || fixture?.FindAll(ParticipantAuthorizationStage.TenantRoleClaim).Any() == true,
            _ => true,
        };
        DateTimeOffset now = clock.UtcNow;
        return ValueTask.FromResult<ChatBotOwnerAuthorityEvidence?>(new(request, "synthetic-regression-owner", "v1", now, now, now.AddMinutes(5), allowed, false,
            request.Authority == "service-grant" ? independentMachineGrants
                ? new Hexalith.ChatBot.Contracts.Identities.ServiceClientGrant("synthetic-stage-grant", request.TenantId, request.ResourceId, ServiceClientClass.BackgroundWorker,
                    [request.Operation], [], request.Origin, now.AddMinutes(5), false, ["exact-operation"], "v1")
                : RegressionAuthorityFixture.ReadServiceGrant(fixture) : null));
    }

    private static bool HasScope(ClaimsPrincipal? principal, string scope)
        => principal?.FindAll(ParticipantAuthorizationStage.TenantRoleClaim).Any(claim =>
            AdminRoles.TryFromWireValue(claim.Value, out AdminRole role) &&
            (scope == "tenant" ? role == AdminRole.TenantAdmin : AdminScopes.ScopesForRole(role).Any(value => AdminScopes.ToWireValue(value) == scope))) == true;
}
