namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>Internal evidence boundary; transport claims never implement an admission check.</summary>
internal interface IChatBotAdmissionEvidenceValidator
{
    /// <summary>Validates stable identity, concurrency and prohibited caller selectors without writing state.</summary>
    ValueTask<string?> ValidateOperationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);

    /// <summary>Requires current exact-artifact qualification before trusting any classifier result.</summary>
    ValueTask<string?> ValidateClassificationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);

    /// <summary>Validates the selected profile's owner evidence before any bookkeeping or effect.</summary>
    ValueTask<string?> ValidateProfileAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);

    /// <summary>Requires the accepted owner commit unit, including bookkeeping, before reservation or dispatch.</summary>
    ValueTask<string?> ValidateAtomicParticipationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);
}
