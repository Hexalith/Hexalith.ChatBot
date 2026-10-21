using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>
/// Synthetic-only legacy reducer/transport fixture. Permits the old harness to exercise its downstream
/// state and replay mechanics; this is neither qualification evidence nor accepted owner atomic participation.
/// Production admission tests explicitly use ChatBotAdmissionEvidenceValidator instead.
/// </summary>
internal sealed class SyntheticLegacyAdmissionEvidenceValidator(Action? beforeProfile = null) : IChatBotAdmissionEvidenceValidator
{
    /// <inheritdoc />
    public ValueTask<string?> ValidateOperationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);
    /// <inheritdoc />
    public ValueTask<string?> ValidateClassificationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult<string?>(null);
    /// <inheritdoc />
    public ValueTask<string?> ValidateProfileAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        beforeProfile?.Invoke();
        return ValueTask.FromResult<string?>(null);
    }
    /// <inheritdoc />
    public ValueTask<string?> ValidateAtomicParticipationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult<string?>(context.Submission.Request.Concurrency is null ? null : ChatBotMessageCodes.CanonicalExecutionUnavailable);
}
