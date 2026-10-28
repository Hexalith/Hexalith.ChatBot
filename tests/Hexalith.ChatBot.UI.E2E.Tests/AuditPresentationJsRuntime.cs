using Microsoft.JSInterop;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Provides the inert JavaScript boundary for static audit presentation renders.</summary>
internal sealed class AuditPresentationJsRuntime : IJSRuntime
{
    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);

    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);
}
