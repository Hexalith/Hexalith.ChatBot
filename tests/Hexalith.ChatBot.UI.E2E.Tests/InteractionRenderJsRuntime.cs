using Microsoft.JSInterop;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Supplies the no-op browser seam for mounted component lifecycle tests.</summary>
internal sealed class InteractionRenderJsRuntime : IJSRuntime
{
    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);

    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);
}
