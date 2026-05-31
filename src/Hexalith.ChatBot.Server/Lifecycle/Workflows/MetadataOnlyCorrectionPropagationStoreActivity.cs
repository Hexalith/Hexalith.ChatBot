using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class MetadataOnlyCorrectionPropagationStoreActivity(
    string storeKey,
    ISystemClock clock) : ICorrectionPropagationStoreActivity
{
    public string StoreKey { get; } = string.IsNullOrWhiteSpace(storeKey)
        ? throw new ArgumentException("Store key is required.", nameof(storeKey))
        : storeKey;

    public ValueTask<CorrectionPropagationActivityResult> InvalidateAndRebuildAsync(
        CorrectionPropagationActivityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new CorrectionPropagationActivityResult(
            StoreKey,
            "success",
            null,
            clock.UtcNow));
    }
}
