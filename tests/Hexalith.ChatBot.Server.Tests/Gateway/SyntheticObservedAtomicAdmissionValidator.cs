using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Observes the real validator with explicitly synthetic proofs while retaining production atomic denial.</summary>
internal sealed class SyntheticObservedAtomicAdmissionValidator(IChatBotAdmissionEvidenceValidator inner) : IChatBotAdmissionEvidenceValidator
{
    /// <summary>Number of operation validations attempted.</summary>
    public int OperationCalls { get; private set; }
    /// <summary>Failure returned by the operation validator, if any.</summary>
    public string? OperationFailure { get; private set; }
    /// <summary>Number of classification validations attempted.</summary>
    public int ClassificationCalls { get; private set; }
    /// <summary>Failure returned by the classification validator, if any.</summary>
    public string? ClassificationFailure { get; private set; }
    /// <summary>Number of profile validations attempted.</summary>
    public int ProfileCalls { get; private set; }
    /// <summary>Number of atomic participation validations attempted.</summary>
    public int AtomicCalls { get; private set; }
    /// <summary>The operation context presented to the real atomic boundary.</summary>
    public ChatBotGatewayContext? AtomicContext { get; private set; }

    /// <inheritdoc />
    public async ValueTask<string?> ValidateOperationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        OperationCalls++;
        OperationFailure = await inner.ValidateOperationAsync(context, cancellationToken).ConfigureAwait(false);
        return OperationFailure;
    }

    /// <inheritdoc />
    public async ValueTask<string?> ValidateClassificationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ClassificationCalls++;
        ClassificationFailure = await inner.ValidateClassificationAsync(context, cancellationToken).ConfigureAwait(false);
        return ClassificationFailure;
    }

    /// <inheritdoc />
    public ValueTask<string?> ValidateProfileAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ProfileCalls++;
        return inner.ValidateProfileAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<string?> ValidateAtomicParticipationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        AtomicCalls++;
        AtomicContext = context;
        return inner.ValidateAtomicParticipationAsync(context, cancellationToken);
    }
}
