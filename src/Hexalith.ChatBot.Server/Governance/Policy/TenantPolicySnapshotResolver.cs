using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Authentication;

namespace Hexalith.ChatBot.Server.Governance.Policy;

/// <summary>Resolves exact references in synthetic history only; it is never a live policy provider.</summary>
internal static class TenantPolicySnapshotResolver
{
    /// <summary>Denies missing, malformed, foreign, unauthorized, future, stale, or ambiguous synthetic references.</summary>
    public static ImmutableTenantPolicySnapshot? Resolve(
        IEnumerable<TenantPolicySnapshotHistory> histories, ChatBotRequestContext context,
        string? snapshotId, long policyVersion, bool isAuthorized, DateTimeOffset evidenceObservedAtUtc, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(histories);
        ArgumentNullException.ThrowIfNull(context);
        if (!isAuthorized || !context.Principal.Identities.Any(static identity => identity.IsAuthenticated) ||
            !TenantPolicySchema.IsSafePolicyToken(context.TenantId) || !TenantPolicySchema.IsSafePolicyToken(snapshotId) ||
            policyVersion < 1 || evidenceObservedAtUtc.Offset != TimeSpan.Zero || nowUtc.Offset != TimeSpan.Zero ||
            evidenceObservedAtUtc > nowUtc || nowUtc - evidenceObservedAtUtc > TimeSpan.FromMinutes(5))
        {
            return null;
        }

        ImmutableTenantPolicySnapshot[] matches = histories.SelectMany(static history => history.Snapshots)
            .Where(snapshot => snapshot.TenantId == context.TenantId && snapshot.SnapshotId == snapshotId &&
                snapshot.PolicyVersion == policyVersion && snapshot.EffectiveAtUtc <= nowUtc).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
