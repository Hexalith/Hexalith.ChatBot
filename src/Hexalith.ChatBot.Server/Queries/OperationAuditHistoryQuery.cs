using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record OperationAuditHistoryQuery(string OperationId, string? TaskId);
