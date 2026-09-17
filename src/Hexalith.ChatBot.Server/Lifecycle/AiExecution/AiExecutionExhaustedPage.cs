using System.Security.Claims;

using Hexalith.EventStore.Client.Queries;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed record AiExecutionExhaustedPage(
    IReadOnlyList<AiExecutionExhaustedRow> Items,
    string? NextCursor,
    bool HasMore,
    int PageSize);
