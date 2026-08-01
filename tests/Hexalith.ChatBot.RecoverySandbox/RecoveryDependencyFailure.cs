namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Metadata-only failure signal emitted by an exercised dependency boundary.</summary>
internal sealed record RecoveryDependencyFailure(
    string Dependency,
    string CorrelationId,
    DateTimeOffset ObservedAtUtc);
