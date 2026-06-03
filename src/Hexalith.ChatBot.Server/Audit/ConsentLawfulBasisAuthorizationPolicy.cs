using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.10 (AC2, NFR2): the ONLY place a <see cref="ClaimsPrincipal"/> touches the consent/lawful-basis read
/// decision. It gates on the human compliance-admin scope and projects the requester's <b>actual</b> per-project owner
/// grants into the bounded <see cref="ConsentLawfulBasisAuthorityView"/> the pure
/// <see cref="ConsentLawfulBasisRedactionPolicy"/> consumes — mirroring
/// <see cref="DeletionErasureAuthorizationPolicy.AuthorityFor"/>. No second authority path is introduced. Reads stay
/// fail-closed: a project ref absent from the bounded view collapses to a redacted (subject-locator-dropped) record in
/// the redaction policy, never to a leak of the resource identity.
/// </summary>
internal static class ConsentLawfulBasisAuthorizationPolicy
{
    public static bool CanRecordConsentLawfulBasis(ClaimsPrincipal principal)
        => AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance);

    public static ConsentLawfulBasisAuthorityView AuthorityFor(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        HashSet<string> grantedProjects = principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .ToHashSet(StringComparer.Ordinal);

        return new ConsentLawfulBasisAuthorityView(CanRecordConsentLawfulBasis(principal), grantedProjects);
    }
}
