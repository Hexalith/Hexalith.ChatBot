namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>Identifies malformed caller data detected while planning a command dispatch.</summary>
internal sealed class CommandDispatchValidationException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
