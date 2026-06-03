using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.10 (AC2, NFR2): <c>ConsentLawfulBasisAuthorizationPolicy</c> is the ONLY place a <see cref="ClaimsPrincipal"/>
/// is projected into the bounded <c>ConsentLawfulBasisAuthorityView</c> the pure
/// <c>ConsentLawfulBasisRedactionPolicy</c> consumes. It mirrors <c>TenantExportAuthorizationPolicy</c> — gating on the
/// human compliance scope and surfacing only the reviewer's actual, safe per-project owner grants. The gateway and
/// contract tests exercise the redaction only with a hand-built view; these pin the projection itself
/// (compliance-scope flag + <c>IsSafeStableIdentifier</c> grant filtering + non-human/non-compliance denial).
/// </summary>
public static class ConsentLawfulBasisAuthorizationPolicyTests
{
    [Fact]
    public static void AuthorityForShouldProjectComplianceScopeAndSafeProjectGrants()
    {
        ConsentLawfulBasisAuthorityView authority = ConsentLawfulBasisAuthorizationPolicy.AuthorityFor(
            ProjectOwner("compliance-admin", "project-authorized-001", "project-authorized-002"));

        authority.HasComplianceScope.ShouldBeTrue();
        authority.AuthorizedProjectRefs.ShouldBe(
            ["project-authorized-001", "project-authorized-002"], ignoreOrder: true);
    }

    [Fact]
    public static void AuthorityForShouldDropUnsafeProjectClaimValues()
    {
        ConsentLawfulBasisAuthorityView authority = ConsentLawfulBasisAuthorizationPolicy.AuthorityFor(
            ProjectOwner("compliance-admin", "project-authorized-001", "unsafe project!"));

        authority.AuthorizedProjectRefs.ShouldContain("project-authorized-001");
        authority.AuthorizedProjectRefs.ShouldNotContain("unsafe project!");
    }

    [Fact]
    public static void NonComplianceAndNonHumanActorsShouldNotBeAbleToRecordConsent()
    {
        ConsentLawfulBasisAuthorizationPolicy.CanRecordConsentLawfulBasis(Actor("human", "compliance-admin"))
            .ShouldBeTrue();

        foreach (ClaimsPrincipal principal in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "mailbox-admin"),
                     Actor("service", "compliance-admin"),
                     Actor("ai", "compliance-admin"),
                 })
        {
            ConsentLawfulBasisAuthorizationPolicy.CanRecordConsentLawfulBasis(principal).ShouldBeFalse();
            ConsentLawfulBasisAuthorizationPolicy.AuthorityFor(principal).HasComplianceScope.ShouldBeFalse();
        }
    }

    private static ClaimsPrincipal ProjectOwner(string role, params string[] projects)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, "human"),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
                .. projects.Select(static project => new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, project)),
            ],
            "test"));

    private static ClaimsPrincipal Actor(string actorType, string role)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
}
