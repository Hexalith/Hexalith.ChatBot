using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Records each fed tenant in feed order and reports no readings; the fed timestamp is not kept.</summary>
internal sealed class BoundaryAuthorizationFailureCounter : IAuthorizationFailureCounter
{
    private readonly Lock _gate = new();
    private readonly List<string> _tenants = [];

    /// <summary>Gets the recorded tenant values, in recording order.</summary>
    public IReadOnlyList<string> Tenants
    {
        get
        {
            lock (_gate)
            {
                return [.. _tenants];
            }
        }
    }

    /// <inheritdoc />
    public void Record(string tenantId, DateTimeOffset timestamp)
    {
        lock (_gate)
        {
            _tenants.Add(tenantId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AuthorizationFailureReading> ReadAndReset() => [];
}
