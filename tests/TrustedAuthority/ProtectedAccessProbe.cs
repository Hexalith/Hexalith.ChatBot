using System.Reflection;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Counts any attempted protected access and fails the fixture if the authorization boundary is crossed.</summary>
internal class ProtectedAccessProbe : DispatchProxy
{
    /// <summary>The number of protected store calls.</summary>
    public int Calls { get; private set; }
    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Calls++;
        throw new InvalidOperationException("Protected access was not authorized.");
    }
}
