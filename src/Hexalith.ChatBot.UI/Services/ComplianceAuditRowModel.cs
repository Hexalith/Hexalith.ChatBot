using System.Globalization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using CommandSubmissionResponse = Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse;

namespace Hexalith.ChatBot.UI.Services;

public sealed record ComplianceAuditRowModel(
    string AuditRecordRef,
    string Actor,
    string ActorType,
    string Command,
    string Decision,
    string Reason,
    string Correlation,
    string PolicySnapshot,
    string Redaction,
    string Escalation,
    string SafeNextAction,
    string TimestampZ);
