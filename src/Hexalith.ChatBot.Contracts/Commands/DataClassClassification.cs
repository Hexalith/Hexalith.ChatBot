using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The full classification tuple for a single ChatBot-owned data class (NFR52/NFR53). Every field is a bounded,
/// <c>AuditMetadata</c>-safe token. <see cref="DataClassId"/> and <see cref="RetentionClassId"/> reference the one
/// canonical <see cref="ComplianceRetentionClassIds"/> spine; <see cref="OwnerRole"/> is an <c>AdminRoles</c> wire
/// token; the three sensitivity/behavior/eligibility dimensions are closed sets; <see cref="MinimizationRuleRef"/>
/// is a safe compliance token describing the NFR52 minimization constraint (never raw content).
/// </summary>
public sealed record DataClassClassification(
    string DataClassId,
    string OwnerRole,
    string RetentionClassId,
    string RedactionSensitivity,
    string DeletionBehavior,
    string ExportEligibility,
    string MinimizationRuleRef);
