using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record OperationStatusQuery(string OperationId, string? TaskId);
