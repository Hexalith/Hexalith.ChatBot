using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ServiceClientGrantResolution(ServiceClientGrant? Grant, string ReasonCode)
{
    public bool IsResolved => Grant is not null;

    public static ServiceClientGrantResolution Resolved(ServiceClientGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        return new ServiceClientGrantResolution(grant, string.Empty);
    }

    public static ServiceClientGrantResolution Denied(string reasonCode)
        => new(null, reasonCode);
}
