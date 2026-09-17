using System.Globalization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using CommandSubmissionResponse = Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse;

namespace Hexalith.ChatBot.UI.Services;

public sealed record ComplianceAuditDetailModel(
    string AuditRecordRef,
    string Command,
    string Correlation,
    string PolicySnapshot,
    string Redaction,
    string Escalation,
    string SafeNextAction,
    string RedactionReasonCode,
    IReadOnlyList<string> VisibleMetadataRefs);
