using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Admin;
using Hexalith.ChatBot.Server.Lifecycle.Retry;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.9 (AC2, NFR2): the ONLY place a <see cref="ClaimsPrincipal"/> touches the deletion/erasure decision. It
/// gates on the human compliance-admin scope and projects the requester's <b>actual</b> per-project owner grants into
/// the bounded <see cref="DeletionErasureAuthorityView"/> the pure <see cref="DeletionErasurePlanner"/> consumes —
/// mirroring <see cref="TenantExportAuthorizationPolicy.AuthorityFor"/>. No second authority path is introduced.
/// Destruction stays fail-closed: a project ref absent from the bounded view collapses to <c>retained</c>/<c>unauthorized</c>
/// in the planner, never to a destructive action.
/// </summary>
internal static class DeletionErasureAuthorizationPolicy
{
    public static bool CanRequestDeletionErasure(ClaimsPrincipal principal)
        => AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance);

    public static DeletionErasureAuthorityView AuthorityFor(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        HashSet<string> grantedProjects = principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .ToHashSet(StringComparer.Ordinal);

        return new DeletionErasureAuthorityView(CanRequestDeletionErasure(principal), grantedProjects);
    }
}

/// <summary>
/// Story 9.9 (AC4, NFR17/NFR18): maps a per-class destruction failure to a bounded
/// <see cref="DeletionErasureClassStatuses"/> token via the ONE retry taxonomy (<see cref="RetryFailurePolicy"/>) —
/// a retryable decision is <c>failed-retryable</c>, a terminal/exhausted decision is <c>failed-terminal</c>. No
/// second retryable-vs-terminal classifier is introduced. (The byte-destroying runtime that calls this is the
/// deferred surface; the classification seam itself ships now.)
/// </summary>
internal static class DeletionErasureFailureClassifier
{
    public static string ClassifyClassStatus(string reasonCode, int retryCount, DateTimeOffset observedAt)
    {
        RetryPolicyDecision decision = RetryFailurePolicy.Classify(reasonCode, retryCount, observedAt);
        return decision.IsRetryable
            ? DeletionErasureClassStatuses.FailedRetryable
            : DeletionErasureClassStatuses.FailedTerminal;
    }
}
