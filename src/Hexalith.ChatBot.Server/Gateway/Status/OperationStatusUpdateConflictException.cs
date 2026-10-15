namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>A metadata-only rejection of an incompatible canonical status update.</summary>
internal sealed class OperationStatusUpdateConflictException() : InvalidOperationException("operation_status_conflict");
