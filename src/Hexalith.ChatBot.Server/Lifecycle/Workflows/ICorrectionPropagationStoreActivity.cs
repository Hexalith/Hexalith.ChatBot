namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationStoreActivity
{
    string StoreKey { get; }

    ValueTask<CorrectionPropagationActivityResult> InvalidateAndRebuildAsync(
        CorrectionPropagationActivityRequest request,
        CancellationToken cancellationToken);
}
