using System.Security.Claims;

using Hexalith.EventStore.Client.Queries;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed record AiExecutionRecoveryRequest(string Key);
