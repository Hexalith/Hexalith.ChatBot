using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The candidate scope tokens for a degraded/failed dependency, in NFR41 precedence order (narrowest first). Any
/// component the signal genuinely lacks is left <see langword="null"/>; the resolver picks the narrowest present.
/// </summary>
internal sealed record ScopeCandidates(
    string? WorkflowItemRef = null,
    string? OperationRef = null,
    string? CommandSurfaceRef = null,
    string? ServiceClientRef = null,
    string? ProjectRef = null,
    string? MailboxRef = null,
    string? TenantRef = null);

/// <summary>
/// Produces the single metadata-only <see cref="DegradedDependencyIncident"/> for a degraded/failed dependency
/// signal (NFR41). It fires exactly one incident for <see cref="ChatBotHealthStatus.Degraded"/>/
/// <see cref="ChatBotHealthStatus.Failed"/>, carrying the resolved narrowest scope, the fixed 300s detection
/// budget, and a deterministic owner-role/next-action when the caller omits them; it returns <see langword="null"/>
/// for <see cref="ChatBotHealthStatus.Healthy"/>/<see cref="ChatBotHealthStatus.Unknown"/> — never fabricating a
/// degraded incident from a healthy/no-data signal. Pure: the only clock input is the passed detection instant.
/// </summary>
internal static class DegradedDependencyIncidentFactory
{
    public const string DefaultOwnerRole = "operations-admin";
    public const string DefaultNextSafeAction = "escalate-to-operations";

    // Mirrors the RetryFailurePolicy reason->owner-role mapping shape; deterministic, no process-state hashing.
    private static readonly Dictionary<string, string> OwnerRoleByReason = new(StringComparer.Ordinal)
    {
        ["graph_subscription_expired"] = "mailbox-admin",
        ["graph_token_expired"] = "mailbox-admin",
        ["graph_permission_revoked"] = "tenant-admin",
        ["graph_scope_mismatch"] = "tenant-admin",
        ["degraded_mailbox"] = "mailbox-admin",
        ["recoverable_mailbox_degradation"] = "mailbox-admin",
    };

    public static DegradedDependencyIncident? Create(
        string dependencyId,
        ChatBotHealthStatus health,
        ScopeCandidates candidates,
        string reasonCode,
        string? ownerRole,
        string? nextSafeAction,
        string correlationId,
        DateTimeOffset detectedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (health is not (ChatBotHealthStatus.Degraded or ChatBotHealthStatus.Failed))
        {
            // Healthy/Unknown signals never fabricate a degraded incident.
            return null;
        }

        (DependencyScopeKind scopeKind, string affectedScope) = DependencyScopeResolver.Resolve(
            candidates.WorkflowItemRef,
            candidates.OperationRef,
            candidates.CommandSurfaceRef,
            candidates.ServiceClientRef,
            candidates.ProjectRef,
            candidates.MailboxRef,
            candidates.TenantRef);

        return new DegradedDependencyIncident(
            dependencyId,
            scopeKind,
            affectedScope,
            health,
            detectedAtUtc.ToUniversalTime(),
            DegradedDependencyContractValidator.DefaultDetectionBudgetSeconds,
            ResolveOwnerRole(ownerRole, reasonCode),
            string.IsNullOrWhiteSpace(nextSafeAction) ? DefaultNextSafeAction : nextSafeAction,
            reasonCode,
            correlationId);
    }

    private static string ResolveOwnerRole(string? ownerRole, string reasonCode)
        => !string.IsNullOrWhiteSpace(ownerRole)
            ? ownerRole
            : OwnerRoleByReason.GetValueOrDefault(reasonCode, DefaultOwnerRole);
}
