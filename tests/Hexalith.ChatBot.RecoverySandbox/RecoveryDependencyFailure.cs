namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Metadata-only failure signal emitted by an exercised dependency boundary.</summary>
/// <param name="Dependency">The dependency token that failed.</param>
/// <param name="CorrelationId">The exercised operation's correlation id.</param>
/// <param name="ObservedAtUtc">When the real failing component observed the fault.</param>
/// <param name="FaultSignalCode">
/// The real reason/error code the failing component itself returned (e.g. <c>ai_provider_unavailable</c>,
/// <c>audit_unavailable</c>, <c>graph_subscription_expired</c>) — independent of <paramref name="Dependency"/>,
/// so the monitor's scope mapping cannot be a byte-identical copy of the injector's own expectation table.
/// </param>
internal sealed record RecoveryDependencyFailure(
    string Dependency,
    string CorrelationId,
    DateTimeOffset ObservedAtUtc,
    string FaultSignalCode);
