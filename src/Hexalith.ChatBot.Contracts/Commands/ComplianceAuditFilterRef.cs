using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ComplianceAuditFilterRef(
    string FilterRef,
    string FilterKey,
    string ValueRef);
