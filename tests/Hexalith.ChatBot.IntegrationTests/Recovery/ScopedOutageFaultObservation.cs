namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Metadata-only observation captured when the application path records a dependency fault.</summary>
internal sealed record ScopedOutageFaultObservation(
    DateTimeOffset DependencyFailureObservedAtUtc,
    DateTimeOffset ScopeRecordedAtUtc,
    string ObservedScope,
    bool IndependentControlSucceeded,
    bool UnauthorizedMutationDetected)
{
    /// <summary>
    /// Gets a value indicating whether the independent-control probe was never answered (a client-side timeout)
    /// rather than answered with a non-<c>202</c>.
    /// </summary>
    /// <remarks>
    /// Both keep <see cref="IndependentControlSucceeded"/> false and both fail closed — nothing here may make a
    /// degraded control path readable as containment. The distinction exists so a failing run can say whether the
    /// containment evidence was negative or simply missing, which are different investigations.
    /// </remarks>
    public bool IndependentControlUnobserved { get; init; }

    /// <summary>
    /// Gets the stable, metadata-only cause of an unavailable control probe — <c>status-{code}</c>,
    /// <c>transport-{error}</c> or <c>client-timeout</c> — so a failing run names why the control path was
    /// unavailable instead of only that it was.
    /// </summary>
    public string? IndependentControlCause { get; init; }
}
