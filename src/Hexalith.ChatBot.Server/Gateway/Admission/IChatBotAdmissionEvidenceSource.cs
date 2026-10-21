namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>Trusted registry seam. Payload IDs, markers, policy decisions and model outputs supply no proof.</summary>
internal interface IChatBotAdmissionEvidenceSource
{
    /// <summary>Reads the current deployment binding independently from qualification evidence.</summary>
    ChatBotClassifierRuntimeIdentity? RuntimeIdentity { get; }

    /// <summary>Reads the independently validated immutable record and recomputed current gate status.</summary>
    ValueTask<ChatBotClassifierQualification?> ReadQualificationAsync(CancellationToken cancellationToken);

    /// <summary>Reads exact current owner proofs; no payload, marker or local policy approval can supply them.</summary>
    ValueTask<ChatBotProfileAdmissionEvidence?> ReadProfileEvidenceAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);
}
