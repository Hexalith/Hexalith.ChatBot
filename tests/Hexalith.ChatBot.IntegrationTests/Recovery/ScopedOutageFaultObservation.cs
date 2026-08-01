namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Metadata-only observation captured when the application path records a dependency fault.</summary>
internal sealed record ScopedOutageFaultObservation(
    DateTimeOffset DependencyFailureObservedAtUtc,
    DateTimeOffset ScopeRecordedAtUtc,
    string ObservedScope,
    bool IndependentControlSucceeded,
    bool UnauthorizedMutationDetected);
