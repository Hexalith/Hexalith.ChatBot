namespace Hexalith.ChatBot.Server.Audit;

internal interface IOperatorAlertSink
{
    ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken);
}
