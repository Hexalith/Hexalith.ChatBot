using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record GovernedOperationQuery(string NoteId, string? TaskId);
