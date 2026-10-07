using System.Reflection;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Injects authority changes after real asynchronous store boundaries complete.</summary>
internal class TrustedAuthorityBoundaryProxy : DispatchProxy
{
    private object _inner = null!;
    private Action<string> _after = null!;

    /// <summary>Records the protected calls made through the real implementation.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>Wraps an existing store without replacing its state or read behavior.</summary>
    public static T Create<T>(T inner, Action<string> after) where T : class
    {
        T wrapped = Create<T, TrustedAuthorityBoundaryProxy>();
        TrustedAuthorityBoundaryProxy proxy = (TrustedAuthorityBoundaryProxy)(object)wrapped;
        proxy._inner = inner;
        proxy._after = after;
        return wrapped;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod!;
        Calls.Add(method.Name);
        object? result = method.Invoke(_inner, args);
        Type type = method.ReturnType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return GetType().BaseType!.GetMethod(nameof(CompleteTask), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(this, [result, method.Name]);
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            return GetType().BaseType!.GetMethod(nameof(CompleteValueTask), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(this, [result, method.Name]);
        }
        _after(method.Name);
        return result;
    }

    private async Task<T> CompleteTask<T>(Task<T> task, string method)
    {
        T result = await task.ConfigureAwait(false);
        await Task.Yield();
        _after(method);
        return result;
    }

    private async ValueTask<T> CompleteValueTask<T>(ValueTask<T> task, string method)
    {
        T result = await task.ConfigureAwait(false);
        await Task.Yield();
        _after(method);
        return result;
    }
}
