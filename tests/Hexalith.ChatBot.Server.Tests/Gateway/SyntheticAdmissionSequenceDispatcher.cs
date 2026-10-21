using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Detects any dispatch past the unavailable production atomic gate.</summary>
internal sealed class SyntheticAdmissionSequenceDispatcher : ICommandDispatcher
{
    /// <summary>Number of attempted dispatches, including any forbidden attempt.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        Calls++;
        throw new InvalidOperationException("An unavailable atomic boundary must prevent dispatch.");
    }
}
