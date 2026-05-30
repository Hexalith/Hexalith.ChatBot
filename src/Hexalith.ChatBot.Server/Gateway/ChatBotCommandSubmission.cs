using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotCommandSubmission(
    ClaimsPrincipal Principal,
    CommandSubmissionRequest Request,
    string CorrelationId,
    string? TaskId);
