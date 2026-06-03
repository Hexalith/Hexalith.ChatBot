using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Immutable command submission constructed once at the adapter boundary. <see cref="Origin"/> is the
/// surface that declared the command (FR85 / S7); because this record, <see cref="ChatBotGatewayContext"/>,
/// and the audit envelope are all immutable, origin is structurally un-rewritable by any downstream stage.
/// <para>
/// <see cref="ReplayRunId"/> (Story 9.4, FR95a) rides the <b>same</b> structurally-un-rewritable channel as
/// <see cref="Origin"/>: a replay/simulation run sets it once here at the boundary, and it travels unchanged into
/// <see cref="ChatBotGatewayContext"/> and the audit envelope (<c>AuditEnvelopeFactory.Create</c>), so no downstream
/// stage can forge, strip, or mutate it. A production submission leaves it <see langword="null"/> by omission — never
/// by an active "clear" step.
/// </para>
/// </summary>
internal sealed record ChatBotCommandSubmission(
    ClaimsPrincipal Principal,
    CommandSubmissionRequest Request,
    string CorrelationId,
    string? TaskId,
    ChatBotSurfaceOrigin Origin = ChatBotSurfaceOrigin.Api,
    string? ReplayRunId = null);
