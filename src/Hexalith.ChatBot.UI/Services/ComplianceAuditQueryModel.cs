using System.Globalization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using CommandSubmissionResponse = Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>The FR56 query dimensions captured by the surface's labelled filter controls.</summary>
public sealed record ComplianceAuditQueryModel(
    string? Tenant,
    string? Actor,
    string? Command,
    string? Resource,
    string? Decision,
    string? Reason,
    string? Correlation,
    string? MessageId,
    string? Surface,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit);
