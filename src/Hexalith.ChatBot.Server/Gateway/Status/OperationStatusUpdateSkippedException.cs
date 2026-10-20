namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>Stops the SDK transform before a write when a caller deliberately leaves the record unchanged.</summary>
internal sealed class OperationStatusUpdateSkippedException : Exception;
