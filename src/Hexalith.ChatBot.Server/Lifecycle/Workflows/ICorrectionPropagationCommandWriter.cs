namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationCommandWriter
{
    ValueTask SubmitAsync<TCommand>(
        CorrectionPropagationRequest request,
        string commandType,
        TCommand command,
        CancellationToken cancellationToken);
}
