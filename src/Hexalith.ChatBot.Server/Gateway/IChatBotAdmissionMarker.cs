using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.ChatBot.Server.Gateway;

internal interface IChatBotAdmissionMarker
{
    string Create(
        string messageId,
        string tenantId,
        string aggregateId,
        string commandType,
        JsonElement payload,
        string correlationId,
        string actorId,
        string surfaceOrigin,
        string? taskId);

    bool IsValid(CommandEnvelope command);
}
