using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Http;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Builds the closed provider-message identities used by recovery checkpoint and replay exercises.</summary>
internal static class RecoveryNotificationIdentity
{
    /// <summary>The ordinary subscription-continuity lane.</summary>
    public const string ContinuityLane = "continuity";

    /// <summary>The scoped Graph-outage lane.</summary>
    public const string GraphLane = "graph";

    /// <summary>The distinct controlled-loss evidence lane.</summary>
    public const string ControlledLossLane = "controlled-loss";

    public const string HeaderName = "X-Recovery-Notification-Phase";

    /// <summary>The closed scenario selector supplied by the controller.</summary>
    public const string ScenarioLaneHeaderName = "X-Recovery-Scenario-Lane";
    public const string CheckpointPhase = "checkpoint";
    public const string RecoveryPhase = "recovery";
    public const string PreFaultPhase = "pre-fault";
    public const string LossPhase = "loss";
    public const string PostRecoveryPhase = "post-recovery";

    /// <summary>Parses the endpoint's lane/phase headers without accepting missing, repeated or cross-lane values.</summary>
    /// <param name="headers">The incoming subscription request headers.</param>
    /// <param name="providerMessageId">The configured provider identity.</param>
    /// <param name="identity">The composed identity, or null for refused input.</param>
    /// <returns>Whether the headers identify one supported scenario and phase.</returns>
    public static bool TryComposeFromHeaders(IHeaderDictionary headers, string providerMessageId, [NotNullWhen(true)] out string? identity)
    {
        identity = null;
        if (headers[ScenarioLaneHeaderName].Count != 1 || headers[HeaderName].Count != 1)
        {
            return false;
        }

        try
        {
            identity = Compose(providerMessageId, headers[ScenarioLaneHeaderName].ToString(), headers[HeaderName].ToString());
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Returns a stable identity per scenario lane and phase, rejecting open-ended controller input.</summary>
    public static string Compose(string providerMessageId, string scenarioLane, string phase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerMessageId);
        bool allowed = (scenarioLane, phase) switch
        {
            (ContinuityLane, CheckpointPhase or RecoveryPhase) => true,
            (GraphLane, RecoveryPhase) => true,
            (ControlledLossLane, PreFaultPhase or LossPhase or PostRecoveryPhase) => true,
            _ => false,
        };
        if (!allowed)
        {
            throw new InvalidOperationException("The recovery notification lane or phase is outside the closed sandbox contract.");
        }

        return $"{providerMessageId}-{scenarioLane}-{phase}";
    }

    /// <summary>Returns whether an identity belongs to the one closed controlled-loss fault-window candidate.</summary>
    public static bool IsControlledLossCandidate(string providerMessageId)
        => providerMessageId.EndsWith($"-{ControlledLossLane}-{LossPhase}", StringComparison.Ordinal);
}
