namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationDelayInput(
    CorrectionPropagationRequest Request,
    string ReasonCode);
