using System.Reflection;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Satisfies client injection for static render tests and fails any call that would reach the API.</summary>
public class RenderOnlyChatBotClient : DispatchProxy
{
    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        => throw new InvalidOperationException($"Static render tests must not call {targetMethod?.Name}.");
}
