using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>A server-issued principal containing only owner-proven operation authority.</summary>
internal sealed class ChatBotAuthorityPrincipal : ClaimsPrincipal
{
    private readonly string[] _projects;
    private readonly string[] _adminScopes;
    private readonly AdminRole[] _adminRoles;
    private readonly (ChatBotOwnerAuthorityEvidence Evidence, DateTimeOffset Started)[] _evidence;
    private readonly ServiceClientGrantProjectionCache? _grants;

    /// <summary>Creates a narrow principal after exact evidence validation.</summary>
    internal ChatBotAuthorityPrincipal(ChatBotRequestContext context, string? adminScope, IEnumerable<string> projects, IEnumerable<string>? scopedAdminGrants = null, IEnumerable<AdminRole>? scopedRoles = null, IEnumerable<(ChatBotOwnerAuthorityEvidence Evidence, DateTimeOffset Started)>? validatedEvidence = null, ServiceClientGrantProjectionCache? grants = null)
        : base(new ClaimsIdentity(BindingClaims(context), "trusted-context"))
    {
        Context = context;
        _evidence = validatedEvidence?.ToArray() ?? [];
        _grants = grants;
        AdminScope = context.IsMachine ? null : adminScope;
        _adminScopes = context.IsMachine ? [] : (scopedAdminGrants ?? (adminScope is null ? [] : [adminScope])).Distinct(StringComparer.Ordinal).ToArray();
        _adminRoles = context.IsMachine ? [] : (scopedRoles ?? (AdminRoles.TryFromWireValue(
            adminScope == "tenant" ? "tenant-admin" : adminScope == "operate" ? "operations-admin" : $"{adminScope}-admin", out AdminRole role) ? [role] : [])).Distinct().ToArray();
        _projects = projects.Distinct(StringComparer.Ordinal).ToArray();
        ClaimsIdentity authority = new("owner-authority");
        if (AdminScope is not null)
        {
            authority.AddClaim(new Claim(ParticipantAuthorizationStage.TenantRoleClaim, AdminScope == "tenant" ? "tenant-admin" : AdminScope == "operate" ? "operations-admin" : $"{AdminScope}-admin"));
        }

        foreach (string project in _projects)
        {
            authority.AddClaim(new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, project));
        }

        AddIdentity(authority);
    }

    private static IEnumerable<Claim> BindingClaims(ChatBotRequestContext context)
    {
        yield return new("sub", context.SubjectId);
        yield return new(ClaimTypes.NameIdentifier, context.SubjectId);
        if (context.TenantId is not null)
        {
            yield return new("eventstore:tenant", context.TenantId);
        }

        yield return new(ParticipantAuthorizationStage.ActorTypeClaim, context.ActorClass);
        if (context.ServiceClientId is not null)
        {
            yield return new(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, context.ServiceClientId);
        }
    }

    /// <summary>Revalidates all retained owner bounds and known revocation at a protected-effect boundary.</summary>
    public bool IsCurrent(DateTimeOffset now)
        => _evidence.All(item => ChatBotRequestAuthorizer.IsValidEvidence(item.Evidence, item.Evidence.Request, item.Started, now) &&
            (item.Evidence.ServiceGrant is not { } grant || (grant.ExpiresAt > now && _grants is not null &&
                !_grants.IsRevoked(item.Evidence.Request, grant.GrantId) && !_grants.PredatesClientRevocation(item.Evidence))));

    /// <summary>The immutable authenticated binding.</summary>
    public ChatBotRequestContext Context { get; }
    /// <summary>The exact current scoped ChatBot administration grant.</summary>
    public string? AdminScope { get; }
    /// <summary>Tests only exact owner-proven administration scope.</summary>
    public bool HasAdminScope(string scope) => _adminScopes.Contains(scope, StringComparer.Ordinal);
    /// <summary>Tests only the role mapped from a current operation-scoped owner grant.</summary>
    public bool HasAdminRole(AdminRole role) => _adminRoles.Contains(role);
    /// <summary>Tests only exact owner-proven project scope.</summary>
    public bool HasProject(string project) => _projects.Contains(project, StringComparer.Ordinal);
}
