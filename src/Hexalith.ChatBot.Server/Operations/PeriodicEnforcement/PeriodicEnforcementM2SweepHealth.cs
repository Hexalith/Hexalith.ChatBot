namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

/// <summary>The per-sweep view published on the authenticated M2 release-gate endpoint.</summary>
internal sealed record PeriodicEnforcementM2SweepHealth(
    DateTimeOffset? LastRanAtUtc,
    DateTimeOffset? LastSucceededAtUtc,
    bool HasBreaches,
    bool HasCoverage);
