using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record TaskIntentReviewQuery(
    string ProjectId,
    string TaskIntentId,
    bool ProjectReadAuthorized,
    string? TaskId);
