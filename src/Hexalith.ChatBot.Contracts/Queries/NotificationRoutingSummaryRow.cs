using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// A summary-safe routing-map row. All fields are declared enum/token values; no recipient PII.
/// </summary>
public sealed record NotificationRoutingSummaryRow(
    string StateClass,
    string Scope,
    string RecipientRole,
    string Channel);
