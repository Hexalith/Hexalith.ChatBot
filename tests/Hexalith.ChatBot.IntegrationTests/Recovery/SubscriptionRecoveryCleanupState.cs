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

    /// <summary>Detaches the active generation and replaces it with a fresh empty generation.</summary>
    /// <param name="activeState">The active generation field to replace.</param>
    /// <returns>The detached generation that cleanup must use exclusively.</returns>
    internal static SubscriptionRecoveryCleanupState DetachAndReset(
        ref SubscriptionRecoveryCleanupState activeState)
    {
        ArgumentNullException.ThrowIfNull(activeState);
        SubscriptionRecoveryCleanupState detached = activeState;
        activeState = new SubscriptionRecoveryCleanupState();
        return detached;
    }
}
