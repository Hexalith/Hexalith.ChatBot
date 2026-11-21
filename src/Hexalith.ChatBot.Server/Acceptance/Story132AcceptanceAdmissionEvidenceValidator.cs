using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

namespace Hexalith.ChatBot.Server.Acceptance;

/// <summary>
/// Development/Testing-only evidence validator that lets Story 13.2 Message/Ask-AI/Stop admit through CommandGateway
/// without claiming production A9a/A13 atomic participation. Production must never register this type.
/// </summary>
internal sealed class Story132AcceptanceAdmissionEvidenceValidator : IChatBotAdmissionEvidenceValidator
{
    public ValueTask<string?> ValidateOperationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(null);
    }

    public ValueTask<string?> ValidateClassificationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(null);
    }

    public ValueTask<string?> ValidateProfileAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(null);
    }

    public ValueTask<string?> ValidateAtomicParticipationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(null);
    }
}
