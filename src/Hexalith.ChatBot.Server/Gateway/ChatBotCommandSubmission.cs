using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Immutable command submission constructed once at the adapter boundary. <see cref="Origin"/> is the
/// surface that declared the command (FR85 / S7); because this record, <see cref="ChatBotGatewayContext"/>,
/// and the audit envelope are all immutable, origin is structurally un-rewritable by any downstream stage.
/// </summary>
internal sealed record ChatBotCommandSubmission(
    ClaimsPrincipal Principal,
    CommandSubmissionRequest Request,
    string CorrelationId,
    string? TaskId,
    ChatBotSurfaceOrigin Origin = ChatBotSurfaceOrigin.Api);
