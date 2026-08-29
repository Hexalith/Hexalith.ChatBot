using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tracks one scoped-outage recovery scenario generation until cleanup detaches it.</summary>
internal sealed class ScopedOutageRecoveryCleanupState
{
    /// <summary>Gets or sets the affected-tenant Graph sentinel owned by the generation.</summary>
    internal ProjectConversationSourceEmailView? GraphAffectedSentinel { get; set; }

    /// <summary>Gets or sets the control-tenant Graph sentinel owned by the generation.</summary>
    internal ProjectConversationSourceEmailView? GraphControlSentinel { get; set; }

    /// <summary>Gets or sets the Graph intake recovered after restoration.</summary>
    internal string? GraphRecoveredIntakeRef { get; set; }

    /// <summary>Gets or sets the Graph duplicate-probe intake reference.</summary>
    internal string? GraphDuplicateProbeIntakeRef { get; set; }

    /// <summary>Gets or sets whether Graph sentinels stayed unchanged during the fault.</summary>
    internal bool GraphFaultLeftStateUnchanged { get; set; }

    /// <summary>Gets or sets the affected-tenant identity sentinel owned by the generation.</summary>
    internal ProjectConversationSourceEmailView? IdentityAffectedSentinel { get; set; }

    /// <summary>Gets or sets the control-tenant identity sentinel owned by the generation.</summary>
    internal ProjectConversationSourceEmailView? IdentityControlSentinel { get; set; }

    /// <summary>Gets or sets whether identity sentinels stayed unchanged during the fault.</summary>
    internal bool IdentityFaultLeftStateUnchanged { get; set; }

    /// <summary>Gets every distinct canonical Graph intake or candidate identity exposed by a producer response.</summary>
    internal HashSet<string> GraphIntakeRefs { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets Graph identities whose storage-tenant aggregate must remain absent.</summary>
    internal HashSet<string> GraphDurableAbsenceRefs { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the independent control-operation notes owned by the generation.</summary>
    internal List<string> ControlOperationRefs { get; } = [];

    /// <summary>Gets whether this generation owns state that cleanup must handle.</summary>
    internal bool HasOwnedState => GraphAffectedSentinel is not null ||
        GraphControlSentinel is not null ||
        IdentityAffectedSentinel is not null ||
        IdentityControlSentinel is not null ||
        GraphIntakeRefs.Count > 0 ||
        ControlOperationRefs.Count > 0;

    /// <summary>Detaches the active generation and replaces it with a fresh empty generation.</summary>
    /// <param name="activeState">The active generation field to replace.</param>
    /// <returns>The detached generation that cleanup must use exclusively.</returns>
    internal static ScopedOutageRecoveryCleanupState DetachAndReset(ref ScopedOutageRecoveryCleanupState activeState)
    {
        ArgumentNullException.ThrowIfNull(activeState);
        ScopedOutageRecoveryCleanupState detached = activeState;
        activeState = new ScopedOutageRecoveryCleanupState();
        return detached;
    }
}
