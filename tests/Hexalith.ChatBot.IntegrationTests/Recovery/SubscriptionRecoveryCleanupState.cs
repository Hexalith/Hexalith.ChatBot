using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tracks one subscription or controlled-loss recovery scenario generation until cleanup detaches it.</summary>
internal sealed class SubscriptionRecoveryCleanupState
{
    /// <summary>Gets or sets the affected-tenant sentinel owned by the generation.</summary>
    internal ProjectConversationSourceEmailView? AffectedTenantSentinel { get; set; }

    /// <summary>Gets or sets the control-tenant sentinel owned by the generation.</summary>
    internal ProjectConversationSourceEmailView? ControlTenantSentinel { get; set; }

    /// <summary>Gets or sets the committed-before-outage subscription intake reference.</summary>
    internal string? SubscriptionCheckpointIntakeRef { get; set; }

    /// <summary>Gets or sets the intake reconciled after subscription restoration.</summary>
    internal string? ReconciledIntakeRef { get; set; }

    /// <summary>Gets or sets the duplicate-probe intake reference.</summary>
    internal string? DuplicateProbeIntakeRef { get; set; }

    /// <summary>Gets or sets the controlled-loss pre-fault intake reference.</summary>
    internal string? ControlledPreFaultIntakeRef { get; set; }

    /// <summary>Gets or sets the controlled-loss rejected candidate reference.</summary>
    internal string? ControlledRejectedCandidateRef { get; set; }

    /// <summary>Gets or sets the controlled-loss post-recovery intake reference.</summary>
    internal string? ControlledPostRecoveryIntakeRef { get; set; }

    /// <summary>Gets or sets whether controlled-loss sentinels stayed unchanged during the fault.</summary>
    internal bool ControlledFaultLeftStateUnchanged { get; set; }

    /// <summary>Gets or sets whether subscription sentinels stayed unchanged during the fault.</summary>
    internal bool SubscriptionFaultLeftStateUnchanged { get; set; }

    /// <summary>Gets every distinct canonical intake or candidate identity exposed by a producer response.</summary>
    internal HashSet<string> StorageIntakeRefs { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets identities whose storage-tenant aggregate must remain absent after cleanup.</summary>
    internal HashSet<string> StorageDurableAbsenceRefs { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets identities whose control-tenant aggregate and read models must remain absent.</summary>
    internal HashSet<string> ControlTenantAbsenceRefs { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets whether this generation owns state that cleanup must handle.</summary>
    internal bool HasOwnedState => AffectedTenantSentinel is not null ||
        ControlTenantSentinel is not null ||
        StorageIntakeRefs.Count > 0 ||
        ControlTenantAbsenceRefs.Count > 0;

    /// <summary>Detaches the active generation and replaces it with a fresh empty generation.</summary>
    /// <param name="activeState">The active generation field to replace.</param>
    /// <returns>The detached generation that cleanup must use exclusively.</returns>
    internal static SubscriptionRecoveryCleanupState DetachAndReset(ref SubscriptionRecoveryCleanupState activeState)
    {
        ArgumentNullException.ThrowIfNull(activeState);
        SubscriptionRecoveryCleanupState detached = activeState;
        activeState = new SubscriptionRecoveryCleanupState();
        return detached;
    }
}
