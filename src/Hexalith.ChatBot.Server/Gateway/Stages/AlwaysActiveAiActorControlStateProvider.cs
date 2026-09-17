using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IAiActorControlStateProvider"/> that always reports
/// <see cref="AiActorControlState.Active"/>. The durable projection feeding a disabled state is deferred per the
/// Story 7.12/7.15 read-side deferral; the validator seam is wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysActiveAiActorControlStateProvider : IAiActorControlStateProvider
{
    public ValueTask<AiActorControlState> GetControlStateAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(AiActorControlState.Active);
}
