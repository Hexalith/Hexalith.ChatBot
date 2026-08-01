namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>One independently recorded dependency-scope observation.</summary>
internal sealed record RecoveryScopeObservation(
    string Dependency,
    string CorrelationId,
    string ObservedScope,
    DateTimeOffset DependencyFailureObservedAtUtc,
    DateTimeOffset ScopeRecordedAtUtc);
