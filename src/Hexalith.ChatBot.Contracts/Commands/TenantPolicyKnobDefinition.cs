using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record TenantPolicyKnobDefinition(
    string KnobId,
    TenantPolicyKnobType Type,
    TenantPolicyKnobSensitivity Sensitivity,
    string SchemaVersion,
    double? Minimum = null,
    double? Maximum = null,
    IReadOnlyList<string>? EnumValues = null);
