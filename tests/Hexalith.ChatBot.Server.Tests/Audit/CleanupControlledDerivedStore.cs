using Hexalith.ChatBot.Server.Projections.DerivedStores;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Exercises cleanup deadlines and cancellation against a real in-memory sentinel store.</summary>
internal sealed class CleanupControlledDerivedStore : IDerivedStore
{
    private readonly InMemoryDerivedStore _inner = new();
    private readonly TaskCompletionSource _firstCleanupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _ownerTenant;

    /// <summary>Gets or initializes the asynchronous work performed before an actual invalidation.</summary>
    public Func<CancellationToken, Task>? BeforeCleanup { get; set; }

    /// <summary>Gets or initializes work performed before seeding a class.</summary>
    public Action<DerivedStoreClass>? BeforeSeed { get; init; }

    /// <summary>Gets or initializes the callback performed before the probe read.</summary>
    public Action? BeforeRead { get; init; }

    /// <summary>Gets or sets asynchronous coordination before a probe reads its sentinel.</summary>
    public Func<CancellationToken, Task>? BeforeReadAsync { get; set; }

    /// <summary>Gets or sets whether reads intentionally ignore the tenant partition.</summary>
    public bool LeakReads { get; set; }

    /// <summary>Gets or initializes whether deletion itself ignores a cancelled cleanup token.</summary>
    public bool IgnoreCleanupCancellation { get; init; }

    /// <summary>Gets when an invalidation has actually finished mutating the backing store.</summary>
    public Task FirstCleanupCompleted => _firstCleanupCompleted.Task;

    /// <summary>Gets every cleanup token so tests can verify independence and total-budget exhaustion.</summary>
    public List<CancellationToken> CleanupTokens { get; } = [];

    /// <inheritdoc />
    public ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken)
    {
        BeforeSeed?.Invoke(cls);
        _ownerTenant ??= tenantId;
        return _inner.PutAsync(cls, tenantId, resourceId, entry, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
    {
        BeforeRead?.Invoke();
        if (BeforeReadAsync is not null)
        {
            await BeforeReadAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await _inner.GetAsync(cls, LeakReads ? _ownerTenant! : tenantId, resourceId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId)
        => _inner.EnumerateResourceIds(cls, tenantId);

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateTenants() => _inner.EnumerateTenants();

    /// <inheritdoc />
    public async ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
    {
        CleanupTokens.Add(cancellationToken);
        if (BeforeCleanup is not null)
        {
            await BeforeCleanup(cancellationToken).ConfigureAwait(false);
        }

        bool removed = await _inner.InvalidateAsync(
            cls, tenantId, resourceId, IgnoreCleanupCancellation ? CancellationToken.None : cancellationToken).ConfigureAwait(false);
        _firstCleanupCompleted.TrySetResult();
        return removed;
    }
}
