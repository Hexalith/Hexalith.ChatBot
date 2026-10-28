using Microsoft.JSInterop;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Supplies the inert JS runtime needed for static Fluent component rendering.</summary>
internal sealed class AssociationRenderJsRuntime : IJSRuntime
{
    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);
}
