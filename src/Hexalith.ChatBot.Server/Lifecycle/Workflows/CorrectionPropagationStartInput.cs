namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationStartInput(
    CorrectionPropagationRequest Request,
    IReadOnlyList<string> Scope);
