using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Admin;
using Hexalith.ChatBot.Server.Lifecycle.Retry;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.8 (AC2, NFR2): the ONLY place a <see cref="ClaimsPrincipal"/> touches the export decision. It gates on
/// the human compliance-admin scope and projects the reviewer's <b>actual</b> per-project owner grants into the
/// bounded <see cref="TenantExportAuthorityView"/> the pure <see cref="TenantExportPlanner"/> consumes — mirroring
/// <see cref="ComplianceAuditReadPolicy.HasPerProjectAuthority"/>. No second authority path is introduced.
/// </summary>
internal static class TenantExportAuthorizationPolicy
{
    public static bool CanRequestTenantExport(ClaimsPrincipal principal)
        => AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance);

    public static TenantExportAuthorityView AuthorityFor(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        HashSet<string> grantedProjects = principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .ToHashSet(StringComparer.Ordinal);

        return new TenantExportAuthorityView(CanRequestTenantExport(principal), grantedProjects);
    }
}

/// <summary>
/// Story 9.8 (AC3, NFR17/NFR18): maps a per-class extraction failure to a bounded
/// <see cref="TenantExportClassStatuses"/> token via the ONE retry taxonomy (<see cref="RetryFailurePolicy"/>) —
/// a retryable decision is <c>failed-retryable</c>, a terminal/exhausted decision is <c>failed-terminal</c>. No
/// second retryable-vs-terminal classifier is introduced. (The byte-producing extraction runtime that calls this
/// is the deferred surface; the classification seam itself ships now.)
/// </summary>
internal static class TenantExportFailureClassifier
{
    public static string ClassifyClassStatus(string reasonCode, int retryCount, DateTimeOffset observedAt)
    {
        RetryPolicyDecision decision = RetryFailurePolicy.Classify(reasonCode, retryCount, observedAt);
        return decision.IsRetryable
            ? TenantExportClassStatuses.FailedRetryable
            : TenantExportClassStatuses.FailedTerminal;
    }
}
