using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record AssociationRoutingStatusQuery(string AssociationId, string? TaskId);
