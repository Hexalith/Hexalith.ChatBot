using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Supplies the no-op browser seam for mounted component lifecycle tests.</summary>
internal sealed class InteractionRenderJsRuntime : IJSRuntime
{
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    /// <summary>Gets the elements the mounted components asked the browser to focus, in call order.</summary>
    internal List<ElementReference> FocusedElements { get; } = [];

    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (string.Equals(identifier, FocusIdentifier, StringComparison.Ordinal) && args is [ElementReference element, ..])
        {
            FocusedElements.Add(element);
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (string.Equals(identifier, FocusIdentifier, StringComparison.Ordinal) && args is [ElementReference element, ..])
        {
            FocusedElements.Add(element);
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}
