using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// A governed-operation read-model store whose first writes fail, so a projection fault reaches the delivery route.
/// </summary>
/// <param name="failures">The number of writes to fail before delegating to an in-memory store.</param>
internal sealed class FaultingGovernedOperationProjectionStore(int failures) : IGovernedOperationProjectionStore
{
    private readonly InMemoryGovernedOperationProjectionStore _inner = new();
    private int _remainingFailures = failures;
    private int _saveAttempts;

    /// <summary>Gets the number of write attempts, including the failed ones.</summary>
    public int SaveAttempts => Volatile.Read(ref _saveAttempts);

    /// <inheritdoc/>
    public Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
        => _inner.GetAsync(tenantId, noteId, cancellationToken);

    /// <inheritdoc/>
    public Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
    {
        _ = Interlocked.Increment(ref _saveAttempts);
        return Interlocked.Decrement(ref _remainingFailures) >= 0
            ? throw new InvalidOperationException("Simulated governed-operation projection store fault.")
            : _inner.SaveAsync(view, cancellationToken);
    }
}
