namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>No currently accepted A9a registry is available; local configuration cannot close that gate.</summary>
internal sealed class UnavailableChatBotAdmissionEvidenceSource : IChatBotAdmissionEvidenceSource
{
    /// <inheritdoc />
    public ChatBotClassifierRuntimeIdentity? RuntimeIdentity => null;

    /// <inheritdoc />
    public ValueTask<ChatBotClassifierQualification?> ReadQualificationAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<ChatBotClassifierQualification?>(null);
    }

    /// <inheritdoc />
    public ValueTask<ChatBotProfileAdmissionEvidence?> ReadProfileEvidenceAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<ChatBotProfileAdmissionEvidence?>(null);
    }
}
