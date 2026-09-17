using System.Globalization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using CommandSubmissionResponse = Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse;

namespace Hexalith.ChatBot.UI.Services;

public sealed record ComplianceCommandResult(string CommandId, string CorrelationId, string? TaskId);
