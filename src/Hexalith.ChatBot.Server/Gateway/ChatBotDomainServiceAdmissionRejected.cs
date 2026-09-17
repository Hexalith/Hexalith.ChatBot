using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Metadata-only rejection emitted when ChatBot command admission denies an SDK <c>/process</c> request.
/// </summary>
/// <param name="CommandId">The safe command identifier when available.</param>
/// <param name="CommandType">The safe command type name.</param>
/// <param name="ReasonCode">The finite admission reason code.</param>
/// <param name="CorrelationId">The safe correlation identifier when available.</param>
public sealed record ChatBotDomainServiceAdmissionRejected(
    string? CommandId,
    string CommandType,
    string ReasonCode,
    string? CorrelationId) : IRejectionEvent;
